using System.ComponentModel.DataAnnotations;

namespace ConcesionariaApp.Models;

public sealed class ConfiguracionIndexViewModel
{
    public IReadOnlyList<ComisionTipoItemViewModel> ComisionesPorTipo { get; init; } = [];
    public IReadOnlyList<TramoAntiguedadItemViewModel> ComisionesPorAntiguedad { get; init; } = [];
    public IReadOnlyList<TramoCuotasItemViewModel> RecargosPorCuotas { get; init; } = [];
}

public sealed class ComisionTipoItemViewModel
{
    public TipoVehiculo Tipo { get; init; }
    public decimal PorcentajeBase { get; init; }
    public bool Configurada { get; init; }
}

public sealed class ComisionTipoFormViewModel
{
    [Range(0, 1000, ErrorMessage = "El porcentaje base debe estar entre 0 y 1000.")]
    public decimal PorcentajeBase { get; set; }
}

public sealed class TramoAntiguedadItemViewModel
{
    public int Id { get; init; }
    public int MesesMin { get; init; }
    public int? MesesMax { get; init; }
    public decimal PorcentajeAdicional { get; init; }
}

public sealed class TramoAntiguedadFormViewModel
{
    [Range(0, int.MaxValue, ErrorMessage = "El mínimo de meses no puede ser negativo.")]
    public int MesesMin { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El máximo de meses no puede ser negativo.")]
    public int? MesesMax { get; set; }

    [Range(0, 1000, ErrorMessage = "El porcentaje adicional debe estar entre 0 y 1000.")]
    public decimal PorcentajeAdicional { get; set; }
}

public sealed class TramoCuotasItemViewModel
{
    public int Id { get; init; }
    public int CuotasMin { get; init; }
    public int? CuotasMax { get; init; }
    public decimal PorcentajeRecargo { get; init; }
}

public sealed class TramoCuotasFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "El mínimo de cuotas debe ser mayor o igual a 1.")]
    public int CuotasMin { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El máximo de cuotas debe ser mayor o igual a 1.")]
    public int? CuotasMax { get; set; }

    [Range(0, 1000, ErrorMessage = "El porcentaje de recargo debe estar entre 0 y 1000.")]
    public decimal PorcentajeRecargo { get; set; }
}
