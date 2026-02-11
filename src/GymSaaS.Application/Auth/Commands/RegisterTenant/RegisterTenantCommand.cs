using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore; // Necesario para detectar errores de DB

namespace GymSaaS.Application.Auth.Commands.RegisterTenant
{
    public record RegisterTenantCommand : IRequest<int>
    {
        public string GymName { get; init; } = string.Empty;
        public string AdminName { get; init; } = string.Empty;
        public string AdminEmail { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    public class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
    {
        public RegisterTenantCommandValidator()
        {
            RuleFor(v => v.GymName).NotEmpty().WithMessage("El nombre del gimnasio es obligatorio.");
            RuleFor(v => v.AdminName).NotEmpty().WithMessage("El nombre del administrador es obligatorio.");
            RuleFor(v => v.AdminEmail).NotEmpty().EmailAddress().WithMessage("El email no es válido.");
            RuleFor(v => v.Password).MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");
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
            // PASO 1: Crear el Tenant (Gimnasio)
            // ---------------------------------------------------------
            var tenantCode = Guid.NewGuid().ToString();

            var tenant = new Tenant
            {
                Name = request.GymName,
                Code = tenantCode,
                IsActive = true,
                TimeZoneId = "Argentina Standard Time",
                Plan = PlanType.PruebaGratuita,
                Status = SubscriptionStatus.Trialing,
                MaxSocios = 50,
                TrialEndsAt = DateTime.UtcNow.AddDays(14),
                SubscriptionEndsAt = DateTime.UtcNow.AddDays(14)
            };

            try
            {
                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Si falla aquí, es problema del Tenant (ej: Nombre duplicado si es único)
                throw new Exception($"Error al crear el Gimnasio '{request.GymName}': {ex.InnerException?.Message ?? ex.Message}");
            }

            // PASO 2: Crear el Usuario Admin vinculado
            // ---------------------------------------------------------
            try
            {
                var usuario = new Usuario
                {
                    Nombre = request.AdminName,
                    Email = request.AdminEmail,
                    Password = _passwordHasher.Hash(request.Password),
                    Activo = true,
                    Role = "Admin",
                    TenantId = tenant.Id.ToString() // Vinculación crítica
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync(cancellationToken);

                return usuario.Id;
            }
            catch (Exception ex)
            {
                // Si falla aquí, el Tenant quedó huérfano. 
                // DIAGNÓSTICO: Mostramos exactamente por qué falló el usuario.
                throw new Exception($"Gimnasio creado (ID: {tenant.Id}), pero falló la creación del Usuario Admin: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}