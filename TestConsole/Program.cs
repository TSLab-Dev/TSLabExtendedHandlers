using TSLabExtendedHandlers.Binance;

namespace TestConsole
{
    internal class Program
    {
        static void Main()
        {
            // чтобы запустить нужно в проекте закомментировать <Target Name="ILRepack" AfterTargets="Build">
            TestBinanceTradingRules();
            //TestBybitTradingRules();
            Console.ReadKey();
        }

        static void TestBinanceTradingRules()
        {
            var symbol = "BTCUSDT";
            var client = BinanceCommon.GetClient();
            Console.WriteLine(symbol);
            foreach (BinanceSpotFilters item in Enum.GetValues(typeof(BinanceSpotFilters)))
            {
                var value = BinanceSpotTradingRules.GetValue(null, client, symbol, item);
                Console.WriteLine($"{item}: {value}");
            }
        }

        static void TestBybitTradingRules()
        {
            var symbol = "BTCUSDT";
            var client = BybitCommon.GetClient();
            Console.WriteLine(symbol);
            foreach (BybitFilters item in Enum.GetValues(typeof(BybitFilters)))
            {
                var value = BybitTradingRules.GetValue(null, client, symbol, item);
                Console.WriteLine($"{item}: {value}");
            }
        }
    }
}