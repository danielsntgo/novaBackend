using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        builder.ToTable("ventas", tabla => tabla.HasCheckConstraint(
            "ck_ventas_estado", "estado IN ('Finalizada', 'ParcialmenteDevuelta', 'Devuelta', 'Anulada')"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CajaId).HasColumnName("caja_id");
        builder.Property(x => x.ClienteId).HasColumnName("cliente_id");
        builder.Property(x => x.ClienteNombre).HasColumnName("cliente_nombre").HasMaxLength(150);
        builder.Property(x => x.ClienteDocumento).HasColumnName("cliente_documento").HasMaxLength(50);
        builder.Property(x => x.FechaVentaUtc).HasColumnName("fecha_venta_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(18, 2);
        builder.Property(x => x.TipoDescuentoGeneral).HasColumnName("tipo_descuento_general").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ValorDescuentoGeneral).HasColumnName("valor_descuento_general").HasPrecision(18, 2);
        builder.Property(x => x.DescuentoLineas).HasColumnName("descuento_lineas").HasPrecision(18, 2);
        builder.Property(x => x.DescuentoGeneral).HasColumnName("descuento_general").HasPrecision(18, 2);
        builder.Property(x => x.Impuestos).HasColumnName("impuestos").HasPrecision(18, 2);
        builder.Property(x => x.Total).HasColumnName("total").HasPrecision(18, 2);
        builder.Property(x => x.TotalPagado).HasColumnName("total_pagado").HasPrecision(18, 2);
        builder.Property(x => x.TotalDevuelto).HasColumnName("total_devuelto").HasPrecision(18, 2);
        builder.Property(x => x.TotalReintegrado).HasColumnName("total_reintegrado").HasPrecision(18, 2);
        builder.Property(x => x.TotalPendiente).HasColumnName("total_pendiente").HasPrecision(18, 2);
        builder.Property(x => x.MotivoAnulacion).HasColumnName("motivo_anulacion").HasMaxLength(250);
        builder.Property(x => x.AnuladaPorId).HasColumnName("anulada_por_id");
        builder.Property(x => x.FechaAnulacionUtc).HasColumnName("fecha_anulacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasOne<Caja>().WithMany().HasForeignKey(x => x.CajaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Pagos).WithOne().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Documentos).WithOne().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Devoluciones).WithOne().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Detalles).HasField("_detalles").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Pagos).HasField("_pagos").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Documentos).HasField("_documentos").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Devoluciones).HasField("_devoluciones").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => new { x.FechaVentaUtc, x.Id }).HasDatabaseName("ix_ventas_fecha_id");
        builder.HasIndex(x => new { x.ClienteId, x.FechaVentaUtc }).HasDatabaseName("ix_ventas_cliente_fecha");
    }
}
