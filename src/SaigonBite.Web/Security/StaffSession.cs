using System;
using System.Security.Principal;
using System.Web;
using System.Web.Caching;
using System.Web.Security;

namespace SaigonBite.Web.Security
{
    /// <summary>
    /// Vé đăng nhập Forms của nhân viên (HT01): vai trò nằm trong vé đã mã hóa và ký, nên người dùng không tự đổi được.
    /// UserData: "Admin", "KitchenStaff:&lt;userId&gt;" hoặc "Shipper:&lt;userId&gt;". Mỗi trình duyệt chỉ giữ một phiên nhân viên.
    /// </summary>
    public static class StaffSession
    {
        public const string AdminRole = "Admin";
        public const string KitchenRole = "KitchenStaff";
        public const string ShipperRole = "Shipper";

        public static void SignInAdmin(HttpResponseBase response, string identifier)
        {
            Issue(response, identifier, AdminRole);
        }

        public static void SignInKitchen(HttpResponseBase response, string identifier, int userId)
        {
            Issue(response, identifier, KitchenRole + ":" + userId);
        }

        public static void SignInShipper(HttpResponseBase response, string identifier, int userId)
        {
            Issue(response, identifier, ShipperRole + ":" + userId);
        }

        /// <summary>Id của nhân viên bếp đang đăng nhập, hoặc null nếu người dùng hiện tại không phải nhân viên bếp.</summary>
        public static int? KitchenUserId(IPrincipal user)
        {
            return UserIdOf(user, KitchenRole);
        }

        /// <summary>Id của shipper đang đăng nhập, hoặc null nếu người dùng hiện tại không phải shipper.</summary>
        public static int? ShipperUserId(IPrincipal user)
        {
            return UserIdOf(user, ShipperRole);
        }

        private static int? UserIdOf(IPrincipal user, string role)
        {
            var identity = user == null ? null : user.Identity as FormsIdentity;
            if (identity == null || !identity.IsAuthenticated || identity.Ticket == null) return null;
            string data = identity.Ticket.UserData ?? string.Empty;
            int id;
            if (data.StartsWith(role + ":", StringComparison.Ordinal) &&
                int.TryParse(data.Substring(role.Length + 1), out id)) return id;
            return null;
        }

        /// <summary>Dựng người dùng có vai trò từ vé; vé không hợp lệ thì để nguyên (không có vai trò).</summary>
        public static IPrincipal Resolve(IPrincipal current)
        {
            var identity = current == null ? null : current.Identity as FormsIdentity;
            if (identity == null || !identity.IsAuthenticated || identity.Ticket == null) return current;
            string data = identity.Ticket.UserData ?? string.Empty;
            if (data == AdminRole) return new GenericPrincipal(identity, new[] { AdminRole });
            if (KitchenUserId(current).HasValue) return new GenericPrincipal(identity, new[] { KitchenRole });
            if (ShipperUserId(current).HasValue) return new GenericPrincipal(identity, new[] { ShipperRole });
            return current;
        }

        private static void Issue(HttpResponseBase response, string identifier, string userData)
        {
            DateTime now = DateTime.Now;
            var ticket = new FormsAuthenticationTicket(1, identifier, now, now.AddMinutes(60), false, userData);
            response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Secure = FormsAuthentication.RequireSSL,
                Path = FormsAuthentication.FormsCookiePath
            });
        }
    }

    /// <summary>Giới hạn đoán mật khẩu: 5 lần sai theo phạm vi và địa chỉ máy thì khóa 10 phút (lưu trong bộ nhớ tiến trình).</summary>
    public static class LoginThrottle
    {
        private const int MaxFailures = 5;
        private static readonly TimeSpan Lockout = TimeSpan.FromMinutes(10);

        public static string Key(string scope, HttpRequestBase request)
        {
            return scope + "-login-failures:" + request.UserHostAddress;
        }

        public static bool IsBlocked(string key)
        {
            return Count(key) >= MaxFailures;
        }

        public static void RecordFailure(string key)
        {
            HttpRuntime.Cache.Insert(key, Count(key) + 1, null, DateTime.UtcNow.Add(Lockout), Cache.NoSlidingExpiration);
        }

        public static void Reset(string key)
        {
            HttpRuntime.Cache.Remove(key);
        }

        private static int Count(string key)
        {
            object value = HttpRuntime.Cache[key];
            return value is int ? (int)value : 0;
        }
    }
}
