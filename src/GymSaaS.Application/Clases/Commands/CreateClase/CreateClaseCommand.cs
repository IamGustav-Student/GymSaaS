using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;

namespace GymSaaS.Application.Clases.Commands.CreateClase
{
    public record CreateClaseCommand : IRequest<int>
    {
        public string Nombre { get; init; } = string.Empty;
        public string? Instructor { get; init; }
        public DateTime FechaHoraInicio { get; init; }
        public int DuracionMinutos { get; init; }
        public int CupoMaximo { get; init; }
    }

    public class CreateClaseCommandValidator : AbstractValidator<CreateClaseCommand>
    {
        public CreateClaseCommandValidator()
        {
            RuleFor(v => v.Nombre).NotEmpty();

            RuleFor(v => v.FechaHoraInicio)
                .GreaterThan(DateTime.Now).WithMessage("La clase debe programarse en el futuro.");

            RuleFor(v => v.DuracionMinutos)
                .GreaterThan(15).WithMessage("La duración mínima es de 15 minutos.");

            RuleFor(v => v.CupoMaximo)
                .GreaterThan(0).WithMessage("El cupo debe ser mayor a 0.");
        }
    }

    public class CreateClaseCommandHandler : IRequestHandler<CreateClaseCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateClaseCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateClaseCommand request, CancellationToken cancellationToken)
        {
            var entity = new Clase
            {
                Nombre = request.Nombre,
                Instructor = request.Instructor,
                FechaHoraInicio = request.FechaHoraInicio,
                DuracionMinutos = request.DuracionMinutos,
                CupoMaximo = request.CupoMaximo,
                CupoReservado = 0, // Inicia vacía
                Activa = true
            };

            _context.Clases.Add(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
    }
}