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

        // Nuevo Campo
        public decimal Precio { get; init; }
    }

    public class CreateClaseCommandValidator : AbstractValidator<CreateClaseCommand>
    {
        public CreateClaseCommandValidator()
        {
            RuleFor(v => v.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.");

            RuleFor(v => v.FechaHoraInicio)
                .NotEqual(DateTime.MinValue).WithMessage("Fecha inválida.");

            RuleFor(v => v.DuracionMinutos)
                .GreaterThan(15).WithMessage("Mínimo 15 minutos.");

            RuleFor(v => v.CupoMaximo)
                .GreaterThan(0).WithMessage("Cupo debe ser mayor a 0.");

            // Validación de Precio
            RuleFor(v => v.Precio)
                .GreaterThanOrEqualTo(0).WithMessage("El precio no puede ser negativo.");
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
                CupoReservado = 0,
                Activa = true,

                // Guardamos el precio
                Precio = request.Precio
            };

            _context.Clases.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
    }
}