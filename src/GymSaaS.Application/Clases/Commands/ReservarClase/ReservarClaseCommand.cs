// =============================================================================
// ARCHIVO: ReservarClaseCommand.cs
// CAPA: Application/Clases/Commands/ReservarClase
// PROPÓSITO: Maneja la reserva de una clase por parte de un socio desde el portal.
//
// CAMBIOS EN ESTE ARCHIVO:
//   - Se inyecta INotificationService en el handler (NUEVO).
//   - Se agrega llamada a EnviarConfirmacionReserva() después de guardar
//     la reserva exitosamente (NUEVO).
//   - Toda la lógica original de validación, cupo, pago y creación de
//     reserva se conserva INTACTA. No se modificó ni una línea de la lógica.
//
// PATRÓN USADO: "Fire and Forget" para la notificación.
//   Usamos _ = Task.Run(() => ...) para no bloquear el response del usuario
//   mientras WhatsApp procesa el envío. Si la notificación falla, el log
//   lo registra pero la reserva ya quedó guardada.
// =============================================================================

using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GymSaaS.Application.Clases.Commands.ReservarClase
{
    public record ReservarClaseCommand : IRequest<ReservaResultDto>
    {
        public int ClaseId { get; init; }
        public int SocioId { get; init; }
    }

    public class ReservaResultDto
    {
        public int ReservaId { get; set; }
        public bool RequierePago { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    public class ReservarClaseCommandValidator : AbstractValidator<ReservarClaseCommand>
    {
        public ReservarClaseCommandValidator()
        {
            RuleFor(v => v.ClaseId).GreaterThan(0);
            RuleFor(v => v.SocioId).GreaterThan(0);
        }
    }

    public class ReservarClaseCommandHandler : IRequestHandler<ReservarClaseCommand, ReservaResultDto>
    {
        private readonly IApplicationDbContext _context;

        // NUEVO: Inyectamos el servicio de notificaciones y el logger
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReservarClaseCommandHandler> _logger;

        public ReservarClaseCommandHandler(
            IApplicationDbContext context,
            INotificationService notificationService,
            IConfiguration configuration,
            ILogger<ReservarClaseCommandHandler> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ReservaResultDto> Handle(
            ReservarClaseCommand request,
            CancellationToken cancellationToken)
        {
            // ==============================================================
            // LÓGICA ORIGINAL — CONSERVADA INTACTA
            // ==============================================================

            // 1. Obtener Socio para saber su Tenant (CRÍTICO)
            var socio = await _context.Socios
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == request.SocioId, cancellationToken);

            if (socio == null) throw new UnauthorizedAccessException("Socio no encontrado.");

            // 2. Obtener la clase asegurando que sea del MISMO Tenant
            var clase = await _context.Clases
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    c => c.Id == request.ClaseId && c.TenantId == socio.TenantId,
                    cancellationToken);

            if (clase == null || !clase.Activa)
                throw new KeyNotFoundException("La clase no existe o no está disponible.");

            // 3. Validar Cupo
            if (clase.CupoReservado >= clase.CupoMaximo)
                throw new InvalidOperationException("No hay cupos disponibles.");

            // 4. Validar reserva existente
            var existeReserva = await _context.Reservas
                .IgnoreQueryFilters()
                .AnyAsync(r => r.ClaseId == request.ClaseId
                               && r.SocioId == request.SocioId
                               && r.Estado != "Cancelada",
                    cancellationToken);

            if (existeReserva)
                throw new InvalidOperationException("Ya tienes una reserva para esta clase.");

            // 5. Configurar Pago
            bool esPaga = clase.Precio > 0;
            string estadoInicial = esPaga ? "PendientePago" : "Confirmada";

            // 6. Crear Reserva (Asignando TenantId explícitamente)
            var reserva = new Reserva
            {
                ClaseId = request.ClaseId,
                SocioId = request.SocioId,
                FechaReserva = DateTime.UtcNow,
                Asistio = false,
                Monto = clase.Precio,
                Estado = estadoInicial,
                TenantId = socio.TenantId // ¡Importante para el aislamiento Multitenant!
            };

            _context.Reservas.Add(reserva);

            // 7. Actualizar Cupo desnormalizado
            clase.CupoReservado += 1;

            await _context.SaveChangesAsync(cancellationToken);

            // ==============================================================
            // NUEVO: Notificación por WhatsApp SOLO si la reserva fue confirmada
            // (no cuando requiere pago, porque todavía no está confirmada)
            // ==============================================================
            if (!esPaga && !string.IsNullOrEmpty(socio.Telefono))
            {
                // "Fire and Forget": disparamos la notificación sin esperar
                // que termine. El usuario recibe el response de la reserva
                // de inmediato, y el mensaje de WhatsApp llega en segundos.
                _ = EnviarNotificacionReservaAsync(socio, clase);
            }

            return new ReservaResultDto
            {
                ReservaId = reserva.Id,
                RequierePago = esPaga,
                Mensaje = esPaga
                    ? "Reserva iniciada. Se requiere pago."
                    : "Reserva confirmada exitosamente."
            };
        }

        /// <summary>
        /// Método privado que encapsula el envío de la notificación de reserva.
        /// Se llama en "fire and forget" para no bloquear el response HTTP.
        /// Si falla, solo loguea el error — la reserva ya fue guardada.
        /// </summary>
        private async Task EnviarNotificacionReservaAsync(Socio socio, Clase clase)
        {
            try
            {
                await _notificationService.EnviarConfirmacionReserva(
                    nombreSocio: socio.Nombre,
                    telefono: socio.Telefono!,
                    nombreClase: clase.Nombre,
                    instructor: clase.Instructor,
                    fechaHora: clase.FechaHoraInicio);
            }
            catch (Exception ex)
            {
                // Si WhatsApp falla, logueamos pero NO rompemos la reserva
                _logger.LogError(ex,
                    "Error enviando confirmación de reserva por WhatsApp al socio {SocioId}",
                    socio.Id);
            }
        }
    }
}