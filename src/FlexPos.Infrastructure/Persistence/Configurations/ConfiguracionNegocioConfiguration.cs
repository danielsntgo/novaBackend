using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class ConfiguracionNegocioConfiguration : IEntityTypeConfiguration<ConfiguracionNegocio>
{
    public void Configure(EntityTypeBuilder<ConfiguracionNegocio> builder)
    {
        builder.ToTable("configuracion_negocio");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.NombreComercial).HasColumnName("nombre_comercial").HasMaxLength(150).IsRequired();
        builder.Property(x => x.RazonSocial).HasColumnName("razon_social").HasMaxLength(180);
        builder.Property(x => x.IdentificacionFiscal).HasColumnName("identificacion_fiscal").HasMaxLength(40);
        builder.Property(x => x.Direccion).HasColumnName("direccion").HasMaxLength(250);
        builder.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(40);
        builder.Property(x => x.Correo).HasColumnName("correo").HasMaxLength(256);
        builder.Property(x => x.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(3).IsRequired();
        builder.Property(x => x.PermitirVentaSinStock).HasColumnName("permitir_venta_sin_stock");
        builder.Property(x => x.ComisionProductosHabilitada).HasColumnName("comision_productos_habilitada");
        builder.Property(x => x.PermitirSaldosPendientes).HasColumnName("permitir_saldos_pendientes");
        builder.Property(x => x.FacturacionHabilitada).HasColumnName("facturacion_habilitada");
        builder.Property(x => x.ExigirEmpleadoVentaServicio).HasColumnName("exigir_empleado_venta_servicio");
        builder.Property(x => x.EsPrincipal).HasColumnName("es_principal");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => x.EsPrincipal)
            .IsUnique()
            .HasFilter("es_principal = true")
            .HasDatabaseName("ux_configuracion_negocio_principal");
    }
}
