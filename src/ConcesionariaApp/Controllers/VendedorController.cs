using ConcesionariaApp.Models;
using ConcesionariaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ConcesionariaApp.Controllers;

[Authorize(Roles = "Vendedor")]
[Route("Vendedor")]
public class VendedorController(UserManager<Usuario> userManager, DashboardAggregationService dashboard) : Controller
{
    [HttpGet("Dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var today = DateTime.Today;
        var range = new DashboardDateRange(new DateTime(today.Year, today.Month, 1), today);

        return View(new DashboardPageViewModel
        {
            Nombre = user.Nombre,
            Rol = Rol.Vendedor.ToString(),
            DataUrl = Url.Action(nameof(DashboardData), "Vendedor") ?? "/Vendedor/DashboardData",
            Preset = DashboardDatePreset.EsteMes,
            Desde = range.Desde,
            Hasta = range.Hasta,
            Data = await dashboard.GetSellerAsync(range, user.Id)
        });
    }

    [HttpGet("DashboardData")]
    public async Task<IActionResult> DashboardData(
        string? preset,
        DateTime? desde,
        DateTime? hasta)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        try
        {
            var range = dashboard.ResolveRange(preset, desde, hasta);
            return Ok(await dashboard.GetSellerAsync(range, user.Id));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
