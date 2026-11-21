using System;

namespace SaigonBite.Domain
{
    /// <summary>Cài đặt toàn hệ thống. Chỉ có đúng một dòng (Id = 1). Thuộc UC03 (FR16).</summary>
    public class SystemSetting
    {
        /// <summary>Id cố định của dòng cài đặt duy nhất.</summary>
        public const int SingleId = 1;

        public int Id { get; set; }

        /// <summary>Cờ mưa lớn. Chỉ ảnh hưởng đơn mới, không đổi đơn cũ (BR5, BR17).</summary>
        public bool IsHeavyRainOn { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; }
    }
}
