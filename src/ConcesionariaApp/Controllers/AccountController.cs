using ConcesionariaApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ConcesionariaApp.Controllers;

[Route("Account")]
public class AccountController(
    SignInManager<Usuario> signInManager,
    UserManager<Usuario> userManager) : Controller
{
    [HttpGet("Login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return await RedirectByRole();

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost("Login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        var account = await userManager.FindByEmailAsync(model.Email);
        if (account is null || !account.Activo)
        {
            ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
            return await RedirectByRole();

        // Keep authentication failures indistinguishable to avoid account enumeration.
        ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
        return View(model);
    }

    [Authorize]
    [HttpPost("Logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("AccessDenied")]
    public IActionResult AccessDenied() => View();

    private async Task<IActionResult> RedirectByRole()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction(nameof(Login));

        if (await userManager.IsInRoleAsync(user, Rol.Admin.ToString()))
            return RedirectToAction("Dashboard", "Admin");

        if (await userManager.IsInRoleAsync(user, Rol.Vendedor.ToString()))
            return RedirectToAction("Dashboard", "Vendedor");

        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(AccessDenied));
    }
}
