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
    public class OrderServiceTests
    {
        private static readonly DateTimeOffset OffPeakTime =
            new DateTimeOffset(2026, 11, 26, 14, 30, 0, TimeSpan.FromHours(7));

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
        public void Quote_AuthoritativeDbPricesWithQuietDiscountAndVoucher_CalculatesExpectedBreakdown()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User local = db.Users.Single(u => u.Identifier == "local@saigonbite.vn");
                Hub hub = db.Hubs.OrderBy(h => h.Id).First();
                MenuItem comTam = db.MenuItems.Single(m => m.Name == "Com Tam Calmette");
                MenuItem banhMi = db.MenuItems.Single(m => m.Name == "Banh Mi Huynh Hoa");

                var service = new OrderService(db);
                OrderQuoteResult quote = service.Quote(new CreateOrderRequest
                {
                    CustomerId = local.Id,
                    HubId = hub.Id,
                    DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                    DistanceKm = 3.2m,
                    VoucherCode = " saigon15 ",
                    PlacedAtOverride = OffPeakTime,
                    Items = new List<OrderItemRequest>
                    {
                        new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 1 },
                        new OrderItemRequest { MenuItemId = banhMi.Id, Quantity = 1 }
                    }
                });

                Assert.AreEqual(200000m, quote.Breakdown.Subtotal);
                Assert.AreEqual(20000m, quote.Breakdown.QuietDiscount);
                Assert.AreEqual(27000m, quote.Breakdown.VoucherDiscount);
                Assert.AreEqual(153000m, quote.Breakdown.FoodTotal);
                Assert.AreEqual(25000m, quote.Breakdown.DeliveryFee);
                Assert.AreEqual(178000m, quote.Breakdown.PayableTotal);
                Assert.AreEqual("SAIGON15", quote.VoucherCode);
            }
        }

        [TestMethod]
        public void PlaceOrder_LocalCustomerWithMoMo_SavesOrderLinesPaymentAndHistoryAtomically()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User local = db.Users.Single(u => u.Identifier == "local@saigonbite.vn");
                Hub hub = db.Hubs.OrderBy(h => h.Id).First();
                MenuItem comTam = db.MenuItems.Single(m => m.Name == "Com Tam Calmette");
                MenuItem banhMi = db.MenuItems.Single(m => m.Name == "Banh Mi Huynh Hoa");

                var service = new OrderService(db);
                Order placed = service.PlaceOrder(new CreateOrderRequest
                {
                    CustomerId = local.Id,
                    HubId = hub.Id,
                    DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                    DistanceKm = 3.2m,
                    PaymentMethod = PaymentMethod.MoMo,
                    RequestKey = "REQ-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    ExpectedPayableTotal = 205000m,
                    PlacedAtOverride = OffPeakTime,
                    Items = new List<OrderItemRequest>
                    {
                        new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 1 },
                        new OrderItemRequest { MenuItemId = banhMi.Id, Quantity = 1 }
                    }
                });

                try
                {
                    Assert.AreEqual(OrderStatus.Placed, placed.Status);
                    Assert.AreEqual("Paid", placed.PaymentStatus);
                    Assert.AreEqual(205000m, placed.PayableTotal);
                    Assert.AreEqual(2, placed.Lines.Count);
                    Assert.AreEqual(1, placed.Payments.Count);
                    Assert.AreEqual("Charge", placed.Payments.First().Operation);
                    Assert.AreEqual(1, placed.StatusHistory.Count);
                    Assert.AreEqual(OrderStatus.Placed, placed.StatusHistory.First().ToStatus);
                    Assert.IsNull(placed.StatusHistory.First().FromStatus);
                }
                finally
                {
                    DeleteTestOrder(db, placed.Id);
                }
            }
        }

        [TestMethod]
        public void PlaceOrder_DuplicateRequestKey_ReturnsExistingOrderWithoutDoubleCharge()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User local = db.Users.Single(u => u.Identifier == "local@saigonbite.vn");
                Hub hub = db.Hubs.OrderBy(h => h.Id).First();
                MenuItem comTam = db.MenuItems.Single(m => m.Name == "Com Tam Calmette");
                string key = "IDEM-" + Guid.NewGuid().ToString("N").Substring(0, 8);

                var service = new OrderService(db);
                var req = new CreateOrderRequest
                {
                    CustomerId = local.Id,
                    HubId = hub.Id,
                    DeliveryAddress = "10 Le Loi, District 1, HCMC",
                    DistanceKm = 1.5m,
                    PaymentMethod = PaymentMethod.VNPay,
                    RequestKey = key,
                    PlacedAtOverride = OffPeakTime,
                    Items = new List<OrderItemRequest>
                    {
                        new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 1 }
                    }
                };

                Order first = service.PlaceOrder(req);
                try
                {
                    Order second = service.PlaceOrder(req);

                    Assert.AreEqual(first.Id, second.Id);
                    Assert.AreEqual(1, db.Orders.Count(o => o.RequestKey == key));
                    Assert.AreEqual(1, db.PaymentRecords.Count(p => p.OrderId == first.Id));
                }
                finally
                {
                    DeleteTestOrder(db, first.Id);
                }
            }
        }

        [TestMethod]
        public void PlaceOrder_StaleExpectedTotal_ThrowsQuoteChanged()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User local = db.Users.Single(u => u.Identifier == "local@saigonbite.vn");
                Hub hub = db.Hubs.OrderBy(h => h.Id).First();
                MenuItem comTam = db.MenuItems.Single(m => m.Name == "Com Tam Calmette");
                MenuItem banhMi = db.MenuItems.Single(m => m.Name == "Banh Mi Huynh Hoa");

                var service = new OrderService(db);
                string key = "STALE-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    service.PlaceOrder(new CreateOrderRequest
                    {
                        CustomerId = local.Id,
                        HubId = hub.Id,
                        DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                        DistanceKm = 3.2m,
                        PaymentMethod = PaymentMethod.MoMo,
                        RequestKey = key,
                        ExpectedPayableTotal = 150000m,
                        PlacedAtOverride = OffPeakTime,
                        Items = new List<OrderItemRequest>
                        {
                            new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 1 },
                            new OrderItemRequest { MenuItemId = banhMi.Id, Quantity = 1 }
                        }
                    }));

                Assert.AreEqual("QUOTE_CHANGED", ex.Rule);
                Assert.AreEqual(0, db.Orders.Count(o => o.RequestKey == key));
            }
        }

        [TestMethod]
        public void PlaceOrder_TouristWithMoMo_ThrowsBR13()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User tourist = db.Users.Single(u => u.Identifier == "tourist@saigonbite.vn");
                Hub hub = db.Hubs.OrderBy(h => h.Id).First();
                MenuItem comTam = db.MenuItems.Single(m => m.Name == "Com Tam Calmette");

                var service = new OrderService(db);
                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    service.PlaceOrder(new CreateOrderRequest
                    {
                        CustomerId = tourist.Id,
                        HubId = hub.Id,
                        DeliveryAddress = "Rex Hotel, District 1, HCMC",
                        DistanceKm = 1.0m,
                        PaymentMethod = PaymentMethod.MoMo,
                        PlacedAtOverride = OffPeakTime,
                        Items = new List<OrderItemRequest>
                        {
                            new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 1 }
                        }
                    }));

                Assert.AreEqual("BR13", ex.Rule);
            }
        }

        [TestMethod]
        public void GetOrderDetails_OtherCustomer_ThrowsUnauthorized()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User local = db.Users.Single(u => u.Identifier == "local@saigonbite.vn");
                User tourist = db.Users.Single(u => u.Identifier == "tourist@saigonbite.vn");
                Hub hub = db.Hubs.OrderBy(h => h.Id).First();
                MenuItem comTam = db.MenuItems.Single(m => m.Name == "Com Tam Calmette");

                var service = new OrderService(db);
                Order placed = service.PlaceOrder(new CreateOrderRequest
                {
                    CustomerId = local.Id,
                    HubId = hub.Id,
                    DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                    DistanceKm = 1.2m,
                    PaymentMethod = PaymentMethod.Cod,
                    PlacedAtOverride = OffPeakTime,
                    Items = new List<OrderItemRequest>
                    {
                        new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 1 }
                    }
                });

                try
                {
                    var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                        service.GetOrderDetails(placed.Id, tourist.Id));

                    Assert.AreEqual("UNAUTHORIZED", ex.Rule);
                }
                finally
                {
                    DeleteTestOrder(db, placed.Id);
                }
            }
        }

        [TestMethod]
        public void CalculateHubLoad_CountsOnlyConfirmedAndPreparingOrders_MatchesBR8()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User local = db.Users.Single(u => u.Identifier == "local@saigonbite.vn");
                MenuItem comTam = db.MenuItems.Single(m => m.Name == "Com Tam Calmette");

                var tempHub = new Hub
                {
                    Name = "Temp Load Test Hub " + Guid.NewGuid().ToString("N").Substring(0, 6),
                    Address = "1 Test Street",
                    District = "District 1",
                    MaxCapacity = 10,
                    IsActive = true,
                    IsDeleted = false
                };
                db.Hubs.Add(tempHub);
                db.SaveChanges();

                var createdOrderIds = new List<int>();
                try
                {
                    OrderStatus[] statuses =
                    {
                        OrderStatus.Placed,
                        OrderStatus.Confirmed,
                        OrderStatus.Preparing,
                        OrderStatus.Ready,
                        OrderStatus.Delivered,
                        OrderStatus.Cancelled
                    };

                    foreach (OrderStatus status in statuses)
                    {
                        var order = new Order
                        {
                            OrderCode = "SB-LD-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                            CustomerId = local.Id,
                            HubId = tempHub.Id,
                            Status = status,
                            DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                            DistanceKm = 1.5m,
                            PlacedAt = OffPeakTime,
                            HubLoadSnapshot = 0m,
                            HeavyRainSnapshot = false,
                            Subtotal = comTam.Price,
                            QuietDiscount = 0m,
                            VoucherDiscount = 0m,
                            FoodTotal = comTam.Price,
                            DeliveryFee = 15000m,
                            RushFee = 0m,
                            RainFee = 0m,
                            RawTotal = comTam.Price + 15000m,
                            PayableTotal = comTam.Price + 15000m,
                            PaymentMethod = PaymentMethod.Cod,
                            PaymentStatus = "PendingCash"
                        };
                        db.Orders.Add(order);
                        db.SaveChanges();
                        createdOrderIds.Add(order.Id);
                    }

                    var service = new OrderService(db);
                    decimal load = service.CalculateHubLoad(tempHub.Id, tempHub.MaxCapacity);

                    // BR8: only Confirmed (1) + Preparing (1) = 2 of 10 -> 0.20 (Placed and Ready excluded).
                    Assert.AreEqual(0.20m, load);
                }
                finally
                {
                    foreach (int orderId in createdOrderIds)
                    {
                        DeleteTestOrder(db, orderId);
                    }
                    Hub toRemove = db.Hubs.FirstOrDefault(h => h.Id == tempHub.Id);
                    if (toRemove != null)
                    {
                        db.Hubs.Remove(toRemove);
                        db.SaveChanges();
                    }
                }
            }
        }

        [TestMethod]
        public void Quote_WhenHubLoadReaches90Percent_ThrowsBR8AndSuggestsAlternativeHubs()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User local = db.Users.Single(u => u.Identifier == "local@saigonbite.vn");
                MenuItem comTam = db.MenuItems.Single(m => m.Name == "Com Tam Calmette");

                var overloadedHub = new Hub
                {
                    Name = "Temp Overloaded Hub " + Guid.NewGuid().ToString("N").Substring(0, 6),
                    Address = "99 Busy Road",
                    District = "District 1",
                    MaxCapacity = 2,
                    IsActive = true,
                    IsDeleted = false
                };
                db.Hubs.Add(overloadedHub);
                db.SaveChanges();

                db.HubBrands.Add(new HubBrand { HubId = overloadedHub.Id, BrandId = comTam.BrandId });
                db.SaveChanges();

                var createdOrderIds = new List<int>();
                try
                {
                    // 2 Confirmed/Preparing orders on capacity 2 -> 100% load (>= 90%).
                    foreach (OrderStatus status in new[] { OrderStatus.Confirmed, OrderStatus.Preparing })
                    {
                        var order = new Order
                        {
                            OrderCode = "SB-OV-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                            CustomerId = local.Id,
                            HubId = overloadedHub.Id,
                            Status = status,
                            DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                            DistanceKm = 1.5m,
                            PlacedAt = OffPeakTime,
                            HubLoadSnapshot = 0.5m,
                            HeavyRainSnapshot = false,
                            Subtotal = comTam.Price,
                            QuietDiscount = 0m,
                            VoucherDiscount = 0m,
                            FoodTotal = comTam.Price,
                            DeliveryFee = 15000m,
                            RushFee = 0m,
                            RainFee = 0m,
                            RawTotal = comTam.Price + 15000m,
                            PayableTotal = comTam.Price + 15000m,
                            PaymentMethod = PaymentMethod.Cod,
                            PaymentStatus = "PendingCash"
                        };
                        db.Orders.Add(order);
                        db.SaveChanges();
                        createdOrderIds.Add(order.Id);
                    }

                    var service = new OrderService(db);
                    var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                        service.Quote(new CreateOrderRequest
                        {
                            CustomerId = local.Id,
                            HubId = overloadedHub.Id,
                            DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                            DistanceKm = 1.5m,
                            PlacedAtOverride = OffPeakTime,
                            Items = new List<OrderItemRequest>
                            {
                                new OrderItemRequest { MenuItemId = comTam.Id, Quantity = 1 }
                            }
                        }));

                    Assert.AreEqual("BR8", ex.Rule);
                    StringAssert.Contains(ex.Message, "Please choose another hub:");
                    StringAssert.Contains(ex.Message, "District 1");
                }
                finally
                {
                    foreach (int orderId in createdOrderIds)
                    {
                        DeleteTestOrder(db, orderId);
                    }
                    db.HubBrands.RemoveRange(db.HubBrands.Where(hb => hb.HubId == overloadedHub.Id));
                    Hub toRemove = db.Hubs.FirstOrDefault(h => h.Id == overloadedHub.Id);
                    if (toRemove != null) db.Hubs.Remove(toRemove);
                    db.SaveChanges();
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
