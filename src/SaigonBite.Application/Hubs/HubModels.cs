using System.Collections.Generic;
using SaigonBite.Domain;

namespace SaigonBite.Application.Hubs
{
    /// <summary>Dữ liệu một bếp trả về cho giao diện và API (FR14.2).</summary>
    public sealed class HubDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        /// <summary>Địa chỉ bếp (Hub.Address).</summary>
        public string Location { get; set; }
        public string District { get; set; }
        public int Capacity { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Danh sách Id brand mà bếp phục vụ.</summary>
        public IList<int> BrandIds { get; set; }

        /// <summary>Tên các brand, để hiển thị.</summary>
        public IList<string> BrandNames { get; set; }

        /// <summary>Số đơn đang xử lý (Confirmed + Preparing). Được tính ra, không nhập tay (BR8).</summary>
        public int ActiveOrders { get; set; }

        /// <summary>Tải hiện tại, 0.2 nghĩa là 20%. Có thể lớn hơn 1 nếu công suất bị giảm.</summary>
        public decimal Load { get; set; }

        /// <summary>Tải theo phần trăm đã làm tròn, dùng để hiển thị.</summary>
        public int LoadPercent { get; set; }

        public HubLoadLevel LoadLevel { get; set; }

        /// <summary>Phiên bản dữ liệu (base64). Gửi lại khi sửa để phát hiện xung đột.</summary>
        public string RowVersion { get; set; }
    }

    /// <summary>Dữ liệu người dùng gửi lên khi tạo hoặc sửa bếp (FR14.1, FR14.3).</summary>
    public sealed class HubInput
    {
        public string Name { get; set; }
        /// <summary>Địa chỉ bếp (Hub.Address).</summary>
        public string Location { get; set; }
        public string District { get; set; }
        public int Capacity { get; set; }
        public IList<int> BrandIds { get; set; }

        /// <summary>Bắt buộc khi sửa, bỏ trống khi tạo mới.</summary>
        public string RowVersion { get; set; }

        public HubInput()
        {
            BrandIds = new List<int>();
        }
    }

    /// <summary>Một brand để hiển thị trong ô chọn khi tạo hoặc sửa bếp.</summary>
    public sealed class BrandOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>Cờ mưa lớn toàn hệ thống (FR16).</summary>
    public sealed class RainFlagDto
    {
        public bool IsHeavyRainOn { get; set; }
        public System.DateTimeOffset UpdatedAt { get; set; }
        public string RowVersion { get; set; }
    }

    /// <summary>Dữ liệu gửi lên để đổi trạng thái hoạt động của bếp.</summary>
    public sealed class HubActiveInput
    {
        public bool IsActive { get; set; }
        public string RowVersion { get; set; }
    }

    /// <summary>Dữ liệu gửi lên để bật hoặc tắt cờ mưa.</summary>
    public sealed class RainFlagInput
    {
        public bool IsHeavyRainOn { get; set; }
        public string RowVersion { get; set; }
    }
}
