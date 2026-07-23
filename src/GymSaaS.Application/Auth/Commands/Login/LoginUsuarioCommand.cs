using FluentValidation;
using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Auth.Commands.Login
{
    // DTO de Respuesta
    public record LoginResultDto
    {
        public int UsuarioId { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string TenantId { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
    }

    // Comando
    public record LoginUsuarioCommand : IRequest<LoginResultDto>
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    // Validador
    public class LoginUsuarioCommandValidator : AbstractValidator<LoginUsuarioCommand>
    {
        public LoginUsuarioCommandValidator()
        {
            RuleFor(v => v.Email).NotEmpty().EmailAddress();
            RuleFor(v => v.Password).NotEmpty();
        }
    }

    // Handler
    public class LoginUsuarioCommandHandler : IRequestHandler<LoginUsuarioCommand, LoginResultDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public LoginUsuarioCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<LoginResultDto> Handle(LoginUsuarioCommand request, CancellationToken cancellationToken)
        {
            // 1. Buscar usuario ignorando el filtro de tenant (Global Search)
            // Necesitamos acceder al DbSet como IQueryable que permita ignorar filtros.
            // En Clean Arch estricto, a veces exponemos un método especial, pero EF Core lo permite directo
            // si el IApplicationDbContext expone DbSet<Usuario>.

            // TRUCO: Casteamos a DbContext para acceder a IgnoreQueryFilters si la interfaz no lo expone directo,
            // O asumimos que en Infrastructure lo manejamos.
            // Para mantenerlo simple y compatible:

            // NOTA: el mismo email puede existir en más de un Tenant (ej: el admin
            // de una cadena con varias sucursales — ver módulo Sucursales, que clona
            // el usuario admin en cada sucursal nueva). Ordenamos por Id para que el
            // login sea determinístico y siempre entre a la sucursal "original"
            // (la de menor Id); desde ahí puede cambiar de sucursal con el switcher.
            var usuario = await _context.Usuarios
                .IgnoreQueryFilters() // Esto requiere "using Microsoft.EntityFrameworkCore;"
                .Where(u => u.Email == request.Email)
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (usuario == null)
                throw new UnauthorizedAccessException("Credenciales inválidas.");

            // 2. Verificar Password
            if (!_passwordHasher.Verify(request.Password, usuario.Password))
                throw new UnauthorizedAccessException("Credenciales inválidas.");

            if (!usuario.Activo)
                throw new UnauthorizedAccessException("Usuario inactivo.");

            // 3. Retornar éxito
            return new LoginResultDto
            {
                UsuarioId = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                TenantId = usuario.TenantId,
                Role = usuario.Role
            };
        }
    }
}