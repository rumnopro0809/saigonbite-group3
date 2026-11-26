using System;
using System.Collections.Generic;
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
    public class OrderEditTests
    {
        private static readonly DateTimeOffset OffPeakTime =
            new DateTimeOffset(2026, 11, 27, 14, 30, 0, TimeSpan.FromHours(7));

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
        public void EditOrder_InPlaced_ReusesOriginalSnapshotEvenWhenSystemRainChanges()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User local = db.Users.Single(u => u.Identifier == "local@saigonbite.vn");
                Hub hub = db.Hubs.OrderBy(h => h.Id).First();
                MenuItem comTam = db.MenuItems.Single(m => m.Name == "Com Tam Calmette");
                SystemSetting setting = db.SystemSettings.First();
                bool originalRain = setting.IsHeavyRainOn;

                var service = new OrderService(db);
                Order placed = service.PlaceOrder(new CreateOrderRequest
                {
                    CustomerId = local.Id,
                    HubId = hub.Id,
                    DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                    DistanceKm = 1.5m,
                    PaymentMethod = PaymentMethod.MoMo,
                    PlacedAtOverride = OffPeakTime,
                    Items = new List<OrderItemRequest>
                    {
                        new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 1 }
                    }
                });

                try
                {
                    // Admin toggles heavy rain ON after customer placed the order.
                    setting.IsHeavyRainOn = true;
                    db.SaveChanges();

                    Order edited = service.EditOrder(new EditOrderRequest
                    {
                        OrderId = placed.Id,
                        CustomerId = local.Id,
                        ExpectedRowVersion = placed.RowVersion,
                        Items = new List<OrderItemRequest>
                        {
                            new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 2 }
                        }
                    });

                    Assert.AreEqual(OrderStatus.Placed, edited.Status);
                    Assert.IsFalse(edited.HeavyRainSnapshot);
                    Assert.AreEqual(0m, edited.RainFee);
                    Assert.AreEqual(190000m, edited.Subtotal);
                    Assert.AreEqual(OffPeakTime, edited.PlacedAt);
                }
                finally
                {
                    setting.IsHeavyRainOn = originalRain;
                    db.SaveChanges();
                    DeleteTestOrder(db, placed.Id);
                }
            }
        }

        [TestMethod]
        public void EditOrder_OnlinePaidIncrease_RecordsAdditionalChargeAndKeepsPlaced()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User local = db.Users.Single(u => u.Identifier == "local@saigonbite.vn");
                Hub hub = db.Hubs.OrderBy(h => h.Id).First();
                MenuItem comTam = db.MenuItems.Single(m => m.Name == "Com Tam Calmette");

                var service = new OrderService(db);
                Order placed = service.PlaceOrder(new CreateOrderRequest
                {
                    CustomerId = local.Id,
                    HubId = hub.Id,
                    DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                    DistanceKm = 1.5m,
                    PaymentMethod = PaymentMethod.MoMo,
                    PlacedAtOverride = OffPeakTime,
                    Items = new List<OrderItemRequest>
                    {
                        new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 1 }
                    }
                });

                try
                {
                    decimal originalTotal = placed.PayableTotal;
                    Order edited = service.EditOrder(new EditOrderRequest
                    {
                        OrderId = placed.Id,
                        CustomerId = local.Id,
                        ExpectedRowVersion = placed.RowVersion,
                        Items = new List<OrderItemRequest>
                        {
                            new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 2 }
                        }
                    });

                    Assert.AreEqual(OrderStatus.Placed, edited.Status);
                    Assert.IsTrue(edited.PayableTotal > originalTotal);

                    PaymentRecord extra = edited.Payments.Single(p => p.Operation == "EditAdditionalCharge");
                    Assert.AreEqual(edited.PayableTotal - originalTotal, extra.Amount);
                    Assert.AreEqual("Paid", extra.Result);
                }
                finally
                {
                    DeleteTestOrder(db, placed.Id);
                }
            }
        }

        [TestMethod]
        public void EditOrder_OnlinePaidDecrease_RecordsPartialRefundAndKeepsPlaced()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User local = db.Users.Single(u => u.Identifier == "local@saigonbite.vn");
                Hub hub = db.Hubs.OrderBy(h => h.Id).First();
                MenuItem comTam = db.MenuItems.Single(m => m.Name == "Com Tam Calmette");

                var service = new OrderService(db);
                Order placed = service.PlaceOrder(new CreateOrderRequest
                {
                    CustomerId = local.Id,
                    HubId = hub.Id,
                    DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                    DistanceKm = 1.5m,
                    PaymentMethod = PaymentMethod.VNPay,
                    PlacedAtOverride = OffPeakTime,
                    Items = new List<OrderItemRequest>
                    {
                        new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 2 }
                    }
                });

                try
                {
                    decimal originalTotal = placed.PayableTotal;
                    Order edited = service.EditOrder(new EditOrderRequest
                    {
                        OrderId = placed.Id,
                        CustomerId = local.Id,
                        ExpectedRowVersion = placed.RowVersion,
                        Items = new List<OrderItemRequest>
                        {
                            new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 1 }
                        }
                    });

                    Assert.AreEqual(OrderStatus.Placed, edited.Status);
                    Assert.IsTrue(edited.PayableTotal < originalTotal);

                    PaymentRecord partialRefund = edited.Payments.Single(p => p.Operation == "EditPartialRefund");
                    Assert.AreEqual(originalTotal - edited.PayableTotal, partialRefund.Amount);
                    Assert.AreEqual("Refunded", partialRefund.Result);
                }
                finally
                {
                    DeleteTestOrder(db, placed.Id);
                }
            }
        }

        [TestMethod]
        public void EditOrder_InConfirmedStatus_ThrowsBR17()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User local = db.Users.Single(u => u.Identifier == "local@saigonbite.vn");
                User kitchen = db.Users.Single(u => u.Identifier == "kitchen.d1@saigonbite.vn");
                Hub hub = db.Hubs.OrderBy(h => h.Id).First();
                MenuItem comTam = db.MenuItems.Single(m => m.Name == "Com Tam Calmette");

                var orderService = new OrderService(db);
                var transitionService = new OrderTransitionService(db);

                Order placed = orderService.PlaceOrder(new CreateOrderRequest
                {
                    CustomerId = local.Id,
                    HubId = hub.Id,
                    DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                    DistanceKm = 1.5m,
                    PaymentMethod = PaymentMethod.MoMo,
                    PlacedAtOverride = OffPeakTime,
                    Items = new List<OrderItemRequest>
                    {
                        new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 1 }
                    }
                });

                try
                {
                    transitionService.Transition(new TransitionOrderRequest
                    {
                        OrderId = placed.Id,
                        ActorId = kitchen.Id,
                        TargetStatus = OrderStatus.Confirmed
                    });

                    var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                        orderService.EditOrder(new EditOrderRequest
                        {
                            OrderId = placed.Id,
                            CustomerId = local.Id,
                            Items = new List<OrderItemRequest>
                            {
                                new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 2 }
                            }
                        }));

                    Assert.AreEqual("BR17", ex.Rule);
                }
                finally
                {
                    DeleteTestOrder(db, placed.Id);
                }
            }
        }

        [TestMethod]
        public void EditOrder_CodExceedsMillionAfterEdit_ThrowsBR14()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User local = db.Users.Single(u => u.Identifier == "local@saigonbite.vn");
                Hub hub = db.Hubs.OrderBy(h => h.Id).First();
                MenuItem mienCua = db.MenuItems.Single(m => m.Name == "Mien Xao Cua");

                var service = new OrderService(db);
                Order placed = service.PlaceOrder(new CreateOrderRequest
                {
                    CustomerId = local.Id,
                    HubId = hub.Id,
                    DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                    DistanceKm = 1.5m,
                    PaymentMethod = PaymentMethod.Cod,
                    PlacedAtOverride = OffPeakTime,
                    Items = new List<OrderItemRequest>
                    {
                        new OrderItemRequest { MenuItemId = mienCua.Id, Quantity = 1 }
                    }
                });

                try
                {
                    var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                        service.EditOrder(new EditOrderRequest
                        {
                            OrderId = placed.Id,
                            CustomerId = local.Id,
                            ExpectedRowVersion = placed.RowVersion,
                            Items = new List<OrderItemRequest>
                            {
                                new OrderItemRequest { MenuItemId = mienCua.Id, Quantity = 12 }
                            }
                        }));

                    Assert.AreEqual("BR14", ex.Rule);
                    Order reloaded = db.Orders.AsNoTracking().Include(o => o.Lines).Single(o => o.Id == placed.Id);
                    Assert.AreEqual(1, reloaded.Lines.Single().Quantity);
                }
                finally
                {
                    DeleteTestOrder(db, placed.Id);
                }
            }
        }

        [TestMethod]
        public void EditOrder_StaleRowVersion_ThrowsConcurrency()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User local = db.Users.Single(u => u.Identifier == "local@saigonbite.vn");
                Hub hub = db.Hubs.OrderBy(h => h.Id).First();
                MenuItem comTam = db.MenuItems.Single(m => m.Name == "Com Tam Calmette");

                var service = new OrderService(db);
                Order placed = service.PlaceOrder(new CreateOrderRequest
                {
                    CustomerId = local.Id,
                    HubId = hub.Id,
                    DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                    DistanceKm = 1.5m,
                    PaymentMethod = PaymentMethod.Cod,
                    PlacedAtOverride = OffPeakTime,
                    Items = new List<OrderItemRequest>
                    {
                        new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 1 }
                    }
                });

                try
                {
                    byte[] staleVersion = placed.RowVersion.ToArray();

                    // First edit succeeds and advances SQL Server RowVersion.
                    service.EditOrder(new EditOrderRequest
                    {
                        OrderId = placed.Id,
                        CustomerId = local.Id,
                        ExpectedRowVersion = staleVersion,
                        Items = new List<OrderItemRequest>
                        {
                            new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 2 }
                        }
                    });

                    // Second edit using the old RowVersion must fail concurrency check.
                    var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                        service.EditOrder(new EditOrderRequest
                        {
                            OrderId = placed.Id,
                            CustomerId = local.Id,
                            ExpectedRowVersion = staleVersion,
                            Items = new List<OrderItemRequest>
                            {
                                new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 3 }
                            }
                        }));

                    Assert.AreEqual("CONCURRENCY", ex.Rule);
                }
                finally
                {
                    DeleteTestOrder(db, placed.Id);
                }
            }
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
