using System.Collections.Generic;
using System.Linq;

namespace SaigonBite.Application
{
    /// <summary>
    /// Kiểm tra dữ liệu bếp trước khi lưu (FR14.1, FR14.3).
    /// Giao diện cũng có kiểm tra, nhưng server phải kiểm tra lại vì request có thể bị giả mạo.
    /// </summary>
    public static class HubValidator
    {
        public const int NameMaxLength = 100;
        public const int LocationMaxLength = 200;

        /// <param name="input">Dữ liệu người dùng gửi lên.</param>
        /// <param name="validBrandIds">Các Id brand đang tồn tại và chưa bị xóa.</param>
        /// <returns>Danh sách lỗi. Rỗng nghĩa là hợp lệ.</returns>
        public static IList<ValidationError> Validate(HubInput input, ICollection<int> validBrandIds)
        {
            var errors = new List<ValidationError>();
            if (input == null)
            {
                errors.Add(new ValidationError(null, "Thiếu dữ liệu bếp."));
                return errors;
            }

            if (string.IsNullOrWhiteSpace(input.Name))
                errors.Add(new ValidationError("Name", "Hãy nhập tên bếp."));
            else if (input.Name.Trim().Length > NameMaxLength)
                errors.Add(new ValidationError("Name", "Tên bếp tối đa " + NameMaxLength + " ký tự."));

            if (string.IsNullOrWhiteSpace(input.Location))
                errors.Add(new ValidationError("Location", "Hãy nhập vị trí của bếp."));
            else if (input.Location.Trim().Length > LocationMaxLength)
                errors.Add(new ValidationError("Location", "Vị trí tối đa " + LocationMaxLength + " ký tự."));

            // Công suất là mẫu số khi tính tải (BR8) nên phải lớn hơn 0.
            if (input.Capacity <= 0)
                errors.Add(new ValidationError("Capacity", "Công suất phải là số nguyên lớn hơn 0."));

            var brandIds = input.BrandIds ?? new List<int>();
            if (brandIds.Count != brandIds.Distinct().Count())
                errors.Add(new ValidationError("BrandIds", "Brand bị chọn trùng."));
            if (brandIds.Any(id => !validBrandIds.Contains(id)))
                errors.Add(new ValidationError("BrandIds", "Có brand không tồn tại hoặc đã bị xóa."));

            return errors;
        }
    }
}
