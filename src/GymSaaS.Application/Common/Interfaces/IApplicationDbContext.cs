using GymSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace GymSaaS.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Tenant> Tenants { get; }
        DbSet<Usuario> Usuarios { get; }
        DbSet<Socio> Socios { get; }
        DbSet<TipoMembresia> TiposMembresia { get; }
        DbSet<MembresiaSocio> MembresiasSocios { get; }
        DbSet<Pago> Pagos { get; }
        DbSet<Asistencia> Asistencias { get; }
        DbSet<ConfiguracionPago> ConfiguracionesPagos { get; }
        

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}