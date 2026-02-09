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
        private readonly IEncryptionService _encryptionService;

        public ConfiguracionController(
            IApplicationDbContext context,
            ICurrentTenantService tenantService,
            IEncryptionService encryptionService)
        {
            _context = context;
            _tenantService = tenantService;
            _encryptionService = encryptionService;
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
    }
}