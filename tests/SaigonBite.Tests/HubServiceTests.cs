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
    /// <summary>UC03 hub management: FR14.1 to FR14.4, BR8 load rules (TC14.1 to TC14.4) and the UC01 collaboration.</summary>
    [TestClass]
    public class HubServiceTests
    {
        [ClassInitialize]
        public static void EnsureDatabaseMigrated(TestContext context)
        {
            Uc03TestData.MigrateDatabase();
        }

        private static HubInput Input(Uc03Fixture f, string label, int capacity, params int[] brandIds)
        {
            return new HubInput
            {
                Name = f.Name(label),
                Location = "12 Nguyen Hue, District 1",
                District = "District 1",
                Capacity = capacity,
                BrandIds = brandIds.ToList()
            };
        }

        private static int HubCountNamed(Uc03Fixture f, string name)
        {
            return f.Read(db => db.Hubs.Count(h => h.Name == name));
        }

        // ---------- TC14.1 create ----------

        [TestMethod]
        public void Create_PersistsHubAndBrandLinksAtomicallyAndReturnsCreated()
        {
            using (var f = new Uc03Fixture())
            {
                Brand b1 = f.NewBrand("b1");
                Brand b2 = f.NewBrand("b2");
                HubInput input = Input(f, "create", 10, b1.Id, b2.Id);

                var result = f.Hubs(s => s.Create(input));

                Assert.AreEqual(ResultStatus.Created, result.Status);
                Assert.IsTrue(result.IsSuccess);
                HubDto dto = result.Value;
                Assert.IsTrue(dto.Id > 0);
                Assert.AreEqual(10, dto.Capacity);
                Assert.IsTrue(dto.IsActive);
                Assert.AreEqual(0, dto.ActiveOrders);
                Assert.AreEqual(0, dto.LoadPercent);
                Assert.AreEqual(HubLoadLevel.Quiet, dto.LoadLevel);
                CollectionAssert.AreEquivalent(new[] { b1.Id, b2.Id }, dto.BrandIds.ToArray());
                Assert.IsFalse(string.IsNullOrEmpty(dto.RowVersion));

                Hub stored = f.Read(db => db.Hubs.Single(h => h.Id == dto.Id));
                Assert.AreEqual(input.Name, stored.Name);
                Assert.AreEqual(input.Location, stored.Address);
                Assert.AreEqual(10, stored.MaxCapacity);
                Assert.IsTrue(stored.IsActive);
                Assert.IsFalse(stored.IsDeleted);
                int[] links = f.Read(db => db.HubBrands.Where(hb => hb.HubId == dto.Id).Select(hb => hb.BrandId).ToArray());
                CollectionAssert.AreEquivalent(new[] { b1.Id, b2.Id }, links);
            }
        }

        [TestMethod]
        public void Create_RejectsZeroAndNegativeCapacityAndWritesNothing()
        {
            using (var f = new Uc03Fixture())
            {
                Brand brand = f.NewBrand();
                foreach (int capacity in new[] { 0, -1 })
                {
                    HubInput input = Input(f, "cap" + capacity, capacity, brand.Id);
                    var result = f.Hubs(s => s.Create(input));
                    Uc03TestData.AssertFails(result, ResultStatus.Invalid, "Capacity");
                    Assert.AreEqual(0, HubCountNamed(f, input.Name));
                }
                Assert.AreEqual(0, f.Read(db => db.HubBrands.Count(hb => hb.BrandId == brand.Id)));
            }
        }

        [TestMethod]
        public void Create_AcceptsCapacityOne()
        {
            using (var f = new Uc03Fixture())
            {
                var result = f.Hubs(s => s.Create(Input(f, "cap1", 1)));
                Assert.AreEqual(ResultStatus.Created, result.Status);
                Assert.AreEqual(1, result.Value.Capacity);
                Assert.AreEqual(1, f.Read(db => db.Hubs.Single(h => h.Id == result.Value.Id).MaxCapacity));
            }
        }

        [TestMethod]
        public void Create_RejectsBlankOrOverlongNameAndLocation_AndAcceptsMaximumLengths()
        {
            using (var f = new Uc03Fixture())
            {
                foreach (string blank in new[] { null, "", "   " })
                {
                    HubInput input = Input(f, "blank", 5);
                    input.Name = blank;
                    Uc03TestData.AssertFails(f.Hubs(s => s.Create(input)), ResultStatus.Invalid, "Name");

                    input = Input(f, "blank", 5);
                    input.Location = blank;
                    Uc03TestData.AssertFails(f.Hubs(s => s.Create(input)), ResultStatus.Invalid, "Location");
                }

                // Names carry the unique suffix at the end so cleanup can find them.
                string longName = new string('N', HubValidator.NameMaxLength + 1 - f.Suffix.Length) + f.Suffix;
                HubInput tooLongName = Input(f, "x", 5);
                tooLongName.Name = longName;
                Uc03TestData.AssertFails(f.Hubs(s => s.Create(tooLongName)), ResultStatus.Invalid, "Name");
                Assert.AreEqual(0, HubCountNamed(f, longName));

                HubInput tooLongLocation = Input(f, "loc", 5);
                tooLongLocation.Location = new string('L', HubValidator.LocationMaxLength + 1);
                Uc03TestData.AssertFails(f.Hubs(s => s.Create(tooLongLocation)), ResultStatus.Invalid, "Location");
                Assert.AreEqual(0, HubCountNamed(f, tooLongLocation.Name));

                HubInput maxLengths = Input(f, "x", 5);
                maxLengths.Name = new string('N', HubValidator.NameMaxLength - f.Suffix.Length) + f.Suffix;
                maxLengths.Location = new string('L', HubValidator.LocationMaxLength);
                var ok = f.Hubs(s => s.Create(maxLengths));
                Assert.AreEqual(ResultStatus.Created, ok.Status);
                Assert.AreEqual(HubValidator.NameMaxLength, f.Read(db => db.Hubs.Single(h => h.Id == ok.Value.Id).Name.Length));
            }
        }

        [TestMethod]
        public void Create_RejectsDuplicateUnknownAndSoftDeletedBrandsWithoutPartialHub()
        {
            using (var f = new Uc03Fixture())
            {
                Brand good = f.NewBrand("good");
                Brand deleted = f.NewBrand("deleted", deleted: true);

                HubInput duplicate = Input(f, "dup", 5, good.Id, good.Id);
                Uc03TestData.AssertFails(f.Hubs(s => s.Create(duplicate)), ResultStatus.Invalid, "BrandIds");
                Assert.AreEqual(0, HubCountNamed(f, duplicate.Name));

                HubInput unknown = Input(f, "unknown", 5, good.Id, int.MaxValue);
                Uc03TestData.AssertFails(f.Hubs(s => s.Create(unknown)), ResultStatus.Invalid, "BrandIds");
                Assert.AreEqual(0, HubCountNamed(f, unknown.Name));

                HubInput softDeleted = Input(f, "softdel", 5, good.Id, deleted.Id);
                Uc03TestData.AssertFails(f.Hubs(s => s.Create(softDeleted)), ResultStatus.Invalid, "BrandIds");
                Assert.AreEqual(0, HubCountNamed(f, softDeleted.Name));

                Assert.AreEqual(0, f.Read(db => db.HubBrands.Count(hb => hb.BrandId == good.Id || hb.BrandId == deleted.Id)));
            }
        }

        // ---------- TC14.2 list/get load ----------

        [TestMethod]
        public void ListAndGet_LoadCountsOnlyConfirmedAndPreparing_AndClassifiesThresholds()
        {
            using (var f = new Uc03Fixture())
            {
                Hub hub = f.NewHub(capacity: 10);
                // Orders that must never count towards load (BR8).
                f.AddOrders(hub.Id, OrderStatus.Placed, 2);
                f.AddOrders(hub.Id, OrderStatus.Ready, 2);
                f.AddOrders(hub.Id, OrderStatus.Cancelled, 2);
                f.AddOrders(hub.Id, OrderStatus.OutForDelivery, 1);
                f.AddOrders(hub.Id, OrderStatus.Delivered, 1);
                f.AddOrders(hub.Id, OrderStatus.Failed, 1);

                AssertLoad(f, hub.Id, 0, 0, HubLoadLevel.Quiet);

                f.NewOrder(hub.Id, OrderStatus.Confirmed);
                f.NewOrder(hub.Id, OrderStatus.Preparing);
                AssertLoad(f, hub.Id, 2, 20, HubLoadLevel.Quiet);

                f.NewOrder(hub.Id, OrderStatus.Confirmed);
                AssertLoad(f, hub.Id, 3, 30, HubLoadLevel.Normal);   // exactly 30% is Normal, not Quiet

                f.AddOrders(hub.Id, OrderStatus.Preparing, 5);
                AssertLoad(f, hub.Id, 8, 80, HubLoadLevel.Normal);

                f.NewOrder(hub.Id, OrderStatus.Confirmed);
                AssertLoad(f, hub.Id, 9, 90, HubLoadLevel.Full);     // exactly 90% is Full
            }
        }

        private static void AssertLoad(Uc03Fixture f, int hubId, int active, int percent, HubLoadLevel level)
        {
            HubDto fromGet = f.Hubs(s => s.Get(hubId)).Value;
            HubDto fromList = f.FindInList(hubId);
            Assert.IsNotNull(fromList);
            foreach (HubDto dto in new[] { fromGet, fromList })
            {
                Assert.AreEqual(active, dto.ActiveOrders);
                Assert.AreEqual(percent, dto.LoadPercent);
                Assert.AreEqual(level, dto.LoadLevel);
                Assert.AreEqual(active / 10m, dto.Load);
            }
        }

        // ---------- TC14.3 update ----------

        [TestMethod]
        public void Update_ChangesFieldsAndBrands_AndKeepsExistingOrdersAndSnapshots()
        {
            using (var f = new Uc03Fixture())
            {
                Brand b1 = f.NewBrand("b1");
                Brand b2 = f.NewBrand("b2");
                Brand b3 = f.NewBrand("b3");
                Hub hub = f.NewHub(10, true, "update", b1.Id, b2.Id);
                Order confirmed = f.NewOrder(hub.Id, OrderStatus.Confirmed);
                Order placed = f.NewOrder(hub.Id, OrderStatus.Placed);
                // Give one order distinctive snapshots through a normal update of the row.
                using (var db = new SaigonBiteDbContext())
                {
                    Order o = db.Orders.Single(x => x.Id == placed.Id);
                    o.HubLoadSnapshot = 0.35m;
                    o.HeavyRainSnapshot = true;
                    o.PayableTotal = 123456m;
                    db.SaveChanges();
                }
                var before = f.Read(db => db.Orders.Where(o => o.HubId == hub.Id).ToList()
                    .ToDictionary(o => o.Id, o => new { o.Status, o.HubLoadSnapshot, o.HeavyRainSnapshot, o.PayableTotal, o.OrderCode }));

                HubDto current = f.Hubs(s => s.Get(hub.Id)).Value;
                var input = new HubInput
                {
                    Name = f.Name("renamed"),
                    Location = "99 Le Loi, District 1",
                    District = "District 1",
                    Capacity = 20,
                    BrandIds = new List<int> { b2.Id, b3.Id },
                    RowVersion = current.RowVersion
                };
                var result = f.Hubs(s => s.Update(hub.Id, input));

                Assert.AreEqual(ResultStatus.Ok, result.Status);
                Assert.AreEqual(input.Name, result.Value.Name);
                Assert.AreEqual(input.Location, result.Value.Location);
                Assert.AreEqual(20, result.Value.Capacity);
                CollectionAssert.AreEquivalent(new[] { b2.Id, b3.Id }, result.Value.BrandIds.ToArray());
                Assert.AreNotEqual(current.RowVersion, result.Value.RowVersion);

                Hub stored = f.Read(db => db.Hubs.Single(h => h.Id == hub.Id));
                Assert.AreEqual(input.Name, stored.Name);
                Assert.AreEqual(20, stored.MaxCapacity);
                Assert.IsFalse(stored.IsDeleted);
                int[] links = f.Read(db => db.HubBrands.Where(hb => hb.HubId == hub.Id).Select(hb => hb.BrandId).ToArray());
                CollectionAssert.AreEquivalent(new[] { b2.Id, b3.Id }, links);

                var after = f.Read(db => db.Orders.Where(o => o.HubId == hub.Id).ToList()
                    .ToDictionary(o => o.Id, o => new { o.Status, o.HubLoadSnapshot, o.HeavyRainSnapshot, o.PayableTotal, o.OrderCode }));
                Assert.AreEqual(before.Count, after.Count);
                foreach (int id in before.Keys) Assert.AreEqual(before[id], after[id]);
                Assert.AreEqual(OrderStatus.Confirmed, after[confirmed.Id].Status);
                Assert.AreEqual(0.35m, after[placed.Id].HubLoadSnapshot);
                Assert.IsTrue(after[placed.Id].HeavyRainSnapshot);
                Assert.AreEqual(123456m, after[placed.Id].PayableTotal);
            }
        }

        [TestMethod]
        public void Update_LoweringCapacityBelowActiveOrdersGivesLoadAboveFullWithoutChangingOrderStatus()
        {
            using (var f = new Uc03Fixture())
            {
                Hub hub = f.NewHub(10);
                f.AddOrders(hub.Id, OrderStatus.Confirmed, 3);
                f.AddOrders(hub.Id, OrderStatus.Preparing, 1);   // 4 active
                int[] orderIds = f.Read(db => db.Orders.Where(o => o.HubId == hub.Id).Select(o => o.Id).ToArray());

                HubDto current = f.Hubs(s => s.Get(hub.Id)).Value;
                var input = new HubInput
                {
                    Name = current.Name,
                    Location = current.Location,
                    District = current.District,
                    Capacity = 2,
                    BrandIds = new List<int>(),
                    RowVersion = current.RowVersion
                };
                var result = f.Hubs(s => s.Update(hub.Id, input));

                Assert.AreEqual(ResultStatus.Ok, result.Status);
                Assert.AreEqual(4, result.Value.ActiveOrders);
                Assert.AreEqual(2m, result.Value.Load);
                Assert.AreEqual(200, result.Value.LoadPercent);
                Assert.AreEqual(HubLoadLevel.Full, result.Value.LoadLevel);

                var statuses = f.Read(db => db.Orders.Where(o => o.HubId == hub.Id).OrderBy(o => o.Id).Select(o => o.Status).ToList());
                Assert.AreEqual(4, statuses.Count);
                Assert.AreEqual(3, statuses.Count(s => s == OrderStatus.Confirmed));
                Assert.AreEqual(1, statuses.Count(s => s == OrderStatus.Preparing));
                CollectionAssert.AreEquivalent(orderIds, f.Read(db => db.Orders.Where(o => o.HubId == hub.Id).Select(o => o.Id).ToArray()));
            }
        }

        [TestMethod]
        public void Update_RejectsInvalidInputAndMissingHub_ChangingNothing()
        {
            using (var f = new Uc03Fixture())
            {
                Hub hub = f.NewHub(10);
                HubDto current = f.Hubs(s => s.Get(hub.Id)).Value;

                var badCapacity = new HubInput
                {
                    Name = f.Name("changed"), Location = "elsewhere", District = "District 1", Capacity = 0,
                    BrandIds = new List<int>(), RowVersion = current.RowVersion
                };
                Uc03TestData.AssertFails(f.Hubs(s => s.Update(hub.Id, badCapacity)), ResultStatus.Invalid, "Capacity");

                var unknownBrand = new HubInput
                {
                    Name = f.Name("changed"), Location = "elsewhere", District = "District 1", Capacity = 5,
                    BrandIds = new List<int> { int.MaxValue }, RowVersion = current.RowVersion
                };
                Uc03TestData.AssertFails(f.Hubs(s => s.Update(hub.Id, unknownBrand)), ResultStatus.Invalid, "BrandIds");

                Hub stored = f.Read(db => db.Hubs.Single(h => h.Id == hub.Id));
                Assert.AreEqual(current.Name, stored.Name);
                Assert.AreEqual(10, stored.MaxCapacity);

                var missing = new HubInput
                {
                    Name = f.Name("x"), Location = "x", District = "District 1", Capacity = 5, RowVersion = current.RowVersion
                };
                Uc03TestData.AssertFails(f.Hubs(s => s.Update(int.MaxValue, missing)), ResultStatus.NotFound);
            }
        }

        // ---------- optimistic concurrency ----------

        [TestMethod]
        public void StaleRowVersion_IsRejectedForUpdateSetActiveAndDelete_AndChangesNothing()
        {
            using (var f = new Uc03Fixture())
            {
                Hub hub = f.NewHub(10);
                string stale = f.Hubs(s => s.Get(hub.Id)).Value.RowVersion;

                // Another user saves first, so `stale` no longer matches.
                var first = new HubInput
                {
                    Name = f.Name("first"), Location = "first", District = "District 1", Capacity = 11,
                    BrandIds = new List<int>(), RowVersion = stale
                };
                var firstResult = f.Hubs(s => s.Update(hub.Id, first));
                Assert.AreEqual(ResultStatus.Ok, firstResult.Status);
                Assert.AreNotEqual(stale, firstResult.Value.RowVersion);

                var second = new HubInput
                {
                    Name = f.Name("second"), Location = "second", District = "District 1", Capacity = 12,
                    BrandIds = new List<int>(), RowVersion = stale
                };
                Uc03TestData.AssertFails(f.Hubs(s => s.Update(hub.Id, second)), ResultStatus.Conflict);
                Uc03TestData.AssertFails(f.Hubs(s => s.SetActive(hub.Id, false, stale)), ResultStatus.Conflict);
                Uc03TestData.AssertFails(f.Hubs(s => s.Delete(hub.Id, stale)), ResultStatus.Conflict);

                Hub stored = f.Read(db => db.Hubs.Single(h => h.Id == hub.Id));
                Assert.AreEqual(first.Name, stored.Name);
                Assert.AreEqual(11, stored.MaxCapacity);
                Assert.IsTrue(stored.IsActive);
                Assert.IsFalse(stored.IsDeleted);
            }
        }

        [TestMethod]
        public void MissingOrMalformedRowVersion_IsRejectedForUpdateSetActiveAndDelete_AndChangesNothing()
        {
            using (var f = new Uc03Fixture())
            {
                Hub hub = f.NewHub(10);
                string good = f.Hubs(s => s.Get(hub.Id)).Value.RowVersion;

                foreach (string bad in new string[] { null, "", "not-base64!!" })
                {
                    var input = new HubInput
                    {
                        Name = f.Name("bad"), Location = "bad", District = "District 1", Capacity = 99,
                        BrandIds = new List<int>(), RowVersion = bad
                    };
                    Uc03TestData.AssertFails(f.Hubs(s => s.Update(hub.Id, input)), ResultStatus.Invalid, "RowVersion");
                    Uc03TestData.AssertFails(f.Hubs(s => s.SetActive(hub.Id, false, bad)), ResultStatus.Invalid, "RowVersion");
                    Uc03TestData.AssertFails(f.Hubs(s => s.Delete(hub.Id, bad)), ResultStatus.Invalid, "RowVersion");
                }

                // Well-formed base64 that is not a real rowversion must never succeed.
                var wrongLength = new HubInput
                {
                    Name = f.Name("bad"), Location = "bad", District = "District 1", Capacity = 99,
                    BrandIds = new List<int>(), RowVersion = "AAAA"
                };
                Assert.IsFalse(f.Hubs(s => s.Update(hub.Id, wrongLength)).IsSuccess);
                Assert.IsFalse(f.Hubs(s => s.SetActive(hub.Id, false, "AAAA")).IsSuccess);
                Assert.IsFalse(f.Hubs(s => s.Delete(hub.Id, "AAAA")).IsSuccess);

                Hub stored = f.Read(db => db.Hubs.Single(h => h.Id == hub.Id));
                Assert.AreEqual(f.Name("hub"), stored.Name);
                Assert.AreEqual(10, stored.MaxCapacity);
                Assert.IsTrue(stored.IsActive);
                Assert.IsFalse(stored.IsDeleted);
                Assert.AreEqual(good, f.Hubs(s => s.Get(hub.Id)).Value.RowVersion);
            }
        }

        // ---------- FR14.4 deactivate / soft delete ----------

        [TestMethod]
        public void SetActive_False_KeepsHubAndExistingOrders_AndCanBeReactivated()
        {
            using (var f = new Uc03Fixture())
            {
                Hub hub = f.NewHub(10);
                f.AddOrders(hub.Id, OrderStatus.Confirmed, 2);
                f.NewOrder(hub.Id, OrderStatus.Preparing);
                HubDto current = f.Hubs(s => s.Get(hub.Id)).Value;

                var off = f.Hubs(s => s.SetActive(hub.Id, false, current.RowVersion));

                Assert.AreEqual(ResultStatus.Ok, off.Status);
                Assert.IsFalse(off.Value.IsActive);
                Assert.AreEqual(3, off.Value.ActiveOrders);
                HubDto listed = f.FindInList(hub.Id);
                Assert.IsNotNull(listed);
                Assert.IsFalse(listed.IsActive);
                Hub stored = f.Read(db => db.Hubs.Single(h => h.Id == hub.Id));
                Assert.IsFalse(stored.IsActive);
                Assert.IsFalse(stored.IsDeleted);
                Assert.AreEqual(3, f.Read(db => db.Orders.Count(o => o.HubId == hub.Id &&
                    (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Preparing))));

                var on = f.Hubs(s => s.SetActive(hub.Id, true, off.Value.RowVersion));
                Assert.AreEqual(ResultStatus.Ok, on.Status);
                Assert.IsTrue(on.Value.IsActive);
            }
        }

        [TestMethod]
        public void Delete_SoftDeletesHubKeepingRowOrdersAndHistory_AndSecondDeleteIsNotFound()
        {
            using (var f = new Uc03Fixture())
            {
                Hub hub = f.NewHub(10);
                Order order = f.NewOrder(hub.Id, OrderStatus.Confirmed);
                f.AddHistory(order.Id, OrderStatus.Placed, OrderStatus.Confirmed, Uc03TestData.Vn(1, 12, 5, 0));
                f.NewOrder(hub.Id, OrderStatus.Delivered);
                HubDto current = f.Hubs(s => s.Get(hub.Id)).Value;

                var result = f.Hubs(s => s.Delete(hub.Id, current.RowVersion));

                Assert.AreEqual(ResultStatus.Ok, result.Status);
                Assert.IsNull(f.FindInList(hub.Id));
                Uc03TestData.AssertFails(f.Hubs(s => s.Get(hub.Id)), ResultStatus.NotFound);

                Hub stored = f.Read(db => db.Hubs.SingleOrDefault(h => h.Id == hub.Id));
                Assert.IsNotNull(stored);
                Assert.IsTrue(stored.IsDeleted);
                Assert.IsFalse(stored.IsActive);
                Assert.AreEqual(2, f.Read(db => db.Orders.Count(o => o.HubId == hub.Id)));
                Assert.AreEqual(1, f.Read(db => db.OrderStatusHistories.Count(h => h.OrderId == order.Id)));

                string newVersion = Convert.ToBase64String(stored.RowVersion);
                Uc03TestData.AssertFails(f.Hubs(s => s.Delete(hub.Id, newVersion)), ResultStatus.NotFound);
                Uc03TestData.AssertFails(f.Hubs(s => s.SetActive(hub.Id, true, newVersion)), ResultStatus.NotFound);
                Assert.IsTrue(f.Read(db => db.Hubs.Single(h => h.Id == hub.Id).IsDeleted));
            }
        }

        // ---------- UC01 collaboration (BR8) ----------

        private sealed class QuoteSetup
        {
            public Hub Hub;
            public MenuItem Item;
            public User Customer;
        }

        private static QuoteSetup SeedForQuote(Uc03Fixture f, int capacity)
        {
            Brand brand = f.NewBrand();
            Hub hub = f.NewHub(capacity, true, "quote", brand.Id);
            return new QuoteSetup { Hub = hub, Item = f.NewItem(brand.Id), Customer = f.Customer };
        }

        private static CreateOrderRequest DraftFor(QuoteSetup s)
        {
            return new CreateOrderRequest
            {
                CustomerId = s.Customer.Id,
                HubId = s.Hub.Id,
                DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                DistanceKm = 3m,
                Items = new List<OrderItemRequest> { new OrderItemRequest { MenuItemId = s.Item.Id, Quantity = 2 } }
            };
        }

        private static OrderQuoteResult Quote(QuoteSetup s)
        {
            using (var db = new SaigonBiteDbContext()) return new OrderService(db).Quote(DraftFor(s));
        }

        private static void AssertQuoteRejected(QuoteSetup s, string rule)
        {
            Uc03TestData.AssertBusinessRule(rule, () => Quote(s));
        }

        [TestMethod]
        public void Quote_RejectsNewOrderForHubDeactivatedThroughHubService_AndAcceptsAgainAfterReactivation()
        {
            using (var f = new Uc03Fixture())
            {
                QuoteSetup s = SeedForQuote(f, 10);
                Assert.AreEqual(0m, Quote(s).HubLoad);

                HubDto current = f.Hubs(h => h.Get(s.Hub.Id)).Value;
                var off = f.Hubs(h => h.SetActive(s.Hub.Id, false, current.RowVersion));
                Assert.AreEqual(ResultStatus.Ok, off.Status);
                // UC01 (OrderService.Quote) reports an inactive hub as unavailable.
                AssertQuoteRejected(s, "NOT_FOUND");

                var on = f.Hubs(h => h.SetActive(s.Hub.Id, true, off.Value.RowVersion));
                Assert.AreEqual(ResultStatus.Ok, on.Status);
                Assert.AreEqual(0m, Quote(s).HubLoad);
            }
        }

        [TestMethod]
        public void Quote_RejectsNewOrderForSoftDeletedHub()
        {
            using (var f = new Uc03Fixture())
            {
                QuoteSetup s = SeedForQuote(f, 10);
                HubDto current = f.Hubs(h => h.Get(s.Hub.Id)).Value;
                Assert.AreEqual(ResultStatus.Ok, f.Hubs(h => h.Delete(s.Hub.Id, current.RowVersion)).Status);
                AssertQuoteRejected(s, "NOT_FOUND");
            }
        }

        [TestMethod]
        public void Quote_AcceptsHubAtEightyPercentAndRejectsAtNinetyPercent()
        {
            using (var f = new Uc03Fixture())
            {
                QuoteSetup s = SeedForQuote(f, 10);
                f.AddOrders(s.Hub.Id, OrderStatus.Confirmed, 4);
                f.AddOrders(s.Hub.Id, OrderStatus.Preparing, 4);
                // Orders that do not count must not push the hub over the limit.
                f.AddOrders(s.Hub.Id, OrderStatus.Placed, 3);
                f.AddOrders(s.Hub.Id, OrderStatus.Ready, 3);
                f.AddOrders(s.Hub.Id, OrderStatus.Cancelled, 3);

                Assert.AreEqual(0.8m, Quote(s).HubLoad);
                Assert.AreEqual(80, f.FindInList(s.Hub.Id).LoadPercent);

                f.NewOrder(s.Hub.Id, OrderStatus.Confirmed);
                Assert.AreEqual(HubLoadLevel.Full, f.FindInList(s.Hub.Id).LoadLevel);
                AssertQuoteRejected(s, "BR8");
            }
        }
    }
}
