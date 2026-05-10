using App.Models;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

public class AccountController(IAuthService authService) : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var succeeded = await authService.SignInAsync(model.Username, model.Password, model.RememberMe);

            if (succeeded)
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await authService.SignOutAsync();
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }
}
