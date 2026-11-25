using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonBite.Application;
using SaigonBite.Application.Orders;
using SaigonBite.Domain;
using SaigonBite.Infrastructure;
using SaigonBite.Infrastructure.Migrations;

namespace SaigonBite.Tests
{
    /// <summary>Shared helpers for the UC03 (hubs, capacity, rain flag, utilization report) tests.</summary>
    internal static class Uc03TestData
    {
        public static void MigrateDatabase()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AppDbContext, Configuration>());
            using (var db = new AppDbContext()) db.Database.Initialize(force: false);
        }

        /// <summary>Vietnam-local (UTC+7) timestamp in March 2001, far from any seeded data.</summary>
        public static DateTimeOffset Vn(int day, int hour, int minute, int second)
        {
            return new DateTimeOffset(2001, 3, day, hour, minute, second, TimeSpan.FromHours(7));
        }

        public static bool GetRainFlag()
        {
            using (var db = new AppDbContext()) return new RainSettingService(db).Get().IsHeavyRainOn;
        }

        /// <summary>Forces the global rain flag to <paramref name="value"/> (used to set up and to restore).</summary>
        public static void SetRainFlag(bool value)
        {
            using (var db = new AppDbContext())
            {
                var service = new RainSettingService(db);
                RainFlagDto current = service.Get();
                if (current.IsHeavyRainOn == value) return;
                var result = service.Set(value, current.RowVersion);
                Assert.IsTrue(result.IsSuccess);
            }
        }

        public static void AssertFails<T>(ServiceResult<T> result, ResultStatus status, string field = null)
        {
            Assert.AreEqual(status, result.Status);
            Assert.IsFalse(result.IsSuccess);
            if (field != null)
                Assert.IsTrue(result.Errors.Any(e => e.Field == field), "Expected an error on field " + field);
        }

        public static void AssertBusinessRule(string rule, Action action)
        {
            Assert.AreEqual(rule, Assert.ThrowsException<BusinessRuleException>(action).Rule);
        }
    }

    /// <summary>
    /// Self-seeding fixture. Every row it creates carries a unique suffix (hub/brand names, customer identifier),
    /// and Dispose removes only those rows, so it is safe on the shared test database.
    /// </summary>
    internal sealed class Uc03Fixture : IDisposable
    {
        private readonly List<int> _hubIds = new List<int>();
        private readonly List<int> _brandIds = new List<int>();
        private User _customer;

        public AppDbContext Db { get; }
        public string Suffix { get; } = Guid.NewGuid().ToString("N").Substring(0, 12);

        public Uc03Fixture()
        {
            Db = new AppDbContext();
        }

        public string Name(string label)
        {
            return "UC03 " + label + " " + Suffix;
        }

        public User Customer
        {
            get
            {
                if (_customer == null)
                {
                    _customer = new User
                    {
                        Identifier = "uc03-" + Suffix + "@test.local",
                        FullName = "UC03 customer",
                        PasswordHash = "test-not-a-password",
                        Role = ActorRole.Customer,
                        CustomerType = CustomerType.Local,
                        IsActive = true
                    };
                    Db.Users.Add(_customer);
                    Db.SaveChanges();
                }
                return _customer;
            }
        }

        /// <summary>Runs <paramref name="action"/> against a brand-new context so nothing is served from a tracking cache.</summary>
        public T Read<T>(Func<AppDbContext, T> action)
        {
            using (var db = new AppDbContext()) return action(db);
        }

        /// <summary>Runs one HubService call on its own context (like one web request).</summary>
        public T Hubs<T>(Func<HubService, T> action)
        {
            using (var db = new AppDbContext()) return action(new HubService(db));
        }

        public Brand NewBrand(string label = "brand", bool deleted = false)
        {
            var brand = new Brand { Name = Name(label), Category = "Food", IsDeleted = deleted };
            Db.Brands.Add(brand);
            Db.SaveChanges();
            _brandIds.Add(brand.Id);
            return brand;
        }

        public MenuItem NewItem(int brandId, decimal price = 100000m)
        {
            var item = new MenuItem { BrandId = brandId, Name = "UC03 item", Price = price, IsAvailable = true };
            Db.MenuItems.Add(item);
            Db.SaveChanges();
            return item;
        }

        public Hub NewHub(int capacity = 10, bool active = true, string label = "hub", params int[] brandIds)
        {
            var hub = new Hub { Name = Name(label), Location = "UC03 test location", Capacity = capacity, IsActive = active };
            Db.Hubs.Add(hub);
            Db.SaveChanges();
            _hubIds.Add(hub.Id);
            foreach (int brandId in brandIds)
                Db.HubBrands.Add(new HubBrand { HubId = hub.Id, BrandId = brandId });
            Db.SaveChanges();
            return hub;
        }

        public Order NewOrder(int hubId, OrderStatus status, DateTimeOffset? placedAt = null, bool quiet = false)
        {
            var order = new Order
            {
                HubId = hubId,
                CustomerId = Customer.Id,
                OrderCode = "U3-" + Guid.NewGuid().ToString("N").Substring(0, 24),
                Status = status,
                PlacedAt = placedAt ?? Uc03TestData.Vn(1, 12, 0, 0),
                QuietDiscountGranted = quiet,
                DeliveryAddress = "UC03 test address",
                PaymentStatus = "Sample"
            };
            Db.Orders.Add(order);
            Db.SaveChanges();
            return order;
        }

        public void AddOrders(int hubId, OrderStatus status, int count)
        {
            for (int i = 0; i < count; i++) NewOrder(hubId, status);
        }

        public OrderStatusHistory AddHistory(int orderId, OrderStatus from, OrderStatus to, DateTimeOffset at)
        {
            var history = new OrderStatusHistory
            {
                OrderId = orderId,
                From = from,
                FromStatus = from,
                To = to,
                ToStatus = to,
                ChangedAt = at
            };
            Db.OrderStatusHistories.Add(history);
            Db.SaveChanges();
            return history;
        }

        public HubDto FindInList(int hubId)
        {
            return Hubs(s => s.List()).SingleOrDefault(h => h.Id == hubId);
        }

        public void Dispose()
        {
            Db.Dispose();
            using (var cleanup = new AppDbContext())
            using (var transaction = cleanup.Database.BeginTransaction())
            {
                string suffix = Suffix;
                int[] hubIds = _hubIds.Concat(cleanup.Hubs.Where(h => h.Name.Contains(suffix)).Select(h => h.Id))
                    .Distinct().ToArray();
                int[] brandIds = _brandIds.Concat(cleanup.Brands.Where(b => b.Name.Contains(suffix)).Select(b => b.Id))
                    .Distinct().ToArray();
                int[] userIds = cleanup.Users.Where(u => u.Identifier == "uc03-" + suffix + "@test.local")
                    .Select(u => u.Id).ToArray();

                int[] orderIds = cleanup.Orders.Where(o => hubIds.Contains(o.HubId) || userIds.Contains(o.CustomerId))
                    .Select(o => o.Id).ToArray();
                cleanup.Set<OrderStatusHistory>().RemoveRange(cleanup.OrderStatusHistories.Where(h => orderIds.Contains(h.OrderId)));
                cleanup.Set<PaymentRecord>().RemoveRange(cleanup.PaymentRecords.Where(p => orderIds.Contains(p.OrderId)));
                cleanup.Set<OrderLine>().RemoveRange(cleanup.OrderLines.Where(l => orderIds.Contains(l.OrderId)));
                cleanup.Set<Order>().RemoveRange(cleanup.Orders.Where(o => orderIds.Contains(o.Id)));
                cleanup.SaveChanges();

                cleanup.Set<MenuItem>().RemoveRange(cleanup.MenuItems.Where(m => brandIds.Contains(m.BrandId)));
                cleanup.Set<HubBrand>().RemoveRange(cleanup.HubBrands.Where(hb => hubIds.Contains(hb.HubId) || brandIds.Contains(hb.BrandId)));
                cleanup.Set<User>().RemoveRange(cleanup.Users.Where(u => userIds.Contains(u.Id)));
                cleanup.SaveChanges();
                cleanup.Set<Hub>().RemoveRange(cleanup.Hubs.Where(h => hubIds.Contains(h.Id)));
                cleanup.Set<Brand>().RemoveRange(cleanup.Brands.Where(b => brandIds.Contains(b.Id)));
                cleanup.SaveChanges();
                transaction.Commit();
            }
        }
    }
}
