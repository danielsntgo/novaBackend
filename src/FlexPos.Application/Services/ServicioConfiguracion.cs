using FlexPos.Application.DTOs.Configuracion;
using FlexPos.Application.Interfaces;
using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using FlexPos.Domain.Errors;

namespace FlexPos.Application.Services;

public sealed class ServicioConfiguracion(
    IRepositorioConfiguracion repositorio,
    IRepositorioFinanzas finanzas,
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
            var efectivoBase = CuentaFinanciera.Crear(
                configuracion.Id, "Caja principal", TipoCuentaFinanciera.Efectivo,
                configuracion.CodigoMoneda, null, null, null);
            var porAsignarBase = CuentaFinanciera.Crear(
                configuracion.Id, "Cuenta por asignar", TipoCuentaFinanciera.Otro,
                configuracion.CodigoMoneda, null, null, null);
            if (!efectivoBase.EsExitoso || !porAsignarBase.EsExitoso)
            {
                return Resultado<ConfiguracionNegocioDto>.Fallo(
                    efectivoBase.Error ?? porAsignarBase.Error!);
            }

            finanzas.Agregar(efectivoBase.Valor!);
            finanzas.Agregar(porAsignarBase.Valor!);
        }
        else
        {
            var monedaSolicitada = solicitud.CodigoMoneda?.Trim().ToUpperInvariant();
            var cambiaMoneda = EsCodigoMonedaValido(monedaSolicitada) &&
                               !string.Equals(configuracion.CodigoMoneda, monedaSolicitada, StringComparison.Ordinal);
            CuentaFinanciera? cuentaEfectivoBase = null;
            CuentaFinanciera? cuentaPorAsignarBase = null;
            if (cambiaMoneda &&
                (await servicios.ExisteAlgunoAsync(cancellationToken) ||
                 await inventario.ExisteAlgunoAsync(cancellationToken) ||
                 await compras.ExisteAlgunaAsync(cancellationToken) ||
                 await repositorio.ExisteCajaOVentaAsync(cancellationToken)))
            {
                return Resultado<ConfiguracionNegocioDto>.Fallo(new ErrorDominio(
                    "configuracion.moneda_conflicto",
                    "No se puede cambiar la moneda mientras existan servicios, articulos, compras, cajas o ventas registradas."));
            }

            if (cambiaMoneda)
            {
                cuentaEfectivoBase = await finanzas.ObtenerCuentaBaseAsync(
                    configuracion.Id, TipoCuentaFinanciera.Efectivo, ServicioFinanzas.CuentaEfectivoBase, cancellationToken);
                cuentaPorAsignarBase = await finanzas.ObtenerCuentaBaseAsync(
                    configuracion.Id, TipoCuentaFinanciera.Otro, "Cuenta por asignar", cancellationToken);
                if (cuentaEfectivoBase is null || cuentaPorAsignarBase is null)
                {
                    return Resultado<ConfiguracionNegocioDto>.Fallo(new ErrorDominio(
                        "configuracion.moneda_conflicto",
                        "No se encontraron las cuentas financieras base para actualizar la moneda."));
                }

                var metodos = await repositorio.ListarMetodosPagoAsync(configuracion.Id, cancellationToken);
                if (metodos.Any(x => x.CuentaFinancieraId != cuentaEfectivoBase.Id &&
                                     x.CuentaFinancieraId != cuentaPorAsignarBase.Id))
                {
                    return Resultado<ConfiguracionNegocioDto>.Fallo(new ErrorDominio(
                        "configuracion.moneda_conflicto",
                        "No se puede cambiar la moneda mientras un método use una cuenta financiera personalizada."));
                }
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

            if (cambiaMoneda)
            {
                foreach (var cuenta in new[] { cuentaEfectivoBase!, cuentaPorAsignarBase! })
                {
                    var cambioCuenta = cuenta.Actualizar(
                        cuenta.Nombre, cuenta.Tipo, configuracion.CodigoMoneda, cuenta.Entidad,
                        cuenta.UltimosDigitos, cuenta.CupoCredito, tieneDependencias: false, saldoActual: 0m);
                    if (!cambioCuenta.EsExitoso)
                    {
                        return Resultado<ConfiguracionNegocioDto>.Fallo(cambioCuenta.Error!);
                    }
                }
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

        var cuenta = await finanzas.ObtenerCuentaAsync(
            configuracion.Id, solicitud.CuentaFinancieraId, cancellationToken);
        if (cuenta is null || !cuenta.Activa || cuenta.CodigoMoneda != configuracion.CodigoMoneda)
        {
            return Resultado<MetodoPagoDto>.Fallo(new ErrorDominio(
                "metodo_pago.cuenta_invalida", "La cuenta debe existir, estar activa y usar la moneda del negocio."));
        }

        var nuevo = MetodoPagoConfigurado.Crear(
            configuracion.Id, solicitud.Nombre, solicitud.RequiereReferencia, solicitud.EsEfectivo, cuenta);
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

        var cuenta = await finanzas.ObtenerCuentaAsync(
            metodo.ConfiguracionNegocioId, solicitud.CuentaFinancieraId, cancellationToken);
        var configuracion = await repositorio.ObtenerPrincipalAsync(cancellationToken);
        if (cuenta is null || !cuenta.Activa || configuracion is null || cuenta.CodigoMoneda != configuracion.CodigoMoneda)
        {
            return Resultado<MetodoPagoDto>.Fallo(new ErrorDominio(
                "metodo_pago.cuenta_invalida", "La cuenta debe existir, estar activa y usar la moneda del negocio."));
        }

        var resultado = metodo.Actualizar(
            solicitud.Nombre, solicitud.RequiereReferencia, solicitud.EsEfectivo, cuenta);
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
        entidad.CuentaFinancieraId, entidad.CuentaFinanciera.Nombre,
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
