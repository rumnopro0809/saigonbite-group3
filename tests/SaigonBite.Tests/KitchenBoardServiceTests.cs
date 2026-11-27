using System;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;
using SaigonBite.Infrastructure.Services;
using SaigonBite.Infrastructure.Migrations;

namespace SaigonBite.Tests
{
    [TestClass]
    public class KitchenBoardServiceTests
    {
        [ClassInitialize]
        public static void EnsureDatabaseMigrated(TestContext context)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<SaigonBiteDbContext, Configuration>());
            using (var db = new SaigonBiteDbContext()) db.Database.Initialize(force: false);
        }

        [TestMethod]
        public void GetBoard_ReturnsOnlyOwnHubActiveOrdersInFifoOrderWithLines()
        {
            using (var f = new Fixture())
            {
                DateTimeOffset t0 = new DateTimeOffset(2026, 11, 25, 12, 0, 0, TimeSpan.FromHours(7));
                Order newest = f.CreateOrder(OrderStatus.Ready, placedAt: t0.AddMinutes(30));
                Order oldest = f.CreateOrder(OrderStatus.Placed, placedAt: t0);
                Order middle = f.CreateOrder(OrderStatus.Preparing, placedAt: t0.AddMinutes(10));
                Order confirmed = f.CreateOrder(OrderStatus.Confirmed, placedAt: t0.AddMinutes(20));
                f.CreateOrder(OrderStatus.Delivered, placedAt: t0.AddMinutes(1));
                f.CreateOrder(OrderStatus.Cancelled, placedAt: t0.AddMinutes(2));
                f.CreateOrder(OrderStatus.Failed, placedAt: t0.AddMinutes(3));
                f.CreateOrder(OrderStatus.OutForDelivery, placedAt: t0.AddMinutes(4));
                Order foreign = f.CreateOrder(OrderStatus.Placed, placedAt: t0.AddMinutes(5), hubId: f.OtherHub.Id);

                KitchenBoard board = new KitchenBoardService(f.Db).GetBoard(f.Kitchen.Id);

                Assert.AreEqual(f.Hub.Id, board.HubId);
                Assert.AreEqual(f.Hub.Name, board.HubName);
                Assert.AreEqual(10, board.Capacity);
                CollectionAssert.AreEqual(
                    new[] { oldest.Id, middle.Id, confirmed.Id, newest.Id },
                    board.Orders.Select(o => o.OrderId).ToArray());
                Assert.IsFalse(board.Orders.Any(o => o.OrderId == foreign.Id));

                KitchenOrderCard card = board.Orders.Single(o => o.OrderId == oldest.Id);
                Assert.AreEqual(oldest.OrderCode, card.OrderCode);
                Assert.AreEqual(OrderStatus.Placed, card.Status);
                Assert.AreEqual(oldest.PayableTotal, card.PayableTotal);
                Assert.AreEqual(PaymentMethod.MoMo, card.PaymentMethod);
                Assert.AreEqual("Paid", card.PaymentStatus);
                Assert.AreEqual(oldest.DeliveryAddress, card.DeliveryAddress);
                Assert.AreEqual(t0, card.PlacedAt);
                Assert.IsFalse(string.IsNullOrEmpty(card.RowVersion));
                Assert.AreEqual(1, card.Lines.Count);
                Assert.AreEqual(2, card.Lines[0].Quantity);
                Assert.AreEqual(f.ItemName, card.Lines[0].Name);
            }
        }

        [TestMethod]
        public void GetBoard_Load_CountsOnlyConfirmedAndPreparing()
        {
            using (var f = new Fixture())
            {
                f.CreateOrder(OrderStatus.Confirmed);
                f.CreateOrder(OrderStatus.Confirmed);
                f.CreateOrder(OrderStatus.Preparing);
                f.CreateOrder(OrderStatus.Placed);
                f.CreateOrder(OrderStatus.Ready);

                KitchenBoard board = new KitchenBoardService(f.Db).GetBoard(f.Kitchen.Id);

                Assert.AreEqual(10, board.Capacity);
                Assert.AreEqual(3, board.ActiveOrders);
                Assert.AreEqual(0.30m, board.Load);
                Assert.AreEqual(30, board.LoadPercent);
                Assert.AreEqual(5, board.Orders.Count);
            }
        }

        [TestMethod]
        public void GetBoard_EmptyHub_ReturnsEmptyBoardWithZeroLoad()
        {
            using (var f = new Fixture())
            {
                KitchenBoard board = new KitchenBoardService(f.Db).GetBoard(f.Kitchen.Id);
                Assert.AreEqual(f.Hub.Id, board.HubId);
                Assert.AreEqual(0, board.Orders.Count);
                Assert.AreEqual(0, board.ActiveOrders);
                Assert.AreEqual(0, board.LoadPercent);
            }
        }

        [TestMethod]
        public void GetBoard_NonKitchenOrInvalidUsers_ThrowUnauthorized()
        {
            using (var f = new Fixture())
            {
                var service = new KitchenBoardService(f.Db);
                foreach (int userId in new[] { f.Customer.Id, f.Admin.Id, f.InactiveKitchen.Id, f.ProfilelessKitchen.Id, int.MaxValue })
                {
                    int id = userId;
                    var ex = Assert.ThrowsException<BusinessRuleException>(() => service.GetBoard(id));
                    Assert.AreEqual("UNAUTHORIZED", ex.Rule);
                }
            }
        }

        [TestMethod]
        public void Act_NonKitchenOrInvalidUsers_ThrowUnauthorizedAndLeaveOrderUnchanged()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.Placed);
                var service = new KitchenBoardService(f.Db);
                foreach (int userId in new[] { f.Customer.Id, f.Admin.Id, f.InactiveKitchen.Id, f.ProfilelessKitchen.Id, int.MaxValue })
                {
                    int id = userId;
                    var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                        service.Act(id, order.Id, KitchenAction.Confirm, null, null));
                    Assert.AreEqual("UNAUTHORIZED", ex.Rule);
                }
                f.AssertUnchanged(order.Id, OrderStatus.Placed);
            }
        }

        [TestMethod]
        public void Act_HappyPath_PlacedToConfirmedToPreparingToReady_WritesHistoryAndRaisesLoad()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.Placed);
                var service = new KitchenBoardService(f.Db);
                Assert.AreEqual(0, service.GetBoard(f.Kitchen.Id).ActiveOrders);

                DateTimeOffset before = DateTimeOffset.UtcNow.AddMinutes(-1);

                Order result = service.Act(f.Kitchen.Id, order.Id, KitchenAction.Confirm, null, f.RowVersion(order.Id));
                Assert.AreEqual(OrderStatus.Confirmed, result.Status);
                Assert.AreEqual(1, service.GetBoard(f.Kitchen.Id).ActiveOrders);
                Assert.AreEqual(10, service.GetBoard(f.Kitchen.Id).LoadPercent);

                service.Act(f.Kitchen.Id, order.Id, KitchenAction.StartPreparing, null, f.RowVersion(order.Id));
                Assert.AreEqual(1, service.GetBoard(f.Kitchen.Id).ActiveOrders);

                service.Act(f.Kitchen.Id, order.Id, KitchenAction.MarkReady, null, f.RowVersion(order.Id));
                KitchenBoard board = service.GetBoard(f.Kitchen.Id);
                Assert.AreEqual(0, board.ActiveOrders);
                Assert.AreEqual(OrderStatus.Ready, board.Orders.Single(o => o.OrderId == order.Id).Status);

                using (var db = new SaigonBiteDbContext())
                {
                    Assert.AreEqual(OrderStatus.Ready, db.Orders.Single(o => o.Id == order.Id).Status);
                    var history = db.OrderStatusHistories.Where(h => h.OrderId == order.Id).OrderBy(h => h.Id).ToList();
                    Assert.AreEqual(3, history.Count);
                    var expected = new[]
                    {
                        Tuple.Create(OrderStatus.Placed, OrderStatus.Confirmed),
                        Tuple.Create(OrderStatus.Confirmed, OrderStatus.Preparing),
                        Tuple.Create(OrderStatus.Preparing, OrderStatus.Ready)
                    };
                    for (int i = 0; i < 3; i++)
                    {
                        Assert.AreEqual(f.Kitchen.Id, history[i].ActorId);
                        Assert.AreEqual(expected[i].Item1, history[i].FromStatus);
                        Assert.AreEqual(expected[i].Item2, history[i].ToStatus);
                        Assert.AreEqual(expected[i].Item1, history[i].FromStatus);
                        Assert.AreEqual(expected[i].Item2, history[i].ToStatus);
                        Assert.IsTrue(history[i].ChangedAt > before);
                    }
                }
            }
        }

        [TestMethod]
        public void Act_ConfirmOrderOfAnotherHub_ThrowsBR15AndLeavesOrderUnchanged()
        {
            using (var f = new Fixture())
            {
                Order foreign = f.CreateOrder(OrderStatus.Placed, hubId: f.OtherHub.Id);
                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    new KitchenBoardService(f.Db).Act(f.Kitchen.Id, foreign.Id, KitchenAction.Confirm, null, null));
                Assert.AreEqual("BR15", ex.Rule);
                f.AssertUnchanged(foreign.Id, OrderStatus.Placed);
            }
        }

        [TestMethod]
        public void Act_StartPreparingOnPlaced_ThrowsBR15WithoutHistory()
        {
            AssertWrongState(OrderStatus.Placed, KitchenAction.StartPreparing);
        }

        [TestMethod]
        public void Act_MarkReadyOnConfirmed_ThrowsBR15WithoutHistory()
        {
            AssertWrongState(OrderStatus.Confirmed, KitchenAction.MarkReady);
        }

        [TestMethod]
        public void Act_ConfirmOnPreparing_ThrowsBR15WithoutHistory()
        {
            AssertWrongState(OrderStatus.Preparing, KitchenAction.Confirm);
        }

        [TestMethod]
        public void Act_ActionOnDeliveredOrder_ThrowsBR15WithoutHistory()
        {
            AssertWrongState(OrderStatus.Delivered, KitchenAction.Confirm);
        }

        [TestMethod]
        public void Act_RejectWithoutReason_ThrowsBR15AndLeavesOrderUnchanged()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.Placed);
                var service = new KitchenBoardService(f.Db);
                foreach (string reason in new string[] { null, "", "   " })
                {
                    string r = reason;
                    var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                        service.Act(f.Kitchen.Id, order.Id, KitchenAction.Reject, r, null));
                    Assert.AreEqual("BR15", ex.Rule);
                }
                f.AssertUnchanged(order.Id, OrderStatus.Placed);
                using (var db = new SaigonBiteDbContext())
                    Assert.AreEqual("Paid", db.Orders.Single(o => o.Id == order.Id).PaymentStatus);
            }
        }

        [TestMethod]
        public void Act_RejectPlacedPaidMoMoOrder_CancelsRefundsAndRecordsReason()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.Placed, PaymentMethod.MoMo, "Paid");
                Order result = new KitchenBoardService(f.Db)
                    .Act(f.Kitchen.Id, order.Id, KitchenAction.Reject, "Out of ingredients", f.RowVersion(order.Id));
                Assert.AreEqual(OrderStatus.Cancelled, result.Status);

                using (var db = new SaigonBiteDbContext())
                {
                    Order stored = db.Orders.Single(o => o.Id == order.Id);
                    Assert.AreEqual(OrderStatus.Cancelled, stored.Status);
                    Assert.AreEqual("Refunded", stored.PaymentStatus);

                    OrderStatusHistory h = db.OrderStatusHistories.Single(x => x.OrderId == order.Id);
                    Assert.AreEqual(f.Kitchen.Id, h.ActorId);
                    Assert.AreEqual(OrderStatus.Placed, h.FromStatus);
                    Assert.AreEqual(OrderStatus.Cancelled, h.ToStatus);
                    Assert.AreEqual("Out of ingredients", h.Reason);

                    PaymentRecord refund = db.PaymentRecords.Single(p => p.OrderId == order.Id && p.Operation == "Refund");
                    Assert.AreEqual(order.PayableTotal, refund.Amount);
                }

                Assert.IsFalse(new KitchenBoardService(f.Db).GetBoard(f.Kitchen.Id).Orders.Any(o => o.OrderId == order.Id));
            }
        }

        [TestMethod]
        public void Act_RejectConfirmedOrder_ThrowsBR15AndKeepsPayment()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.Confirmed);
                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    new KitchenBoardService(f.Db).Act(f.Kitchen.Id, order.Id, KitchenAction.Reject, "Changed my mind", null));
                Assert.AreEqual("BR15", ex.Rule);
                f.AssertUnchanged(order.Id, OrderStatus.Confirmed);
                using (var db = new SaigonBiteDbContext())
                    Assert.AreEqual("Paid", db.Orders.Single(o => o.Id == order.Id).PaymentStatus);
            }
        }

        [TestMethod]
        public void Act_StaleRowVersion_ThrowsConcurrencyAndChangesNothing()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.Placed);
                byte[] stale = f.RowVersion(order.Id);
                var service = new KitchenBoardService(f.Db);

                service.Act(f.Kitchen.Id, order.Id, KitchenAction.Confirm, null, stale);
                Assert.IsFalse(stale.SequenceEqual(f.RowVersion(order.Id)));

                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    service.Act(f.Kitchen.Id, order.Id, KitchenAction.StartPreparing, null, stale));
                Assert.AreEqual("CONCURRENCY", ex.Rule);

                f.AssertUnchanged(order.Id, OrderStatus.Confirmed, expectedHistoryRows: 1);
            }
        }

        [TestMethod]
        public void Act_UnknownOrder_ThrowsNotFound()
        {
            using (var f = new Fixture())
            {
                int missing = (f.Db.Orders.Max(o => (int?)o.Id) ?? 0) + 1000;
                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    new KitchenBoardService(f.Db).Act(f.Kitchen.Id, missing, KitchenAction.Confirm, null, null));
                Assert.AreEqual("NOT_FOUND", ex.Rule);
            }
        }

        private static void AssertWrongState(OrderStatus initial, KitchenAction action)
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(initial);
                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    new KitchenBoardService(f.Db).Act(f.Kitchen.Id, order.Id, action, "reason", f.RowVersion(order.Id)));
                Assert.AreEqual("BR15", ex.Rule);
                f.AssertUnchanged(order.Id, initial);
            }
        }

        private sealed class Fixture : IDisposable
        {
            private readonly string _suffix = Guid.NewGuid().ToString("N").Substring(0, 12);
            private readonly string _codePrefix;
            private int _orderCounter;
            private readonly Brand _brand;
            private readonly MenuItem _item;

            public SaigonBiteDbContext Db { get; private set; }
            public Hub Hub { get; private set; }
            public Hub OtherHub { get; private set; }
            public User Customer { get; private set; }
            public User Admin { get; private set; }
            public User Kitchen { get; private set; }
            public User OtherKitchen { get; private set; }
            public User InactiveKitchen { get; private set; }
            public User ProfilelessKitchen { get; private set; }
            public string ItemName { get { return _item.Name; } }

            public Fixture()
            {
                Db = new SaigonBiteDbContext();
                _codePrefix = "SB-K-" + _suffix + "-";
                _brand = new Brand { Name = "Kitchen test " + _suffix, Category = "Test" };
                Hub = new Hub { Name = "Kitchen test hub " + _suffix, Address = "Test kitchen address", District = "UC03 test", MaxCapacity = 10, IsActive = true };
                OtherHub = new Hub { Name = "Kitchen test other hub " + _suffix, Address = "Other test address", District = "UC03 test", MaxCapacity = 10, IsActive = true };
                Db.Brands.Add(_brand);
                Db.Hubs.Add(Hub);
                Db.Hubs.Add(OtherHub);
                Db.SaveChanges();

                _item = new MenuItem
                {
                    BrandId = _brand.Id,
                    Name = "Kitchen test item " + _suffix,
                    Price = 100000m,
                    IsAvailable = true
                };
                Customer = NewUser("customer", ActorRole.Customer, true, CustomerType.Local);
                Admin = NewUser("admin", ActorRole.Admin, true);
                Kitchen = NewUser("kitchen", ActorRole.KitchenStaff, true);
                OtherKitchen = NewUser("otherkitchen", ActorRole.KitchenStaff, true);
                InactiveKitchen = NewUser("inactivekitchen", ActorRole.KitchenStaff, false);
                ProfilelessKitchen = NewUser("noprofile", ActorRole.KitchenStaff, true);
                Db.MenuItems.Add(_item);
                Db.Users.Add(Customer);
                Db.Users.Add(Admin);
                Db.Users.Add(Kitchen);
                Db.Users.Add(OtherKitchen);
                Db.Users.Add(InactiveKitchen);
                Db.Users.Add(ProfilelessKitchen);
                Db.SaveChanges();

                Db.StaffProfiles.Add(new StaffProfile { UserId = Kitchen.Id, HubId = Hub.Id });
                Db.StaffProfiles.Add(new StaffProfile { UserId = OtherKitchen.Id, HubId = OtherHub.Id });
                Db.StaffProfiles.Add(new StaffProfile { UserId = InactiveKitchen.Id, HubId = Hub.Id });
                Db.SaveChanges();
            }

            private User NewUser(string name, ActorRole role, bool active, CustomerType? customerType = null)
            {
                return new User
                {
                    Identifier = name + "." + _suffix + "@saigonbite.test",
                    PasswordHash = "test-only-not-for-login",
                    FullName = name + " test actor",
                    Role = role,
                    CustomerType = customerType,
                    IsActive = active
                };
            }

            public Order CreateOrder(OrderStatus status, PaymentMethod method = PaymentMethod.MoMo,
                string paymentStatus = "Paid", DateTimeOffset? placedAt = null, int? hubId = null)
            {
                _orderCounter++;
                var order = new Order
                {
                    OrderCode = _codePrefix + _orderCounter,
                    CustomerId = Customer.Id,
                    HubId = hubId ?? Hub.Id,
                    Status = status,
                    DeliveryAddress = "24 Nguyen Hue, District 1, HCMC " + _orderCounter,
                    DistanceKm = 3.2m,
                    PlacedAt = placedAt ?? new DateTimeOffset(2026, 11, 25, 14, 0, 0, TimeSpan.FromHours(7)).AddMinutes(_orderCounter),
                    HubLoadSnapshot = 0.20m,
                    HeavyRainSnapshot = false,
                    Subtotal = 200000m,
                    QuietDiscount = 20000m,
                    FoodTotal = 180000m,
                    DeliveryFee = 25000m,
                    RawTotal = 205000m,
                    PayableTotal = 205000m,
                    PaymentMethod = method,
                    PaymentStatus = paymentStatus
                };
                order.Lines.Add(new OrderLine
                {
                    MenuItemId = _item.Id,
                    Quantity = 2,
                    UnitPriceSnapshot = 100000m,
                    ItemNameSnapshot = _item.Name,
                    CategorySnapshot = "Test"
                });
                Db.Orders.Add(order);
                Db.SaveChanges();
                return order;
            }

            public byte[] RowVersion(int orderId)
            {
                using (var db = new SaigonBiteDbContext())
                    return db.Orders.AsNoTracking().Single(o => o.Id == orderId).RowVersion;
            }

            public void AssertUnchanged(int orderId, OrderStatus expectedStatus, int expectedHistoryRows = 0)
            {
                using (var db = new SaigonBiteDbContext())
                {
                    Assert.AreEqual(expectedStatus, db.Orders.AsNoTracking().Single(o => o.Id == orderId).Status);
                    Assert.AreEqual(expectedHistoryRows, db.OrderStatusHistories.Count(h => h.OrderId == orderId));
                    Assert.AreEqual(0, db.PaymentRecords.Count(p => p.OrderId == orderId && p.Operation == "Refund"));
                }
            }

            public void Dispose()
            {
                try
                {
                    var orders = Db.Orders.Where(o => o.OrderCode.StartsWith(_codePrefix)).ToList();
                    foreach (Order order in orders)
                    {
                        Db.Set<OrderStatusHistory>().RemoveRange(Db.OrderStatusHistories.Where(h => h.OrderId == order.Id));
                        Db.Set<PaymentRecord>().RemoveRange(Db.PaymentRecords.Where(p => p.OrderId == order.Id));
                        Db.Set<OrderLine>().RemoveRange(Db.OrderLines.Where(l => l.OrderId == order.Id));
                        Db.Orders.Remove(order);
                    }
                    Db.SaveChanges();
                    int[] hubIds = { Hub.Id, OtherHub.Id };
                    Db.Set<StaffProfile>().RemoveRange(Db.StaffProfiles.Where(s => hubIds.Contains(s.HubId)));
                    Db.MenuItems.Remove(_item);
                    Db.Set<User>().RemoveRange(new[] { Customer, Admin, Kitchen, OtherKitchen, InactiveKitchen, ProfilelessKitchen });
                    Db.Hubs.Remove(Hub);
                    Db.Hubs.Remove(OtherHub);
                    Db.Brands.Remove(_brand);
                    Db.SaveChanges();
                }
                finally
                {
                    Db.Dispose();
                }
            }
        }
    }
}
