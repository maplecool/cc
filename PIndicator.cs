using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class PIndicator : Indicator
    {
        [Parameter("Phần trăm chịu lỗ tối đa", DefaultValue = 5)]
        public int stopLossRiskPercent { get; set; }

        [Parameter("Số pips tối đa chịu lỗ", DefaultValue = 20)]
        public int stopLossInPips { get; set; }

        [Output("Show vWAP", LineStyle = LineStyle.DotsRare, Thickness = 2, Color = Colors.Gold)]
        public IndicatorDataSeries VWAP { get; set; }

        [Parameter("Show Account Summary", DefaultValue = false)]
        public bool ShowAccountSummary { get; set; }

        [Parameter("Show Trendline", DefaultValue = false)]
        public bool ShowTrendline { get; set; }

        [Parameter("Vị trí đặt thông tin", DefaultValue = 1, MinValue = 1, MaxValue = 4)]
        public int corner { get; set; }
        [Parameter("RSI Signal Periods", DefaultValue = 14, MinValue = 10)]
        public int RSIPeriods { get; set; }

        [Parameter("Historical Volatility Signal Periods", DefaultValue = 20, MinValue = 10)]
        public int HVPeriods { get; set; }

        [Parameter("TrendLines Periods", DefaultValue = 14, MinValue = 10)]
        public int TrendLinesPeriods { get; set; }

        [Parameter("VWAP Periods", DefaultValue = 0)]
        public int VWAPPeriods { get; set; }

        // private int end_bar = 0;
        private int start_bar = 0;
        private int oldCurrentDay = 0;
        public StaticPosition corner_position;
        public int CurrentDay = 0;

        // Indicators:
        private RelativeStrengthIndex _RSI;
        private HistoricalVolatility _HV;
        private ExponentialMovingAverage _EMA10;
        private ExponentialMovingAverage _EMA20;
        private ExponentialMovingAverage _EMA50;
        private ExponentialMovingAverage _EMA100;
        private ExponentialMovingAverage _EMA200;

        // Text:
        private const string UpArrow = "▲";
        private const string NeutralArrow = "▬";
        private const string DownArrow = "▼";

        protected override void Initialize()
        {
            _RSI = Indicators.RelativeStrengthIndex(MarketSeries.Close, RSIPeriods);
            _HV = Indicators.HistoricalVolatility(MarketSeries.Close, HVPeriods, 2520000);
            _EMA10 = Indicators.ExponentialMovingAverage(MarketSeries.Close, 10);
            _EMA20 = Indicators.ExponentialMovingAverage(MarketSeries.Close, 20);
            _EMA100 = Indicators.ExponentialMovingAverage(MarketSeries.Close, 100);
            _EMA200 = Indicators.ExponentialMovingAverage(MarketSeries.Close, 200);
        }

        public override void Calculate(int index)
        {
            switch (corner)
            {
                case 1:
                    corner_position = StaticPosition.TopLeft;
                    break;
                case 2:
                    corner_position = StaticPosition.TopRight;
                    break;
                case 3:
                    corner_position = StaticPosition.BottomLeft;
                    break;
                case 4:
                    corner_position = StaticPosition.BottomRight;
                    break;
            }
            CalculateIndicatorsInformation(index, corner_position);
            if (ShowAccountSummary)
            {
                CalculateAccountSummary(corner_position);
            }
            InitializeVWap(index);
            if (ShowTrendline)
            {
                InitializeTrendlines();
            }

            return;
        }

        public void CalculateAccountSummary(StaticPosition corner_position)
        {
            double costPerPip = 0;
            double positionSizeForRisk = 0;
            double gain = 0;
            double gainToday = 0;
            double totalGain = 0;
            double totalGainToday = 0;

            foreach (var position in History)
            {
                gain += position.NetProfit;
                if (position.ClosingTime.DayOfYear == Time.DayOfYear)
                {
                    gainToday += position.NetProfit;
                }
            }

            totalGain = Math.Round((gain / (Account.Balance - gain)) * 100, 3);
            if (gainToday != 0)
            {
                totalGainToday = Math.Round((gainToday / (Account.Balance - gainToday)) * 100, 3);
            }

            costPerPip = (double)((int)(Symbol.PipValue * 10000000)) / 100;
            positionSizeForRisk = ((Account.Balance * 50 / 100) / (20 * costPerPip)) * (Account.PreciseLeverage / 500);

            string text = string.Format("\n\n\n\nTotal gain: {0,0}% \nToday gain: {1,0}% \nBalance: {2,0}$ \nEquity: {3,0}$ \nProfit: {4,0}$ \nLot: {5,0} lot", Math.Round(totalGain, 2), Math.Round(totalGainToday, 2), Account.Balance, Account.Equity, Math.Round(gain, 2), Math.Round(positionSizeForRisk, 2));
            ChartObjects.DrawText("Account Text", "\t" + text, corner_position, Colors.SlateGray);
        }

        public void CalculateIndicatorsInformation(int index, StaticPosition corner_position)
        {
            ChartObjects.RemoveObject("Index TREND");
            ChartObjects.RemoveObject("Index SA");
            ChartObjects.RemoveObject("Positions");
            ChartObjects.RemoveObject("Index Positions");

            double vwap = InitializeVWap(index);

            if (Chart.TimeFrame <= TimeFrame.Minute20)
            {
                if (MarketSeries.Close.LastValue > _EMA10.Result.LastValue && MarketSeries.Close.LastValue > _EMA20.Result.LastValue && MarketSeries.Close.LastValue > vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\nTrending Up", corner_position, Colors.MediumSpringGreen);
                }
                else if (MarketSeries.Close.LastValue > _EMA10.Result.LastValue && MarketSeries.Close.LastValue < _EMA20.Result.LastValue)
                {
                    ChartObjects.DrawText("Index TREND", "\nNo Entry Available", corner_position, Colors.Gold);
                }
                else if (MarketSeries.Close.LastValue > _EMA10.Result.LastValue && MarketSeries.Close.LastValue > _EMA20.Result.LastValue && MarketSeries.Close.LastValue < vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\nNo Entry Available", corner_position, Colors.Gold);
                }
                else
                {
                    ChartObjects.DrawText("Index TREND", "\nTrending Down", corner_position, Colors.OrangeRed);
                }
            }
            else if (Chart.TimeFrame >= TimeFrame.Hour)
            {
                if (MarketSeries.Close.LastValue > _EMA100.Result.LastValue && MarketSeries.Close.LastValue > _EMA200.Result.LastValue && MarketSeries.Close.LastValue > vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\nTrending Up", corner_position, Colors.MediumSpringGreen);
                }
                else if (MarketSeries.Close.LastValue > _EMA100.Result.LastValue && MarketSeries.Close.LastValue < _EMA200.Result.LastValue)
                {
                    ChartObjects.DrawText("Index TREND", "\nNo Entry Available", corner_position, Colors.Gold);
                }
                else if (MarketSeries.Close.LastValue > _EMA100.Result.LastValue && MarketSeries.Close.LastValue > _EMA200.Result.LastValue && MarketSeries.Close.LastValue < vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\nNo Entry Available", corner_position, Colors.Gold);
                }
                else
                {
                    ChartObjects.DrawText("Index TREND", "\nTrending Down", corner_position, Colors.OrangeRed);
                }
            }

            if (_HV.Result.LastValue != 0)
            {
                ChartObjects.DrawText("SA", "\n\nVolatility:", corner_position, Colors.White);
                if (_HV.Result.LastValue >= 0.5 && _HV.Result.LastValue < 1)
                {
                    ChartObjects.DrawText("Index SA", "\n\n\t Normal", corner_position, Colors.LightGreen);
                }
                else if (_HV.Result.IsRising() && (_HV.Result.LastValue <= 2 && _HV.Result.LastValue >= 1))
                {
                    ChartObjects.DrawText("Index SA", "\n\n\t High", corner_position, Colors.OrangeRed);
                }
                else if (_HV.Result.LastValue > 2)
                {
                    ChartObjects.DrawText("Index SA", "\n\n\t Super High", corner_position, Colors.Red);
                }
                else
                {
                    ChartObjects.DrawText("Index SA", "\n\n\t Low", corner_position, Colors.Goldenrod);
                }
            }
            if (Positions.Count != 0)
            {
                double netProfit = 0;
                double Percentage = 0;
                double BuyQuantity = 0;
                double SellQuantity = 0;
                double lots = 0;
                string type;

                foreach (var position in Positions)
                {
                    if (position.SymbolCode == Symbol.Code)
                    {
                        if (position.TradeType == TradeType.Buy)
                        {
                            BuyQuantity += position.Quantity;
                        }
                        else
                        {
                            SellQuantity += position.Quantity;
                        }
                        netProfit += position.NetProfit;
                    }
                }
                if (BuyQuantity != SellQuantity)
                {
                    if (BuyQuantity > SellQuantity)
                    {
                        lots = BuyQuantity - SellQuantity;
                        type = "BUYING";
                    }
                    else
                    {
                        lots = SellQuantity - BuyQuantity;
                        type = "SELLING";
                    }
                }
                else
                {
                    lots = BuyQuantity;
                    type = "HEDGED";
                }
                Percentage = netProfit / Account.Balance;
                if (Percentage > 0)
                {
                    ChartObjects.DrawText("Positions", "\n\n\n" + Symbol.Code, corner_position, Colors.MediumSpringGreen);
                    ChartObjects.DrawText("Index Positions", ":\n\n\n\t" + "  " + Math.Round(Percentage * 100, 4) + "% | " + Math.Round(lots, 2) + " lots | " + type, corner_position, Colors.MediumSpringGreen);
                }
                else if (Percentage < 0)
                {
                    ChartObjects.DrawText("Positions", "\n\n\n" + Symbol.Code, corner_position, Colors.OrangeRed);
                    ChartObjects.DrawText("Index Positions", "\n\n\n\t" + "  " + Math.Round(Percentage * 100, 4) + "% | " + Math.Round(lots, 2) + " lots | " + type, corner_position, Colors.OrangeRed);
                }
            }
            else
            {
                ChartObjects.DrawText("Positions", "\n\n\n" + Symbol.Code + " (Chưa có lệnh)", corner_position, Colors.White);
            }
        }

        public double InitializeVWap(int index)
        {
            int ii = index;
            double CumTypPrice = 0;
            double CumVol = 0;

            if (VWAPPeriods == 0)
            {
                while (MarketSeries.OpenTime[ii] >= MarketSeries.OpenTime[ii].Date && ii != 0)
                {
                    CumTypPrice += ((MarketSeries.Close[ii] + MarketSeries.High[ii] + MarketSeries.Low[ii]) / 3) * MarketSeries.TickVolume[ii];
                    CumVol += MarketSeries.TickVolume[ii];
                    ii--;
                    if (MarketSeries.OpenTime[ii].Hour == 0 && MarketSeries.OpenTime[ii].Minute == 0)
                        break;
                }
            }
            else
            {
                for (; ii >= MarketSeries.OpenTime.Count - VWAPPeriods; ii--)
                {
                    CumTypPrice += ((MarketSeries.Close[ii] + MarketSeries.High[ii] + MarketSeries.Low[ii]) / 3) * MarketSeries.TickVolume[ii];
                    CumVol += MarketSeries.TickVolume[ii];
                }
            }

            return VWAP[index] = CumTypPrice / CumVol;
        }

        private void InitializeTrendlines()
        {
            int count = MarketSeries.Close.Count;

            int maxIndex1 = FindNextLocalExtremum(MarketSeries.High, count - 1, true);
            int maxIndex2 = FindNextLocalExtremum(MarketSeries.High, maxIndex1 - TrendLinesPeriods, true);

            int minIndex1 = FindNextLocalExtremum(MarketSeries.Low, count - 1, false);
            int minIndex2 = FindNextLocalExtremum(MarketSeries.Low, minIndex1 - TrendLinesPeriods, false);

            int startIndex = Math.Min(maxIndex2, minIndex2) - 200;
            int endIndex = count + 200;

            DrawTrendLine("high", startIndex, endIndex, maxIndex1, MarketSeries.High[maxIndex1], maxIndex2, MarketSeries.High[maxIndex2]);
            DrawTrendLine("low", startIndex, endIndex, minIndex1, MarketSeries.Low[minIndex1], minIndex2, MarketSeries.Low[minIndex2]);
        }

        private void DrawTrendLine(string lineName, int startIndex, int endIndex, int index1, double value1, int index2, double value2)
        {
            double gradient = (value2 - value1) / (index2 - index1);

            double startValue = value1 + (startIndex - index1) * gradient;
            double endValue = value1 + (endIndex - index1) * gradient;

            ChartObjects.DrawLine(lineName, startIndex, startValue, endIndex, endValue, Colors.White, 1, LineStyle.LinesDots);
            ChartObjects.DrawLine(lineName + "_green", index1, value1, index2, value2, Colors.MediumSpringGreen, 1, LineStyle.LinesDots);
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
