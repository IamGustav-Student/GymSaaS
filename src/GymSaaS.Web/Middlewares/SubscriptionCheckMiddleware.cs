using GymSaaS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Web.Middlewares
{
    public class SubscriptionCheckMiddleware
    {
        private readonly RequestDelegate _next;

        // Rutas permitidas incluso si la suscripción venció (Lista Blanca)
        private readonly string[] _rutasPermitidas = new[]
        {
            "/subscription", // Para poder pagar
            "/auth",         // Para poder salir (Logout)
            "/home",         // Landing page pública
            "/webhooks",     // Para que MercadoPago nos avise del pago
            "/css", "/js", "/lib", "/favicon" // Recursos estáticos
        };

        public SubscriptionCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IApplicationDbContext dbContext, ICurrentTenantService currentTenant)
        {
            // 1. Si no hay Tenant identificado, dejamos pasar (es landing o login global)
            if (string.IsNullOrEmpty(currentTenant.TenantId))
            {
                await _next(context);
                return;
            }

            // 2. Verificar si la ruta actual está en la lista blanca
            var path = context.Request.Path.ToString().ToLower();
            if (_rutasPermitidas.Any(r => path.StartsWith(r)))
            {
                await _next(context);
                return;
            }

            // 3. Consultar estado de la suscripción
            // Usamos AsNoTracking por rendimiento, solo queremos leer la fecha
            var tenantId = currentTenant.TenantId;
            var tenant = await dbContext.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id.ToString() == tenantId);

            if (tenant != null)
            {
                // LÓGICA DE BLOQUEO CON ZONA HORARIA LOCAL:
                // Convertimos DateTime.UtcNow a la zona horaria específica del gimnasio.
                var timeZoneId = string.IsNullOrEmpty(tenant.TimeZoneId) ? "Argentina Standard Time" : tenant.TimeZoneId;
                var gymTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                DateTime horaLocalGimnasio = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, gymTimeZone);

                // Si la fecha de fin es menor a la hora local actual del gimnasio, está vencido.
                bool estaVencido = tenant.SubscriptionEndsAt < horaLocalGimnasio;

                if (estaVencido)
                {
                    // REDIRECCIÓN FORZADA A LA PANTALLA DE PAGO
                    context.Response.Redirect("/Subscription/Pricing?reason=expired");
                    return; // Cortamos el pipeline aquí, no se ejecuta nada más.
                }
            }

            // Si todo está bien, adelante.
            await _next(context);
        }
    }
}