using ConcesionariaApp.Data;
using ConcesionariaApp.Models;
using ConcesionariaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConcesionariaApp.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Vendedores")]
public class VendedoresController(
    ApplicationDbContext db,
    UserManager<Usuario> userManager,
    AuditoriaService auditoria) : Controller
{
    private const int PageSize = 8;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? buscar, bool? activo, int page = 1)
    {
        var query = db.Usuarios.AsNoTracking().Where(x => x.Rol == Rol.Vendedor);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim();
            query = query.Where(x => x.Nombre.Contains(term) || (x.Email != null && x.Email.Contains(term)));
        }
        if (activo.HasValue) query = query.Where(x => x.Activo == activo.Value);

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);
        var sellers = await query.OrderBy(x => x.Nombre).Skip((page - 1) * PageSize).Take(PageSize)
            .Select(x => new VendedorListItem { Usuario = x, CantidadVentas = db.Ventas.Count(v => v.VendedorId == x.Id) })
            .ToListAsync();
        return View(new PagedResult<VendedorListItem> { Items = sellers, Page = page, PageSize = PageSize, TotalItems = total });
    }

    [HttpGet("Nuevo")]
    public IActionResult Nuevo() => View("Form", new VendedorFormViewModel { FechaAlta = DateTime.Today });

    [HttpPost("Nuevo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Nuevo(VendedorFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);
        if (await userManager.FindByEmailAsync(model.Email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con ese email.");
            return View("Form", model);
        }

        var seller = new Usuario
        {
            UserName = model.Email.Trim(), Email = model.Email.Trim(), Nombre = model.Nombre.Trim(),
            Telefono = model.Telefono.Trim(), FechaAlta = model.FechaAlta, Rol = Rol.Vendedor, Activo = true
        };
        var result = await userManager.CreateAsync(seller, model.Password);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View("Form", model);
        }
        var roleResult = await userManager.AddToRoleAsync(seller, Rol.Vendedor.ToString());
        if (!roleResult.Succeeded)
        {
            AddIdentityErrors(roleResult);
            return View("Form", model);
        }
        await auditoria.RegistrarAsync("CargoVendedor", nameof(Usuario), seller.Id, despues: Snapshot(seller));
        TempData["Success"] = "Vendedor creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Editar/{id:int}")]
    public async Task<IActionResult> Editar(int id)
    {
        var seller = await FindSeller(id);
        if (seller is null) return NotFound();
        return View(new VendedorEditViewModel { Id = seller.Id, Nombre = seller.Nombre, Email = seller.Email!, Telefono = seller.Telefono, FechaAlta = seller.FechaAlta });
    }

    [HttpPost("Editar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, VendedorEditViewModel model)
    {
        var seller = await FindSeller(id);
        if (seller is null) return NotFound();
        if (!ModelState.IsValid) return View(model);

        var duplicate = await db.Usuarios.AnyAsync(x => x.Id != id && x.Email == model.Email);
        if (duplicate)
        {
            ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con ese email.");
            return View(model);
        }
        var before = Snapshot(seller);
        var emailResult = await userManager.SetEmailAsync(seller, model.Email.Trim());
        var usernameResult = emailResult.Succeeded ? await userManager.SetUserNameAsync(seller, model.Email.Trim()) : emailResult;
        if (!usernameResult.Succeeded)
        {
            AddIdentityErrors(usernameResult);
            return View(model);
        }
        seller.Nombre = model.Nombre.Trim(); seller.Telefono = model.Telefono.Trim();
        var update = await userManager.UpdateAsync(seller);
        if (!update.Succeeded)
        {
            AddIdentityErrors(update);
            return View(model);
        }
        await auditoria.RegistrarAsync("EditoVendedor", nameof(Usuario), id, before, Snapshot(seller));
        TempData["Success"] = "Vendedor actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("RestablecerPassword/{id:int}")]
    public async Task<IActionResult> RestablecerPassword(int id)
    {
        var seller = await FindSeller(id);
        if (seller is null) return NotFound();
        return View(new RestablecerPasswordViewModel { Id = seller.Id, Nombre = seller.Nombre });
    }

    [HttpPost("RestablecerPassword/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestablecerPassword(int id, RestablecerPasswordViewModel model)
    {
        var seller = await FindSeller(id);
        if (seller is null) return NotFound();
        model.Id = id; model.Nombre = seller.Nombre;
        if (!ModelState.IsValid) return View(model);
        var token = await userManager.GeneratePasswordResetTokenAsync(seller);
        var result = await userManager.ResetPasswordAsync(seller, token, model.Password);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }
        await auditoria.RegistrarAsync("RestablecioPassword", nameof(Usuario), id, despues: new { seller.Id, seller.Email });
        TempData["Success"] = "Contraseña restablecida correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Desactivar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id)
    {
        var seller = await FindSeller(id);
        if (seller is null) return NotFound();
        var before = Snapshot(seller);
        seller.Activo = false;
        var result = await userManager.UpdateAsync(seller);
        if (!result.Succeeded) { AddIdentityErrors(result); return RedirectToAction(nameof(Index)); }
        await auditoria.RegistrarAsync("DesactivoVendedor", nameof(Usuario), id, before, Snapshot(seller));
        TempData["Success"] = "Vendedor desactivado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Reactivar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id)
    {
        var seller = await FindSeller(id);
        if (seller is null) return NotFound();
        var before = Snapshot(seller);
        seller.Activo = true;
        var result = await userManager.UpdateAsync(seller);
        if (!result.Succeeded) { AddIdentityErrors(result); return RedirectToAction(nameof(Index)); }
        await auditoria.RegistrarAsync("ReactivoVendedor", nameof(Usuario), id, before, Snapshot(seller));
        TempData["Success"] = "Vendedor reactivado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<Usuario?> FindSeller(int id) => await db.Usuarios.SingleOrDefaultAsync(x => x.Id == id && x.Rol == Rol.Vendedor);
    private void AddIdentityErrors(IdentityResult result) { foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); }
    private static object Snapshot(Usuario x) => new { x.Id, x.Nombre, x.Email, x.Telefono, x.FechaAlta, x.Activo, Rol = x.Rol.ToString() };
}
