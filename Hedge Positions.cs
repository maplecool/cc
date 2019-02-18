using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Robots
{
    [Robot()]
    public class HedgePositions : Robot
    {
        protected override void OnStart()
        {
            double quantitybuy = 0;
            double quantitysell = 0;

            foreach (var position in Positions)
            {
                if (position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TradeType.Buy)
                    {
                        quantitybuy += position.Quantity;
                    }
                    else
                    {
                        quantitysell += position.Quantity;
                    }
                }
            }
            if (quantitybuy > quantitysell)
            {
                ExecuteMarketOrder(TradeType.Sell, Symbol, (quantitybuy - quantitysell) * 100000);
            }
            else if (quantitybuy < quantitysell)
            {
                ExecuteMarketOrder(TradeType.Buy, Symbol, (quantitysell - quantitybuy) * 100000);
            }
            else
            {
                Stop();
            }
            Stop();
        }
    }
}
