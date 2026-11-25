using System.Web;
using System.Web.Mvc;

namespace SaigonBite.Web.Security
{
    /// <summary>
    /// Chỉ cho nhân viên bếp đã đăng nhập vào trang bếp (FR13, NFR04). Chưa đăng nhập thì chuyển sang
    /// /Kitchen/Login; phạm vi bếp được kiểm tra thêm ở KitchenBoardService theo tài khoản trong vé.
    /// </summary>
    public sealed class KitchenOnlyAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            return httpContext.User != null
                && httpContext.User.Identity.IsAuthenticated
                && httpContext.User.IsInRole(StaffSession.KitchenRole)
                && StaffSession.KitchenUserId(httpContext.User).HasValue;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var url = new UrlHelper(filterContext.RequestContext);
            string target = filterContext.HttpContext.Request.HttpMethod == "GET"
                ? filterContext.HttpContext.Request.RawUrl : url.Action("Index", "Kitchen");
            filterContext.Result = new RedirectResult(url.Action("Login", "Kitchen", new { returnUrl = target }));
        }
    }
}
