using System.Configuration;
using System.Web;
using System.Web.Mvc;

namespace SaigonBite.Web.Security
{
    /// <summary>
    /// Chỉ cho người dùng có vai trò Admin vào trang MVC (NFR04).
    /// TẠM (HT01): khi chưa có đăng nhập, cấu hình "Uc03:BypassAdminLogin" = true sẽ cho qua.
    /// </summary>
    public sealed class AdminOnlyAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (AdminBypass.IsEnabled) return true;
            return httpContext.User != null
                && httpContext.User.Identity.IsAuthenticated
                && httpContext.User.IsInRole("Admin");
        }
    }

    /// <summary>Đọc cấu hình bỏ qua đăng nhập tạm thời. Dùng chung cho cả MVC và API.</summary>
    public static class AdminBypass
    {
        public static bool IsEnabled
        {
            get { return ConfigurationManager.AppSettings["Uc03:BypassAdminLogin"] == "true"; }
        }
    }
}
