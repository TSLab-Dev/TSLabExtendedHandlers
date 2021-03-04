using Binance.Net;
using Binance.Net.Objects.Futures.FuturesData;
using Binance.Net.Objects.Spot;
using CryptoExchange.Net.Authentication;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TSLab.Script;
using TSLab.Script.Handlers;
using TSLab.Script.Realtime;

namespace TSLabExtendedHandlers.Binance
{
    [HandlerCategory("TSLabExtended.Binance")]
    [HandlerName("BinancePosition")]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = "SECURITYSource")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Получить значение позиции из бинанса.\r\n" +
        "Для фьючерсов Usdt-M и Coin-M.\r\n" +
        "По умолчанию работает только в режиме агента.\r\n" +
        "Можно включить 'Показывать в лаборатории'.")]
    public class BinancePosition : IStreamHandler, IContextUses, ICustomListValues, INeedVariableId
    {
        public IContext Context { get; set; }

        public string VariableId { get; set; }

        [HandlerParameter(Name = "Инструмент", Default = " ")]
        [Description("Инструмент по которому ищется позиция, если не указан, то берется из источника.")]
        public string Symbol { get; set; }

        [HandlerParameter(true, nameof(BinancePositionField.PositionAmount), Name = "Поле позиции")]
        public BinancePositionField PositionField { get; set; }

        [HandlerParameter(Name = "Показывать в лаборатории", NotOptimized = true)]
        public bool IsShowAll { get; set; }

        // защита от частого срабатывания
        private static ConcurrentDictionary<string, (DateTime, double)> _cache = new ConcurrentDictionary<string, (DateTime, double)>();

        public IList<double> Execute(ISecurity sec)
        {
            if (Context.IsOptimization)
                return Enumerable.Repeat(0.0, sec.Bars.Count).ToList();

            if (!IsShowAll && !(sec is ISecurityRt))
                return Enumerable.Repeat(0.0, sec.Bars.Count).ToList();

            if (!IsShowAll && (sec as ISecurityRt)?.IsPortfolioActive != true)
                return Enumerable.Repeat(0.0, sec.Bars.Count).ToList();

            // защита от частого срабатывания
            _cache.TryGetValue(VariableId, out var item);
            if (DateTime.Now < item.Item1.AddSeconds(1))
                return Enumerable.Repeat(item.Item2, sec.Bars.Count).ToList();

            var symbol = !string.IsNullOrWhiteSpace(Symbol) ? Symbol : sec.Symbol;
            var client = GetClient(sec);
            var place = GetBinancePlace(sec);
            var value = 0.0;

            try
            {
                value = GetValue(client, place, symbol, PositionField);
            }
            catch (Exception ex)
            {
                Context.Log($"Fail {this.GetType().Name}: {ex.Message}", MessageType.Warning, true);
            }

            _cache[VariableId] = (DateTime.Now, value);
            return Enumerable.Repeat(value, sec.Bars.Count).ToList();
        }

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            if (paramName.Equals("Symbol", StringComparison.InvariantCultureIgnoreCase))
                return new[] { Symbol ?? "" };

            return new[] { "" };
        }

        private double GetValue(BinanceClient client, BinancePlace place, string symbol, BinancePositionField field)
        {
            BinancePositionDetailsBase pos = null;
            if (place == BinancePlace.FuturesUSDT)
            {
                var res = client.FuturesUsdt.GetPositionInformation();
                if (res.Error != null) throw new Exception(res.Error.ToString());
                pos = res.Data.FirstOrDefault(x => x.Symbol == symbol);
            }
            else if (place == BinancePlace.FuturesCOIN)
            {
                var res = client.FuturesCoin.GetPositionInformation();
                if (res.Error != null) throw new Exception(res.Error.ToString());
                pos = res.Data.FirstOrDefault(x => x.Symbol == symbol);
            }

            if (pos == null) return default;

            switch (field)
            {
                case BinancePositionField.PositionAmount:
                    return (double)pos.PositionAmount;
                case BinancePositionField.EntryPrice:
                    return (double)pos.EntryPrice;
                case BinancePositionField.Leverage:
                    return (double)pos.Leverage;
                case BinancePositionField.LiquidationPrice:
                    return (double)pos.LiquidationPrice;
                case BinancePositionField.MarkPrice:
                    return (double)pos.MarkPrice;
                case BinancePositionField.UnrealizedProfit:
                    return (double)pos.UnrealizedProfit;
            }
            return default;
        }

        private static BinanceClient GetClient(ISecurity sec)
        {
            return null;
            //dynamic settings = sec.SecurityDescription.TradePlace.DataSource.Settings;
            //string key = settings.Public;
            //string secret = settings.Secret;
            //var opt = new BinanceClientOptions();
            //opt.ApiCredentials = new ApiCredentials(key, secret);
            //return new BinanceClient(opt);
        }

        private static BinancePlace GetBinancePlace(ISecurity sec)
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
}
