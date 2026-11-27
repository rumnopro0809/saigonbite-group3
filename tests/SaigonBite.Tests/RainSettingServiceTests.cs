using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonBite.Application.Hubs;
using SaigonBite.Application.Orders;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;
using SaigonBite.Infrastructure.Services;

namespace SaigonBite.Tests
{
    /// <summary>UC03 heavy-rain flag: FR16, BR5, BR17 (TC16). Every test restores the global flag it found.</summary>
    [TestClass]
    public class RainSettingServiceTests
    {
        [ClassInitialize]
        public static void EnsureDatabaseMigrated(TestContext context)
        {
            Uc03TestData.MigrateDatabase();
        }

        private static int SettingRows()
        {
            using (var db = new SaigonBiteDbContext()) return db.SystemSettings.Count(s => true);
        }

        private static ServiceResult<RainFlagDto> Set(bool on, string rowVersion)
        {
            using (var db = new SaigonBiteDbContext()) return new RainSettingService(db).Set(on, rowVersion);
        }

        private static RainFlagDto Get()
        {
            using (var db = new SaigonBiteDbContext()) return new RainSettingService(db).Get();
        }

        [TestMethod]
        public void Get_CreatesDefaultOffRowWhenMissing_AndReturnsExistingOtherwise()
        {
            bool original = Uc03TestData.GetRainFlag();
            try
            {
                using (var db = new SaigonBiteDbContext())
                {
                    db.Set<SystemSetting>().RemoveRange(db.SystemSettings.Where(s => true).ToList());
                    db.SaveChanges();
                }

                RainFlagDto created = Get();
                Assert.IsFalse(created.IsHeavyRainOn);
                Assert.IsFalse(string.IsNullOrEmpty(created.RowVersion));
                Assert.AreEqual(1, SettingRows());

                Assert.AreEqual(ResultStatus.Ok, Set(true, created.RowVersion).Status);
                Assert.IsTrue(Get().IsHeavyRainOn);
                Assert.AreEqual(1, SettingRows());
            }
            finally
            {
                Uc03TestData.SetRainFlag(original);
            }
        }

        [TestMethod]
        public void Set_TogglesFlagWithValidRowVersion_AndPersistsIt()
        {
            bool original = Uc03TestData.GetRainFlag();
            try
            {
                Uc03TestData.SetRainFlag(false);
                RainFlagDto before = Get();

                var on = Set(true, before.RowVersion);
                Assert.AreEqual(ResultStatus.Ok, on.Status);
                Assert.IsTrue(on.Value.IsHeavyRainOn);
                Assert.AreNotEqual(before.RowVersion, on.Value.RowVersion);
                Assert.IsTrue(Get().IsHeavyRainOn);

                var off = Set(false, on.Value.RowVersion);
                Assert.AreEqual(ResultStatus.Ok, off.Status);
                Assert.IsFalse(off.Value.IsHeavyRainOn);
                Assert.IsFalse(Get().IsHeavyRainOn);
            }
            finally
            {
                Uc03TestData.SetRainFlag(original);
            }
        }

        [TestMethod]
        public void Set_RejectsStaleRowVersionWithConflict_AndKeepsFlag()
        {
            bool original = Uc03TestData.GetRainFlag();
            try
            {
                Uc03TestData.SetRainFlag(false);
                string stale = Get().RowVersion;
                var first = Set(true, stale);                       // another admin saves first
                Assert.AreEqual(ResultStatus.Ok, first.Status);

                var result = Set(false, stale);
                Uc03TestData.AssertFails(result, ResultStatus.Conflict);
                Assert.IsTrue(Get().IsHeavyRainOn);
                Assert.AreEqual(first.Value.RowVersion, Get().RowVersion);
            }
            finally
            {
                Uc03TestData.SetRainFlag(original);
            }
        }

        [TestMethod]
        public void Set_RejectsMissingOrMalformedRowVersion_AndKeepsFlag()
        {
            bool original = Uc03TestData.GetRainFlag();
            try
            {
                Uc03TestData.SetRainFlag(false);
                RainFlagDto before = Get();
                foreach (string bad in new string[] { null, "", "not-base64!!" })
                    Uc03TestData.AssertFails(Set(true, bad), ResultStatus.Invalid, "RowVersion");

                Assert.IsFalse(Set(true, "AAAA").IsSuccess);        // well-formed but not a real rowversion

                RainFlagDto after = Get();
                Assert.IsFalse(after.IsHeavyRainOn);
                Assert.AreEqual(before.RowVersion, after.RowVersion);
            }
            finally
            {
                Uc03TestData.SetRainFlag(original);
            }
        }

        [TestMethod]
        public void ExistingOrderKeepsHeavyRainSnapshotAfterFlagChanges_WhileNewQuoteUsesNewFlag_TC16()
        {
            bool original = Uc03TestData.GetRainFlag();
            try
            {
                using (var f = new Uc03Fixture())
                {
                    Brand brand = f.NewBrand();
                    Hub hub = f.NewHub(20, true, "rain", brand.Id);
                    MenuItem item = f.NewItem(brand.Id);
                    var draft = new CreateOrderRequest
                    {
                        CustomerId = f.Customer.Id,
                        PaymentMethod = PaymentMethod.MoMo,
                        HubId = hub.Id,
                        DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                        DistanceKm = 3m,
                        Items = new List<OrderItemRequest> { new OrderItemRequest { MenuItemId = item.Id, Quantity = 2 } }
                    };

                    Uc03TestData.SetRainFlag(false);
                    int dryOrderId = PlaceOrder(f, draft, "dry-" + f.Suffix);
                    Assert.IsFalse(f.Read(db => db.Orders.Single(o => o.Id == dryOrderId).HeavyRainSnapshot));

                    Uc03TestData.SetRainFlag(true);
                    Assert.IsFalse(f.Read(db => db.Orders.Single(o => o.Id == dryOrderId).HeavyRainSnapshot),
                        "turning rain on must not change an existing order");
                    OrderQuoteResult rainQuote = QuoteDraft(f, draft);
                    Assert.IsTrue(rainQuote.IsHeavyRainOn);
                    Assert.IsTrue(rainQuote.Breakdown.RainFee > 0m);
                    int wetOrderId = PlaceOrder(f, draft, "wet-" + f.Suffix);
                    Assert.IsTrue(f.Read(db => db.Orders.Single(o => o.Id == wetOrderId).HeavyRainSnapshot));

                    Uc03TestData.SetRainFlag(false);
                    Assert.IsTrue(f.Read(db => db.Orders.Single(o => o.Id == wetOrderId).HeavyRainSnapshot),
                        "turning rain off must not change an existing order");
                    Assert.IsFalse(f.Read(db => db.Orders.Single(o => o.Id == dryOrderId).HeavyRainSnapshot));
                    OrderQuoteResult dryQuote = QuoteDraft(f, draft);
                    Assert.IsFalse(dryQuote.IsHeavyRainOn);
                    Assert.AreEqual(0m, dryQuote.Breakdown.RainFee);
                }
            }
            finally
            {
                Uc03TestData.SetRainFlag(original);
            }
        }

        private static OrderQuoteResult QuoteDraft(Uc03Fixture f, CreateOrderRequest draft)
        {
            draft.RequestKey = null;
            using (var db = new SaigonBiteDbContext()) return new OrderService(db).Quote(draft);
        }

        private static int PlaceOrder(Uc03Fixture f, CreateOrderRequest draft, string requestKey)
        {
            using (var db = new SaigonBiteDbContext())
            {
                draft.RequestKey = requestKey;
                return new OrderService(db).PlaceOrder(draft).Id;
            }
        }
    }
}
