using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class LiquidacionComisionConfiguration : IEntityTypeConfiguration<LiquidacionComision>
{
    public void Configure(EntityTypeBuilder<LiquidacionComision> builder)
    {
        builder.ToTable("liquidaciones_comision", tabla =>
            tabla.HasCheckConstraint("ck_liquidaciones_comision_importe", "importe > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.EmpleadoId).HasColumnName("empleado_id");
        builder.Property(x => x.MetodoPagoId).HasColumnName("metodo_pago_id");
        builder.Property(x => x.MetodoPagoNombre).HasColumnName("metodo_pago_nombre").HasMaxLength(80).IsRequired();
        builder.Property(x => x.EsEfectivo).HasColumnName("es_efectivo");
        builder.Property(x => x.Importe).HasColumnName("importe").HasPrecision(18, 2);
        builder.Property(x => x.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Referencia).HasColumnName("referencia").HasMaxLength(120);
        builder.Property(x => x.CajaId).HasColumnName("caja_id");
        builder.Property(x => x.FechaUtc).HasColumnName("fecha_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasOne<Empleado>().WithMany().HasForeignKey(x => x.EmpleadoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MetodoPagoConfigurado>().WithMany().HasForeignKey(x => x.MetodoPagoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Caja>().WithMany().HasForeignKey(x => x.CajaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EmpleadoId, x.FechaUtc }).HasDatabaseName("ix_liquidaciones_comision_empleado_fecha");
    }
}
