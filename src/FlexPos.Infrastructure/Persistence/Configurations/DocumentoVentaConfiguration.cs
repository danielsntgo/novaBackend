using FlexPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlexPos.Infrastructure.Persistence.Configurations;

public sealed class DocumentoVentaConfiguration : IEntityTypeConfiguration<DocumentoVenta>
{
    public void Configure(EntityTypeBuilder<DocumentoVenta> builder)
    {
        builder.ToTable("documentos_venta");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.VentaId).HasColumnName("venta_id");
        builder.Property(x => x.TipoDocumento).HasColumnName("tipo_documento").HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Prefijo).HasColumnName("prefijo").HasMaxLength(20);
        builder.Property(x => x.Numero).HasColumnName("numero");
        builder.Property(x => x.NumeroCompleto).HasColumnName("numero_completo").HasMaxLength(50).IsRequired();
        builder.Property(x => x.FechaEmisionUtc).HasColumnName("fecha_emision_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.NombreComercial).HasColumnName("nombre_comercial").HasMaxLength(150).IsRequired();
        builder.Property(x => x.RazonSocial).HasColumnName("razon_social").HasMaxLength(180);
        builder.Property(x => x.IdentificacionFiscal).HasColumnName("identificacion_fiscal").HasMaxLength(40);
        builder.Property(x => x.Direccion).HasColumnName("direccion").HasMaxLength(250);
        builder.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(40);
        builder.Property(x => x.Correo).HasColumnName("correo").HasMaxLength(256);
        builder.Property(x => x.ClienteNombre).HasColumnName("cliente_nombre").HasMaxLength(150);
        builder.Property(x => x.ClienteDocumento).HasColumnName("cliente_documento").HasMaxLength(50);
        builder.Property(x => x.CodigoMoneda).HasColumnName("codigo_moneda").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(18, 2);
        builder.Property(x => x.Descuentos).HasColumnName("descuentos").HasPrecision(18, 2);
        builder.Property(x => x.Impuestos).HasColumnName("impuestos").HasPrecision(18, 2);
        builder.Property(x => x.Total).HasColumnName("total").HasPrecision(18, 2);
        builder.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        builder.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => new { x.VentaId, x.TipoDocumento }).IsUnique().HasDatabaseName("ux_documentos_venta_tipo");
        builder.HasIndex(x => new { x.TipoDocumento, x.NumeroCompleto }).IsUnique().HasDatabaseName("ux_documentos_venta_numero");
    }
}
