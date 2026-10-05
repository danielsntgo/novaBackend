namespace FlexPos.Application.DTOs.Reportes;

public sealed record ReporteDto(
    string Tipo,
    string Nombre,
    DateTimeOffset GeneradoUtc,
    DateTimeOffset? DesdeUtc,
    DateTimeOffset? HastaUtc,
    IReadOnlyList<string> Columnas,
    IReadOnlyList<FilaReporteDto> Filas,
    IReadOnlyList<TotalReporteDto> Totales,
    int TotalRegistros,
    int Limite,
    bool Truncado);

public sealed record FilaReporteDto(IReadOnlyDictionary<string, object?> Valores);

public sealed record TotalReporteDto(string Nombre, object Valor, string? CodigoMoneda = null);

public sealed record FiltrosReporte(
    DateTimeOffset? DesdeUtc = null,
    DateTimeOffset? HastaUtc = null,
    Guid? ClienteId = null,
    Guid? EmpleadoId = null,
    Guid? ArticuloId = null,
    Guid? ServicioId = null,
    Guid? ProveedorId = null,
    Guid? MetodoPagoId = null,
    string? Estado = null,
    int Limite = 5000);

public sealed record ArchivoReporte(byte[] Contenido, string Nombre, string TipoContenido);
