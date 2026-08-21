using System.ComponentModel.DataAnnotations;

namespace ConcesionariaApp.Models;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
}

public sealed class VehiculoListItem
{
    public Vehiculo Vehiculo { get; init; } = null!;
    public bool TieneVentas { get; init; }
}

public sealed class VehiculoFormViewModel
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Marca { get; set; } = "";
    [Required, StringLength(80)] public string Modelo { get; set; } = "";
    [Range(1900, 2100)] public int Anio { get; set; }
    [Required] public TipoVehiculo Tipo { get; set; }
    public bool EsUsado { get; set; }
    [StringLength(10)] public string? Patente { get; set; }
    [Required, StringLength(80)] public string Color { get; set; } = "";
    [Range(0, int.MaxValue)] public int Kilometraje { get; set; }
    [Range(0.01, 9999999999999999, ErrorMessage = "El precio debe ser mayor que cero.")] public decimal PrecioBase { get; set; }
    [DataType(DataType.Date), Required] public DateTime FechaIngreso { get; set; }
    public bool TieneVentas { get; set; }
}

public sealed class VehiculoEstadoViewModel
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = "";
    [Required] public EstadoVehiculo Estado { get; set; }
}

public sealed class VendedorListItem
{
    public Usuario Usuario { get; init; } = null!;
    public int CantidadVentas { get; init; }
}

public sealed class VendedorFormViewModel
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Nombre { get; set; } = "";
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
    [Required, StringLength(40)] public string Telefono { get; set; } = "";
    [DataType(DataType.Date), Required] public DateTime FechaAlta { get; set; }
    [DataType(DataType.Password), Required, MinLength(8)] public string Password { get; set; } = "";
    [DataType(DataType.Password), Compare(nameof(Password)), Required] public string ConfirmarPassword { get; set; } = "";
}

public sealed class VendedorEditViewModel
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Nombre { get; set; } = "";
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
    [Required, StringLength(40)] public string Telefono { get; set; } = "";
    [DataType(DataType.Date), Required] public DateTime FechaAlta { get; set; }
}

public sealed class RestablecerPasswordViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    [DataType(DataType.Password), Required, MinLength(8)] public string Password { get; set; } = "";
    [DataType(DataType.Password), Compare(nameof(Password)), Required] public string ConfirmarPassword { get; set; } = "";
}

public sealed class ClienteFormViewModel
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Nombre { get; set; } = "";
    [Required, StringLength(80)] public string Apellido { get; set; } = "";
    [Required, StringLength(20)] public string DNI { get; set; } = "";
    [Required, StringLength(40)] public string Telefono { get; set; } = "";
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
    [StringLength(200)] public string? Direccion { get; set; }
}

public sealed class ClienteHistorialViewModel
{
    public Cliente Cliente { get; init; } = null!;
    public IReadOnlyList<Venta> Ventas { get; init; } = [];
}
