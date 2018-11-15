using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class HedgingandScalpingcBot : Robot
    {
        [Parameter("Khối lượng giao dịch", DefaultValue = 1000, MinValue = 1000, Step = 1000)]
        public int Volume { get; set; }

        [Parameter("Số lệnh tối đa", DefaultValue = 5)]
        public int MaxOrders { get; set; }

        [Parameter("Phần trăm chịu lỗ tối đa", DefaultValue = 5)]
        public double MaxDropDown { get; set; }

        [Parameter("Phần trăm chịu lỗ bảo hiểm rủi ro", DefaultValue = 5)]
        public double MaxHedgeZone { get; set; }

        [Parameter("Số pips tối đa chịu lỗ", DefaultValue = 20)]
        public double MaxDropDownInPips { get; set; }

        [Parameter("RSI Signal Periods", DefaultValue = 14, MinValue = 1)]
        public int RSIPeriods { get; set; }

        [Parameter("% K Periods", DefaultValue = 9)]
        public int KPeriods { get; set; }

        [Parameter("% K Slowing", DefaultValue = 3)]
        public int KSlowing { get; set; }

        [Parameter("% D Periods", DefaultValue = 9)]
        public int DPeriods { get; set; }

        [Parameter("Moving Average Type", DefaultValue = MovingAverageType.Simple)]
        public MovingAverageType MAType { get; set; }

        [Parameter("TrendLines Periods", DefaultValue = 30, MinValue = 14)]
        public int TrendLinesPeriods { get; set; }

        private RelativeStrengthIndex _RSI;
        private StochasticOscillator _STOCH;

        // Fomula
        public double FirstResistancePoint = 0;
        public double SecondResistancePoint = 0;
        public double FirstSupportPoint = 0;
        public double SecondSupportPoint = 0;
        public double vWapPoint = 0;
        public bool Hedged = false;

        protected override void OnStart()
        {
            _RSI = Indicators.RelativeStrengthIndex(MarketSeries.Close, RSIPeriods);
            _STOCH = Indicators.StochasticOscillator(KPeriods, KSlowing, DPeriods, MAType);
            InitializePivotPoints();
        }

        protected override void OnTick()
        {
            InitializePivotPoints();
            ModifyStopLosses();
            ModifyTakeProfits();
            ExecuteNewMarketOrder();
            if (!Hedged && Positions.Find("Bảo hiểm rủi ro") == null)
                HedgePositions();
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

        public void ModifyStopLosses()
        {
            // Tính cắt lỗ của lệnh khi đặt 
            foreach (var openedPosition in Positions)
            {
                if (openedPosition.TradeType == TradeType.Buy)
                {
                    if (openedPosition.Label == "Lệnh ngắn hạn")
                    {
                    }
                    if (openedPosition.StopLoss == null || openedPosition.TakeProfit == null)
                    {
                        ModifyPosition(openedPosition, FirstSupportPoint, FirstResistancePoint);
                    }
                    else if (openedPosition.StopLoss > FirstSupportPoint)
                    {
                        ModifyPosition(openedPosition, FirstSupportPoint, openedPosition.TakeProfit);
                    }
                    else if (openedPosition.StopLoss <= FirstSupportPoint && openedPosition.StopLoss > SecondSupportPoint)
                    {
                        ModifyPosition(openedPosition, SecondSupportPoint, openedPosition.TakeProfit);
                    }
                }
                else
                {
                    if (openedPosition.StopLoss == null || openedPosition.TakeProfit == null)
                    {
                        ModifyPosition(openedPosition, FirstResistancePoint, FirstSupportPoint);
                    }
                    else if (openedPosition.StopLoss < FirstResistancePoint)
                    {
                        ModifyPosition(openedPosition, FirstResistancePoint, openedPosition.TakeProfit);
                    }
                    else if (openedPosition.StopLoss >= FirstResistancePoint && openedPosition.StopLoss < SecondResistancePoint)
                    {
                        ModifyPosition(openedPosition, SecondResistancePoint, openedPosition.TakeProfit);
                    }
                }
            }
        }

        public void ModifyTakeProfits()
        {
            // Tính chốt lời của lệnh khi đặt 
            foreach (var openedPosition in Positions)
            {
                if (openedPosition.TradeType == TradeType.Buy)
                {
                    if (openedPosition.TakeProfit < FirstResistancePoint)
                    {
                        ModifyPosition(openedPosition, openedPosition.StopLoss, FirstResistancePoint);
                    }
                    else if (openedPosition.TakeProfit >= FirstResistancePoint && openedPosition.TakeProfit < SecondResistancePoint)
                    {
                        ModifyPosition(openedPosition, openedPosition.StopLoss, SecondResistancePoint);
                    }
                }
                else
                {
                    if (openedPosition.TakeProfit > FirstSupportPoint)
                    {
                        ModifyPosition(openedPosition, openedPosition.StopLoss, FirstSupportPoint);
                    }
                    else if (openedPosition.TakeProfit <= FirstSupportPoint && openedPosition.TakeProfit > SecondSupportPoint)
                    {
                        ModifyPosition(openedPosition, openedPosition.StopLoss, SecondSupportPoint);
                    }
                }
            }
        }

        public void ExecuteNewMarketOrder()
        {
            // Đặt lệnh mới
            double RSI_OverBought = 70.0;
            double RSI_OverSold = 30.0;
            double STOCH_OverBought = 80.0;
            double STOCH_OverSold = 20.0;
            double GoodVolume = Symbol.QuantityToVolumeInUnits(Math.Round((Account.Balance * MaxDropDown / 100) / (MaxDropDownInPips * (double)((int)(Symbol.PipValue * 10000000)) / 100), 2));

            if (Positions.Count < MaxOrders)
            {
                if (TimeFrame.Hour == MarketSeries.TimeFrame || TimeFrame.Hour4 == MarketSeries.TimeFrame)
                {
                    if (_RSI.Result.LastValue > RSI_OverBought && _STOCH.PercentK.LastValue > STOCH_OverBought && (Symbol.Bid < FirstResistancePoint || Symbol.Bid < SecondResistancePoint))
                    {
                        ExecuteMarketOrder(TradeType.Sell, Symbol, GoodVolume / (TimeFrame.Hour == MarketSeries.TimeFrame ? 10 : 40), "Lệnh dài hạn");
                    }
                    else if (_RSI.Result.LastValue < RSI_OverSold && _STOCH.PercentK.LastValue < STOCH_OverSold && (Symbol.Ask > FirstSupportPoint || Symbol.Bid < SecondSupportPoint))
                    {
                        ExecuteMarketOrder(TradeType.Buy, Symbol, GoodVolume / (TimeFrame.Hour == MarketSeries.TimeFrame ? 1 : 4), "Lệnh dài hạn");
                    }
                }
                else if (MarketSeries.TimeFrame < TimeFrame.Minute45)
                {
                    if (_RSI.Result.LastValue > 60.0 && _STOCH.PercentK.LastValue > 70.0 && (Symbol.Bid < FirstResistancePoint || Symbol.Bid < SecondResistancePoint))
                    {
                        ExecuteMarketOrder(TradeType.Sell, Symbol, GoodVolume < 1000 ? 1000 : GoodVolume, "Lệnh ngắn hạn");
                    }
                    else if (_RSI.Result.LastValue < 40.0 && _STOCH.PercentK.LastValue < 50.0 && (Symbol.Ask > FirstSupportPoint || Symbol.Ask > SecondSupportPoint))
                    {
                        ExecuteMarketOrder(TradeType.Buy, Symbol, GoodVolume < 1000 ? 1000 : GoodVolume, "Lệnh ngắn hạn");
                    }
                }
            }
        }

        public void HedgePositions()
        {
            double NegativeTradeBuyLots = 0;
            double NegativeTradeSellLots = 0;
            double NegativeNetProfits = 0;

            if (Positions.Count < 0)
            {
                return;
            }
            foreach (var position in Positions)
            {
                if (position.TradeType == TradeType.Buy)
                {
                    NegativeTradeBuyLots += position.Quantity;
                }
                else if (position.TradeType == TradeType.Sell)
                {
                    NegativeTradeSellLots += position.Quantity;
                }
                NegativeNetProfits += position.NetProfit;
            }
            if (NegativeNetProfits * 100 / Account.Balance < -MaxHedgeZone)
            {
                if (NegativeTradeBuyLots > NegativeTradeSellLots)
                {
                    if (NegativeTradeSellLots == 0)
                    {
                        ExecuteMarketOrder(TradeType.Sell, Symbol, (long)(NegativeTradeBuyLots * 100000), "Bảo hiểm rủi ro");
                        Hedged = true;
                    }
                    else if (NegativeTradeSellLots > 0)
                    {
                        ExecuteMarketOrder(TradeType.Sell, Symbol, (long)((NegativeTradeBuyLots - NegativeTradeSellLots) * 100000), "Bảo hiểm rủi ro");
                        Hedged = true;
                    }
                    else
                    {
                        Print("Lỗi!!! Negative Trade Buy bị ngáo rồi!");
                    }
                }
                if (NegativeTradeBuyLots < NegativeTradeSellLots)
                {
                    if (NegativeTradeBuyLots == 0)
                    {
                        ExecuteMarketOrder(TradeType.Buy, Symbol, (long)(NegativeTradeSellLots * 100000), "Bảo hiểm rủi ro");
                        Hedged = true;
                    }
                    if (NegativeTradeBuyLots > 0)
                    {
                        ExecuteMarketOrder(TradeType.Buy, Symbol, (long)((NegativeTradeSellLots - NegativeTradeBuyLots) * 100000), "Bảo hiểm rủi ro");
                        Hedged = true;
                    }
                    else
                    {
                        Print("Lỗi!!! Negative Trade Sell bị ngáo rồi!");
                    }
                }
            }
        }

        public void InitializePivotPoints()
        {
            int count = MarketSeries.Close.Count;

            int maxIndex1 = FindNextLocalExtremum(MarketSeries.High, count - 1, true);
            int maxIndex2 = FindNextLocalExtremum(MarketSeries.High, maxIndex1 - TrendLinesPeriods, true);

            int minIndex1 = FindNextLocalExtremum(MarketSeries.Low, count - 1, false);
            int minIndex2 = FindNextLocalExtremum(MarketSeries.Low, minIndex1 - TrendLinesPeriods, false);

            FirstResistancePoint = MarketSeries.High[maxIndex1];
            SecondResistancePoint = MarketSeries.High[maxIndex2];
            FirstSupportPoint = MarketSeries.Low[minIndex1];
            SecondSupportPoint = MarketSeries.Low[minIndex2];
        }

        public int FindNextLocalExtremum(DataSeries series, int maxIndex, bool findMax)
        {
            for (int index = maxIndex; index >= 0; index--)
            {
                if (IsLocalExtremum(series, index, findMax))
                {
                    return index;
                }
            }
            return 0;
        }

        public bool IsLocalExtremum(DataSeries series, int index, bool findMax)
        {
            int end = Math.Min(index + TrendLinesPeriods, series.Count - 1);
            int start = Math.Max(index - TrendLinesPeriods, 0);

            double value = series[index];

            for (int i = start; i < end; i++)
            {
                if (findMax && value < series[i])
                    return false;

                if (!findMax && value > series[i])
                    return false;
            }
            return true;
        }
    }
}
