using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using SaigonBite.Domain;

namespace SaigonBite.Infrastructure.Security
{
    /// <summary>Một nhân viên bếp để hiển thị trong trang quản trị tài khoản.</summary>
    public sealed class KitchenStaffInfo
    {
        public int UserId { get; set; }
        public string Identifier { get; set; }
        public string FullName { get; set; }
        public int HubId { get; set; }
        public string HubName { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Xác thực và tạo tài khoản nhân viên (HT01, HT03, FR12, FR13, NFR04): Admin và Kitchen Staff.
    /// Mỗi vai trò đăng nhập ở trang riêng; tài khoản của vai trò này không dùng được ở vai trò khác.
    /// </summary>
    public sealed class StaffAccountService
    {
        public const int IdentifierMinLength = 3;
        public const int IdentifierMaxLength = 40;
        public const int NameMaxLength = 120;
        public const int PasswordMinLength = 8;

        // Băm giả để thời gian phản hồi khi sai tên đăng nhập gần giống khi sai mật khẩu.
        private static readonly string DummyHash = PasswordHasher.Hash(Guid.NewGuid().ToString("N"));
        private readonly AppDbContext _db;

        public StaffAccountService(AppDbContext db)
        {
            if (db == null) throw new ArgumentNullException("db");
            _db = db;
        }

        /// <summary>Trả về tài khoản đang hoạt động đúng vai trò nếu đúng thông tin, ngược lại trả null.</summary>
        public User Authenticate(string identifier, string password, ActorRole role)
        {
            if (role != ActorRole.Admin && role != ActorRole.KitchenStaff && role != ActorRole.Shipper) return null;
            if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrEmpty(password)) return null;

            string id = identifier.Trim();
            User user = _db.Users.Include(u => u.StaffProfile).SingleOrDefault(u => u.Identifier == id);
            bool passwordOk = PasswordHasher.Verify(password, user == null ? DummyHash : user.PasswordHash);
            if (user == null || !passwordOk) return null;
            if (user.Role != role || !user.IsActive || user.IsDeleted) return null;
            if (role == ActorRole.KitchenStaff && user.StaffProfile == null) return null;
            return user;
        }

        /// <summary>Tạo tài khoản nhân viên bếp gắn với một bếp. Không lưu gì nếu có lỗi.</summary>
        public User CreateKitchenStaff(string identifier, string fullName, string password, int hubId)
        {
            string id = ValidateNewAccount(identifier, fullName, password, out string name);

            if (!_db.Hubs.Any(h => h.Id == hubId && !h.IsDeleted))
                throw new BusinessRuleException("HUB_NOT_FOUND", "The hub does not exist or was deleted.");

            var user = new User
            {
                Identifier = id,
                FullName = name,
                PasswordHash = PasswordHasher.Hash(password),
                Role = ActorRole.KitchenStaff,
                IsActive = true,
                IsDeleted = false,
                StaffProfile = new StaffProfile { HubId = hubId }
            };
            _db.Users.Add(user);
            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Chỉ mục duy nhất là chốt chặn cuối khi hai yêu cầu cùng lúc.
                if (user.StaffProfile != null) _db.Entry(user.StaffProfile).State = EntityState.Detached;
                _db.Entry(user).State = EntityState.Detached;
                throw new BusinessRuleException("DUPLICATE_IDENTIFIER", "This identifier is already in use.");
            }
            return user;
        }

        /// <summary>Tạo tài khoản shipper (không gắn bếp). Không lưu gì nếu có lỗi.</summary>
        public User CreateShipper(string identifier, string fullName, string password)
        {
            string id = ValidateNewAccount(identifier, fullName, password, out string name);
            var user = new User
            {
                Identifier = id,
                FullName = name,
                PasswordHash = PasswordHasher.Hash(password),
                Role = ActorRole.Shipper,
                IsActive = true,
                IsDeleted = false
            };
            _db.Users.Add(user);
            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _db.Entry(user).State = EntityState.Detached;
                throw new BusinessRuleException("DUPLICATE_IDENTIFIER", "This identifier is already in use.");
            }
            return user;
        }

        private string ValidateNewAccount(string identifier, string fullName, string password, out string name)
        {
            string id = (identifier ?? string.Empty).Trim();
            if (id.Length < IdentifierMinLength || id.Length > IdentifierMaxLength || !IsValidIdentifier(id))
                throw new BusinessRuleException("INVALID_IDENTIFIER",
                    "Identifier must be 3-40 letters, digits, '.', '_' or '-'.");

            name = (fullName ?? string.Empty).Trim();
            if (name.Length == 0 || name.Length > NameMaxLength)
                throw new BusinessRuleException("INVALID_NAME", "Full name is required (at most 120 characters).");

            if (string.IsNullOrEmpty(password) || password.Length < PasswordMinLength)
                throw new BusinessRuleException("INVALID_PASSWORD", "Password must have at least 8 characters.");

            // Gồm cả tài khoản đã xóa mềm: tên đăng nhập không được dùng lại cho người khác.
            if (_db.Users.Any(u => u.Identifier == id))
                throw new BusinessRuleException("DUPLICATE_IDENTIFIER", "This identifier is already in use.");
            return id;
        }

        /// <summary>Danh sách nhân viên bếp chưa bị xóa, kèm tên bếp.</summary>
        public IList<KitchenStaffInfo> ListKitchenStaff()
        {
            return _db.Users
                .Where(u => u.Role == ActorRole.KitchenStaff && !u.IsDeleted && u.StaffProfile != null)
                .OrderBy(u => u.Identifier)
                .Select(u => new KitchenStaffInfo
                {
                    UserId = u.Id,
                    Identifier = u.Identifier,
                    FullName = u.FullName,
                    HubId = u.StaffProfile.HubId,
                    HubName = u.StaffProfile.Hub.Name,
                    IsActive = u.IsActive
                })
                .ToList();
        }

        private static bool IsValidIdentifier(string value)
        {
            foreach (char c in value)
            {
                bool ok = (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')
                    || c == '.' || c == '_' || c == '-';
                if (!ok) return false;
            }
            return true;
        }

        private static bool IsUniqueViolation(Exception exception)
        {
            for (Exception inner = exception; inner != null; inner = inner.InnerException)
                if (inner is SqlException sql && (sql.Number == 2601 || sql.Number == 2627)) return true;
            return false;
        }
    }
}
