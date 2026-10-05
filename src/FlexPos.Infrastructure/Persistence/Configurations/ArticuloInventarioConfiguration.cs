using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class ArticuloInventarioConfiguration : IEntityTypeConfiguration<ArticuloInventario>
{
    public void Configure(EntityTypeBuilder<ArticuloInventario> builder)
    {
        builder.ToTable("articulos_inventario", tabla =>
        {
            tabla.HasCheckConstraint("ck_articulos_inventario_minimo", "cantidad_minima >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(50);
        builder.Property(x => x.CodigoNormalizado).HasColumnName("codigo_normalizado").HasMaxLength(50);
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(120).IsRequired();
        builder.Property(x => x.NombreNormalizado).HasColumnName("nombre_normalizado").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.UnidadBase).HasColumnName("unidad_base").HasMaxLength(20).IsRequired();
        builder.Property(x => x.UnidadBaseNormalizada).HasColumnName("unidad_base_normalizada").HasMaxLength(20).IsRequired();
        builder.Property(x => x.ManejaFraccion).HasColumnName("maneja_fraccion");
        builder.Property(x => x.Categoria).HasColumnName("categoria").HasMaxLength(100);
        builder.Property(x => x.CategoriaNormalizada).HasColumnName("categoria_normalizada").HasMaxLength(100);
        builder.Property(x => x.ExistenciaActual).HasColumnName("existencia_actual").HasPrecision(18, 3);
        builder.Property(x => x.CantidadMinima).HasColumnName("cantidad_minima").HasPrecision(18, 3);
        builder.Property(x => x.Activo).HasColumnName("activo");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();

        builder.OwnsOne(x => x.CostoPromedio, dinero =>
        {
            dinero.Property(x => x.Importe).HasColumnName("costo_promedio_importe").HasPrecision(18, 2);
            dinero.Property(x => x.CodigoMoneda).HasColumnName("costo_promedio_codigo_moneda").HasMaxLength(3).IsRequired();
        });
        builder.Navigation(x => x.CostoPromedio).IsRequired();

        builder.OwnsOne(x => x.PrecioVenta, dinero =>
        {
            dinero.Property(x => x.Importe).HasColumnName("precio_venta_importe").HasPrecision(18, 2);
            dinero.Property(x => x.CodigoMoneda).HasColumnName("precio_venta_codigo_moneda").HasMaxLength(3);
        });
        builder.Navigation(x => x.PrecioVenta).IsRequired(false);

        builder.HasIndex(x => new { x.Activo, x.Tipo, x.NombreNormalizado })
            .HasDatabaseName("ix_articulos_activo_tipo_nombre");
        builder.HasIndex(x => x.NombreNormalizado).HasOperators("text_pattern_ops")
            .HasDatabaseName("ix_articulos_nombre_prefijo");
        builder.HasIndex(x => x.CodigoNormalizado).IsUnique().HasFilter("codigo_normalizado IS NOT NULL")
            .HasDatabaseName("ux_articulos_codigo_normalizado");
        builder.HasIndex(x => x.CategoriaNormalizada).HasOperators("text_pattern_ops")
            .HasDatabaseName("ix_articulos_categoria_prefijo");
    }
}
