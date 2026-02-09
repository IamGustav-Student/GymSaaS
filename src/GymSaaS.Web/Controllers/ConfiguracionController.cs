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
        private readonly IEncryptionService _encryptionService; // Inyección

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
                // DESENCRIPTAR PARA MOSTRAR (Opcional, a veces es mejor no mostrar el token real)
                // Aquí lo desencriptamos para que el usuario pueda ver/editar lo que guardó.
                config.AccessToken = _encryptionService.Decrypt(config.AccessToken);
                config.PublicKey = _encryptionService.Decrypt(config.PublicKey);
                // Nota: WebhookSecret también debería encriptarse si es sensible
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

            // ENCRIPTACIÓN ANTES DE GUARDAR
            // Usamos Trim() para evitar espacios en blanco accidentales al copiar/pegar
            string tokenCifrado = _encryptionService.Encrypt(model.AccessToken.Trim());
            string publicKeyCifrada = _encryptionService.Encrypt(model.PublicKey?.Trim() ?? "");

            if (config == null)
            {
                config = new ConfiguracionPago
                {
                    TenantId = _tenantService.TenantId,
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

            // Para la vista de retorno, mostramos los valores limpios (el modelo original)
            // para no mostrar el string encriptado al usuario.
            return View(model);
        }
    }
}