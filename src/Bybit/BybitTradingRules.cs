using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using TSLab.Script;
using TSLab.Script.Handlers;

namespace TSLabExtendedHandlers.Binance
{
    [HandlerCategory("TSLabExtended.Bybit")]
    [HandlerName("BybitTradingRules")]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = "SECURITYSource")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Получить торговые параметры по инструменту, только perpetual.")]
    public class BybitTradingRules : IStreamHandler, IContextUses, ICustomListValues, INeedVariableId
    {
        public IContext Context { get; set; }

        public string VariableId { get; set; }

        [HandlerParameter(Name = "Инструмент", Default = " ")]
        [Description("Инструмент по которому ищутся данные, если не указан, то берется из источника.")]
        public string Symbol { get; set; }

        [HandlerParameter(true, nameof(BybitFilters.MinOrderQuantity), Name = "Поле")]
        public BybitFilters Field { get; set; }

        public IList<double> Execute(ISecurity sec)
        {
            var symbol = !string.IsNullOrWhiteSpace(Symbol) ? Symbol : sec.Symbol;
            var client = BybitCommon.GetClient(sec);
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

        public static double GetValue(IContext ctx, BybitClient client, string symbol, BybitFilters field)
        {
            var listSymbols = ctx == null 
                ? LoadSymbols(client)
                : ctx.LoadGlobalObject($"Bybit.ListSymbols.{DateTime.Now:dd.MM.yyyy}", () => LoadSymbols(client), fromStorage: false);

            var listRiskLimits = ctx == null
                ? LoadRiskLimits(client)
                : ctx.LoadGlobalObject($"Bybit.ListRiskLimits.{DateTime.Now:dd.MM.yyyy}", () => LoadRiskLimits(client), fromStorage: false);

            var itemSymbol = listSymbols.FirstOrDefault(x => x.Name == symbol);
            var itemRiskLimit = listRiskLimits.FirstOrDefault(x => x.Symbol == symbol);

            return field switch
            {
                BybitFilters.MaxOrderQuantity => (double?)itemSymbol?.LotSizeFilter?.MaxOrderQuantity ?? default,
                BybitFilters.MinOrderQuantity => (double?)itemSymbol?.LotSizeFilter?.MinOrderQuantity ?? default,
                BybitFilters.QuantityStep => (double?)itemSymbol?.LotSizeFilter?.QuantityStep ?? default,
                BybitFilters.InitialMargin => (double?)itemRiskLimit?.InitialMargin ?? default,
                BybitFilters.MaintenanceMargin => (double?)itemRiskLimit?.MaintenanceMargin ?? default,
                _ => default,
            };
        }

        private static List<BybitLinearInverseSymbol> LoadSymbols(BybitClient client)
        {
            string error = null;
            for (int i = 0; i < 5; i++)
            {
                var res = client.V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear).Result;
                if (res.Success == true)
                    return res.Data.List.ToList();
                error = res.Error.Message;
                Thread.Sleep(1000);
            }
            throw new Exception($"BybitTradingRules: {error ?? "Не удалось загрузить данные."}");
        }

        private static List<BybitRiskLimit> LoadRiskLimits(BybitClient client)
        {
            string error = null;
            for (int i = 0; i < 5; i++)
            {
                var res = client.V5Api.ExchangeData.GetRiskLimitAsync(Category.Linear).Result;
                if (res.Success == true)
                    return res.Data.List.ToList();
                error = res.Error.Message;
                Thread.Sleep(1000);
            }
            throw new Exception($"BybitTradingRules: {error ?? "Не удалось загрузить данные."}");
        }
    }
}
