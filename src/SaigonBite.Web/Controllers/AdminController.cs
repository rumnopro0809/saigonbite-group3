using System.Web.Mvc;
using System.Web.Security;
using SaigonBite.Domain;
using SaigonBite.Infrastructure;
using SaigonBite.Infrastructure.Security;
using SaigonBite.Web.Models;
using SaigonBite.Web.Security;
using SaigonBite.Web.ViewModels;

namespace SaigonBite.Web.Controllers
{
    /// <summary>Đăng nhập và đăng xuất quản trị (HT01, FR13, NFR04). Vai trò lưu trong vé Forms đã mã hóa.</summary>
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public sealed class AdminController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Hubs");
        }

        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
                return Redirect(SafeTarget(returnUrl));
            return View(new AdminLoginViewModel { ReturnUrl = SafeTarget(returnUrl) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            bool en = HubText.IsEnglish(Request);

            string key = LoginThrottle.Key("admin", Request);
            if (LoginThrottle.IsBlocked(key))
            {
                ModelState.AddModelError("", en
                    ? "Too many failed sign-ins. Try again in 10 minutes."
                    : "Đăng nhập sai quá nhiều lần. Vui lòng thử lại sau 10 phút.");
                return View(model);
            }

            User admin;
            using (var db = new AppDbContext())
                admin = new AdminAccountService(db).Authenticate(model.Identifier, model.Password);

            if (admin == null)
            {
                LoginThrottle.RecordFailure(key);
                ModelState.AddModelError("", en
                    ? "The username or password is incorrect."
                    : "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            LoginThrottle.Reset(key);
            StaffSession.SignInAdmin(Response, admin.Identifier);
            return Redirect(SafeTarget(model.ReturnUrl));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        private string SafeTarget(string returnUrl)
        {
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl : Url.Action("Index", "Hubs");
        }
    }
}
