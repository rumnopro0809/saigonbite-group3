using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using SaigonBite.Application.Orders;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Services;
using SaigonBite.Web.Models;

namespace SaigonBite.Web.Controllers
{
    public class OrdersController : BaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            if (CurrentUser == null)
            {
                TempData["AuthNotice"] = CurrentLang == "vi"
                    ? "Vui lòng chọn một tài khoản demo trên thanh điều hướng để xem đơn hàng."
                    : "Please select a demo account from the top bar to view orders.";
                return RedirectToAction("Index", "Home");
            }

            var service = new OrderService(Db);
            List<Order> orders = service.GetOrdersForActor(CurrentUser.Id);

            return View(new OrderListViewModel
            {
                Orders = orders,
                CurrentUser = CurrentUser,
                Lang = CurrentLang
            });
        }

        [HttpGet]
        public ActionResult Create(int? hubId)
        {
            ActionResult guard = RequireCustomerAccount();
            if (guard != null) return guard;

            var model = new CreateOrderViewModel
            {
                DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                DistanceKm = 3.2m,
                RequestKey = Guid.NewGuid().ToString("N")
            };

            PopulateCreateViewModel(model, hubId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateOrderViewModel model, string submitAction)
        {
            ActionResult guard = RequireCustomerAccount();
            if (guard != null) return guard;

            if (model == null) model = new CreateOrderViewModel();
            if (string.IsNullOrWhiteSpace(model.RequestKey))
            {
                model.RequestKey = Guid.NewGuid().ToString("N");
            }

            Dictionary<int, int> postedQuantities = (model.Items ?? new List<MenuSelectionRowViewModel>())
                .Where(i => i != null && i.Quantity > 0)
                .GroupBy(i => i.MenuItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            PopulateCreateViewModel(model, model.HubId, postedQuantities);

            var orderRequest = new CreateOrderRequest
            {
                CustomerId = CurrentUser.Id,
                HubId = model.HubId,
                DeliveryAddress = model.DeliveryAddress,
                DistanceKm = model.DistanceKm,
                VoucherCode = model.VoucherCode,
                PaymentMethod = model.PaymentMethod,
                Notes = model.Notes,
                RequestKey = model.RequestKey,
                ExpectedPayableTotal = model.ExpectedPayableTotal,
                Items = postedQuantities
                    .Select(kv => new OrderItemRequest { MenuItemId = kv.Key, Quantity = kv.Value })
                    .ToList()
            };

            var service = new OrderService(Db);

            try
            {
                if (string.Equals(submitAction, "place", StringComparison.OrdinalIgnoreCase))
                {
                    Order created = service.PlaceOrder(orderRequest);
                    TempData["OrderSuccess"] = CurrentLang == "vi"
                        ? "Đặt đơn thành công: " + created.OrderCode
                        : "Order placed successfully: " + created.OrderCode;
                    return RedirectToAction("Details", new { id = created.Id });
                }

                model.Quote = service.Quote(orderRequest);
                model.ExpectedPayableTotal = model.Quote.Breakdown.PayableTotal;
            }
            catch (BusinessRuleException ex)
            {
                model.ErrorRule = ex.Rule;
                model.ErrorMessage = ex.Message;

                if (ex.Rule == "QUOTE_CHANGED")
                {
                    try
                    {
                        model.Quote = service.Quote(orderRequest);
                        model.ExpectedPayableTotal = model.Quote.Breakdown.PayableTotal;
                    }
                    catch (BusinessRuleException)
                    {
                    }
                }
            }

            return View(model);
        }

        [HttpGet]
        public ActionResult Details(int id)
        {
            if (CurrentUser == null)
            {
                TempData["AuthNotice"] = CurrentLang == "vi"
                    ? "Vui lòng chọn tài khoản demo để xem chi tiết đơn hàng."
                    : "Please sign in with a demo account to view order details.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var service = new OrderService(Db);
                Order order = service.GetOrderDetails(id, CurrentUser.Id);

                bool isOwnerCustomer = CurrentUser.Role == ActorRole.Customer && order.CustomerId == CurrentUser.Id;
                bool canEdit = isOwnerCustomer && order.Status == OrderStatus.Placed;
                bool canCancel = (order.Status == OrderStatus.Placed || order.Status == OrderStatus.Confirmed) &&
                    (isOwnerCustomer || (CurrentUser.Role == ActorRole.Admin && order.Status == OrderStatus.Confirmed));

                return View(new OrderDetailViewModel
                {
                    Order = order,
                    CurrentUser = CurrentUser,
                    CanEdit = canEdit,
                    CanCancel = canCancel,
                    Lang = CurrentLang,
                    NoticeMessage = TempData["OrderSuccess"] as string
                });
            }
            catch (BusinessRuleException)
            {
                TempData["AuthNotice"] = CurrentLang == "vi"
                    ? "Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập."
                    : "Order was not found or you do not have permission to view it.";
                return RedirectToAction("Index");
            }
        }

        private ActionResult RequireCustomerAccount()
        {
            if (CurrentUser != null && CurrentUser.Role == ActorRole.Customer && CurrentUser.CustomerType.HasValue)
            {
                return null;
            }

            TempData["AuthNotice"] = CurrentLang == "vi"
                ? "Vui lòng chuyển sang tài khoản Khách hàng (Local hoặc Tourist) trên thanh điều hướng để đặt đơn."
                : "Please switch to a Customer demo account (Local or Tourist) from the top bar to place an order.";
            return RedirectToAction("Index", "Home");
        }

        private void PopulateCreateViewModel(
            CreateOrderViewModel model,
            int? requestedHubId,
            Dictionary<int, int> preservedQuantities = null)
        {
            List<Hub> hubs = Db.Hubs
                .Where(h => !h.IsDeleted && h.IsActive)
                .OrderBy(h => h.Id)
                .ToList();

            int selectedHubId = hubs.Any(h => requestedHubId.HasValue && h.Id == requestedHubId.Value)
                ? requestedHubId.Value
                : (hubs.Any() ? hubs.First().Id : 0);

            List<int> brandIds = Db.HubBrands
                .Where(hb => hb.HubId == selectedHubId)
                .Select(hb => hb.BrandId)
                .ToList();

            List<MenuItem> menuItems = Db.MenuItems
                .Include(m => m.Brand)
                .Where(m => !m.IsDeleted && m.IsAvailable && m.Brand != null && !m.Brand.IsDeleted && m.Brand.IsActive && brandIds.Contains(m.BrandId))
                .OrderBy(m => m.BrandId)
                .ThenBy(m => m.Id)
                .ToList();

            CustomerType customerType = CurrentUser.CustomerType.Value;
            List<PaymentMethod> allowedMethods = customerType == CustomerType.Local
                ? new List<PaymentMethod> { PaymentMethod.MoMo, PaymentMethod.VNPay, PaymentMethod.Cod }
                : new List<PaymentMethod> { PaymentMethod.ApplePay, PaymentMethod.PayPal, PaymentMethod.Cod };

            if (!allowedMethods.Contains(model.PaymentMethod))
            {
                model.PaymentMethod = allowedMethods[0];
            }

            model.Lang = CurrentLang;
            model.CustomerType = customerType;
            model.HubId = selectedHubId;
            model.AvailableHubs = hubs;
            model.AvailablePaymentMethods = allowedMethods;
            model.Items = menuItems.Select(m => new MenuSelectionRowViewModel
            {
                MenuItemId = m.Id,
                ItemName = m.Name,
                BrandName = m.Brand != null ? m.Brand.Name : "SaigonBite",
                Category = m.Brand != null ? m.Brand.Category : "",
                Description = m.Description,
                Price = m.Price,
                ImageUrl = m.ImageUrl,
                Quantity = (preservedQuantities != null && preservedQuantities.ContainsKey(m.Id))
                    ? preservedQuantities[m.Id]
                    : 0
            }).ToList();
        }
    }
}
