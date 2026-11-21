using System;
using System.Collections.Generic;
using SaigonBite.Domain;

namespace SaigonBite.Application
{
    /// <summary>Một dòng của báo cáo R2: số liệu của một bếp (FR19, AMB06).</summary>
    public sealed class HubUtilizationRow
    {
        public int HubId { get; set; }
        public string HubName { get; set; }

        /// <summary>Số đơn đã xử lý: đơn từng đạt trạng thái Confirmed trong kỳ (AMB06).</summary>
        public int HandledCount { get; set; }

        /// <summary>
        /// Thời gian chuẩn bị trung bình (phút), từ Confirmed đến Ready.
        /// Là null khi không có đơn nào đủ cả hai mốc: giao diện phải hiện "chưa có dữ liệu", không hiện 0 phút.
        /// </summary>
        public decimal? AveragePrepMinutes { get; set; }

        /// <summary>Số đơn đủ cặp Confirmed/Ready đã dùng để tính trung bình.</summary>
        public int PrepSampleCount { get; set; }

        /// <summary>Tải tại thời điểm xem báo cáo (không phải tải trong kỳ), theo BR8.</summary>
        public int CurrentLoadPercent { get; set; }

        public HubLoadLevel CurrentLoadLevel { get; set; }

        /// <summary>Số lượt ưu đãi Quiet Kitchen đã ghi nhận lúc đặt đơn, kể cả đơn bị hủy sau đó (AMB06).</summary>
        public int QuietGrantedCount { get; set; }
    }

    /// <summary>Báo cáo R2: hiệu suất sử dụng bếp trong một khoảng ngày.</summary>
    public sealed class HubUtilizationReport
    {
        /// <summary>Ngày bắt đầu (tính theo giờ Việt Nam, UTC+7).</summary>
        public DateTime From { get; set; }

        /// <summary>Ngày kết thúc, được tính trọn ngày.</summary>
        public DateTime To { get; set; }

        public int HubCount { get; set; }
        public int TotalHandled { get; set; }
        public int TotalQuietGranted { get; set; }
        public IList<HubUtilizationRow> Rows { get; set; }

        public HubUtilizationReport()
        {
            Rows = new List<HubUtilizationRow>();
        }
    }
}
