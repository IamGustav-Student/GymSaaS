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
        private readonly ICurrentTenantService _tenantService;

        public CreateSocioCommandHandler(IApplicationDbContext context, ICurrentTenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<int> Handle(CreateSocioCommand request, CancellationToken cancellationToken)
        {
            // 1. VERIFICACIÓN DEL PLAN (Límite de 50)
            // Aquí podrías leer el Tipo de Plan desde una configuración. 
            // Por ahora, asumimos que todos empiezan en Free y validamos.

            // Contamos socios actuales (excluyendo borrados lógicos si los hubiera)
            var cantidadSocios = await _context.Socios.CountAsync(cancellationToken);

            // LIMITE HARDCODED PARA PLAN "DESPEGUE"
            // En el futuro, esto vendría de: if (tenant.Plan == "Despegue" && cantidad >= 50)
            if (cantidadSocios >= 50)
            {
                // Lanzamos excepción que el controlador deberá atrapar para mostrar el mensaje de venta
                throw new InvalidOperationException("Has alcanzado el límite de 50 socios del Plan Despegue. ¡Actualiza a Ilimitado para seguir creciendo!");
            }

            // 2. Creación del Socio
            var entity = new Socio
            {
                Nombre = request.Nombre,
                Apellido = request.Apellido,
                Dni = request.Dni,
                Email = request.Email,
                Telefono = request.Telefono,
                CodigoAcceso = Guid.NewGuid().ToString(), // Generamos el QR único
                TenantId = _tenantService.TenantId
            };

            _context.Socios.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
    }
}    