using ConcesionariaApp.Data;
using ConcesionariaApp.Models;
using ConcesionariaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConcesionariaApp.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Auditoria")]
public class AuditoriaController(ApplicationDbContext db, DashboardAggregationService dashboard) : Controller
{
    private const int PageSize = 10;
    private static readonly TimeZoneInfo ArgentinaTimeZone = ResolveArgentinaTimeZone();

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? preset,
        DateTime? desde,
        DateTime? hasta,
        int? usuarioId,
        string? accion,
        int page = 1)
    {
        DashboardDateRange range;
        try
        {
            range = dashboard.ResolveRange(preset, desde, hasta);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        var query = db.RegistrosAuditoria.AsNoTracking()
            .Where(x => x.Fecha >= ToUtc(range.Desde) && x.Fecha < ToUtc(range.Hasta.AddDays(1)));

        if (usuarioId.HasValue)
            query = query.Where(x => x.UsuarioId == usuarioId.Value);
        if (!string.IsNullOrWhiteSpace(accion))
            query = query.Where(x => x.Accion == accion);

        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)PageSize)));
        var records = await query
            .OrderByDescending(x => x.Fecha)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(x => new AuditoriaListItem
            {
                Id = x.Id,
                Fecha = x.Fecha,
                Usuario = x.Usuario.Nombre,
                Rol = x.Usuario.Rol,
                Accion = x.Accion,
                EntidadAfectada = x.EntidadAfectada,
                EntidadId = x.EntidadId,
                DetalleJson = x.DetalleJson
            })
            .ToListAsync();

        return View(new AuditoriaPageViewModel
        {
            Preset = dashboard.ParsePreset(preset),
            Desde = range.Desde,
            Hasta = range.Hasta,
            UsuarioId = usuarioId,
            Accion = accion,
            Usuarios = await db.Usuarios.AsNoTracking()
                .Where(x => x.Rol == Rol.Admin || x.Rol == Rol.Vendedor)
                .OrderBy(x => x.Nombre)
                .Select(x => new AuditoriaUsuarioOption { Id = x.Id, Nombre = x.Nombre, Rol = x.Rol, Activo = x.Activo })
                .ToListAsync(),
            Acciones = await db.RegistrosAuditoria.AsNoTracking()
                .Select(x => x.Accion)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(),
            Registros = new PagedResult<AuditoriaListItem>
            {
                Items = records.Select(x => new AuditoriaListItem
                {
                    Id = x.Id,
                    Fecha = FromUtc(x.Fecha),
                    Usuario = x.Usuario,
                    Rol = x.Rol,
                    Accion = x.Accion,
                    EntidadAfectada = x.EntidadAfectada,
                    EntidadId = x.EntidadId,
                    DetalleJson = x.DetalleJson
                }).ToList(),
                Page = page,
                PageSize = PageSize,
                TotalItems = total
            }
        });
    }

    private static DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), ArgentinaTimeZone);

    private static DateTime FromUtc(DateTime value) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value, DateTimeKind.Utc), ArgentinaTimeZone);

    private static TimeZoneInfo ResolveArgentinaTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }
}
