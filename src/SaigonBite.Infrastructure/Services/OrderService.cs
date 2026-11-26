using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SaigonBite.Application.Orders;
using SaigonBite.Application.Payments;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;

namespace SaigonBite.Infrastructure.Services
{
    /// <summary>
    /// Handles UC01 order quoting, atomic placement (D01/D05), and role-scoped tracking queries.
    /// </summary>
    public sealed class OrderService : IOrderService
    {
        private readonly SaigonBiteDbContext _db;
        private readonly PricingService _pricingService;
        private readonly PaymentSelector _paymentSelector;

        public OrderService(
            SaigonBiteDbContext db,
            PricingService pricingService = null,
            PaymentSelector paymentSelector = null)
        {
            if (db == null) throw new ArgumentNullException("db");
            _db = db;
            _pricingService = pricingService ?? new PricingService();
            _paymentSelector = paymentSelector ?? PaymentSelector.CreateDefault();
        }

        public decimal CalculateHubLoad(int hubId, int maxCapacity)
        {
            if (maxCapacity <= 0) return 1m;

            int activeOrders = _db.Orders.Count(o =>
                o.HubId == hubId &&
                (o.Status == OrderStatus.Placed ||
                 o.Status == OrderStatus.Confirmed ||
                 o.Status == OrderStatus.Preparing ||
                 o.Status == OrderStatus.Ready));

            return (decimal)activeOrders / maxCapacity;
        }

        public OrderQuoteResult Quote(CreateOrderRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
                throw new BusinessRuleException("VALIDATION", "Delivery address is required.");

            User customer = _db.Users.FirstOrDefault(u => u.Id == request.CustomerId && !u.IsDeleted && u.IsActive);
            if (customer == null || customer.Role != ActorRole.Customer || !customer.CustomerType.HasValue)
                throw new BusinessRuleException("UNAUTHORIZED", "An active customer account is required.");

            Hub hub = _db.Hubs.FirstOrDefault(h => h.Id == request.HubId && !h.IsDeleted && h.IsActive);
            if (hub == null)
                throw new BusinessRuleException("NOT_FOUND", "Selected kitchen hub is unavailable.");

            if (request.Items == null || request.Items.Count == 0)
                throw new BusinessRuleException("BR12", "Use 1-30 order lines.");

            // Combine duplicate MenuItemId selections while preserving non-positive inputs for BR12 checks.
            List<OrderItemRequest> normalizedItems = NormalizeItems(request.Items);
            List<int> menuItemIds = normalizedItems.Select(i => i.MenuItemId).Distinct().ToList();

            Dictionary<int, MenuItem> menuLookup = _db.MenuItems
                .Include(m => m.Brand)
                .Where(m => menuItemIds.Contains(m.Id))
                .ToDictionary(m => m.Id);

            HashSet<int> hubBrandIds = new HashSet<int>(_db.HubBrands
                .Where(hb => hb.HubId == hub.Id)
                .Select(hb => hb.BrandId)
                .ToList());

            var resolvedItems = new List<ResolvedOrderLineDto>();
            var quantities = new List<int>();
            decimal subtotal = 0m;
            bool allAvailableAtHub = true;

            foreach (OrderItemRequest line in normalizedItems)
            {
                quantities.Add(line.Quantity);

                MenuItem item;
                if (!menuLookup.TryGetValue(line.MenuItemId, out item) ||
                    item.IsDeleted ||
                    !item.IsAvailable ||
                    item.Brand == null ||
                    item.Brand.IsDeleted ||
                    !item.Brand.IsActive ||
                    !hubBrandIds.Contains(item.BrandId))
                {
                    allAvailableAtHub = false;
                    continue;
                }

                decimal lineTotal = item.Price * line.Quantity;
                subtotal += lineTotal;
                resolvedItems.Add(new ResolvedOrderLineDto
                {
                    MenuItemId = item.Id,
                    ItemName = item.Name,
                    Category = item.Brand.Category,
                    UnitPrice = item.Price,
                    Quantity = line.Quantity,
                    LineTotal = lineTotal
                });
            }

            if (!allAvailableAtHub)
                throw new BusinessRuleException("BR12", "Item is unavailable or not served by this hub.");

            decimal hubLoad = CalculateHubLoad(hub.Id, hub.MaxCapacity);
            SystemSetting setting = _db.SystemSettings.FirstOrDefault();
            bool heavyRain = setting != null && setting.IsHeavyRainOn;
            DateTimeOffset placedAt = request.PlacedAtOverride ?? DateTimeOffset.UtcNow;

            Voucher voucher = null;
            VoucherInput voucherInput = null;
            if (!string.IsNullOrWhiteSpace(request.VoucherCode))
            {
                string codeUpper = request.VoucherCode.Trim().ToUpperInvariant();
                voucher = _db.Vouchers.FirstOrDefault(v => !v.IsDeleted && v.Code.ToUpper() == codeUpper);
                if (voucher == null)
                    throw new BusinessRuleException("BR9", "Voucher code not found.");

                voucherInput = new VoucherInput
                {
                    Type = voucher.Type,
                    Value = voucher.Value,
                    Cap = voucher.Cap,
                    MinimumSubtotal = voucher.MinimumSubtotal,
                    Active = voucher.IsActive,
                    ValidFrom = voucher.ValidFrom,
                    ValidTo = voucher.ValidTo
                };
            }

            PriceBreakdown breakdown = _pricingService.Calculate(new PricingInput
            {
                Subtotal = subtotal,
                DistanceKm = request.DistanceKm,
                HubLoad = hubLoad,
                Quantities = quantities.ToArray(),
                PlacedAt = placedAt,
                HeavyRain = heavyRain,
                AllItemsAvailableAtHub = true,
                Voucher = voucherInput
            });

            return new OrderQuoteResult
            {
                HubId = hub.Id,
                HubName = hub.Name,
                HubLoad = hubLoad,
                IsHeavyRainOn = heavyRain,
                VoucherId = voucher != null ? (int?)voucher.Id : null,
                VoucherCode = voucher != null ? voucher.Code : null,
                CustomerType = customer.CustomerType.Value,
                Breakdown = breakdown,
                ResolvedItems = resolvedItems
            };
        }

