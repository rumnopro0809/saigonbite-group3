using System.Collections.Generic;

namespace SaigonBite.Domain
{
    /// <summary>Một bếp (hub) nhận và chuẩn bị đơn. Thuộc UC03 (FR14).</summary>
    public class Hub
    {
        public int Id { get; set; }

        /// <summary>Tên hiển thị, ví dụ "Bếp Quận 1". (Đề xuất của nhóm, chờ xác nhận.)</summary>
        public string Name { get; set; }

        /// <summary>Địa chỉ hoặc vị trí của bếp.</summary>
        public string Location { get; set; }

        /// <summary>Công suất: số đơn tối đa bếp xử lý cùng lúc. Phải lớn hơn 0.</summary>
        public int Capacity { get; set; }

        /// <summary>False nghĩa là bếp không nhận đơn mới, đơn đang chạy vẫn xử lý tiếp (FR14.4).</summary>
        public bool IsActive { get; set; }

        /// <summary>Xóa mềm: giữ lại dữ liệu để lịch sử đơn hàng vẫn đúng.</summary>
        public bool IsDeleted { get; set; }

        /// <summary>Dùng để phát hiện hai người sửa cùng lúc (rowversion).</summary>
        public byte[] RowVersion { get; set; }

        /// <summary>Các brand mà bếp này phục vụ.</summary>
        public virtual ICollection<HubBrand> Brands { get; set; }
        public virtual ICollection<StaffProfile> StaffProfiles { get; set; } = new List<StaffProfile>();

        public Hub()
        {
            Brands = new List<HubBrand>();
        }
    }
}
