using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class TransferenciaFinancieraConfiguration : IEntityTypeConfiguration<TransferenciaFinanciera>
{
    public void Configure(EntityTypeBuilder<TransferenciaFinanciera> builder)
    {
        builder.ToTable("transferencias_financieras", tabla =>
            tabla.HasCheckConstraint("ck_transferencias_financieras_importe", "importe > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CuentaOrigenId).HasColumnName("cuenta_origen_id");
        builder.Property(x => x.CuentaDestinoId).HasColumnName("cuenta_destino_id");
        builder.Property(x => x.Importe).HasColumnName("importe").HasPrecision(18, 2);
        builder.Property(x => x.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Concepto).HasColumnName("concepto").HasMaxLength(250);
        builder.Property(x => x.Referencia).HasColumnName("referencia").HasMaxLength(120);
        builder.Property(x => x.FechaUtc).HasColumnName("fecha_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasOne<CuentaFinanciera>().WithMany().HasForeignKey(x => x.CuentaOrigenId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CuentaFinanciera>().WithMany().HasForeignKey(x => x.CuentaDestinoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.FechaUtc).HasDatabaseName("ix_transferencias_financieras_fecha");
    }
}
