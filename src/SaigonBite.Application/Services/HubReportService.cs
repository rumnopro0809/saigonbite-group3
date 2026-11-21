using System;
using System.Collections.Generic;
using System.Linq;
using SaigonBite.Domain;

namespace SaigonBite.Application
{
    /// <summary>
    /// Báo cáo R2: hiệu suất sử dụng bếp (FR19). Cách đếm theo AMB06 (đề xuất của nhóm, chờ giảng viên xác nhận).
    /// </summary>
    public sealed class HubReportService
    {
        // Báo cáo và giao diện dùng giờ Việt Nam (UTC+7).
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
        private readonly IAppDbContext _db;
        private readonly HubLoadService _loadService;

        public HubReportService(IAppDbContext db)
        {
            _db = db;
            _loadService = new HubLoadService(db);
        }

        /// <summary>
        /// Tạo báo cáo R2 cho khoảng ngày [from, to]. Khoảng được tính từ 00:00 ngày from
        /// đến trước 00:00 ngày sau ngày to (giờ UTC+7), nên ngày cuối được tính trọn.
        /// </summary>
        public ServiceResult<HubUtilizationReport> Build(DateTime from, DateTime to)
        {
            if (from.Date > to.Date)
            {
                return ServiceResult<HubUtilizationReport>.Invalid(new List<ValidationError>
                {
                    new ValidationError("To", "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.")
                });
            }

            var start = new DateTimeOffset(from.Date, VietnamOffset);
            var endExclusive = new DateTimeOffset(to.Date.AddDays(1), VietnamOffset);

            // Các lần đơn chuyển sang Confirmed trong kỳ. "Đã xử lý" = từng được Confirmed (AMB06).
            var confirmed = _db.OrderStatusHistories
                .Where(h => h.To == OrderStatus.Confirmed && h.ChangedAt >= start && h.ChangedAt < endExclusive)
                .Select(h => new { h.OrderId, HubId = h.Order.HubId, h.ChangedAt })
                .ToList()
                .GroupBy(h => h.OrderId)
                .Select(g => g.OrderBy(x => x.ChangedAt).First())
                .ToList();

            // Các lần đơn chuyển sang Ready, bắt đầu từ đầu kỳ. Ghép với mốc Confirmed ở bộ nhớ.
            var readyByOrder = _db.OrderStatusHistories
                .Where(h => h.To == OrderStatus.Ready && h.ChangedAt >= start)
                .Select(h => new { h.OrderId, h.ChangedAt })
                .ToList()
                .ToLookup(h => h.OrderId, h => h.ChangedAt);

            // Số lượt Quiet Kitchen lấy từ ảnh chụp lúc đặt đơn, kể cả đơn bị hủy sau đó (AMB06).
            var quietByHub = _db.Orders
                .Where(o => o.QuietDiscountGranted && o.PlacedAt >= start && o.PlacedAt < endExclusive)
                .GroupBy(o => o.HubId)
                .Select(g => new { HubId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.HubId, x => x.Count);

            var activeCounts = _loadService.CountActiveOrders();
            var report = new HubUtilizationReport { From = from.Date, To = to.Date };

            foreach (var hub in _db.Hubs.Where(h => !h.IsDeleted).OrderBy(h => h.Id).ToList())
            {
                var hubConfirmed = confirmed.Where(c => c.HubId == hub.Id).ToList();

                // Chỉ đơn có đủ cặp Confirmed và Ready mới được tính thời gian chuẩn bị.
                var minutes = new List<double>();
                foreach (var c in hubConfirmed)
                {
                    var readyTimes = readyByOrder[c.OrderId].Where(r => r > c.ChangedAt).ToList();
                    if (readyTimes.Count == 0) continue;
                    minutes.Add((readyTimes.Min() - c.ChangedAt).TotalMinutes);
                }

                int active = activeCounts.ContainsKey(hub.Id) ? activeCounts[hub.Id] : 0;
                decimal load = HubLoad.Calculate(active, hub.Capacity);
                report.Rows.Add(new HubUtilizationRow
                {
                    HubId = hub.Id,
                    HubName = hub.Name,
                    HandledCount = hubConfirmed.Count,
                    // Không có mẫu thì để null, tránh hiện "0 phút" gây hiểu nhầm.
                    AveragePrepMinutes = minutes.Count == 0 ? (decimal?)null : Math.Round((decimal)minutes.Average(), 1),
                    PrepSampleCount = minutes.Count,
                    CurrentLoadPercent = (int)Math.Round(load * 100m, 0, MidpointRounding.AwayFromZero),
                    CurrentLoadLevel = HubLoad.Classify(load),
                    QuietGrantedCount = quietByHub.ContainsKey(hub.Id) ? quietByHub[hub.Id] : 0
                });
            }

            report.HubCount = report.Rows.Count;
            report.TotalHandled = report.Rows.Sum(r => r.HandledCount);
            report.TotalQuietGranted = report.Rows.Sum(r => r.QuietGrantedCount);
            return ServiceResult<HubUtilizationReport>.Ok(report);
        }
    }
}
