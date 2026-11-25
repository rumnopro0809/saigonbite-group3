using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using SaigonBite.Application.Orders;
using SaigonBite.Application.Payments;
using SaigonBite.Domain;

namespace SaigonBite.Infrastructure.Services
{
    /// <summary>Thao tác của shipper trên đơn được gán (HT03, FR17, BR14).</summary>
    public enum ShipperAction { PickUp, Deliver, Fail }

    public sealed class ShipperInfo
    {
        public int UserId { get; set; }
        public string Identifier { get; set; }
        public string FullName { get; set; }
    }

    /// <summary>Một đơn Ready trên màn hình gán shipper của Admin (AMB01).</summary>
    public sealed class DispatchOrderCard
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public string HubName { get; set; }
        public string DeliveryAddress { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public decimal PayableTotal { get; set; }
        public int? ShipperId { get; set; }
        public string ShipperName { get; set; }
        public string RowVersion { get; set; }
    }

    public sealed class ShipperOrderCard
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public OrderStatus Status { get; set; }
        public string HubName { get; set; }
        public string DeliveryAddress { get; set; }
        public string Notes { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public decimal PayableTotal { get; set; }
        public string RowVersion { get; set; }
        public IList<KitchenOrderLine> Lines { get; set; }
    }

    /// <summary>
    /// Gán shipper cho đơn Ready (Admin, AMB01) và thao tác giao hàng của shipper (FR17, BR14-BR16).
    /// Chuyển trạng thái đi qua <see cref="OrderTransitionService"/> nên luật COD, lý do thất bại và lịch sử
    /// chỉ viết một chỗ; lớp này xác định người thao tác và ánh xạ thao tác sang trạng thái đích.
    /// </summary>
    public sealed class ShipperService
    {
        private static readonly OrderStatus[] ShipperStatuses = { OrderStatus.Ready, OrderStatus.OutForDelivery };

        private readonly AppDbContext _db;
        private readonly PaymentSelector _payments;

        public ShipperService(AppDbContext db, PaymentSelector paymentSelector = null)
        {
            if (db == null) throw new ArgumentNullException("db");
            _db = db;
            _payments = paymentSelector;
        }

        /// <summary>Shipper đang hoạt động, để Admin chọn khi gán.</summary>
        public IList<ShipperInfo> ListShippers()
        {
            return _db.Users.AsNoTracking()
                .Where(u => u.Role == ActorRole.Shipper && u.IsActive && !u.IsDeleted)
                .OrderBy(u => u.Identifier)
                .Select(u => new ShipperInfo { UserId = u.Id, Identifier = u.Identifier, FullName = u.FullName })
                .ToList();
        }

        /// <summary>Các đơn Ready (chưa lấy hàng), cũ nhất trước. Chỉ Admin được xem.</summary>
        public IList<DispatchOrderCard> ReadyOrders(int adminUserId)
        {
            RequireAdmin(adminUserId);
            return _db.Orders.AsNoTracking()
                .Where(o => o.Status == OrderStatus.Ready)
                .OrderBy(o => o.PlacedAt).ThenBy(o => o.Id)
                .Select(o => new
                {
                    o.Id, o.OrderCode, HubName = o.Hub.Name, o.DeliveryAddress, o.PaymentMethod, o.PayableTotal,
                    o.ShipperId, ShipperName = o.Shipper == null ? null : o.Shipper.FullName, o.RowVersion
                })
                .ToList()
                .Select(o => new DispatchOrderCard
                {
                    OrderId = o.Id, OrderCode = o.OrderCode, HubName = o.HubName, DeliveryAddress = o.DeliveryAddress,
                    PaymentMethod = o.PaymentMethod, PayableTotal = o.PayableTotal, ShipperId = o.ShipperId,
                    ShipperName = o.ShipperName,
                    RowVersion = o.RowVersion == null ? null : Convert.ToBase64String(o.RowVersion)
                }).ToList();
        }

