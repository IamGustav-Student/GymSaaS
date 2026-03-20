using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Web.Services
{
    public class SubscriptionCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SubscriptionCleanupService> _logger;

        public SubscriptionCleanupService(IServiceProvider serviceProvider, ILogger<SubscriptionCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SubscriptionCleanupService iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                        
                        // 1. Desactivar membresías de alumnos vencidas
                        await DesactivarMembresiasVencidas(context, stoppingToken);

                        // 2. Suspender gimnasios con Trial vencido
                        await SuspenderTenantsVencidos(context, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el ciclo de SubscriptionCleanupService");
                }

                // Esperar 24 horas hasta la próxima ejecución (o según configuración)
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task DesactivarMembresiasVencidas(IApplicationDbContext context, CancellationToken ct)
        {
            var hoy = DateTime.UtcNow.Date;
            
            var vencidas = await context.MembresiasSocios
                .IgnoreQueryFilters()
                .Where(m => m.Activa && m.FechaFin.Date < hoy)
                .ToListAsync(ct);

            if (vencidas.Any())
            {
                foreach (var m in vencidas)
                {
                    m.Activa = false;
                    m.Estado = "Vencida";
                }
                await context.SaveChangesAsync(ct);
                _logger.LogInformation("Se desactivaron {Count} membresías de alumnos.", vencidas.Count);
            }
        }

        private async Task SuspenderTenantsVencidos(IApplicationDbContext context, CancellationToken ct)
        {
            var ahora = DateTime.UtcNow;

            var tenantsASuspender = await context.Tenants
                .Where(t => t.Status == SubscriptionStatus.Trial && t.TrialEndsAt < ahora)
                .ToListAsync(ct);

            if (tenantsASuspender.Any())
            {
                foreach (var t in tenantsASuspender)
                {
                    t.Status = SubscriptionStatus.Suspended;
                    t.IsActive = false; // Bloquea acceso general
                    _logger.LogInformation("Gimnasio {TenantCode} suspendido por Trial vencido.", t.Code);
                }
                await context.SaveChangesAsync(ct);
            }
        }
    }
}
