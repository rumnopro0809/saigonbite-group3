using System;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonBite.Application.Orders;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;
using SaigonBite.Infrastructure.Migrations;
using SaigonBite.Infrastructure.Services;

namespace SaigonBite.Tests
{
    [TestClass]
    public class OrderTransitionTests
    {
        [ClassInitialize]
        public static void EnsureDatabaseSeeded(TestContext context)
        {
            Database.SetInitializer(
                new MigrateDatabaseToLatestVersion<SaigonBiteDbContext, Configuration>());

            using (var db = new SaigonBiteDbContext())
            {
                db.Database.Initialize(force: false);
            }
        }

        [TestMethod]
        public void Transition_PlacedToConfirmedByOwnHubKitchen_UpdatesStatusAndWritesHistory()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User kitchen = db.Users.Single(u => u.Identifier == "kitchen.d1@saigonbite.vn");
                Order order = CreateTestOrder(db, OrderStatus.Placed, PaymentMethod.MoMo, "Paid");
                try
                {
                    var service = new OrderTransitionService(db);
                    Order updated = service.Transition(new TransitionOrderRequest
                    {
                        OrderId = order.Id,
                        ActorId = kitchen.Id,
                        TargetStatus = OrderStatus.Confirmed,
                        Reason = "Kitchen accepted order"
                    });

                    Assert.AreEqual(OrderStatus.Confirmed, updated.Status);
                    var history = db.OrderStatusHistories.Single(h => h.OrderId == order.Id);
                    Assert.AreEqual(OrderStatus.Placed, history.FromStatus);
                    Assert.AreEqual(OrderStatus.Confirmed, history.ToStatus);
                    Assert.AreEqual(kitchen.Id, history.ActorId);
                }
                finally
                {
                    DeleteTestOrder(db, order.Id);
                }
            }
        }

        [TestMethod]
        public void Transition_ConfirmedToCancelledByCustomerOnMoMo_RefundsFullAmountAtomically()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User customer = db.Users.Single(u => u.Identifier == "local@saigonbite.vn");
                Order order = CreateTestOrder(db, OrderStatus.Confirmed, PaymentMethod.MoMo, "Paid");
                try
                {
                    var service = new OrderTransitionService(db);
                    Order updated = service.Transition(new TransitionOrderRequest
                    {
                        OrderId = order.Id,
                        ActorId = customer.Id,
                        TargetStatus = OrderStatus.Cancelled,
                        Reason = "Changed dining plans"
                    });

                    Assert.AreEqual(OrderStatus.Cancelled, updated.Status);
                    Assert.AreEqual("Refunded", updated.PaymentStatus);

                    var refundRecord = db.PaymentRecords.Single(p => p.OrderId == order.Id && p.Operation == "Refund");
                    Assert.AreEqual(order.PayableTotal, refundRecord.Amount);
                    Assert.AreEqual("Refunded", refundRecord.Result);
                    Assert.IsTrue(db.OrderStatusHistories.Any(h => h.OrderId == order.Id && h.ToStatus == OrderStatus.Cancelled));
                }
                finally
                {
                    DeleteTestOrder(db, order.Id);
                }
            }
        }

        [TestMethod]
        public void Transition_OutForDeliveryToDeliveredOnCodWithCashCollected_MarksPaid()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User shipper = db.Users.Single(u => u.Identifier == "shipper1@saigonbite.vn");
                Order order = CreateTestOrder(db, OrderStatus.OutForDelivery, PaymentMethod.Cod, "PendingCash", shipper.Id);
                try
                {
                    var service = new OrderTransitionService(db);
                    Order updated = service.Transition(new TransitionOrderRequest
                    {
                        OrderId = order.Id,
                        ActorId = shipper.Id,
                        TargetStatus = OrderStatus.Delivered,
                        CashCollected = true
                    });

                    Assert.AreEqual(OrderStatus.Delivered, updated.Status);
                    Assert.AreEqual("Paid", updated.PaymentStatus);

                    var cashRecord = db.PaymentRecords.Single(p => p.OrderId == order.Id && p.Operation == "CodCollected");
                    Assert.AreEqual(order.PayableTotal, cashRecord.Amount);
                    Assert.AreEqual("Paid", cashRecord.Result);
                }
                finally
                {
                    DeleteTestOrder(db, order.Id);
                }
            }
        }

        [TestMethod]
        public void Transition_Invalid_PreparingToCancelledByCustomer_ThrowsBR15()
        {
            AssertInvalidTransition(
                OrderStatus.Preparing,
                OrderStatus.Cancelled,
                PaymentMethod.MoMo,
                "Paid",
                "local@saigonbite.vn");
        }

        [TestMethod]
        public void Transition_Invalid_ReadyToDeliveredByShipper_ThrowsBR15()
        {
            AssertInvalidTransition(
                OrderStatus.Ready,
                OrderStatus.Delivered,
                PaymentMethod.MoMo,
                "Paid",
                "shipper1@saigonbite.vn",
                assignDefaultShipper: true);
        }

        [TestMethod]
        public void Transition_Invalid_DeliveredToPlacedByAdmin_ThrowsBR15()
        {
            AssertInvalidTransition(
                OrderStatus.Delivered,
                OrderStatus.Placed,
                PaymentMethod.MoMo,
                "Paid",
                "admin@saigonbite.vn");
        }

        [TestMethod]
        public void Transition_Invalid_PlacedToPreparingSkippingConfirmed_ThrowsBR15()
        {
            AssertInvalidTransition(
                OrderStatus.Placed,
                OrderStatus.Preparing,
                PaymentMethod.MoMo,
                "Paid",
                "kitchen.d1@saigonbite.vn");
        }

        [TestMethod]
        public void Transition_Invalid_CodDeliveredWithoutCashCollected_ThrowsBR15()
        {
            AssertInvalidTransition(
                OrderStatus.OutForDelivery,
                OrderStatus.Delivered,
                PaymentMethod.Cod,
                "PendingCash",
                "shipper1@saigonbite.vn",
                assignDefaultShipper: true,
                cashCollected: false);
        }

        [TestMethod]
        public void Transition_Invalid_KitchenCancelPlacedWithoutReason_ThrowsBR15()
        {
            AssertInvalidTransition(
                OrderStatus.Placed,
                OrderStatus.Cancelled,
                PaymentMethod.MoMo,
                "Paid",
                "kitchen.d1@saigonbite.vn",
                reason: "   ");
        }

        private static void AssertInvalidTransition(
            OrderStatus initialStatus,
            OrderStatus targetStatus,
            PaymentMethod method,
            string paymentStatus,
            string actorIdentifier,
            bool assignDefaultShipper = false,
            bool cashCollected = false,
            string reason = null)
        {
            using (var db = new SaigonBiteDbContext())
            {
                User actor = db.Users.Single(u => u.Identifier == actorIdentifier);
                int? shipperId = assignDefaultShipper ? (int?)actor.Id : null;
                Order order = CreateTestOrder(db, initialStatus, method, paymentStatus, shipperId);
                try
                {
                    var service = new OrderTransitionService(db);
                    var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                        service.Transition(new TransitionOrderRequest
                        {
                            OrderId = order.Id,
                            ActorId = actor.Id,
                            TargetStatus = targetStatus,
                            CashCollected = cashCollected,
                            Reason = reason
                        }));

                    Assert.AreEqual("BR15", ex.Rule);

                    Order reloaded = db.Orders.AsNoTracking().Single(o => o.Id == order.Id);
                    Assert.AreEqual(initialStatus, reloaded.Status);
                    Assert.AreEqual(0, db.OrderStatusHistories.Count(h => h.OrderId == order.Id));
                }
                finally
                {
                    DeleteTestOrder(db, order.Id);
                }
            }
        }

        private static Order CreateTestOrder(
            SaigonBiteDbContext db,
            OrderStatus status,
            PaymentMethod method,
            string paymentStatus,
            int? shipperId = null)
        {
            User customer = db.Users.Single(u => u.Identifier == "local@saigonbite.vn");
            Hub hub = db.Hubs.OrderBy(h => h.Id).First();
            MenuItem item = db.MenuItems.OrderBy(m => m.Id).First();

            var order = new Order
            {
                OrderCode = "SB-TST-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                CustomerId = customer.Id,
                HubId = hub.Id,
                ShipperId = shipperId,
                Status = status,
                DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                DistanceKm = 3.2m,
                PlacedAt = new DateTimeOffset(2026, 11, 25, 14, 0, 0, TimeSpan.FromHours(7)),
                HubLoadSnapshot = 0.20m,
                HeavyRainSnapshot = false,
                Subtotal = 200000m,
                QuietDiscount = 20000m,
                VoucherDiscount = 0m,
                FoodTotal = 180000m,
                DeliveryFee = 25000m,
                RushFee = 0m,
                RainFee = 0m,
                RawTotal = 205000m,
                PayableTotal = 205000m,
                PaymentMethod = method,
                PaymentStatus = paymentStatus
            };
            order.Lines.Add(new OrderLine
            {
                MenuItemId = item.Id,
                Quantity = 2,
                UnitPriceSnapshot = 100000m,
                ItemNameSnapshot = item.Name,
                CategorySnapshot = "Test"
            });

            db.Orders.Add(order);
            db.SaveChanges();
            return order;
        }

        private static void DeleteTestOrder(SaigonBiteDbContext db, int orderId)
        {
            db.OrderStatusHistories.RemoveRange(db.OrderStatusHistories.Where(h => h.OrderId == orderId));
            db.PaymentRecords.RemoveRange(db.PaymentRecords.Where(p => p.OrderId == orderId));
            db.OrderLines.RemoveRange(db.OrderLines.Where(l => l.OrderId == orderId));
            Order order = db.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order != null) db.Orders.Remove(order);
            db.SaveChanges();
        }
    }
}
