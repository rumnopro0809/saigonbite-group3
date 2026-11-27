using System;
using System.Web.Mvc;
using SaigonBite.Domain;
using SaigonBite.Infrastructure.Services;
using SaigonBite.Web.Security;
using SaigonBite.Web.ViewModels;

namespace SaigonBite.Web.Controllers
{
    /// <summary>
    /// Màn hình shipper (HT03, F08): xem đơn được gán, lấy hàng, giao thành công (COD phải xác nhận đã thu tiền),
    /// giao thất bại (bắt buộc lý do). Quyền theo tài khoản đang đăng nhập (HT01); luật nằm ở ShipperService.
    /// </summary>
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [ShipperOnly]
    public sealed class ShipperController : BaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            try
            {
                return View(new ShipperService(Db).GetMyOrders(CurrentUser.Id));
            }
            catch (BusinessRuleException ex) when (ex.Rule == "UNAUTHORIZED")
            {
                // Tài khoản bị khóa hoặc mất hồ sơ sau khi đăng nhập: về trang đăng nhập chung (HT01).
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
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
                new ShipperService(Db).Act(CurrentUser.Id, orderId, action, cashCollected, reason, version);
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

    }
}
