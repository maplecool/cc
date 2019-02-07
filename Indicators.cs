using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.ChinaStandardTime, AccessRights = AccessRights.FullAccess)]
    public class PIndicator : Indicator
    {
        [Parameter("Offset Reset time", DefaultValue = 0)]
        public int TimeOffset { get; set; }

        [Parameter("Phần trăm chịu lỗ tối đa", DefaultValue = 5)]
        public int stopLossRiskPercent { get; set; }

        [Parameter("Số pips tối đa chịu lỗ", DefaultValue = 20)]
        public int stopLossInPips { get; set; }

        [Output("Đường độ lệch chuẩn trên", Color = Colors.Gray, PlotType = PlotType.Points)]
        public IndicatorDataSeries SD3Pos { get; set; }

        [Output("Đường độ lệch chuẩn dưới", Color = Colors.Gray, PlotType = PlotType.Points)]
        public IndicatorDataSeries SD3Neg { get; set; }

        [Output("Khối lượng giá trung bình (vWAP)", LineStyle = LineStyle.Solid, Thickness = 2, Color = Colors.LawnGreen)]
        public IndicatorDataSeries VWAP { get; set; }

        [Parameter("Hiện đường độ lệch chuẩn (Standard Deviation)", DefaultValue = false)]
        public bool ShowDeviation { get; set; }

        [Parameter("Hiện lịch sử khối lượng giá trung bình", DefaultValue = false)]
        public bool ShowHistoricalvWap { get; set; }

        [Parameter("Hiện thông tin tài khoản", DefaultValue = false)]
        public bool ShowAccountSummary { get; set; }

        [Parameter("Hiện Trendline", DefaultValue = false)]
        public bool ShowTrendline { get; set; }

        [Parameter("Vị trí đặt thông tin", DefaultValue = 1, MinValue = 0, MaxValue = 4)]
        public int corner { get; set; }

        [Parameter("Chu kỳ đường trung bình cộng dài", DefaultValue = 26, MinValue = 1)]
        public int LongCycle { get; set; }

        [Parameter("Chu ký đường trung bình cộng ngắn", DefaultValue = 12, MinValue = 1)]
        public int ShortCycle { get; set; }

        [Parameter("MACD Signal Periods", DefaultValue = 9, MinValue = 1)]
        public int MACDPeriods { get; set; }

        [Parameter("RSI Signal Periods", DefaultValue = 14, MinValue = 1)]
        public int RSIPeriods { get; set; }

        [Parameter("% K Periods", DefaultValue = 9, MinValue = 1)]
        public int KPeriods { get; set; }

        [Parameter("% K Slowing", DefaultValue = 3, MinValue = 1)]
        public int KSlowing { get; set; }

        [Parameter("% D Periods", DefaultValue = 9, MinValue = 1)]
        public int DPeriods { get; set; }

        [Parameter("Moving Average Type", DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType MAType { get; set; }

        [Parameter("TrendLines Periods", DefaultValue = 14, MinValue = 14)]
        public int TrendLinesPeriods { get; set; }

        // private int end_bar = 0;
        private int start_bar = 0;
        private int oldCurrentDay = 0;

        public StaticPosition corner_position;
        public int CurrentDay = 0;

        // Indicators:
        private MacdCrossOver _MACD;
        private RelativeStrengthIndex _RSI;
        private StochasticOscillator _STOCH;

        // Text:
        private const string UpArrow = "▲";
        private const string DownArrow = "▼";

        protected override void Initialize()
        {
            _MACD = Indicators.MacdCrossOver(MarketSeries.Close, LongCycle, ShortCycle, MACDPeriods);
            _RSI = Indicators.RelativeStrengthIndex(MarketSeries.Close, RSIPeriods);
            _STOCH = Indicators.StochasticOscillator(KPeriods, KSlowing, DPeriods, MAType);
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
            InitializeVWap(index, corner_position);
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
            positionSizeForRisk = (Account.Balance * 50 / 100) / (20 * costPerPip);

            string text = string.Format("\n\n\n\n\n\n\nTotal gain: {0,0}% \nToday gain: {1,0}% \nBalance: {2,0} USD \nEquity: {3,0} USD \nProfit: {4,0} USD \nMaximum Lot: {5,0} lot", Math.Round(totalGain, 2), Math.Round(totalGainToday, 2), Account.Balance, Account.Equity, Math.Round(gain, 2), Math.Round(positionSizeForRisk, 2));
            ChartObjects.DrawText("Account Text", "\t" + text, corner_position, Colors.White);
        }

        public void CalculateIndicatorsInformation(int index, StaticPosition corner_position)
        {
            ChartObjects.RemoveObject("Index MACD");

            if (_MACD.Histogram[index] != 0 || _MACD.MACD[index] > 0 || _MACD.Signal[index] > 0)
            {
                if (_MACD.Histogram[index] > 0 || _MACD.MACD[index] > 0 || _MACD.Signal[index] > 0)
                {
                    ChartObjects.DrawText("MACD", "\nMACD:", corner_position, Colors.White);
                    ChartObjects.DrawText("Index MACD", "\n\t" + UpArrow + " " + Math.Round(_MACD.MACD.LastValue, 3) + ", " + Math.Round(_MACD.Histogram.LastValue, 3) + ", " + Math.Round(_MACD.Signal.LastValue, 3), corner_position, Colors.MediumSpringGreen);

                }
                else if (_MACD.Histogram[index] < 0 || _MACD.MACD[index] > 0 || _MACD.Signal[index] > 0)
                {
                    ChartObjects.DrawText("MACD", "\nMACD", corner_position, Colors.White);
                    ChartObjects.DrawText("Index MACD", "\n\t" + DownArrow + " " + Math.Round(_MACD.MACD.LastValue, 3) + ", " + Math.Round(_MACD.Histogram.LastValue, 3) + ", " + Math.Round(_MACD.Signal.LastValue, 3), corner_position, Colors.OrangeRed);
                }
            }

            ChartObjects.RemoveObject("Index RSI");

            if (_RSI.Result.LastValue != 0)
            {
                ChartObjects.DrawText("RSI", "\n\nRSI:", corner_position, Colors.White);
                if (_RSI.Result.IsRising() && _RSI.Result.LastValue < 70)
                {
                    ChartObjects.DrawText("Index RSI", "\n\n\t" + UpArrow + " " + Math.Round(_RSI.Result.LastValue), corner_position, Colors.MediumSpringGreen);
                }
                else
                {
                    ChartObjects.DrawText("Index RSI", "\n\n\t " + DownArrow + " " + Math.Round(_RSI.Result.LastValue), corner_position, Colors.OrangeRed);
                }
            }

            ChartObjects.RemoveObject("Index Stoch");

            if (_STOCH.PercentK.LastValue != 0 || _STOCH.PercentD.LastValue != 0)
            {
                ChartObjects.DrawText("Stoch", "\n\n\nStoch:", corner_position, Colors.White);
                if (_STOCH.PercentK.IsRising() && _STOCH.PercentK.LastValue < 80 && _STOCH.PercentD.LastValue < 70)
                {
                    ChartObjects.DrawText("Index Stoch", "\n\n\n\t" + UpArrow + " " + Math.Round(_STOCH.PercentD.LastValue) + ", " + Math.Round(_STOCH.PercentK.LastValue), corner_position, Colors.MediumSpringGreen);
                }
                else
                {
                    ChartObjects.DrawText("Index Stoch", "\n\n\n\t" + DownArrow + " " + Math.Round(_STOCH.PercentD.LastValue) + ", " + Math.Round(_STOCH.PercentK.LastValue), corner_position, Colors.OrangeRed);
                }
            }

            ChartObjects.RemoveObject("Positions");
            ChartObjects.RemoveObject("Index Positions");

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
                    ChartObjects.DrawText("Positions", "\n\n\n\n" + Symbol.Code, corner_position, Colors.MediumSpringGreen);
                    ChartObjects.DrawText("Index Positions", ":\n\n\n\n\t" + "  " + Math.Round(Percentage * 100, 4) + "% | " + Math.Round(lots, 2) + " lots | " + type + " | " + Account.UnrealizedNetProfit + " $", corner_position, Colors.MediumSpringGreen);
                }
                else if (Percentage < 0)
                {
                    ChartObjects.DrawText("Positions", "\n\n\n\n" + Symbol.Code, corner_position, Colors.OrangeRed);
                    ChartObjects.DrawText("Index Positions", "\n\n\n\n\t" + "  " + Math.Round(Percentage * 100, 4) + "% | " + Math.Round(lots, 2) + " lots | " + type + " | " + Account.UnrealizedNetProfit + " $", corner_position, Colors.OrangeRed);
                }
            }
            else
            {
                ChartObjects.DrawText("Positions", "\n\n\n\n" + Symbol.Code + " (Chưa có lệnh)", corner_position, Colors.White);
            }

            ChartObjects.DrawText("SymbolSpread", "\n\n\n\n\n" + "Spread: " + Math.Round(Symbol.Spread / Symbol.PipSize, 5) + " pips", corner_position, Colors.White);
            ChartObjects.DrawText("Free Margin", "\n\n\n\n\n\n" + "Free Margin: " + Math.Round(Account.FreeMargin, 2) + " $", corner_position, Colors.White);

        }

        public void InitializeVWap(int index, StaticPosition corner_position)
        {
            int end_bar = index;
            int CurrentDay = MarketSeries.OpenTime[end_bar].DayOfYear;
            double TotalPV = 0;
            double TotalVolume = 0;
            double highest = 0;
            double lowest = 999999;
            double close = MarketSeries.Close[index];

            if (CurrentDay == oldCurrentDay)
            {
                for (int i = start_bar; i <= end_bar; i++)
                {
                    TotalPV += MarketSeries.TickVolume[i] * ((MarketSeries.Low[i] + MarketSeries.High[i] + MarketSeries.Close[i]) / 3);
                    TotalVolume += MarketSeries.TickVolume[i];
                    VWAP[i] = TotalPV / TotalVolume;

                    if (MarketSeries.High[i] > highest)
                    {
                        highest = MarketSeries.High[i];
                    }
                    if (MarketSeries.Low[i] < lowest)
                    {
                        lowest = MarketSeries.Low[i];
                    }

                    double SD = 0;
                    for (int k = start_bar; k <= i; k++)
                    {

                        double HLC = (MarketSeries.High[k] + MarketSeries.Low[k] + MarketSeries.Close[k]) / 3;
                        double OHLC = (MarketSeries.High[k] + MarketSeries.Low[k] + MarketSeries.Open[k] + MarketSeries.Close[k]) / 4;

                        double avg = HLC;
                        double diff = avg - VWAP[i];
                        SD += (MarketSeries.TickVolume[k] / TotalVolume) * (diff * diff);
                    }

                    SD = Math.Sqrt(SD);

                    if (corner != 0)
                    {
                        ChartObjects.DrawText("VWAP", "vWap:", corner_position, Colors.White);
                        if (Symbol.Bid < Math.Round(VWAP[index], 5) || Symbol.Ask < Math.Round(VWAP[index], 5))
                        {
                            ChartObjects.DrawText("Index VWAP", "\t" + DownArrow + " " + Math.Round(VWAP[index], 5), corner_position, Colors.OrangeRed);
                        }
                        else if (Symbol.Ask > Math.Round(VWAP[index], 5) || Symbol.Bid > Math.Round(VWAP[index], 5))
                        {
                            ChartObjects.DrawText("Index VWAP", "\t" + UpArrow + " " + Math.Round(VWAP[index], 5), corner_position, Colors.MediumSpringGreen);
                        }
                        else
                        {
                            ChartObjects.DrawText("Index VWAP", "\t" + Math.Round(VWAP[index], 5), corner_position, Colors.White);
                        }
                    }

                    if (ShowDeviation)
                    {
                        //ChartObjects.DrawText("sda", "SD: " + Math.Round(SD, 4), StaticPosition.TopRight);
                        //ChartObjects.DrawText("sdb", "\nSD: " + Math.Round(VWAP[index], 4), StaticPosition.TopRight);
                        //ChartObjects.DrawText("sdc", "\n\nSD: " + Math.Round(close, 4), StaticPosition.TopRight);

                        double SD_Pos = VWAP[i] + SD;
                        double SD_Neg = VWAP[i] - SD;
                        double SD2Pos = SD_Pos + SD;
                        double SD2Neg = SD_Neg - SD;

                        SD3Pos[i] = SD2Pos + SD;
                        SD3Neg[i] = SD2Neg - SD;
                    }
                    if (!ShowHistoricalvWap)
                    {
                        //VWAP[index] = sum / start_bar - i;
                        if (i < index - 15)
                        {
                            VWAP[i] = double.NaN;
                        }
                    }
                }
            }
            else
            {
                if (!ShowHistoricalvWap)
                {
                    for (int i = index - 16; i <= index; i++)
                    {
                        VWAP[i] = double.NaN;
                    }
                }
                oldCurrentDay = MarketSeries.OpenTime[end_bar].DayOfYear;
                start_bar = end_bar - TimeOffset;
            }
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
            //ChartObjects.DrawLine(lineName + "_green", index1, value1, index2, value2, Colors.MediumSpringGreen, 1, LineStyle.LinesDots);
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
