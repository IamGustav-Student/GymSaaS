using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GymSaaS.Web.Services
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache; // Inyectamos caché

        public TenantResolutionMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context, IApplicationDbContext dbContext, ICurrentTenantService currentTenantService)
        {
            // Estrategia: Subdominio -> DB (Cacheada) -> Set Context

            var host = context.Request.Host.Host;
            var subdominio = GetSubdomain(host);
            Tenant? tenant = null;

            if (!string.IsNullOrEmpty(subdominio) && subdominio != "www" && subdominio != "localhost")
            {
                // CLAVE DE CACHÉ: Única por subdominio
                var cacheKey = $"tenant_resolver_{subdominio}";

                // 1. Intentar obtener de caché
                if (!_cache.TryGetValue(cacheKey, out tenant))
                {
                    // 2. Si no está, consultar DB (Hit costoso)
                    tenant = await dbContext.Tenants
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Code == subdominio);

                    // 3. Guardar en caché si existe (por 30 minutos)
                    if (tenant != null)
                    {
                        var cacheOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
                            .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                            .SetPriority(CacheItemPriority.High);

                        _cache.Set(cacheKey, tenant, cacheOptions);
                    }
                }

                // 4. Establecer contexto si encontramos el tenant
                if (tenant != null)
                {
                    if (currentTenantService is WebCurrentTenantService webService)
                    {
                        webService.SetTenant(tenant.Code);
                    }

                    // Items para acceso rápido en Vistas (ej: _Layout.cshtml)
                    context.Items["CurrentTenant"] = tenant;
                }
            }

            // Fallback: Usuario Logueado (Legacy / Localhost)
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
            if (parts.Length == 1) return string.Empty;
            return parts[0];
        }
    }
}