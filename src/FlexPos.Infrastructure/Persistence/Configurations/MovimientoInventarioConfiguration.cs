using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class MovimientoInventarioConfiguration : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> builder)
    {
        builder.ToTable("movimientos_inventario", tabla =>
            tabla.HasCheckConstraint("ck_movimientos_inventario_cantidad", "cantidad > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ArticuloId).HasColumnName("articulo_id");
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.Cantidad).HasColumnName("cantidad").HasPrecision(18, 3);
        builder.Property(x => x.ExistenciaResultante).HasColumnName("existencia_resultante").HasPrecision(18, 3);
        builder.Property(x => x.Motivo).HasColumnName("motivo").HasMaxLength(250);
        builder.Property(x => x.CompraId).HasColumnName("compra_id");
        builder.Property(x => x.DetalleCompraId).HasColumnName("detalle_compra_id");
        builder.Property(x => x.VentaId).HasColumnName("venta_id");
        builder.Property(x => x.DetalleVentaId).HasColumnName("detalle_venta_id");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasOne(x => x.Articulo).WithMany().HasForeignKey(x => x.ArticuloId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Compra).WithMany().HasForeignKey(x => x.CompraId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DetalleCompra).WithMany().HasForeignKey(x => x.DetalleCompraId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Venta>().WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DetalleVenta>().WithMany().HasForeignKey(x => x.DetalleVentaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ArticuloId, x.FechaCreacionUtc }).HasDatabaseName("ix_movimientos_articulo_fecha");
        builder.HasIndex(x => x.CompraId).HasDatabaseName("ix_movimientos_compra");
        builder.HasIndex(x => x.VentaId).HasDatabaseName("ix_movimientos_venta");

        builder.OwnsOne(x => x.CostoUnitario, dinero =>
        {
            dinero.Property(x => x.Importe).HasColumnName("costo_unitario_importe").HasPrecision(18, 2);
            dinero.Property(x => x.CodigoMoneda).HasColumnName("costo_unitario_codigo_moneda").HasMaxLength(3).IsRequired();
        });
        builder.Navigation(x => x.CostoUnitario).IsRequired();
    }
}
