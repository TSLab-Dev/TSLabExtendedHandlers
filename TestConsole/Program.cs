using TSLabExtendedHandlers.Binance;

namespace TestConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // чтобы запустить нужно в проекте закомментировать <Target Name="ILRepack" AfterTargets="Build">
            var symbol = "BNBDAI";
            var client = BinanceCommon.GetClient();
            var MinimumTradeAmount = BinanceSpotTradingRules.GetValue(null, client, symbol, BinanceSpotFilters.MinimumTradeAmount);
            var MinimumAmountMovement = BinanceSpotTradingRules.GetValue(null, client, symbol, BinanceSpotFilters.MinimumAmountMovement);
            var MinimumPriceMovement = BinanceSpotTradingRules.GetValue(null, client, symbol, BinanceSpotFilters.MinimumPriceMovement);
            var MinimumOrderSize = BinanceSpotTradingRules.GetValue(null, client, symbol, BinanceSpotFilters.MinimumOrderSize);
            var MaximumMarketOrderAmount = BinanceSpotTradingRules.GetValue(null, client, symbol, BinanceSpotFilters.MaximumMarketOrderAmount);
            var MaxNumberOfOpenLimitOrders = BinanceSpotTradingRules.GetValue(null, client, symbol, BinanceSpotFilters.MaxNumberOfOpenLimitOrders);
        }
    }
}