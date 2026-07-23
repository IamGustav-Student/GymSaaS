using System.Security.Cryptography;
using System.Text;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GymSaaS.Infrastructure.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public RefreshTokenService(IApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<string> GenerarAsync(int usuarioId, CancellationToken cancellationToken)
        {
            var rawToken = GenerarTokenAleatorio();

            _context.RefreshTokens.Add(new RefreshToken
            {
                TokenHash = Hash(rawToken),
                UsuarioId = usuarioId,
                ExpiresAt = DateTime.UtcNow.AddDays(DiasDeVigencia())
            });

            await _context.SaveChangesAsync(cancellationToken);
            return rawToken;
        }

        public async Task<RotarRefreshTokenResult> RotarAsync(string refreshToken, CancellationToken cancellationToken)
        {
            var hash = Hash(refreshToken);

            // IgnoreQueryFilters: en este punto todavía no hay un tenant "actual"
            // establecido (es justamente lo que este token nos va a permitir resolver),
            // así que no correspondería aplicar el filtro global por tenant de Usuario.
            var existente = await _context.RefreshTokens
                .IgnoreQueryFilters()
                .Include(r => r.Usuario)
                .FirstOrDefaultAsync(r => r.TokenHash == hash, cancellationToken);

            if (existente == null || !existente.EstaActivo || existente.Usuario == null || !existente.Usuario.Activo)
                return new RotarRefreshTokenResult(false, null, null);

            var nuevoRawToken = GenerarTokenAleatorio();

            existente.RevokedAt = DateTime.UtcNow;
            existente.ReplacedByTokenHash = Hash(nuevoRawToken);

            _context.RefreshTokens.Add(new RefreshToken
            {
                TokenHash = Hash(nuevoRawToken),
                UsuarioId = existente.UsuarioId,
                ExpiresAt = DateTime.UtcNow.AddDays(DiasDeVigencia())
            });

            await _context.SaveChangesAsync(cancellationToken);

            return new RotarRefreshTokenResult(true, existente.Usuario, nuevoRawToken);
        }

        public async Task RevocarAsync(string refreshToken, CancellationToken cancellationToken)
        {
            var hash = Hash(refreshToken);
            var existente = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.TokenHash == hash, cancellationToken);

            if (existente != null && existente.RevokedAt == null)
            {
                existente.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private double DiasDeVigencia() =>
            double.Parse(_configuration["JwtSettings:RefreshTokenDays"] ?? "30");

        private static string GenerarTokenAleatorio() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        private static string Hash(string rawToken) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }
}
