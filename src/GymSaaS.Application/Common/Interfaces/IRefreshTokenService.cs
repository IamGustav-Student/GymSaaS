using GymSaaS.Domain.Entities;

namespace GymSaaS.Application.Common.Interfaces
{
    public record RotarRefreshTokenResult(bool EsValido, Usuario? Usuario, string? NuevoRefreshToken);

    public interface IRefreshTokenService
    {
        Task<string> GenerarAsync(int usuarioId, CancellationToken cancellationToken);

        // Valida un refresh token y, si es válido, lo revoca y emite uno nuevo (rotación).
        Task<RotarRefreshTokenResult> RotarAsync(string refreshToken, CancellationToken cancellationToken);

        Task RevocarAsync(string refreshToken, CancellationToken cancellationToken);
    }
}
