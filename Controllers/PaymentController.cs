using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BioGamaEcuador.Data;
using BioGamaEcuador.Models.Sales;
using BioGamaEcuador.Services;
using BioGamaEcuador.Services.Payments;
using System.Text.Json;
using System.Security.Claims;

namespace BioGamaEcuador.Controllers;

[Authorize]
public class PaymentController : Controller
{
    private readonly AppDbContext _context;
    private readonly PayPhoneApiLinkService _payPhoneService;
    private readonly PayPalService _payPal;
    private readonly IInventoryService _inventory;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;
    private readonly StockNotifications _stockNotifications;

    public PaymentController(
        AppDbContext context,
        PayPhoneApiLinkService payPhoneService,
        PayPalService payPal,
        IInventoryService inventory,
        IEmailService email,
        IAuditService audit,
        StockNotifications stockNotifications)
    {
        _context = context;
        _payPhoneService = payPhoneService;
        _payPal = payPal;
        _inventory = inventory;
        _email = email;
        _audit = audit;
        _stockNotifications = stockNotifications;
    }

    public async Task<IActionResult> CreateLink(Guid orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Administrador");
        var order = await _context.Orders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.Status == "Pending" && (isAdmin || o.UserId == userId));

        if (order == null) return NotFound();

        string clientTxId = $"BIO-{Guid.NewGuid():N}"[..15];

        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
        if (payment == null)
        {
            payment = new Payment
            {
                OrderId = order.Id,
                Provider = "PayPhone",
                ExternalId = clientTxId,
                Amount = order.Total,
                Currency = "USD",
                Status = "Pending"
            };
            _context.Payments.Add(payment);
        }
        else
        {
            payment.ExternalId = clientTxId;
            payment.Provider = "PayPhone";
            payment.Amount = order.Total;
            payment.Status = "Pending";
        }
        await _context.SaveChangesAsync();

        try
        {
            var url = await _payPhoneService.CreatePaymentLinkAsync(
                order.Total, clientTxId, order.Id.ToString());
            payment.GatewayResponse = url;
            await _context.SaveChangesAsync();
            return Redirect(url);
        }
        catch (Exception ex)
        {
            TempData["Warning"] = ex.Message + " Puedes marcar el pago como pagado manualmente desde el panel de administrador.";
            return RedirectToAction(nameof(Details), new { id = payment.Id });
        }
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Administrador");
        var payment = await _context.Payments
            .Include(p => p.Order)
            .ThenInclude(o => o.Details)
            .FirstOrDefaultAsync(p => p.Id == id && (isAdmin || p.Order.UserId == userId));

        if (payment == null) return NotFound();

