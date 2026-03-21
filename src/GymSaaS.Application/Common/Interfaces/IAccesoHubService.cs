namespace GymSaaS.Application.Common.Interfaces
{
    public interface IAccesoHubService
    {
        Task NotificarAccesoAsync(string tenantCode, object data);
    }
}
