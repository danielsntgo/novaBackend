using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class DevolucionCompraConfiguration : IEntityTypeConfiguration<DevolucionCompra>
{
    public void Configure(EntityTypeBuilder<DevolucionCompra> builder)
    {
        builder.ToTable("devoluciones_compra", tabla =>
        {
            tabla.HasCheckConstraint("ck_devoluciones_compra_importes", "importe_lineas >= 0 AND importe_reintegrado >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompraId).HasColumnName("compra_id");
        builder.Property(x => x.FechaUtc).HasColumnName("fecha_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Motivo).HasColumnName("motivo").HasMaxLength(250).IsRequired();
        builder.Property(x => x.ImporteLineas).HasColumnName("importe_lineas").HasPrecision(18, 2);
        builder.Property(x => x.ImporteReintegrado).HasColumnName("importe_reintegrado").HasPrecision(18, 2);
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.DevolucionCompraId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Detalles).HasField("_detalles").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Pagos).WithOne().HasForeignKey(x => x.DevolucionCompraId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Pagos).HasField("_pagos").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => new { x.CompraId, x.FechaUtc, x.Id }).HasDatabaseName("ix_devoluciones_compra_compra_fecha");
    }
}
