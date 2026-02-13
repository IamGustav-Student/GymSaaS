using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Pagos.Commands.ProcesarWebhook
{
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
            // 1. VERIFICACIÓN CRÍTICA CON MERCADOPAGO
            var estado = await _mpService.ObtenerEstadoPagoAsync(request.PaymentId);

            if (estado != "approved") return false;

            // 2. Recuperamos la referencia externa
            var externalRef = await _mpService.ObtenerExternalReferenceAsync(request.PaymentId);
            if (string.IsNullOrEmpty(externalRef)) return false;

            // --- NUEVA LÓGICA: DETECCIÓN DE TIPO DE PAGO (SaaS vs Socio) ---

            if (externalRef.StartsWith("SUBSCRIPTION|"))
            {
                return await ProcesarSuscripcionGimnasio(externalRef, cancellationToken);
            }

            // Si no tiene prefijo, asumimos que es un ID de MembresiaSocio (Lógica existente)
            if (int.TryParse(externalRef, out int membresiaId))
            {
                return await ProcesarPagoSocio(membresiaId, request.PaymentId, cancellationToken);
            }

            return false;
        }

        private async Task<bool> ProcesarSuscripcionGimnasio(string externalRef, CancellationToken cancellationToken)
        {
            var parts = externalRef.Split('|');
            if (parts.Length < 2 || !int.TryParse(parts[1], out int tenantId)) return false;

            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            if (tenant == null) return false;

            // Actualizamos el estado del Gimnasio para permitir el acceso al sistema
            tenant.IsActive = true;
            tenant.Status = SubscriptionStatus.Active;

            // Extendemos la vigencia (30 días por defecto para planes mensuales)
            tenant.SubscriptionEndsAt = DateTime.UtcNow.AddDays(30);

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<bool> ProcesarPagoSocio(int membresiaId, string paymentId, CancellationToken cancellationToken)
        {
            // 3. Buscamos la membresía pendiente
            var membresia = await _context.MembresiasSocios
                .IgnoreQueryFilters()
                .Include(m => m.TipoMembresia)
                .FirstOrDefaultAsync(m => m.Id == membresiaId, cancellationToken);

            if (membresia == null) return false;

            // Si ya estaba activa, no hacemos nada (evita duplicar pagos en el historial)
            if (membresia.Activa) return true;

            // Verificar Stacking por si tenía otra vigente
            var ultimaMembresia = await _context.MembresiasSocios
                .IgnoreQueryFilters()
                .Where(m => m.SocioId == membresia.SocioId && m.Activa && m.Id != membresia.Id && m.TenantId == membresia.TenantId)
                .OrderByDescending(m => m.FechaFin)
                .FirstOrDefaultAsync(cancellationToken);

            DateTime fechaInicio = DateTime.Now;
            if (ultimaMembresia != null && ultimaMembresia.FechaFin > DateTime.Now)
            {
                fechaInicio = ultimaMembresia.FechaFin;
            }

            membresia.FechaInicio = fechaInicio;
            membresia.FechaFin = fechaInicio.AddDays(membresia.TipoMembresia!.DuracionDias);
            membresia.Activa = true;

            // 4. GUARDAR EN EL HISTORIAL
            var nuevoPago = new Pago
            {
                SocioId = membresia.SocioId,
                MembresiaSocioId = membresia.Id,
                FechaPago = DateTime.Now,
                Monto = membresia.PrecioPagado,
                MetodoPago = "MercadoPago",
                TenantId = membresia.TenantId
            };

            _context.Pagos.Add(nuevoPago);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}