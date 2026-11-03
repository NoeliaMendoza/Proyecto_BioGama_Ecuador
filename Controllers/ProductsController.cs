using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BioGamaEcuador.Data;

namespace BioGamaEcuador.Controllers;

public class ProductsController(AppDbContext context) : Controller
{
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Availability(string? search, int page = 1)
    {
        const int pageSize = 12;
        var query = context.PhysicalProducts.AsNoTracking()
            .Where(p => p.IsActive && p.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search.Trim()}%"));
        var count = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(count / (double)pageSize)));
        var total = await query.SumAsync(p => (int?)(p.Stock - p.ReservedStock)) ?? 0;
        var products = await query.OrderBy(p => p.Name).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new { p.Id, available = p.Stock - p.ReservedStock, p.MinStock })
            .ToListAsync();
        return Json(new { total, products });
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        const int pageSize = 12;
        var q = context.PhysicalProducts.AsNoTracking().Where(p => p.IsActive && p.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => EF.Functions.ILike(p.Name, $"%{search.Trim()}%"));
        var total = await q.CountAsync();
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        ViewBag.Page = Math.Clamp(page, 1, (int)ViewBag.TotalPages);
        ViewBag.Search = search;
        ViewBag.TotalProducts = total;
        ViewBag.AvailableUnits = await q.SumAsync(p => (int?)(p.Stock - p.ReservedStock)) ?? 0;
        return View(await q.OrderBy(p => p.Name).Skip(((int)ViewBag.Page - 1) * pageSize).Take(pageSize).ToListAsync());
    }
}
