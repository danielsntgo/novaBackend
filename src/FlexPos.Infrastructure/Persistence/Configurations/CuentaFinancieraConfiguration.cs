using FlexPos.Domain.Entities;
using FlexPos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class CuentaFinancieraConfiguration : IEntityTypeConfiguration<CuentaFinanciera>
{
    public void Configure(EntityTypeBuilder<CuentaFinanciera> builder)
    {
        builder.ToTable("cuentas_financieras", tabla =>
        {
            tabla.HasCheckConstraint("ck_cuentas_financieras_cupo", "cupo_credito IS NULL OR cupo_credito > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ConfiguracionNegocioId).HasColumnName("configuracion_negocio_id");
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(80).IsRequired();
        builder.Property(x => x.NombreNormalizado).HasColumnName("nombre_normalizado").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Entidad).HasColumnName("entidad").HasMaxLength(100);
        builder.Property(x => x.UltimosDigitos).HasColumnName("ultimos_digitos").HasMaxLength(8);
        builder.Property(x => x.CupoCredito).HasColumnName("cupo_credito").HasPrecision(18, 2);
        builder.Property(x => x.Activa).HasColumnName("activa");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasOne<ConfiguracionNegocio>().WithMany().HasForeignKey(x => x.ConfiguracionNegocioId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ConfiguracionNegocioId, x.NombreNormalizado })
            .IsUnique().HasDatabaseName("ux_cuentas_financieras_configuracion_nombre");
        builder.HasIndex(x => new { x.ConfiguracionNegocioId, x.Tipo })
            .HasDatabaseName("ix_cuentas_financieras_tipo");
        builder.HasIndex(x => x.ConfiguracionNegocioId)
            .IsUnique()
            .HasFilter("tipo = 'Efectivo' AND activa = true")
            .HasDatabaseName("ux_cuentas_financieras_efectivo_activa");
    }
}
