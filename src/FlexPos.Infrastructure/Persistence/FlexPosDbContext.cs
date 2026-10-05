using FlexPos.Application.Interfaces;
using FlexPos.Domain.Abstractions;
using FlexPos.Domain.Entities;
using FlexPos.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlexPos.Infrastructure.Persistence;

public sealed class FlexPosDbContext(
    DbContextOptions<FlexPosDbContext> options,
    IUsuarioActual? usuarioActual = null)
    : IdentityDbContext<UsuarioIdentidad, RolIdentidad, Guid>(options)
{
    public DbSet<TokenRenovacion> TokensRenovacion => Set<TokenRenovacion>();
    public DbSet<ConfiguracionNegocio> ConfiguracionesNegocio => Set<ConfiguracionNegocio>();
    public DbSet<ImpuestoConfigurado> ImpuestosConfigurados => Set<ImpuestoConfigurado>();
    public DbSet<MetodoPagoConfigurado> MetodosPagoConfigurados => Set<MetodoPagoConfigurado>();
    public DbSet<NumeracionDocumento> NumeracionesDocumento => Set<NumeracionDocumento>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<ArticuloInventario> ArticulosInventario => Set<ArticuloInventario>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<DetalleCompra> DetallesCompra => Set<DetalleCompra>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<Caja> Cajas => Set<Caja>();
    public DbSet<MovimientoCaja> MovimientosCaja => Set<MovimientoCaja>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
    public DbSet<PagoVenta> PagosVenta => Set<PagoVenta>();
    public DbSet<DocumentoVenta> DocumentosVenta => Set<DocumentoVenta>();
    public DbSet<DevolucionVenta> DevolucionesVenta => Set<DevolucionVenta>();
    public DbSet<DetalleDevolucionVenta> DetallesDevolucionVenta => Set<DetalleDevolucionVenta>();
    public DbSet<PagoDevolucionVenta> PagosDevolucionVenta => Set<PagoDevolucionVenta>();
    public DbSet<ReglaComision> ReglasComision => Set<ReglaComision>();
    public DbSet<Comision> Comisiones => Set<Comision>();
    public DbSet<LiquidacionComision> LiquidacionesComision => Set<LiquidacionComision>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("flexpos");
        ConfigurarIdentity(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlexPosDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AuditarCambios();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        AuditarCambios();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private static void ConfigurarIdentity(ModelBuilder modelBuilder)
    {
        var usuarios = modelBuilder.Entity<UsuarioIdentidad>();
        usuarios.ToTable("usuarios");
        usuarios.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        usuarios.Property(x => x.UserName).HasColumnName("nombre_usuario");
        usuarios.Property(x => x.NormalizedUserName).HasColumnName("nombre_usuario_normalizado");
        usuarios.Property(x => x.Email).HasColumnName("correo");
        usuarios.Property(x => x.NormalizedEmail).HasColumnName("correo_normalizado");
        usuarios.Property(x => x.EmailConfirmed).HasColumnName("correo_confirmado");
        usuarios.Property(x => x.PasswordHash).HasColumnName("hash_contrasena");
        usuarios.Property(x => x.SecurityStamp).HasColumnName("sello_seguridad");
        usuarios.Property(x => x.ConcurrencyStamp).HasColumnName("sello_concurrencia");
        usuarios.Property(x => x.PhoneNumber).HasColumnName("telefono");
        usuarios.Property(x => x.PhoneNumberConfirmed).HasColumnName("telefono_confirmado");
        usuarios.Property(x => x.TwoFactorEnabled).HasColumnName("doble_factor_habilitado");
        usuarios.Property(x => x.LockoutEnd).HasColumnName("fin_bloqueo_utc").HasColumnType("timestamp with time zone");
        usuarios.Property(x => x.LockoutEnabled).HasColumnName("bloqueo_habilitado");
        usuarios.Property(x => x.AccessFailedCount).HasColumnName("intentos_fallidos");
        usuarios.Property(x => x.Activo).HasColumnName("activo");
        usuarios.Property(x => x.CambioContrasenaObligatorio).HasColumnName("cambio_contrasena_obligatorio");
        usuarios.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        usuarios.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        usuarios.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        usuarios.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        usuarios.Property(x => x.Version).IsRowVersion();
        usuarios.HasIndex(x => x.NormalizedEmail).HasDatabaseName("ix_usuarios_correo_normalizado");

        var roles = modelBuilder.Entity<RolIdentidad>();
        roles.ToTable("roles");
        roles.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        roles.Property(x => x.Name).HasColumnName("nombre");
        roles.Property(x => x.NormalizedName).HasColumnName("nombre_normalizado");
        roles.Property(x => x.ConcurrencyStamp).HasColumnName("sello_concurrencia");
        roles.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").HasColumnType("timestamp with time zone");
        roles.Property(x => x.FechaModificacionUtc).HasColumnName("fecha_modificacion_utc").HasColumnType("timestamp with time zone");
        roles.Property(x => x.CreadoPorId).HasColumnName("creado_por_id");
        roles.Property(x => x.ModificadoPorId).HasColumnName("modificado_por_id");
        roles.Property(x => x.Version).IsRowVersion();

        var usuariosRoles = modelBuilder.Entity<IdentityUserRole<Guid>>();
        usuariosRoles.ToTable("usuarios_roles");
        usuariosRoles.Property(x => x.UserId).HasColumnName("usuario_id");
        usuariosRoles.Property(x => x.RoleId).HasColumnName("rol_id");

        var reclamacionesUsuario = modelBuilder.Entity<IdentityUserClaim<Guid>>();
        reclamacionesUsuario.ToTable("reclamaciones_usuario");
        reclamacionesUsuario.Property(x => x.Id).HasColumnName("id");
        reclamacionesUsuario.Property(x => x.UserId).HasColumnName("usuario_id");
        reclamacionesUsuario.Property(x => x.ClaimType).HasColumnName("tipo");
        reclamacionesUsuario.Property(x => x.ClaimValue).HasColumnName("valor");

        var reclamacionesRol = modelBuilder.Entity<IdentityRoleClaim<Guid>>();
        reclamacionesRol.ToTable("reclamaciones_rol");
        reclamacionesRol.Property(x => x.Id).HasColumnName("id");
        reclamacionesRol.Property(x => x.RoleId).HasColumnName("rol_id");
        reclamacionesRol.Property(x => x.ClaimType).HasColumnName("tipo");
        reclamacionesRol.Property(x => x.ClaimValue).HasColumnName("valor");

        var iniciosExternos = modelBuilder.Entity<IdentityUserLogin<Guid>>();
        iniciosExternos.ToTable("inicios_sesion_externos");
        iniciosExternos.Property(x => x.LoginProvider).HasColumnName("proveedor");
        iniciosExternos.Property(x => x.ProviderKey).HasColumnName("clave_proveedor");
        iniciosExternos.Property(x => x.ProviderDisplayName).HasColumnName("nombre_proveedor");
        iniciosExternos.Property(x => x.UserId).HasColumnName("usuario_id");

        var tokensUsuario = modelBuilder.Entity<IdentityUserToken<Guid>>();
        tokensUsuario.ToTable("tokens_usuario");
        tokensUsuario.Property(x => x.UserId).HasColumnName("usuario_id");
        tokensUsuario.Property(x => x.LoginProvider).HasColumnName("proveedor");
        tokensUsuario.Property(x => x.Name).HasColumnName("nombre");
        tokensUsuario.Property(x => x.Value).HasColumnName("valor");
    }

    private void AuditarCambios()
    {
        ChangeTracker.DetectChanges();
        var fechaUtc = DateTimeOffset.UtcNow;
        var usuarioId = usuarioActual?.ObtenerId();

        foreach (var entrada in ChangeTracker.Entries<IEntidadAuditable>())
        {
            if (entrada.State == EntityState.Added)
            {
                entrada.Entity.RegistrarCreacion(fechaUtc, usuarioId);
            }
            else if (entrada.State == EntityState.Modified)
            {
                entrada.Entity.RegistrarModificacion(fechaUtc, usuarioId);
            }
        }
    }
}
