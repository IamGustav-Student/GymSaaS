using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Web.Services
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IApplicationDbContext dbContext, ICurrentTenantService currentTenantService)
        {
            // Estrategia de Resolución en Cascada (Fallback Strategy)

            // 1. Intentar resolver por Subdominio (La meta de la V2.0)
            // Ejemplo: iron-gym.gymvo.app -> tenant: iron-gym
            var host = context.Request.Host.Host;
            var subdominio = GetSubdomain(host);

            if (!string.IsNullOrEmpty(subdominio) && subdominio != "www" && subdominio != "localhost")
            {
                // Buscamos el tenant por su CODE (que ahora es el slug/subdominio)
                // Usamos AsNoTracking para mejor performance en lectura
                var tenant = await dbContext.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Code == subdominio);

                if (tenant != null)
                {
                    // ¡ÉXITO! Inyectamos el ID en el servicio Scoped para que toda la request lo use
                    // Nota: Necesitamos castear a la implementación concreta o agregar un Setter en la interfaz
                    if (currentTenantService is WebCurrentTenantService webService)
                    {
                        webService.SetTenant(tenant.Code);
                    }

                    // Guardamos el objeto Tenant completo en Items para acceso rápido en Vistas
                    context.Items["CurrentTenant"] = tenant;
                }
            }

            // 2. Si falló subdominio, intentar resolver por Usuario Logueado (Compatibilidad V1.0)
            // Esto sucede si el usuario entra a 'gymvo.app/login' sin subdominio
            if (string.IsNullOrEmpty(currentTenantService.TenantId) && context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                var tenantClaim = context.User.FindFirst("TenantId")?.Value;
                if (!string.IsNullOrEmpty(tenantClaim))
                {
                    if (currentTenantService is WebCurrentTenantService webService)
                    {
                        webService.SetTenant(tenantClaim);
                    }
                }
            }

            await _next(context);
        }

        private string GetSubdomain(string host)
        {
            if (string.IsNullOrEmpty(host)) return string.Empty;

            var parts = host.Split('.');
            if (parts.Length == 1) return string.Empty; // es 'localhost'

            // Lógica simple: tomamos la primera parte como subdominio
            // gym.dominio.com -> gym
            return parts[0];
        }
    }
}