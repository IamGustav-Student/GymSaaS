using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Tenants.Commands.SelectPlan
{
    public record SelectPlanCommand(PlanType Plan) : IRequest<string>;

    public class SelectPlanCommandHandler : IRequestHandler<SelectPlanCommand, string>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentTenantService _currentTenantService;
        private readonly IMercadoPagoService _mercadoPagoService;

        public SelectPlanCommandHandler(
            IApplicationDbContext context,
            ICurrentTenantService currentTenantService,
            IMercadoPagoService mercadoPagoService)
        {
            _context = context;
            _currentTenantService = currentTenantService;
            _mercadoPagoService = mercadoPagoService;
        }

        public async Task<string> Handle(SelectPlanCommand request, CancellationToken cancellationToken)
        {
            // 1. Obtener ID y validarlo
            var tenantIdStr = _currentTenantService.TenantId;
            if (string.IsNullOrEmpty(tenantIdStr)) throw new UnauthorizedAccessException();

            if (!int.TryParse(tenantIdStr, out int tenantId))
            {
                throw new InvalidOperationException($"ID de Tenant inválido: {tenantIdStr}");
            }

            // 2. CORRECCIÓN: Buscar por Id (numérico) y usar IgnoreQueryFilters
            // Usamos IgnoreQueryFilters() para evitar conflictos si hay filtros globales activos
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            if (tenant == null) throw new KeyNotFoundException($"Tenant {tenantId} no encontrado.");
            // LOGICA PLAN GRATUITO
            if (request.Plan == PlanType.Free)
            {
                tenant.Plan = PlanType.Free;
                tenant.Status = SubscriptionStatus.Trial;
                tenant.MaxSocios = 50;
                tenant.TrialEndsAt = DateTime.UtcNow.AddDays(14);
                tenant.SubscriptionEndsAt = DateTime.UtcNow.AddDays(14);

                await _context.SaveChangesAsync(cancellationToken);

                return "/Dashboard/Index";
            }

            // LOGICA PLANES DE PAGO
            decimal precio = 0;
            string titulo = "";
            int? limiteSocios = null;

            switch (request.Plan)
            {
                case PlanType.Basic:
                    precio = 15000m; // Ejemplo: 15.000 ARS
                    titulo = "Plan Básico - Gymvo";
                    limiteSocios = 100;
                    break;
                case PlanType.Pro:
                    precio = 35000m; // Ejemplo: 35.000 ARS
                    titulo = "Plan Pro - Gymvo";
                    limiteSocios = 500;
                    break;
                case PlanType.Enterprise:
                    precio = 85000m; // Ejemplo: 85.000 ARS
                    titulo = "Plan Enterprise - Gymvo";
                    limiteSocios = null; // Ilimitado
                    break;
                default:
                    throw new InvalidOperationException("Plan no válido.");
            }

            // Actualizamos intención de compra
            tenant.Plan = request.Plan;
            tenant.MaxSocios = limiteSocios;
            // No cambiamos el estado a Active todavía, esperamos el Webhook de pago
            
            await _context.SaveChangesAsync(cancellationToken);

            // Generar Link de Pago SAAS (Suscripción del Gimnasio)
            var externalRef = $"SUBSCRIPTION|{tenant.Id}";

            // Email del administrador (idealmente sacarlo del usuario actual)
            var emailComprador = "admin-gym@gymvo.com";

            var urlPago = await _mercadoPagoService.CrearPreferenciaSaaS(
                titulo,
                precio,
                emailComprador,
                externalRef
            );

            return urlPago;
        }
    }
}