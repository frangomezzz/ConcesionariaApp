using System.Globalization;
using ConcesionariaApp.Data;
using ConcesionariaApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace ConcesionariaApp.Services;

public sealed record VentaPreview(string Vendedor, string Vehiculo, string Cliente, MetodoPago MetodoPago,
    int Cuotas, decimal PrecioBase, decimal Recargo, decimal PrecioFinal, decimal Comision, decimal PorcentajeComision);

public sealed record DatosDePruebaPreview(
    IReadOnlyList<Usuario> Vendedores,
    IReadOnlyList<Vehiculo> Vehiculos,
    IReadOnlyList<Cliente> Clientes,
    IReadOnlyList<VentaPreview> Ventas);

public static class SeedDatosDePrueba
{
    private static readonly DateTime SeedToday = DateTime.Today;

    public static DatosDePruebaPreview BuildPreview(DateTime today)
    {
        var sellers = new List<Usuario>
        {
            Seller("Lucía", "Gómez", "lucia.gomez@concesionaria.local", today.AddMonths(-3)),
            Seller("Martín", "Rossi", "martin.rossi@concesionaria.local", today.AddMonths(-8)),
            Seller("Carla", "Sosa", "carla.sosa@concesionaria.local", today.AddYears(-2)),
            Seller("Diego", "Fernández", "diego.fernandez@concesionaria.local", today.AddYears(-4)),
            Seller("Sofía", "Pereyra", "sofia.pereyra@concesionaria.local", today.AddYears(-5))
        };
        var vehicles = new List<Vehiculo>
        {
            Vehicle("Toyota", "Corolla XLI", 2025, TipoVehiculo.Sedan, false, null, "Blanco", 0, 32900000m, EstadoVehiculo.Vendido, today.AddDays(-120)),
            Vehicle("Volkswagen", "Taos Comfortline", 2024, TipoVehiculo.CuatroPuertas, true, "AF 321 CD", "Gris", 18500, 38900000m, EstadoVehiculo.Vendido, today.AddDays(-100)),
            Vehicle("Ford", "Ranger XLT 4x4", 2023, TipoVehiculo.CuatroPorCuatro, true, "AG 456 EF", "Azul", 32000, 54800000m, EstadoVehiculo.Vendido, today.AddDays(-85)),
            Vehicle("Chevrolet", "Tracker Premier", 2025, TipoVehiculo.CuatroPuertas, false, null, "Rojo", 0, 41500000m, EstadoVehiculo.Vendido, today.AddDays(-70)),
            Vehicle("Fiat", "Cronos Precision", 2024, TipoVehiculo.Sedan, true, "AD 789 GH", "Gris oscuro", 27000, 24500000m, EstadoVehiculo.Reservado, today.AddDays(-55)),
            Vehicle("Toyota", "Hilux SRV", 2025, TipoVehiculo.CuatroPorCuatro, false, null, "Plata", 0, 62500000m, EstadoVehiculo.Reservado, today.AddDays(-45)),
            Vehicle("Peugeot", "208 GT", 2025, TipoVehiculo.Deportivo, false, null, "Negro", 0, 31800000m, EstadoVehiculo.Disponible, today.AddDays(-30)),
            Vehicle("Renault", "Duster Intens", 2023, TipoVehiculo.CuatroPorCuatro, true, "AF 654 JK", "Verde", 41000, 33600000m, EstadoVehiculo.Disponible, today.AddDays(-24)),
            Vehicle("Volkswagen", "Polo Highline", 2024, TipoVehiculo.Sedan, true, "AG 987 LM", "Blanco", 12000, 28900000m, EstadoVehiculo.Disponible, today.AddDays(-15)),
            Vehicle("BMW", "220i M Sport", 2022, TipoVehiculo.Deportivo, true, "AE 246 NP", "Azul oscuro", 36000, 73500000m, EstadoVehiculo.Disponible, today.AddDays(-8))
        };
        var clients = new List<Cliente>
        {
            Client("Valentina", "Méndez", "28.456.789", "11-6123-4567", "valentina.mendez@gmail.com", "Av. Cabildo 2450, CABA", today.AddDays(-110)),
            Client("Nicolás", "Acosta", "31.234.567", "11-6234-5678", "nicolas.acosta@gmail.com", "San Martín 820, San Isidro", today.AddDays(-95)),
            Client("Mariana", "López", "26.789.123", "11-6345-6789", "mariana.lopez@gmail.com", "Belgrano 1430, CABA", today.AddDays(-80)),
            Client("Federico", "Ruiz", "34.567.890", "11-6456-7890", "federico.ruiz@gmail.com", "Mitre 560, Avellaneda", today.AddDays(-60)),
            Client("Paula", "Navarro", "30.987.654", "11-6567-8901", "paula.navarro@gmail.com", "Italia 112, Tigre", today.AddDays(-35))
        };

        var salesData = new[]
        {
            (0, 0, 0, MetodoPago.Efectivo, 1, today.AddDays(-80)),
            (1, 2, 1, MetodoPago.Tarjeta, 5, today.AddDays(-55)),
            (2, 3, 4, MetodoPago.FinanciacionPropia, 12, today.AddDays(-20)),
            (3, 1, 2, MetodoPago.Tarjeta, 3, today.AddDays(-30))
        };
        var calculator = new CalculoVentaService();
        var configuration = DefaultConfiguration();
        var sales = salesData.Select(x =>
        {
            var seller = sellers[x.Item1];
            var vehicle = vehicles[x.Item2];
            var result = calculator.Calcular(new DatosCalculoVenta(vehicle, seller, x.Item6, x.Item4, x.Item5), configuration);
            return new VentaPreview(seller.Nombre, $"{vehicle.Marca} {vehicle.Modelo}", $"{clients[x.Item3].Nombre} {clients[x.Item3].Apellido}", x.Item4, x.Item5, result.PrecioBase, result.PorcentajeRecargoAplicado, result.PrecioFinal, result.ComisionCalculada, result.PorcentajeComisionAplicado);
        }).ToList();
        return new(sellers, vehicles, clients, sales);
    }

