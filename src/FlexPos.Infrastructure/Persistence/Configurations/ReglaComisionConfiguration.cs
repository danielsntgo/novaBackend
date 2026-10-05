using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class ReglaComisionConfiguration : IEntityTypeConfiguration<ReglaComision>
{
    public void Configure(EntityTypeBuilder<ReglaComision> builder)
    {
        builder.ToTable("reglas_comision", tabla =>
        {
            tabla.HasCheckConstraint("ck_reglas_comision_valor", "valor > 0 AND (tipo <> 'Porcentaje' OR valor <= 100)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.EmpleadoId).HasColumnName("empleado_id");
        builder.Property(x => x.ServicioId).HasColumnName("servicio_id");
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Valor).HasColumnName("valor").HasPrecision(18, 2);
        builder.Property(x => x.Activa).HasColumnName("activa");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasOne<Empleado>().WithMany().HasForeignKey(x => x.EmpleadoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Servicio>().WithMany().HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EmpleadoId, x.ServicioId }).IsUnique().HasDatabaseName("ux_reglas_comision_empleado_servicio");
        builder.HasIndex(x => new { x.EmpleadoId, x.Activa }).HasDatabaseName("ix_reglas_comision_empleado_activa");
    }
}
