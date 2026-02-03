using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;

namespace GymSaaS.Application.Ejercicios.Commands.CreateEjercicio
{
    // 1. Objeto de Petición (Request)
    public record CreateEjercicioCommand : IRequest<int>
    {
        public string Nombre { get; init; } = string.Empty;
        public string? GrupoMuscular { get; init; }
        public string? VideoUrl { get; init; }
        public string? Descripcion { get; init; }
    }

    // 2. Validador (Reglas de Negocio)
    public class CreateEjercicioCommandValidator : AbstractValidator<CreateEjercicioCommand>
    {
        public CreateEjercicioCommandValidator()
        {
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

    // 3. Manejador (Handler)
    public class CreateEjercicioCommandHandler : IRequestHandler<CreateEjercicioCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateEjercicioCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateEjercicioCommand request, CancellationToken cancellationToken)
        {
            var entity = new Ejercicio
            {
                Nombre = request.Nombre,
                GrupoMuscular = request.GrupoMuscular,
                VideoUrl = request.VideoUrl,
                Descripcion = request.Descripcion
            };

            // TenantId se asigna automáticamente por el DbContext
            _context.Ejercicios.Add(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
    }
}