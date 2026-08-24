using ConcesionariaApp.Models;
using ConcesionariaApp.Services;
using Xunit;

namespace ConcesionariaApp.Tests;

public sealed class CalculoVentaServiceTests
{
    private readonly ICalculoVentaService service = new CalculoVentaService();

    public static IEnumerable<object[]> ComisionCases()
    {
        foreach (var type in Enum.GetValues<TipoVehiculo>())
        {
            var basePercentage = type switch
            {
                TipoVehiculo.Sedan => 3m,
                TipoVehiculo.CuatroPuertas => 3.5m,
                TipoVehiculo.CuatroPorCuatro => 5m,
                TipoVehiculo.Deportivo => 4m,
                _ => throw new ArgumentOutOfRangeException()
            };

            yield return [type, 3, basePercentage, 0m];
            yield return [type, 8, basePercentage, .5m];
            yield return [type, 24, basePercentage, 1m];
            yield return [type, 48, basePercentage, 1.5m];
        }
    }

    [Theory]
    [MemberData(nameof(ComisionCases))]
    public void CalculaComisionParaCadaTipoYTramo(
        TipoVehiculo tipo,
        int antiguedadMeses,
        decimal porcentajeBase,
        decimal porcentajeAdicional)
    {
        var resultado = service.Calcular(
            Datos(tipo, antiguedadMeses, MetodoPago.Efectivo, 1, 100_000m),
            ConfiguracionCompleta());

        Assert.Equal(porcentajeBase + porcentajeAdicional, resultado.PorcentajeComisionAplicado);
        Assert.Equal(100_000m * (porcentajeBase + porcentajeAdicional) / 100m, resultado.ComisionCalculada);
        Assert.Equal(antiguedadMeses, resultado.AntiguedadMeses);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 5)]
    [InlineData(3, 5)]
    [InlineData(4, 10)]
    [InlineData(6, 10)]
    [InlineData(7, 18)]
    [InlineData(12, 18)]
    public void CalculaPrecioFinalParaCadaTramoDeCuotas(int cuotas, decimal recargo)
    {
        var resultado = service.Calcular(
            Datos(TipoVehiculo.Sedan, 3, MetodoPago.Tarjeta, cuotas, 100_000m),
            ConfiguracionCompleta());

        Assert.Equal(recargo, resultado.PorcentajeRecargoAplicado);
        Assert.Equal(100_000m * (1m + recargo / 100m), resultado.PrecioFinal);
    }

    [Fact]
    public void EfectivoAplicaUnaCuotaYRecargoCero()
    {
        var resultado = service.Calcular(
            Datos(TipoVehiculo.Sedan, 3, MetodoPago.Efectivo, 1, 100_000m),
            ConfiguracionCompleta());

        Assert.Equal(0m, resultado.PorcentajeRecargoAplicado);
        Assert.Equal(100_000m, resultado.PrecioFinal);
    }

    [Fact]
    public void LanzaErrorCuandoLaAntiguedadNoTieneTramo()
    {
        var configuracion = ConfiguracionCompleta() with
        {
            ComisionesPorAntiguedad =
            [new ComisionPorAntiguedad { MesesMin = 0, MesesMax = 5, PorcentajeAdicional = 0m }]
        };

        var exception = Assert.Throws<ReglaCalculoVentaException>(() => service.Calcular(
            Datos(TipoVehiculo.Sedan, 12, MetodoPago.Efectivo, 1, 100_000m), configuracion));

        Assert.Contains("antigüedad", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LanzaErrorCuandoLasCuotasNoTienenTramo()
    {
        var configuracion = ConfiguracionCompleta() with
        {
            RecargosPorCuotas =
            [new RecargoPorCuotas { CuotasMin = 1, CuotasMax = 6, PorcentajeRecargo = 0m }]
        };

        var exception = Assert.Throws<ReglaCalculoVentaException>(() => service.Calcular(
            Datos(TipoVehiculo.Sedan, 3, MetodoPago.Tarjeta, 7, 100_000m), configuracion));

        Assert.Contains("cuotas", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LanzaErrorCuandoElTipoNoTieneComision()
    {
        var configuracion = ConfiguracionCompleta() with
        {
            ComisionesPorTipo = new Dictionary<TipoVehiculo, decimal>()
        };

        var exception = Assert.Throws<ReglaCalculoVentaException>(() => service.Calcular(
            Datos(TipoVehiculo.Deportivo, 3, MetodoPago.Efectivo, 1, 100_000m), configuracion));

        Assert.Contains("tipo de vehículo", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CalculaCasoCombinadoDeVendedorConDosAniosYUnCuatroPorCuatro()
    {
        var resultado = service.Calcular(
            Datos(TipoVehiculo.CuatroPorCuatro, 24, MetodoPago.Tarjeta, 5, 1_000_000m),
            ConfiguracionCompleta());

        Assert.Equal(6m, resultado.PorcentajeComisionAplicado);
        Assert.Equal(60_000m, resultado.ComisionCalculada);
        Assert.Equal(10m, resultado.PorcentajeRecargoAplicado);
        Assert.Equal(1_100_000m, resultado.PrecioFinal);
    }

    private static DatosCalculoVenta Datos(TipoVehiculo tipo, int meses, MetodoPago metodo, int cuotas, decimal precio)
    {
        var alta = new DateTime(2020, 1, 15);
        return new DatosCalculoVenta(
            new Vehiculo { Tipo = tipo, PrecioBase = precio },
            new Usuario { FechaAlta = alta },
            alta.AddMonths(meses),
            metodo,
            cuotas);
    }

    private static ConfiguracionCalculoVenta ConfiguracionCompleta() => new(
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
}
