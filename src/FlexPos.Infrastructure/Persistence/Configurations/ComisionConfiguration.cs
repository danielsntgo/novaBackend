using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class ComisionConfiguration : IEntityTypeConfiguration<Comision>
{
    public void Configure(EntityTypeBuilder<Comision> builder)
    {
        builder.ToTable("comisiones", tabla =>
        {
            tabla.HasCheckConstraint("ck_comisiones_importes", "base_calculo >= 0 AND cantidad > 0 AND importe > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.EmpleadoId).HasColumnName("empleado_id");
        builder.Property(x => x.ServicioId).HasColumnName("servicio_id");
        builder.Property(x => x.VentaId).HasColumnName("venta_id");
        builder.Property(x => x.DetalleVentaId).HasColumnName("detalle_venta_id");
        builder.Property(x => x.TipoMovimiento).HasColumnName("tipo_movimiento").HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.TipoTarifa).HasColumnName("tipo_tarifa").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ValorTarifa).HasColumnName("valor_tarifa").HasPrecision(18, 2);
        builder.Property(x => x.BaseCalculo).HasColumnName("base_calculo").HasPrecision(18, 2);
        builder.Property(x => x.Cantidad).HasColumnName("cantidad").HasPrecision(18, 3);
        builder.Property(x => x.Importe).HasColumnName("importe").HasPrecision(18, 2);
        builder.Property(x => x.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.ComisionOriginalId).HasColumnName("comision_original_id");
        builder.Property(x => x.DevolucionVentaId).HasColumnName("devolucion_venta_id");
        builder.Property(x => x.LiquidacionComisionId).HasColumnName("liquidacion_comision_id");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasOne<Empleado>().WithMany().HasForeignKey(x => x.EmpleadoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Servicio>().WithMany().HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Venta>().WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DetalleVenta>().WithMany().HasForeignKey(x => x.DetalleVentaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ComisionOriginal).WithMany().HasForeignKey(x => x.ComisionOriginalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DevolucionVenta>().WithMany().HasForeignKey(x => x.DevolucionVentaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LiquidacionComision>().WithMany().HasForeignKey(x => x.LiquidacionComisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.DetalleVentaId).IsUnique()
            .HasFilter("tipo_movimiento = 'DevengoServicio'").HasDatabaseName("ux_comisiones_devengo_detalle");
        builder.HasIndex(x => new { x.EmpleadoId, x.Estado, x.FechaCreacionUtc }).HasDatabaseName("ix_comisiones_empleado_estado_fecha");
        builder.HasIndex(x => x.ComisionOriginalId).HasDatabaseName("ix_comisiones_original");
        builder.HasIndex(x => x.LiquidacionComisionId).HasDatabaseName("ix_comisiones_liquidacion");
    }
}
