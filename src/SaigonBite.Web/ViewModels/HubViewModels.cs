using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SaigonBite.Application.Hubs;
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

        [Display(Name = "Quận")]
        [Required(ErrorMessage = "Hãy nhập quận của bếp.")]
        [StringLength(HubValidator.DistrictMaxLength, ErrorMessage = "Quận tối đa 80 ký tự.")]
        public string District { get; set; }

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
                District = dto.District,
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
                District = District,
                Capacity = Capacity,
                BrandIds = SelectedBrandIds ?? new List<int>(),
                RowVersion = RowVersion
            };
        }
    }

    /// <summary>Các hàm nhỏ giúp view chọn chữ và màu cho trạng thái bếp.</summary>
    public static class HubDisplay
    {
        public static string StatusText(HubDto hub, bool english = false)
        {
            if (!hub.IsActive) return english ? "Stopped accepting orders" : "Đã ngừng nhận đơn";
            if (hub.LoadLevel == HubLoadLevel.Full) return english ? "Paused (at capacity)" : "Tạm ngừng nhận đơn";
            return english ? "Accepting orders" : "Đang nhận đơn";
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

    /// <summary>
    /// Ngôn ngữ của trang quản trị (NFR03) và bản dịch tiếng Anh cho các thông báo của service.
    /// Service giữ thông báo tiếng Việt làm gốc; lớp web đổi sang tiếng Anh khi cookie sb_lang = en.
    /// </summary>
    public static class HubText
    {
        public static bool IsEnglish(System.Web.HttpRequestBase request)
        {
            var cookie = request == null ? null : request.Cookies["sb_lang"];
            return cookie != null && cookie.Value == "en";
        }

        private static readonly IDictionary<string, string> English = new Dictionary<string, string>
        {
            { "Thiếu dữ liệu bếp.", "Hub data is missing." },
            { "Hãy nhập tên bếp.", "Enter the hub name." },
            { "Tên bếp tối đa " + HubValidator.NameMaxLength + " ký tự.", "The hub name can have at most " + HubValidator.NameMaxLength + " characters." },
            { "Hãy nhập vị trí của bếp.", "Enter the hub location." },
            { "Vị trí tối đa " + HubValidator.LocationMaxLength + " ký tự.", "The location can have at most " + HubValidator.LocationMaxLength + " characters." },
            { "Hãy nhập quận của bếp.", "Enter the hub district." },
            { "Quận tối đa " + HubValidator.DistrictMaxLength + " ký tự.", "The district can have at most " + HubValidator.DistrictMaxLength + " characters." },
            { "Công suất phải là số nguyên lớn hơn 0.", "Capacity must be a whole number greater than 0." },
            { "Brand bị chọn trùng.", "A brand was selected more than once." },
            { "Có brand không tồn tại hoặc đã bị xóa.", "A selected brand does not exist or was deleted." },
            { "Không tìm thấy bếp.", "Hub not found." },
            { "Bếp này vừa được người khác sửa. Hãy tải lại trang rồi thử lại.", "Someone else just changed this hub. Reload the page and try again." },
            { "Thiếu phiên bản dữ liệu (rowVersion). Hãy tải lại trang.", "The data version (rowVersion) is missing. Reload the page." },
            { "Phiên bản dữ liệu (rowVersion) không đúng định dạng.", "The data version (rowVersion) has an invalid format." },
            { "Cờ mưa vừa được người khác đổi. Hãy tải lại trang rồi thử lại.", "Someone else just changed the rain flag. Reload the page and try again." },
            { "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.", "The end date must be on or after the start date." },
            { "Không tạo được báo cáo.", "The report could not be created." },
            { "Đã tạo bếp mới.", "The hub was created." },
            { "Đã lưu thay đổi.", "Changes were saved." },
            { "Không tìm thấy dữ liệu.", "Data not found." },
            { "Dữ liệu không hợp lệ.", "The data is not valid." },
            { "Thiếu dữ liệu bếp trong nội dung request.", "Hub data is missing from the request body." },
            { "Thiếu dữ liệu trong nội dung request.", "Data is missing from the request body." },
            { "Cần có cả hai tham số from và to (định dạng yyyy-MM-dd).", "Both from and to are required (format yyyy-MM-dd)." }
        };

        public static string Localize(string message, bool english)
        {
            string translated;
            return english && message != null && English.TryGetValue(message, out translated) ? translated : message;
        }
    }
}
