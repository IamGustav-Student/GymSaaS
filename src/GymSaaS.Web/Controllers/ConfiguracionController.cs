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
<<<<<<< HEAD
        private readonly IEncryptionService _encryptionService;
=======
>>>>>>> parent of 34be421 (.env y MP)

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

<<<<<<< HEAD
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
=======
            if (config == null) config = new ConfiguracionPago();
>>>>>>> parent of 34be421 (.env y MP)

            return View(config);
        }

        // POST: Guarda las claves
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pagos(ConfiguracionPago model)
        {
            var config = await _context.ConfiguracionesPagos
                .FirstOrDefaultAsync(c => c.TenantId == _tenantService.TenantId);

<<<<<<< HEAD
            // Encriptamos antes de guardar
            string tokenCifrado = _encryptionService.Encrypt(model.AccessToken.Trim());
            string publicKeyCifrada = _encryptionService.Encrypt(model.PublicKey?.Trim() ?? "");

            // CORRECCIÓN CS8601: Asegurar que TenantId no sea nulo
            string tenantIdSeguro = _tenantService.TenantId ?? "default";

=======
>>>>>>> parent of 34be421 (.env y MP)
            if (config == null)
            {
                config = new ConfiguracionPago
                {
<<<<<<< HEAD
                    TenantId = tenantIdSeguro,
                    AccessToken = tokenCifrado,
                    PublicKey = publicKeyCifrada,
                    Activo = true,
                    ModoSandbox = model.ModoSandbox
=======
                    TenantId = _tenantService.TenantId,
                    AccessToken = model.AccessToken,
                    PublicKey = model.PublicKey
>>>>>>> parent of 34be421 (.env y MP)
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

<<<<<<< HEAD
            return View(model);
=======
            return View(config);
>>>>>>> parent of 34be421 (.env y MP)
        }
    }
}