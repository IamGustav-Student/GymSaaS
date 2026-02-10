using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities; // Necesario para crear la entidad MembresiaSocio
using MediatR;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Preference;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Pagos.Commands.CrearLinkPago
{
    // =========================================================================
    // LÓGICA EXISTENTE (NO MODIFICADA)
    // =========================================================================

    // Solo pedimos el ID. El resto lo averiguamos nosotros.
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

            var nombrePlan = membresia.TipoMembresia?.Nombre ?? "Membresía";
            var emailSocio = membresia.Socio!.Email;

            // 2. Construimos la preferencia con los datos reales de la BD
            var preferenceRequest = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = $"Pago: {nombrePlan}",
                        Quantity = 1,
                        CurrencyId = "ARS",
                        UnitPrice = membresia.PrecioPagado // Usamos el precio real guardado
                    }
                },
                Payer = new PreferencePayerRequest
                {
                    Email = emailSocio
                },
                ExternalReference = membresia.Id.ToString(),

                // RECUERDA: Actualizar esta URL si reiniciaste Ngrok
                NotificationUrl = "https://unpatriotically-untempled-consuelo.ngrok-free.dev/api/webhooks/mercadopago",

                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = "https://localhost:7196/Portal/Dashboard?status=success", // Ajustado para volver al Portal
                    Failure = "https://localhost:7196/Portal/Renovar?status=failure",
                    Pending = "https://localhost:7196/Portal/Renovar?status=pending"
                },
                AutoReturn = "approved"
            };

            return await _mercadoPagoService.CrearPreferenciaAsync(preferenceRequest);
        }
    }

    // =========================================================================
    // NUEVA LÓGICA AGREGADA (CONTRATAR MEMBRESÍA)
    // =========================================================================

    // Comando para registrar la intención de compra y crear la deuda (Estado: PendientePago)
    public record ContratarMembresiaCommand(int SocioId, int TipoMembresiaId) : IRequest<int>;

    public class ContratarMembresiaCommandHandler : IRequestHandler<ContratarMembresiaCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public ContratarMembresiaCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(ContratarMembresiaCommand request, CancellationToken cancellationToken)
        {
            // 1. Validar el Plan
            var plan = await _context.TiposMembresia.FindAsync(new object[] { request.TipoMembresiaId }, cancellationToken);
            if (plan == null) throw new KeyNotFoundException("El plan seleccionado no existe.");

            // 2. Crear la entidad MembresiaSocio
            var nuevaMembresia = new MembresiaSocio
            {
                SocioId = request.SocioId,
                TipoMembresiaId = request.TipoMembresiaId,
                FechaInicio = DateTime.Now,
                FechaFin = DateTime.Now.AddDays(plan.DuracionDias),
                PrecioPagado = plan.Precio,
                Estado = "PendientePago", // Estado inicial crítico para seguridad
                Activa = false
            };

            // 3. Guardar en BD para obtener el ID
            _context.MembresiasSocios.Add(nuevaMembresia);
            await _context.SaveChangesAsync(cancellationToken);

            // 4. Retornar ID para que el siguiente comando genere el link de pago
            return nuevaMembresia.Id;
        }
    }
}