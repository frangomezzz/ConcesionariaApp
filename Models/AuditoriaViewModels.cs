namespace ConcesionariaApp.Models;

public sealed class AuditoriaPageViewModel
{
    public DashboardDatePreset Preset { get; init; }
    public DateTime Desde { get; init; }
    public DateTime Hasta { get; init; }
    public int? UsuarioId { get; init; }
    public string? Accion { get; init; }
    public IReadOnlyList<AuditoriaUsuarioOption> Usuarios { get; init; } = [];
    public IReadOnlyList<string> Acciones { get; init; } = [];
    public PagedResult<AuditoriaListItem> Registros { get; init; } = new();
}

public sealed class AuditoriaUsuarioOption
{
    public int Id { get; init; }
    public string Nombre { get; init; } = "";
    public Rol Rol { get; init; }
    public bool Activo { get; init; }
}

public sealed class AuditoriaListItem
{
    public int Id { get; init; }
    public DateTime Fecha { get; init; }
    public string Usuario { get; init; } = "";
    public Rol Rol { get; init; }
    public string Accion { get; init; } = "";
    public string EntidadAfectada { get; init; } = "";
    public int EntidadId { get; init; }
    public string? DetalleJson { get; init; }
}
