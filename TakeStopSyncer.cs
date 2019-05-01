using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class StopSyncer : Robot
    {
        [Parameter("Điều chỉnh theo giá", DefaultValue = false)]
        public bool modifyByPrice { get; set; }

        [Parameter("Giá cắt lỗ", DefaultValue = 0)]
        public double SL { get; set; }

        [Parameter("Giá chốt lời", DefaultValue = 0)]
        public double TP { get; set; }

        [Parameter("Số pips tối đa chịu lỗ", DefaultValue = 25)]
        public double stoplosspips { get; set; }

        [Parameter("Số pips chốt lời", DefaultValue = 35)]
        public double takeprofitpips { get; set; }

        [Parameter("Sử dụng trượt giá", DefaultValue = false)]
        public bool trailingStop { get; set; }

        protected override void OnStart()
        {
            Modify();
            Stop();
        }

        protected override void OnError(Error CodeOfError)
        {
            if (CodeOfError.Code == ErrorCode.NoMoney)
            {
                Print("Lỗi!!! Không đủ tiền để mở lệnh! Bot đã bị đóng!");
            }
        }

        public void Modify()
        {
            foreach (var position in Positions)
            {
                if (modifyByPrice == false)
                {
                    if (position.SymbolCode == Symbol.Code)
                    {
                        if (position.TradeType == TradeType.Buy)
                        {
                            ModifyPositionAsync(position, position.EntryPrice - stoplosspips * Symbol.PipSize, position.EntryPrice + takeprofitpips * Symbol.PipSize, trailingStop);
                        }
                        else
                        {
                            ModifyPositionAsync(position, position.EntryPrice + stoplosspips * Symbol.PipSize, position.EntryPrice - takeprofitpips * Symbol.PipSize, trailingStop);
                        }
                    }
                }
                else
                {
                    if (position.SymbolCode == Symbol.Code)
                    {
                        if (SL < 0)
                        {
                            SL = 0;
                        }
                        if (TP < 0)
                        {
                            TP = 0;
                        }
                        ModifyPositionAsync(position, SL, TP, trailingStop);
                    }
                }
            }

            Stop();
        }

    }
}
