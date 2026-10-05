using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class CitaConfiguration : IEntityTypeConfiguration<Cita>
{
    public void Configure(EntityTypeBuilder<Cita> builder)
    {
        builder.ToTable("citas", tabla =>
        {
            tabla.HasCheckConstraint("ck_citas_duracion", "duracion_minutos BETWEEN 1 AND 1440");
            tabla.HasCheckConstraint(
                "ck_citas_estado",
                "estado IN ('Pendiente', 'Confirmada', 'Atendida', 'Cancelada', 'NoAsistio')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ClienteId).HasColumnName("cliente_id");
        builder.Property(x => x.ServicioId).HasColumnName("servicio_id");
        builder.Property(x => x.EmpleadoId).HasColumnName("empleado_id");
        builder.Property(x => x.InicioUtc).HasColumnName("fecha_inicio_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.DuracionMinutos).HasColumnName("duracion_minutos");
        builder.Property(x => x.FinUtc)
            .HasColumnName("fecha_fin_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(x => x.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Servicio).WithMany().HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Empleado).WithMany().HasForeignKey(x => x.EmpleadoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EmpleadoId, x.InicioUtc }).HasDatabaseName("ix_citas_empleado_inicio");
        builder.HasIndex(x => new { x.ClienteId, x.InicioUtc }).HasDatabaseName("ix_citas_cliente_inicio");
        builder.HasIndex(x => new { x.Estado, x.InicioUtc }).HasDatabaseName("ix_citas_estado_inicio");
    }
}