        public Order PlaceOrder(CreateOrderRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");

            string normalizedKey = string.IsNullOrWhiteSpace(request.RequestKey)
                ? null
                : request.RequestKey.Trim();

            // D05 idempotency check: return existing order if RequestKey was already persisted.
            if (normalizedKey != null)
            {
                Order existing = BuildOrderDetailsQuery()
                    .FirstOrDefault(o => o.CustomerId == request.CustomerId && o.RequestKey == normalizedKey);
                if (existing != null)
                {
                    return existing;
                }
            }

            OrderQuoteResult quote = Quote(request);

            // D01 quote-change check: require fresh confirmation if total drifted before placement.
            if (request.ExpectedPayableTotal.HasValue &&
                request.ExpectedPayableTotal.Value != quote.Breakdown.PayableTotal)
            {
                throw new BusinessRuleException(
                    "QUOTE_CHANGED",
                    "Order total changed from " +
                    request.ExpectedPayableTotal.Value.ToString("N0") +
                    " to " +
                    quote.Breakdown.PayableTotal.ToString("N0") +
                    " VND. Please review and confirm again.");
            }

            IPaymentStrategy strategy = _paymentSelector.Select(
                quote.CustomerType,
                request.PaymentMethod,
                quote.Breakdown.PayableTotal);

            DateTimeOffset placedAt = request.PlacedAtOverride ?? DateTimeOffset.UtcNow;
            string orderCode = "SB-" +
                               placedAt.ToOffset(TimeSpan.FromHours(7)).ToString("yyyyMMdd") +
                               "-" +
                               Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();

            PaymentResult paymentResult = strategy.Charge(new PaymentRequest
            {
                OrderCode = orderCode,
                Amount = quote.Breakdown.PayableTotal,
                CustomerType = quote.CustomerType,
                CorrelationKey = normalizedKey
            });

            if (!paymentResult.Success)
                throw new BusinessRuleException("PAYMENT", paymentResult.Message ?? "Simulated payment failed.");

            using (var tx = _db.Database.BeginTransaction())
            {
                var order = new Order
                {
                    OrderCode = orderCode,
                    CustomerId = request.CustomerId,
                    HubId = quote.HubId,
                    VoucherId = quote.VoucherId,
                    Status = OrderStatus.Placed,
                    RequestKey = normalizedKey,
                    DeliveryAddress = request.DeliveryAddress.Trim(),
                    DistanceKm = request.DistanceKm,
                    PlacedAt = placedAt,
                    HubLoadSnapshot = quote.HubLoad,
                    HeavyRainSnapshot = quote.IsHeavyRainOn,
                    Subtotal = quote.Breakdown.Subtotal,
                    QuietDiscount = quote.Breakdown.QuietDiscount,
                    VoucherDiscount = quote.Breakdown.VoucherDiscount,
                    FoodTotal = quote.Breakdown.FoodTotal,
                    DeliveryFee = quote.Breakdown.DeliveryFee,
                    RushFee = quote.Breakdown.RushFee,
                    RainFee = quote.Breakdown.RainFee,
                    RawTotal = quote.Breakdown.RawTotal,
                    PayableTotal = quote.Breakdown.PayableTotal,
                    PaymentMethod = request.PaymentMethod,
                    PaymentStatus = paymentResult.Status,
                    Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
                };

                foreach (ResolvedOrderLineDto line in quote.ResolvedItems)
                {
                    order.Lines.Add(new OrderLine
                    {
                        MenuItemId = line.MenuItemId,
                        Quantity = line.Quantity,
                        UnitPriceSnapshot = line.UnitPrice,
                        ItemNameSnapshot = line.ItemName,
                        CategorySnapshot = line.Category
                    });
                }

                order.Payments.Add(new PaymentRecord
                {
                    Method = request.PaymentMethod,
                    Operation = "Charge",
                    Amount = paymentResult.Amount,
                    Result = paymentResult.Status,
                    CorrelationKey = paymentResult.CorrelationKey,
                    CreatedAt = paymentResult.ProcessedAt
                });

                order.StatusHistory.Add(new OrderStatusHistory
                {
                    ActorId = request.CustomerId,
                    FromStatus = null,
                    ToStatus = OrderStatus.Placed,
                    Reason = "Order placed",
                    CashCollected = false,
                    ChangedAt = placedAt
                });

                _db.Orders.Add(order);
                _db.SaveChanges();
                tx.Commit();

                return BuildOrderDetailsQuery().First(o => o.Id == order.Id);
            }
        }

