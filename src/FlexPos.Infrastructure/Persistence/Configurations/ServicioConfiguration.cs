using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class ServicioConfiguration : IEntityTypeConfiguration<Servicio>
{
    public void Configure(EntityTypeBuilder<Servicio> builder)
    {
        builder.ToTable("servicios");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(120).IsRequired();
        builder.Property(x => x.NombreNormalizado).HasColumnName("nombre_normalizado").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(500);
        builder.Property(x => x.Categoria).HasColumnName("categoria").HasMaxLength(100);
        builder.Property(x => x.CategoriaNormalizada).HasColumnName("categoria_normalizada").HasMaxLength(100);
        builder.Property(x => x.DuracionMinutos).HasColumnName("duracion_minutos");
        builder.Property(x => x.Activo).HasColumnName("activo");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.OwnsOne(x => x.Precio, precio =>
        {
            precio.Property(x => x.Importe)
                .HasColumnName("precio_importe")
                .HasPrecision(18, 2)
                .IsRequired();
            precio.Property(x => x.CodigoMoneda)
                .HasColumnName("precio_codigo_moneda")
                .HasMaxLength(3)
                .IsRequired();
        });
        builder.Navigation(x => x.Precio).IsRequired();
        builder.HasIndex(x => new { x.Activo, x.NombreNormalizado }).HasDatabaseName("ix_servicios_activo_nombre");
        builder.HasIndex(x => x.NombreNormalizado).HasOperators("text_pattern_ops").HasDatabaseName("ix_servicios_nombre_prefijo");
        builder.HasIndex(x => x.CategoriaNormalizada).HasOperators("text_pattern_ops").HasDatabaseName("ix_servicios_categoria_prefijo");
    }
}
