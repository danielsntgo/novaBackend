using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class CompraConfiguration : IEntityTypeConfiguration<Compra>
{
    public void Configure(EntityTypeBuilder<Compra> builder)
    {
        builder.ToTable("compras", tabla =>
            tabla.HasCheckConstraint("ck_compras_total", "total_importe >= 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ProveedorId).HasColumnName("proveedor_id");
        builder.Property(x => x.FechaCompraUtc).HasColumnName("fecha_compra_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Referencia).HasColumnName("referencia").HasMaxLength(80);
        builder.Property(x => x.ReferenciaNormalizada).HasColumnName("referencia_normalizada").HasMaxLength(80);
        builder.Property(x => x.Observacion).HasColumnName("observacion").HasMaxLength(500);
        builder.Property(x => x.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(3).IsRequired();
        builder.Property(x => x.TotalImporte).HasColumnName("total_importe").HasPrecision(18, 2);
        builder.Property(x => x.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.FechaConfirmacionUtc).HasColumnName("fecha_confirmacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasOne(x => x.Proveedor).WithMany().HasForeignKey(x => x.ProveedorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Detalles).WithOne(x => x.Compra).HasForeignKey(x => x.CompraId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Detalles).HasField("_detalles").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => new { x.Estado, x.FechaCompraUtc }).HasDatabaseName("ix_compras_estado_fecha");
        builder.HasIndex(x => x.ProveedorId).HasDatabaseName("ix_compras_proveedor");
        builder.HasIndex(x => x.ReferenciaNormalizada).HasOperators("text_pattern_ops").HasDatabaseName("ix_compras_referencia_prefijo");
    }
}
