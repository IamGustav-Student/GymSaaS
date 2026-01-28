using FluentValidation;

namespace GymSaaS.Application.Common.Validators
{
    public static class CreditCardValidatorExtensions
    {
        public static IRuleBuilderOptions<T, string> EsTarjetaCreditoValida<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.Must(CumpleLuhn).WithMessage("El número de tarjeta no es válido.");
        }

        // Algoritmo de Luhn (Estándar ISO/IEC 7812)
        private static bool CumpleLuhn(string numeroTarjeta)
        {
            if (string.IsNullOrWhiteSpace(numeroTarjeta)) return false;

            // Limpiamos espacios y guiones
            numeroTarjeta = numeroTarjeta.Replace(" ", "").Replace("-", "");

            if (!numeroTarjeta.All(char.IsDigit)) return false;

            int sum = 0;
            bool shouldDouble = false;

            // Recorremos de derecha a izquierda
            for (int i = numeroTarjeta.Length - 1; i >= 0; i--)
            {
                int digit = numeroTarjeta[i] - '0';

                if (shouldDouble)
                {
                    digit *= 2;
                    if (digit > 9) digit -= 9;
                }

                sum += digit;
                shouldDouble = !shouldDouble;
            }

            return (sum % 10) == 0;
        }
    }
}