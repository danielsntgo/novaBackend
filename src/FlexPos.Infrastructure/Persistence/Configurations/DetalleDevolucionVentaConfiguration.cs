using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class DetalleDevolucionVentaConfiguration : IEntityTypeConfiguration<DetalleDevolucionVenta>
{
    public void Configure(EntityTypeBuilder<DetalleDevolucionVenta> builder)
    {
        builder.ToTable("detalles_devolucion_venta", tabla =>
            tabla.HasCheckConstraint("ck_detalles_devolucion_cantidad", "cantidad > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.DevolucionVentaId).HasColumnName("devolucion_venta_id");
        builder.Property(x => x.DetalleVentaId).HasColumnName("detalle_venta_id");
        builder.Property(x => x.Cantidad).HasColumnName("cantidad").HasPrecision(18, 3);
        builder.Property(x => x.Importe).HasColumnName("importe").HasPrecision(18, 2);
        builder.HasOne<DetalleVenta>().WithMany().HasForeignKey(x => x.DetalleVentaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.DetalleVentaId).HasDatabaseName("ix_devolucion_detalle_venta");
    }
}
