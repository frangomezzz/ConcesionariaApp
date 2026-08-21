using ConcesionariaApp.Models;
using ConcesionariaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ConcesionariaApp.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin")]
public class AdminController(UserManager<Usuario> userManager, DashboardAggregationService dashboard) : Controller
{
    [HttpGet("Dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var user = await userManager.GetUserAsync(User);
        var today = DateTime.Today;
        var range = new DashboardDateRange(new DateTime(today.Year, today.Month, 1), today);
        return View(new DashboardPageViewModel
        {
            Nombre = user?.Nombre ?? User.Identity?.Name ?? "usuario",
            Rol = Rol.Admin.ToString(),
            DataUrl = Url.Action(nameof(DashboardData), "Admin") ?? "/Admin/DashboardData",
            Preset = DashboardDatePreset.EsteMes,
            Desde = range.Desde,
            Hasta = range.Hasta,
            Data = await dashboard.GetAdminAsync(range)
        });
    }

    [HttpGet("DashboardData")]
    public async Task<IActionResult> DashboardData(
        string? preset,
        DateTime? desde,
        DateTime? hasta)
    {
        try
        {
            var range = dashboard.ResolveRange(preset, desde, hasta);
            return Ok(await dashboard.GetAdminAsync(range));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
