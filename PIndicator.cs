using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, AccessRights = AccessRights.FullAccess)]
    public class PIndicator : Indicator
    {
        [Output("Show vWAP", LineStyle = LineStyle.DotsRare, Thickness = 2, Color = Colors.Gold)]
        public IndicatorDataSeries VWAP { get; set; }

        [Parameter("Show Account Summary", DefaultValue = false)]
        public bool ShowAccountSummary { get; set; }

        [Parameter("Vị trí đặt thông tin", DefaultValue = 1, MinValue = 1, MaxValue = 4)]
        public int corner { get; set; }

        [Parameter("Average True Range Signal Periods", DefaultValue = 14, MinValue = 1)]
        public int ATRPeriods { get; set; }

        [Parameter("Average True Range Moving Average Type", DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType maType { get; set; }

        [Parameter("Historical Volatility Signal Periods", DefaultValue = 14, MinValue = 1)]
        public int HVPeriods { get; set; }

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

            return;
        }

        public void CalculateAccountSummary(StaticPosition corner_position)
        {
            double gain = 0;
            double gainToday = 0;
            double totalGain = 0;
            double totalGainToday = 0;

            foreach (var position in History)
            {
                if (position.ClosingTime.Month >= 4 && position.ClosingTime.Year == 2019)
                {
                    gain += position.NetProfit;
                }
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

            string text = string.Format("\n\n\n\n\n\n\nTotal gain: {0,0}% \nToday gain: {1,0}% \nBalance: {2,0}$ \nEquity: {3,0}$ \nProfit: {4,0}$", Math.Round(totalGain, 2), Math.Round(totalGainToday, 2), Account.Balance, Account.Equity, Math.Round(gain, 2));
            ChartObjects.DrawText("Account Text", "\t" + text, corner_position, Colors.SlateGray);
        }

        public void CalculateIndicators(int index, StaticPosition corner_position)
        {
            ChartObjects.RemoveObject("Index TREND");
            ChartObjects.RemoveObject("Index SA");
            ChartObjects.RemoveObject("Index ATR");
            ChartObjects.RemoveObject("Index Quantity");
            ChartObjects.RemoveObject("Index POWER");
            ChartObjects.RemoveObject("Positions");
            ChartObjects.RemoveObject("Index Positions");

            double vwap = InitializeVWap(index);

            if (Chart.TimeFrame <= TimeFrame.Minute20)
            {
                if (MarketSeries.Close.IsRising() && MarketSeries.Close.LastValue > _EMA10.Result.LastValue && MarketSeries.Close.LastValue > _EMA20.Result.LastValue && MarketSeries.Close.LastValue > vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\n\nTrending Up", corner_position, Colors.Green);
                }
                else if (MarketSeries.Close.IsRising() && MarketSeries.Close.LastValue > _EMA10.Result.LastValue && MarketSeries.Close.LastValue < _EMA20.Result.LastValue && MarketSeries.Close.LastValue > vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\n\nTrending Up", corner_position, Colors.Green);
                }
                else if (MarketSeries.Close.IsFalling() && MarketSeries.Close.LastValue < _EMA10.Result.LastValue && MarketSeries.Close.LastValue < _EMA20.Result.LastValue && MarketSeries.Close.LastValue < vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\n\nTrending Down", corner_position, Colors.OrangeRed);
                }
                else if (MarketSeries.Close.IsFalling() && MarketSeries.Close.LastValue > _EMA10.Result.LastValue && MarketSeries.Close.LastValue < _EMA20.Result.LastValue && MarketSeries.Close.LastValue < vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\n\nTrending Down", corner_position, Colors.OrangeRed);
                }
                else
                {
                    ChartObjects.DrawText("Index TREND", "\n\nNo Entry Available", corner_position, Colors.Gold);
                }
            }
            else if (Chart.TimeFrame >= TimeFrame.Hour)
            {
                if (MarketSeries.Close.IsRising() && MarketSeries.Close.LastValue > _EMA100.Result.LastValue && MarketSeries.Close.LastValue > _EMA200.Result.LastValue && MarketSeries.Close.LastValue > vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\n\nTrending Up", corner_position, Colors.Green);
                }
                else if (MarketSeries.Close.IsRising() && MarketSeries.Close.LastValue > _EMA100.Result.LastValue && MarketSeries.Close.LastValue < _EMA200.Result.LastValue && MarketSeries.Close.LastValue > vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\n\nTrending Up", corner_position, Colors.Green);
                }
                else if (MarketSeries.Close.IsFalling() && MarketSeries.Close.LastValue < _EMA100.Result.LastValue && MarketSeries.Close.LastValue < _EMA200.Result.LastValue && MarketSeries.Close.LastValue < vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\n\nTrending Down", corner_position, Colors.OrangeRed);
                }
                else if (MarketSeries.Close.IsFalling() && MarketSeries.Close.LastValue > _EMA100.Result.LastValue && MarketSeries.Close.LastValue < _EMA200.Result.LastValue && MarketSeries.Close.LastValue < vwap)
                {
                    ChartObjects.DrawText("Index TREND", "\n\nTrending Down", corner_position, Colors.OrangeRed);
                }
                else
                {
                    ChartObjects.DrawText("Index TREND", "\n\nNo Entry Available", corner_position, Colors.Gold);
                }
            }
            if (_HV.Result.LastValue > 0)
            {
                if (_HV.Result.IsRising() && _HV.Result.HasCrossedBelow(_HV.Result.Minimum(HVPeriods), HVPeriods) && _HV.Result.LastValue < _HV.Result.Maximum(HVPeriods))
                {
                    ChartObjects.DrawText("Index SA", "\n\n\nVolatility (High)", corner_position, Colors.OrangeRed);
                }
                else if (_HV.Result.IsRising() && _HV.Result.HasCrossedAbove(_HV.Result.Maximum(HVPeriods), HVPeriods))
                {
                    ChartObjects.DrawText("Index SA", "\n\n\nVolatility (Very High)", corner_position, Colors.Red);
                }
                if (_HV.Result.IsFalling() && _HV.Result.HasCrossedBelow(_HV.Result.Minimum(HVPeriods), HVPeriods) && _HV.Result.LastValue < _HV.Result.Minimum(HVPeriods))
                {
                    ChartObjects.DrawText("Index SA", "\n\n\nVolatility (Low)", corner_position, Colors.Goldenrod);
                }
                else
                {
                    ChartObjects.DrawText("Index SA", "\n\n\nVolatility (Normal)", corner_position, Colors.Green);
                }
            }
            if (_ATR.Result.LastValue > 0)
            {
                ChartObjects.DrawText("ATR", "\n\n\n\nATR (" + Math.Round(_ATR.Result.LastValue, 5) + ")", corner_position, Colors.Goldenrod);
            }

            double TrendPower = MarketSeries.High[index] - MarketSeries.Low[index];
            if (TrendPower > 0)
            {
                ChartObjects.DrawText("Index POWER", "\n\n\n\n\nBulls (" + Math.Round(TrendPower, 5) + ")", corner_position, Colors.Green);
            }
            else if (TrendPower < 0)
            {
                ChartObjects.DrawText("Index POWER", "\n\n\n\n\nBear (" + Math.Round(TrendPower, 5) + ")", corner_position, Colors.Red);
            }
            else
            {
                ChartObjects.DrawText("Index POWER", "\n\n\n\n\nSideways (" + Math.Round(TrendPower, 5) + ")", corner_position, Colors.Goldenrod);
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
                    ChartObjects.DrawText("Index Positions", "\n\n\n\n\n\n" + Symbol.Code + " " + Math.Round(Percentage * 100, 4) + "% | " + Math.Round(lots, 2) + " lots | " + type, corner_position, Colors.MediumSpringGreen);
                }
                else if (Percentage < 0)
                {
                    ChartObjects.DrawText("Index Positions", "\n\n\n\n\n\n" + Symbol.Code + " " + Math.Round(Percentage * 100, 4) + "% | " + Math.Round(lots, 2) + " lots | " + type, corner_position, Colors.OrangeRed);
                }
                else
                {
                    ChartObjects.DrawText("Index Positions", "\n\n\n\n\n\n" + Symbol.Code + " " + Math.Round(Percentage * 100, 4) + "% | " + Math.Round(lots, 2) + " lots | " + type, corner_position, Colors.White);
                }
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
    }
}
