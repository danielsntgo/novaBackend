using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class EmpleadoConfiguration : IEntityTypeConfiguration<Empleado>
{
    public void Configure(EntityTypeBuilder<Empleado> builder)
    {
        builder.ToTable("empleados");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
        builder.Property(x => x.NombreNormalizado).HasColumnName("nombre_normalizado").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Cargo).HasColumnName("cargo").HasMaxLength(100).IsRequired();
        builder.Property(x => x.CargoNormalizado).HasColumnName("cargo_normalizado").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Documento).HasColumnName("documento").HasMaxLength(50);
        builder.Property(x => x.DocumentoNormalizado).HasColumnName("documento_normalizado").HasMaxLength(50);
        builder.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(40);
        builder.Property(x => x.TelefonoNormalizado).HasColumnName("telefono_normalizado").HasMaxLength(40);
        builder.Property(x => x.Correo).HasColumnName("correo").HasMaxLength(256);
        builder.Property(x => x.CorreoNormalizado).HasColumnName("correo_normalizado").HasMaxLength(256);
        builder.Property(x => x.Activo).HasColumnName("activo");
        builder.Property(x => x.RevisionHorario).HasColumnName("revision_horario");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => new { x.Activo, x.NombreNormalizado }).HasDatabaseName("ix_empleados_activo_nombre");
        builder.HasIndex(x => x.NombreNormalizado).HasOperators("text_pattern_ops").HasDatabaseName("ix_empleados_nombre_prefijo");
        builder.HasIndex(x => x.CargoNormalizado).HasOperators("text_pattern_ops").HasDatabaseName("ix_empleados_cargo_prefijo");
        builder.HasIndex(x => x.DocumentoNormalizado).HasOperators("text_pattern_ops").HasDatabaseName("ix_empleados_documento_prefijo");
        builder.HasIndex(x => x.TelefonoNormalizado).HasOperators("text_pattern_ops").HasDatabaseName("ix_empleados_telefono_prefijo");
        builder.HasIndex(x => x.CorreoNormalizado).HasOperators("text_pattern_ops").HasDatabaseName("ix_empleados_correo_prefijo");

        builder.OwnsMany(x => x.HorariosSemanales, horarios =>
        {
            horarios.ToTable("horarios_semanales_empleado");
            horarios.WithOwner().HasForeignKey("empleado_id");
            horarios.Property<Guid>("empleado_id").HasColumnName("empleado_id");
            horarios.Property(x => x.DiaSemana).HasColumnName("dia_semana").HasConversion<int>();
            horarios.Property(x => x.HoraInicio).HasColumnName("hora_inicio").HasColumnType("time without time zone");
            horarios.Property(x => x.HoraFin).HasColumnName("hora_fin").HasColumnType("time without time zone");
            horarios.HasKey("empleado_id", "DiaSemana", "HoraInicio");
            horarios.HasIndex("empleado_id", "DiaSemana", "HoraFin")
                .HasDatabaseName("ix_horarios_empleado_dia_fin");
        });
        builder.Navigation(x => x.HorariosSemanales)
            .HasField("_horariosSemanales")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
