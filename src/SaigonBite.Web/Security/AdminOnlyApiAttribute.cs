using System.Web.Http;
using System.Web.Http.Controllers;

namespace SaigonBite.Web.Security
{
    /// <summary>
    /// Chỉ cho Admin gọi API (NFR04). Nếu không đúng quyền, API trả mã 401.
    /// TẠM (HT01): xem AdminBypass.
    /// </summary>
    public sealed class AdminOnlyApiAttribute : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            if (AdminBypass.IsEnabled) return true;
            var user = actionContext.RequestContext.Principal;
            return user != null && user.Identity.IsAuthenticated && user.IsInRole("Admin");
        }
    }
}
