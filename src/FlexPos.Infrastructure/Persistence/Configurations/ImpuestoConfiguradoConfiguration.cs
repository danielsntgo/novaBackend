using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class ImpuestoConfiguradoConfiguration : IEntityTypeConfiguration<ImpuestoConfigurado>
{
    public void Configure(EntityTypeBuilder<ImpuestoConfigurado> builder)
    {
        builder.ToTable("impuestos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ConfiguracionNegocioId).HasColumnName("configuracion_negocio_id");
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        builder.Property(x => x.NombreNormalizado).HasColumnName("nombre_normalizado").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Porcentaje).HasColumnName("porcentaje").HasPrecision(5, 2);
        builder.Property(x => x.Activo).HasColumnName("activo");
        ConfigurarAuditoria(builder);
        builder.HasIndex(x => new { x.ConfiguracionNegocioId, x.NombreNormalizado })
            .IsUnique()
            .HasDatabaseName("ux_impuestos_configuracion_nombre");
        builder.HasOne<ConfiguracionNegocio>()
            .WithMany()
            .HasForeignKey(x => x.ConfiguracionNegocioId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurarAuditoria(EntityTypeBuilder<ImpuestoConfigurado> builder)
    {
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
    }
}
