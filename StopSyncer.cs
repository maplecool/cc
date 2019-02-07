using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class StopSyncer : Robot
    {

        [Parameter("SL", DefaultValue = 0)]
        public double SL { get; set; }

        [Parameter("TP", DefaultValue = 0)]
        public double TP { get; set; }

        [Parameter("Trail at", DefaultValue = false)]
        public bool TPT { get; set; }



        protected override void OnStart()
        {
            ModifyOrders();
        }

        protected override void OnTick()
        {
            // Put your core logic here
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }


        private void ModifyOrders()
        {

            foreach (var pos in Positions)
            {
                if (pos.SymbolCode == Symbol.Code)
                {
                    if (SL < 0)
                    {
                        SL = 0;
                    }

                    if (TP < 0)
                    {
                        TP = 0;
                    }
                    if (TPT)
                    {
                        ModifyPositionAsync(pos, SL, TP, true);
                    }
                    else
                    {
                        ModifyPositionAsync(pos, SL, TP, false);
                    }
                }
            }
            Stop();
        }

    }


}


