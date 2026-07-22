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
        private readonly INotificationService _notificationService;
        private readonly ILogger<EjecutarReintentosCommandHandler> _logger;

        public EjecutarReintentosCommandHandler(
            IApplicationDbContext context,
            INotificationService notificationService,
            ILogger<EjecutarReintentosCommandHandler> logger)
        {
            _context = context;
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

                    // NOTA: no existe todavía una integración de recobro automático real
                    // (requeriría tarjeta guardada vía MercadoPago Customer/Card API o una
                    // suscripción recurrente/Preapproval). Hasta que eso exista, este job NO
                    // debe fingir que cobró: solo recuerda la deuda al socio y cuenta los
                    // intentos, dejando el cobro real para que el socio lo complete manualmente
                    // (link de pago) o para reintentar cuando se implemente el recobro real.
                    pago.IntentosFallidos++;
                    if (pago.IntentosFallidos < 3)
                    {
                        pago.ProximoReintento = DateTime.UtcNow.AddDays(3);

                        if (pago.Socio != null && pago.ProximoReintento.HasValue)
                            await _notificationService.EnviarAlertaPagoFallido(pago.Socio.Nombre, pago.Socio.Telefono ?? "", pago.ProximoReintento.Value);
                    }
                    else
                    {
                        pago.ProximoReintento = null;
                        pago.EstadoTransaccion = "final_failed";
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