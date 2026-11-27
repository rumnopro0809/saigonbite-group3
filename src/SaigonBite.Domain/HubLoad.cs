using System;

namespace SaigonBite.Domain
{
    /// <summary>Mức tải của bếp, dùng để hiển thị trạng thái và quyết định nhận đơn.</summary>
    public enum HubLoadLevel
    {
        /// <summary>Tải dưới 30%: bếp rảnh, đơn mới được giảm giá Quiet Kitchen (BR7).</summary>
        Quiet,
        /// <summary>Tải từ 30% đến dưới 90%: nhận đơn bình thường.</summary>
        Normal,
        /// <summary>Tải từ 90% trở lên: tạm ngừng nhận đơn mới (BR8).</summary>
        Full
    }

    /// <summary>Tính tải của bếp. Chỉ là phép tính thuần, không dùng database.</summary>
    public static class HubLoad
    {
        /// <summary>Ngưỡng Quiet Kitchen: tải nhỏ hơn 30% (BR7).</summary>
        public const decimal QuietBelow = .3m;

        /// <summary>Ngưỡng ngừng nhận đơn: tải từ 90% trở lên (BR8).</summary>
        public const decimal BlockedFrom = .9m;

        /// <summary>BR8: chỉ đơn Confirmed và Preparing được tính vào tải. Placed, Ready, Cancelled... thì không.</summary>
        public static bool CountsTowardLoad(OrderStatus status)
        {
            return status == OrderStatus.Confirmed || status == OrderStatus.Preparing;
        }

        /// <summary>Tải = số đơn đang xử lý / công suất. Kết quả có thể lớn hơn 1 (100%) khi công suất bị giảm.</summary>
        public static decimal Calculate(int activeOrders, int capacity)
        {
            // Công suất phải dương, nếu không sẽ chia cho 0 (FR14.1).
            if (capacity <= 0) throw new BusinessRuleException("FR14.1", "Công suất phải là số nguyên dương.");
            if (activeOrders < 0) throw new ArgumentOutOfRangeException("activeOrders");
            return (decimal)activeOrders / capacity;
        }

        /// <summary>Phân loại tải theo hai ngưỡng BR7 và BR8.</summary>
        public static HubLoadLevel Classify(decimal load)
        {
            if (load >= BlockedFrom) return HubLoadLevel.Full;
            if (load < QuietBelow) return HubLoadLevel.Quiet;
            return HubLoadLevel.Normal;
        }
    }
}
