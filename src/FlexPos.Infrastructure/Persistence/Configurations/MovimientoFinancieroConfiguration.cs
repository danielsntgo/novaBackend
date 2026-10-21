using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class MovimientoFinancieroConfiguration : IEntityTypeConfiguration<MovimientoFinanciero>
{
    public void Configure(EntityTypeBuilder<MovimientoFinanciero> builder)
    {
        builder.ToTable("movimientos_financieros", tabla =>
            tabla.HasCheckConstraint("ck_movimientos_financieros_importe", "importe > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CuentaFinancieraId).HasColumnName("cuenta_financiera_id");
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Sentido).HasColumnName("sentido").HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.Importe).HasColumnName("importe").HasPrecision(18, 2);
        builder.Property(x => x.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Concepto).HasColumnName("concepto").HasMaxLength(250);
        builder.Property(x => x.FechaUtc).HasColumnName("fecha_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CajaId).HasColumnName("caja_id");
        builder.Property(x => x.MetodoPagoId).HasColumnName("metodo_pago_id");
        builder.Property(x => x.VentaId).HasColumnName("venta_id");
        builder.Property(x => x.DevolucionVentaId).HasColumnName("devolucion_venta_id");
        builder.Property(x => x.CompraId).HasColumnName("compra_id");
        builder.Property(x => x.LiquidacionComisionId).HasColumnName("liquidacion_comision_id");
        builder.Property(x => x.TransferenciaFinancieraId).HasColumnName("transferencia_financiera_id");
        builder.Property(x => x.Migrado).HasColumnName("migrado");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasOne<CuentaFinanciera>().WithMany().HasForeignKey(x => x.CuentaFinancieraId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Caja>().WithMany().HasForeignKey(x => x.CajaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MetodoPagoConfigurado>().WithMany().HasForeignKey(x => x.MetodoPagoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Venta>().WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DevolucionVenta>().WithMany().HasForeignKey(x => x.DevolucionVentaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Compra>().WithMany().HasForeignKey(x => x.CompraId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LiquidacionComision>().WithMany().HasForeignKey(x => x.LiquidacionComisionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TransferenciaFinanciera>().WithMany().HasForeignKey(x => x.TransferenciaFinancieraId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.CuentaFinancieraId, x.FechaUtc, x.Id })
            .HasDatabaseName("ix_movimientos_financieros_cuenta_fecha");
        builder.HasIndex(x => x.CajaId).HasDatabaseName("ix_movimientos_financieros_caja");
        builder.HasIndex(x => x.VentaId).HasDatabaseName("ix_movimientos_financieros_venta");
        builder.HasIndex(x => x.DevolucionVentaId).HasDatabaseName("ix_movimientos_financieros_devolucion");
        builder.HasIndex(x => x.LiquidacionComisionId).HasDatabaseName("ix_movimientos_financieros_liquidacion");
    }
}
