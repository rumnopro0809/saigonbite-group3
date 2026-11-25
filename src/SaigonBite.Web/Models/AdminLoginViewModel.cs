using System.ComponentModel.DataAnnotations;

namespace SaigonBite.Web.Models
{
    public sealed class AdminLoginViewModel
    {
        [Required(ErrorMessage = "Nhập tên đăng nhập.")]
        public string Identifier { get; set; }

        [Required(ErrorMessage = "Nhập mật khẩu.")]
        public string Password { get; set; }

        public string ReturnUrl { get; set; }
    }
}
