using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Robots
{
    [Robot()]
    public class ReversePositions : Robot
    {
        protected override void OnStart()
        {
            foreach (var position in Positions)
            {
                if (position.SymbolCode == Symbol.Code)
                {
                    ReversePositionAsync(position);
                }
            }
            Stop();
        }
    }
}
