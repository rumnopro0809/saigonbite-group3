using System;
using System.Web.Mvc;
using SaigonBite.Domain;
using SaigonBite.Infrastructure.Services;
using SaigonBite.Web.Security;
using SaigonBite.Web.ViewModels;

namespace SaigonBite.Web.Controllers
{
    /// <summary>
    /// Màn hình bếp (HT03, F07): xem đơn của bếp mình, xác nhận, chuẩn bị, báo xong, từ chối.
    /// Quyền và phạm vi bếp do KitchenBoardService kiểm tra theo tài khoản đang đăng nhập (HT01), không theo dữ liệu form.
    /// </summary>
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [KitchenOnly]
    public sealed class KitchenController : BaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            try
            {
                return View(new KitchenBoardService(Db).GetBoard(CurrentUser.Id));
            }
            catch (BusinessRuleException ex) when (ex.Rule == "UNAUTHORIZED")
            {
                // Tài khoản bị khóa hoặc mất hồ sơ sau khi đăng nhập: về trang đăng nhập chung (HT01).
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
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
                new KitchenBoardService(Db).Act(CurrentUser.Id, orderId, action, reason, version);
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

    }
}
