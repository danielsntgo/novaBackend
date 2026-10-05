using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlexPos.Infrastructure.Persistence.Repositories;

public sealed class RepositorioConfiguracion(FlexPosDbContext contexto) : IRepositorioConfiguracion
{
    public Task<ConfiguracionNegocio?> ObtenerPrincipalAsync(CancellationToken cancellationToken) =>
        contexto.ConfiguracionesNegocio.SingleOrDefaultAsync(x => x.EsPrincipal, cancellationToken);

    public void Agregar(ConfiguracionNegocio configuracion) =>
        contexto.ConfiguracionesNegocio.Add(configuracion);

    public async Task<IReadOnlyList<ImpuestoConfigurado>> ListarImpuestosAsync(
        Guid configuracionId,
        CancellationToken cancellationToken) =>
        await contexto.ImpuestosConfigurados
            .AsNoTracking()
            .Where(x => x.ConfiguracionNegocioId == configuracionId)
            .OrderBy(x => x.NombreNormalizado)
            .ToArrayAsync(cancellationToken);

    public Task<ImpuestoConfigurado?> ObtenerImpuestoAsync(
        Guid configuracionId,
        Guid impuestoId,
        CancellationToken cancellationToken) =>
        contexto.ImpuestosConfigurados.SingleOrDefaultAsync(
            x => x.ConfiguracionNegocioId == configuracionId && x.Id == impuestoId,
            cancellationToken);

    public void Agregar(ImpuestoConfigurado impuesto) => contexto.ImpuestosConfigurados.Add(impuesto);

    public async Task<IReadOnlyList<MetodoPagoConfigurado>> ListarMetodosPagoAsync(
        Guid configuracionId,
        CancellationToken cancellationToken) =>
        await contexto.MetodosPagoConfigurados
            .AsNoTracking()
            .Where(x => x.ConfiguracionNegocioId == configuracionId)
            .OrderBy(x => x.NombreNormalizado)
            .ToArrayAsync(cancellationToken);

    public Task<MetodoPagoConfigurado?> ObtenerMetodoPagoAsync(
        Guid configuracionId,
        Guid metodoId,
        CancellationToken cancellationToken) =>
        contexto.MetodosPagoConfigurados.SingleOrDefaultAsync(
            x => x.ConfiguracionNegocioId == configuracionId && x.Id == metodoId,
            cancellationToken);

    public Task<bool> ExisteOtroMetodoEfectivoAsync(
        Guid configuracionId,
        Guid? excluirId,
        CancellationToken cancellationToken) =>
        contexto.MetodosPagoConfigurados.AnyAsync(x =>
            x.ConfiguracionNegocioId == configuracionId && x.EsEfectivo &&
            (!excluirId.HasValue || x.Id != excluirId.Value), cancellationToken);

    public void Agregar(MetodoPagoConfigurado metodo) => contexto.MetodosPagoConfigurados.Add(metodo);

    public async Task<IReadOnlyList<NumeracionDocumento>> ListarNumeracionesAsync(
        Guid configuracionId,
        CancellationToken cancellationToken) =>
        await contexto.NumeracionesDocumento
            .AsNoTracking()
            .Where(x => x.ConfiguracionNegocioId == configuracionId)
            .OrderBy(x => x.TipoDocumento)
            .ToArrayAsync(cancellationToken);

    public Task<NumeracionDocumento?> ObtenerNumeracionAsync(
        Guid configuracionId,
        TipoDocumentoVenta tipoDocumento,
        CancellationToken cancellationToken) =>
        contexto.NumeracionesDocumento.SingleOrDefaultAsync(
            x => x.ConfiguracionNegocioId == configuracionId && x.TipoDocumento == tipoDocumento,
            cancellationToken);

    public async Task<bool> ExisteCajaOVentaAsync(CancellationToken cancellationToken) =>
        await contexto.Cajas.AnyAsync(cancellationToken) || await contexto.Ventas.AnyAsync(cancellationToken);

    public void Agregar(NumeracionDocumento numeracion) => contexto.NumeracionesDocumento.Add(numeracion);

    public async Task<bool> GuardarCambiosAsync(CancellationToken cancellationToken)
    {
        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            contexto.ChangeTracker.Clear();
            return false;
        }
        catch (DbUpdateException excepcion) when
            (excepcion.GetBaseException() is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            contexto.ChangeTracker.Clear();
            return false;
        }
    }
}
