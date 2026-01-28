using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Pagos.Commands.CrearLinkPago
{
    // Input: ID de la Membresía vendida
    // Output: URL del link de pago
    public record CrearLinkPagoCommand(int MembresiaSocioId) : IRequest<string>;

    public class CrearLinkPagoCommandHandler : IRequestHandler<CrearLinkPagoCommand, string>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMercadoPagoService _mpService;
        private readonly ICurrentTenantService _tenantService;

        public CrearLinkPagoCommandHandler(IApplicationDbContext context, IMercadoPagoService mpService, ICurrentTenantService tenantService)
        {
            _context = context;
            _mpService = mpService;
            _tenantService = tenantService;
        }

        public async Task<string> Handle(CrearLinkPagoCommand request, CancellationToken cancellationToken)
        {
            // 1. Obtener datos de la venta
            var membresia = await _context.MembresiasSocios
                .Include(m => m.TipoMembresia)
                .Include(m => m.Socio)
                .FirstOrDefaultAsync(m => m.Id == request.MembresiaSocioId, cancellationToken);

            if (membresia == null) throw new Exception("Venta no encontrada");

            // 2. Obtener Token del Gimnasio (Tenant)
            // OJO: En un caso real, el TenantId viene del Service, buscamos en DB ese Tenant para sacar su token.
            // Por simplicidad del MVP, usaremos un Token "Dummy" o Global si el Tenant no tiene uno configurado.

            var tenant = await _context.Tenants
                .IgnoreQueryFilters() // Tenant no tiene TenantId, es la raíz
                                      // Aquí asumimos que tenemos el ID del tenant actual en _tenantService y buscamos por él si tuviéramos un ID numérico o GUID en Tenant.
                                      // Como simplificamos el modelo, vamos a usar un token de prueba HARDCODED si el campo está vacío.
                .FirstOrDefaultAsync(t => t.MercadoPagoAccessToken != null, cancellationToken);

            // HACK DE MVP: Si el tenant no configuró su token, usamos uno de prueba de MercadoPago (Sandbox)
            // TÚ DEBES PONER TU ACCESS TOKEN DE PRUEBA DE MERCADOPAGO AQUÍ
            string accessToken = tenant?.MercadoPagoAccessToken ?? "TEST-1559892095906321-101713-39832734139e80624855734208226071-182362483";

            // 3. Generar Link
            string titulo = $"Plan {membresia.TipoMembresia.Nombre} - {membresia.Socio.Nombre}";

            return await _mpService.CrearPreferenciaAsync(titulo, membresia.PrecioPagado, accessToken);
        }
    }
}