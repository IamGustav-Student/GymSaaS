using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Application.Rutinas.Commands.CreateRutina; // Reutilizamos DTOs
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Rutinas.Commands.UpdateRutina
{
    public record UpdateRutinaCommand : IRequest
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public int SocioId { get; init; }
        public DateTime? FechaFin { get; init; }
        public List<RutinaEjercicioCommandDto> Ejercicios { get; init; } = new();
    }

    public class UpdateRutinaCommandValidator : AbstractValidator<UpdateRutinaCommand>
    {
        public UpdateRutinaCommandValidator()
        {
            RuleFor(v => v.Id).GreaterThan(0);
            RuleFor(v => v.Nombre).NotEmpty().WithMessage("Nombre requerido.");
            RuleFor(v => v.SocioId).GreaterThan(0).WithMessage("Socio requerido.");
            RuleFor(v => v.Ejercicios).NotEmpty().WithMessage("Agrega al menos un ejercicio.");
        }
    }

    public class UpdateRutinaCommandHandler : IRequestHandler<UpdateRutinaCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateRutinaCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateRutinaCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.Rutinas
                .Include(r => r.RutinaEjercicios)
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

            if (entity == null) throw new KeyNotFoundException($"Rutina {request.Id} no encontrada.");

            // 1. Actualizar Cabecera
            entity.Nombre = request.Nombre;
            entity.SocioId = request.SocioId;
            entity.FechaFin = request.FechaFin;

            // 2. Actualizar Detalles (Borrón y Cuenta Nueva)
            _context.RutinaEjercicios.RemoveRange(entity.RutinaEjercicios);

            foreach (var item in request.Ejercicios)
            {
                entity.RutinaEjercicios.Add(new RutinaEjercicio
                {
                    EjercicioId = item.EjercicioId,
                    Series = item.Series,
                    Repeticiones = item.Repeticiones,
                    PesoSugerido = item.PesoSugerido,
                    Notas = item.Notas
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
