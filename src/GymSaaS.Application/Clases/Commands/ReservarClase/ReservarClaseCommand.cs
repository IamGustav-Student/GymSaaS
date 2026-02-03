using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Clases.Commands.ReservarClase
{
    public record ReservarClaseCommand : IRequest<int>
    {
        public int ClaseId { get; init; }
        public int SocioId { get; init; }
    }

    public class ReservarClaseCommandValidator : AbstractValidator<ReservarClaseCommand>
    {
        public ReservarClaseCommandValidator()
        {
            RuleFor(v => v.ClaseId).GreaterThan(0);
            RuleFor(v => v.SocioId).GreaterThan(0);
        }
    }

    public class ReservarClaseCommandHandler : IRequestHandler<ReservarClaseCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public ReservarClaseCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(ReservarClaseCommand request, CancellationToken cancellationToken)
        {
            // 1. Obtener la clase (incluyendo validación de fecha si fuera necesario)
            var clase = await _context.Clases
                .FirstOrDefaultAsync(c => c.Id == request.ClaseId, cancellationToken);

            if (clase == null || !clase.Activa)
            {
                throw new KeyNotFoundException($"La clase {request.ClaseId} no existe o no está activa.");
            }

            // 2. Validar Cupo Disponible
            if (clase.CupoReservado >= clase.CupoMaximo)
            {
                throw new InvalidOperationException("No hay cupos disponibles para esta clase.");
            }

            // 3. Validar que el socio no esté ya inscripto
            var existeReserva = await _context.Reservas
                .AnyAsync(r => r.ClaseId == request.ClaseId && r.SocioId == request.SocioId, cancellationToken);

            if (existeReserva)
            {
                throw new InvalidOperationException("El socio ya tiene una reserva para esta clase.");
            }

            // 4. Crear la Reserva
            var reserva = new Reserva
            {
                ClaseId = request.ClaseId,
                SocioId = request.SocioId,
                FechaReserva = DateTime.UtcNow,
                Asistio = false
            };

            _context.Reservas.Add(reserva);

            // 5. Actualizar el contador de cupos (Optimista)
            clase.CupoReservado += 1;

            await _context.SaveChangesAsync(cancellationToken);

            return reserva.Id;
        }
    }
}