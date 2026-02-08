using GymSaaS.Application.Common.Interfaces;
using MediatR;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Preference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace GymSaaS.Application.Pagos.Commands.CrearLinkPagoReserva
{
    // SEGURIDAD: Eliminamos 'Precio' o 'Monto' del contrato.
    // El cliente solo puede decir "Quién" y "Qué", no "Cuánto".
    public record CrearLinkPagoReservaCommand(int SocioId, int TipoMembresiaId) : IRequest<string>;

    public class CrearLinkPagoReservaCommandHandler : IRequestHandler<CrearLinkPagoReservaCommand, string>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly IConfiguration _configuration;

        public CrearLinkPagoReservaCommandHandler(
            IApplicationDbContext context,
            IMercadoPagoService mercadoPagoService,
            IConfiguration configuration)
        {
            _context = context;
            _mercadoPagoService = mercadoPagoService;
            _configuration = configuration;
        }

        public async Task<string> Handle(CrearLinkPagoReservaCommand request, CancellationToken cancellationToken)
        {
            // 1. VALIDACIÓN DE IDENTIDAD Y PRODUCTO (Source of Truth)

            // Buscamos la membresía en la DB para obtener el PRECIO REAL
            var membresia = await _context.TiposMembresia
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == request.TipoMembresiaId, cancellationToken);

            if (membresia == null)
                throw new Exception("El plan de membresía seleccionado no existe o no está disponible.");

            // Buscamos al socio para asociar el pago correctamente
            var socio = await _context.Socios
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.SocioId, cancellationToken);

            if (socio == null)
                throw new Exception("Socio no encontrado. Por favor reinicie sesión.");

            // 2. CONSTRUCCIÓN SEGURA DE LA PREFERENCIA DE PAGO
            // Usamos membresia.Precio (DB) en lugar de cualquier dato externo
            var requestMp = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = $"Plan: {membresia.Nombre}",
                        Quantity = 1,
                        CurrencyId = "ARS", // O la moneda de tu Tenant
                        UnitPrice = membresia.Precio, // <--- BLINDAJE FINANCIERO AQUÍ
                        Description = $"Renovación {membresia.Nombre} - {membresia.DuracionDias} días"
                    }
                },
                Payer = new PreferencePayerRequest
                {
                    Email = socio.Email ?? "socio@gymvo.com", // Fallback si no tiene email
                    Name = socio.Nombre,
                    Surname = socio.Apellido
                },
                BackUrls = new PreferenceBackUrlsRequest
                {
                    // URLs de retorno para la UX
                    Success = "https://tu-dominio.com/Portal/PagoExitoso",
                    Failure = "https://tu-dominio.com/Portal/PagoFallido",
                    Pending = "https://tu-dominio.com/Portal/PagoPendiente"
                },
                AutoReturn = "approved",
                // ExternalReference: Clave para la conciliación automática (Webhooks)
                // Guardamos IDs clave para procesar luego
                ExternalReference = $"MEMB-{socio.Id}-{membresia.Id}-{Guid.NewGuid().ToString().Substring(0, 8)}"
            };

            // 3. EJECUCIÓN A TRAVÉS DEL SERVICIO DE INFRAESTRUCTURA
            return await _mercadoPagoService.CrearPreferenciaAsync(requestMp);
        }
    }
}