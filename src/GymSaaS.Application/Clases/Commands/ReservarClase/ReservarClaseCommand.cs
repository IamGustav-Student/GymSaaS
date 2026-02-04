using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

        public ReservarClaseCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReservaResultDto> Handle(ReservarClaseCommand request, CancellationToken cancellationToken)
        {
            // 1. Obtener Socio para saber su Tenant (CRÍTICO)
            var socio = await _context.Socios
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == request.SocioId, cancellationToken);

            if (socio == null) throw new UnauthorizedAccessException("Socio no encontrado.");

            // 2. Obtener la clase asegurando que sea del MISMO Tenant
            var clase = await _context.Clases
                .IgnoreQueryFilters() // Bypass filtro global
                .FirstOrDefaultAsync(c => c.Id == request.ClaseId && c.TenantId == socio.TenantId, cancellationToken);

            if (clase == null || !clase.Activa)
            {
                throw new KeyNotFoundException($"La clase no existe o no está disponible.");
            }

            // 3. Validar Cupo
            if (clase.CupoReservado >= clase.CupoMaximo)
            {
                throw new InvalidOperationException("No hay cupos disponibles.");
            }

            // 4. Validar reserva existente
            var existeReserva = await _context.Reservas
                .IgnoreQueryFilters()
                .AnyAsync(r => r.ClaseId == request.ClaseId
                               && r.SocioId == request.SocioId
                               && r.Estado != "Cancelada", cancellationToken);

            if (existeReserva)
            {
                throw new InvalidOperationException("Ya tienes una reserva para esta clase.");
            }

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
                TenantId = socio.TenantId // ¡Importante!
            };

            _context.Reservas.Add(reserva);

            // 7. Actualizar Cupo
            clase.CupoReservado += 1;

            await _context.SaveChangesAsync(cancellationToken);

            return new ReservaResultDto
            {
                ReservaId = reserva.Id,
                RequierePago = esPaga,
                Mensaje = esPaga ? "Reserva iniciada. Se requiere pago." : "Reserva confirmada exitosamente."
            };
        }
    }
}