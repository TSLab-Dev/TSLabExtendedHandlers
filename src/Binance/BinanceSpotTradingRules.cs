using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using TSLab.Script;
using TSLab.Script.Handlers;

namespace TSLabExtendedHandlers.Binance
{
    [HandlerCategory("TSLabExtended.Binance")]
    [HandlerName("BinanceSpotTradingRules")]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = "SECURITYSource")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Получить торговые параметры по инструменту, только спот.")]
    public class BinanceSpotTradingRules : IStreamHandler, IContextUses, ICustomListValues, INeedVariableId
    {
        public IContext Context { get; set; }

        public string VariableId { get; set; }

        [HandlerParameter(Name = "Инструмент", Default = " ")]
        [Description("Инструмент по которому ищутся данные, если не указан, то берется из источника.")]
        public string Symbol { get; set; }

        [HandlerParameter(true, nameof(BinanceSpotFilters.MinimumTradeAmount), Name = "Поле")]
        public BinanceSpotFilters Field { get; set; }

        public IList<double> Execute(ISecurity sec)
        {
            var symbol = !string.IsNullOrWhiteSpace(Symbol) ? Symbol : sec.Symbol;
            var client = BinanceCommon.GetClient(sec);
            var value = 0.0;

            try
            {
                value = GetValue(Context, client, symbol, Field);
            }
            catch (Exception ex)
            {
                Context.Log($"Fail {this.GetType().Name}: {ex.Message}", MessageType.Error, true);
            }

            return Enumerable.Repeat(value, sec.Bars.Count).ToList();
        }

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            if (paramName.Equals("Symbol", StringComparison.InvariantCultureIgnoreCase))
                return new[] { Symbol ?? "" };
            return new[] { "" };
        }

        public static double GetValue(IContext ctx, BinanceClient client, string symbol, BinanceSpotFilters field)
        {
            var symbols = TryLoadBinanceSpotSymbolsFromCache(ctx, client);
            symbols ??= LoadBinanceSpotSymbols(client);

            var item = symbols.FirstOrDefault(x => x.Name == symbol);
            if (item == null) return default;

            switch (field)
            {
                case BinanceSpotFilters.MinimumTradeAmount:
                    return item.MinimumTradeAmount;
                case BinanceSpotFilters.MinimumAmountMovement:
                    return item.MinimumAmountMovement;
                case BinanceSpotFilters.MinimumPriceMovement:
                    return item.MinimumPriceMovement;
                case BinanceSpotFilters.MinimumOrderSize:
                    return item.MinimumOrderSize;
                case BinanceSpotFilters.MaximumMarketOrderAmount:
                    return item.MaximumMarketOrderAmount;
                case BinanceSpotFilters.MaxNumberOfOpenLimitOrders:
                    return item.MaxNumberOfOpenLimitOrders;
            }
            return default;
        }

        private static List<BinanceSpotSymbol> TryLoadBinanceSpotSymbolsFromCache(IContext ctx, BinanceClient client)
        {
            if (ctx == null)
                return null;
            try
            {
                var json = ctx.LoadGlobalObject($"BinanceSpotTradingRules.V2.{DateTime.Now:dd.MM.yyyy}", () =>
                {
                    var values = LoadBinanceSpotSymbols(client);
                    return JsonConvert.SerializeObject(values);
                }, fromStorage: false);

                var symbols = JsonConvert.DeserializeObject<List<BinanceSpotSymbol>>(json);
                return symbols;
            }
            catch 
            { }
            return null;
        }

        private static List<BinanceSpotSymbol> LoadBinanceSpotSymbols(BinanceClient client)
        {
            var info = LoadExchangeInfo(client);
            var res = info.Symbols.Select(x =>
            {
                return new BinanceSpotSymbol
                {
                    Name = x.Name,
                    MinimumTradeAmount = (double?)x.LotSizeFilter?.MinQuantity ?? 0,
                    MinimumAmountMovement = (double?)x.LotSizeFilter?.StepSize ?? 0,
                    MinimumPriceMovement = (double?)x.PriceFilter?.TickSize ?? 0,
                    MinimumOrderSize = (double?)x.NotionalFilter?.MinNotional ?? 0,
                    MaximumMarketOrderAmount = (double?)x.MarketLotSizeFilter?.MaxQuantity ?? 0,
                    MaxNumberOfOpenLimitOrders = x.MaxOrdersFilter?.MaxNumberOrders ?? 0,
                };
            }).ToList();
            return res;
        }

        private static BinanceExchangeInfo LoadExchangeInfo(BinanceClient client)
        {
            string error = null;
            for (int i = 0; i < 5; i++)
            {
                var res = client.SpotApi.ExchangeData.GetExchangeInfoAsync().Result;
                if (res.Success == true)
                    return res.Data;
                error = res.Error.Message;
                Thread.Sleep(1000);
            }
            throw new Exception($"BinanceSpotTradingRules: {error ?? "Не удалось загрузить данные."}");
        }

        private class BinanceSpotSymbol
        {
            public string Name { get; set; }
            public double MinimumTradeAmount { get; set; }
            public double MinimumAmountMovement { get; set; }
            public double MinimumPriceMovement { get; set; }
            public double MinimumOrderSize { get; set; }
            public double MaximumMarketOrderAmount { get; set; }
            public double MaxNumberOfOpenLimitOrders { get; set; }
            public override string ToString()
            {
                return Name;
            }
        }
    }
}
