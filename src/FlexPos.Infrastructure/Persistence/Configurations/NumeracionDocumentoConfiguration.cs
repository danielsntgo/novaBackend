using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class NumeracionDocumentoConfiguration : IEntityTypeConfiguration<NumeracionDocumento>
{
    public void Configure(EntityTypeBuilder<NumeracionDocumento> builder)
    {
        builder.ToTable("numeraciones_documento");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ConfiguracionNegocioId).HasColumnName("configuracion_negocio_id");
        builder.Property(x => x.TipoDocumento).HasColumnName("tipo_documento").HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Prefijo).HasColumnName("prefijo").HasMaxLength(20);
        builder.Property(x => x.SiguienteNumero).HasColumnName("siguiente_numero");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => new { x.ConfiguracionNegocioId, x.TipoDocumento })
            .IsUnique()
            .HasDatabaseName("ux_numeraciones_documento_tipo");
        builder.HasOne<ConfiguracionNegocio>()
            .WithMany()
            .HasForeignKey(x => x.ConfiguracionNegocioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
