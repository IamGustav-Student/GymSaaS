// =============================================================================
// ARCHIVO: WhatsAppNotificationService.cs
// CAPA: Infrastructure/Services
// PROPÓSITO: Implementación real del INotificationService usando la API de
//            WhatsApp Business (Meta/Twilio).
//
// CÓMO FUNCIONA:
//   Esta clase implementa TODOS los métodos de INotificationService.
//   Para cada tipo de notificación construye el mensaje de texto
//   y lo envía a la API de WhatsApp.
//
// CONFIGURACIÓN NECESARIA EN appsettings.json (o variables de entorno):
//   "WhatsApp": {
//     "Provider": "twilio",          <-- "twilio" o "meta" o "mock"
//     "AccountSid": "ACxxxxxxxx",    <-- Solo para Twilio
//     "AuthToken": "xxxxxxxx",       <-- Solo para Twilio
//     "FromNumber": "whatsapp:+14155238886",  <-- Número Twilio Sandbox
//     "MetaAccessToken": "EAAxxxxxxx",        <-- Solo para Meta API
//     "MetaPhoneNumberId": "1234567890"       <-- Solo para Meta API
//   }
//   "App": {
//     "BaseUrl": "https://tudominio.com"
//   }
//
// SI NO TENÉS CREDENCIALES:
//   Cuando Provider = "mock" (o cuando faltan las credenciales), el servicio
//   simplemente loguea el mensaje en la consola. Útil para desarrollo.
//
// NOTA PARA EL JUNIOR:
//   Esta clase no necesita ser modificada cuando querés agregar una nueva
//   notificación. Simplemente implementás el nuevo método de la interfaz.
// =============================================================================

