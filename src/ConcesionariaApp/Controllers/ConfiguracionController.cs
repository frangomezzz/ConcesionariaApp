using ConcesionariaApp.Data;
using ConcesionariaApp.Models;
using ConcesionariaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConcesionariaApp.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Configuracion")]
public class ConfiguracionController(ApplicationDbContext db, AuditoriaService auditoria) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await ConstruirModeloAsync());

    [HttpPost("Tipo/{tipo}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarTipo(TipoVehiculo tipo, ComisionTipoFormViewModel model)
    {
        if (!Enum.IsDefined(tipo)) return BadRequest();
        if (!ModelState.IsValid)
            return await VistaConConfiguracionAsync();

        var existente = await db.ComisionesPorTipoVehiculo.FindAsync(tipo);
        var antes = existente is null ? null : Snapshot(existente);
        if (existente is null)
        {
            existente = new ComisionPorTipoVehiculo { Tipo = tipo };
            db.ComisionesPorTipoVehiculo.Add(existente);
        }
        existente.PorcentajeBase = model.PorcentajeBase;
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync(
            antes is null ? "CargoComisionPorTipoVehiculo" : "EditoComisionPorTipoVehiculo",
            nameof(ComisionPorTipoVehiculo),
            (int)tipo,
            antes,
            Snapshot(existente));
        TempData["Success"] = "Comisión por tipo de vehículo actualizada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Antiguedad/Nuevo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NuevoAntiguedad(TramoAntiguedadFormViewModel model)
    {
        if (!ModelState.IsValid)
            return await VistaConConfiguracionAsync();

        var tramo = new ComisionPorAntiguedad
        {
            MesesMin = model.MesesMin,
            MesesMax = model.MesesMax,
            PorcentajeAdicional = model.PorcentajeAdicional
        };
        db.ComisionesPorAntiguedad.Add(tramo);
        var error = await ValidarAntiguedadAsync(tramo);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            return await VistaConConfiguracionAsync();
        }

        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync("CargoComisionPorAntiguedad", nameof(ComisionPorAntiguedad), tramo.Id, despues: Snapshot(tramo));
        TempData["Success"] = "Tramo de antigüedad agregado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Antiguedad/Editar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarAntiguedad(int id, TramoAntiguedadFormViewModel model)
    {
        var tramo = await db.ComisionesPorAntiguedad.FindAsync(id);
        if (tramo is null) return NotFound();
        if (!ModelState.IsValid)
            return await VistaConConfiguracionAsync();

        var antes = Snapshot(tramo);
        tramo.MesesMin = model.MesesMin;
        tramo.MesesMax = model.MesesMax;
        tramo.PorcentajeAdicional = model.PorcentajeAdicional;
        var error = await ValidarAntiguedadAsync(tramo, id);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            return await VistaConConfiguracionAsync();
        }

        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync("EditoComisionPorAntiguedad", nameof(ComisionPorAntiguedad), id, antes, Snapshot(tramo));
        TempData["Success"] = "Tramo de antigüedad actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Antiguedad/Eliminar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarAntiguedad(int id)
    {
        var tramo = await db.ComisionesPorAntiguedad.FindAsync(id);
        if (tramo is null) return NotFound();

        db.ComisionesPorAntiguedad.Remove(tramo);
        var error = await ValidarAntiguedadAsync(excluirId: id);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            return await VistaConConfiguracionAsync();
        }

        var antes = Snapshot(tramo);
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync("EliminoComisionPorAntiguedad", nameof(ComisionPorAntiguedad), id, antes: antes);
        TempData["Success"] = "Tramo de antigüedad eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Cuotas/Nuevo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NuevoCuotas(TramoCuotasFormViewModel model)
    {
        if (!ModelState.IsValid)
            return await VistaConConfiguracionAsync();

        var tramo = new RecargoPorCuotas
        {
            CuotasMin = model.CuotasMin,
            CuotasMax = model.CuotasMax,
            PorcentajeRecargo = model.PorcentajeRecargo
        };
        db.RecargosPorCuotas.Add(tramo);
        var error = await ValidarCuotasAsync(tramo);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            return await VistaConConfiguracionAsync();
        }

        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync("CargoRecargoPorCuotas", nameof(RecargoPorCuotas), tramo.Id, despues: Snapshot(tramo));
        TempData["Success"] = "Tramo de cuotas agregado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Cuotas/Editar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarCuotas(int id, TramoCuotasFormViewModel model)
    {
        var tramo = await db.RecargosPorCuotas.FindAsync(id);
        if (tramo is null) return NotFound();
        if (!ModelState.IsValid)
            return await VistaConConfiguracionAsync();

        var antes = Snapshot(tramo);
        tramo.CuotasMin = model.CuotasMin;
        tramo.CuotasMax = model.CuotasMax;
        tramo.PorcentajeRecargo = model.PorcentajeRecargo;
        var error = await ValidarCuotasAsync(tramo, id);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            return await VistaConConfiguracionAsync();
        }

        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync("EditoRecargoPorCuotas", nameof(RecargoPorCuotas), id, antes, Snapshot(tramo));
        TempData["Success"] = "Tramo de cuotas actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Cuotas/Eliminar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCuotas(int id)
    {
        var tramo = await db.RecargosPorCuotas.FindAsync(id);
        if (tramo is null) return NotFound();

        db.RecargosPorCuotas.Remove(tramo);
        var error = await ValidarCuotasAsync(excluirId: id);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            return await VistaConConfiguracionAsync();
        }

        var antes = Snapshot(tramo);
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync("EliminoRecargoPorCuotas", nameof(RecargoPorCuotas), id, antes: antes);
        TempData["Success"] = "Tramo de cuotas eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> VistaConConfiguracionAsync() => View(nameof(Index), await ConstruirModeloAsync());

    private async Task<ConfiguracionIndexViewModel> ConstruirModeloAsync()
    {
        var comisiones = await db.ComisionesPorTipoVehiculo.AsNoTracking().ToDictionaryAsync(x => x.Tipo);
        return new ConfiguracionIndexViewModel
        {
            ComisionesPorTipo = Enum.GetValues<TipoVehiculo>().Select(tipo => new ComisionTipoItemViewModel
            {
                Tipo = tipo,
                PorcentajeBase = comisiones.TryGetValue(tipo, out var comision) ? comision.PorcentajeBase : 0m,
                Configurada = comisiones.ContainsKey(tipo)
            }).ToList(),
            ComisionesPorAntiguedad = await db.ComisionesPorAntiguedad.AsNoTracking().OrderBy(x => x.MesesMin).Select(x => new TramoAntiguedadItemViewModel
            {
                Id = x.Id, MesesMin = x.MesesMin, MesesMax = x.MesesMax, PorcentajeAdicional = x.PorcentajeAdicional
            }).ToListAsync(),
            RecargosPorCuotas = await db.RecargosPorCuotas.AsNoTracking().OrderBy(x => x.CuotasMin).Select(x => new TramoCuotasItemViewModel
            {
                Id = x.Id, CuotasMin = x.CuotasMin, CuotasMax = x.CuotasMax, PorcentajeRecargo = x.PorcentajeRecargo
            }).ToListAsync()
        };
    }

    private async Task<string?> ValidarAntiguedadAsync(ComisionPorAntiguedad? agregado = null, int? excluirId = null)
    {
        var tramos = await db.ComisionesPorAntiguedad.AsNoTracking()
            .Where(x => !excluirId.HasValue || x.Id != excluirId.Value)
            .ToListAsync();
        if (agregado is null)
            return ValidadorTramosConfiguracion.ValidarAntiguedad(tramos);
        return ValidadorTramosConfiguracion.ValidarAntiguedad(tramos.Append(agregado));
    }

    private async Task<string?> ValidarCuotasAsync(RecargoPorCuotas? agregado = null, int? excluirId = null)
    {
        var tramos = await db.RecargosPorCuotas.AsNoTracking()
            .Where(x => !excluirId.HasValue || x.Id != excluirId.Value)
            .ToListAsync();
        if (agregado is null)
            return ValidadorTramosConfiguracion.ValidarCuotas(tramos);
        return ValidadorTramosConfiguracion.ValidarCuotas(tramos.Append(agregado));
    }

    private static object Snapshot(ComisionPorTipoVehiculo x) => new { Tipo = x.Tipo.ToString(), x.PorcentajeBase };
    private static object Snapshot(ComisionPorAntiguedad x) => new { x.Id, x.MesesMin, x.MesesMax, x.PorcentajeAdicional };
    private static object Snapshot(RecargoPorCuotas x) => new { x.Id, x.CuotasMin, x.CuotasMax, x.PorcentajeRecargo };
}