        return View(payment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> MarkAsPaid(Guid id)
    {
        var payment = await _context.Payments
            .Include(p => p.Order)
            .ThenInclude(o => o.Details)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null) return NotFound();
        if (payment.Status != "Pending") return RedirectToAction(nameof(Details), new { id });

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            payment.Status = "Approved";
            payment.ConfirmedAt = DateTime.UtcNow;

            var order = payment.Order;
            order.Status = "Confirmed";

            foreach (var detail in order.Details)
            {
                if (detail.PhysicalProductId.HasValue)
                {
                    await _inventory.ConfirmAsync(detail.PhysicalProductId.Value, detail.Quantity, $"Order:{order.Id}", "system");
                }
                else if (detail.CourseId.HasValue)
                {
                    var enrollment = await _context.Enrollments
                        .FirstOrDefaultAsync(e => e.OrderId == order.Id && e.CourseId == detail.CourseId);
                    if (enrollment != null) enrollment.Status = "Confirmed";
                    var course = await _context.Courses.FindAsync(detail.CourseId);
                    if (course != null)
                    {
                        course.ReservedSeats -= detail.Quantity;
                        course.ConfirmedSeats += detail.Quantity;
                    }
                    await _inventory.LogConfirmationAsync(detail.CourseId.Value, detail.Quantity, $"Order:{order.Id}", "system");
                }
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            await _stockNotifications.PublishAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

            try { await _audit.LogAsync("PaymentMarkedAsPaid", "Payment", id.ToString(), "Pending", "Approved", User.Identity!.Name ?? "admin", HttpContext.Connection.RemoteIpAddress?.ToString()); } catch { }
            TempData["Success"] = "Pago marcado como pagado correctamente.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [AllowAnonymous]
    [HttpPost("api/payments/payphone/webhook")]
    public async Task<IActionResult> Webhook([FromBody] PayPhoneWebhook? body)
    {
        if (!ModelState.IsValid || body == null || string.IsNullOrWhiteSpace(body.ClientTransactionId) || body.ClientTransactionId.Length > 100)
            return BadRequest("Falta clientTransactionId.");

        var payment = await _context.Payments
            .Include(p => p.Order).ThenInclude(o => o.Details)
            .FirstOrDefaultAsync(p => p.ExternalId == body.ClientTransactionId && p.Provider == "PayPhone");

        if (payment == null) return Ok("not_found");
        if (payment.Status != "Pending") return Ok(payment.Status);

        VerifiedPayPhoneTransaction verified;
        try
        {
            verified = await _payPhoneService.GetTransactionAsync(body.ClientTransactionId, HttpContext.RequestAborted);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or JsonException or OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "No se pudo verificar el pago con PayPhone.");
        }
        var expectedCents = decimal.Round(payment.Amount * 100, 0, MidpointRounding.AwayFromZero);
        if (verified.ClientTransactionId != payment.ExternalId || verified.Amount != expectedCents ||
            !string.Equals(verified.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
            return BadRequest("La transaccion verificada no coincide con el pago.");
        if (verified.StatusCode is not (2 or 3)) return Accepted("pending_verification");

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var approved = verified.StatusCode == 3;
            payment.Status = approved ? "Approved" : "Failed";
            payment.ConfirmedAt = approved ? DateTime.UtcNow : null;

            var order = payment.Order;
            order.Status = approved ? "Confirmed" : "Cancelled";

            foreach (var detail in order.Details)
            {
                if (detail.PhysicalProductId.HasValue)
                {
                    if (approved)
                        await _inventory.ConfirmAsync(detail.PhysicalProductId.Value, detail.Quantity, $"Order:{order.Id}", "system");
                    else
                        await _inventory.ReleaseAsync(detail.PhysicalProductId.Value, detail.Quantity, $"Order:{order.Id}|failed", "system");
                }
                else if (detail.CourseId.HasValue)
                {
                    var enrollment = await _context.Enrollments
                        .FirstOrDefaultAsync(e => e.OrderId == order.Id && e.CourseId == detail.CourseId);
                    if (approved)
                    {
                        if (enrollment != null) enrollment.Status = "Confirmed";
                        var course = await _context.Courses.FindAsync(detail.CourseId);
                        if (course != null)
                        {
                            course.ReservedSeats -= detail.Quantity;
                            course.ConfirmedSeats += detail.Quantity;
                        }
                        await _inventory.LogConfirmationAsync(detail.CourseId.Value, detail.Quantity, $"Order:{order.Id}", "system");
                    }
                    else
                    {
                        if (enrollment != null) enrollment.Status = "Cancelled";
                        await _context.Database.ExecuteSqlInterpolatedAsync(
                            $"UPDATE \"Courses\" SET \"ReservedSeats\" = GREATEST(0, \"ReservedSeats\" - {detail.Quantity}) WHERE \"Id\" = {detail.CourseId}");
                        await _inventory.LogReleaseAsync(detail.CourseId.Value, detail.Quantity, $"Order:{order.Id}|failed", "system");
                    }
                }
            }

            if (!approved && order.UserId != null)
            {
                var user = await _context.Users.FindAsync(order.UserId);
                if (user?.Email != null)
                    await _email.SendPaymentFailedAsync(user.Email, order.Id, "Pago rechazado por PayPhone");
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            await _stockNotifications.PublishAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

            try { await _audit.LogAsync("PaymentWebhookReceived", "Payment", payment.Id.ToString(), "Pending", payment.Status, "system", body.ClientTransactionId); } catch { }

        return Ok(payment.Status);
    }

    [HttpGet("api/payments/paypal/confirm")]
    [HttpPost("api/payments/paypal/confirm")]
    public async Task<IActionResult> ConfirmPayPal([FromForm] string token, [FromQuery] string? token_q)
    {
        token = token ?? token_q ?? "";
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest("Falta token de PayPal.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Administrador");
        var payment = await _context.Payments
            .Include(p => p.Order).ThenInclude(o => o.Details)
            .FirstOrDefaultAsync(p => p.ExternalId == token && p.Provider == "PayPal" &&
                (isAdmin || p.Order.UserId == userId));

        if (payment == null)
            return NotFound("Orden o pago no encontrado.");

        if (payment.Status != "Pending")
            return RedirectToActionResult(payment.OrderId);

        var capture = await _payPal.CaptureOrderAsync(token);
        if (capture.Status != "COMPLETED") return BadRequest("PayPal no confirmo el pago.");

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = payment.Order;
            if (order == null) return NotFound();

            payment.Status = "Approved";
            payment.ConfirmedAt = DateTime.UtcNow;
            payment.GatewayResponse = capture.RawResponse;
            order.Status = "Confirmed";

            foreach (var detail in order.Details)
            {
                if (detail.CourseId.HasValue)
                {
                    var enrollment = await _context.Enrollments
                        .FirstOrDefaultAsync(e => e.OrderId == order.Id && e.CourseId == detail.CourseId);
                    if (enrollment != null)
                    {
                        enrollment.Status = "Confirmed";
                        var course = await _context.Courses.FindAsync(detail.CourseId);
                        if (course != null)
                        {
                            course.ReservedSeats -= detail.Quantity;
                            course.ConfirmedSeats += detail.Quantity;
                        }
                        await _inventory.LogConfirmationAsync(detail.CourseId.Value, detail.Quantity, $"Enrollment:{enrollment.Id}", "system");
                    }
                }
                else if (detail.PhysicalProductId.HasValue)
                {
                    await _inventory.ConfirmAsync(detail.PhysicalProductId.Value, detail.Quantity, $"Order:{order.Id}", "system");
                }
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            await _stockNotifications.PublishAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

            try { await _audit.LogAsync("PaymentApproved", "Payment", payment.Id.ToString(), "Pending", "Approved", User.Identity!.Name ?? "system", HttpContext.Connection.RemoteIpAddress?.ToString()); } catch { }

        return RedirectToActionResult(payment.OrderId);
    }

    private IActionResult RedirectToActionResult(Guid orderId)
    {
        TempData["Success"] = "Pago confirmado correctamente.";
        return RedirectToAction("Confirmation", "Orders", new { id = orderId });
    }
}

public class PayPhoneWebhook
{
    public string? ClientTransactionId { get; set; }
    public string? Status { get; set; }
    public string? TransactionStatus { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? Reference { get; set; }
}
