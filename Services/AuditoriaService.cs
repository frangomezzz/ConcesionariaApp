using System.Security.Claims;
using System.Text.Json;
using ConcesionariaApp.Data;
using ConcesionariaApp.Models;

namespace ConcesionariaApp.Services;

public sealed class AuditoriaService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
{
    public async Task RegistrarAsync(string accion, string entidad, int entidadId, object? antes = null, object? despues = null)
    {
        var userIdValue = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
            throw new InvalidOperationException("No se pudo identificar al usuario que ejecutó la acción.");

        var detalle = antes is null && despues is null
            ? null
            : JsonSerializer.Serialize(new { Antes = antes, Despues = despues });

        db.RegistrosAuditoria.Add(new RegistroAuditoria
        {
            UsuarioId = userId,
            Accion = accion,
            EntidadAfectada = entidad,
            EntidadId = entidadId,
            Fecha = DateTime.UtcNow,
            DetalleJson = detalle
        });
        await db.SaveChangesAsync();
    }
}
