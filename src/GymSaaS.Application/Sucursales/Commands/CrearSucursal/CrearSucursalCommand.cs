using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Sucursales.Commands.CrearSucursal
{
    // Crea una nueva sucursal (Tenant independiente: suscripción, socios y
    // membresías propias) bajo la misma Empresa del admin actual, y clona su
    // usuario admin (mismo email/contraseña) para que pueda entrar a ambas.
    public record CrearSucursalCommand : IRequest<int>
    {
        public int UsuarioActualId { get; init; }
        public string NombreSucursal { get; init; } = string.Empty;
    }

    public class CrearSucursalCommandValidator : AbstractValidator<CrearSucursalCommand>
    {
        public CrearSucursalCommandValidator()
        {
            RuleFor(v => v.UsuarioActualId).GreaterThan(0);
            RuleFor(v => v.NombreSucursal).NotEmpty().MaximumLength(100);
        }
    }

    public class CrearSucursalCommandHandler : IRequestHandler<CrearSucursalCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentTenantService _currentTenantService;

        public CrearSucursalCommandHandler(IApplicationDbContext context, ICurrentTenantService currentTenantService)
        {
            _context = context;
            _currentTenantService = currentTenantService;
        }

        public async Task<int> Handle(CrearSucursalCommand request, CancellationToken cancellationToken)
        {
            var tenantIdStr = _currentTenantService.TenantId;
            if (string.IsNullOrEmpty(tenantIdStr) || !int.TryParse(tenantIdStr, out var tenantId))
                throw new UnauthorizedAccessException();

            var tenantActual = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            if (tenantActual == null) throw new KeyNotFoundException("Gimnasio actual no encontrado.");

            var adminActual = await _context.Usuarios
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == request.UsuarioActualId && u.TenantId == tenantIdStr, cancellationToken);

            if (adminActual == null) throw new UnauthorizedAccessException("El usuario actual no pertenece a este gimnasio.");

            // Si el gimnasio todavía no tenía Empresa (creado antes de esta funcionalidad), se crea ahora.
            if (tenantActual.EmpresaId == null)
            {
                var empresaNueva = new Empresa { Nombre = tenantActual.Name };
                _context.Empresas.Add(empresaNueva);
                await _context.SaveChangesAsync(cancellationToken);
                tenantActual.EmpresaId = empresaNueva.Id;
            }

            var nuevaSucursal = new Tenant
            {
                Name = request.NombreSucursal,
                Code = $"{request.NombreSucursal.ToLower().Replace(" ", "-").Trim()}-{Guid.NewGuid().ToString()[..6]}",
                EmpresaId = tenantActual.EmpresaId,

                // Cada sucursal es independiente: suscripción propia, arranca en trial.
                Plan = PlanType.Free,
                MaxSocios = 50,
                IsActive = true,
                Status = SubscriptionStatus.Trial,
                HasUsedTrial = true,
                TrialEndsAt = DateTime.UtcNow.AddDays(14),
                SubscriptionEndsAt = DateTime.UtcNow.AddDays(14)
            };

            _context.Tenants.Add(nuevaSucursal);
            await _context.SaveChangesAsync(cancellationToken);

            var adminClonado = new Usuario
            {
                Nombre = adminActual.Nombre,
                Email = adminActual.Email,
                Password = adminActual.Password, // ya hasheada: mismas credenciales en ambas sucursales
                Activo = true,
                Role = "Admin",
                TenantId = nuevaSucursal.Id.ToString()
            };

            _context.Usuarios.Add(adminClonado);
            await _context.SaveChangesAsync(cancellationToken);

            return nuevaSucursal.Id;
        }
    }
}
