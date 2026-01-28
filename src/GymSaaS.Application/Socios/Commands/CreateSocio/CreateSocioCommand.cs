using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Socios.Commands.CreateSocio
{
    // 1. Comando (Datos del Formulario)
    public record CreateSocioCommand : IRequest<int>
    {
        public string Nombre { get; init; } = string.Empty;
        public string Apellido { get; init; } = string.Empty;
        public string Dni { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? Telefono { get; init; }
    }

    // 2. Validador (Reglas de Negocio)
    public class CreateSocioCommandValidator : AbstractValidator<CreateSocioCommand>
    {
        private readonly IApplicationDbContext _context;

        public CreateSocioCommandValidator(IApplicationDbContext context)
        {
            _context = context;

            RuleFor(v => v.Nombre).NotEmpty().MaximumLength(50);
            RuleFor(v => v.Apellido).NotEmpty().MaximumLength(50);
            RuleFor(v => v.Dni)
                .NotEmpty().WithMessage("El DNI es obligatorio.")
                .Length(7, 12).WithMessage("DNI inválido.");

            RuleFor(v => v.Email).NotEmpty().EmailAddress();

            // Validación de DNI Único (Crucial para no duplicar gente en el mismo gym)
            RuleFor(v => v.Dni).MustAsync(BeUniqueDni).WithMessage("Ya existe un socio con este DNI.");
        }

        private async Task<bool> BeUniqueDni(string dni, CancellationToken cancellationToken)
        {
            // El Global Query Filter se asegura de revisar solo dentro de ESTE gimnasio
            return !await _context.Socios.AnyAsync(s => s.Dni == dni, cancellationToken);
        }
    }

    // 3. Handler (Ejecución)
    public class CreateSocioCommandHandler : IRequestHandler<CreateSocioCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateSocioCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateSocioCommand request, CancellationToken cancellationToken)
        {
            var entity = new Socio
            {
                Nombre = request.Nombre,
                Apellido = request.Apellido,
                Dni = request.Dni,
                Email = request.Email,
                Telefono = request.Telefono,
                Activo = true
                // TenantId se inyecta solo al guardar (ver ApplicationDbContext)
            };

            _context.Socios.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
    }
}