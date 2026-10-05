using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;

namespace FlexPos.Domain.Entities;

public sealed class ReglaComision : EntidadAuditable
{
    private ReglaComision()
    {
    }

    private ReglaComision(Guid empleadoId, Guid servicioId, TipoTarifaComision tipo, decimal valor)
    {
        EmpleadoId = empleadoId;
        ServicioId = servicioId;
        Tipo = tipo;
        Valor = valor;
    }

    public Guid EmpleadoId { get; private set; }
    public Guid ServicioId { get; private set; }
    public TipoTarifaComision Tipo { get; private set; }
    public decimal Valor { get; private set; }
    public bool Activa { get; private set; } = true;

    public Resultado<decimal> CalcularImporte(DetalleVenta linea)
    {
        if (!Activa || linea.Tipo != TipoLineaVenta.Servicio || linea.ServicioId != ServicioId ||
            linea.EmpleadoId != EmpleadoId)
        {
            return Resultado<decimal>.Fallo(new ErrorDominio(
                "comision.linea_no_corresponde", "La regla de comisión no corresponde a la línea de servicio."));
        }

        try
        {
            var importe = Tipo == TipoTarifaComision.ValorFijo
                ? Valor * linea.Cantidad
                : linea.BaseNetaAntesImpuestos * Valor / 100m;
            importe = decimal.Round(importe, 2, MidpointRounding.AwayFromZero);
            return importe > 9_999_999_999_999_999.99m
                ? Resultado<decimal>.Fallo(new ErrorDominio(
                    "comision.importe_invalido", "El importe de comisión excede el rango permitido."))
                : Resultado<decimal>.Exito(importe);
        }
        catch (OverflowException)
        {
            return Resultado<decimal>.Fallo(new ErrorDominio(
                "comision.importe_invalido", "El importe de comisión excede el rango permitido."));
        }
    }

    public static Resultado<ReglaComision> Crear(
        Guid empleadoId,
        Guid servicioId,
        TipoTarifaComision tipo,
        decimal valor)
    {
        var error = Validar(empleadoId, servicioId, tipo, valor);
        return error is null
            ? Resultado<ReglaComision>.Exito(new ReglaComision(empleadoId, servicioId, tipo, valor))
            : Resultado<ReglaComision>.Fallo(error);
    }

    public Resultado<bool> Actualizar(TipoTarifaComision tipo, decimal valor)
    {
        var error = Validar(EmpleadoId, ServicioId, tipo, valor);
        if (error is not null)
        {
            return Resultado<bool>.Fallo(error);
        }

        Tipo = tipo;
        Valor = valor;
        return Resultado<bool>.Exito(true);
    }

    public void EstablecerEstado(bool activa) => Activa = activa;

    private static ErrorDominio? Validar(Guid empleadoId, Guid servicioId, TipoTarifaComision tipo, decimal valor)
    {
        if (empleadoId == Guid.Empty || servicioId == Guid.Empty || !Enum.IsDefined(tipo) ||
            valor <= 0m || decimal.Round(valor, 2) != valor ||
            tipo == TipoTarifaComision.Porcentaje && valor > 100m ||
            valor > 9_999_999_999_999_999.99m)
        {
            return new ErrorDominio(
                "comision.regla_invalida",
                "La regla requiere empleado, servicio y una tarifa positiva; el porcentaje no puede superar 100.");
        }

        return null;
    }
}
