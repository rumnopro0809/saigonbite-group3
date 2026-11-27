using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Web.Models
{
    public sealed class LoginViewModel
    {
        public string Lang { get; set; }

        [Required(ErrorMessage = "Email or phone is required.")]
        public string Identifier { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public sealed class RegisterLocalViewModel
    {
        public string Lang { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(120)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vietnamese phone number is required.")]
        [MaxLength(30)]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Password must be at least 8 characters.")]
        [MinLength(8)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Simulated OTP code is required (demo: 123456).")]
        public string OtpCode { get; set; }
    }

    public sealed class RegisterTouristViewModel
    {
        public string Lang { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(120)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Passport number (6-9 chars) is required.")]
        [MaxLength(40)]
        public string PassportNumber { get; set; }

        [Required(ErrorMessage = "Nationality is required.")]
        [MaxLength(80)]
        public string Nationality { get; set; }

        [Required(ErrorMessage = "Valid email is required.")]
        [MaxLength(120)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password must be at least 8 characters.")]
        [MinLength(8)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Simulated email code is required (demo: 654321).")]
        public string EmailCode { get; set; }
    }

    public sealed class AdminUsersViewModel
    {
        public AdminUsersViewModel()
        {
            Users = new List<User>();
        }

        public string Lang { get; set; }
        public bool IncludeDeleted { get; set; }
        public IList<User> Users { get; set; }
    }
}
