using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class TakeStopSyncer : Robot
    {

        [Parameter("Phần trăm chịu lỗ tối đa", DefaultValue = 5)]
        public double MaxDrawDown { get; set; }

        [Parameter("Số pips tối đa chịu lỗ", DefaultValue = 25)]
        public double MaxDrawDownInPips { get; set; }

        [Parameter("Số pips chốt lời", DefaultValue = 35)]
        public double takeprofitpips { get; set; }

        [Parameter("Sử dụng trượt giá", DefaultValue = false)]
        public bool trailingStop { get; set; }

        [Parameter("Sử dụng quỹ phòng hộ", DefaultValue = false)]
        public bool hedge { get; set; }

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
            double costPerPip = (double)((int)(Symbol.PipValue * 10000000)) / 100;
            double MaxPositionSize = (Account.Balance * MaxDrawDown / 100) / (MaxDrawDownInPips * costPerPip);
            Print("Số lot tối đa có thể chơi: " + Math.Round(MaxPositionSize, 2) + " lot với Phần trăm chịu lỗ tối đa: " + MaxDrawDown + "% và Số pips tối đa chịu lỗ: " + MaxDrawDownInPips + " pips");

            foreach (var position in Positions)
            {
                if (position.SymbolCode == Symbol.Code)
                {
                    if (position.TradeType == TradeType.Buy)
                    {
                        ModifyPositionAsync(position, position.EntryPrice - MaxDrawDownInPips * Symbol.PipSize, position.EntryPrice + takeprofitpips * Symbol.PipSize, trailingStop);

                    }
                    else
                    {
                        ModifyPositionAsync(position, position.EntryPrice + MaxDrawDownInPips * Symbol.PipSize, position.EntryPrice - takeprofitpips * Symbol.PipSize, trailingStop);

                    }
                }
            }
        }

    }
}
