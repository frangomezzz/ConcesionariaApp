using ConcesionariaApp.Data;
using ConcesionariaApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ConcesionariaApp.Services;

public sealed class DashboardAggregationService(ApplicationDbContext db)
{
    private const int LatestSalesLimit = 15;
    private static readonly TimeSpan MaxCustomRange = TimeSpan.FromDays(3663);

    public DashboardDateRange ResolveRange(string? preset, DateTime? desde, DateTime? hasta)
    {
        var today = DateTime.Today;
        var normalizedPreset = (preset ?? "este-mes").Trim().ToLowerInvariant();

        return normalizedPreset switch
        {
            "ultimos-3-meses" => new(new DateTime(today.AddMonths(-2).Year, today.AddMonths(-2).Month, 1), today),
            "ultimo-anio" => new(new DateTime(today.AddMonths(-11).Year, today.AddMonths(-11).Month, 1), today),
            "personalizado" => ResolveCustomRange(desde, hasta),
            _ => new(new DateTime(today.Year, today.Month, 1), today)
        };
    }

    public DashboardDatePreset ParsePreset(string? preset) => (preset ?? "este-mes").Trim().ToLowerInvariant() switch
    {
        "ultimos-3-meses" => DashboardDatePreset.UltimosTresMeses,
        "ultimo-anio" => DashboardDatePreset.UltimoAnio,
        "personalizado" => DashboardDatePreset.Personalizado,
        _ => DashboardDatePreset.EsteMes
    };

    public async Task<DashboardData> GetAdminAsync(DashboardDateRange range)
    {
        return await BuildAsync(range, null);
    }

    public async Task<DashboardData> GetSellerAsync(DashboardDateRange range, int vendedorId)
    {
        return await BuildAsync(range, vendedorId);
    }

    public async Task<ReportesData> GetReportesAsync(
        DashboardDateRange range,
        int? vendedorId = null,
        TipoVehiculo? tipo = null,
        string? marcaModelo = null)
    {
        var sales = await LoadActiveSalesAsync(range, vendedorId, tipo, marcaModelo);
        return new ReportesData
        {
            Range = range,
            MonthlySales = BuildMonthlySales(sales, range),
            MonthlyRevenue = BuildMonthlyRevenue(sales, range),
            CommissionsBySeller = BuildSellerCommissions(sales),
            SellerRanking = BuildSellerSales(sales)
        };
    }

    private async Task<DashboardData> BuildAsync(
        DashboardDateRange range,
        int? vendedorId)
    {
        var sales = db.Ventas.AsNoTracking()
            .Where(x => x.FechaVenta >= range.Desde.Date && x.FechaVenta < range.HastaExclusivo);

        if (vendedorId.HasValue)
            sales = sales.Where(x => x.VendedorId == vendedorId.Value);

        var activeSales = await LoadActiveSalesAsync(range, vendedorId, null, null);

        var latestSales = await sales
            .OrderByDescending(x => x.FechaVenta)
            .ThenByDescending(x => x.Id)
            .Take(LatestSalesLimit)
            .Select(x => new DashboardSaleRow
            {
                Id = x.Id,
                Fecha = x.FechaVenta,
                Cliente = x.Cliente.Nombre + " " + x.Cliente.Apellido,
                Vehiculo = x.Vehiculo.Anio + " " + x.Vehiculo.Marca + " " + x.Vehiculo.Modelo,
                Vendedor = x.Vendedor.Nombre,
                PrecioFinal = x.PrecioFinal,
                Comision = x.ComisionCalculada,
                Estado = x.Estado == EstadoVenta.Activa ? "Activa" : "Anulada"
            })
            .ToListAsync();

        var monthlySales = BuildMonthlySales(activeSales, range);
        var totalSales = activeSales.Count;
        var totalFacturado = activeSales.Sum(x => x.PrecioFinal);
        var totalComisiones = activeSales.Sum(x => x.Comision);

        if (vendedorId.HasValue)
        {
            return new DashboardData
            {
                Range = range,
                SellerCards = new SellerDashboardCards
                {
                    Sales = totalSales,
                    TotalVendido = totalFacturado,
                    Comisiones = totalComisiones,
                    VehiculosVendidos = totalSales
                },
                MonthlySales = monthlySales,
                LatestSales = latestSales
            };
        }

        var activeSellers = await db.Usuarios.AsNoTracking()
            .CountAsync(x => x.Rol == Rol.Vendedor && x.Activo);

        return new DashboardData
        {
            Range = range,
            AdminCards = new AdminDashboardCards
            {
                Sales = totalSales,
                TotalFacturado = totalFacturado,
                Comisiones = totalComisiones,
                VehiculosVendidos = totalSales,
                VendedoresActivos = activeSellers
            },
            MonthlySales = monthlySales,
            MonthlyRevenue = BuildMonthlyRevenue(activeSales, range),
            SalesBySeller = BuildSellerSales(activeSales),
            CommissionsBySeller = BuildSellerCommissions(activeSales),
            SalesByVehicleType = BuildVehicleTypes(activeSales),
            LatestSales = latestSales
        };
    }

