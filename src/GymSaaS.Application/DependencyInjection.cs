using FluentValidation;
using GymSaaS.Application.Common.Behaviours;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace GymSaaS.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // 1. Configurar MediatR (Busca handlers en este ensamblado)
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            // 2. Configurar FluentValidation (Busca validadores en este ensamblado)
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            // 3. Configurar el comportamiento de validación en la tubería de MediatR
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            return services;
        }
    }
}