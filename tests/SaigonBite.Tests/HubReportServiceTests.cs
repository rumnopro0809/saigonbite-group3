using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonBite.Application;
using SaigonBite.Domain;
using SaigonBite.Infrastructure;

namespace SaigonBite.Tests
{
    /// <summary>
    /// UC03 hub utilization report R2: FR19, AMB06 (TC19). Range = inclusive Vietnam-local start day,
    /// exclusive start of the day after the end day (UTC+7). All data is in March 2001.
    /// </summary>
    [TestClass]
    public class HubReportServiceTests
    {
        private static readonly DateTime Day10 = new DateTime(2001, 3, 10);
        private static readonly DateTime Day11 = new DateTime(2001, 3, 11);

        [ClassInitialize]
        public static void EnsureDatabaseMigrated(TestContext context)
        {
            Uc03TestData.MigrateDatabase();
        }

        private static ServiceResult<HubUtilizationReport> Build(DateTime from, DateTime to)
        {
            using (var db = new AppDbContext()) return new HubReportService(db).Build(from, to);
        }

        private static HubUtilizationRow RowFor(DateTime from, DateTime to, int hubId)
        {
            var result = Build(from, to);
            Assert.AreEqual(ResultStatus.Ok, result.Status);
            return result.Value.Rows.Single(r => r.HubId == hubId);
        }

        private static Order Confirmed(Uc03Fixture f, int hubId, DateTimeOffset at, OrderStatus current = OrderStatus.Confirmed)
        {
            Order order = f.NewOrder(hubId, current);
            f.AddHistory(order.Id, OrderStatus.Placed, OrderStatus.Confirmed, at);
            return order;
        }

        [TestMethod]
        public void Build_RejectsFromAfterTo()
        {
            var result = Build(Day11, Day10);
            Uc03TestData.AssertFails(result, ResultStatus.Invalid);
            Assert.IsTrue(result.Errors.Count > 0);
            Assert.IsNull(result.Value);
        }

        [TestMethod]
        public void Build_AcceptsSingleDayRange_AndKeepsReportDates()
        {
            var result = Build(Day10, Day10);
            Assert.AreEqual(ResultStatus.Ok, result.Status);
            Assert.AreEqual(Day10, result.Value.From);
            Assert.AreEqual(Day10, result.Value.To);
            Assert.AreEqual(result.Value.Rows.Count, result.Value.HubCount);
        }

        [TestMethod]
        public void Build_HandledCountUsesVietnamTimeBoundaries()
        {
            using (var f = new Uc03Fixture())
            {
                Hub hub = f.NewHub(10);
                Confirmed(f, hub.Id, Uc03TestData.Vn(9, 23, 59, 59));    // day before: out
                Confirmed(f, hub.Id, Uc03TestData.Vn(10, 0, 0, 0));      // first instant of day 10: in
                Confirmed(f, hub.Id, Uc03TestData.Vn(10, 23, 59, 59));   // last second of day 10: in
                Confirmed(f, hub.Id, Uc03TestData.Vn(11, 0, 0, 0));      // first instant of day 11: out
                // Same instants expressed in UTC: 16:59:59Z = 23:59:59+07 (in), 17:00:00Z = 00:00:00+07 next day (out).
                Confirmed(f, hub.Id, new DateTimeOffset(2001, 3, 10, 16, 59, 59, TimeSpan.Zero));
                Confirmed(f, hub.Id, new DateTimeOffset(2001, 3, 10, 17, 0, 0, TimeSpan.Zero));
                // Placed but never confirmed: not handled.
                f.NewOrder(hub.Id, OrderStatus.Placed);
                f.AddHistory(f.NewOrder(hub.Id, OrderStatus.Placed).Id, OrderStatus.Placed, OrderStatus.Placed, Uc03TestData.Vn(10, 12, 0, 0));

                Assert.AreEqual(3, RowFor(Day10, Day10, hub.Id).HandledCount);
                Assert.AreEqual(4, RowFor(new DateTime(2001, 3, 9), Day10, hub.Id).HandledCount);
                Assert.AreEqual(5, RowFor(Day10, Day11, hub.Id).HandledCount);
                Assert.AreEqual(2, RowFor(Day11, Day11, hub.Id).HandledCount);
                Assert.AreEqual(0, RowFor(new DateTime(2001, 3, 12), new DateTime(2001, 3, 12), hub.Id).HandledCount);
            }
        }

