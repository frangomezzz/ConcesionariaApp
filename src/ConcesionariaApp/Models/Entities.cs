using Microsoft.AspNetCore.Identity;

namespace ConcesionariaApp.Models;

public enum Rol { Admin, Vendedor }
public enum TipoVehiculo { Sedan, CuatroPuertas, CuatroPorCuatro, Deportivo }
public enum EstadoVehiculo { Disponible, Reservado, Vendido }
public enum MetodoPago { Efectivo, Tarjeta, FinanciacionPropia }
public enum EstadoVenta { Activa, Anulada }

public class Usuario : IdentityUser<int>
{
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
    public Rol Rol { get; set; }
    public DateTime FechaAlta { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<Venta> Ventas { get; set; } = [];
    public ICollection<RegistroAuditoria> RegistrosAuditoria { get; set; } = [];
}

public class Vehiculo
{
    public int Id { get; set; }
    public string Marca { get; set; } = "";
    public string Modelo { get; set; } = "";
    public int Anio { get; set; }
    public TipoVehiculo Tipo { get; set; }
    public bool EsUsado { get; set; }
    public string? Patente { get; set; }
    public string Color { get; set; } = "";
    public int Kilometraje { get; set; }
    public decimal PrecioBase { get; set; }
    public EstadoVehiculo Estado { get; set; }
    public DateTime FechaIngreso { get; set; }
    public bool Activo { get; set; } = true;
}

public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string DNI { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Direccion { get; set; }
    public DateTime FechaAlta { get; set; }
    public ICollection<Venta> Ventas { get; set; } = [];
}

public class Venta
{
    public int Id { get; set; }
    public int VehiculoId { get; set; }
    public Vehiculo Vehiculo { get; set; } = null!;
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public int VendedorId { get; set; }
    public Usuario Vendedor { get; set; } = null!;
    public DateTime FechaVenta { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public int CantidadCuotas { get; set; }
    public decimal PrecioBase { get; set; }
    public decimal PrecioFinal { get; set; }
    public decimal PorcentajeComisionAplicado { get; set; }
    public decimal ComisionCalculada { get; set; }
    public EstadoVenta Estado { get; set; } = EstadoVenta.Activa;
    public string? Observaciones { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public int? AnuladoPorUsuarioId { get; set; }
    public Usuario? AnuladoPorUsuario { get; set; }
}

public class ComisionPorTipoVehiculo
{
    public TipoVehiculo Tipo { get; set; }
    public decimal PorcentajeBase { get; set; }
}

public class ComisionPorAntiguedad
{
    public int Id { get; set; }
    public int MesesMin { get; set; }
    public int? MesesMax { get; set; }
    public decimal PorcentajeAdicional { get; set; }
}

public class RecargoPorCuotas
{
    public int Id { get; set; }
    public int CuotasMin { get; set; }
    public int? CuotasMax { get; set; }
    public decimal PorcentajeRecargo { get; set; }
}

public class RegistroAuditoria
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public string Accion { get; set; } = "";
    public string EntidadAfectada { get; set; } = "";
    public int EntidadId { get; set; }
    public DateTime Fecha { get; set; }
    public string? DetalleJson { get; set; }
}
