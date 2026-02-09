namespace GymSaaS.Domain.Entities
{
    public enum SubscriptionStatus
    {
        Inactive,
        Trialing,   // En periodo de prueba
        Active,     // Pagando correctamente
        PastDue,    // Pago fallido, reintentando
        Cancelled   // Dado de baja
    }
}