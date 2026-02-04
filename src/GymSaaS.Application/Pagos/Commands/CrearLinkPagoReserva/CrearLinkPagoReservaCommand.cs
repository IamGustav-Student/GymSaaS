using GymSaaS.Application.Common.Interfaces;
using MediatR;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Preference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace GymSaaS.Application.Pagos.Commands.CrearLinkPagoReserva
{
    public record CrearLinkPagoReservaCommand(int ReservaId) : IRequest<string>;

    public class CrearLinkPagoReservaCommandHandler : IRequestHandler<CrearLinkPagoReservaCommand, string>
    {
        private readonly IApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public CrearLinkPagoReservaCommandHandler(IApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<string> Handle(CrearLinkPagoReservaCommand request, CancellationToken cancellationToken)
        {
            var reserva = await _context.Reservas
                .Include(r => r.Clase)
                .Include(r => r.Socio)
                .FirstOrDefaultAsync(r => r.Id == request.ReservaId, cancellationToken);

            if (reserva == null) throw new Exception("Reserva no encontrada");

            // Configurar MP (Toma el Token de tu appsettings.json)
            MercadoPagoConfig.AccessToken = _configuration["MercadoPago:AccessToken"];

            // Crear la Preferencia de Pago
            var requestMp = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = $"Clase: {reserva.Clase.Nombre}",
                        Quantity = 1,
                        CurrencyId = "ARS",
                        UnitPrice = reserva.Monto,
                        Description = $"Reserva para el {reserva.Clase.FechaHoraInicio:dd/MM HH:mm}"
                    }
                },
                Payer = new PreferencePayerRequest
                {
                    Email = reserva.Socio.Email ?? "socio@gymvo.com",
                    Name = reserva.Socio.Nombre,
                    Surname = reserva.Socio.Apellido
                },
                BackUrls = new PreferenceBackUrlsRequest
                {
                    // Ajusta estas URLs a tu dominio real o localhost
                    Success = "https://localhost:7039/Portal/Clases",
                    Failure = "https://localhost:7039/Portal/Clases",
                    Pending = "https://localhost:7039/Portal/Clases"
                },
                AutoReturn = "approved",
                ExternalReference = $"RESERVA-{reserva.Id}"
            };

            var client = new PreferenceClient();
            Preference preference = await client.CreateAsync(requestMp, cancellationToken: cancellationToken);

            return preference.InitPoint; // Devuelve la URL de MercadoPago
        }
    }
}