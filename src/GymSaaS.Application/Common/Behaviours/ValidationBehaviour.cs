using FluentValidation;
using MediatR;
using FluentValidation.Results; // Necesario para ValidationFailure

namespace GymSaaS.Application.Common.Behaviours
{
    public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var failures = new List<ValidationFailure>();

                // CAMBIO CLAVE: Usamos foreach + await en lugar de Task.WhenAll
                // Esto fuerza a que las validaciones se hagan UNA POR UNA (Secuencial).
                // Así el DbContext nunca recibe dos peticiones simultáneas.
                foreach (var validator in _validators)
                {
                    var result = await validator.ValidateAsync(context, cancellationToken);
                    if (result.Errors.Any())
                    {
                        failures.AddRange(result.Errors);
                    }
                }

                if (failures.Any())
                    throw new ValidationException(failures);
            }

            return await next();
        }
    }
}