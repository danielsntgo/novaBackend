using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class Caja : EntidadAuditable
{
    private Caja()
    {
    }

    private Caja(decimal efectivoApertura, DateTimeOffset fechaAperturaUtc, Guid usuarioAperturaId)
    {
        EfectivoApertura = efectivoApertura;
        FechaAperturaUtc = fechaAperturaUtc.ToUniversalTime();
        UsuarioAperturaId = usuarioAperturaId;
        Estado = EstadoCaja.Abierta;
    }

    public EstadoCaja Estado { get; private set; }
    public decimal EfectivoApertura { get; private set; }
    public DateTimeOffset FechaAperturaUtc { get; private set; }
    public Guid UsuarioAperturaId { get; private set; }
    public decimal? EfectivoEsperadoCierre { get; private set; }
    public decimal? EfectivoContadoCierre { get; private set; }
    public decimal? DiferenciaCierre { get; private set; }
    public DateTimeOffset? FechaCierreUtc { get; private set; }
    public Guid? UsuarioCierreId { get; private set; }

    public static Resultado<Caja> Abrir(decimal efectivoInicial, DateTimeOffset fechaUtc, Guid usuarioId)
    {
        if (efectivoInicial < 0m || decimal.Round(efectivoInicial, 2) != efectivoInicial ||
            fechaUtc == default || usuarioId == Guid.Empty)
        {
            return Resultado<Caja>.Fallo(new ErrorDominio(
                "caja.apertura_invalida", "El efectivo inicial, la fecha o el usuario de apertura no son válidos."));
        }

        return Resultado<Caja>.Exito(new Caja(efectivoInicial, fechaUtc, usuarioId));
    }

    public Resultado<bool> Cerrar(
        decimal efectivoContado,
        decimal efectivoEsperado,
        DateTimeOffset fechaUtc,
        Guid usuarioId)
    {
        if (Estado != EstadoCaja.Abierta || efectivoContado < 0m || efectivoEsperado < 0m ||
            decimal.Round(efectivoContado, 2) != efectivoContado ||
            decimal.Round(efectivoEsperado, 2) != efectivoEsperado ||
            fechaUtc == default || usuarioId == Guid.Empty)
        {
            return Resultado<bool>.Fallo(new ErrorDominio(
                "caja.cierre_invalido", "Los datos del cierre no son válidos o la caja ya está cerrada."));
        }

        Estado = EstadoCaja.Cerrada;
        EfectivoEsperadoCierre = efectivoEsperado;
        EfectivoContadoCierre = efectivoContado;
        DiferenciaCierre = efectivoContado - efectivoEsperado;
        FechaCierreUtc = fechaUtc.ToUniversalTime();
        UsuarioCierreId = usuarioId;
        return Resultado<bool>.Exito(true);
    }
}