        [TestMethod]
        public void Build_HandledCountIncludesLaterCancelledOrders_AndCountsEachOrderOnce()
        {
            using (var f = new Uc03Fixture())
            {
                Hub hub = f.NewHub(10);
                Order cancelledLater = Confirmed(f, hub.Id, Uc03TestData.Vn(10, 9, 0, 0), OrderStatus.Cancelled);
                f.AddHistory(cancelledLater.Id, OrderStatus.Confirmed, OrderStatus.Cancelled, Uc03TestData.Vn(10, 9, 30, 0));
                Order twice = Confirmed(f, hub.Id, Uc03TestData.Vn(10, 10, 0, 0), OrderStatus.Preparing);
                f.AddHistory(twice.Id, OrderStatus.Confirmed, OrderStatus.Confirmed, Uc03TestData.Vn(10, 10, 5, 0));

                HubUtilizationRow row = RowFor(Day10, Day10, hub.Id);
                Assert.AreEqual(2, row.HandledCount);
                // Only the Preparing order loads the hub (1 of 10); the cancelled one no longer does.
                Assert.AreEqual(10, row.CurrentLoadPercent);
            }
        }

        [TestMethod]
        public void Build_AveragePrepMinutesUsesOnlyOrdersWithBothConfirmedAndReady()
        {
            using (var f = new Uc03Fixture())
            {
                Hub hub = f.NewHub(10);
                Order a = Confirmed(f, hub.Id, Uc03TestData.Vn(10, 10, 0, 0), OrderStatus.Ready);
                f.AddHistory(a.Id, OrderStatus.Preparing, OrderStatus.Ready, Uc03TestData.Vn(10, 10, 20, 0));   // 20 min
                Order b = Confirmed(f, hub.Id, Uc03TestData.Vn(10, 11, 0, 0), OrderStatus.Ready);
                f.AddHistory(b.Id, OrderStatus.Preparing, OrderStatus.Ready, Uc03TestData.Vn(10, 11, 10, 0));   // 10 min
                Confirmed(f, hub.Id, Uc03TestData.Vn(10, 12, 0, 0), OrderStatus.Preparing);                     // no Ready: excluded
                Confirmed(f, hub.Id, Uc03TestData.Vn(10, 13, 0, 0), OrderStatus.Cancelled);                     // no Ready: excluded

                HubUtilizationRow row = RowFor(Day10, Day10, hub.Id);

                Assert.AreEqual(4, row.HandledCount);
                Assert.AreEqual(2, row.PrepSampleCount);
                Assert.IsTrue(row.AveragePrepMinutes.HasValue);
                Assert.AreEqual(15.0m, row.AveragePrepMinutes.Value);
            }
        }

        [TestMethod]
        public void Build_OrderConfirmedOnLastDayAndReadyAfterMidnightStillHasPrepSample()
        {
            using (var f = new Uc03Fixture())
            {
                Hub hub = f.NewHub(10);
                Order o = Confirmed(f, hub.Id, Uc03TestData.Vn(10, 23, 50, 0), OrderStatus.Ready);
                f.AddHistory(o.Id, OrderStatus.Preparing, OrderStatus.Ready, Uc03TestData.Vn(11, 0, 10, 0));

                HubUtilizationRow row = RowFor(Day10, Day10, hub.Id);

                Assert.AreEqual(1, row.HandledCount);
                Assert.AreEqual(1, row.PrepSampleCount);
                Assert.AreEqual(20.0m, row.AveragePrepMinutes.Value);
            }
        }

        [TestMethod]
        public void Build_WithoutAnyCompleteSample_AveragePrepIsNullAndSampleCountZero()
        {
            using (var f = new Uc03Fixture())
            {
                Hub hub = f.NewHub(10);
                // A hub with no orders at all, and a hub whose only order never became Ready.
                Hub idle = f.NewHub(10, true, "idle");
                Confirmed(f, hub.Id, Uc03TestData.Vn(10, 10, 0, 0), OrderStatus.Preparing);

                HubUtilizationRow row = RowFor(Day10, Day10, hub.Id);
                Assert.AreEqual(1, row.HandledCount);
                Assert.AreEqual(0, row.PrepSampleCount);
                Assert.IsNull(row.AveragePrepMinutes);

                HubUtilizationRow idleRow = RowFor(Day10, Day10, idle.Id);
                Assert.AreEqual(0, idleRow.HandledCount);
                Assert.AreEqual(0, idleRow.PrepSampleCount);
                Assert.IsNull(idleRow.AveragePrepMinutes);
            }
        }

