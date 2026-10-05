using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class MovimientoCajaConfiguration : IEntityTypeConfiguration<MovimientoCaja>
{
    public void Configure(EntityTypeBuilder<MovimientoCaja> builder)
    {
        builder.ToTable("movimientos_caja", tabla => tabla.HasCheckConstraint("ck_movimientos_caja_importe", "importe > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CajaId).HasColumnName("caja_id");
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.Importe).HasColumnName("importe").HasPrecision(18, 2);
        builder.Property(x => x.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Concepto).HasColumnName("concepto").HasMaxLength(250);
        builder.Property(x => x.VentaId).HasColumnName("venta_id");
        builder.Property(x => x.DevolucionVentaId).HasColumnName("devolucion_venta_id");
        builder.Property(x => x.LiquidacionComisionId).HasColumnName("liquidacion_comision_id");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasOne<Caja>().WithMany().HasForeignKey(x => x.CajaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Venta>().WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DevolucionVenta>().WithMany().HasForeignKey(x => x.DevolucionVentaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LiquidacionComision>().WithMany().HasForeignKey(x => x.LiquidacionComisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.CajaId, x.FechaCreacionUtc }).HasDatabaseName("ix_movimientos_caja_fecha");
    }
}
