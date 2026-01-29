using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Pagos.Commands.ProcesarWebhook
{
    // Solo necesitamos el ID del pago que nos manda MP
    public record ProcesarWebhookCommand(string PaymentId) : IRequest<bool>;

    public class ProcesarWebhookCommandHandler : IRequestHandler<ProcesarWebhookCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMercadoPagoService _mpService;

        public ProcesarWebhookCommandHandler(IApplicationDbContext context, IMercadoPagoService mpService)
        {
            _context = context;
            _mpService = mpService;
        }

        public async Task<bool> Handle(ProcesarWebhookCommand request, CancellationToken cancellationToken)
        {
            // 1. Verificamos con MP que el pago sea real y esté Aprobado
            var estado = await _mpService.ObtenerEstadoPagoAsync(request.PaymentId);

            if (estado != "approved")
            {
                return false; // Ignoramos pagos pendientes o rechazados
            }

            // 2. Recuperamos la etiqueta (MembresiaId) que pegamos en el paso 1
            var externalRef = await _mpService.ObtenerExternalReferenceAsync(request.PaymentId);

            if (!int.TryParse(externalRef, out int membresiaId))
            {
                return false; // No tiene etiqueta válida
            }

            // 3. Buscamos la membresía en la BD (Ignoramos filtro Tenant por ser proceso de fondo)
            var membresia = await _context.MembresiasSocios
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == membresiaId, cancellationToken);

            if (membresia == null) return false;

            // 4. Si ya estaba pagada, no hacemos nada (idempotencia)
            if (membresia.Activa) return true;

            // 5. ACTIVAMOS LA MEMBRESÍA 🚀
            membresia.Activa = true;

            // 6. Registramos el pago en el historial (Ahora sí funciona porque Pago.cs tiene los campos)
            var nuevoPago = new Pago
            {
                SocioId = membresia.SocioId,
                MembresiaSocioId = membresia.Id,
                FechaPago = DateTime.Now,
                Monto = membresia.PrecioPagado,
                MetodoPago = "MercadoPago",
                TenantId = membresia.TenantId // Mantenemos la integridad del Tenant
            };

            _context.Pagos.Add(nuevoPago);

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}