using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Auth.Commands.RegisterTenant
{
    public record RegisterTenantCommand : IRequest<int>
    {
        public string GymName { get; init; } = string.Empty;
        public string AdminName { get; init; } = string.Empty;
        public string AdminEmail { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        // NUEVA IMPLEMENTACIÓN: Captura del plan seleccionado desde el frontend
        public string SelectedPlan { get; init; } = string.Empty;
    }

    public class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
    {
        public RegisterTenantCommandValidator()
        {
            RuleFor(v => v.GymName).NotEmpty().WithMessage("El nombre del gimnasio es obligatorio.");
            RuleFor(v => v.AdminName).NotEmpty().WithMessage("El nombre del administrador es obligatorio.");
            RuleFor(v => v.AdminEmail).NotEmpty().EmailAddress().WithMessage("El email no es válido.");
            RuleFor(v => v.Password).MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");
            RuleFor(v => v.SelectedPlan).NotEmpty().WithMessage("El plan de suscripción es obligatorio.");
        }
    }

    public class RegisterTenantCommandHandler : IRequestHandler<RegisterTenantCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public RegisterTenantCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<int> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
        {
            // NUEVA IMPLEMENTACIÓN: Traducción de la elección del usuario a reglas de negocio
            // -------------------------------------------------------------------------
            var planType = request.SelectedPlan switch
            {
                "Basic" => PlanType.Basic,
                "Pro" => PlanType.Pro,
                "Enterprise" => PlanType.Enterprise,
                _ => PlanType.Free
            };

            int? limiteSocios = planType switch
            {
                PlanType.Free => 50,
                PlanType.Basic => 100,
                PlanType.Pro => 500,
                PlanType.Enterprise => null, // Ilimitado
                _ => 50
            };

            // PASO 1: Crear el Tenant con su configuración de plan
            // -------------------------------------------------------------------------
            var tenant = new Tenant
            {
                Name = request.GymName,
                Code = request.GymName.ToLower().Replace(" ", "-").Trim(),
                SubscriptionPlan = request.SelectedPlan,
                Plan = planType,
                MaxSocios = limiteSocios,

                // REGLA DE NEGOCIO: 
                // - Todos los gimnasios inician en estado Trial por 14 días.
                IsActive = true,
                Status = SubscriptionStatus.Trial,
                HasUsedTrial = true,

                TrialEndsAt = DateTime.UtcNow.AddDays(14),

                // La fecha de fin de suscripción coincide con el fin del trial inicialmente
                SubscriptionEndsAt = DateTime.UtcNow.AddDays(14)
            };

            try
            {
                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al crear el Gimnasio '{request.GymName}': {ex.InnerException?.Message ?? ex.Message}");
            }

            // PASO 2: Crear el Usuario Admin vinculado
            // -------------------------------------------------------------------------
            try
            {
                var usuario = new Usuario
                {
                    Nombre = request.AdminName,
                    Email = request.AdminEmail,
                    Password = _passwordHasher.Hash(request.Password),
                    Activo = true,
                    Role = "Admin",
                    TenantId = tenant.Id.ToString()
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync(cancellationToken);

                return usuario.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"Gimnasio creado (ID: {tenant.Id}), pero falló la creación del Usuario Admin: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}