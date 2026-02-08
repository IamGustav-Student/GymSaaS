using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Clases.Commands.CancelarReserva
{
    public record CancelarReservaCommand : IRequest<bool>
    {
        public int ReservaId { get; init; }
        public int SocioId { get; init; }
    }

    public class CancelarReservaCommandHandler : IRequestHandler<CancelarReservaCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public CancelarReservaCommandHandler(IApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<bool> Handle(CancelarReservaCommand request, CancellationToken cancellationToken)
        {
            // 1. Obtener la reserva y validar propiedad
            // Buscamos por ReservaId si viene > 0, sino intentamos buscar por contexto (menos común aquí, pero seguro)
            var reserva = await _context.Reservas
                .Include(r => r.Clase)
                .FirstOrDefaultAsync(r => r.Id == request.ReservaId && r.SocioId == request.SocioId, cancellationToken);

            if (reserva == null) return false;

            var claseId = reserva.ClaseId;
            var nombreClase = reserva.Clase?.Nombre ?? "Clase";
            var fechaClase = reserva.Clase?.FechaHoraInicio ?? DateTime.Now;

            // 2. Eliminar la reserva (Liberar cupo)
            // Opción A: Borrado físico
            _context.Reservas.Remove(reserva);

            // Opción B (Soft Delete) si prefieres mantener historial:
            // reserva.Activa = false; 

            // Actualizar contador desnormalizado si existe
            if (reserva.Clase != null)
            {
                reserva.Clase.CupoReservado = Math.Max(0, reserva.Clase.CupoReservado - 1);
            }

            // 3. LÓGICA DE PROMOCIÓN AUTOMÁTICA (WAITLIST)
            var siguienteEnEspera = await _context.ListasEspera
                .Include(l => l.Socio)
                .Where(l => l.ClaseId == claseId)
                .OrderBy(l => l.FechaRegistro) // FIFO
                .FirstOrDefaultAsync(cancellationToken);

            if (siguienteEnEspera != null)
            {
                // A. Crear reserva para el afortunado
                var nuevaReserva = new Reserva
                {
                    ClaseId = claseId,
                    SocioId = siguienteEnEspera.SocioId,
                    FechaReserva = DateTime.UtcNow,
                    Activa = true, // <--- AHORA SÍ COMPILA
                    Asistio = false,
                    Pagado = false // Asumimos pago en recepción o pase libre por ahora
                };

                _context.Reservas.Add(nuevaReserva);
                if (reserva.Clase != null) reserva.Clase.CupoReservado++;

                // B. Eliminar de la lista de espera
                _context.ListasEspera.Remove(siguienteEnEspera);

                // C. Notificar
                if (siguienteEnEspera.Socio != null && !string.IsNullOrEmpty(siguienteEnEspera.Socio.Telefono))
                {
                    var mensaje = $"¡Buenas noticias {siguienteEnEspera.Socio.Nombre}! Se liberó un cupo en {nombreClase} ({fechaClase:HH:mm}) y te hemos inscripto automáticamente.";
                    _ = _notificationService.EnviarNotificacion(siguienteEnEspera.Socio.Telefono, mensaje);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}