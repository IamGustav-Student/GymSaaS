namespace GymSaaS.Application.Common.Interfaces
{
    // 1. Aquí definimos la clase que faltaba (El DTO)
    public record DatosPagoMP(string Status, string ExternalReference);

    public interface IMercadoPagoService
    {
        // Método para cobrar (Multi-Tenant)
        Task<string> CrearPreferenciaAsync(string titulo, decimal precio, string accessToken);

        // Método para verificar estado (Webhooks)
        Task<DatosPagoMP> ConsultarPago(string paymentId);
    }
}