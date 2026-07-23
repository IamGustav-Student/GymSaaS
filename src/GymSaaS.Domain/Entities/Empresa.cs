using GymSaaS.Domain.Common;

namespace GymSaaS.Domain.Entities
{
    // Agrupa varios Tenants (sucursales) bajo el mismo dueño/cadena.
    // Cada sucursal sigue siendo un Tenant independiente (suscripción, socios,
    // membresías y facturación propias) — Empresa solo permite que el mismo
    // admin cambie de sucursal y vea un resumen consolidado básico.
    public class Empresa : BaseEntity
    {
        public string Nombre { get; set; } = string.Empty;
    }
}
