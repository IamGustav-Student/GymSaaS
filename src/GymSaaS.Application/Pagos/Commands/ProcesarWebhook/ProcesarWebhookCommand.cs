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
            // Preguntamos a MP: "¿Es verdad que este pago está aprobado?"
            var estado = await _mpService.ObtenerEstadoPagoAsync(request.PaymentId);

            // Si MP dice "pending" o "rejected", abortamos. No activamos nada.
            if (estado != "approved") return false;

            // 2. Recuperamos ID Membresía
            var externalRef = await _mpService.ObtenerExternalReferenceAsync(request.PaymentId);
            if (!int.TryParse(externalRef, out int membresiaId)) return false;

            // 3. Buscamos la membresía pendiente
            var membresia = await _context.MembresiasSocios
                .IgnoreQueryFilters()
                .Include(m => m.TipoMembresia)
                .FirstOrDefaultAsync(m => m.Id == membresiaId, cancellationToken);

            if (membresia == null) return false;

            // Si ya estaba activa, no hacemos nada (evita duplicar pagos en el historial)
            if (membresia.Activa) return true;

            // --- LÓGICA DE ACTIVACIÓN Y ACUMULACIÓN ---

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
            membresia.Activa = true; // <--- AHORA SÍ ACTIVAMOS

            // 4. GUARDAR EN EL HISTORIAL (Solo ahora que está verificado)
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