using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.ToTable("proveedores");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(160).IsRequired();
        builder.Property(x => x.NombreNormalizado).HasColumnName("nombre_normalizado").HasMaxLength(160).IsRequired();
        builder.Property(x => x.IdentificacionFiscal).HasColumnName("identificacion_fiscal").HasMaxLength(60);
        builder.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(40);
        builder.Property(x => x.Correo).HasColumnName("correo").HasMaxLength(256);
        builder.Property(x => x.Activo).HasColumnName("activo");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => new { x.Activo, x.NombreNormalizado }).HasDatabaseName("ix_proveedores_activo_nombre");
        builder.HasIndex(x => x.NombreNormalizado).HasOperators("text_pattern_ops").HasDatabaseName("ix_proveedores_nombre_prefijo");
    }
}
