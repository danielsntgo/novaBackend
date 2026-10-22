using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class DetalleDevolucionCompraConfiguration : IEntityTypeConfiguration<DetalleDevolucionCompra>
{
    public void Configure(EntityTypeBuilder<DetalleDevolucionCompra> builder)
    {
        builder.ToTable("detalles_devolucion_compra", tabla =>
        {
            tabla.HasCheckConstraint("ck_detalles_devolucion_compra_cantidad", "cantidad > 0");
            tabla.HasCheckConstraint("ck_detalles_devolucion_compra_importe", "importe >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.DevolucionCompraId).HasColumnName("devolucion_compra_id");
        builder.Property(x => x.DetalleCompraId).HasColumnName("detalle_compra_id");
        builder.Property(x => x.Cantidad).HasColumnName("cantidad").HasPrecision(18, 3);
        builder.Property(x => x.Importe).HasColumnName("importe").HasPrecision(18, 2);
        builder.HasOne<DetalleCompra>().WithMany().HasForeignKey(x => x.DetalleCompraId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.DevolucionCompraId, x.DetalleCompraId }).IsUnique()
            .HasDatabaseName("ux_detalles_devolucion_compra_linea");
    }
}
