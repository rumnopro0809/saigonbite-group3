using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SaigonBite.Application;
using SaigonBite.Domain;

namespace SaigonBite.Web.ViewModels
{
    /// <summary>Dữ liệu cho trang danh sách bếp (màn hình F06).</summary>
    public sealed class HubIndexVm
    {
        public IList<HubDto> Hubs { get; set; }
        public RainFlagDto Rain { get; set; }
    }

    /// <summary>
    /// Dữ liệu của form tạo/sửa bếp. Dùng ViewModel riêng, không bind thẳng vào entity,
    /// để người dùng không gửi thêm trường như IsDeleted (chống over-posting).
    /// </summary>
    public sealed class HubFormVm
    {
        public int Id { get; set; }

        [Display(Name = "Tên bếp")]
        [Required(ErrorMessage = "Hãy nhập tên bếp.")]
        [StringLength(HubValidator.NameMaxLength, ErrorMessage = "Tên bếp tối đa 100 ký tự.")]
        public string Name { get; set; }

        [Display(Name = "Vị trí")]
        [Required(ErrorMessage = "Hãy nhập vị trí của bếp.")]
        [StringLength(HubValidator.LocationMaxLength, ErrorMessage = "Vị trí tối đa 200 ký tự.")]
        public string Location { get; set; }

        [Display(Name = "Công suất (số đơn tối đa cùng lúc)")]
        [Range(1, int.MaxValue, ErrorMessage = "Công suất phải là số nguyên lớn hơn 0.")]
        public int Capacity { get; set; }

        [Display(Name = "Brand phục vụ")]
        public List<int> SelectedBrandIds { get; set; }

        /// <summary>Phiên bản dữ liệu, chỉ dùng khi sửa.</summary>
        public string RowVersion { get; set; }

        /// <summary>Danh sách brand để hiển thị ô chọn. Không nhận từ form.</summary>
        public IList<BrandOptionDto> AllBrands { get; set; }

        public HubFormVm()
        {
            SelectedBrandIds = new List<int>();
            AllBrands = new List<BrandOptionDto>();
        }

        public static HubFormVm FromDto(HubDto dto)
        {
            return new HubFormVm
            {
                Id = dto.Id,
                Name = dto.Name,
                Location = dto.Location,
                Capacity = dto.Capacity,
                SelectedBrandIds = new List<int>(dto.BrandIds),
                RowVersion = dto.RowVersion
            };
        }

        public HubInput ToInput()
        {
            return new HubInput
            {
                Name = Name,
                Location = Location,
                Capacity = Capacity,
                BrandIds = SelectedBrandIds ?? new List<int>(),
                RowVersion = RowVersion
            };
        }
    }

    /// <summary>Các hàm nhỏ giúp view chọn chữ và màu cho trạng thái bếp.</summary>
    public static class HubDisplay
    {
        public static string StatusText(HubDto hub)
        {
            if (!hub.IsActive) return "Đã ngừng nhận đơn";
            return hub.LoadLevel == HubLoadLevel.Full ? "Tạm ngừng nhận đơn" : "Đang nhận đơn";
        }

        /// <summary>Bếp đầy tải (BR8) hoặc đã bị ngừng thì hiện chip đỏ, còn lại chip xanh.</summary>
        public static string StatusCss(HubDto hub)
        {
            return (!hub.IsActive || hub.LoadLevel == HubLoadLevel.Full) ? "sb-chip sb-chip-bad" : "sb-chip sb-chip-ok";
        }

        public static string LoadCss(HubLoadLevel level)
        {
            return level == HubLoadLevel.Full ? "sb-chip sb-chip-bad" : "sb-chip sb-chip-ok";
        }
    }

    /// <summary>Một nút chọn nhanh khoảng ngày trên trang báo cáo R2.</summary>
    public sealed class DatePreset
    {
        public string Label { get; set; }
        public string From { get; set; }
        public string To { get; set; }
    }

    /// <summary>Dữ liệu cho trang báo cáo R2 (màn hình F10).</summary>
    public sealed class HubReportVm
    {
        public HubUtilizationReport Report { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string ErrorMessage { get; set; }
        public IList<DatePreset> Presets { get; set; }
    }
}
