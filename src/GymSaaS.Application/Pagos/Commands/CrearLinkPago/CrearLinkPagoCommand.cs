using GymSaaS.Application.Common.Interfaces;
using MediatR;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Preference;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Pagos.Commands.CrearLinkPago
{
    // CAMBIO: Solo pedimos el ID. El resto lo averiguamos nosotros.
    public record CrearLinkPagoCommand(int MembresiaId) : IRequest<string>;

    public class CrearLinkPagoCommandHandler : IRequestHandler<CrearLinkPagoCommand, string>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMercadoPagoService _mercadoPagoService;

        public CrearLinkPagoCommandHandler(IApplicationDbContext context, IMercadoPagoService mercadoPagoService)
        {
            _context = context;
            _mercadoPagoService = mercadoPagoService;
        }

        public async Task<string> Handle(CrearLinkPagoCommand request, CancellationToken cancellationToken)
        {
            // 1. Buscamos la membresía y los datos del socio en la BD
            var membresia = await _context.MembresiasSocios
                .Include(m => m.Socio)
                .Include(m => m.TipoMembresia)
                .FirstOrDefaultAsync(m => m.Id == request.MembresiaId, cancellationToken);

            if (membresia == null) throw new KeyNotFoundException($"No existe la membresía {request.MembresiaId}");

            // 2. Construimos la preferencia con los datos reales de la BD
            var preferenceRequest = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = $"Pago: {membresia.TipoMembresia.Nombre}",
                        Quantity = 1,
                        CurrencyId = "ARS",
                        UnitPrice = membresia.PrecioPagado // Usamos el precio real guardado
                    }
                },
                Payer = new PreferencePayerRequest
                {
                    Email = membresia.Socio.Email
                },
                ExternalReference = membresia.Id.ToString(),

                // RECUERDA: Actualizar esta URL si reiniciaste Ngrok
                NotificationUrl = "https://unpatriotically-untempled-consuelo.ngrok-free.dev/api/webhooks/mercadopago",

                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = "https://localhost:7196/Pagos/Success",
                    Failure = "https://localhost:7196/Pagos/Failure",
                    Pending = "https://localhost:7196/Pagos/Pending"
                },
                AutoReturn = "approved"
            };

            return await _mercadoPagoService.CrearPreferenciaAsync(preferenceRequest);
        }
    }
}