    public static void PrintPreview(TextWriter output, DateTime today)
    {
        var preview = BuildPreview(today);
        output.WriteLine($"PREVIEW DATOS DE PRUEBA - fecha de referencia: {today:yyyy-MM-dd}");
        output.WriteLine("VENDEDORES");
        foreach (var x in preview.Vendedores) output.WriteLine($"- {x.Nombre} {x.Email}, alta {x.FechaAlta:yyyy-MM-dd}");
        output.WriteLine("VEHICULOS");
        foreach (var x in preview.Vehiculos) output.WriteLine($"- {x.Marca} {x.Modelo}, {x.Tipo}, {(x.EsUsado ? "Usado" : "0km")}, ${x.PrecioBase:N2}, {x.Estado}");
        output.WriteLine("CLIENTES");
        foreach (var x in preview.Clientes) output.WriteLine($"- {x.Nombre} {x.Apellido}, DNI {x.DNI}, {x.Email}");
        output.WriteLine("VENTAS Y CALCULOS");
        foreach (var x in preview.Ventas) output.WriteLine($"- {x.Vendedor} / {x.Vehiculo} / {x.Cliente}: {x.MetodoPago}, {x.Cuotas} cuota(s), base ${x.PrecioBase:N2}, recargo {x.Recargo}%, final ${x.PrecioFinal:N2}, comisión {x.PorcentajeComision}% = ${x.Comision:N2}");
    }

    public static async Task SeedAsync(ApplicationDbContext db, UserManager<Usuario> userManager, string testVendorPassword)
    {
        var existingSellers = await db.Usuarios.Where(x => x.Rol == Rol.Vendedor).ToListAsync();
        if (existingSellers.Count >= 5)
        {
            await UpdateSeedSellerDatesAsync(db, existingSellers);
            foreach (var seller in existingSellers)
            {
                if (seller.PasswordHash == "SEED_ONLY")
                {
                    seller.PasswordHash = userManager.PasswordHasher.HashPassword(seller, testVendorPassword);
                    await userManager.UpdateAsync(seller);
                }
                if (!await userManager.IsInRoleAsync(seller, Rol.Vendedor.ToString()))
                    await userManager.AddToRoleAsync(seller, Rol.Vendedor.ToString());
            }
            return;
        }
        var preview = BuildPreview(SeedToday);
        foreach (var seller in preview.Vendedores)
        {
            seller.UserName = seller.Email;
            seller.NormalizedUserName = userManager.NormalizeName(seller.Email);
            seller.NormalizedEmail = userManager.NormalizeEmail(seller.Email);
            seller.SecurityStamp = Guid.NewGuid().ToString();
            seller.ConcurrencyStamp = Guid.NewGuid().ToString();
            seller.PasswordHash = userManager.PasswordHasher.HashPassword(seller, testVendorPassword);
        }
        db.Usuarios.AddRange(preview.Vendedores);
        db.Vehiculos.AddRange(preview.Vehiculos);
        db.Clientes.AddRange(preview.Clientes);
        await db.SaveChangesAsync();

        foreach (var seller in preview.Vendedores)
            await userManager.AddToRoleAsync(seller, Rol.Vendedor.ToString());

        var rules = await db.ComisionesPorTipoVehiculo.ToDictionaryAsync(x => x.Tipo, x => x.PorcentajeBase);
        var ageRules = await db.ComisionesPorAntiguedad.ToListAsync();
        var surchargeRules = await db.RecargosPorCuotas.ToListAsync();
        var configuration = new ConfiguracionCalculoVenta(rules, ageRules, surchargeRules);
        var calculator = new CalculoVentaService();
        var salesData = new[]
        {
            (0, 0, 0, MetodoPago.Efectivo, 1, SeedToday.AddDays(-80)),
            (1, 2, 1, MetodoPago.Tarjeta, 5, SeedToday.AddDays(-55)),
            (2, 3, 4, MetodoPago.FinanciacionPropia, 12, SeedToday.AddDays(-20)),
            (3, 1, 2, MetodoPago.Tarjeta, 3, SeedToday.AddDays(-30))
        };
        foreach (var x in salesData)
        {
            var seller = preview.Vendedores[x.Item1]; var vehicle = preview.Vehiculos[x.Item2]; var client = preview.Clientes[x.Item3];
            var result = calculator.Calcular(new DatosCalculoVenta(vehicle, seller, x.Item6, x.Item4, x.Item5), configuration);
            db.Ventas.Add(new Venta { VehiculoId = vehicle.Id, ClienteId = client.Id, VendedorId = seller.Id, FechaVenta = x.Item6, MetodoPago = x.Item4, CantidadCuotas = x.Item5, PrecioBase = result.PrecioBase, PrecioFinal = result.PrecioFinal, PorcentajeComisionAplicado = result.PorcentajeComisionAplicado, ComisionCalculada = result.ComisionCalculada, Estado = EstadoVenta.Activa });
        }
        await db.SaveChangesAsync();
    }

