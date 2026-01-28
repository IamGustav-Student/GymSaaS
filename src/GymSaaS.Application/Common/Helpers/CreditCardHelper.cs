using System.Text.RegularExpressions;

namespace GymSaaS.Application.Common.Helpers
{
    public enum CardBrand
    {
        Unknown,
        Visa,
        MasterCard,
        Amex,
        Discover,
        DinersClub,
        JCB
    }

    public static class CreditCardHelper
    {
        // Expresiones Regulares Oficiales
        private static readonly Regex VisaRegex = new(@"^4[0-9]{12}(?:[0-9]{3})?$");
        private static readonly Regex MasterCardRegex = new(@"^(?:5[1-5][0-9]{2}|222[1-9]|22[3-9][0-9]|2[3-6][0-9]{2}|27[01][0-9]|2720)[0-9]{12}$");
        private static readonly Regex AmexRegex = new(@"^3[47][0-9]{13}$");
        private static readonly Regex DiscoverRegex = new(@"^6(?:011|5[0-9]{2})[0-9]{12}$");

        public static CardBrand GetBrand(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber)) return CardBrand.Unknown;

            // Limpiamos espacios y guiones para analizar solo números
            string cleanNumber = cardNumber.Replace(" ", "").Replace("-", "");

            if (VisaRegex.IsMatch(cleanNumber)) return CardBrand.Visa;
            if (MasterCardRegex.IsMatch(cleanNumber)) return CardBrand.MasterCard;
            if (AmexRegex.IsMatch(cleanNumber)) return CardBrand.Amex;
            if (DiscoverRegex.IsMatch(cleanNumber)) return CardBrand.Discover;

            return CardBrand.Unknown;
        }

        // Método extra para obtener el icono de Bootstrap Icons sugerido
        public static string GetIconClass(CardBrand brand)
        {
            return brand switch
            {
                CardBrand.Visa => "bi-credit-card-2-front-fill text-primary", // O un icono específico si existe
                CardBrand.MasterCard => "bi-credit-card-2-back-fill text-warning",
                CardBrand.Amex => "bi-credit-card text-info",
                _ => "bi-credit-card text-secondary"
            };
        }
    }
}