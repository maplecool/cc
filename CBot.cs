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

        [Parameter("Số pips tối đa chốt lời", DefaultValue = 2)]
        public double MaxTakeProfitPips { get; set; }

        [Parameter("RSI Signal Periods", DefaultValue = 9, MinValue = 1)]
        public int RSIPeriods { get; set; }

        [Parameter("TrendLines Periods", DefaultValue = 30, MinValue = 14)]
        public int TrendLinesPeriods { get; set; }

        private RelativeStrengthIndex _RSI;
        public int TakeProfit = 0;
        public int StopLoss = 0;
        public double FirstResistancePoint = 0;
        public double SecondResistancePoint = 0;
        public double FirstSupportPoint = 0;
        public double SecondSupportPoint = 0;

        protected override void OnStart()
        {
            _RSI = Indicators.RelativeStrengthIndex(MarketSeries.Close, RSIPeriods);

            InitializePivotPoints();
        }

        protected override void OnTick()
        {
            InitializePivotPoints();
            ModifyStopLosses();
            ModifyTakeProfits();
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

        private void ModifyStopLosses()
        {
            // Tính cắt lỗ của lệnh khi đặt 
            foreach (var openedPosition in Positions)
            {
                if (openedPosition.TradeType == TradeType.Buy)
                {
                    if (openedPosition.StopLoss == null || openedPosition.TakeProfit == null)
                    {
                        ModifyPosition(openedPosition, FirstSupportPoint, FirstResistancePoint);
                    }
                    else if (openedPosition.StopLoss > FirstSupportPoint)
                    {
                        ModifyPosition(openedPosition, FirstSupportPoint, openedPosition.TakeProfit);
                    }
                    else if (openedPosition.StopLoss < FirstSupportPoint && openedPosition.StopLoss > SecondSupportPoint)
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
                    else if (openedPosition.StopLoss > FirstResistancePoint && openedPosition.StopLoss < SecondResistancePoint)
                    {
                        ModifyPosition(openedPosition, SecondResistancePoint, openedPosition.TakeProfit);
                    }
                }
            }
        }

        private void ModifyTakeProfits()
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
                    else if (openedPosition.TakeProfit > FirstResistancePoint && openedPosition.TakeProfit <= SecondResistancePoint)
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
                    else if (openedPosition.TakeProfit < FirstSupportPoint && openedPosition.TakeProfit > SecondSupportPoint)
                    {
                        ModifyPosition(openedPosition, openedPosition.StopLoss, SecondSupportPoint);
                    }
                }
            }
        }

        private void MakeNewPosition()
        {
            // Đặt lệnh mới
        }

        private void HedgePositions()
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
            if (NegativeNetProfits * 100 / Account.Balance < -MaxDropDown)
            {
                if (NegativeTradeBuyLots > NegativeTradeSellLots)
                {
                    if (NegativeTradeSellLots == 0)
                    {
                        ExecuteMarketOrder(TradeType.Sell, Symbol, (long)(NegativeTradeBuyLots * 100000), "Bảo hiểm rủi ro", StopLoss, TakeProfit);
                        return;
                    }
                    else if (NegativeTradeSellLots > 0)
                    {
                        ExecuteMarketOrder(TradeType.Sell, Symbol, (long)((NegativeTradeBuyLots - NegativeTradeSellLots) * 100000), "Bảo hiểm rủi ro", StopLoss, TakeProfit);
                        return;
                    }
                    else
                    {
                        Print("Lỗi!!! Negative Trade Buy bị ngáo rồi!");
                        return;
                    }
                }
                if (NegativeTradeBuyLots < NegativeTradeSellLots)
                {
                    if (NegativeTradeBuyLots == 0)
                    {
                        ExecuteMarketOrder(TradeType.Buy, Symbol, (long)(NegativeTradeSellLots * 100000), "Bảo hiểm rủi ro", StopLoss, TakeProfit);
                        return;
                    }
                    if (NegativeTradeBuyLots > 0)
                    {
                        ExecuteMarketOrder(TradeType.Buy, Symbol, (long)((NegativeTradeSellLots - NegativeTradeBuyLots) * 100000), "Bảo hiểm rủi ro", StopLoss, TakeProfit);
                        return;
                    }
                    else
                    {
                        Print("Lỗi!!! Negative Trade Sell bị ngáo rồi!");
                        return;
                    }
                }
            }
        }

        private void InitializePivotPoints()
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
            Print("RS: " + FirstResistancePoint + " " + SecondResistancePoint + " SP: " + FirstSupportPoint + " " + SecondSupportPoint);
        }

        private int FindNextLocalExtremum(DataSeries series, int maxIndex, bool findMax)
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

        private bool IsLocalExtremum(DataSeries series, int index, bool findMax)
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
