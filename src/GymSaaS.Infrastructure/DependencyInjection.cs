using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Infrastructure.Persistence;
using GymSaaS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymSaaS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                    builder => builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

            // Servicios existentes
            services.AddTransient<IPasswordHasher, PasswordHasher>();
            services.AddTransient<IMercadoPagoService, MercadoPagoService>();
            services.AddTransient<IJwtTokenGenerator, JwtTokenGenerator>();

            // NUEVO: Servicio de Notificaciones (Dunning)
            services.AddTransient<INotificationService, WhatsAppNotificationService>();

            return services;
        }
    }
}