using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.ChinaStandardTime, AccessRights = AccessRights.FullAccess)]
    public class Spreads : Indicator
    {
        [Parameter("Vị trí đặt thông tin", DefaultValue = 2, MinValue = 0, MaxValue = 4)]
        public int corner { get; set; }

        public StaticPosition corner_position;

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

            ChartObjects.DrawText("SymbolSpread", "Spread: " + Math.Round(Symbol.Spread / Symbol.PipSize, 5) + " pips", corner_position, Colors.Goldenrod);
            return;
        }
    }
}
