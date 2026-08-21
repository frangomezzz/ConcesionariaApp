using ConcesionariaApp.Data;
using ConcesionariaApp.Models;
using ConcesionariaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConcesionariaApp.Controllers;

[Authorize(Roles = "Admin,Vendedor")]
[Route("Admin/Clientes")]
public class ClientesController(ApplicationDbContext db, AuditoriaService auditoria) : Controller
{
    private const int PageSize = 8;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? buscar, int page = 1)
    {
        var query = db.Clientes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim();
            query = query.Where(x => x.Nombre.Contains(term) || x.Apellido.Contains(term) || x.DNI.Contains(term));
        }
        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);
        var clients = await query.OrderBy(x => x.Apellido).ThenBy(x => x.Nombre)
            .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
        return View(new PagedResult<Cliente> { Items = clients, Page = page, PageSize = PageSize, TotalItems = total });
    }

    [HttpGet("Nuevo")]
    public IActionResult Nuevo() => View("Form", new ClienteFormViewModel());

    [HttpPost("Nuevo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Nuevo(ClienteFormViewModel model)
    {
        model.DNI = model.DNI.Trim();
        await ValidateDniAsync(model.DNI, null);
        if (!ModelState.IsValid) return View("Form", model);
        var client = new Cliente
        {
            Nombre = model.Nombre.Trim(), Apellido = model.Apellido.Trim(), DNI = model.DNI,
            Telefono = model.Telefono.Trim(), Email = model.Email.Trim(), Direccion = Clean(model.Direccion),
            FechaAlta = DateTime.Today
        };
        db.Clientes.Add(client);
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync("CargoCliente", nameof(Cliente), client.Id, despues: Snapshot(client));
        TempData["Success"] = "Cliente creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Editar/{id:int}")]
    public async Task<IActionResult> Editar(int id)
    {
        var client = await db.Clientes.FindAsync(id);
        if (client is null) return NotFound();
        return View("Form", ToForm(client));
    }

    [HttpPost("Editar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, ClienteFormViewModel model)
    {
        var client = await db.Clientes.FindAsync(id);
        if (client is null) return NotFound();
        model.DNI = model.DNI.Trim();
        await ValidateDniAsync(model.DNI, id);
        if (!ModelState.IsValid) return View("Form", model);
        var before = Snapshot(client);
        client.Nombre = model.Nombre.Trim(); client.Apellido = model.Apellido.Trim(); client.DNI = model.DNI;
        client.Telefono = model.Telefono.Trim(); client.Email = model.Email.Trim(); client.Direccion = Clean(model.Direccion);
        await db.SaveChangesAsync();
        await auditoria.RegistrarAsync("EditoCliente", nameof(Cliente), id, before, Snapshot(client));
        TempData["Success"] = "Cliente actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Historial/{id:int}")]
    public async Task<IActionResult> Historial(int id)
    {
        var client = await db.Clientes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (client is null) return NotFound();
        var sales = await db.Ventas.AsNoTracking().Include(x => x.Vehiculo).Include(x => x.Vendedor)
            .Where(x => x.ClienteId == id).OrderByDescending(x => x.FechaVenta).ToListAsync();
        return View(new ClienteHistorialViewModel { Cliente = client, Ventas = sales });
    }

    private async Task ValidateDniAsync(string dni, int? currentId)
    {
        if (await db.Clientes.AnyAsync(x => x.DNI == dni && (!currentId.HasValue || x.Id != currentId.Value)))
            ModelState.AddModelError(nameof(ClienteFormViewModel.DNI), "Ya existe un cliente con ese DNI.");
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ClienteFormViewModel ToForm(Cliente x) => new() { Id = x.Id, Nombre = x.Nombre, Apellido = x.Apellido, DNI = x.DNI, Telefono = x.Telefono, Email = x.Email, Direccion = x.Direccion };
    private static object Snapshot(Cliente x) => new { x.Id, x.Nombre, x.Apellido, x.DNI, x.Telefono, x.Email, x.Direccion, x.FechaAlta };
}
