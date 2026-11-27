using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using SaigonBite.Application.Orders;
using SaigonBite.Application.Payments;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;

namespace SaigonBite.Infrastructure.Services
{
    /// <summary>
    /// Handles UC01 order quoting, atomic placement (D01/D05), Placed-only editing (BR17), and tracking queries.
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

            // BR8: hub load counts only orders currently in Confirmed or Preparing.
            int activeOrders = _db.Orders.Count(o =>
                o.HubId == hubId &&
                (o.Status == OrderStatus.Confirmed ||
                 o.Status == OrderStatus.Preparing));

            return (decimal)activeOrders / maxCapacity;
        }

        public OrderQuoteResult Quote(CreateOrderRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
                throw new BusinessRuleException("VALIDATION", "Delivery address is required.");

            User customer = LoadCustomerAccount(request.CustomerId);

            Hub hub = _db.Hubs.FirstOrDefault(h => h.Id == request.HubId && !h.IsDeleted && h.IsActive);
            if (hub == null)
                throw new BusinessRuleException("NOT_FOUND", "Selected kitchen hub is unavailable.");

            int[] quantities;
            decimal subtotal;
            List<ResolvedOrderLineDto> resolvedItems = ResolveItemsForHub(
                hub.Id,
                request.Items,
                null,
                out quantities,
                out subtotal);

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

                voucherInput = ToVoucherInput(voucher);
            }

            PriceBreakdown breakdown;
            try
            {
                breakdown = _pricingService.Calculate(new PricingInput
                {
                    Subtotal = subtotal,
                    DistanceKm = request.DistanceKm,
                    HubLoad = hubLoad,
                    Quantities = quantities,
                    PlacedAt = placedAt,
                    HeavyRain = heavyRain,
                    AllItemsAvailableAtHub = true,
                    Voucher = voucherInput
                });
            }
            catch (BusinessRuleException ex)
            {
                if (ex.Rule == "BR8")
                {
                    List<string> alternatives = FindAlternativeHubNames(hub.Id);
                    string suggestion = alternatives.Count > 0
                        ? " Please choose another hub: " + string.Join(", ", alternatives) + "."
                        : " Please choose another available kitchen hub.";

                    throw new BusinessRuleException(
                        "BR8",
                        hub.Name + " is overloaded (" + Math.Round(hubLoad * 100m, 0) + "% capacity)." + suggestion);
                }

                throw;
            }

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

        public EditOrderQuoteResult QuoteEdit(EditOrderRequest request)
        {
            Order order;
            User customer;
            return EvaluateEdit(request, out order, out customer);
        }

        public Order EditOrder(EditOrderRequest request)
        {
            Order order;
            User customer;
            EditOrderQuoteResult quote = EvaluateEdit(request, out order, out customer);

            if (request.ExpectedRowVersion != null &&
                (order.RowVersion == null || !order.RowVersion.SequenceEqual(request.ExpectedRowVersion)))
            {
                throw new BusinessRuleException(
                    "CONCURRENCY",
                    "Order was modified or confirmed while you were editing. Please refresh.");
            }

            PaymentResult adjustmentResult = null;
            string adjustmentOperation = null;

            // Prepare simulated online adjustment (AMB05) before entering DB transaction.
            if (quote.HasOnlineAdjustment)
            {
                IPaymentStrategy strategy = _paymentSelector.SelectForRefund(order.PaymentMethod);
                if (quote.Difference > 0m)
                {
                    adjustmentOperation = "EditAdditionalCharge";
                    adjustmentResult = strategy.Charge(new PaymentRequest
                    {
                        OrderCode = order.OrderCode,
                        Amount = quote.Difference,
                        CustomerType = customer.CustomerType.Value,
                        Reason = "Order edit additional charge"
                    });
                }
                else if (quote.Difference < 0m)
                {
                    adjustmentOperation = "EditPartialRefund";
                    adjustmentResult = strategy.Refund(new PaymentRequest
                    {
                        OrderCode = order.OrderCode,
                        Amount = Math.Abs(quote.Difference),
                        CustomerType = customer.CustomerType.Value,
                        Reason = "Order edit partial refund"
                    });
                }

                if (adjustmentResult != null && !adjustmentResult.Success)
                {
                    throw new BusinessRuleException(
                        "PAYMENT",
                        adjustmentResult.Message ?? "Simulated payment adjustment failed.");
                }
            }

            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    _db.OrderLines.RemoveRange(order.Lines.ToList());

                    foreach (ResolvedOrderLineDto line in quote.ResolvedItems)
                    {
                        _db.OrderLines.Add(new OrderLine
                        {
                            OrderId = order.Id,
                            MenuItemId = line.MenuItemId,
                            Quantity = line.Quantity,
                            UnitPriceSnapshot = line.UnitPrice,
                            ItemNameSnapshot = line.ItemName,
                            CategorySnapshot = line.Category
                        });
                    }

                    if (!request.KeepVoucher)
                    {
                        order.VoucherId = null;
                    }

                    if (request.Notes != null)
                    {
                        order.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
                    }

                    // BR17: update breakdown while keeping original PlacedAt, HubLoadSnapshot, HeavyRainSnapshot, and Placed status.
                    order.Subtotal = quote.Breakdown.Subtotal;
                    order.QuietDiscount = quote.Breakdown.QuietDiscount;
                    order.VoucherDiscount = quote.Breakdown.VoucherDiscount;
                    order.FoodTotal = quote.Breakdown.FoodTotal;
                    order.DeliveryFee = quote.Breakdown.DeliveryFee;
                    order.RushFee = quote.Breakdown.RushFee;
                    order.RainFee = quote.Breakdown.RainFee;
                    order.RawTotal = quote.Breakdown.RawTotal;
                    order.PayableTotal = quote.Breakdown.PayableTotal;

                    if (adjustmentResult != null)
                    {
                        _db.PaymentRecords.Add(new PaymentRecord
                        {
                            OrderId = order.Id,
                            Method = order.PaymentMethod,
                            Operation = adjustmentOperation,
                            Amount = adjustmentResult.Amount,
                            Result = adjustmentResult.Status,
                            CorrelationKey = adjustmentResult.CorrelationKey,
                            CreatedAt = adjustmentResult.ProcessedAt
                        });
                    }

                    _db.SaveChanges();
                    tx.Commit();
                    return BuildOrderDetailsQuery().First(o => o.Id == order.Id);
                }
                catch (DbUpdateConcurrencyException)
                {
                    tx.Rollback();
                    throw new BusinessRuleException(
                        "CONCURRENCY",
                        "Order was modified concurrently. Please reload the latest state.");
                }
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

        private EditOrderQuoteResult EvaluateEdit(EditOrderRequest request, out Order order, out User customer)
        {
            if (request == null) throw new ArgumentNullException("request");

            customer = LoadCustomerAccount(request.CustomerId);
            order = BuildOrderDetailsQuery().FirstOrDefault(o => o.Id == request.OrderId);
            if (order == null || order.CustomerId != customer.Id)
                throw new BusinessRuleException("UNAUTHORIZED", "Order was not found or access is denied.");

            if (order.Status != OrderStatus.Placed)
                throw new BusinessRuleException("BR17", "Only orders in Placed status can be edited.");

            Dictionary<int, decimal> existingUnitPrices = order.Lines
                .ToDictionary(l => l.MenuItemId, l => l.UnitPriceSnapshot);

            int[] quantities;
            decimal subtotal;
            List<ResolvedOrderLineDto> resolvedItems = ResolveItemsForHub(
                order.HubId,
                request.Items,
                existingUnitPrices,
                out quantities,
                out subtotal);

            VoucherInput voucherInput = (request.KeepVoucher && order.Voucher != null)
                ? ToVoucherInput(order.Voucher)
                : null;

            // BR17: recalculate strictly with the frozen PlacedAt, HubLoadSnapshot, HeavyRainSnapshot, and DistanceKm.
            PriceBreakdown breakdown = _pricingService.Calculate(new PricingInput
            {
                Subtotal = subtotal,
                DistanceKm = order.DistanceKm,
                HubLoad = order.HubLoadSnapshot,
                Quantities = quantities,
                PlacedAt = order.PlacedAt,
                HeavyRain = order.HeavyRainSnapshot,
                AllItemsAvailableAtHub = true,
                Voucher = voucherInput
            });

            PricingService.ValidatePayment(customer.CustomerType.Value, order.PaymentMethod, breakdown.PayableTotal);

            decimal difference = breakdown.PayableTotal - order.PayableTotal;
            IPaymentStrategy strategy = _paymentSelector.SelectForRefund(order.PaymentMethod);
            bool isOnlinePaid = strategy.IsOnline &&
                string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase);
            bool hasAdjustment = isOnlinePaid && difference != 0m;
            string adjustmentType = !hasAdjustment
                ? "None"
                : (difference > 0m ? "AdditionalCharge" : "PartialRefund");

            return new EditOrderQuoteResult
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                OriginalPayableTotal = order.PayableTotal,
                NewPayableTotal = breakdown.PayableTotal,
                Difference = difference,
                OriginalPlacedAt = order.PlacedAt,
                OriginalHubLoad = order.HubLoadSnapshot,
                OriginalHeavyRain = order.HeavyRainSnapshot,
                HasOnlineAdjustment = hasAdjustment,
                AdjustmentType = adjustmentType,
                Breakdown = breakdown,
                ResolvedItems = resolvedItems
            };
        }

        private List<ResolvedOrderLineDto> ResolveItemsForHub(
            int hubId,
            List<OrderItemRequest> items,
            Dictionary<int, decimal> existingUnitPrices,
            out int[] quantities,
            out decimal subtotal)
        {
            if (items == null || items.Count == 0)
                throw new BusinessRuleException("BR12", "Use 1-30 order lines.");

            List<OrderItemRequest> normalizedItems = NormalizeItems(items);
            List<int> menuItemIds = normalizedItems.Select(i => i.MenuItemId).Distinct().ToList();

            Dictionary<int, MenuItem> menuLookup = _db.MenuItems
                .Include(m => m.Brand)
                .Where(m => menuItemIds.Contains(m.Id))
                .ToDictionary(m => m.Id);

            HashSet<int> hubBrandIds = new HashSet<int>(_db.HubBrands
                .Where(hb => hb.HubId == hubId)
                .Select(hb => hb.BrandId)
                .ToList());

            var resolvedItems = new List<ResolvedOrderLineDto>();
            var qtyList = new List<int>();
            subtotal = 0m;
            bool allAvailableAtHub = true;

            foreach (OrderItemRequest line in normalizedItems)
            {
                qtyList.Add(line.Quantity);

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

                // AMB03: preserve frozen price for lines already on the order; use DB price for newly added items.
                decimal unitPrice = (existingUnitPrices != null && existingUnitPrices.ContainsKey(item.Id))
                    ? existingUnitPrices[item.Id]
                    : item.Price;

                decimal lineTotal = unitPrice * line.Quantity;
                subtotal += lineTotal;
                resolvedItems.Add(new ResolvedOrderLineDto
                {
                    MenuItemId = item.Id,
                    ItemName = item.Name,
                    Category = item.Brand.Category,
                    UnitPrice = unitPrice,
                    Quantity = line.Quantity,
                    LineTotal = lineTotal
                });
            }

            if (!allAvailableAtHub)
                throw new BusinessRuleException("BR12", "Item is unavailable or not served by this hub.");

            quantities = qtyList.ToArray();
            return resolvedItems;
        }

        private User LoadCustomerAccount(int customerId)
        {
            User customer = _db.Users.FirstOrDefault(u => u.Id == customerId && !u.IsDeleted && u.IsActive);
            if (customer == null || customer.Role != ActorRole.Customer || !customer.CustomerType.HasValue)
                throw new BusinessRuleException("UNAUTHORIZED", "An active customer account is required.");
            return customer;
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

        private static VoucherInput ToVoucherInput(Voucher voucher)
        {
            return new VoucherInput
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

        private List<string> FindAlternativeHubNames(int excludedHubId)
        {
            List<Hub> candidates = _db.Hubs
                .Where(h => h.Id != excludedHubId && !h.IsDeleted && h.IsActive && h.MaxCapacity > 0)
                .OrderBy(h => h.Id)
                .ToList();

            var available = new List<string>();
            foreach (Hub candidate in candidates)
            {
                if (CalculateHubLoad(candidate.Id, candidate.MaxCapacity) < 0.9m)
                {
                    available.Add(candidate.Name);
                }
            }

            return available;
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
