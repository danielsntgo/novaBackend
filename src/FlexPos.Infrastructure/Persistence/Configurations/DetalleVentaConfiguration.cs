using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class DetalleVentaConfiguration : IEntityTypeConfiguration<DetalleVenta>
{
    public void Configure(EntityTypeBuilder<DetalleVenta> builder)
    {
        builder.ToTable("detalles_venta", tabla => tabla.HasCheckConstraint("ck_detalles_venta_cantidad", "cantidad > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.VentaId).HasColumnName("venta_id");
        builder.Property(x => x.ArticuloInventarioId).HasColumnName("articulo_inventario_id");
        builder.Property(x => x.ServicioId).HasColumnName("servicio_id");
        builder.Property(x => x.EmpleadoId).HasColumnName("empleado_id");
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(50);
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Unidad).HasColumnName("unidad").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Cantidad).HasColumnName("cantidad").HasPrecision(18, 3);
        builder.Property(x => x.PrecioUnitario).HasColumnName("precio_unitario").HasPrecision(18, 2);
        builder.Property(x => x.CostoInventarioUnitario).HasColumnName("costo_inventario_unitario").HasPrecision(18, 2);
        builder.Property(x => x.TipoDescuentoLinea).HasColumnName("tipo_descuento_linea").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ValorDescuento).HasColumnName("valor_descuento").HasPrecision(18, 2);
        builder.Property(x => x.ImporteBruto).HasColumnName("importe_bruto").HasPrecision(18, 2);
        builder.Property(x => x.DescuentoImporte).HasColumnName("descuento_importe").HasPrecision(18, 2);
        builder.Property(x => x.DescuentoGeneralImporte).HasColumnName("descuento_general_importe").HasPrecision(18, 2);
        builder.Property(x => x.ImpuestoNombre).HasColumnName("impuesto_nombre").HasMaxLength(100);
        builder.Property(x => x.ImpuestoPorcentaje).HasColumnName("impuesto_porcentaje").HasPrecision(5, 2);
        builder.Property(x => x.ImpuestoImporte).HasColumnName("impuesto_importe").HasPrecision(18, 2);
        builder.Property(x => x.TotalImporte).HasColumnName("total_importe").HasPrecision(18, 2);
        builder.HasOne<ArticuloInventario>().WithMany().HasForeignKey(x => x.ArticuloInventarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Servicio>().WithMany().HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Empleado>().WithMany().HasForeignKey(x => x.EmpleadoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ArticuloInventarioId).HasDatabaseName("ix_detalles_venta_articulo");
    }
}
