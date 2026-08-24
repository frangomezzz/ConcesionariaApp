using System.Data;
using System.Security.Claims;
using ConcesionariaApp.Data;
using ConcesionariaApp.Models;
using ConcesionariaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConcesionariaApp.Controllers;

[Authorize(Roles = "Admin,Vendedor")]
[Route("Ventas")]
public class VentasController(
    ApplicationDbContext db,
    UserManager<Usuario> userManager,
    ICalculoVentaService calculoVenta,
    AuditoriaService auditoria) : Controller
{
    private const int PageSize = 8;

    [Authorize(Roles = "Admin")]
    [HttpGet("")]
    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, EstadoVenta? estado, string? buscar, int page = 1)
    {
        var query = db.Ventas.AsNoTracking().AsQueryable();
        query = ApplyDateFilter(query, desde, hasta);
        if (estado.HasValue) query = query.Where(x => x.Estado == estado.Value);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim();
            query = query.Where(x =>
                (x.Cliente.Nombre + " " + x.Cliente.Apellido).Contains(term) ||
                x.Vehiculo.Marca.Contains(term) || x.Vehiculo.Modelo.Contains(term) ||
                (x.Vehiculo.Patente != null && x.Vehiculo.Patente.Contains(term)) ||
                x.Vendedor.Nombre.Contains(term));
        }

        return View("AdminIndex", await BuildListModelAsync(query, page, desde, hasta, estado, buscar));
    }

    [Authorize(Roles = "Vendedor")]
    [HttpGet("Registrar")]
    public async Task<IActionResult> Registrar()
    {
        return View("Registrar", await BuildRegistrarModelAsync(new VentaRegistrarViewModel
        {
            FechaVenta = DateTime.Today,
            MetodoPago = MetodoPago.Efectivo,
            CantidadCuotas = 1
        }));
    }

    [Authorize(Roles = "Vendedor")]
    [HttpPost("Registrar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(VentaRegistrarViewModel model)
    {
        model.FechaVenta = model.FechaVenta.Date;
        if (model.MetodoPago == MetodoPago.Efectivo)
            model.CantidadCuotas = 1;

        if (!ModelState.IsValid)
            return View("Registrar", await BuildRegistrarModelAsync(model));

        var vendedor = await userManager.GetUserAsync(User);
        if (vendedor is null) return Challenge();

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var cliente = await db.Clientes.SingleOrDefaultAsync(x => x.Id == model.ClienteId!.Value);
        var vehiculo = await db.Vehiculos.SingleOrDefaultAsync(x => x.Id == model.VehiculoId!.Value);

        if (cliente is null)
            ModelState.AddModelError(nameof(model.ClienteId), "El cliente seleccionado ya no existe.");
        if (vehiculo is null || !vehiculo.Activo)
            ModelState.AddModelError(nameof(model.VehiculoId), "El vehículo seleccionado ya no está disponible.");

        if (!ModelState.IsValid)
            return View("Registrar", await BuildRegistrarModelAsync(model));

        var filasActualizadas = await db.Vehiculos
            .Where(x => x.Id == vehiculo!.Id && x.Activo && x.Estado == EstadoVehiculo.Disponible)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Estado, EstadoVehiculo.Vendido));

        if (filasActualizadas != 1)
        {
            ModelState.AddModelError(string.Empty, "El vehículo ya no está disponible. Otro vendedor pudo haberlo vendido; revisá el stock e intentá nuevamente.");
            return View("Registrar", await BuildRegistrarModelAsync(model));
        }

        try
        {
            var resultado = calculoVenta.Calcular(
                new DatosCalculoVenta(vehiculo!, vendedor, model.FechaVenta, model.MetodoPago, model.CantidadCuotas),
                await LoadConfigurationAsync());

            var venta = new Venta
            {
                VehiculoId = vehiculo!.Id,
                ClienteId = cliente!.Id,
                VendedorId = vendedor.Id,
                FechaVenta = model.FechaVenta,
                MetodoPago = resultado.MetodoPago,
                CantidadCuotas = resultado.CantidadCuotas,
                PrecioBase = resultado.PrecioBase,
                PrecioFinal = resultado.PrecioFinal,
                PorcentajeComisionAplicado = resultado.PorcentajeComisionAplicado,
                ComisionCalculada = resultado.ComisionCalculada,
                Estado = EstadoVenta.Activa,
                Observaciones = Clean(model.Observaciones)
            };
            db.Ventas.Add(venta);
            await db.SaveChangesAsync();
            await auditoria.RegistrarAsync("CargoVenta", nameof(Venta), venta.Id, despues: Snapshot(venta));
            await transaction.CommitAsync();

            TempData["Success"] = "Venta registrada correctamente.";
            return RedirectToAction(nameof(Detalle), new { id = venta.Id });
        }
        catch (ReglaCalculoVentaException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View("Registrar", await BuildRegistrarModelAsync(model));
        }
    }

    [Authorize(Roles = "Vendedor")]
    [HttpPost("CalcularPreview")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CalcularPreview(PreviewVentaRequest request)
    {
        if (request.MetodoPago == MetodoPago.Efectivo)
            request.CantidadCuotas = 1;

        var vendedor = await userManager.GetUserAsync(User);
        var vehiculo = await db.Vehiculos.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.VehiculoId && x.Activo && x.Estado == EstadoVehiculo.Disponible);
        if (vendedor is null || vehiculo is null)
            return BadRequest(new { error = "El vehículo seleccionado ya no está disponible." });

        try
        {
            var resultado = calculoVenta.Calcular(
                new DatosCalculoVenta(vehiculo, vendedor, request.FechaVenta.Date, request.MetodoPago, request.CantidadCuotas),
                await LoadConfigurationAsync());
            return Ok(new
            {
                precioBase = resultado.PrecioBase,
                precioFinal = resultado.PrecioFinal,
                porcentajeRecargo = resultado.PorcentajeRecargoAplicado,
                porcentajeComision = resultado.PorcentajeComisionAplicado,
                comision = resultado.ComisionCalculada
            });
        }
        catch (ReglaCalculoVentaException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [Authorize(Roles = "Vendedor")]
    [HttpGet("BuscarClientes")]
    public async Task<IActionResult> BuscarClientes(string? term)
    {
        var normalized = term?.Trim() ?? "";
        var clients = await db.Clientes.AsNoTracking()
            .Where(x => normalized == "" || x.Nombre.Contains(normalized) || x.Apellido.Contains(normalized) || x.DNI.Contains(normalized))
            .OrderBy(x => x.Apellido).ThenBy(x => x.Nombre).Take(8)
            .Select(x => new { id = x.Id, nombre = x.Nombre + " " + x.Apellido, dni = x.DNI, email = x.Email })
            .ToListAsync();
        return Ok(clients);
    }

    [Authorize(Roles = "Vendedor")]
    [HttpGet("BuscarVehiculos")]
    public async Task<IActionResult> BuscarVehiculos(string? term)
    {
        var normalized = term?.Trim() ?? "";
        var vehicles = await db.Vehiculos.AsNoTracking()
            .Where(x => x.Activo && x.Estado == EstadoVehiculo.Disponible &&
                (normalized == "" || x.Marca.Contains(normalized) || x.Modelo.Contains(normalized) ||
                 (x.Patente != null && x.Patente.Contains(normalized))))
            .OrderBy(x => x.Marca).ThenBy(x => x.Modelo).Take(8)
            .Select(x => new
            {
                id = x.Id,
                descripcion = x.Anio + " " + x.Marca + " " + x.Modelo,
                marca = x.Marca,
                modelo = x.Modelo,
                anio = x.Anio,
                tipo = x.Tipo.ToString(),
                color = x.Color,
                precioBase = x.PrecioBase,
                patente = x.Patente
            })
            .ToListAsync();
        return Ok(vehicles);
    }

    [Authorize(Roles = "Vendedor")]
    [HttpGet("MisVentas")]
    public async Task<IActionResult> MisVentas(DateTime? desde, DateTime? hasta, string? buscarVehiculo, EstadoVenta? estado, int page = 1)
    {
        var vendedor = await userManager.GetUserAsync(User);
        if (vendedor is null) return Challenge();

        var query = db.Ventas.AsNoTracking().Where(x => x.VendedorId == vendedor.Id);
        query = ApplyDateFilter(query, desde, hasta);
        if (estado.HasValue) query = query.Where(x => x.Estado == estado.Value);
        if (!string.IsNullOrWhiteSpace(buscarVehiculo))
        {
            var term = buscarVehiculo.Trim();
            query = query.Where(x => x.Vehiculo.Marca.Contains(term) || x.Vehiculo.Modelo.Contains(term) ||
                (x.Vehiculo.Patente != null && x.Vehiculo.Patente.Contains(term)));
        }

        return View(await BuildListModelAsync(query, page, desde, hasta, estado, buscarVehiculo));
    }

    [Authorize(Roles = "Vendedor")]
    [HttpGet("MisComisiones")]
    public async Task<IActionResult> MisComisiones(DateTime? desde, DateTime? hasta, int page = 1)
    {
        var vendedor = await userManager.GetUserAsync(User);
        if (vendedor is null) return Challenge();

        var hoy = DateTime.Today;
        var desdeReal = (desde ?? new DateTime(hoy.Year, hoy.Month, 1)).Date;
        var hastaReal = (hasta ?? hoy).Date;
        var query = db.Ventas.AsNoTracking().Where(x => x.VendedorId == vendedor.Id && x.FechaVenta >= desdeReal && x.FechaVenta < hastaReal.AddDays(1));
        var activos = query.Where(x => x.Estado == EstadoVenta.Activa);
        var items = await ProjectListQuery(query).OrderByDescending(x => x.FechaVenta).ToListAsync();
        var total = items.Count;
        page = ClampPage(page, total);

        var monthStart = new DateTime(hoy.Year, hoy.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var comisionMes = await db.Ventas.AsNoTracking()
            .Where(x => x.VendedorId == vendedor.Id && x.Estado == EstadoVenta.Activa && x.FechaVenta >= monthStart && x.FechaVenta < monthEnd)
            .SumAsync(x => (decimal?)x.ComisionCalculada) ?? 0m;

        return View(new MisComisionesViewModel
        {
            Items = items.Skip((page - 1) * PageSize).Take(PageSize).ToList(),
            Page = page,
            PageSize = PageSize,
            TotalItems = total,
            Desde = desdeReal,
            Hasta = hastaReal,
            ComisionMes = comisionMes,
            ComisionTotal = await activos.SumAsync(x => (decimal?)x.ComisionCalculada) ?? 0m,
            VentasComisionables = await activos.CountAsync()
        });
    }

    [HttpGet("Detalle/{id:int}")]
    public async Task<IActionResult> Detalle(int id, string? error = null)
    {
        var sale = await SaleForCurrentUserQuery()
            .Include(x => x.Vehiculo).Include(x => x.Cliente).Include(x => x.Vendedor).Include(x => x.AnuladoPorUsuario)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (sale is null) return NotFound();
        return View(new VentaDetalleViewModel { Venta = sale, Error = error });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("Anular/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Anular(int id, AnularVentaRequest request)
    {
        var motivo = request.MotivoAnulacion?.Trim();
        var sale = await db.Ventas.AsNoTracking().Include(x => x.Vehiculo).Include(x => x.Cliente).Include(x => x.Vendedor)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (sale is null) return NotFound();
        if (sale.Estado != EstadoVenta.Activa)
            return RedirectToAction(nameof(Detalle), new { id });
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(motivo))
            return View("../Ventas/Detalle", new VentaDetalleViewModel { Venta = sale, Error = "El motivo de anulación es obligatorio.", MotivoAnulacion = request.MotivoAnulacion ?? "" });

        var admin = await userManager.GetUserAsync(User);
        if (admin is null) return Challenge();

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var fechaAnulacion = DateTime.UtcNow;
        var changed = await db.Ventas.Where(x => x.Id == id && x.Estado == EstadoVenta.Activa)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Estado, EstadoVenta.Anulada)
                .SetProperty(x => x.FechaAnulacion, fechaAnulacion)
                .SetProperty(x => x.AnuladoPorUsuarioId, admin.Id)
                .SetProperty(x => x.MotivoAnulacion, motivo));
        if (changed != 1)
        {
            await transaction.RollbackAsync();
            return RedirectToAction(nameof(Detalle), new { id, error = "La venta ya fue anulada por otro usuario." });
        }

        var after = new
        {
            sale.Id,
            Estado = EstadoVenta.Anulada.ToString(),
            FechaAnulacion = fechaAnulacion,
            AnuladoPorUsuarioId = admin.Id,
            MotivoAnulacion = motivo
        };
        await auditoria.RegistrarAsync("AnuloVenta", nameof(Venta), id, Snapshot(sale), after);
        await transaction.CommitAsync();
        TempData["Success"] = "La venta fue anulada. El estado del vehículo no se modificó automáticamente.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    private IQueryable<Venta> SaleForCurrentUserQuery()
    {
        var query = db.Ventas.AsNoTracking();
        if (User.IsInRole("Vendedor"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userId, out var id)) query = query.Where(x => x.VendedorId == id);
            else query = query.Where(x => false);
        }
        return query;
    }

    private async Task<MisVentasViewModel> BuildListModelAsync(
        IQueryable<Venta> query, int page, DateTime? desde, DateTime? hasta, EstadoVenta? estado, string? buscar)
    {
        var total = await query.CountAsync();
        page = ClampPage(page, total);
        var items = await ProjectListQuery(query).OrderByDescending(x => x.FechaVenta)
            .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
        return new MisVentasViewModel
        {
            Items = items,
            Page = page,
            PageSize = PageSize,
            TotalItems = total,
            Desde = desde,
            Hasta = hasta,
            Estado = estado,
            BuscarVehiculo = buscar
        };
    }

    private static IQueryable<VentaListaItemViewModel> ProjectListQuery(IQueryable<Venta> query) => query.Select(x => new VentaListaItemViewModel
    {
        Id = x.Id,
        FechaVenta = x.FechaVenta,
        Cliente = x.Cliente.Nombre + " " + x.Cliente.Apellido,
        ClienteEmail = x.Cliente.Email,
        Vehiculo = x.Vehiculo.Anio + " " + x.Vehiculo.Marca + " " + x.Vehiculo.Modelo,
        VehiculoIdentificacion = x.Vehiculo.Patente ?? "Sin patente",
        Vendedor = x.Vendedor.Nombre,
        PrecioFinal = x.PrecioFinal,
        PrecioBase = x.PrecioBase,
        PorcentajeComision = x.PorcentajeComisionAplicado,
        Comision = x.ComisionCalculada,
        Estado = x.Estado
    });

    private async Task<VentaRegistrarViewModel> BuildRegistrarModelAsync(VentaRegistrarViewModel model)
    {
        var client = model.ClienteId.HasValue
            ? await db.Clientes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == model.ClienteId.Value)
            : null;
        var vehicle = model.VehiculoId.HasValue
            ? await db.Vehiculos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == model.VehiculoId.Value)
            : null;
        return new VentaRegistrarViewModel
        {
            ClienteId = model.ClienteId,
            VehiculoId = model.VehiculoId,
            FechaVenta = model.FechaVenta == default ? DateTime.Today : model.FechaVenta,
            MetodoPago = model.MetodoPago,
            CantidadCuotas = model.CantidadCuotas,
            Observaciones = model.Observaciones,
            ClienteSeleccionado = client,
            VehiculoSeleccionado = vehicle
        };
    }

    private async Task<ConfiguracionCalculoVenta> LoadConfigurationAsync() => new(
        await db.ComisionesPorTipoVehiculo.AsNoTracking().ToDictionaryAsync(x => x.Tipo, x => x.PorcentajeBase),
        await db.ComisionesPorAntiguedad.AsNoTracking().ToListAsync(),
        await db.RecargosPorCuotas.AsNoTracking().ToListAsync());

    private static IQueryable<Venta> ApplyDateFilter(IQueryable<Venta> query, DateTime? desde, DateTime? hasta)
    {
        if (desde.HasValue) query = query.Where(x => x.FechaVenta >= desde.Value.Date);
        if (hasta.HasValue) query = query.Where(x => x.FechaVenta < hasta.Value.Date.AddDays(1));
        return query;
    }

    private static int ClampPage(int page, int total) => Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)PageSize)));
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static object Snapshot(Venta x) => new
    {
        x.Id, x.VehiculoId, x.ClienteId, x.VendedorId, x.FechaVenta, MetodoPago = x.MetodoPago.ToString(),
        x.CantidadCuotas, x.PrecioBase, x.PrecioFinal, x.PorcentajeComisionAplicado, x.ComisionCalculada,
        Estado = x.Estado.ToString(), x.Observaciones
    };
}
