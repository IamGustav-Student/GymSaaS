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
            var tenantId = _currentTenantService.TenantId;
            if (string.IsNullOrEmpty(tenantId)) throw new UnauthorizedAccessException();

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Code == tenantId, cancellationToken);
            if (tenant == null) throw new KeyNotFoundException($"Tenant {tenantId} no encontrado.");

            // LOGICA PLAN GRATUITO
            if (request.Plan == PlanType.PruebaGratuita)
            {
                tenant.Plan = PlanType.PruebaGratuita;
                tenant.Status = SubscriptionStatus.Active; // Activación inmediata
                tenant.MaxSocios = 50;
                tenant.TrialEndsAt = DateTime.UtcNow.AddDays(30);
                tenant.SubscriptionEndsAt = DateTime.UtcNow.AddDays(30);

                await _context.SaveChangesAsync(cancellationToken);

                // Redirección interna al Dashboard
                return "/Dashboard/Index";
            }

            // LOGICA PLANES DE PAGO
            decimal precio = 0;
            string titulo = "";
            int? limiteSocios = null;

            switch (request.Plan)
            {
                case PlanType.Basico:
                    precio = 100000m;
                    titulo = "Suscripción Gymvo Básico (Mensual)";
                    limiteSocios = 50;
                    break;
                case PlanType.Premium:
                    precio = 180000m;
                    titulo = "Suscripción Gymvo Premium (Ilimitado)";
                    limiteSocios = null; // Ilimitado
                    break;
                default:
                    throw new InvalidOperationException("Plan no válido.");
            }

            // Actualizamos intención de compra
            tenant.Plan = request.Plan;
            tenant.MaxSocios = limiteSocios;
            tenant.Status = SubscriptionStatus.Inactive; // Inactivo hasta que pague

            await _context.SaveChangesAsync(cancellationToken);

            // Generar Link de Pago SAAS (Usa credenciales maestras)
            // Usamos un External Reference único para conciliar el Webhook después
            var externalRef = $"SAAS-{tenant.Code}-{request.Plan}-{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Nota: Aquí deberíamos obtener el email del admin del tenant, usamos uno genérico por simplicidad técnica momentánea
            // o idealmente inyectar ICurrentUserService para sacar el email del usuario logueado.
            var emailComprador = "admin-gym@gymvo.com";

            var urlPago = await _mercadoPagoService.CrearPreferenciaSaaS(
                titulo,
                precio,
                emailComprador,
                externalRef
            );

            return urlPago; // URL externa de MercadoPago
        }
    }
}