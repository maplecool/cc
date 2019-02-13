using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, AccessRights = AccessRights.FullAccess)]
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

        [Parameter("Average True Range Signal Periods", DefaultValue = 14, MinValue = 1)]
        public int ATRPeriods { get; set; }

        [Parameter("Average True Range Moving Average Type", DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType maType { get; set; }

        [Parameter("Historical Volatility Signal Periods", DefaultValue = 14, MinValue = 1)]
        public int HVPeriods { get; set; }

        [Parameter("TrendLines Periods", DefaultValue = 14, MinValue = 10)]
        public int TrendLinesPeriods { get; set; }

        [Parameter("VWAP Periods", DefaultValue = 0)]
        public int VWAPPeriods { get; set; }

        private AverageTrueRange _ATR;
        private HistoricalVolatility _HV;
        public StaticPosition corner_position;
        private ExponentialMovingAverage _EMA10;
        private ExponentialMovingAverage _EMA20;
        private ExponentialMovingAverage _EMA50;
        private ExponentialMovingAverage _EMA100;
        private ExponentialMovingAverage _EMA200;

        private const VerticalAlignment vAlign = VerticalAlignment.Top;
        private const HorizontalAlignment hAlign = HorizontalAlignment.Center;

        protected override void Initialize()
        {
            _ATR = Indicators.AverageTrueRange(ATRPeriods, maType);
            _HV = Indicators.HistoricalVolatility(MarketSeries.Close, HVPeriods, 252);
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
            CalculateIndicators(index, corner_position);
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

            string text = string.Format("\n\n\n\n\nTotal gain: {0,0}% \nToday gain: {1,0}% \nBalance: {2,0}$ \nEquity: {3,0}$ \nProfit: {4,0}$ \nQuanity: {5,0} lot", Math.Round(totalGain, 2), Math.Round(totalGainToday, 2), Account.Balance, Account.Equity, Math.Round(gain, 2), Math.Round(positionSizeForRisk, 2));
            ChartObjects.DrawText("Account Text", "\t" + text, corner_position, Colors.SlateGray);
        }

        public void CalculateIndicators(int index, StaticPosition corner_position)
        {
            ChartObjects.RemoveObject("Index TREND");
            ChartObjects.RemoveObject("Index SA");
            ChartObjects.RemoveObject("Index ATR");
            ChartObjects.RemoveObject("Positions");
            ChartObjects.RemoveObject("Index Positions");

            double vwap = InitializeVWap(index);

            if (Chart.TimeFrame <= TimeFrame.Minute20)
            {
                if (MarketSeries.Close.IsRising() && MarketSeries.Close.LastValue > _EMA10.Result.LastValue && MarketSeries.Close.LastValue > _EMA20.Result.LastValue && MarketSeries.Close.LastValue > vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\nTrending Up", corner_position, Colors.MediumSpringGreen);
                }
                else if (MarketSeries.Close.IsRising() && MarketSeries.Close.LastValue > _EMA10.Result.LastValue && MarketSeries.Close.LastValue < _EMA20.Result.LastValue && MarketSeries.Close.LastValue > vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\nTrending Up", corner_position, Colors.MediumSpringGreen);
                }
                else if (MarketSeries.Close.IsFalling() && MarketSeries.Close.LastValue < _EMA10.Result.LastValue && MarketSeries.Close.LastValue < _EMA20.Result.LastValue && MarketSeries.Close.LastValue < vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\nTrending Down", corner_position, Colors.OrangeRed);
                }
                else if (MarketSeries.Close.IsFalling() && MarketSeries.Close.LastValue > _EMA10.Result.LastValue && MarketSeries.Close.LastValue < _EMA20.Result.LastValue && MarketSeries.Close.LastValue < vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\nTrending Down", corner_position, Colors.OrangeRed);
                }
                else
                {
                    ChartObjects.DrawText("Index TREND", "\nNo Entry Available", corner_position, Colors.Gold);
                }
            }
            else if (Chart.TimeFrame >= TimeFrame.Hour)
            {
                if (MarketSeries.Close.IsRising() && MarketSeries.Close.LastValue > _EMA100.Result.LastValue && MarketSeries.Close.LastValue > _EMA200.Result.LastValue && MarketSeries.Close.LastValue > vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\nTrending Up", corner_position, Colors.MediumSpringGreen);
                }
                else if (MarketSeries.Close.IsRising() && MarketSeries.Close.LastValue > _EMA100.Result.LastValue && MarketSeries.Close.LastValue < _EMA200.Result.LastValue && MarketSeries.Close.LastValue > vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\nTrending Up", corner_position, Colors.MediumSpringGreen);
                }
                else if (MarketSeries.Close.IsFalling() && MarketSeries.Close.LastValue < _EMA100.Result.LastValue && MarketSeries.Close.LastValue < _EMA200.Result.LastValue && MarketSeries.Close.LastValue < vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\nTrending Down", corner_position, Colors.OrangeRed);
                }
                else if (MarketSeries.Close.IsFalling() && MarketSeries.Close.LastValue > _EMA100.Result.LastValue && MarketSeries.Close.LastValue < _EMA200.Result.LastValue && MarketSeries.Close.LastValue < vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\nTrending Down", corner_position, Colors.OrangeRed);
                }
                else
                {
                    ChartObjects.DrawText("Index TREND", "\nNo Entry Available", corner_position, Colors.Gold);
                }
            }
            if (_HV.Result.LastValue > 0)
            {
                ChartObjects.DrawText("SA", "\n\nVolatility: ", corner_position, Colors.White);
                if (_HV.Result.IsRising() && _HV.Result.HasCrossedBelow(_HV.Result.Minimum(HVPeriods), HVPeriods) && _HV.Result.LastValue < _HV.Result.Maximum(HVPeriods))
                {
                    ChartObjects.DrawText("Index SA", "\n\n\t High (" + (Math.Round(_HV.Result.LastValue, 5) * 1000) + " %)", corner_position, Colors.OrangeRed);
                }
                else if (_HV.Result.IsRising() && _HV.Result.HasCrossedAbove(_HV.Result.Maximum(HVPeriods), HVPeriods))
                {
                    ChartObjects.DrawText("Index SA", "\n\n\t Very High (" + (Math.Round(_HV.Result.LastValue, 5) * 1000) + " %)", corner_position, Colors.Red);
                }
                if (_HV.Result.IsFalling() && _HV.Result.HasCrossedBelow(_HV.Result.Minimum(HVPeriods), HVPeriods) && _HV.Result.LastValue < _HV.Result.Minimum(HVPeriods))
                {
                    ChartObjects.DrawText("Index SA", "\n\n\t Low (" + (Math.Round(_HV.Result.LastValue, 5) * 1000) + " %)", corner_position, Colors.Goldenrod);
                }
                else
                {
                    ChartObjects.DrawText("Index SA", "\n\n\t Normal (" + (Math.Round(_HV.Result.LastValue, 5) * 1000) + " %)", corner_position, Colors.LightGreen);
                }
            }
            if (_ATR.Result.LastValue > 0)
            {
                ChartObjects.DrawText("ATR", "\n\n\nMomentum:", corner_position, Colors.White);
                if (_ATR.Result.IsRising() && _ATR.Result.HasCrossedBelow(_ATR.Result.Minimum(ATRPeriods), ATRPeriods) && _ATR.Result.LastValue < _ATR.Result.Maximum(ATRPeriods))
                {
                    ChartObjects.DrawText("Index ATR", "\n\n\n\t      High (" + (Math.Round(_ATR.Result.LastValue, 5) * 10000) + " pips)", corner_position, Colors.OrangeRed);
                }
                else if (_ATR.Result.IsRising() && _ATR.Result.HasCrossedAbove(_ATR.Result.Maximum(ATRPeriods), ATRPeriods))
                {
                    ChartObjects.DrawText("Index ATR", "\n\n\n\t      Very High (" + (Math.Round(_ATR.Result.LastValue, 5) * 10000) + " pips)", corner_position, Colors.Red);
                }
                if (_ATR.Result.IsRising() && _ATR.Result.HasCrossedBelow(_ATR.Result.Minimum(ATRPeriods), ATRPeriods) && _ATR.Result.LastValue < _ATR.Result.Minimum(ATRPeriods))
                {
                    ChartObjects.DrawText("Index ATR", "\n\n\n\t      Low (" + (Math.Round(_ATR.Result.LastValue, 5) * 10000) + " pips)", corner_position, Colors.Goldenrod);
                }
                else
                {
                    ChartObjects.DrawText("Index ATR", "\n\n\n\t      Normal (" + (Math.Round(_ATR.Result.LastValue, 5) * 10000) + " pips)", corner_position, Colors.LightGreen);
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
                    ChartObjects.DrawText("Index Positions", "\n\n\n\n" + Symbol.Code + " " + Math.Round(Percentage * 100, 4) + "% | " + Math.Round(lots, 2) + " lots | " + type, corner_position, Colors.MediumSpringGreen);
                }
                else if (Percentage < 0)
                {
                    ChartObjects.DrawText("Index Positions", "\n\n\n\n" + Symbol.Code + " " + Math.Round(Percentage * 100, 4) + "% | " + Math.Round(lots, 2) + " lots | " + type, corner_position, Colors.OrangeRed);
                }
                else
                {
                    ChartObjects.DrawText("Index Positions", "\n\n\n\n" + Symbol.Code + " " + Math.Round(Percentage * 100, 4) + "% | " + Math.Round(lots, 2) + " lots | " + type, corner_position, Colors.White);
                }
            }
            else
            {
                ChartObjects.DrawText("Positions", "\n\n\n\n" + Symbol.Code + " (Chưa có lệnh)", corner_position, Colors.White);
            }
        }

        public double InitializeVWap(int index)
        {
            int i = index;
            double CumulativeTypicalPrice = 0;
            double CumulativeVolume = 0;

            if (VWAPPeriods == 0)
            {
                while (MarketSeries.OpenTime[i] >= MarketSeries.OpenTime[i].Date && i != 0)
                {
                    CumulativeTypicalPrice += ((MarketSeries.Close[i] + MarketSeries.High[i] + MarketSeries.Low[i]) / 3) * MarketSeries.TickVolume[i];
                    CumulativeVolume += MarketSeries.TickVolume[i];
                    i--;
                    if (MarketSeries.OpenTime[i].Hour == 0 && MarketSeries.OpenTime[i].Minute == 0)
                        break;
                }
            }
            else
            {
                for (; i >= MarketSeries.OpenTime.Count - VWAPPeriods; i--)
                {
                    CumulativeTypicalPrice += ((MarketSeries.Close[i] + MarketSeries.High[i] + MarketSeries.Low[i]) / 3) * MarketSeries.TickVolume[i];
                    CumulativeVolume += MarketSeries.TickVolume[i];
                }
            }

            return VWAP[index] = CumulativeTypicalPrice / CumulativeVolume;
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