    private async Task<List<ActiveSale>> LoadActiveSalesAsync(
        DashboardDateRange range,
        int? vendedorId,
        TipoVehiculo? tipo,
        string? marcaModelo)
    {
        var query = db.Ventas.AsNoTracking()
            .Where(x => x.Estado == EstadoVenta.Activa
                && x.FechaVenta >= range.Desde.Date
                && x.FechaVenta < range.HastaExclusivo);

        if (vendedorId.HasValue)
            query = query.Where(x => x.VendedorId == vendedorId.Value);
        if (tipo.HasValue)
            query = query.Where(x => x.Vehiculo.Tipo == tipo.Value);
        if (!string.IsNullOrWhiteSpace(marcaModelo))
        {
            var term = marcaModelo.Trim();
            query = query.Where(x => x.Vehiculo.Marca.Contains(term) || x.Vehiculo.Modelo.Contains(term));
        }

        return await query.Select(x => new ActiveSale
        {
            Fecha = x.FechaVenta,
            Vendedor = x.Vendedor.Nombre,
            Tipo = x.Vehiculo.Tipo,
            PrecioFinal = x.PrecioFinal,
            Comision = x.ComisionCalculada
        }).ToListAsync();
    }

    private static DashboardChart BuildMonthlySales(IReadOnlyList<ActiveSale> sales, DashboardDateRange range)
    {
        var firstMonth = new DateTime(range.Desde.Year, range.Desde.Month, 1);
        var lastMonth = new DateTime(range.Hasta.Year, range.Hasta.Month, 1);
        var values = sales.GroupBy(x => (x.Fecha.Year, x.Fecha.Month))
            .ToDictionary(x => x.Key, x => (decimal)x.Count());
        var labels = new List<string>();
        var data = new List<decimal>();

        for (var month = firstMonth; month <= lastMonth; month = month.AddMonths(1))
        {
            labels.Add(month.ToString("MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-AR")));
            data.Add(values.GetValueOrDefault((month.Year, month.Month)));
        }

        return Chart(labels, "Ventas activas", data);
    }

    private static DashboardChart BuildMonthlyRevenue(IReadOnlyList<ActiveSale> sales, DashboardDateRange range)
    {
        var firstMonth = new DateTime(range.Desde.Year, range.Desde.Month, 1);
        var lastMonth = new DateTime(range.Hasta.Year, range.Hasta.Month, 1);
        var values = sales.GroupBy(x => (x.Fecha.Year, x.Fecha.Month))
            .ToDictionary(x => x.Key, x => x.Sum(y => y.PrecioFinal));
        var labels = new List<string>();
        var data = new List<decimal>();

        for (var month = firstMonth; month <= lastMonth; month = month.AddMonths(1))
        {
            labels.Add(month.ToString("MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-AR")));
            data.Add(values.GetValueOrDefault((month.Year, month.Month)));
        }

        return Chart(labels, "Facturación", data);
    }

    private static DashboardChart BuildSellerSales(IReadOnlyList<ActiveSale> sales)
    {
        var grouped = sales.GroupBy(x => x.Vendedor)
            .OrderByDescending(x => x.Count())
            .ThenBy(x => x.Key)
            .Select(x => new { x.Key, Value = (decimal)x.Count() })
            .ToList();
        return Chart(grouped.Select(x => x.Key).ToList(), "Ventas activas", grouped.Select(x => x.Value).ToList());
    }

    private static DashboardChart BuildSellerCommissions(IReadOnlyList<ActiveSale> sales)
    {
        var grouped = sales.GroupBy(x => x.Vendedor)
            .OrderByDescending(x => x.Sum(y => y.Comision))
            .ThenBy(x => x.Key)
            .Select(x => new { x.Key, Value = x.Sum(y => y.Comision) })
            .ToList();
        return Chart(grouped.Select(x => x.Key).ToList(), "Comisiones snapshot", grouped.Select(x => x.Value).ToList());
    }

    private static DashboardChart BuildVehicleTypes(IReadOnlyList<ActiveSale> sales)
    {
        var grouped = sales.GroupBy(x => x.Tipo)
            .OrderByDescending(x => x.Count())
            .Select(x => new { Key = DisplayType(x.Key), Value = (decimal)x.Count() })
            .ToList();
        return Chart(grouped.Select(x => x.Key).ToList(), "Vehículos vendidos", grouped.Select(x => x.Value).ToList());
    }

    private static DashboardChart Chart(IReadOnlyList<string> labels, string label, IReadOnlyList<decimal> data) => new()
    {
        Labels = labels,
        Datasets = [new DashboardDataset { Label = label, Data = data }]
    };

    private static DashboardDateRange ResolveCustomRange(DateTime? desde, DateTime? hasta)
    {
        if (!desde.HasValue || !hasta.HasValue)
            throw new ArgumentException("Para un rango personalizado se requieren las fechas desde y hasta.");

        var start = desde.Value.Date;
        var end = hasta.Value.Date;
        if (start > end)
            throw new ArgumentException("La fecha desde no puede ser posterior a la fecha hasta.");
        if (end == DateTime.MaxValue.Date || end - start > MaxCustomRange)
            throw new ArgumentException("El rango personalizado no puede superar los 10 años.");

        return new(start, end);
    }

    private static string DisplayType(TipoVehiculo type) => type switch
    {
        TipoVehiculo.Sedan => "Sedán",
        TipoVehiculo.CuatroPuertas => "4 puertas",
        TipoVehiculo.CuatroPorCuatro => "4x4",
        TipoVehiculo.Deportivo => "Deportivo",
        _ => type.ToString()
    };

    private sealed class ActiveSale
    {
        public DateTime Fecha { get; init; }
        public string Vendedor { get; init; } = "";
        public TipoVehiculo Tipo { get; init; }
        public decimal PrecioFinal { get; init; }
        public decimal Comision { get; init; }
    }
}
