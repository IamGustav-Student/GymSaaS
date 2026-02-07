using FluentValidation;
using GymSaaS.Application.Common.Behaviours;
using GymSaaS.Application.Common.Helpers;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Application.Common.Validators;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymSaaS.Application.Pagos.Commands.ProcesarPagoTarjeta
{
    public record ProcesarPagoTarjetaCommand : IRequest<bool>
    {
        public int SocioId { get; init; }
        public string Titular { get; init; } = string.Empty;
        public string NumeroTarjeta { get; init; } = string.Empty;
        public string Vencimiento { get; init; } = string.Empty; // MM/YY
        public string CVV { get; init; } = string.Empty;
        public decimal Monto { get; init; }
    }

    public class ProcesarPagoTarjetaCommandValidator : AbstractValidator<ProcesarPagoTarjetaCommand>
    {
        public ProcesarPagoTarjetaCommandValidator()
        {
            RuleFor(v => v.Titular).NotEmpty().WithMessage("El nombre del titular es requerido.");
            RuleFor(v => v.NumeroTarjeta).NotEmpty().EsTarjetaCreditoValida();
            RuleFor(v => v.Vencimiento).Matches(@"^(0[1-9]|1[0-2])\/?([0-9]{2})$").WithMessage("Formato inválido (MM/YY).");
            RuleFor(v => v.CVV).NotEmpty().Matches(@"^\d{3,4}$").WithMessage("CVV inválido.");
            RuleFor(v => v.Monto).GreaterThan(0);
        }
    }

    public class ProcesarPagoTarjetaCommandHandler : IRequestHandler<ProcesarPagoTarjetaCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ProcesarPagoTarjetaCommandHandler> _logger;

        public ProcesarPagoTarjetaCommandHandler(
            IApplicationDbContext context,
            IMercadoPagoService mercadoPagoService,
            INotificationService notificationService,
            ILogger<ProcesarPagoTarjetaCommandHandler> logger)
        {
            _context = context;
            _mercadoPagoService = mercadoPagoService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<bool> Handle(ProcesarPagoTarjetaCommand request, CancellationToken cancellationToken)
        {
            var nuevoPago = new Pago
            {
                SocioId = request.SocioId,
                Monto = request.Monto,
                FechaPago = DateTime.UtcNow,
                MetodoPago = "TarjetaCredito",
                Pagado = false,
                EstadoTransaccion = "pending",
                IntentosFallidos = 0
            };

            _context.Pagos.Add(nuevoPago);
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                // Ahora IMercadoPagoService SÍ tiene este método
                var resultadoMP = await _mercadoPagoService.ProcesarPago(request.Monto, request.NumeroTarjeta, request.Titular);

                nuevoPago.Pagado = true;
                nuevoPago.EstadoTransaccion = "approved";
                nuevoPago.IdTransaccionExterna = resultadoMP;
                nuevoPago.TokenTarjeta = "tok_simulado_" + Guid.NewGuid();

                await _context.SaveChangesAsync(cancellationToken);

                var socio = await _context.Socios.FindAsync(new object[] { request.SocioId }, cancellationToken);

                // --- CORRECCIÓN WARNINGS ---
                if (socio != null)
                    await _notificationService.EnviarConfirmacionPago(socio.Nombre, socio.Telefono ?? "", request.Monto);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo al procesar pago tarjeta");

                bool esFondosInsuficientes = ex.Message.Contains("cc_rejected_insufficient_amount")
                                          || ex.Message.Contains("insufficient_funds")
                                          || true;

                if (esFondosInsuficientes)
                {
                    nuevoPago.Pagado = false;
                    nuevoPago.EstadoTransaccion = "rejected_insufficient_funds";
                    nuevoPago.IntentosFallidos++;
                    nuevoPago.ProximoReintento = DateTime.UtcNow.AddDays(3);

                    await _context.SaveChangesAsync(cancellationToken);

                    var socio = await _context.Socios.FindAsync(new object[] { request.SocioId }, cancellationToken);

                    // --- CORRECCIÓN WARNINGS ---
                    if (socio != null && nuevoPago.ProximoReintento.HasValue)
                    {
                        await _notificationService.EnviarAlertaPagoFallido(
                            socio.Nombre,
                            socio.Telefono ?? "",
                            nuevoPago.ProximoReintento.Value
                        );
                    }

                    return true;
                }

                throw;
            }
        }
    }
}