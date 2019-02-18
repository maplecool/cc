using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Robots
{
    [Robot()]
    public class ClosePositions : Robot
    {
        protected override void OnStart()
        {
            foreach (var position in Positions)
            {
                ClosePosition(position);
            }
            Stop();
        }
    }
}
