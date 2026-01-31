using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Web.Controllers
{
    [Authorize]
    public class ConfiguracionController : Controller
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentTenantService _tenantService;

        public ConfiguracionController(IApplicationDbContext context, ICurrentTenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        // GET: Muestra el formulario
        public async Task<IActionResult> Pagos()
        {
            var config = await _context.ConfiguracionesPagos
                .FirstOrDefaultAsync(c => c.TenantId == _tenantService.TenantId);

            if (config == null) config = new ConfiguracionPago();

            return View(config);
        }

        // POST: Guarda las claves
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pagos(ConfiguracionPago model)
        {
            var config = await _context.ConfiguracionesPagos
                .FirstOrDefaultAsync(c => c.TenantId == _tenantService.TenantId);

            if (config == null)
            {
                config = new ConfiguracionPago
                {
                    TenantId = _tenantService.TenantId,
                    AccessToken = model.AccessToken,
                    PublicKey = model.PublicKey
                };
                _context.ConfiguracionesPagos.Add(config);
            }
            else
            {
                config.AccessToken = model.AccessToken;
                config.PublicKey = model.PublicKey;
            }

            await _context.SaveChangesAsync(CancellationToken.None);
            TempData["SuccessMessage"] = "Credenciales de MercadoPago actualizadas.";

            return View(config);
        }
    }
}