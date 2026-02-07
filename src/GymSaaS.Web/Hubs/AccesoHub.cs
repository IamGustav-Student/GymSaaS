using Microsoft.AspNetCore.SignalR;

namespace GymSaaS.Web.Hubs
{
    public class AccesoHub : Hub
    {
        // El cliente (Dueño) se conecta y dice "Soy del Gym X"
        public async Task UnirseAlGrupoGym(string tenantId)
        {
            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
            }
        }

        public async Task SalirDelGrupoGym(string tenantId)
        {
            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenantId);
            }
        }
    }
}