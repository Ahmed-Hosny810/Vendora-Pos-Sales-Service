
namespace Pos.SalesService.Application.Helpers
{
    public static class CurrencyRounding
    {
        private const int DecimalPlaces = 2;

        public static decimal Round(decimal value)
        {
            return Math.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero);
        }
    }
}
