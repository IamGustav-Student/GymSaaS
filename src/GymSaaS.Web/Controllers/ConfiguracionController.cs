using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GymSaaS.Web.Controllers
{
    [Authorize]
    public class ConfiguracionController : Controller
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentTenantService _tenantService;
        private readonly IEncryptionService _encryptionService;
        private readonly IMemoryCache _cache;

        public ConfiguracionController(
            IApplicationDbContext context,
            ICurrentTenantService tenantService,
            IEncryptionService encryptionService,
            IMemoryCache cache)
        {
            _context = context;
            _tenantService = tenantService;
            _encryptionService = encryptionService;
            _cache = cache;
        }


        // GET: Muestra el formulario
        public async Task<IActionResult> Pagos()
        {
            var config = await _context.ConfiguracionesPagos
                .FirstOrDefaultAsync(c => c.TenantId == _tenantService.TenantId);

            if (config == null)
            {
                config = new ConfiguracionPago();
            }
            else
            {
                // Desencriptar para mostrar en la vista
                config.AccessToken = _encryptionService.Decrypt(config.AccessToken);
                config.PublicKey = _encryptionService.Decrypt(config.PublicKey ?? string.Empty);
            }

            return View(config);
        }

        // POST: Guarda las claves encriptadas
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pagos(ConfiguracionPago model)
        {
            if (string.IsNullOrEmpty(model.AccessToken))
            {
                ModelState.AddModelError("AccessToken", "El Access Token es requerido.");
                return View(model);
            }

            var config = await _context.ConfiguracionesPagos
                .FirstOrDefaultAsync(c => c.TenantId == _tenantService.TenantId);

            // Encriptamos antes de guardar
            string tokenCifrado = _encryptionService.Encrypt(model.AccessToken.Trim());
            string publicKeyCifrada = _encryptionService.Encrypt(model.PublicKey?.Trim() ?? "");

            // CORRECCIÓN CS8601: Asegurar que TenantId no sea nulo
            string tenantIdSeguro = _tenantService.TenantId ?? "default";

            if (config == null)
            {
                config = new ConfiguracionPago
                {
                    TenantId = tenantIdSeguro,
                    AccessToken = tokenCifrado,
                    PublicKey = publicKeyCifrada,
                    Activo = true,
                    ModoSandbox = model.ModoSandbox
                };
                _context.ConfiguracionesPagos.Add(config);
            }
            else
            {
                config.AccessToken = tokenCifrado;
                config.PublicKey = publicKeyCifrada;
                config.ModoSandbox = model.ModoSandbox;
                config.Activo = true;
            }

            await _context.SaveChangesAsync(CancellationToken.None);
            TempData["SuccessMessage"] = "Credenciales de MercadoPago actualizadas y encriptadas correctamente.";

            return View(model);
        }

        // ==========================================
        // GEO-FENCING (Ubicación del Gimnasio)
        // ==========================================

        public async Task<IActionResult> Geofencing()
        {
            var tenantId = _tenantService.TenantId;
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id.ToString() == tenantId);

            if (tenant == null) return NotFound();

            return View(tenant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Geofencing(double latitud, double longitud, int radio)
        {
            var tenantId = _tenantService.TenantId;
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id.ToString() == tenantId);

            if (tenant == null) return NotFound();

            tenant.Latitud = latitud;
            tenant.Longitud = longitud;
            tenant.RadioPermitidoMetros = radio;

            await _context.SaveChangesAsync(CancellationToken.None);
            TempData["SuccessMessage"] = "Ubicación del gimnasio actualizada con éxito.";

            return View(tenant);
        }

        // ==========================================
        // BRANDING PERSONALIZADO
        // ==========================================

        public async Task<IActionResult> Branding()
        {
            var tenantId = _tenantService.TenantId;
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id.ToString() == tenantId);

            if (tenant == null) return NotFound();
            return View(tenant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Branding(string? logoUrl, string? colorPrimario, string? gymNombreDisplay)
        {
            var tenantId = _tenantService.TenantId;
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id.ToString() == tenantId);

            if (tenant == null) return NotFound();

            tenant.LogoUrl          = string.IsNullOrWhiteSpace(logoUrl)         ? null : logoUrl.Trim();
            tenant.ColorPrimario    = string.IsNullOrWhiteSpace(colorPrimario)    ? null : colorPrimario.Trim();
            tenant.GymNombreDisplay = string.IsNullOrWhiteSpace(gymNombreDisplay) ? null : gymNombreDisplay.Trim();

            await _context.SaveChangesAsync(CancellationToken.None);

            // Invalida el caché del middleware para que el cambio sea instantáneo
            _cache.Remove($"tenant_resolver_{tenant.Code}");

            TempData["SuccessMessage"] = "¡Branding actualizado correctamente!";
            return RedirectToAction(nameof(Branding));
        }
    }
}