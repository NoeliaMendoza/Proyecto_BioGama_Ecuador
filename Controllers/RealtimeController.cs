using Microsoft.AspNetCore.Mvc;

namespace BioGamaEcuador.Controllers;

public class RealtimeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Products");
}