        [TestMethod]
        public void Build_QuietKitchenCountComesFromOrderSnapshotIncludingLaterCancelledOrders()
        {
            using (var f = new Uc03Fixture())
            {
                Hub hub = f.NewHub(10);
                f.NewOrder(hub.Id, OrderStatus.Placed, Uc03TestData.Vn(10, 9, 0, 0), quiet: true);
                f.NewOrder(hub.Id, OrderStatus.Cancelled, Uc03TestData.Vn(10, 23, 59, 59), quiet: true);   // cancelled later, still counted
                f.NewOrder(hub.Id, OrderStatus.Placed, Uc03TestData.Vn(10, 12, 0, 0), quiet: false);       // no discount granted
                f.NewOrder(hub.Id, OrderStatus.Placed, Uc03TestData.Vn(11, 0, 0, 0), quiet: true);         // next Vietnam day: out of range
                f.NewOrder(hub.Id, OrderStatus.Placed, Uc03TestData.Vn(9, 23, 59, 59), quiet: true);       // previous day: out of range

                Assert.AreEqual(2, RowFor(Day10, Day10, hub.Id).QuietGrantedCount);
                Assert.AreEqual(3, RowFor(Day10, Day11, hub.Id).QuietGrantedCount);
                Assert.AreEqual(0, RowFor(new DateTime(2001, 3, 12), new DateTime(2001, 3, 12), hub.Id).QuietGrantedCount);

                var report = Build(Day10, Day10).Value;
                Assert.IsTrue(report.TotalQuietGranted >= 2);
                Assert.AreEqual(report.Rows.Sum(r => r.QuietGrantedCount), report.TotalQuietGranted);
                Assert.AreEqual(report.Rows.Sum(r => r.HandledCount), report.TotalHandled);
            }
        }

        [TestMethod]
        public void Build_CurrentLoadMatchesHubServiceLoad_TC19()
        {
            using (var f = new Uc03Fixture())
            {
                Hub hub = f.NewHub(10);
                f.AddOrders(hub.Id, OrderStatus.Confirmed, 2);
                f.AddOrders(hub.Id, OrderStatus.Preparing, 1);
                f.AddOrders(hub.Id, OrderStatus.Placed, 2);
                f.AddOrders(hub.Id, OrderStatus.Ready, 2);
                f.AddOrders(hub.Id, OrderStatus.Cancelled, 2);

                HubUtilizationRow row = RowFor(Day10, Day10, hub.Id);
                HubDto dto = f.Hubs(s => s.Get(hub.Id)).Value;

                Assert.AreEqual(30, dto.LoadPercent);
                Assert.AreEqual(dto.LoadPercent, row.CurrentLoadPercent);
                Assert.AreEqual(dto.LoadLevel, row.CurrentLoadLevel);
                Assert.AreEqual(HubLoadLevel.Normal, row.CurrentLoadLevel);
                Assert.AreEqual(hub.Name, row.HubName);

                f.NewOrder(hub.Id, OrderStatus.Confirmed);
                f.AddOrders(hub.Id, OrderStatus.Preparing, 5);
                row = RowFor(Day10, Day10, hub.Id);
                dto = f.Hubs(s => s.Get(hub.Id)).Value;
                Assert.AreEqual(90, row.CurrentLoadPercent);
                Assert.AreEqual(dto.LoadPercent, row.CurrentLoadPercent);
                Assert.AreEqual(HubLoadLevel.Full, row.CurrentLoadLevel);
            }
        }

        [TestMethod]
        public void Build_OmitsSoftDeletedHubs()
        {
            using (var f = new Uc03Fixture())
            {
                Hub live = f.NewHub(10, true, "live");
                Hub gone = f.NewHub(10, true, "gone");
                HubDto current = f.Hubs(s => s.Get(gone.Id)).Value;
                Assert.AreEqual(ResultStatus.Ok, f.Hubs(s => s.Delete(gone.Id, current.RowVersion)).Status);

                var report = Build(Day10, Day10).Value;

                Assert.IsTrue(report.Rows.Any(r => r.HubId == live.Id));
                Assert.IsFalse(report.Rows.Any(r => r.HubId == gone.Id));
            }
        }
    }
}
