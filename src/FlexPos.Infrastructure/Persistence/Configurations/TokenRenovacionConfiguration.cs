using FlexPos.Domain.Entities;
using FlexPos.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class TokenRenovacionConfiguration : IEntityTypeConfiguration<TokenRenovacion>
{
    public void Configure(EntityTypeBuilder<TokenRenovacion> builder)
    {
        builder.ToTable("tokens_renovacion");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(x => x.Hash).HasColumnName("hash").HasMaxLength(64).IsRequired();
        builder.Property(x => x.SelloSeguridad).HasColumnName("sello_seguridad").HasMaxLength(256).IsRequired();
        builder.Property(x => x.CreadoUtc).HasColumnName("creado_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.VenceUtc).HasColumnName("vence_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.RevocadoUtc).HasColumnName("revocado_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ReemplazadoPorHash).HasColumnName("reemplazado_por_hash").HasMaxLength(64);
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => x.Hash).IsUnique().HasDatabaseName("ix_tokens_renovacion_hash");
        builder.HasIndex(x => new { x.UsuarioId, x.RevocadoUtc }).HasDatabaseName("ix_tokens_renovacion_usuario_activos");
        builder.HasOne<UsuarioIdentidad>()
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
