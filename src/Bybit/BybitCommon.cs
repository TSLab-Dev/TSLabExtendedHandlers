using Bybit.Net.Clients;
using Bybit.Net.Objects;
using CryptoExchange.Net.Authentication;
using TSLab.Script;

namespace TSLabExtendedHandlers.Binance
{
    public static class BybitCommon
    {
        public static BybitClient GetClient()
        {
            var opt = new BybitClientOptions();
            return new BybitClient(opt);
        }

        public static BybitClient GetClient(ISecurity sec)
        {
            dynamic settings = sec.SecurityDescription.TradePlace.DataSource.Settings;
            string key = settings.Public;
            string secret = settings.Secret;
            var opt = new BybitClientOptions();
            opt.ApiCredentials = new ApiCredentials(key, secret);
            return new BybitClient(opt);
        }
    }

    public enum BybitFilters
    {
        MaxOrderQuantity, // Максимальный размер ордера
        MinOrderQuantity, // Минимальный размер ордера
        QuantityStep, // Размер контракта
        InitialMargin, // Начальная маржа
        MaintenanceMargin, // Поддерживающая маржа
    }
}
