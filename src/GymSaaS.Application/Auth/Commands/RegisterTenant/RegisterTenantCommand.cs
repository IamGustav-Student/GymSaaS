using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;

namespace GymSaaS.Application.Auth.Commands.RegisterTenant
{
    // 1. El "Formulario" de datos que recibimos
    public record RegisterTenantCommand : IRequest<int>
    {
        public string GymName { get; init; } = string.Empty;
        public string AdminName { get; init; } = string.Empty;
        public string AdminEmail { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    // 2. Validaciones (Reglas de negocio)
    public class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
    {
        public RegisterTenantCommandValidator()
        {
            RuleFor(v => v.GymName).NotEmpty().WithMessage("El nombre del gimnasio es obligatorio.");
            RuleFor(v => v.AdminName).NotEmpty();
            RuleFor(v => v.AdminEmail).NotEmpty().EmailAddress();
            RuleFor(v => v.Password).MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");
        }
    }

    // 3. La Lógica (Handler)
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
            // A. Generar el ID Lógico del Tenant (Code)
            // Este GUID será el que vincule a todos los usuarios y datos de este gimnasio.
            var tenantCode = Guid.NewGuid().ToString();

            // B. Crear el Tenant (Gimnasio)
            var tenant = new Tenant
            {
                Name = request.GymName,
                Code = tenantCode, // <--- CORRECCIÓN CRÍTICA: Guardamos el código generado
                SubscriptionPlan = "Free",
                IsActive = true
            };

            _context.Tenants.Add(tenant);

            // C. Crear el Usuario Admin vinculado a ese Tenant Code
            var usuario = new Usuario
            {
                Nombre = request.AdminName,
                Email = request.AdminEmail,
                Password = _passwordHasher.Hash(request.Password),
                Activo = true,
                TenantId = tenantCode // <--- VINCULACIÓN CORRECTA: Ahora apunta al Code del Tenant
            };

            _context.Usuarios.Add(usuario);

            await _context.SaveChangesAsync(cancellationToken);

            return usuario.Id;
        }
    }
}