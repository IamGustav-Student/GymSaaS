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

            // CONFIGURACIÓN NIVEL PRO: Dinero
            builder.Entity<TipoMembresia>().Property(p => p.Precio).HasPrecision(18, 2);
            builder.Entity<MembresiaSocio>().Property(p => p.PrecioPagado).HasPrecision(18, 2);
            builder.Entity<Pago>().Property(p => p.Monto).HasPrecision(18, 2);

            // SEGURIDAD MULTI-TENANT (GLOBAL QUERY FILTERS)
            builder.Entity<Usuario>().HasQueryFilter(e => e.TenantId == _currentTenantService.TenantId);

            // --- CORRECCIÓN CRÍTICA AQUÍ ---
            // Fusionamos Tenant + SoftDelete en una sola línea con &&
            builder.Entity<Socio>().HasQueryFilter(s => s.TenantId == _currentTenantService.TenantId && !s.IsDeleted);

            builder.Entity<TipoMembresia>().HasQueryFilter(e => e.TenantId == _currentTenantService.TenantId);
            builder.Entity<MembresiaSocio>().HasQueryFilter(e => e.TenantId == _currentTenantService.TenantId);
            builder.Entity<Pago>().HasQueryFilter(e => e.TenantId == _currentTenantService.TenantId);
            builder.Entity<Asistencia>().HasQueryFilter(e => e.TenantId == _currentTenantService.TenantId);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // 1. INYECCIÓN DE TENANT (Para TODAS las entidades)
            foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>())
            {
                if (entry.State == EntityState.Added)
                {
                    // Respetar asignación manual (Usuario Admin)
                    if (entry.Entity is Usuario usuario && !string.IsNullOrEmpty(usuario.TenantId))
                    {
                        continue;
                    }

                    // CORRECCIÓN: Aseguramos que no sea nulo usando '?? string.Empty' 
                    // o simplemente '!' si estamos seguros de que el servicio siempre responde.
                    entry.Entity.TenantId = _currentTenantService.TenantId ?? string.Empty;
                }
            }

            // 2. LÓGICA SOFT DELETE (Solo Socios)
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