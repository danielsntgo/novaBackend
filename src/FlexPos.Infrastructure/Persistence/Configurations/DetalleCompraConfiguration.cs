using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class DetalleCompraConfiguration : IEntityTypeConfiguration<DetalleCompra>
{
    public void Configure(EntityTypeBuilder<DetalleCompra> builder)
    {
        builder.ToTable("detalles_compra", tabla =>
            tabla.HasCheckConstraint("ck_detalles_compra_cantidad", "cantidad > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompraId).HasColumnName("compra_id");
        builder.Property(x => x.ArticuloId).HasColumnName("articulo_id");
        builder.Property(x => x.NombreArticulo).HasColumnName("nombre_articulo").HasMaxLength(120).IsRequired();
        builder.Property(x => x.UnidadBase).HasColumnName("unidad_base").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Cantidad).HasColumnName("cantidad").HasPrecision(18, 3);
        builder.Property(x => x.Vigente).HasColumnName("vigente");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasOne<ArticuloInventario>().WithMany().HasForeignKey(x => x.ArticuloId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(x => x.CostoUnitario, dinero =>
        {
            dinero.Property(x => x.Importe).HasColumnName("costo_unitario_importe").HasPrecision(18, 2);
            dinero.Property(x => x.CodigoMoneda).HasColumnName("costo_unitario_codigo_moneda").HasMaxLength(3).IsRequired();
        });
        builder.Navigation(x => x.CostoUnitario).IsRequired();
        builder.OwnsOne(x => x.TotalLinea, dinero =>
        {
            dinero.Property(x => x.Importe).HasColumnName("total_linea_importe").HasPrecision(18, 2);
            dinero.Property(x => x.CodigoMoneda).HasColumnName("total_linea_codigo_moneda").HasMaxLength(3).IsRequired();
        });
        builder.Navigation(x => x.TotalLinea).IsRequired();
        builder.HasIndex(x => new { x.CompraId, x.Vigente }).HasDatabaseName("ix_detalles_compra_vigentes");
        builder.HasIndex(x => x.ArticuloId).HasDatabaseName("ix_detalles_compra_articulo");
    }
}
