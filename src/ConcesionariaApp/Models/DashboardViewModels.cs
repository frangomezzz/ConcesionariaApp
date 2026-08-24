namespace ConcesionariaApp.Models;

public enum DashboardDatePreset
{
    EsteMes,
    UltimosTresMeses,
    UltimoAnio,
    Personalizado
}

public sealed record DashboardDateRange(DateTime Desde, DateTime Hasta)
{
    public DateTime HastaExclusivo => Hasta.Date.AddDays(1);
}

public sealed class DashboardPageViewModel
{
    public string Nombre { get; init; } = "usuario";
    public string Rol { get; init; } = "";
    public string DataUrl { get; init; } = "";
    public DashboardDatePreset Preset { get; init; }
    public DateTime Desde { get; init; }
    public DateTime Hasta { get; init; }
    public DashboardData Data { get; init; } = new();
}

public sealed class DashboardData
{
    public DashboardDateRange Range { get; init; } = new(DateTime.Today, DateTime.Today);
    public AdminDashboardCards? AdminCards { get; init; }
    public SellerDashboardCards? SellerCards { get; init; }
    public DashboardChart MonthlySales { get; init; } = new();
    public DashboardChart MonthlyRevenue { get; init; } = new();
    public DashboardChart SalesBySeller { get; init; } = new();
    public DashboardChart CommissionsBySeller { get; init; } = new();
    public DashboardChart SalesByVehicleType { get; init; } = new();
    public IReadOnlyList<DashboardSaleRow> LatestSales { get; init; } = [];
}

public sealed class ReportesPageViewModel
{
    public string Nombre { get; init; } = "usuario";
    public string DataUrl { get; init; } = "";
    public DashboardDatePreset Preset { get; init; }
    public DateTime Desde { get; init; }
    public DateTime Hasta { get; init; }
    public int? VendedorId { get; init; }
    public string? MarcaModelo { get; init; }
    public TipoVehiculo? Tipo { get; init; }
    public IReadOnlyList<ReportesSellerOption> Vendedores { get; init; } = [];
    public ReportesData Data { get; init; } = new();
}

public sealed class ReportesData
{
    public DashboardDateRange Range { get; init; } = new(DateTime.Today, DateTime.Today);
    public DashboardChart MonthlySales { get; init; } = new();
    public DashboardChart MonthlyRevenue { get; init; } = new();
    public DashboardChart CommissionsBySeller { get; init; } = new();
    public DashboardChart SellerRanking { get; init; } = new();
}

public sealed class ReportesSellerOption
{
    public int Id { get; init; }
    public string Nombre { get; init; } = "";
    public Rol Rol { get; init; }
    public bool Activo { get; init; }
}

public sealed class AdminDashboardCards
{
    public int Sales { get; init; }
    public decimal TotalFacturado { get; init; }
    public int VehiculosVendidos { get; init; }
    public decimal Comisiones { get; init; }
    public int VendedoresActivos { get; init; }
}

public sealed class SellerDashboardCards
{
    public int Sales { get; init; }
    public decimal TotalVendido { get; init; }
    public decimal Comisiones { get; init; }
    public int VehiculosVendidos { get; init; }
}

public sealed class DashboardChart
{
    public IReadOnlyList<string> Labels { get; init; } = [];
    public IReadOnlyList<DashboardDataset> Datasets { get; init; } = [];
}

public sealed class DashboardDataset
{
    public string Label { get; init; } = "";
    public IReadOnlyList<decimal> Data { get; init; } = [];
}

public sealed class DashboardSaleRow
{
    public int Id { get; init; }
    public DateTime Fecha { get; init; }
    public string Cliente { get; init; } = "";
    public string Vehiculo { get; init; } = "";
    public string Vendedor { get; init; } = "";
    public decimal PrecioFinal { get; init; }
    public decimal Comision { get; init; }
    public string Estado { get; init; } = "";
}
