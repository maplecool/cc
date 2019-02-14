using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.ChinaStandardTime, AccessRights = AccessRights.None)]
    public class PhuBot : Robot
    {
        [Parameter("Số lệnh tối đa", DefaultValue = 5)]
        public int MaxOrders { get; set; }

        [Parameter("Số pips tối đa chịu lỗ", DefaultValue = 25)]
        public double stoplosspips { get; set; }

        [Parameter("Số pips chốt lời", DefaultValue = 35)]
        public double takeprofitpips { get; set; }

        [Parameter("Phần trăm chịu lỗ tối đa", DefaultValue = 2)]
        public double MaxDropDown { get; set; }

        private ExponentialMovingAverage _EMA100;
        private ExponentialMovingAverage _EMA200;

        protected override void OnStart()
        {
            _EMA100 = Indicators.ExponentialMovingAverage(MarketSeries.Close, 100);
            _EMA200 = Indicators.ExponentialMovingAverage(MarketSeries.Close, 200);
        }

        protected override void OnError(Error CodeOfError)
        {
            if (CodeOfError.Code == ErrorCode.NoMoney)
            {
                Print("Lỗi!!! Không đủ tiền để mở lệnh! Bot đã bị đóng!");
            }
            else if (CodeOfError.Code == ErrorCode.BadVolume)
            {
                Print("Lỗi!!! Số lượng của lệnh không phù hợp! Bot đã bị đóng!");
            }
        }

        protected override void OnTick()
        {
            double GoodVolume = Symbol.QuantityToVolumeInUnits(Math.Round((Account.Balance * MaxDropDown / 100) / (stoplosspips * (double)((int)(Symbol.PipValue * 10000000)) / 100), 2));
            if (Positions.Count < MaxOrders && (Time.Hour >= 7 || Time.Hour < 18))
            {
                if (MarketSeries.Close.IsRising() && MarketSeries.Close.LastValue > _EMA100.Result.LastValue && MarketSeries.Close.LastValue > _EMA200.Result.LastValue)
                {
                    ExecuteMarketOrder(TradeType.Buy, Symbol, GoodVolume);
                }
                else if (MarketSeries.Close.IsFalling() && MarketSeries.Close.LastValue < _EMA100.Result.LastValue && MarketSeries.Close.LastValue < _EMA200.Result.LastValue)
                {
                    ExecuteMarketOrder(TradeType.Sell, Symbol, GoodVolume);
                }
                else
                {
                    Print("No Entry Available");
                }
            }
            if (Positions.Count > 0)
            {
                foreach (var position in Positions)
                {
                    if (position.TradeType == TradeType.Buy)
                    {
                        ModifyPositionAsync(position, position.EntryPrice - stoplosspips * Symbol.PipSize, position.EntryPrice + takeprofitpips * Symbol.PipSize);
                    }
                    else
                    {
                        ModifyPositionAsync(position, position.EntryPrice + stoplosspips * Symbol.PipSize, position.EntryPrice - takeprofitpips * Symbol.PipSize);
                    }
                }
            }
        }

    }
}
