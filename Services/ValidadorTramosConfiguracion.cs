using ConcesionariaApp.Models;

namespace ConcesionariaApp.Services;

public static class ValidadorTramosConfiguracion
{
    public static string? ValidarAntiguedad(IEnumerable<ComisionPorAntiguedad> tramos)
    {
        var rangos = tramos.Select(x => (Min: x.MesesMin, Max: x.MesesMax)).ToList();
        return Validar(rangos, 0, "antigüedad");
    }

    public static string? ValidarCuotas(IEnumerable<RecargoPorCuotas> tramos)
    {
        var rangos = tramos.Select(x => (Min: x.CuotasMin, Max: x.CuotasMax)).ToList();
        return Validar(rangos, 1, "cuotas");
    }

    private static string? Validar(IReadOnlyCollection<(int Min, int? Max)> tramos, int minimoDominio, string nombre)
    {
        if (tramos.Count == 0)
            return $"Debe existir al menos un tramo de {nombre}.";

        if (tramos.Any(x => x.Min < minimoDominio))
            return $"Los tramos de {nombre} no pueden comenzar antes de {minimoDominio}.";

        if (tramos.Any(x => x.Max.HasValue && x.Max.Value < x.Min))
            return $"El máximo de un tramo de {nombre} no puede ser menor que su mínimo.";

        var ordenados = tramos.OrderBy(x => x.Min).ToList();
        if (ordenados[0].Min != minimoDominio)
            return $"Los tramos de {nombre} deben comenzar en {minimoDominio}; de lo contrario queda un hueco sin configurar.";

        for (var i = 0; i < ordenados.Count - 1; i++)
        {
            var actual = ordenados[i];
            var siguiente = ordenados[i + 1];
            if (!actual.Max.HasValue)
                return $"Un tramo de {nombre} sin máximo debe ser el último.";
            if (siguiente.Min != actual.Max.Value + 1)
                return $"Los tramos de {nombre} tienen un solapamiento o un hueco entre {actual.Max} y {siguiente.Min}.";
        }

        return null;
    }
}
