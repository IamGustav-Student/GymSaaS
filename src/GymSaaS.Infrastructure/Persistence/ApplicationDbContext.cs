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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(builder);

            // CONFIGURACIÓN NIVEL PRO: Dinero
            // SQL Server requiere definir precisión para decimales o lanza warnings
            builder.Entity<TipoMembresia>().Property(p => p.Precio).HasPrecision(18, 2);
            builder.Entity<MembresiaSocio>().Property(p => p.PrecioPagado).HasPrecision(18, 2);
            builder.Entity<Pago>().Property(p => p.Monto).HasPrecision(18, 2);

            // SEGURIDAD MULTI-TENANT (GLOBAL QUERY FILTERS)
            // Esto inyecta automáticamente "WHERE TenantId = 'xyz'" en TODAS las consultas.
            builder.Entity<Usuario>().HasQueryFilter(e => e.TenantId == _currentTenantService.TenantId);
            builder.Entity<Socio>().HasQueryFilter(e => e.TenantId == _currentTenantService.TenantId);
            builder.Entity<TipoMembresia>().HasQueryFilter(e => e.TenantId == _currentTenantService.TenantId);
            builder.Entity<MembresiaSocio>().HasQueryFilter(e => e.TenantId == _currentTenantService.TenantId);
            builder.Entity<Pago>().HasQueryFilter(e => e.TenantId == _currentTenantService.TenantId);
            builder.Entity<Asistencia>().HasQueryFilter(e => e.TenantId == _currentTenantService.TenantId);

            // SOFT DELETE GLOBAL QUERY FILTER
            builder.Entity<Socio>().HasQueryFilter(s => !s.IsDeleted);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Inyección automática de TenantId (Ya lo tenías)
            foreach (var entry in ChangeTracker.Entries<Socio>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.TenantId = _currentTenantService.TenantId;
                }
            }

            // --- NUEVO: LÓGICA SOFT DELETE ---
            foreach (var entry in ChangeTracker.Entries<Socio>())
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified; // Cambiar Borrado a Modificado
                    entry.Entity.IsDeleted = true;      // Marcar bandera
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}