using FluentValidation;
using GymSaaS.Application.Common.Validators; // Importamos nuestra extensión
using MediatR;
using GymSaaS.Application.Common.Helpers;

namespace GymSaaS.Application.Pagos.Commands.ProcesarPagoTarjeta
{
    public record ProcesarPagoTarjetaCommand : IRequest<bool>
    {
        public string Titular { get; init; } = string.Empty;
        public string NumeroTarjeta { get; init; } = string.Empty;
        public string Vencimiento { get; init; } = string.Empty; // MM/YY
        public string CVV { get; init; } = string.Empty;
        public decimal Monto { get; init; }
    }

    public class ProcesarPagoTarjetaCommandValidator : AbstractValidator<ProcesarPagoTarjetaCommand>
    {
        public ProcesarPagoTarjetaCommandValidator()
        {
            RuleFor(v => v.Titular)
                .NotEmpty().WithMessage("El nombre del titular es requerido.");

            // 1. AQUI USAMOS NUESTRO VALIDADOR PERSONALIZADO
            RuleFor(v => v.NumeroTarjeta)
                .NotEmpty()
                .EsTarjetaCreditoValida(); // <--- ¡Magia!

            // 2. Validar Formato Fecha (MM/YY) y que no esté vencida
            RuleFor(v => v.Vencimiento)
                .Matches(@"^(0[1-9]|1[0-2])\/?([0-9]{2})$").WithMessage("Formato inválido (MM/YY)")
                .Must(NoEstarVencida).WithMessage("La tarjeta está vencida.");

            // 3. Validar CVV (3 o 4 dígitos)
            RuleFor(v => v.CVV)
                .NotEmpty()
                .Matches(@"^\d{3,4}$").WithMessage("CVV inválido.");

            RuleFor(v => v.Monto).GreaterThan(0);
            RuleFor(v => v.NumeroTarjeta)
            .Must(SerMarcaAceptada)
            .WithMessage("Lo sentimos, no aceptamos American Express ni marcas desconocidas. Solo Visa o MasterCard.");
        }

        private bool SerMarcaAceptada(string numeroTarjeta)
        {
            var marca = CreditCardHelper.GetBrand(numeroTarjeta);

            // Definimos qué marcas aceptamos
            return marca == CardBrand.Visa || marca == CardBrand.MasterCard;
        }

        private bool NoEstarVencida(string vencimiento)
        {
            if (string.IsNullOrEmpty(vencimiento) || !vencimiento.Contains("/")) return false;

            var partes = vencimiento.Split('/');
            if (partes.Length != 2) return false;

            if (int.TryParse(partes[0], out int mes) && int.TryParse(partes[1], out int anio))
            {
                // Asumimos año 20xx
                int anioCompleto = 2000 + anio;
                var fechaVencimiento = new DateTime(anioCompleto, mes, 1).AddMonths(1).AddDays(-1);

                return fechaVencimiento >= DateTime.UtcNow.Date;
            }
            return false;
        }
    }

    // El Handler iría aquí, conectando con la API de Pagos...
}