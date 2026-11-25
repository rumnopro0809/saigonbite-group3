using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SaigonBite.Application.Orders;
using SaigonBite.Application.Payments;
using SaigonBite.Domain;

namespace SaigonBite.Infrastructure.Services
{
    /// <summary>Thao tác của nhân viên bếp trên một đơn (HT03, FR17, BR15).</summary>
    public enum KitchenAction { Confirm, StartPreparing, MarkReady, Reject }

    public sealed class KitchenOrderLine
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
    }

    public sealed class KitchenOrderCard
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public OrderStatus Status { get; set; }
        public DateTimeOffset PlacedAt { get; set; }
        public decimal PayableTotal { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public string DeliveryAddress { get; set; }
        public string Notes { get; set; }

        /// <summary>Phiên bản dữ liệu (base64); gửi lại khi thao tác để không ghi đè kết quả của người khác.</summary>
        public string RowVersion { get; set; }
        public IList<KitchenOrderLine> Lines { get; set; }
    }

    public sealed class KitchenBoard
    {
        public int HubId { get; set; }
        public string HubName { get; set; }
        public int Capacity { get; set; }

        /// <summary>Số đơn Confirmed + Preparing của bếp (BR8).</summary>
        public int ActiveOrders { get; set; }
        public decimal Load { get; set; }
        public int LoadPercent { get; set; }
        public IList<KitchenOrderCard> Orders { get; set; }
    }

    /// <summary>
    /// Bảng đơn của nhân viên bếp: chỉ thấy và chỉ thao tác được đơn của bếp mình.
    /// Mọi chuyển trạng thái đi qua <see cref="OrderTransitionService"/> nên luật BR15, lịch sử và hoàn tiền
    /// chỉ viết một chỗ; lớp này chỉ xác định nhân viên và ánh xạ thao tác sang trạng thái đích.
    /// </summary>
    public sealed class KitchenBoardService
    {
        private static readonly OrderStatus[] BoardStatuses =
            { OrderStatus.Placed, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Ready };

        private readonly AppDbContext _db;
        private readonly PaymentSelector _payments;

        public KitchenBoardService(AppDbContext db, PaymentSelector paymentSelector = null)
        {
            if (db == null) throw new ArgumentNullException("db");
            _db = db;
            _payments = paymentSelector;
        }

        public KitchenBoard GetBoard(int staffUserId)
        {
            User staff = RequireKitchenStaff(staffUserId);
            int hubId = staff.StaffProfile.HubId;
            Hub hub = _db.Hubs.AsNoTracking().Single(h => h.Id == hubId);

            var orders = _db.Orders.AsNoTracking().Include(o => o.Lines)
                .Where(o => o.HubId == hubId && BoardStatuses.Contains(o.Status))
                .OrderBy(o => o.PlacedAt).ThenBy(o => o.Id)
                .ToList();

            int active = orders.Count(o => HubLoad.CountsTowardLoad(o.Status));
            decimal load = HubLoad.Calculate(active, hub.Capacity);
            return new KitchenBoard
            {
                HubId = hub.Id,
                HubName = hub.Name,
                Capacity = hub.Capacity,
                ActiveOrders = active,
                Load = load,
                LoadPercent = (int)Math.Round(load * 100m, 0, MidpointRounding.AwayFromZero),
                Orders = orders.Select(o => new KitchenOrderCard
                {
                    OrderId = o.Id,
                    OrderCode = o.OrderCode,
                    Status = o.Status,
                    PlacedAt = o.PlacedAt,
                    PayableTotal = o.PayableTotal,
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    DeliveryAddress = o.DeliveryAddress,
                    Notes = o.Notes,
                    RowVersion = o.RowVersion == null ? null : Convert.ToBase64String(o.RowVersion),
                    Lines = o.Lines.OrderBy(l => l.Id)
                        .Select(l => new KitchenOrderLine { Name = l.ItemNameSnapshot, Quantity = l.Quantity }).ToList()
                }).ToList()
            };
        }

        public Order Act(int staffUserId, int orderId, KitchenAction action, string reason, byte[] expectedRowVersion)
        {
            User staff = RequireKitchenStaff(staffUserId);
            return new OrderTransitionService(_db, _payments).Transition(new TransitionOrderRequest
            {
                OrderId = orderId,
                ActorId = staff.Id,
                TargetStatus = TargetOf(action),
                // Chỉ từ chối mới cần lý do (BR16); các thao tác khác bỏ qua.
                Reason = action == KitchenAction.Reject ? reason : null,
                ExpectedRowVersion = expectedRowVersion
            });
        }

        private static OrderStatus TargetOf(KitchenAction action)
        {
            switch (action)
            {
                case KitchenAction.Confirm: return OrderStatus.Confirmed;
                case KitchenAction.StartPreparing: return OrderStatus.Preparing;
                case KitchenAction.MarkReady: return OrderStatus.Ready;
                case KitchenAction.Reject: return OrderStatus.Cancelled;
                default: throw new BusinessRuleException("BR15", "Unsupported kitchen action.");
            }
        }

        private User RequireKitchenStaff(int userId)
        {
            User user = _db.Users.Include(u => u.StaffProfile)
                .FirstOrDefault(u => u.Id == userId && u.Role == ActorRole.KitchenStaff && u.IsActive && !u.IsDeleted);
            if (user == null || user.StaffProfile == null)
                throw new BusinessRuleException("UNAUTHORIZED", "Kitchen staff account not found or inactive.");
            return user;
        }
    }
}