        /// <summary>
        /// Admin gán (hoặc đổi) shipper cho đơn Ready. Lỗi: UNAUTHORIZED (không phải Admin), NOT_FOUND (đơn),
        /// BR15 (đơn không ở Ready), SHIPPER_NOT_FOUND (không phải shipper đang hoạt động), CONCURRENCY.
        /// </summary>
        public Order AssignShipper(int adminUserId, int orderId, int shipperId, byte[] expectedRowVersion)
        {
            RequireAdmin(adminUserId);
            Order order = _db.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) throw new BusinessRuleException("NOT_FOUND", "Order not found.");
            if (expectedRowVersion != null &&
                (order.RowVersion == null || !order.RowVersion.SequenceEqual(expectedRowVersion)))
                throw new BusinessRuleException("CONCURRENCY", "Order was modified concurrently. Please refresh and try again.");
            if (order.Status != OrderStatus.Ready)
                throw new BusinessRuleException("BR15", "Only a Ready order can be assigned to a shipper.");
            if (!_db.Users.Any(u => u.Id == shipperId && u.Role == ActorRole.Shipper && u.IsActive && !u.IsDeleted))
                throw new BusinessRuleException("SHIPPER_NOT_FOUND", "The shipper does not exist or is inactive.");

            order.ShipperId = shipperId;
            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new BusinessRuleException("CONCURRENCY", "Order was modified concurrently. Please refresh and try again.");
            }
            return order;
        }

        /// <summary>Đơn Ready hoặc OutForDelivery được gán cho shipper này.</summary>
        public IList<ShipperOrderCard> GetMyOrders(int shipperUserId)
        {
            RequireShipper(shipperUserId);
            var orders = _db.Orders.AsNoTracking().Include(o => o.Lines).Include(o => o.Hub)
                .Where(o => o.ShipperId == shipperUserId && ShipperStatuses.Contains(o.Status))
                .OrderBy(o => o.PlacedAt).ThenBy(o => o.Id)
                .ToList();
            return orders.Select(o => new ShipperOrderCard
            {
                OrderId = o.Id,
                OrderCode = o.OrderCode,
                Status = o.Status,
                HubName = o.Hub.Name,
                DeliveryAddress = o.DeliveryAddress,
                Notes = o.Notes,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                PayableTotal = o.PayableTotal,
                RowVersion = o.RowVersion == null ? null : Convert.ToBase64String(o.RowVersion),
                Lines = o.Lines.OrderBy(l => l.Id)
                    .Select(l => new KitchenOrderLine { Name = l.ItemNameSnapshot, Quantity = l.Quantity }).ToList()
            }).ToList();
        }

        /// <summary>
        /// PickUp: Ready→OutForDelivery. Deliver: OutForDelivery→Delivered (COD cần cashCollected, BR14).
        /// Fail: OutForDelivery→Failed, bắt buộc lý do (BR15). Đơn không gán cho shipper này bị BR15.
        /// </summary>
        public Order Act(int shipperUserId, int orderId, ShipperAction action, bool cashCollected, string reason,
            byte[] expectedRowVersion)
        {
            User shipper = RequireShipper(shipperUserId);
            if (action == ShipperAction.Fail && string.IsNullOrWhiteSpace(reason))
                throw new BusinessRuleException("BR15", "A failed delivery needs a reason.");

            return new OrderTransitionService(_db, _payments).Transition(new TransitionOrderRequest
            {
                OrderId = orderId,
                ActorId = shipper.Id,
                TargetStatus = TargetOf(action),
                Reason = action == ShipperAction.Fail ? reason.Trim() : null,
                CashCollected = action == ShipperAction.Deliver && cashCollected,
                CustomerUnreachable = action == ShipperAction.Fail,
                ExpectedRowVersion = expectedRowVersion
            });
        }

        private static OrderStatus TargetOf(ShipperAction action)
        {
            switch (action)
            {
                case ShipperAction.PickUp: return OrderStatus.OutForDelivery;
                case ShipperAction.Deliver: return OrderStatus.Delivered;
                case ShipperAction.Fail: return OrderStatus.Failed;
                default: throw new BusinessRuleException("BR15", "Unsupported shipper action.");
            }
        }

        private User RequireShipper(int userId)
        {
            User user = _db.Users.FirstOrDefault(u => u.Id == userId && u.Role == ActorRole.Shipper && u.IsActive && !u.IsDeleted);
            if (user == null) throw new BusinessRuleException("UNAUTHORIZED", "Shipper account not found or inactive.");
            return user;
        }

        private void RequireAdmin(int userId)
        {
            if (!_db.Users.Any(u => u.Id == userId && u.Role == ActorRole.Admin && u.IsActive && !u.IsDeleted))
                throw new BusinessRuleException("UNAUTHORIZED", "Admin account not found or inactive.");
        }
    }
}
