using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Application.Socios.Commands.CreateSocio;
using Microsoft.EntityFrameworkCore; // <--- AGREGAR ESTA LÍNEA

public class CreateSocioCommandValidator : AbstractValidator<CreateSocioCommand>
{
    public CreateSocioCommandValidator(IApplicationDbContext context)
    {
        // 1. Validaciones de Formato
        RuleFor(v => v.Nombre).NotEmpty().MaximumLength(50);
        RuleFor(v => v.Apellido).NotEmpty().MaximumLength(50);
        RuleFor(v => v.Email).NotEmpty().EmailAddress();

        // 2. Validación de DNI Único (SOLO UNA VEZ)
        RuleFor(v => v.Dni)
            .NotEmpty()
            .MustAsync(async (dni, cancellation) =>
            {
                return !await context.Socios.AnyAsync(s => s.Dni == dni, cancellation);
            })
            .WithMessage("Ya existe un socio registrado con este DNI."); // Mensaje único y claro
    }
}