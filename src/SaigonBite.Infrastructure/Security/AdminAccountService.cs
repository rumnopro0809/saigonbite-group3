using System;
using SaigonBite.Domain;

namespace SaigonBite.Infrastructure.Security
{
    /// <summary>
    /// Xác thực tài khoản Admin (HT01, NFR04). Đăng nhập khách hàng, admin và bếp tách riêng:
    /// tài khoản của vai trò này không vào được trang của vai trò khác.
    /// </summary>
    public sealed class AdminAccountService
    {
        private readonly StaffAccountService _staff;

        public AdminAccountService(AppDbContext db)
        {
            if (db == null) throw new ArgumentNullException("db");
            _staff = new StaffAccountService(db);
        }

        /// <summary>Trả về Admin đang hoạt động nếu đúng thông tin, ngược lại trả null.</summary>
        public User Authenticate(string identifier, string password)
        {
            return _staff.Authenticate(identifier, password, ActorRole.Admin);
        }
    }
}
