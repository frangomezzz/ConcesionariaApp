using ConcesionariaApp.Data;
using ConcesionariaApp.Models;
using ConcesionariaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConcesionariaApp.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Vehiculos")]
public class VehiculosController(ApplicationDbContext db, AuditoriaService auditoria) : Controller
{
    private const int PageSize = 8;

    [HttpGet("")]
    public async Task<IActionResult> Index(TipoVehiculo? tipo, EstadoVehiculo? estado, bool? activo, int page = 1)
    {
        var query = db.Vehiculos.AsNoTracking().AsQueryable();
        if (tipo.HasValue) query = query.Where(x => x.Tipo == tipo.Value);
        if (estado.HasValue) query = query.Where(x => x.Estado == estado.Value);
        if (activo.HasValue) query = query.Where(x => x.Activo == activo.Value);

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);
        var vehicles = await query.OrderByDescending(x => x.FechaIngreso)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .Select(x => new VehiculoListItem
            {
                Vehiculo = x,
                TieneVentas = db.Ventas.Any(v => v.VehiculoId == x.Id)
            }).ToListAsync();

        return View(new PagedResult<VehiculoListItem>
        {
            Items = vehicles, Page = page, PageSize = PageSize, TotalItems = total
        });
    }

    [HttpGet("Nuevo")]
    public IActionResult Nuevo() => View("Form", new VehiculoFormViewModel
    {
        Anio = DateTime.Today.Year,
        FechaIngreso = DateTime.Today
    });

    [HttpPost("Nuevo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Nuevo(VehiculoFormViewModel model)
    {
        model.Patente = NormalizePlate(model.Patente);
        await ValidatePlateAsync(model.Patente, null);
        if (!ModelState.IsValid) return View("Form", model);

        var vehicle = new Vehiculo
        {
            Marca = model.Marca.Trim(), Modelo = model.Modelo.Trim(), Anio = model.Anio,
            Tipo = model.Tipo, EsUsado = model.EsUsado, Patente = model.Patente,
            Color = model.Color.Trim(), Kilometraje = model.Kilometraje,
            PrecioBase = model.PrecioBase, FechaIngreso = model.FechaIngreso,
            Estado = EstadoVehiculo.Disponible, Activo = true
        };
        db.Vehiculos.Add(vehicle);
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync("CargoVehiculo", nameof(Vehiculo), vehicle.Id, despues: Snapshot(vehicle));
        TempData["Success"] = "Vehículo creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Editar/{id:int}")]
    public async Task<IActionResult> Editar(int id)
    {
        var vehicle = await db.Vehiculos.FindAsync(id);
        if (vehicle is null) return NotFound();
        return View("Form", ToForm(vehicle, await HasSales(id)));
    }

    [HttpPost("Editar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, VehiculoFormViewModel model)
    {
        var vehicle = await db.Vehiculos.FindAsync(id);
        if (vehicle is null) return NotFound();
        var before = Snapshot(vehicle);
        var hasSales = await HasSales(id);
        model.Patente = NormalizePlate(model.Patente);
        await ValidatePlateAsync(model.Patente, id);
        if (hasSales && (model.Tipo != vehicle.Tipo || model.PrecioBase != vehicle.PrecioBase))
            ModelState.AddModelError(string.Empty, "Tipo y Precio Base no se pueden modificar porque el vehículo tiene ventas asociadas.");
        if (!ModelState.IsValid)
        {
            model.TieneVentas = hasSales;
            return View("Form", model);
        }

        vehicle.Marca = model.Marca.Trim(); vehicle.Modelo = model.Modelo.Trim(); vehicle.Anio = model.Anio;
        vehicle.EsUsado = model.EsUsado; vehicle.Patente = model.Patente; vehicle.Color = model.Color.Trim();
        vehicle.Kilometraje = model.Kilometraje; vehicle.FechaIngreso = model.FechaIngreso;
        if (!hasSales)
        {
            vehicle.Tipo = model.Tipo;
            vehicle.PrecioBase = model.PrecioBase;
        }
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync("EditoVehiculo", nameof(Vehiculo), id, before, Snapshot(vehicle));
        TempData["Success"] = "Vehículo actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("CambiarEstado/{id:int}")]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var vehicle = await db.Vehiculos.FindAsync(id);
        if (vehicle is null) return NotFound();
        return View(new VehiculoEstadoViewModel { Id = id, Descripcion = $"{vehicle.Anio} {vehicle.Marca} {vehicle.Modelo}", Estado = vehicle.Estado });
    }

    [HttpPost("CambiarEstado/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, VehiculoEstadoViewModel model)
    {
        var vehicle = await db.Vehiculos.FindAsync(id);
        if (vehicle is null) return NotFound();
        if (!ModelState.IsValid) { model.Id = id; model.Descripcion = $"{vehicle.Anio} {vehicle.Marca} {vehicle.Modelo}"; return View(model); }
        var before = Snapshot(vehicle);
        vehicle.Estado = model.Estado;
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync("CambioEstadoVehiculo", nameof(Vehiculo), id, before, Snapshot(vehicle));
        TempData["Success"] = "Estado del vehículo actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Desactivar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id)
    {
        var vehicle = await db.Vehiculos.FindAsync(id);
        if (vehicle is null) return NotFound();
        var before = Snapshot(vehicle);
        vehicle.Activo = false;
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync("DesactivoVehiculo", nameof(Vehiculo), id, before, Snapshot(vehicle));
        TempData["Success"] = "Vehículo desactivado. Su estado comercial fue conservado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Reactivar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id)
    {
        var vehicle = await db.Vehiculos.FindAsync(id);
        if (vehicle is null) return NotFound();
        var before = Snapshot(vehicle);
        vehicle.Activo = true;
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync("ReactivoVehiculo", nameof(Vehiculo), id, before, Snapshot(vehicle));
        TempData["Success"] = "Vehículo reactivado conservando su estado comercial.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> HasSales(int id) => await db.Ventas.AnyAsync(x => x.VehiculoId == id);

    private async Task ValidatePlateAsync(string? plate, int? currentId)
    {
        if (!string.IsNullOrEmpty(plate) && await db.Vehiculos.AnyAsync(x => x.Patente == plate && (!currentId.HasValue || x.Id != currentId.Value)))
            ModelState.AddModelError(nameof(VehiculoFormViewModel.Patente), "Ya existe otro vehículo con esa patente.");
    }

    private static string? NormalizePlate(string? plate) => string.IsNullOrWhiteSpace(plate) ? null : plate.Trim().ToUpperInvariant();
    private static VehiculoFormViewModel ToForm(Vehiculo x, bool hasSales) => new()
    {
        Id = x.Id, Marca = x.Marca, Modelo = x.Modelo, Anio = x.Anio, Tipo = x.Tipo,
        EsUsado = x.EsUsado, Patente = x.Patente, Color = x.Color, Kilometraje = x.Kilometraje,
        PrecioBase = x.PrecioBase, FechaIngreso = x.FechaIngreso, TieneVentas = hasSales
    };
    private static object Snapshot(Vehiculo x) => new { x.Id, x.Marca, x.Modelo, x.Anio, Tipo = x.Tipo.ToString(), x.EsUsado, x.Patente, x.Color, x.Kilometraje, x.PrecioBase, Estado = x.Estado.ToString(), x.FechaIngreso, x.Activo };
}
