using System.Linq;
using System.Web;
using System.Web.Mvc;
using SaigonBite.Domain;
using SaigonBite.Infrastructure.Data;
using SaigonBite.Web.Infrastructure;

namespace SaigonBite.Web.Security
{
    /// <summary>
    /// UC03/HT03 (NFR04): chỉ cho tài khoản đúng vai trò vào trang. Dùng chung phiên đăng nhập của HT01
    /// (Session["CurrentUserId"] do AccountController đặt), nên không có trang đăng nhập riêng cho bếp hay shipper.
    /// Chưa đăng nhập hoặc sai vai trò thì chuyển về /Account/Login. Service vẫn kiểm tra lại quyền theo tài khoản.
    /// </summary>
    public abstract class RoleOnlyAttribute : AuthorizeAttribute
    {
        private readonly ActorRole _role;

        protected RoleOnlyAttribute(ActorRole role)
        {
            _role = role;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null || httpContext.Session == null) return false;
            object raw = httpContext.Session[CurrentActorHelper.UserIdSessionKey];
            if (!(raw is int)) return false;
            int userId = (int)raw;
            using (var db = new SaigonBiteDbContext())
            {
                return db.Users.Any(u => u.Id == userId && u.Role == _role && u.IsActive && !u.IsDeleted);
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var url = new UrlHelper(filterContext.RequestContext);
            filterContext.Result = new RedirectResult(url.Action("Login", "Account"));
        }
    }

    /// <summary>Chỉ System Admin (UC03 quản lý bếp, cờ mưa, R2 và gán shipper).</summary>
    public sealed class AdminOnlyAttribute : RoleOnlyAttribute
    {
        public AdminOnlyAttribute() : base(ActorRole.Admin) { }
    }

    /// <summary>Chỉ nhân viên bếp (HT03, FR17). Phạm vi bếp được KitchenBoardService kiểm tra theo StaffProfile.</summary>
    public sealed class KitchenOnlyAttribute : RoleOnlyAttribute
    {
        public KitchenOnlyAttribute() : base(ActorRole.KitchenStaff) { }
    }

    /// <summary>Chỉ shipper (HT03, FR17). Đơn của ai do ShipperService kiểm tra theo ShipperId.</summary>
    public sealed class ShipperOnlyAttribute : RoleOnlyAttribute
    {
        public ShipperOnlyAttribute() : base(ActorRole.Shipper) { }
    }
}
