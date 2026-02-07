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

        // DbSets Existentes
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Socio> Socios => Set<Socio>();
        public DbSet<TipoMembresia> TiposMembresia => Set<TipoMembresia>();
        public DbSet<MembresiaSocio> MembresiasSocios => Set<MembresiaSocio>();
        public DbSet<Pago> Pagos => Set<Pago>();
        public DbSet<Asistencia> Asistencias => Set<Asistencia>();
        public DbSet<ConfiguracionPago> ConfiguracionesPagos { get; set; }

        // DbSets Fase 4
        public DbSet<Ejercicio> Ejercicios => Set<Ejercicio>();
        public DbSet<Rutina> Rutinas => Set<Rutina>();
        public DbSet<RutinaEjercicio> RutinaEjercicios => Set<RutinaEjercicio>();
        public DbSet<Clase> Clases => Set<Clase>();
        public DbSet<Reserva> Reservas => Set<Reserva>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(builder);

            // ==============================================================================
            // BLINDAJE DE SEGURIDAD MULTITENANT (CORREGIDO)
            // ==============================================================================
            // REGLA DE ORO: Si _currentTenantService.TenantId es NULL, NO devolvemos nada.
            // Se eliminó el operador '||' que causaba el Data Bleed.

            builder.Entity<Usuario>().HasQueryFilter(e =>
                _currentTenantService.TenantId != null && e.TenantId == _currentTenantService.TenantId);

            builder.Entity<Socio>().HasQueryFilter(e =>
                _currentTenantService.TenantId != null && e.TenantId == _currentTenantService.TenantId && !e.IsDeleted);

            builder.Entity<TipoMembresia>().HasQueryFilter(e =>
                _currentTenantService.TenantId != null && e.TenantId == _currentTenantService.TenantId);

            builder.Entity<MembresiaSocio>().HasQueryFilter(e =>
                _currentTenantService.TenantId != null && e.TenantId == _currentTenantService.TenantId);

            builder.Entity<Pago>().HasQueryFilter(e =>
                _currentTenantService.TenantId != null && e.TenantId == _currentTenantService.TenantId);

            builder.Entity<ConfiguracionPago>().HasQueryFilter(e =>
                _currentTenantService.TenantId != null && e.TenantId == _currentTenantService.TenantId);

            builder.Entity<Asistencia>().HasQueryFilter(e =>
                _currentTenantService.TenantId != null && e.TenantId == _currentTenantService.TenantId);

            builder.Entity<Ejercicio>().HasQueryFilter(e =>
                _currentTenantService.TenantId != null && e.TenantId == _currentTenantService.TenantId);

            builder.Entity<Rutina>().HasQueryFilter(e =>
                _currentTenantService.TenantId != null && e.TenantId == _currentTenantService.TenantId);

            builder.Entity<Clase>().HasQueryFilter(e =>
                _currentTenantService.TenantId != null && e.TenantId == _currentTenantService.TenantId);

            builder.Entity<Reserva>().HasQueryFilter(e =>
                _currentTenantService.TenantId != null && e.TenantId == _currentTenantService.TenantId);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Inyección automática de TenantId
            foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>())
            {
                if (entry.State == EntityState.Added)
                {
                    // Si ya tiene TenantId (ej. migración o seed), lo respetamos
                    if (entry.Entity is Usuario usuario && !string.IsNullOrEmpty(usuario.TenantId))
                    {
                        continue;
                    }

                    // SAFETY CHECK: No permitir guardar datos "huérfanos" sin Tenant
                    if (string.IsNullOrEmpty(_currentTenantService.TenantId))
                    {
                        // Opcional: Lanzar excepción si es crítico
                        // throw new InvalidOperationException("No se puede guardar entidad sin contexto de Tenant.");
                    }
                    else
                    {
                        entry.Entity.TenantId = _currentTenantService.TenantId;
                    }
                }
            }

            // Soft Delete para Socios
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