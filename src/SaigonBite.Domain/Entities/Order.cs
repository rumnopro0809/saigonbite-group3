using System;

namespace SaigonBite.Domain
{
    /// <summary>
    /// TẠM (UC01): bản rút gọn của Order, chỉ có các cột UC03 cần để tính tải và báo cáo R2.
    /// Khi UC01 (Triều) thêm Order đầy đủ thì gộp vào, giữ nguyên các cột này.
    /// </summary>
    public class Order
    {
        public int Id { get; set; }
        public int HubId { get; set; }
        public virtual Hub Hub { get; set; }
        public OrderStatus Status { get; set; }
        public DateTimeOffset PlacedAt { get; set; }

        /// <summary>Ảnh chụp lúc đặt: đơn này có được giảm Quiet Kitchen không (BR7, BR17, AMB06).</summary>
        public bool QuietDiscountGranted { get; set; }
    }

    /// <summary>
    /// TẠM (HT03): bản rút gọn của lịch sử đổi trạng thái đơn. R2 lấy mốc Confirmed và Ready từ đây.
    /// </summary>
    public class OrderStatusHistory
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public virtual Order Order { get; set; }
        public OrderStatus From { get; set; }
        public OrderStatus To { get; set; }
        public DateTimeOffset ChangedAt { get; set; }
    }
}
