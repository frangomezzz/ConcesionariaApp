using System.ComponentModel.DataAnnotations;

namespace ConcesionariaApp.Models;

public sealed class VentaRegistrarViewModel
{
    [Required(ErrorMessage = "Seleccioná un cliente.")]
    public int? ClienteId { get; set; }

    [Required(ErrorMessage = "Seleccioná un vehículo disponible.")]
    public int? VehiculoId { get; set; }

    [DataType(DataType.Date), Required(ErrorMessage = "Ingresá la fecha de venta.")]
    public DateTime FechaVenta { get; set; } = DateTime.Today;

    [Required]
    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;

    [Range(1, 12, ErrorMessage = "La cantidad de cuotas debe estar entre 1 y 12.")]
    public int CantidadCuotas { get; set; } = 1;

    [StringLength(1000)]
    public string? Observaciones { get; set; }

    public Cliente? ClienteSeleccionado { get; init; }
    public Vehiculo? VehiculoSeleccionado { get; init; }
}

public sealed class PreviewVentaRequest
{
    public int VehiculoId { get; set; }
    public DateTime FechaVenta { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public int CantidadCuotas { get; set; }
}

public sealed class VentaListaItemViewModel
{
    public int Id { get; init; }
    public DateTime FechaVenta { get; init; }
    public string Cliente { get; init; } = "";
    public string ClienteEmail { get; init; } = "";
    public string Vehiculo { get; init; } = "";
    public string VehiculoIdentificacion { get; init; } = "";
    public string Vendedor { get; init; } = "";
    public decimal PrecioFinal { get; init; }
    public decimal PrecioBase { get; init; }
    public decimal PorcentajeComision { get; init; }
    public decimal Comision { get; init; }
    public EstadoVenta Estado { get; init; }
}

public sealed class MisVentasViewModel
{
    public IReadOnlyList<VentaListaItemViewModel> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
    public DateTime? Desde { get; init; }
    public DateTime? Hasta { get; init; }
    public int? VehiculoId { get; init; }
    public EstadoVenta? Estado { get; init; }
    public string? BuscarVehiculo { get; init; }
}

public sealed class AdminVentasViewModel
{
    public IReadOnlyList<VentaListaItemViewModel> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
    public DateTime? Desde { get; init; }
    public DateTime? Hasta { get; init; }
    public EstadoVenta? Estado { get; init; }
    public string? Buscar { get; init; }
}

public sealed class MisComisionesViewModel
{
    public IReadOnlyList<VentaListaItemViewModel> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
    public DateTime Desde { get; init; }
    public DateTime Hasta { get; init; }
    public decimal ComisionMes { get; init; }
    public decimal ComisionTotal { get; init; }
    public int VentasComisionables { get; init; }
}

public sealed class VentaDetalleViewModel
{
    public Venta Venta { get; init; } = null!;
    public string? Error { get; init; }
    public string MotivoAnulacion { get; set; } = "";
}

public sealed class AnularVentaRequest
{
    [Required(ErrorMessage = "El motivo de anulación es obligatorio.")]
    [StringLength(1000)]
    public string MotivoAnulacion { get; set; } = "";
}
