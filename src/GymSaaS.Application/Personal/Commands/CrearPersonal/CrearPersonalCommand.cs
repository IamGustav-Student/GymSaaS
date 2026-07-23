using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Common;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Personal.Commands.CrearPersonal
{
    // Alta de un empleado (hoy solo rol Recepcionista) dentro del gimnasio actual.
    public record CrearPersonalCommand : IRequest<int>
    {
        public string Nombre { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    public class CrearPersonalCommandValidator : AbstractValidator<CrearPersonalCommand>
    {
        public CrearPersonalCommandValidator(IApplicationDbContext context)
        {
            RuleFor(v => v.Nombre).NotEmpty().MaximumLength(100);
            RuleFor(v => v.Email).NotEmpty().EmailAddress();
            RuleFor(v => v.Password).MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");

            RuleFor(v => v.Email)
                .MustAsync(async (email, cancellation) => !await context.Usuarios
                    .IgnoreQueryFilters()
                    .AnyAsync(u => u.Email == email, cancellation))
                .WithMessage("Ya existe un usuario con ese email.");
        }
    }

    public class CrearPersonalCommandHandler : IRequestHandler<CrearPersonalCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICurrentTenantService _currentTenantService;

        public CrearPersonalCommandHandler(
            IApplicationDbContext context,
            IPasswordHasher passwordHasher,
            ICurrentTenantService currentTenantService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _currentTenantService = currentTenantService;
        }

        public async Task<int> Handle(CrearPersonalCommand request, CancellationToken cancellationToken)
        {
            var tenantId = _currentTenantService.TenantId;
            if (string.IsNullOrEmpty(tenantId)) throw new UnauthorizedAccessException();

            var usuario = new Usuario
            {
                Nombre = request.Nombre,
                Email = request.Email,
                Password = _passwordHasher.Hash(request.Password),
                Activo = true,
                Role = Roles.Recepcionista,
                TenantId = tenantId
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync(cancellationToken);

            return usuario.Id;
        }
    }
}
