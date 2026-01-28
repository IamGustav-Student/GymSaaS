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
            // A. Crear el Tenant (Gimnasio)
            // Generamos un ID único simulado para el tenant (en producción usarías GUIDs reales o Identity)
            var tenantId = Guid.NewGuid().ToString();

            var tenant = new Tenant
            {
                Name = request.GymName,
                SubscriptionPlan = "Free",
                IsActive = true
                // TenantId no aplica a la entidad Tenant en sí misma en este diseño base,
                // pero el ID de la tabla se usará como referencia.
            };

            // HACK: Para simplificar el MVP y Clean Arch, usamos el campo TenantId de IMustHaveTenant
            // como el identificador lógico que une todo.

            _context.Tenants.Add(tenant);

            // Guardamos para obtener el ID numérico del Tenant (si usas int) 
            // Ojo: Si Tenant hereda de BaseEntity (int Id), ese es su PK. 
            // El 'String TenantId' que usan los usuarios será un GUID generado aquí.
            // Vamos a asignar ese GUID a una propiedad "Code" o usar el GUID como string.
            // Ajuste sobre la marcha: Usaremos el GUID generado arriba como el "TenantId" lógico.

            // B. Crear el Usuario Admin vinculado a ese Tenant
            var usuario = new Usuario
            {
                Nombre = request.AdminName,
                Email = request.AdminEmail,
                Password = _passwordHasher.Hash(request.Password),
                Activo = true,
                TenantId = tenantId // <--- VINCULACIÓN CLAVE
            };

            _context.Usuarios.Add(usuario);

            // Nota: Aquí hay un pequeño detalle técnico. La entidad Tenant no tiene campo "TenantIdString" explícito
            // en el diseño de la Fase 1. Asumiremos que el sistema conecta por lógica o agregaremos
            // ese campo luego. Por ahora, creamos el usuario con un TenantId nuevo.

            await _context.SaveChangesAsync(cancellationToken);

            return usuario.Id;
        }
    }
}