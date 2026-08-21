using ConcesionariaApp.Data;
using ConcesionariaApp.Models;
using ConcesionariaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConcesionariaApp.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Reportes")]
public class ReportesController(ApplicationDbContext db, DashboardAggregationService dashboard) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var range = new DashboardDateRange(new DateTime(today.Year, today.Month, 1), today);
        return View(new ReportesPageViewModel
        {
            Nombre = User.Identity?.Name ?? "usuario",
            DataUrl = Url.Action(nameof(Data), "Reportes") ?? "/Admin/Reportes/Data",
            Preset = DashboardDatePreset.EsteMes,
            Desde = range.Desde,
            Hasta = range.Hasta,
            Vendedores = await SellerOptionsAsync(),
            Data = await dashboard.GetReportesAsync(range)
        });
    }

    [HttpGet("Data")]
    public async Task<IActionResult> Data(
        string? preset,
        DateTime? desde,
        DateTime? hasta,
        int? vendedorId,
        TipoVehiculo? tipo,
        string? marcaModelo)
    {
        try
        {
            var range = dashboard.ResolveRange(preset, desde, hasta);
            return Ok(await dashboard.GetReportesAsync(range, vendedorId, tipo, marcaModelo));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    private async Task<IReadOnlyList<ReportesSellerOption>> SellerOptionsAsync() =>
        await db.Usuarios.AsNoTracking()
            .Where(x => x.Rol == Rol.Vendedor)
            .OrderBy(x => x.Nombre)
            .Select(x => new ReportesSellerOption { Id = x.Id, Nombre = x.Nombre, Rol = x.Rol, Activo = x.Activo })
            .ToListAsync();
}
