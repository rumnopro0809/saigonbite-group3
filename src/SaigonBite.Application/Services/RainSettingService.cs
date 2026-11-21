using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using SaigonBite.Domain;

namespace SaigonBite.Application
{
    /// <summary>Đọc và ghi cờ mưa lớn toàn hệ thống (FR16). UC01 đọc cờ này khi chụp snapshot đơn mới.</summary>
    public sealed class RainSettingService
    {
        private readonly IAppDbContext _db;

        public RainSettingService(IAppDbContext db)
        {
            _db = db;
        }

        /// <summary>Lấy trạng thái cờ mưa. Nếu chưa có dòng cài đặt thì tạo dòng mặc định (tắt).</summary>
        public RainFlagDto Get()
        {
            return ToDto(GetOrCreate());
        }

        /// <summary>Bật hoặc tắt cờ mưa. Chỉ ảnh hưởng đơn đặt sau đó, không đổi đơn cũ (BR5, BR17).</summary>
        public ServiceResult<RainFlagDto> Set(bool isOn, string rowVersionText)
        {
            var setting = GetOrCreate();

            byte[] rowVersion = null;
            if (string.IsNullOrEmpty(rowVersionText))
            {
                return ServiceResult<RainFlagDto>.Invalid(new List<ValidationError>
                {
                    new ValidationError("RowVersion", "Thiếu phiên bản dữ liệu (rowVersion). Hãy tải lại trang.")
                });
            }
            try { rowVersion = Convert.FromBase64String(rowVersionText); }
            catch (FormatException)
            {
                return ServiceResult<RainFlagDto>.Invalid(new List<ValidationError>
                {
                    new ValidationError("RowVersion", "Phiên bản dữ liệu (rowVersion) không đúng định dạng.")
                });
            }

            _db.SetOriginalRowVersion(setting, rowVersion);
            setting.IsHeavyRainOn = isOn;
            setting.UpdatedAt = DateTimeOffset.UtcNow;
            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<RainFlagDto>.Conflict("Cờ mưa vừa được người khác đổi. Hãy tải lại trang rồi thử lại.");
            }
            return ServiceResult<RainFlagDto>.Ok(ToDto(setting));
        }

        private SystemSetting GetOrCreate()
        {
            var setting = _db.SystemSettings.FirstOrDefault(s => s.Id == SystemSetting.SingleId);
            if (setting == null)
            {
                setting = new SystemSetting { Id = SystemSetting.SingleId, IsHeavyRainOn = false, UpdatedAt = DateTimeOffset.UtcNow };
                _db.SystemSettings.Add(setting);
                _db.SaveChanges();
            }
            return setting;
        }

        private static RainFlagDto ToDto(SystemSetting s)
        {
            return new RainFlagDto
            {
                IsHeavyRainOn = s.IsHeavyRainOn,
                UpdatedAt = s.UpdatedAt,
                RowVersion = s.RowVersion == null ? null : Convert.ToBase64String(s.RowVersion)
            };
        }
    }
}
