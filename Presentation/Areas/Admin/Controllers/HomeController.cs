using Microsoft.AspNetCore.Mvc;

namespace Presentation.Areas.Admin.Controllers;

public class HomeController : AdminController
{
    public IActionResult Index()
    {
        return View();
    }
}