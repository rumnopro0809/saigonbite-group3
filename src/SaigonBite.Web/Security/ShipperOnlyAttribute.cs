using System.Web;
using System.Web.Mvc;

namespace SaigonBite.Web.Security
{
    /// <summary>
    /// Chỉ cho shipper đã đăng nhập vào trang giao hàng (FR17, NFR04). Chưa đăng nhập thì chuyển sang /Shipper/Login;
    /// việc đơn có gán cho shipper này hay không do ShipperService kiểm tra theo tài khoản trong vé.
    /// </summary>
    public sealed class ShipperOnlyAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            return httpContext.User != null
                && httpContext.User.Identity.IsAuthenticated
                && httpContext.User.IsInRole(StaffSession.ShipperRole)
                && StaffSession.ShipperUserId(httpContext.User).HasValue;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var url = new UrlHelper(filterContext.RequestContext);
            string target = filterContext.HttpContext.Request.HttpMethod == "GET"
                ? filterContext.HttpContext.Request.RawUrl : url.Action("Index", "Shipper");
            filterContext.Result = new RedirectResult(url.Action("Login", "Shipper", new { returnUrl = target }));
        }
    }
}
