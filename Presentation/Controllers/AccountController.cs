using App.Models;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

public class AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager) : Controller
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
            var result = await signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, false);

            if (result.Succeeded)
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
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }
}