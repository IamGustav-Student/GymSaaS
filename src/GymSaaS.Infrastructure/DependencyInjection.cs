using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Infrastructure.Persistence;
using GymSaaS.Infrastructure.Services; // <--- Importante para encontrar las clases
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymSaaS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // 1. Base de Datos
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

            // 2. Servicios de Infraestructura
            services.AddScoped<IPasswordHasher, PasswordHasher>();

            // 3. NUEVO: Registramos MercadoPago (Esta es la línea que faltaba)
            services.AddScoped<IMercadoPagoService, MercadoPagoService>();

            return services;
        }
    }
}