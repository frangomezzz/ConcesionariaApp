using ConcesionariaApp.Services;
using Xunit;

namespace ConcesionariaApp.Tests;

public sealed class DashboardAggregationServiceTests
{
    private readonly DashboardAggregationService service = new(null!);

    [Fact]
    public void ResuelveEsteMesDesdeElPrimerDiaHastaHoy()
    {
        var today = DateTime.Today;
        var range = service.ResolveRange("este-mes", null, null);

        Assert.Equal(new DateTime(today.Year, today.Month, 1), range.Desde);
        Assert.Equal(today, range.Hasta);
        Assert.Equal(today.AddDays(1), range.HastaExclusivo);
    }

    [Fact]
    public void ResuelveUltimosTresMesesIncluyendoElMesActual()
    {
        var today = DateTime.Today;
        var range = service.ResolveRange("ultimos-3-meses", null, null);

        Assert.Equal(new DateTime(today.AddMonths(-2).Year, today.AddMonths(-2).Month, 1), range.Desde);
        Assert.Equal(today, range.Hasta);
    }

    [Fact]
    public void RechazaRangoPersonalizadoIncompleto()
    {
        var exception = Assert.Throws<ArgumentException>(() => service.ResolveRange("personalizado", DateTime.Today, null));

        Assert.Contains("fechas", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RechazaRangoPersonalizadoInvertido()
    {
        var exception = Assert.Throws<ArgumentException>(() => service.ResolveRange(
            "personalizado", DateTime.Today, DateTime.Today.AddDays(-1)));

        Assert.Contains("posterior", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RechazaRangoPersonalizadoDemasiadoAmplio()
    {
        var exception = Assert.Throws<ArgumentException>(() => service.ResolveRange(
            "personalizado", DateTime.Today.AddYears(-11), DateTime.Today));

        Assert.Contains("10 años", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
