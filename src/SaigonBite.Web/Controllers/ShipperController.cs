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
    /// Màn hình shipper (HT03, F08): đăng nhập, xem đơn được gán, lấy hàng, giao thành công (COD phải xác nhận đã thu tiền),
    /// giao thất bại (bắt buộc lý do). Quyền theo tài khoản trong vé; luật nằm ở ShipperService.
    /// </summary>
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public sealed class ShipperController : Controller
    {
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (StaffSession.ShipperUserId(User).HasValue) return Redirect(SafeTarget(returnUrl));
            return View(new AdminLoginViewModel { ReturnUrl = SafeTarget(returnUrl) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            bool en = HubText.IsEnglish(Request);

            string key = LoginThrottle.Key("shipper", Request);
            if (LoginThrottle.IsBlocked(key))
            {
                ModelState.AddModelError("", en
                    ? "Too many failed sign-ins. Try again in 10 minutes."
                    : "Đăng nhập sai quá nhiều lần. Vui lòng thử lại sau 10 phút.");
                return View(model);
            }

            User shipper;
            using (var db = new AppDbContext())
                shipper = new StaffAccountService(db).Authenticate(model.Identifier, model.Password, ActorRole.Shipper);

            if (shipper == null)
            {
                LoginThrottle.RecordFailure(key);
                ModelState.AddModelError("", en
                    ? "The username or password is incorrect."
                    : "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            LoginThrottle.Reset(key);
            StaffSession.SignInShipper(Response, shipper.Identifier, shipper.Id);
            return Redirect(SafeTarget(model.ReturnUrl));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        [HttpGet, ShipperOnly]
        public ActionResult Index()
        {
            using (var db = new AppDbContext())
            {
                try
                {
                    return View(new ShipperService(db).GetMyOrders(StaffSession.ShipperUserId(User).Value));
                }
                catch (BusinessRuleException ex) when (ex.Rule == "UNAUTHORIZED")
                {
                    FormsAuthentication.SignOut();
                    return RedirectToAction("Login");
                }
            }
        }

        [HttpPost, ShipperOnly, ValidateAntiForgeryToken]
        public ActionResult Act(int orderId, ShipperAction action, string reason, string rowVersion, bool cashCollected = false)
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
                    new ShipperService(db).Act(StaffSession.ShipperUserId(User).Value, orderId, action, cashCollected, reason, version);
                TempData["ShipperOk"] = Done(action, en);
            }
            catch (BusinessRuleException ex)
            {
                TempData["ShipperError"] = Explain(ex, action, en);
            }
            return RedirectToAction("Index");
        }

        private static string Done(ShipperAction action, bool en)
        {
            switch (action)
            {
                case ShipperAction.PickUp: return en ? "Order picked up. You are out for delivery." : "Đã lấy hàng, đang giao.";
                case ShipperAction.Deliver: return en ? "Delivered." : "Đã giao thành công.";
                default: return en ? "Marked as failed delivery." : "Đã ghi nhận giao thất bại.";
            }
        }

        private static string Explain(BusinessRuleException ex, ShipperAction action, bool en)
        {
            switch (ex.Rule)
            {
                case "CONCURRENCY":
                    return en ? "Someone changed this order first. The list was refreshed; check it and try again."
                              : "Đơn vừa được thay đổi ở nơi khác. Danh sách đã được tải lại, hãy kiểm tra rồi thử lại.";
                case "NOT_FOUND":
                    return en ? "The order no longer exists." : "Đơn không còn tồn tại.";
                case "UNAUTHORIZED":
                    return en ? "Your shipper account is not allowed to do this." : "Tài khoản shipper của bạn không có quyền thao tác này.";
                case "BR15":
                    if (action == ShipperAction.Fail)
                        return en ? "A failed delivery needs a reason, and the order must be out for delivery and assigned to you."
                                  : "Giao thất bại cần nhập lý do, và đơn phải đang giao và được gán cho bạn.";
                    if (action == ShipperAction.Deliver)
                        return en ? "Cash on delivery orders can only be completed after you confirm the cash was collected."
                                  : "Đơn thu tiền khi nhận chỉ hoàn tất sau khi bạn xác nhận đã thu tiền mặt.";
                    return en ? "That step is not allowed for this order's current status or assignment."
                              : "Bước này không hợp lệ với trạng thái hoặc việc gán của đơn.";
                default:
                    return HubText.Localize(ex.Message, en);
            }
        }

        private string SafeTarget(string returnUrl)
        {
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl : Url.Action("Index", "Shipper");
        }
    }
}
