using Microsoft.AspNetCore.SignalR;

namespace GymSaaS.Web.Hubs
{
    // Este Hub permite la comunicación en tiempo real entre el portal y el monitor
    public class AccesoHub : Hub
    {
        // El monitor del staff se une a un "canal" basado en el ID de su gimnasio
        public async Task UnirseAlGrupoGym(string tenantId)
        {
            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
            }
        }

        // Se desconecta del canal al cerrar la pestaña
        public async Task SalirDelGrupoGym(string tenantId)
        {
            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenantId);
            }
        }
    }
}