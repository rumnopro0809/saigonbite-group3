using System;
using System.Linq;
using System.Web.Mvc;
using SaigonBite.Domain;
using SaigonBite.Infrastructure.Services;
using SaigonBite.Web.ViewModels;

namespace SaigonBite.Web.Controllers
{
    /// <summary>Admin gán shipper cho đơn đã Ready (AMB01, FR17). Chỉ Admin (kế thừa [AdminOnly]).</summary>
    public sealed class DispatchController : Uc03Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.ActiveTab = "dispatch";
            var service = new ShipperService(Db);
            var model = new DispatchViewModel { Shippers = service.ListShippers() };
            int? adminId = CurrentAdminId();
            model.Orders = adminId.HasValue ? service.ReadyOrders(adminId.Value) : new DispatchOrderCard[0];
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Assign(int orderId, int shipperId, string rowVersion)
        {
            bool en = English;
            int? adminId = CurrentAdminId();
            if (!adminId.HasValue)
            {
                TempData["DispatchError"] = en ? "Sign in with an admin account to assign shippers." : "Hãy đăng nhập bằng tài khoản Admin để gán shipper.";
                return RedirectToAction("Index");
            }

            byte[] version = null;
            if (!string.IsNullOrEmpty(rowVersion))
            {
                try { version = Convert.FromBase64String(rowVersion); }
                catch (FormatException) { version = null; }
            }

            try
            {
                new ShipperService(Db).AssignShipper(adminId.Value, orderId, shipperId, version);
                TempData["DispatchOk"] = en ? "Shipper assigned." : "Đã gán shipper.";
            }
            catch (BusinessRuleException ex)
            {
                TempData["DispatchError"] = Explain(ex, en);
            }
            return RedirectToAction("Index");
        }

        /// <summary>Id tài khoản Admin đang đăng nhập (vé chỉ chứa tên đăng nhập).</summary>
        private int? CurrentAdminId()
        {
            string name = User.Identity.Name;
            if (string.IsNullOrEmpty(name)) return null;
            int id = Db.Users.Where(u => u.Identifier == name && u.Role == ActorRole.Admin && u.IsActive && !u.IsDeleted)
                .Select(u => u.Id).FirstOrDefault();
            return id == 0 ? (int?)null : id;
        }

        private static string Explain(BusinessRuleException ex, bool en)
        {
            switch (ex.Rule)
            {
                case "CONCURRENCY":
                    return en ? "Someone changed this order first. The list was refreshed; try again." : "Đơn vừa được thay đổi ở nơi khác. Danh sách đã được tải lại, hãy thử lại.";
                case "NOT_FOUND":
                    return en ? "The order no longer exists." : "Đơn không còn tồn tại.";
                case "BR15":
                    return en ? "Only an order that is Ready can be assigned." : "Chỉ gán được shipper cho đơn đang ở trạng thái Sẵn sàng giao.";
                case "SHIPPER_NOT_FOUND":
                    return en ? "That shipper does not exist or is inactive." : "Shipper không tồn tại hoặc đã ngừng hoạt động.";
                default:
                    return en ? "You are not allowed to do this." : "Bạn không có quyền thực hiện thao tác này.";
            }
        }
    }
}
