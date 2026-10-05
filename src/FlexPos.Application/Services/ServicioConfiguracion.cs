using FlexPos.Application.DTOs.Configuracion;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;

namespace FlexPos.Application.Services;

public sealed class ServicioConfiguracion(
    IRepositorioConfiguracion repositorio,
    IRepositorioServicios servicios,
    IRepositorioInventario inventario,
    IRepositorioCompras compras)
{
    public async Task<Resultado<ConfiguracionNegocioDto>> ObtenerAsync(CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerPrincipalAsync(cancellationToken);
        return configuracion is null
            ? Resultado<ConfiguracionNegocioDto>.Fallo(NoConfiguracion())
            : Resultado<ConfiguracionNegocioDto>.Exito(Convertir(configuracion));
    }

    public async Task<Resultado<ConfiguracionNegocioDto>> GuardarAsync(
        SolicitudConfiguracionNegocio solicitud,
        CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            var nueva = ConfiguracionNegocio.Crear(
                solicitud.NombreComercial,
                solicitud.RazonSocial,
                solicitud.IdentificacionFiscal,
                solicitud.Direccion,
                solicitud.Telefono,
                solicitud.Correo,
                solicitud.CodigoMoneda,
                solicitud.PermitirVentaSinStock,
                solicitud.ComisionProductosHabilitada,
                solicitud.PermitirSaldosPendientes,
                solicitud.FacturacionHabilitada,
                solicitud.ExigirEmpleadoVentaServicio);

            if (!nueva.EsExitoso)
            {
                return Resultado<ConfiguracionNegocioDto>.Fallo(nueva.Error!);
            }

            configuracion = nueva.Valor!;
            repositorio.Agregar(configuracion);
        }
        else
        {
            var monedaSolicitada = solicitud.CodigoMoneda?.Trim().ToUpperInvariant();
            if (EsCodigoMonedaValido(monedaSolicitada) &&
                !string.Equals(configuracion.CodigoMoneda, monedaSolicitada, StringComparison.Ordinal) &&
                (await servicios.ExisteAlgunoAsync(cancellationToken) ||
                 await inventario.ExisteAlgunoAsync(cancellationToken) ||
                 await compras.ExisteAlgunaAsync(cancellationToken) ||
                 await repositorio.ExisteCajaOVentaAsync(cancellationToken)))
            {
                return Resultado<ConfiguracionNegocioDto>.Fallo(new ErrorDominio(
                    "configuracion.moneda_conflicto",
                    "No se puede cambiar la moneda mientras existan servicios, articulos, compras, cajas o ventas registradas."));
            }

            var actualizacion = configuracion.Actualizar(
                solicitud.NombreComercial,
                solicitud.RazonSocial,
                solicitud.IdentificacionFiscal,
                solicitud.Direccion,
                solicitud.Telefono,
                solicitud.Correo,
                solicitud.CodigoMoneda,
                solicitud.PermitirVentaSinStock,
                solicitud.ComisionProductosHabilitada,
                solicitud.PermitirSaldosPendientes,
                solicitud.FacturacionHabilitada,
                solicitud.ExigirEmpleadoVentaServicio);

            if (!actualizacion.EsExitoso)
            {
                return Resultado<ConfiguracionNegocioDto>.Fallo(actualizacion.Error!);
            }
        }

        if (!await repositorio.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<ConfiguracionNegocioDto>.Fallo(Conflicto());
        }

        return Resultado<ConfiguracionNegocioDto>.Exito(Convertir(configuracion));
    }

    public async Task<Resultado<IReadOnlyList<ImpuestoDto>>> ListarImpuestosAsync(CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<IReadOnlyList<ImpuestoDto>>.Fallo(NoConfiguracion());
        }

        var elementos = await repositorio.ListarImpuestosAsync(configuracion.Id, cancellationToken);
        return Resultado<IReadOnlyList<ImpuestoDto>>.Exito(elementos.Select(Convertir).ToArray());
    }

    public async Task<Resultado<ImpuestoDto>> ObtenerImpuestoAsync(Guid id, CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerPrincipalAsync(cancellationToken);
        var impuesto = configuracion is null
            ? null
            : await repositorio.ObtenerImpuestoAsync(configuracion.Id, id, cancellationToken);
        return impuesto is null
            ? Resultado<ImpuestoDto>.Fallo(NoEncontrado("impuesto"))
            : Resultado<ImpuestoDto>.Exito(Convertir(impuesto));
    }

    public async Task<Resultado<ImpuestoDto>> CrearImpuestoAsync(
        SolicitudImpuesto solicitud,
        CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<ImpuestoDto>.Fallo(NoConfiguracion());
        }

        var nuevo = ImpuestoConfigurado.Crear(configuracion.Id, solicitud.Nombre, solicitud.Porcentaje);
        if (!nuevo.EsExitoso)
        {
            return Resultado<ImpuestoDto>.Fallo(nuevo.Error!);
        }

        repositorio.Agregar(nuevo.Valor!);
        return await GuardarYConvertirAsync(nuevo.Valor!, cancellationToken);
    }

    public async Task<Resultado<ImpuestoDto>> ActualizarImpuestoAsync(
        Guid id,
        SolicitudImpuesto solicitud,
        CancellationToken cancellationToken)
    {
        var impuesto = await ObtenerImpuestoEntidadAsync(id, cancellationToken);
        if (impuesto is null)
        {
            return Resultado<ImpuestoDto>.Fallo(NoEncontrado("impuesto"));
        }

        var resultado = impuesto.Actualizar(solicitud.Nombre, solicitud.Porcentaje);
        if (!resultado.EsExitoso)
        {
            return Resultado<ImpuestoDto>.Fallo(resultado.Error!);
        }

        return await GuardarYConvertirAsync(impuesto, cancellationToken);
    }

    public async Task<Resultado<ImpuestoDto>> EstablecerEstadoImpuestoAsync(
        Guid id,
        bool activo,
        CancellationToken cancellationToken)
    {
        var impuesto = await ObtenerImpuestoEntidadAsync(id, cancellationToken);
        if (impuesto is null)
        {
            return Resultado<ImpuestoDto>.Fallo(NoEncontrado("impuesto"));
        }

        impuesto.EstablecerEstado(activo);
        return await GuardarYConvertirAsync(impuesto, cancellationToken);
    }

    public async Task<Resultado<IReadOnlyList<MetodoPagoDto>>> ListarMetodosPagoAsync(CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<IReadOnlyList<MetodoPagoDto>>.Fallo(NoConfiguracion());
        }

        var elementos = await repositorio.ListarMetodosPagoAsync(configuracion.Id, cancellationToken);
        return Resultado<IReadOnlyList<MetodoPagoDto>>.Exito(elementos.Select(Convertir).ToArray());
    }

    public async Task<Resultado<MetodoPagoDto>> ObtenerMetodoPagoAsync(Guid id, CancellationToken cancellationToken)
    {
        var entidad = await ObtenerMetodoPagoEntidadAsync(id, cancellationToken);
        return entidad is null
            ? Resultado<MetodoPagoDto>.Fallo(NoEncontrado("metodo_pago"))
            : Resultado<MetodoPagoDto>.Exito(Convertir(entidad));
    }

    public async Task<Resultado<MetodoPagoDto>> CrearMetodoPagoAsync(
        SolicitudMetodoPago solicitud,
        CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<MetodoPagoDto>.Fallo(NoConfiguracion());
        }

        if (solicitud.EsEfectivo &&
            await repositorio.ExisteOtroMetodoEfectivoAsync(configuracion.Id, null, cancellationToken))
        {
            return Resultado<MetodoPagoDto>.Fallo(new ErrorDominio(
                "metodo_pago.efectivo_duplicado", "Solo puede haber un método de pago marcado como efectivo."));
        }

        var nuevo = MetodoPagoConfigurado.Crear(
            configuracion.Id, solicitud.Nombre, solicitud.RequiereReferencia, solicitud.EsEfectivo);
        if (!nuevo.EsExitoso)
        {
            return Resultado<MetodoPagoDto>.Fallo(nuevo.Error!);
        }

        repositorio.Agregar(nuevo.Valor!);
        return await GuardarYConvertirAsync(nuevo.Valor!, cancellationToken);
    }

    public async Task<Resultado<MetodoPagoDto>> ActualizarMetodoPagoAsync(
        Guid id,
        SolicitudMetodoPago solicitud,
        CancellationToken cancellationToken)
    {
        var metodo = await ObtenerMetodoPagoEntidadAsync(id, cancellationToken);
        if (metodo is null)
        {
            return Resultado<MetodoPagoDto>.Fallo(NoEncontrado("metodo_pago"));
        }

        if (solicitud.EsEfectivo &&
            await repositorio.ExisteOtroMetodoEfectivoAsync(metodo.ConfiguracionNegocioId, metodo.Id, cancellationToken))
        {
            return Resultado<MetodoPagoDto>.Fallo(new ErrorDominio(
                "metodo_pago.efectivo_duplicado", "Solo puede haber un método de pago marcado como efectivo."));
        }

        var resultado = metodo.Actualizar(solicitud.Nombre, solicitud.RequiereReferencia, solicitud.EsEfectivo);
        if (!resultado.EsExitoso)
        {
            return Resultado<MetodoPagoDto>.Fallo(resultado.Error!);
        }

        return await GuardarYConvertirAsync(metodo, cancellationToken);
    }

    public async Task<Resultado<MetodoPagoDto>> EstablecerEstadoMetodoPagoAsync(
        Guid id,
        bool activo,
        CancellationToken cancellationToken)
    {
        var metodo = await ObtenerMetodoPagoEntidadAsync(id, cancellationToken);
        if (metodo is null)
        {
            return Resultado<MetodoPagoDto>.Fallo(NoEncontrado("metodo_pago"));
        }

        metodo.EstablecerEstado(activo);
        return await GuardarYConvertirAsync(metodo, cancellationToken);
    }

    public async Task<Resultado<IReadOnlyList<NumeracionDocumentoDto>>> ListarNumeracionesAsync(
        CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<IReadOnlyList<NumeracionDocumentoDto>>.Fallo(NoConfiguracion());
        }

        var elementos = await repositorio.ListarNumeracionesAsync(configuracion.Id, cancellationToken);
        return Resultado<IReadOnlyList<NumeracionDocumentoDto>>.Exito(elementos.Select(Convertir).ToArray());
    }

    public async Task<Resultado<NumeracionDocumentoDto>> CrearNumeracionAsync(
        SolicitudNumeracionDocumento solicitud,
        CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerPrincipalAsync(cancellationToken);
        if (configuracion is null)
        {
            return Resultado<NumeracionDocumentoDto>.Fallo(NoConfiguracion());
        }

        var nueva = NumeracionDocumento.Crear(
            configuracion.Id,
            solicitud.TipoDocumento,
            solicitud.Prefijo,
            solicitud.SiguienteNumero);
        if (!nueva.EsExitoso)
        {
            return Resultado<NumeracionDocumentoDto>.Fallo(nueva.Error!);
        }

        repositorio.Agregar(nueva.Valor!);
        if (!await repositorio.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<NumeracionDocumentoDto>.Fallo(Conflicto());
        }

        return Resultado<NumeracionDocumentoDto>.Exito(Convertir(nueva.Valor!));
    }

    public async Task<Resultado<NumeracionDocumentoDto>> ActualizarPrefijoAsync(
        TipoDocumentoVenta tipoDocumento,
        string? prefijo,
        CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerPrincipalAsync(cancellationToken);
        var numeracion = configuracion is null
            ? null
            : await repositorio.ObtenerNumeracionAsync(configuracion.Id, tipoDocumento, cancellationToken);
        if (numeracion is null)
        {
            return Resultado<NumeracionDocumentoDto>.Fallo(NoEncontrado("numeracion"));
        }

        var actualizacion = numeracion.ActualizarPrefijo(prefijo);
        if (!actualizacion.EsExitoso)
        {
            return Resultado<NumeracionDocumentoDto>.Fallo(actualizacion.Error!);
        }

        if (!await repositorio.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<NumeracionDocumentoDto>.Fallo(Conflicto());
        }

        return Resultado<NumeracionDocumentoDto>.Exito(Convertir(numeracion));
    }

    private async Task<ImpuestoConfigurado?> ObtenerImpuestoEntidadAsync(Guid id, CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerPrincipalAsync(cancellationToken);
        return configuracion is null
            ? null
            : await repositorio.ObtenerImpuestoAsync(configuracion.Id, id, cancellationToken);
    }

    private async Task<MetodoPagoConfigurado?> ObtenerMetodoPagoEntidadAsync(Guid id, CancellationToken cancellationToken)
    {
        var configuracion = await repositorio.ObtenerPrincipalAsync(cancellationToken);
        return configuracion is null
            ? null
            : await repositorio.ObtenerMetodoPagoAsync(configuracion.Id, id, cancellationToken);
    }

    private async Task<Resultado<ImpuestoDto>> GuardarYConvertirAsync(
        ImpuestoConfigurado impuesto,
        CancellationToken cancellationToken)
    {
        if (!await repositorio.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<ImpuestoDto>.Fallo(Conflicto());
        }

        return Resultado<ImpuestoDto>.Exito(Convertir(impuesto));
    }

    private async Task<Resultado<MetodoPagoDto>> GuardarYConvertirAsync(
        MetodoPagoConfigurado metodo,
        CancellationToken cancellationToken)
    {
        if (!await repositorio.GuardarCambiosAsync(cancellationToken))
        {
            return Resultado<MetodoPagoDto>.Fallo(Conflicto());
        }

        return Resultado<MetodoPagoDto>.Exito(Convertir(metodo));
    }

    private static ConfiguracionNegocioDto Convertir(ConfiguracionNegocio entidad) => new(
        entidad.Id, entidad.NombreComercial, entidad.RazonSocial, entidad.IdentificacionFiscal,
        entidad.Direccion, entidad.Telefono, entidad.Correo, entidad.CodigoMoneda,
        entidad.PermitirVentaSinStock, entidad.ComisionProductosHabilitada,
        entidad.PermitirSaldosPendientes, entidad.FacturacionHabilitada, entidad.ExigirEmpleadoVentaServicio,
        entidad.FechaCreacionUtc, entidad.FechaModificacionUtc);

    private static ImpuestoDto Convertir(ImpuestoConfigurado entidad) => new(
        entidad.Id, entidad.Nombre, entidad.Porcentaje, entidad.Activo,
        entidad.FechaCreacionUtc, entidad.FechaModificacionUtc);

    private static MetodoPagoDto Convertir(MetodoPagoConfigurado entidad) => new(
        entidad.Id, entidad.Nombre, entidad.RequiereReferencia, entidad.EsEfectivo, entidad.Activo,
        entidad.FechaCreacionUtc, entidad.FechaModificacionUtc);

    private static NumeracionDocumentoDto Convertir(NumeracionDocumento entidad) => new(
        entidad.Id, entidad.TipoDocumento.ToString(), entidad.Prefijo, entidad.SiguienteNumero,
        entidad.FechaCreacionUtc, entidad.FechaModificacionUtc);

    private static ErrorDominio NoConfiguracion() => new(
        "configuracion.no_configurada",
        "Primero debe registrar la configuración del establecimiento.");

    private static ErrorDominio NoEncontrado(string recurso) => new(
        $"configuracion.{recurso}_no_encontrado",
        "No se encontró el elemento solicitado.");

    private static ErrorDominio Conflicto() => new(
        "configuracion.conflicto",
        "El elemento cambió en otra operación o ya existe con los mismos datos.");

    private static bool EsCodigoMonedaValido(string? codigo) =>
        codigo is { Length: 3 } && codigo.All(caracter => caracter is >= 'A' and <= 'Z');
}
