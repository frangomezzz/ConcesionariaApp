using ConcesionariaApp.Models;
using ConcesionariaApp.Services;
using Xunit;

namespace ConcesionariaApp.Tests;

public sealed class ValidadorTramosConfiguracionTests
{
    [Fact]
    public void AceptaTramosContinuosDesdeElInicioDelDominio()
    {
        var antiguedad = ValidadorTramosConfiguracion.ValidarAntiguedad([
            new ComisionPorAntiguedad { MesesMin = 0, MesesMax = 5 },
            new ComisionPorAntiguedad { MesesMin = 6, MesesMax = 11 },
            new ComisionPorAntiguedad { MesesMin = 12, MesesMax = 35 },
            new ComisionPorAntiguedad { MesesMin = 36 }
        ]);
        var cuotas = ValidadorTramosConfiguracion.ValidarCuotas([
            new RecargoPorCuotas { CuotasMin = 1, CuotasMax = 1 },
            new RecargoPorCuotas { CuotasMin = 2, CuotasMax = 3 },
            new RecargoPorCuotas { CuotasMin = 4, CuotasMax = 6 }
        ]);

        Assert.Null(antiguedad);
        Assert.Null(cuotas);
    }

    [Fact]
    public void RechazaUnHuecoEntreTramos()
    {
        var error = ValidadorTramosConfiguracion.ValidarAntiguedad([
            new ComisionPorAntiguedad { MesesMin = 0, MesesMax = 5 },
            new ComisionPorAntiguedad { MesesMin = 7, MesesMax = null }
        ]);

        Assert.Contains("hueco", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RechazaUnSolapamientoEntreTramos()
    {
        var error = ValidadorTramosConfiguracion.ValidarCuotas([
            new RecargoPorCuotas { CuotasMin = 1, CuotasMax = 3 },
            new RecargoPorCuotas { CuotasMin = 3, CuotasMax = null }
        ]);

        Assert.Contains("solapamiento", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RechazaEliminarElPrimerTramoPorqueDejaUnHueco()
    {
        var error = ValidadorTramosConfiguracion.ValidarCuotas([
            new RecargoPorCuotas { CuotasMin = 2, CuotasMax = 3 },
            new RecargoPorCuotas { CuotasMin = 4, CuotasMax = 6 }
        ]);

        Assert.Contains("comenzar en 1", error, StringComparison.OrdinalIgnoreCase);
    }
}
