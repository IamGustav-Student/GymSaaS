namespace GymSaaS.Domain.Entities
{
    public enum SubscriptionStatus
    {
        Trial,      // Período de prueba (14 días)
        Active,     // Gimnasio con suscripción al día
        PastDue,    // Pago pendiente o fallido en MP
        Suspended   // Acceso bloqueado por falta de pago
    }
}