using ConcesionariaApp.Models;

namespace ConcesionariaApp.Services;

public interface ICalculoVentaService
{
    ResultadoCalculoVenta Calcular(DatosCalculoVenta datos, ConfiguracionCalculoVenta configuracion);
}

public sealed record DatosCalculoVenta(
    Vehiculo Vehiculo,
    Usuario Vendedor,
    DateTime FechaVenta,
    MetodoPago MetodoPago,
    int CantidadCuotas);

public sealed record ConfiguracionCalculoVenta(
    IReadOnlyDictionary<TipoVehiculo, decimal> ComisionesPorTipo,
    IReadOnlyList<ComisionPorAntiguedad> ComisionesPorAntiguedad,
    IReadOnlyList<RecargoPorCuotas> RecargosPorCuotas);

public sealed record ResultadoCalculoVenta(
    decimal PrecioBase,
    decimal PrecioFinal,
    decimal PorcentajeRecargoAplicado,
    int AntiguedadMeses,
    decimal PorcentajeBase,
    decimal PorcentajeAdicional,
    decimal PorcentajeComisionAplicado,
    decimal ComisionCalculada,
    MetodoPago MetodoPago,
    int CantidadCuotas);

public sealed class ReglaCalculoVentaException(string message) : InvalidOperationException(message);

public sealed class CalculoVentaService : ICalculoVentaService
{
    public ResultadoCalculoVenta Calcular(DatosCalculoVenta datos, ConfiguracionCalculoVenta configuracion)
    {
        if (datos.Vehiculo.PrecioBase < 0)
            throw new ReglaCalculoVentaException("El precio base del vehículo no puede ser negativo.");
        if (datos.FechaVenta < datos.Vendedor.FechaAlta)
            throw new ReglaCalculoVentaException("La fecha de venta no puede ser anterior a la fecha de alta del vendedor.");
        if (datos.CantidadCuotas < 1)
            throw new ReglaCalculoVentaException("La cantidad de cuotas debe ser mayor o igual a 1.");
        if (datos.MetodoPago == MetodoPago.Efectivo && datos.CantidadCuotas != 1)
            throw new ReglaCalculoVentaException("Una venta en efectivo debe tener exactamente 1 cuota.");

        if (!configuracion.ComisionesPorTipo.TryGetValue(datos.Vehiculo.Tipo, out var porcentajeBase))
            throw new ReglaCalculoVentaException($"No hay comisión configurada para el tipo de vehículo '{datos.Vehiculo.Tipo}'.");

        var antiguedadMeses = MesesCompletos(datos.Vendedor.FechaAlta, datos.FechaVenta);
        var antiguedadTramos = configuracion.ComisionesPorAntiguedad
            .Where(x => x.MesesMin <= antiguedadMeses && (!x.MesesMax.HasValue || antiguedadMeses <= x.MesesMax.Value))
            .ToList();
        if (antiguedadTramos.Count != 1)
            throw new ReglaCalculoVentaException($"La antigüedad de {antiguedadMeses} meses no cae en un único tramo de comisión configurado.");

        var porcentajeAdicional = antiguedadTramos[0].PorcentajeAdicional;
        var porcentajeComision = porcentajeBase + porcentajeAdicional;
        var comision = decimal.Round(datos.Vehiculo.PrecioBase * porcentajeComision / 100m, 2);

        var porcentajeRecargo = 0m;
        if (datos.MetodoPago != MetodoPago.Efectivo)
        {
            var recargoTramos = configuracion.RecargosPorCuotas
                .Where(x => x.CuotasMin <= datos.CantidadCuotas && (!x.CuotasMax.HasValue || datos.CantidadCuotas <= x.CuotasMax.Value))
                .ToList();
            if (recargoTramos.Count != 1)
                throw new ReglaCalculoVentaException($"La cantidad de {datos.CantidadCuotas} cuotas no cae en un único tramo de recargo configurado.");

            porcentajeRecargo = recargoTramos[0].PorcentajeRecargo;
        }

        var precioFinal = decimal.Round(datos.Vehiculo.PrecioBase * (1m + porcentajeRecargo / 100m), 2);
        return new ResultadoCalculoVenta(
            datos.Vehiculo.PrecioBase,
            precioFinal,
            porcentajeRecargo,
            antiguedadMeses,
            porcentajeBase,
            porcentajeAdicional,
            porcentajeComision,
            comision,
            datos.MetodoPago,
            datos.CantidadCuotas);
    }

    public static int MesesCompletos(DateTime fechaAlta, DateTime fechaVenta)
    {
        var meses = (fechaVenta.Year - fechaAlta.Year) * 12 + fechaVenta.Month - fechaAlta.Month;
        return Math.Max(0, meses - (fechaVenta.Day < fechaAlta.Day ? 1 : 0));
    }
}
