using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FlexPos.Infrastructure.Persistence;

internal static class BloqueoAgendaPostgreSql
{
    public static async Task TomarAsync(
        DatabaseFacade baseDatos,
        Guid empleadoId,
        CancellationToken cancellationToken)
    {
        var clave = empleadoId.ToString("N");
        await baseDatos.SqlQuery<int>($"""
            SELECT 1 AS "Value"
            FROM (SELECT pg_advisory_xact_lock(hashtextextended({clave}, 0))) AS bloqueo
            """).SingleAsync(cancellationToken);
    }
}
