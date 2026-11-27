using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
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
    /// <summary>
    /// Verifies NFR01: full price calculation for a 30-line order completes in under 1.5 seconds.
    /// </summary>
    [TestClass]
    public class PerformanceTests
    {
        public TestContext TestContext { get; set; }

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
        public void NFR01_PricingService_30LineOrderCalculation_CompletesUnder1500Ms()
        {
            var service = new PricingService();
            var input = new PricingInput
            {
                Subtotal = 1250000m,
                DistanceKm = 6.4m,
                HubLoad = 0.25m,
                Quantities = Enumerable.Repeat(2, 30).ToArray(),
                PlacedAt = new DateTimeOffset(2026, 11, 27, 18, 15, 0, TimeSpan.FromHours(7)),
                HeavyRain = true,
                AllItemsAvailableAtHub = true,
                Voucher = new VoucherInput
                {
                    Type = VoucherType.Percent,
                    Value = 15m,
                    Cap = 40000m,
                    MinimumSubtotal = 100000m,
                    Active = true,
                    ValidFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(7)),
                    ValidTo = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.FromHours(7))
                }
            };

            // Warm up JIT compilation before timing.
            service.Calculate(input);

            Stopwatch singleSw = Stopwatch.StartNew();
            PriceBreakdown breakdown = service.Calculate(input);
            singleSw.Stop();

            Stopwatch batchSw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                service.Calculate(input);
            }
            batchSw.Stop();

            double singleMs = singleSw.Elapsed.TotalMilliseconds;
            double avgMs = batchSw.Elapsed.TotalMilliseconds / 100.0;

            if (TestContext != null)
            {
                TestContext.WriteLine(
                    "NFR01 PricingService 30-line timing: single={0:F3} ms, 100-run avg={1:F3} ms (limit: 1500 ms)",
                    singleMs,
                    avgMs);
            }

            Assert.IsTrue(breakdown.PayableTotal > 0m);
            Assert.IsTrue(singleMs < 1500.0, "Single 30-line calculation exceeded 1500 ms: " + singleMs);
            Assert.IsTrue(avgMs < 1500.0, "Average 30-line calculation exceeded 1500 ms: " + avgMs);
        }

        [TestMethod]
        public void NFR01_OrderService_QuoteWithSqlExpress_CompletesUnder1500Ms()
        {
            using (var db = new SaigonBiteDbContext())
            {
                User local = db.Users.Single(u => u.Identifier == "local@saigonbite.vn");
                Hub hub = db.Hubs.OrderBy(h => h.Id).First();
                List<MenuItem> items = db.MenuItems.Where(m => !m.IsDeleted && m.IsAvailable).ToList();

                var request = new CreateOrderRequest
                {
                    CustomerId = local.Id,
                    HubId = hub.Id,
                    DeliveryAddress = "24 Nguyen Hue, District 1, HCMC",
                    DistanceKm = 4.5m,
                    VoucherCode = "SAIGON15",
                    PlacedAtOverride = new DateTimeOffset(2026, 11, 27, 12, 15, 0, TimeSpan.FromHours(7)),
                    Items = items.Select(m => new OrderItemRequest { MenuItemId = m.Id, Quantity = 1 }).ToList()
                };

                var service = new OrderService(db);

                // Warm up EF query plan and connection pool.
                service.Quote(request);

                Stopwatch sw = Stopwatch.StartNew();
                OrderQuoteResult quote = service.Quote(request);
                sw.Stop();

                double elapsedMs = sw.Elapsed.TotalMilliseconds;
                if (TestContext != null)
                {
                    TestContext.WriteLine(
                        "NFR01 OrderService.Quote (SQL Server Express): {0:F2} ms (limit: 1500 ms)",
                        elapsedMs);
                }

                Assert.IsNotNull(quote);
                Assert.IsTrue(quote.Breakdown.PayableTotal > 0m);
                Assert.IsTrue(elapsedMs < 1500.0, "OrderService.Quote exceeded 1500 ms: " + elapsedMs);
            }
        }
    }
}
