using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class MetodoPagoConfiguradoConfiguration : IEntityTypeConfiguration<MetodoPagoConfigurado>
{
    public void Configure(EntityTypeBuilder<MetodoPagoConfigurado> builder)
    {
        builder.ToTable("metodos_pago");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ConfiguracionNegocioId).HasColumnName("configuracion_negocio_id");
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(80).IsRequired();
        builder.Property(x => x.NombreNormalizado).HasColumnName("nombre_normalizado").HasMaxLength(80).IsRequired();
        builder.Property(x => x.RequiereReferencia).HasColumnName("requiere_referencia");
        builder.Property(x => x.EsEfectivo).HasColumnName("es_efectivo");
        builder.Property(x => x.Activo).HasColumnName("activo");
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => new { x.ConfiguracionNegocioId, x.NombreNormalizado })
            .IsUnique()
            .HasDatabaseName("ux_metodos_pago_configuracion_nombre");
        builder.HasIndex(x => x.ConfiguracionNegocioId)
            .IsUnique()
            .HasFilter("es_efectivo = true")
            .HasDatabaseName("ux_metodos_pago_unico_efectivo");
        builder.HasOne<ConfiguracionNegocio>()
            .WithMany()
            .HasForeignKey(x => x.ConfiguracionNegocioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
