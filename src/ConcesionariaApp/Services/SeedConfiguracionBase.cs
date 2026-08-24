using ConcesionariaApp.Data;
using ConcesionariaApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConcesionariaApp.Services;

public static class SeedConfiguracionBase
{
    public const string AdminEmail = "admin@concesionaria.local";

    public static async Task SeedAsync(
        ApplicationDbContext db,
        UserManager<Usuario> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        bool seedAdmin,
        string? adminPassword)
    {
        foreach (var role in Enum.GetNames<Rol>())
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<int>(role));
        }

        if (seedAdmin)
        {
            var admin = await userManager.FindByEmailAsync(AdminEmail);
            if (admin is null)
            {
                if (string.IsNullOrWhiteSpace(adminPassword))
                    throw new InvalidOperationException("Seeding:AdminPassword debe configurarse mediante User Secrets o una variable de entorno en Development.");

                admin = new Usuario
                {
                    Nombre = "Administrador inicial",
                    Email = AdminEmail,
                    UserName = AdminEmail,
                    Telefono = "011-5555-0100",
                    Rol = Rol.Admin,
                    FechaAlta = DateTime.UtcNow,
                    Activo = true
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (!result.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            }
            else
            {
                admin.Nombre = "Administrador inicial";
                admin.Rol = Rol.Admin;
                admin.Activo = true;
                admin.UserName ??= AdminEmail;
                admin.NormalizedUserName ??= userManager.NormalizeName(AdminEmail);
                admin.NormalizedEmail ??= userManager.NormalizeEmail(AdminEmail);
                admin.SecurityStamp ??= Guid.NewGuid().ToString();
                admin.ConcurrencyStamp ??= Guid.NewGuid().ToString();
                var update = await userManager.UpdateAsync(admin);
                if (!update.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", update.Errors.Select(x => x.Description)));
            }
            if (!await userManager.IsInRoleAsync(admin, Rol.Admin.ToString()))
                await userManager.AddToRoleAsync(admin, Rol.Admin.ToString());
        }

        var comisiones = new Dictionary<TipoVehiculo, decimal>
        {
            [TipoVehiculo.Sedan] = 3m,
            [TipoVehiculo.CuatroPuertas] = 3.5m,
            [TipoVehiculo.CuatroPorCuatro] = 5m,
            [TipoVehiculo.Deportivo] = 4m
        };
        foreach (var item in comisiones)
        {
            if (!await db.ComisionesPorTipoVehiculo.AnyAsync(x => x.Tipo == item.Key))
                db.ComisionesPorTipoVehiculo.Add(new ComisionPorTipoVehiculo { Tipo = item.Key, PorcentajeBase = item.Value });
        }

        if (!await db.ComisionesPorAntiguedad.AnyAsync())
        {
            db.ComisionesPorAntiguedad.AddRange(
                new ComisionPorAntiguedad { MesesMin = 0, MesesMax = 5, PorcentajeAdicional = 0m },
                new ComisionPorAntiguedad { MesesMin = 6, MesesMax = 11, PorcentajeAdicional = .5m },
                new ComisionPorAntiguedad { MesesMin = 12, MesesMax = 35, PorcentajeAdicional = 1m },
                new ComisionPorAntiguedad { MesesMin = 36, MesesMax = null, PorcentajeAdicional = 1.5m });
        }
        else
        {
            var antiguedad = await db.ComisionesPorAntiguedad.OrderBy(x => x.MesesMin).ToListAsync();
            var configuracionAnterior = antiguedad.Count == 4
                && antiguedad[0].MesesMin == 0 && antiguedad[0].MesesMax == 6
                && antiguedad[0].PorcentajeAdicional == 0m
                && antiguedad[1].MesesMin == 6 && antiguedad[1].MesesMax == 12
                && antiguedad[1].PorcentajeAdicional == .5m
                && antiguedad[2].MesesMin == 12 && antiguedad[2].MesesMax == 36
                && antiguedad[2].PorcentajeAdicional == 1m
                && antiguedad[3].MesesMin == 36 && antiguedad[3].MesesMax is null
                && antiguedad[3].PorcentajeAdicional == 1.5m;

            if (configuracionAnterior)
            {
                antiguedad[0].MesesMax = 5;
                antiguedad[1].MesesMax = 11;
                antiguedad[2].MesesMax = 35;
            }
        }

        if (!await db.RecargosPorCuotas.AnyAsync())
        {
            db.RecargosPorCuotas.AddRange(
                new RecargoPorCuotas { CuotasMin = 1, CuotasMax = 1, PorcentajeRecargo = 0m },
                new RecargoPorCuotas { CuotasMin = 2, CuotasMax = 3, PorcentajeRecargo = 5m },
                new RecargoPorCuotas { CuotasMin = 4, CuotasMax = 6, PorcentajeRecargo = 10m },
                new RecargoPorCuotas { CuotasMin = 7, CuotasMax = 12, PorcentajeRecargo = 18m });
        }

        await db.SaveChangesAsync();
    }
}
