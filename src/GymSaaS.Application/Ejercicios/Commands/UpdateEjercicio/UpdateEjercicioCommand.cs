using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Ejercicios.Commands.UpdateEjercicio
{
    public record UpdateEjercicioCommand : IRequest
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string? GrupoMuscular { get; init; }
        public string? VideoUrl { get; init; }
        public string? Descripcion { get; init; }
    }

    public class UpdateEjercicioCommandValidator : AbstractValidator<UpdateEjercicioCommand>
    {
        public UpdateEjercicioCommandValidator()
        {
            RuleFor(v => v.Id).GreaterThan(0);

            RuleFor(v => v.Nombre)
                .NotEmpty().WithMessage("El nombre del ejercicio es obligatorio.")
                .MaximumLength(200);

            RuleFor(v => v.GrupoMuscular)
                .MaximumLength(100);

            RuleFor(v => v.VideoUrl)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .When(v => !string.IsNullOrEmpty(v.VideoUrl))
                .WithMessage("La URL del video no es válida.");
        }
    }

    public class UpdateEjercicioCommandHandler : IRequestHandler<UpdateEjercicioCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateEjercicioCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateEjercicioCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.Ejercicios
                .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

            if (entity == null) throw new KeyNotFoundException($"Ejercicio {request.Id} no encontrado.");

            entity.Nombre = request.Nombre;
            entity.GrupoMuscular = request.GrupoMuscular;
            entity.VideoUrl = request.VideoUrl;
            entity.Descripcion = request.Descripcion;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
