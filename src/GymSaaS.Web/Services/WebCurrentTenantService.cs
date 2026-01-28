using GymSaaS.Application.Common.Interfaces;
using System.Security.Claims;

namespace GymSaaS.Web.Services
{
    public class WebCurrentTenantService : ICurrentTenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WebCurrentTenantService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? TenantId
        {
            get
            {
                // Intentamos leer el TenantId de los Claims del usuario logueado
                return _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
            }
        }
    }
}