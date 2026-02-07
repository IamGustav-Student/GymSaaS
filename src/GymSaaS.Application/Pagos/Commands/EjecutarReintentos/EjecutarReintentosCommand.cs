using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymSaaS.Application.Pagos.Commands.EjecutarReintentos
{
    public record EjecutarReintentosCommand : IRequest<int>
    {
    }

    public class EjecutarReintentosCommandHandler : IRequestHandler<EjecutarReintentosCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EjecutarReintentosCommandHandler> _logger;

        public EjecutarReintentosCommandHandler(
            IApplicationDbContext context,
            IMercadoPagoService mercadoPagoService,
            INotificationService notificationService,
            ILogger<EjecutarReintentosCommandHandler> logger)
        {
            _context = context;
            _mercadoPagoService = mercadoPagoService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<int> Handle(EjecutarReintentosCommand request, CancellationToken cancellationToken)
        {
            var hoy = DateTime.UtcNow.Date;

            var pagosParaReintentar = await _context.Pagos
                .IgnoreQueryFilters()
                .Include(p => p.Socio)
                .Where(p => !p.Pagado
                            && p.ProximoReintento != null
                            && p.ProximoReintento.Value.Date <= hoy
                            && p.IntentosFallidos < 3)
                .ToListAsync(cancellationToken);

            int procesados = 0;

            foreach (var pago in pagosParaReintentar)
            {
                try
                {
                    _logger.LogInformation($"Reintentando pago ID {pago.Id} para Socio {pago.SocioId}");

                    bool cobroExitoso = true; // Simulación

                    if (cobroExitoso)
                    {
                        pago.Pagado = true;
                        pago.EstadoTransaccion = "approved_retry";
                        pago.ProximoReintento = null;
                        pago.FechaPago = DateTime.UtcNow;

                        // CORRECCIÓN WARNINGS
                        if (pago.Socio != null)
                            await _notificationService.EnviarConfirmacionPago(pago.Socio.Nombre, pago.Socio.Telefono ?? "", pago.Monto);
                    }
                    else
                    {
                        pago.IntentosFallidos++;
                        if (pago.IntentosFallidos < 3)
                        {
                            pago.ProximoReintento = DateTime.UtcNow.AddDays(3);

                            // CORRECCIÓN WARNINGS
                            if (pago.Socio != null && pago.ProximoReintento.HasValue)
                                await _notificationService.EnviarAlertaPagoFallido(pago.Socio.Nombre, pago.Socio.Telefono ?? "", pago.ProximoReintento.Value);
                        }
                        else
                        {
                            pago.ProximoReintento = null;
                            pago.EstadoTransaccion = "final_failed";
                        }
                    }

                    procesados++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error procesando reintento pago {pago.Id}");
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return procesados;
        }
    }
}