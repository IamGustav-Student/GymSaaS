using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GymSaaS.Web.Filters
{
    public class ApiExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            if (context.Exception is ValidationException validationEx)
            {
                // Si es un error de validación, lo convertimos en un BadRequest (400)
                // O lo inyectamos en el ModelState si estamos en una vista MVC clásica

                foreach (var error in validationEx.Errors)
                {
                    // Agregamos el error al ModelState para que aparezca en el formulario rojo
                    context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }

                // IMPORTANTE: En MVC, si falla, usualmente queremos devolver la VISTA con los errores,
                // no un JSON (a menos que sea una API pura).
                // Aquí, como estamos usando MediatR dentro del Controller, el Controller ya tiene un 
                // try-catch manual en nuestros ejemplos (Fase 6).

                // Si decides quitar los try-catch de los controladores y confiar en este filtro:
                context.Result = new BadRequestObjectResult(context.ModelState);
                context.ExceptionHandled = true;
            }

            base.OnException(context);
        }
    }
}