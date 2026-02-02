using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Common;
using GymSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace GymSaaS.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        private readonly ICurrentTenantService _currentTenantService;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ICurrentTenantService currentTenantService)
            : base(options)
        {
            _currentTenantService = currentTenantService;
        }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Socio> Socios => Set<Socio>();
        public DbSet<TipoMembresia> TiposMembresia => Set<TipoMembresia>();
        public DbSet<MembresiaSocio> MembresiasSocios => Set<MembresiaSocio>();
        public DbSet<Pago> Pagos => Set<Pago>();
        public DbSet<Asistencia> Asistencias => Set<Asistencia>();
        public DbSet<ConfiguracionPago> ConfiguracionesPagos { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(builder);

            // CONFIGURACIÓN PRO
            builder.Entity<TipoMembresia>().Property(p => p.Precio).HasPrecision(18, 2);
            builder.Entity<MembresiaSocio>().Property(p => p.PrecioPagado).HasPrecision(18, 2);
            builder.Entity<Pago>().Property(p => p.Monto).HasPrecision(18, 2);

            builder.Entity<Tenant>()
                .HasIndex(t => t.Code)
                .IsUnique();

            // =========================================================================
            // CORRECCIÓN CRÍTICA: LÓGICA DE FILTROS "PERMISIVA"
            // =========================================================================
            // Si _currentTenantService.TenantId es NULL (ej: Registro, Login, Landing),
            // el filtro devuelve TRUE (muestra todo). Si tiene valor, filtra por ese valor.

            // 1. Usuarios: Permitir verlos si no hay tenant definido (para login)
            builder.Entity<Usuario>().HasQueryFilter(e =>
                _currentTenantService.TenantId == null || e.TenantId == _currentTenantService.TenantId);

            // 2. Socios: Requieren Tenant Y no estar borrados
            builder.Entity<Socio>().HasQueryFilter(s =>
                (_currentTenantService.TenantId == null || s.TenantId == _currentTenantService.TenantId)
                && !s.IsDeleted);

            // 3. Entidades de Negocio (Mismo patrón)
            builder.Entity<TipoMembresia>().HasQueryFilter(e =>
                _currentTenantService.TenantId == null || e.TenantId == _currentTenantService.TenantId);

            builder.Entity<MembresiaSocio>().HasQueryFilter(e =>
                _currentTenantService.TenantId == null || e.TenantId == _currentTenantService.TenantId);

            builder.Entity<Pago>().HasQueryFilter(e =>
                _currentTenantService.TenantId == null || e.TenantId == _currentTenantService.TenantId);

            builder.Entity<Asistencia>().HasQueryFilter(e =>
                _currentTenantService.TenantId == null || e.TenantId == _currentTenantService.TenantId);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>())
            {
                if (entry.State == EntityState.Added)
                {
                    // Respetar asignación manual (Caso RegisterTenant)
                    if (entry.Entity is Usuario usuario && !string.IsNullOrEmpty(usuario.TenantId))
                    {
                        continue;
                    }

                    // Asignación automática solo si hay tenant logueado
                    if (!string.IsNullOrEmpty(_currentTenantService.TenantId))
                    {
                        entry.Entity.TenantId = _currentTenantService.TenantId;
                    }
                    // Si es null (Registro), confiamos en que el CommandHandler lo asignó manualmente.
                }
            }

            foreach (var entry in ChangeTracker.Entries<Socio>())
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}