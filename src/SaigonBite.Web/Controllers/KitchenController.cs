using System;
using System.Web.Mvc;
using System.Web.Security;
using SaigonBite.Domain;
using SaigonBite.Infrastructure;
using SaigonBite.Infrastructure.Security;
using SaigonBite.Infrastructure.Services;
using SaigonBite.Web.Models;
using SaigonBite.Web.Security;
using SaigonBite.Web.ViewModels;

namespace SaigonBite.Web.Controllers
{
    /// <summary>
    /// Màn hình bếp (HT03, F07): đăng nhập nhân viên bếp, xem đơn của bếp mình, xác nhận, chuẩn bị, báo xong, từ chối.
    /// Quyền và phạm vi bếp do KitchenBoardService kiểm tra theo tài khoản trong vé, không theo dữ liệu form.
    /// </summary>
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public sealed class KitchenController : Controller
    {
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (StaffSession.KitchenUserId(User).HasValue) return Redirect(SafeTarget(returnUrl));
            return View(new AdminLoginViewModel { ReturnUrl = SafeTarget(returnUrl) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            bool en = HubText.IsEnglish(Request);

            string key = LoginThrottle.Key("kitchen", Request);
            if (LoginThrottle.IsBlocked(key))
            {
                ModelState.AddModelError("", en
                    ? "Too many failed sign-ins. Try again in 10 minutes."
                    : "Đăng nhập sai quá nhiều lần. Vui lòng thử lại sau 10 phút.");
                return View(model);
            }

            User staff;
            using (var db = new AppDbContext())
                staff = new StaffAccountService(db).Authenticate(model.Identifier, model.Password, ActorRole.KitchenStaff);

            if (staff == null)
            {
                LoginThrottle.RecordFailure(key);
                ModelState.AddModelError("", en
                    ? "The username or password is incorrect."
                    : "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            LoginThrottle.Reset(key);
            StaffSession.SignInKitchen(Response, staff.Identifier, staff.Id);
            return Redirect(SafeTarget(model.ReturnUrl));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        [HttpGet, KitchenOnly]
        public ActionResult Index()
        {
            using (var db = new AppDbContext())
            {
                try
                {
                    return View(new KitchenBoardService(db).GetBoard(StaffSession.KitchenUserId(User).Value));
                }
                catch (BusinessRuleException ex) when (ex.Rule == "UNAUTHORIZED")
                {
                    // Tài khoản bị khóa hoặc mất hồ sơ bếp sau khi đăng nhập: bỏ vé và quay lại đăng nhập.
                    FormsAuthentication.SignOut();
                    return RedirectToAction("Login");
                }
            }
        }

        [HttpPost, KitchenOnly, ValidateAntiForgeryToken]
        public ActionResult Act(int orderId, KitchenAction action, string reason, string rowVersion)
        {
            bool en = HubText.IsEnglish(Request);
            byte[] version = null;
            if (!string.IsNullOrEmpty(rowVersion))
            {
                try { version = Convert.FromBase64String(rowVersion); }
                catch (FormatException) { version = null; }
            }

            try
            {
                using (var db = new AppDbContext())
                    new KitchenBoardService(db).Act(StaffSession.KitchenUserId(User).Value, orderId, action, reason, version);
                TempData["KitchenOk"] = Done(action, en);
            }
            catch (BusinessRuleException ex)
            {
                TempData["KitchenError"] = Explain(ex, action, en);
            }
            return RedirectToAction("Index");
        }

        private static string Done(KitchenAction action, bool en)
        {
            switch (action)
            {
                case KitchenAction.Confirm: return en ? "Order confirmed." : "Đã xác nhận đơn.";
                case KitchenAction.StartPreparing: return en ? "Preparation started." : "Đã bắt đầu chuẩn bị.";
                case KitchenAction.MarkReady: return en ? "Order is ready for pickup." : "Đơn đã sẵn sàng giao.";
                default: return en ? "Order rejected and the customer was refunded if already paid." : "Đã từ chối đơn; khách được hoàn tiền nếu đã thanh toán.";
            }
        }

        private static string Explain(BusinessRuleException ex, KitchenAction action, bool en)
        {
            switch (ex.Rule)
            {
                case "CONCURRENCY":
                    return en ? "Someone changed this order first. The board was refreshed; check it and try again."
                              : "Đơn vừa được thay đổi ở nơi khác. Bảng đã được tải lại, hãy kiểm tra rồi thử lại.";
                case "NOT_FOUND":
                    return en ? "The order no longer exists." : "Đơn không còn tồn tại.";
                case "UNAUTHORIZED":
                    return en ? "Your kitchen account is not allowed to do this." : "Tài khoản bếp của bạn không có quyền thao tác này.";
                case "BR15":
                    return action == KitchenAction.Reject
                        ? (en ? "A rejection needs a reason, and only a Placed order of your hub can be rejected."
                              : "Từ chối cần nhập lý do, và chỉ từ chối được đơn vừa đặt của bếp bạn.")
                        : (en ? "That step is not allowed for this order's current status or hub."
                              : "Bước này không hợp lệ với trạng thái hiện tại hoặc bếp của đơn.");
                default:
                    return HubText.Localize(ex.Message, en);
            }
        }

        private string SafeTarget(string returnUrl)
        {
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl : Url.Action("Index", "Kitchen");
        }
    }
}
