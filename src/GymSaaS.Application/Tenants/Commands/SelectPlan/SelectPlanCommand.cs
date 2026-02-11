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
            if (request.Plan == PlanType.PruebaGratuita)
            {
                // Si el usuario elige volver al plan gratuito (generalmente no permitido si ya lo usó, pero mantenemos tu lógica)
                tenant.Plan = PlanType.PruebaGratuita;
                tenant.Status = SubscriptionStatus.Active;
                tenant.MaxSocios = 50;
                // Nota: Aquí podrías validar si ya consumió su prueba
                tenant.TrialEndsAt = DateTime.UtcNow.AddDays(30);
                tenant.SubscriptionEndsAt = DateTime.UtcNow.AddDays(30);

                await _context.SaveChangesAsync(cancellationToken);

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
            // No cambiamos el estado a Active todavía, esperamos el Webhook de pago
            tenant.Plan = request.Plan;
            // Opcional: Podrías mantener el límite anterior hasta que pague para no bloquearlo
            tenant.MaxSocios = limiteSocios;
            tenant.Status = SubscriptionStatus.Inactive; // Pendiente de Pago

            await _context.SaveChangesAsync(cancellationToken);

            // Generar Link de Pago SAAS
            // External Reference: "SUBSCRIPTION|{Id}" para que el Webhook sepa qué tenant actualizar
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