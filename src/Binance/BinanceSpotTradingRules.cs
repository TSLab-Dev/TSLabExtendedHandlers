using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
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
            var key = $"BinanceSpotTradingRules.{DateTime.Now:dd.MM.yyyy}";
            var res = ctx == null 
                ? LoadExchangeInfo(client)
                : ctx.LoadGlobalObject(key, () => LoadExchangeInfo(client), fromStorage: false);
            var item = res.Symbols.FirstOrDefault(x => x.Name == symbol);
            if (item == null) return default;

            switch (field)
            {
                case BinanceSpotFilters.MinimumTradeAmount:
                    return (double)item.LotSizeFilter.MinQuantity;
                case BinanceSpotFilters.MinimumAmountMovement:
                    return (double)item.LotSizeFilter.StepSize;
                case BinanceSpotFilters.MinimumPriceMovement:
                    return (double)item.PriceFilter.TickSize;
                case BinanceSpotFilters.MinimumOrderSize:
                    return (double)item.NotionalFilter.MinNotional;
                case BinanceSpotFilters.MaximumMarketOrderAmount:
                    return (double)item.MarketLotSizeFilter.MaxQuantity;
                case BinanceSpotFilters.MaxNumberOfOpenLimitOrders:
                    return (double)item.MaxOrdersFilter.MaxNumberOrders;
            }
            return default;
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
    }
}
