using GymSaaS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace GymSaaS.Web.Services
{
    public class WebCurrentTenantService : ICurrentTenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string? _manualTenantId;

        public WebCurrentTenantService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? TenantId
        {
            get
            {
                // 1. Prioridad ALTA: Si el Middleware resolvió un tenant por subdominio, usamos ese.
                if (!string.IsNullOrEmpty(_manualTenantId))
                {
                    return _manualTenantId;
                }

                // 2. Prioridad MEDIA: Si hay un usuario logueado, usamos su Claim (Compatibilidad Legacy).
                return _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
            }
        }

        // NUEVO MÉTODO: Permite al Middleware establecer el contexto actual
        public void SetTenant(string tenantId)
        {
            _manualTenantId = tenantId;
        }
    }
}