using GymSaaS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GymSaaS.Infrastructure.Services
{
    public class WhatsAppNotificationService : INotificationService
    {
        private readonly ILogger<WhatsAppNotificationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        // Leemos el proveedor configurado para saber cómo enviar
        private string Provider => _configuration["WhatsApp:Provider"] ?? "mock";
        private string BaseUrl => _configuration["App:BaseUrl"] ?? "https://gymvo.app";

        public WhatsAppNotificationService(
            ILogger<WhatsAppNotificationService> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            // HttpClientFactory es la forma correcta de crear HttpClients en .NET
            // Evita el problema de "socket exhaustion" que ocurre con new HttpClient()
            _httpClient = httpClientFactory.CreateClient("WhatsApp");
        }

        // =====================================================================
        // MÉTODO PRIVADO CENTRAL: Envío del mensaje
        // =====================================================================

        /// <summary>
        /// Método privado que centraliza el envío real del mensaje.
        /// Todos los métodos públicos construyen el texto y llaman a este.
        /// Así si cambiamos de Twilio a Meta, solo cambiamos este método.
        /// </summary>
        /// <param name="telefono">Número en formato internacional. Ej: +5491112345678</param>
        /// <param name="mensaje">Texto del mensaje. Soporta emojis.</param>
        private async Task EnviarMensajeAsync(string telefono, string mensaje)
        {
            // Validación básica: si no hay teléfono, no intentamos enviar
            if (string.IsNullOrWhiteSpace(telefono))
            {
                _logger.LogWarning("[WhatsApp] Intento de envío sin número de teléfono. Mensaje omitido.");
                return;
            }

            // Normalizamos el número: eliminamos espacios y guiones
            var telefonoLimpio = telefono.Trim().Replace(" ", "").Replace("-", "");

            // Si el número no empieza con "+", asumimos Argentina (+54)
            if (!telefonoLimpio.StartsWith("+"))
            {
                telefonoLimpio = "+54" + telefonoLimpio;
            }

            // Seleccionamos el proveedor configurado
            switch (Provider.ToLower())
            {
                case "twilio":
                    await EnviarViaTwilio(telefonoLimpio, mensaje);
                    break;

                case "meta":
                    await EnviarViaMeta(telefonoLimpio, mensaje);
                    break;

                default:
                    // Modo "mock": solo loguea. Útil para desarrollo sin credenciales.
                    _logger.LogInformation(
                        "[WhatsApp MOCK] → {Telefono} | {Mensaje}",
                        telefonoLimpio,
                        mensaje);
                    break;
            }
        }

        /// <summary>
        /// Envío real usando la API de Twilio WhatsApp Sandbox.
        /// Requiere AccountSid y AuthToken en la configuración.
        /// Documentación: https://www.twilio.com/docs/whatsapp/api
        /// </summary>
        private async Task EnviarViaTwilio(string telefono, string mensaje)
        {
            var accountSid = _configuration["WhatsApp:AccountSid"];
            var authToken = _configuration["WhatsApp:AuthToken"];
            var fromNumber = _configuration["WhatsApp:FromNumber"]; // Ej: "whatsapp:+14155238886"

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken))
            {
                _logger.LogWarning("[WhatsApp Twilio] Credenciales no configuradas. Mensaje no enviado.");
                return;
            }

            try
            {
                // La API de Twilio usa Basic Auth con AccountSid:AuthToken
                var credentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", credentials);

                // Twilio usa form-urlencoded, no JSON
                var contenido = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("From", fromNumber ?? ""),
                    new KeyValuePair<string, string>("To", $"whatsapp:{telefono}"),
                    new KeyValuePair<string, string>("Body", mensaje)
                });

                var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
                var respuesta = await _httpClient.PostAsync(url, contenido);

                if (respuesta.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[WhatsApp Twilio] ✓ Enviado a {Telefono}", telefono);
                }
                else
                {
                    var error = await respuesta.Content.ReadAsStringAsync();
                    _logger.LogError("[WhatsApp Twilio] ✗ Error {Status}: {Error}", respuesta.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WhatsApp Twilio] Excepción enviando a {Telefono}", telefono);
            }
        }

        /// <summary>
        /// Envío real usando la Meta WhatsApp Business Cloud API.
        /// Requiere MetaAccessToken y MetaPhoneNumberId en la configuración.
        /// Documentación: https://developers.facebook.com/docs/whatsapp/cloud-api
        /// </summary>
        private async Task EnviarViaMeta(string telefono, string mensaje)
        {
            var accessToken = _configuration["WhatsApp:MetaAccessToken"];
            var phoneNumberId = _configuration["WhatsApp:MetaPhoneNumberId"];

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(phoneNumberId))
            {
                _logger.LogWarning("[WhatsApp Meta] Credenciales no configuradas. Mensaje no enviado.");
                return;
            }

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                // Quitamos el "+" del número para Meta API
                var telefonoSinPlus = telefono.TrimStart('+');

                // Meta API usa JSON con estructura específica
                var payload = new
                {
                    messaging_product = "whatsapp",
                    to = telefonoSinPlus,
                    type = "text",
                    text = new { body = mensaje }
                };

                var json = JsonSerializer.Serialize(payload);
                var contenido = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://graph.facebook.com/v18.0/{phoneNumberId}/messages";
                var respuesta = await _httpClient.PostAsync(url, contenido);

                if (respuesta.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[WhatsApp Meta] ✓ Enviado a {Telefono}", telefono);
                }
                else
                {
                    var error = await respuesta.Content.ReadAsStringAsync();
                    _logger.LogError("[WhatsApp Meta] ✗ Error {Status}: {Error}", respuesta.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WhatsApp Meta] Excepción enviando a {Telefono}", telefono);
            }
        }

        // =====================================================================
        // MÉTODOS ORIGINALES — IMPLEMENTACIÓN CONSERVADA INTACTA
        // =====================================================================

        public Task EnviarAlertaPagoFallido(string nombreUsuario, string telefono, DateTime fechaReintento)
        {
            var mensaje =
                $"⚠️ *GymvoOS* — Hola {nombreUsuario}, tu pago falló.\n" +
                $"Reintentaremos el {fechaReintento:dd/MM/yyyy}.\n" +
                $"Para evitar la suspensión de tu membresía, contactá al gimnasio.";

            return EnviarMensajeAsync(telefono, mensaje);
        }

        public Task EnviarConfirmacionPago(string nombreUsuario, string telefono, decimal monto)
        {
            var mensaje =
                $"✅ *GymvoOS* — ¡Hola {nombreUsuario}!\n" +
                $"Recibimos tu pago de *${monto:N2}*. ¡Gracias por confiar en nosotros!\n" +
                $"¡A entrenar! 💪";

            return EnviarMensajeAsync(telefono, mensaje);
        }

        public Task EnviarNotificacion(string telefono, string mensaje)
        {
            // Método genérico: se pasa el mensaje completo desde el caller
            return EnviarMensajeAsync(telefono, mensaje);
        }

        // =====================================================================
        // MÓDULO 1: MEMBRESÍAS Y COBROS
        // =====================================================================

        public Task EnviarAvisoVencimientoProximo(
            string nombreSocio,
            string telefono,
            string nombrePlan,
            int diasRestantes,
            string linkRenovacion)
        {
            var urgencia = diasRestantes <= 3 ? "⚠️ URGENTE" : "📅 Recordatorio";
            var mensaje =
                $"{urgencia} *GymvoOS* — Hola {nombreSocio}!\n\n" +
                $"Tu membresía *{nombrePlan}* vence en *{diasRestantes} día(s)*.\n\n" +
                $"Renová ahora y no pierdas tu acceso:\n{linkRenovacion}\n\n" +
                $"¿Consultas? Contactá al gimnasio. 🏋️";

            return EnviarMensajeAsync(telefono, mensaje);
        }

        public Task EnviarConfirmacionPagoDetallada(
            string nombreSocio,
            string telefono,
            string nombrePlan,
            decimal monto,
            DateTime fechaVencimiento)
        {
            var mensaje =
                $"✅ *GymvoOS* — ¡Pago confirmado!\n\n" +
                $"Hola *{nombreSocio}*, tu membresía fue renovada:\n" +
                $"📋 Plan: *{nombrePlan}*\n" +
                $"💰 Monto: *${monto:N2}*\n" +
                $"📅 Válida hasta: *{fechaVencimiento:dd/MM/yyyy}*\n\n" +
                $"¡Gracias! Nos vemos en el gimnasio. 💪🔥";

            return EnviarMensajeAsync(telefono, mensaje);
        }

        public Task EnviarRecordatorioDeuda(
            string nombreSocio,
            string telefono,
            string linkRenovacion)
        {
            var mensaje =
                $"🔴 *GymvoOS* — Hola {nombreSocio},\n\n" +
                $"Tu membresía está *vencida* o tenés un saldo pendiente.\n\n" +
                $"Regularizá tu situación para seguir disfrutando del gimnasio:\n" +
                $"{linkRenovacion}\n\n" +
                $"¡Te esperamos! 🏋️";

            return EnviarMensajeAsync(telefono, mensaje);
        }

        // =====================================================================
        // MÓDULO 2: CLASES Y RESERVAS
        // =====================================================================

        public Task EnviarConfirmacionReserva(
            string nombreSocio,
            string telefono,
            string nombreClase,
            string? instructor,
            DateTime fechaHora)
        {
            var infoInstructor = string.IsNullOrEmpty(instructor)
                ? ""
                : $"🧑‍🏫 Instructor: *{instructor}*\n";

            var mensaje =
                $"🎯 *GymvoOS* — ¡Reserva confirmada!\n\n" +
                $"Hola *{nombreSocio}*, tu lugar está reservado:\n" +
                $"📚 Clase: *{nombreClase}*\n" +
                $"{infoInstructor}" +
                $"📅 Día: *{fechaHora:dddd dd/MM/yyyy}*\n" +
                $"⏰ Hora: *{fechaHora:HH:mm}*\n\n" +
                $"Te esperamos. ¡No faltes! 💪";

            return EnviarMensajeAsync(telefono, mensaje);
        }

        public Task EnviarRecordatorioClase(
            string nombreSocio,
            string telefono,
            string nombreClase,
            DateTime horaInicio)
        {
            var mensaje =
                $"⏰ *GymvoOS* — Recordatorio de clase\n\n" +
                $"Hola *{nombreSocio}*!\n" +
                $"Tu clase *{nombreClase}* empieza en *1 hora* ({horaInicio:HH:mm}).\n\n" +
                $"¡Preparate y viene con energía! 🔥🏋️";

            return EnviarMensajeAsync(telefono, mensaje);
        }

        public Task EnviarAvísoCancelacionClase(
            string nombreSocio,
            string telefono,
            string nombreClase,
            DateTime fechaHora)
        {
            var mensaje =
                $"❌ *GymvoOS* — Clase cancelada\n\n" +
                $"Hola *{nombreSocio}*, lamentamos informarte que la clase\n" +
                $"*{nombreClase}* del {fechaHora:dd/MM} a las {fechaHora:HH:mm} " +
                $"fue *cancelada* por el gimnasio.\n\n" +
                $"Disculpá los inconvenientes. Para más info, contactá al gimnasio.";

            return EnviarMensajeAsync(telefono, mensaje);
        }

        // =====================================================================
        // MÓDULO 3: CONTROL DE ACCESOS Y ONBOARDING
        // =====================================================================

        public Task EnviarBienvenidaNuevoSocio(
            string nombreSocio,
            string telefono,
            string dni,
            string linkPortal)
        {
            var mensaje =
                $"🏋️ *¡Bienvenido/a a GymvoOS!*\n\n" +
                $"Hola *{nombreSocio}*, tu cuenta fue creada exitosamente.\n\n" +
                $"🔑 *Tu usuario:* {dni}\n" +
                $"📱 *Portal del alumno:* {linkPortal}\n\n" +
                $"Desde el portal podés ver tus rutinas, reservar clases " +
                $"y controlar tu membresía.\n\n" +
                $"¡Mucho éxito en tu entrenamiento! 💪🔥";

            return EnviarMensajeAsync(telefono, mensaje);
        }

        public Task EnviarAlertaInactividad(
            string nombreSocio,
            string telefono,
            int diasSinAsistir)
        {
            // El mensaje varía según cuántos días llevan sin ir
            string motivacion = diasSinAsistir >= 15
                ? $"¡Hace {diasSinAsistir} días que no te vemos! 😢 El equipo te extraña."
                : $"Llevás {diasSinAsistir} días sin entrenar. ¡Volvé que te extrañamos!";

            var mensaje =
                $"💪 *GymvoOS* — ¡Hola {nombreSocio}!\n\n" +
                $"{motivacion}\n\n" +
                $"Recordá que cada entrenamiento te acerca más a tus metas. " +
                $"¡Tu cuerpo te lo va a agradecer! 🏋️🔥\n\n" +
                $"Reservá una clase hoy y retomá el ritmo. ¡Te esperamos!";

            return EnviarMensajeAsync(telefono, mensaje);
        }

        public Task EnviarNotificacionAccesoDenegado(
            string nombreSocio,
            string telefono,
            string linkRenovacion)
        {
            var mensaje =
                $"🚫 *GymvoOS* — Acceso denegado\n\n" +
                $"Hola *{nombreSocio}*, tu membresía está *vencida* o inactiva.\n\n" +
                $"Para regularizar tu situación al instante:\n" +
                $"👇 {linkRenovacion}\n\n" +
                $"¿Necesitás ayuda? Consultá en recepción.";

            return EnviarMensajeAsync(telefono, mensaje);
        }

        // =====================================================================
        // MÓDULO 4: ENTRENAMIENTO Y RUTINAS
        // =====================================================================

        public Task EnviarNotificacionRutinaAsignada(
            string nombreSocio,
            string telefono,
            string nombreRutina,
            string linkPortal)
        {
            var mensaje =
                $"📋 *GymvoOS* — Nueva rutina asignada\n\n" +
                $"¡Hola *{nombreSocio}*! Tu coach te asignó una nueva rutina:\n" +
                $"💪 *{nombreRutina}*\n\n" +
                $"Podés verla completa en tu portal:\n" +
                $"{linkPortal}\n\n" +
                $"¡A darle con todo! 🔥🏋️";

            return EnviarMensajeAsync(telefono, mensaje);
        }

        public Task EnviarFelicitacionLogro(
            string nombreSocio,
            string telefono,
            int totalAsistencias,
            string nivelActual)
        {
            // Emojis que van escalando con el nivel para hacer el mensaje más especial
            var emoji = nivelActual switch
            {
                "Intermedio" => "⭐",
                "Avanzado" => "🌟",
                "Elite" => "🏆",
                "Leyenda" => "👑",
                _ => "💪"
            };

            var mensaje =
                $"{emoji} *¡LOGRO DESBLOQUEADO!* {emoji}\n\n" +
                $"¡Felicitaciones *{nombreSocio}*!\n" +
                $"Completaste tu asistencia número *{totalAsistencias}*.\n\n" +
                $"🎖️ Nivel actual: *{nivelActual}*\n\n" +
                $"¡Seguí así, sos una máquina! El esfuerzo siempre da sus frutos. 💪🔥";

            return EnviarMensajeAsync(telefono, mensaje);
        }

        // =====================================================================
        // MÓDULO 5: ADMINISTRACIÓN (NOTIFICACIONES AL DUEÑO)
        // =====================================================================

        public Task EnviarAlertaSuscripcionGimnasio(
            string nombreGimnasio,
            string telefonoDueno,
            int diasRestantes,
            string linkRenovacion)
        {
            var urgencia = diasRestantes <= 3 ? "🚨 URGENTE" : "⚠️ Aviso";
            var mensaje =
                $"{urgencia} *GymvoOS Admin* — Hola!\n\n" +
                $"La suscripción de *{nombreGimnasio}* vence en *{diasRestantes} día(s)*.\n\n" +
                $"Para mantener el acceso al sistema sin interrupciones:\n" +
                $"{linkRenovacion}\n\n" +
                $"Ante cualquier consulta, contactá al soporte de Gymvo.";

            return EnviarMensajeAsync(telefonoDueno, mensaje);
        }

        public Task EnviarResumenDiario(
            string nombreGimnasio,
            string telefonoDueno,
            int ingresosHoy,
            int membresíasVendidas,
            decimal recaudacionHoy)
        {
            var fecha = DateTime.Now.ToString("dd/MM/yyyy");
            var mensaje =
                $"📊 *GymvoOS — Resumen del {fecha}*\n" +
                $"🏋️ Gimnasio: *{nombreGimnasio}*\n\n" +
                $"🚪 Ingresos hoy: *{ingresosHoy}*\n" +
                $"💳 Membresías vendidas: *{membresíasVendidas}*\n" +
                $"💰 Recaudación: *${recaudacionHoy:N2}*\n\n" +
                $"¡Buen trabajo! Mañana a seguir creciendo. 💪";

            return EnviarMensajeAsync(telefonoDueno, mensaje);
        }
    }
}