    private static Usuario Seller(string first, string last, string email, DateTime date) => new() { Nombre = $"{first} {last}", Email = email, UserName = email, PasswordHash = "SEED_ONLY", Telefono = "11-5555-0000", Rol = Rol.Vendedor, FechaAlta = date, Activo = true };
    private static Vehiculo Vehicle(string brand, string model, int year, TipoVehiculo type, bool used, string? plate, string color, int km, decimal price, EstadoVehiculo state, DateTime date) => new() { Marca = brand, Modelo = model, Anio = year, Tipo = type, EsUsado = used, Patente = plate, Color = color, Kilometraje = km, PrecioBase = price, Estado = state, FechaIngreso = date };
    private static Cliente Client(string first, string last, string dni, string phone, string email, string address, DateTime date) => new() { Nombre = first, Apellido = last, DNI = dni, Telefono = phone, Email = email, Direccion = address, FechaAlta = date };
    private static ConfiguracionCalculoVenta DefaultConfiguration() => new(
        new Dictionary<TipoVehiculo, decimal>
        {
            [TipoVehiculo.Sedan] = 3m,
            [TipoVehiculo.CuatroPuertas] = 3.5m,
            [TipoVehiculo.CuatroPorCuatro] = 5m,
            [TipoVehiculo.Deportivo] = 4m
        },
        [
            new ComisionPorAntiguedad { MesesMin = 0, MesesMax = 5, PorcentajeAdicional = 0m },
            new ComisionPorAntiguedad { MesesMin = 6, MesesMax = 11, PorcentajeAdicional = .5m },
            new ComisionPorAntiguedad { MesesMin = 12, MesesMax = 35, PorcentajeAdicional = 1m },
            new ComisionPorAntiguedad { MesesMin = 36, MesesMax = null, PorcentajeAdicional = 1.5m }
        ],
        [
            new RecargoPorCuotas { CuotasMin = 1, CuotasMax = 1, PorcentajeRecargo = 0m },
            new RecargoPorCuotas { CuotasMin = 2, CuotasMax = 3, PorcentajeRecargo = 5m },
            new RecargoPorCuotas { CuotasMin = 4, CuotasMax = 6, PorcentajeRecargo = 10m },
            new RecargoPorCuotas { CuotasMin = 7, CuotasMax = 12, PorcentajeRecargo = 18m }
        ]);

    private static async Task UpdateSeedSellerDatesAsync(ApplicationDbContext db, IEnumerable<Usuario> sellers)
    {
        var dates = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase)
        {
            ["lucia.gomez@concesionaria.local"] = SeedToday.AddMonths(-3),
            ["martin.rossi@concesionaria.local"] = SeedToday.AddMonths(-8),
            ["carla.sosa@concesionaria.local"] = SeedToday.AddYears(-2),
            ["diego.fernandez@concesionaria.local"] = SeedToday.AddYears(-4),
            ["sofia.pereyra@concesionaria.local"] = SeedToday.AddYears(-5)
        };
        var changed = false;
        foreach (var seller in sellers)
        {
            if (seller.Email is not null && dates.TryGetValue(seller.Email, out var date) && seller.FechaAlta != date)
            {
                seller.FechaAlta = date;
                changed = true;
            }
        }
        if (changed)
            await db.SaveChangesAsync();
    }
}
