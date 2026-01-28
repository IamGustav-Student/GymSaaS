using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Membresias.Commands.AsignarMembresia
{
    public record AsignarMembresiaCommand : IRequest<int>
    {
        public int SocioId { get; init; }
        public int TipoMembresiaId { get; init; }
        public string MetodoPago { get; init; } = "Efectivo"; // Efectivo por defecto
    }

    public class AsignarMembresiaCommandValidator : AbstractValidator<AsignarMembresiaCommand>
    {
        public AsignarMembresiaCommandValidator()
        {
            RuleFor(v => v.SocioId).GreaterThan(0);
            RuleFor(v => v.TipoMembresiaId).GreaterThan(0);
        }
    }

    public class AsignarMembresiaCommandHandler : IRequestHandler<AsignarMembresiaCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public AsignarMembresiaCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(AsignarMembresiaCommand request, CancellationToken cancellationToken)
        {
            // 1. Obtener datos del plan seleccionado
            var plan = await _context.TiposMembresia
                .FindAsync(new object[] { request.TipoMembresiaId }, cancellationToken);

            if (plan == null) throw new KeyNotFoundException("Plan no encontrado");

            // 2. Crear la Membresía para el socio
            var fechaInicio = DateTime.UtcNow; // O local si ajustamos timezone
            var fechaFin = fechaInicio.AddDays(plan.DuracionDias);

            var membresia = new MembresiaSocio
            {
                SocioId = request.SocioId,
                TipoMembresiaId = request.TipoMembresiaId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                PrecioPagado = plan.Precio,
                ClasesRestantes = plan.CantidadClases, // Si es null, es ilimitado
                Activa = true
            };

            _context.MembresiasSocios.Add(membresia);

            // 3. Registrar el PAGO automáticamente (Caja Chica)
            var pago = new Pago
            {
                SocioId = request.SocioId,
                Monto = plan.Precio,
                FechaPago = DateTime.UtcNow,
                MetodoPago = request.MetodoPago
            };

            _context.Pagos.Add(pago);

            // Guardar todo en una sola transacción
            await _context.SaveChangesAsync(cancellationToken);

            return membresia.Id;
        }
    }
}