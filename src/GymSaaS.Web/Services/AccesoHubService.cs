using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GymSaaS.Web.Services
{
    public class AccesoHubService : IAccesoHubService
    {
        private readonly IHubContext<AccesoHub> _hubContext;

        public AccesoHubService(IHubContext<AccesoHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotificarAccesoAsync(string tenantCode, object data)
        {
            await _hubContext.Clients.Group(tenantCode).SendAsync("RecibirNotificacionAcceso", data);
        }
    }
}
