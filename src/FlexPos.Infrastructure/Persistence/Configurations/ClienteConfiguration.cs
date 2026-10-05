using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
        builder.Property(x => x.NombreNormalizado).HasColumnName("nombre_normalizado").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Documento).HasColumnName("documento").HasMaxLength(50);
        builder.Property(x => x.DocumentoNormalizado).HasColumnName("documento_normalizado").HasMaxLength(50);
        builder.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(40);
        builder.Property(x => x.TelefonoNormalizado).HasColumnName("telefono_normalizado").HasMaxLength(40);
        builder.Property(x => x.Correo).HasColumnName("correo").HasMaxLength(256);
        builder.Property(x => x.CorreoNormalizado).HasColumnName("correo_normalizado").HasMaxLength(256);
        builder.Property(x => x.Activo).HasColumnName("activo");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => new { x.Activo, x.NombreNormalizado }).HasDatabaseName("ix_clientes_activo_nombre");
        builder.HasIndex(x => x.NombreNormalizado).HasOperators("text_pattern_ops").HasDatabaseName("ix_clientes_nombre_prefijo");
        builder.HasIndex(x => x.DocumentoNormalizado).HasOperators("text_pattern_ops").HasDatabaseName("ix_clientes_documento_prefijo");
        builder.HasIndex(x => x.TelefonoNormalizado).HasOperators("text_pattern_ops").HasDatabaseName("ix_clientes_telefono_prefijo");
        builder.HasIndex(x => x.CorreoNormalizado).HasOperators("text_pattern_ops").HasDatabaseName("ix_clientes_correo_prefijo");
    }
}
