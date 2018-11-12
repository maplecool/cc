using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class HedgingandScalpingcBot : Robot
    {

        [Parameter("Khối lượng giao dịch", DefaultValue = 1000, MinValue = 1000, Step = 1000)]
        public int Volume { get; set; }

        [Parameter("Số lệnh tối đa", DefaultValue = 5)]
        public int MaxOrders { get; set; }

        [Parameter("Điểm chốt lời", DefaultValue = 150)]
        public int TakeProfit { get; set; }

        [Parameter("Điểm cắt lỗ", DefaultValue = 50)]
        public int StopLoss { get; set; }

        [Parameter("Phần trăm chịu lỗ tối đa", DefaultValue = 5)]
        public double MaxDropDown { get; set; }

        [Parameter("Số pips tối đa chốt lời", DefaultValue = 2)]
        public double MaxTakeProfitPips { get; set; }

        protected override void OnStart()
        {
        }

        protected override void OnTick()
        {
            CalculateStopLoss();
            CalculateTakeProfit();
            MakeNewPosition();
            HedgePositions();
        }

        protected override void OnError(Error CodeOfError)
        {
            if (CodeOfError.Code == ErrorCode.NoMoney)
            {
                Print("Lỗi!!! Không đủ tiền để mở lệnh! Bot đã bị đóng!");
            }
            else if (CodeOfError.Code == ErrorCode.BadVolume)
            {
                Print("Lỗi!!! Số lượng của lệnh không phù hợp! Bot đã bị đóng!");
            }
        }

        protected override void OnPositionOpened(Position openedPosition)
        {
            // Tìm công thức điều chỉnh sl và tp tại đây
        }

        protected override void OnPositionClosed(Position position)
        {
            // Tìm hiểu nghiên cứu thêm
        }

        private void CalculateStopLoss()
        {
            // Tính cắt lỗ của lệnh khi đặt
        }

        private void CalculateTakeProfit()
        {
            // Tính chốt lời của lệnh khi đặt 
        }

        private void MakeNewPosition()
        {
            // Đặt lệnh mới
        }

        private void HedgePositions()
        {
            double NegativeTradeBuyLots = 0;
            double NegativeTradeSellLots = 0;
            double NegativeNetProfits = 0;

            if (Positions.Count < 0)
            {
                return;
            }

            foreach (var position in Positions)
            {
                if (position.TradeType == TradeType.Buy)
                {
                    NegativeTradeBuyLots += position.Quantity;
                }
                else if (position.TradeType == TradeType.Sell)
                {
                    NegativeTradeSellLots += position.Quantity;
                }
                NegativeNetProfits += position.NetProfit;
            }
            if (NegativeNetProfits * 100 / Account.Balance < -MaxDropDown)
            {
                if (NegativeTradeBuyLots > NegativeTradeSellLots)
                {
                    if (NegativeTradeSellLots == 0)
                    {
                        ExecuteMarketOrder(TradeType.Sell, Symbol, (long)(NegativeTradeBuyLots * 100000), "Bảo hiểm rủi ro", StopLoss, TakeProfit);
                        return;
                    }
                    else if (NegativeTradeSellLots > 0)
                    {
                        ExecuteMarketOrder(TradeType.Sell, Symbol, (long)((NegativeTradeBuyLots - NegativeTradeSellLots) * 100000), "Bảo hiểm rủi ro", StopLoss, TakeProfit);
                        return;
                    }
                    else
                    {
                        Print("Lỗi!!! Negative Trade Buy bị ngáo rồi!");
                        return;
                    }
                }
                if (NegativeTradeBuyLots < NegativeTradeSellLots)
                {
                    if (NegativeTradeBuyLots == 0)
                    {
                        ExecuteMarketOrder(TradeType.Buy, Symbol, (long)(NegativeTradeSellLots * 100000), "Bảo hiểm rủi ro", StopLoss, TakeProfit);
                        return;
                    }
                    if (NegativeTradeBuyLots > 0)
                    {
                        ExecuteMarketOrder(TradeType.Buy, Symbol, (long)((NegativeTradeSellLots - NegativeTradeBuyLots) * 100000), "Bảo hiểm rủi ro", StopLoss, TakeProfit);
                        return;
                    }
                    else
                    {
                        Print("Lỗi!!! Negative Trade Sell bị ngáo rồi!");
                        return;
                    }
                }
            }
        }
    }
}
