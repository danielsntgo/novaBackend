using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class CajaConfiguration : IEntityTypeConfiguration<Caja>
{
    public void Configure(EntityTypeBuilder<Caja> builder)
    {
        builder.ToTable("cajas", tabla => tabla.HasCheckConstraint(
            "ck_cajas_estado", "estado IN ('Abierta', 'Cerrada')"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.EfectivoApertura).HasColumnName("efectivo_apertura").HasPrecision(18, 2);
        builder.Property(x => x.FechaAperturaUtc).HasColumnName("fecha_apertura_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.UsuarioAperturaId).HasColumnName("usuario_apertura_id");
        builder.Property(x => x.EfectivoEsperadoCierre).HasColumnName("efectivo_esperado_cierre").HasPrecision(18, 2);
        builder.Property(x => x.EfectivoContadoCierre).HasColumnName("efectivo_contado_cierre").HasPrecision(18, 2);
        builder.Property(x => x.DiferenciaCierre).HasColumnName("diferencia_cierre").HasPrecision(18, 2);
        builder.Property(x => x.FechaCierreUtc).HasColumnName("fecha_cierre_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.UsuarioCierreId).HasColumnName("usuario_cierre_id");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => x.Estado).IsUnique().HasFilter("estado = 'Abierta'").HasDatabaseName("ux_cajas_abierta_compartida");
        builder.HasIndex(x => x.FechaAperturaUtc).HasDatabaseName("ix_cajas_fecha_apertura");
    }
}
