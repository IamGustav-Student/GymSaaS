using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;

namespace GymSaaS.Application.Rutinas.Commands.CreateRutina
{
    public record CreateRutinaCommand : IRequest<int>
    {
        public string Nombre { get; init; } = string.Empty;
        public int SocioId { get; init; }
        public DateTime? FechaFin { get; init; }

        // Lista de ejercicios a agregar
        public List<RutinaEjercicioCommandDto> Ejercicios { get; init; } = new();
    }

    public class RutinaEjercicioCommandDto
    {
        public int EjercicioId { get; set; }
        public int Series { get; set; }
        public int Repeticiones { get; set; }
        public string? PesoSugerido { get; set; }
        public string? Notas { get; set; }
    }

    public class CreateRutinaCommandValidator : AbstractValidator<CreateRutinaCommand>
    {
        public CreateRutinaCommandValidator()
        {
            RuleFor(v => v.Nombre).NotEmpty().WithMessage("El nombre de la rutina es obligatorio.");
            RuleFor(v => v.SocioId).GreaterThan(0).WithMessage("Debes seleccionar un socio.");
            RuleFor(v => v.Ejercicios)
                .NotEmpty().WithMessage("La rutina debe tener al menos un ejercicio asignado.");
        }
    }

    public class CreateRutinaCommandHandler : IRequestHandler<CreateRutinaCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateRutinaCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateRutinaCommand request, CancellationToken cancellationToken)
        {
            var entity = new Rutina
            {
                Nombre = request.Nombre,
                SocioId = request.SocioId,
                FechaAsignacion = DateTime.UtcNow,
                FechaFin = request.FechaFin
            };

            // Agregamos los ejercicios relacionados
            if (request.Ejercicios != null)
            {
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
            }

            _context.Rutinas.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
    }
}