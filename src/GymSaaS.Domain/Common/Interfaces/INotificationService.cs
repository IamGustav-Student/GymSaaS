namespace GymSaaS.Application.Common.Interfaces
{
    public interface INotificationService
    {
        Task EnviarAlertaPagoFallido(string nombreUsuario, string telefono, DateTime fechaReintento);
        Task EnviarConfirmacionPago(string nombreUsuario, string telefono, decimal monto);
    }
}