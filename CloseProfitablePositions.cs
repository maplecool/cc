using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Robots
{
    [Robot()]
    public class CloseProfitablePositions : Robot
    {
        protected override void OnStart()
        {
            foreach (var position in Positions)
            {
                if (position.NetProfit > 0 && position.Pips > 3.0)
                {
                    ClosePosition(position);
                }
            }
        }
    }
}
