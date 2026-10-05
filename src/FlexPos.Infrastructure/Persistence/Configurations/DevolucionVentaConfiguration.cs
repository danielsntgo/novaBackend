using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class DevolucionVentaConfiguration : IEntityTypeConfiguration<DevolucionVenta>
{
    public void Configure(EntityTypeBuilder<DevolucionVenta> builder)
    {
        builder.ToTable("devoluciones_venta");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.VentaId).HasColumnName("venta_id");
        builder.Property(x => x.FechaUtc).HasColumnName("fecha_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Motivo).HasColumnName("motivo").HasMaxLength(250).IsRequired();
        builder.Property(x => x.ImporteLineas).HasColumnName("importe_lineas").HasPrecision(18, 2);
        builder.Property(x => x.ImporteReintegrado).HasColumnName("importe_reintegrado").HasPrecision(18, 2);
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.DevolucionVentaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Pagos).WithOne().HasForeignKey(x => x.DevolucionVentaId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Detalles).HasField("_detalles").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Pagos).HasField("_pagos").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => new { x.VentaId, x.FechaUtc }).HasDatabaseName("ix_devoluciones_venta_fecha");
    }
}
