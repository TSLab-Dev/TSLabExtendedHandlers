using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using TSLab.Script;

namespace TSLabExtendedHandlers.Binance
{
    public static class BinanceCommon
    {
        public static BinanceClient GetClient()
        {
            var opt = new BinanceClientOptions();
            return new BinanceClient(opt);
        }

        public static BinanceClient GetClient(ISecurity sec)
        {
            dynamic settings = sec.SecurityDescription.TradePlace.DataSource.Settings;
            string key = settings.Public;
            string secret = settings.Secret;
            var opt = new BinanceClientOptions();
            opt.ApiCredentials = new BinanceApiCredentials(key, secret);
            return new BinanceClient(opt);
        }

        public static BinancePlace GetBinancePlace(ISecurity sec)
        {
            var name = sec.SecurityDescription.TradePlace.DataSource.GetType().Name;
            if (name.Contains("Coin"))
                return BinancePlace.FuturesCOIN;
            if (name.Contains("Futures"))
                return BinancePlace.FuturesUSDT;
            if (name.Contains("Margin"))
                return BinancePlace.Margin;
            return BinancePlace.Spot;
        }
    }

    public enum BinancePositionField
    {
        PositionAmount,
        EntryPrice,
        Leverage,
        LiquidationPrice,
        MarkPrice,
        UnrealizedProfit,
    }

    public enum BinancePlace
    {
        Spot,
        FuturesUSDT,
        FuturesCOIN,
        Margin,
    }

    public enum BinanceSpotFilters
    {
        MinimumTradeAmount, // LOT_SIZE.minQty
        MinimumAmountMovement, // LOT_SIZE.stepSize
        MinimumPriceMovement, // PRICE_FILTER.tickSize
        MinimumOrderSize, // NOTIONAL.minNotional
        MaximumMarketOrderAmount, // MARKET_LOT_SIZE.maxQty
        MaxNumberOfOpenLimitOrders, // MAX_NUM_ORDERS.maxNumOrders
    }
}
