using BioGamaEcuador.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BioGamaEcuador.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ChatController(ChatCredentials credentials) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.ChatUrl = credentials.Enabled ? credentials.PublicUrl : null;
        ViewBag.IsChatAdmin = ChatCredentials.IsAdministrator(User);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Token() => credentials.Enabled
        ? Json(credentials.Issue(User))
        : StatusCode(503, new { error = "El servicio de chat no está configurado." });
}
