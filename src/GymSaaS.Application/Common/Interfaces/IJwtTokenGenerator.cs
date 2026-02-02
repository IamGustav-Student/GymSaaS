namespace GymSaaS.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(int usuarioId, string nombre, string email, string tenantId);
    }
}