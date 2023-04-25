using Binance.Net;
using Binance.Net.Clients;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using TSLab.Script;
using TSLab.Script.Handlers;

namespace TSLabExtendedHandlers.Binance
{
    [HandlerCategory("TSLabExtended.Binance")]
    [HandlerName("BinanceLeverage")]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = "SECURITYSource")]
    [OutputsCount(0)]
    [Description("Установить плечо для инструмента.\r\n" +
        "Для фьючерсов Usdt-M и Coin-M.")]
    public class BinanceLeverage : IStreamHandler, IContextUses, ICustomListValues, INeedVariableId, INeedVariableVisual
    {
        public IContext Context { get; set; }
        public string VariableId { get; set; }
        public string VariableVisual { get; set; }

        [HandlerParameter(Name = "Инструмент", Default = " ")]
        public string Symbol { get; set; }

        [HandlerParameter(Name = "Плечо", Default = "20")]
        public int Leverage { get; set; }

        // защита от частого срабатывания
        private static readonly ConcurrentDictionary<string, DateTime> _cache = new ();

        public void Execute(ISecurity sec)
        {
            if (Context.IsOptimization)
                return;

            // защита от частого срабатывания
            _cache.TryGetValue(VariableId, out var dt);
            if (DateTime.Now < dt.AddSeconds(1))
                return;

            var symbol = !string.IsNullOrWhiteSpace(Symbol) ? Symbol : sec.Symbol;
            var client = BinanceCommon.GetClient(sec);
            var place = BinanceCommon.GetBinancePlace(sec);

            try
            {
                SetLeverage(client, place, symbol, Leverage);
                Context.Log($"[{VariableVisual}]: '{symbol}' Установлено плечо {Leverage}", MessageType.Info, true);
            }
            catch (Exception ex)
            {
                Context.Log($"[{VariableVisual}]: '{symbol}' Ошибка {ex.Message}", MessageType.Error, true);
            }

            _cache[VariableId] = DateTime.Now;
        }

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            if (paramName.Equals("Symbol", StringComparison.InvariantCultureIgnoreCase))
                return new[] { Symbol ?? "" };

            return new[] { "" };
        }

        private static double SetLeverage(BinanceClient client, BinancePlace place, string symbol, int leverage)
        {
            switch (place)
            {
                case BinancePlace.FuturesUSDT:
                    {
                        var res = client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(symbol, leverage).Result;
                        if (res.Error != null)
                            throw new Exception(res.Error.ToString());
                        return res.Data.Leverage;
                    }

                case BinancePlace.FuturesCOIN:
                    {
                        var res = client.CoinFuturesApi.Account.ChangeInitialLeverageAsync(symbol, leverage).Result;
                        if (res.Error != null)
                            throw new Exception(res.Error.ToString());
                        return res.Data.Leverage;
                    }

                default:
                    throw new Exception($"{place} не поддерживается");
            }
        }
    }
}
