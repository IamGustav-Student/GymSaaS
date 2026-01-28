namespace GymSaaS.Application.Common.Interfaces
{
    public interface ICurrentTenantService
    {
        string? TenantId { get; }
    }
}