        public List<Order> GetOrdersForActor(int actorId)
        {
            User actor = LoadActiveUser(actorId);

            IQueryable<Order> query = _db.Orders
                .Include(o => o.Hub)
                .Include(o => o.Customer)
                .Include(o => o.Lines);

            if (actor.Role == ActorRole.Customer)
            {
                query = query.Where(o => o.CustomerId == actor.Id);
            }
            else if (actor.Role == ActorRole.KitchenStaff)
            {
                int hubId = actor.StaffProfile != null ? actor.StaffProfile.HubId : -1;
                query = query.Where(o => o.HubId == hubId);
            }
            else if (actor.Role == ActorRole.Shipper)
            {
                query = query.Where(o => o.ShipperId == actor.Id);
            }
            else if (actor.Role != ActorRole.Admin)
            {
                return new List<Order>();
            }

            return query.OrderByDescending(o => o.PlacedAt).ToList();
        }

        public Order GetOrderDetails(int orderId, int actorId)
        {
            User actor = LoadActiveUser(actorId);
            Order order = BuildOrderDetailsQuery().FirstOrDefault(o => o.Id == orderId);

            if (order == null || !CanActorAccessOrder(actor, order))
                throw new BusinessRuleException("UNAUTHORIZED", "Order was not found or access is denied.");

            return order;
        }

        private User LoadActiveUser(int actorId)
        {
            User actor = _db.Users
                .Include(u => u.StaffProfile)
                .FirstOrDefault(u => u.Id == actorId && !u.IsDeleted && u.IsActive);

            if (actor == null)
                throw new BusinessRuleException("UNAUTHORIZED", "Signed-in account not found or inactive.");

            return actor;
        }

        private IQueryable<Order> BuildOrderDetailsQuery()
        {
            return _db.Orders
                .Include(o => o.Hub)
                .Include(o => o.Voucher)
                .Include(o => o.Customer)
                .Include(o => o.Shipper)
                .Include(o => o.Lines)
                .Include(o => o.Payments)
                .Include(o => o.StatusHistory.Select(h => h.Actor));
        }

        private static bool CanActorAccessOrder(User actor, Order order)
        {
            if (actor.Role == ActorRole.Admin) return true;
            if (actor.Role == ActorRole.Customer) return order.CustomerId == actor.Id;
            if (actor.Role == ActorRole.KitchenStaff)
                return actor.StaffProfile != null && order.HubId == actor.StaffProfile.HubId;
            if (actor.Role == ActorRole.Shipper)
                return order.ShipperId.HasValue && order.ShipperId.Value == actor.Id;
            return false;
        }

        private static List<OrderItemRequest> NormalizeItems(List<OrderItemRequest> items)
        {
            var grouped = new Dictionary<int, int>();
            var invalidLines = new List<OrderItemRequest>();

            foreach (OrderItemRequest item in items)
            {
                if (item == null) continue;
                if (item.Quantity <= 0)
                {
                    invalidLines.Add(item);
                    continue;
                }

                if (grouped.ContainsKey(item.MenuItemId))
                {
                    grouped[item.MenuItemId] += item.Quantity;
                }
                else
                {
                    grouped[item.MenuItemId] = item.Quantity;
                }
            }

            var result = grouped
                .Select(kv => new OrderItemRequest { MenuItemId = kv.Key, Quantity = kv.Value })
                .ToList();
            result.AddRange(invalidLines);
            return result;
        }
    }
}
