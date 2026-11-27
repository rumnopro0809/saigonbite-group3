using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonBite.Application.Reports;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Tests
{
    // HT04 R1 Revenue Report unit tests (TC18, FR18, AMB04, FR13). Author: 524H0189.
    [TestClass]
    public class RevenueReportServiceTests
    {
        private static readonly TimeSpan VnOffset = TimeSpan.FromHours(7);

        [TestMethod]
        public void TC18_BuildReport_ExcludesCancelledAndFailed_ReconcilesCategoriesAnd5Payments()
        {
            var orders = new List<Order>
            {
                // Delivered Local order via MoMo (with 10% Quiet Kitchen discount allocated across 2 categories)
                MakeOrder(OrderStatus.Delivered, PaymentMethod.MoMo,
                    new DateTimeOffset(2026, 11, 5, 12, 30, 0, VnOffset),
                    foodTotal: 180000m, payableTotal: 205000m,
                    new OrderLine { CategorySnapshot = "Vietnamese Rice", UnitPriceSnapshot = 100000m, Quantity = 1 },
                    new OrderLine { CategorySnapshot = "Banh Mi", UnitPriceSnapshot = 100000m, Quantity = 1 }),

                // Delivered Tourist order via Apple Pay
                MakeOrder(OrderStatus.Delivered, PaymentMethod.ApplePay,
                    new DateTimeOffset(2026, 11, 6, 18, 0, 0, VnOffset),
                    foodTotal: 300000m, payableTotal: 300000m,
                    new OrderLine { CategorySnapshot = "Noodles & Hotpot", UnitPriceSnapshot = 150000m, Quantity = 2 }),

                // Cancelled order MUST be excluded
                MakeOrder(OrderStatus.Cancelled, PaymentMethod.VNPay,
                    new DateTimeOffset(2026, 11, 6, 14, 0, 0, VnOffset),
                    foodTotal: 250000m, payableTotal: 270000m,
                    new OrderLine { CategorySnapshot = "Vietnamese Rice", UnitPriceSnapshot = 250000m, Quantity = 1 }),

                // Failed order MUST be excluded
                MakeOrder(OrderStatus.Failed, PaymentMethod.Cod,
                    new DateTimeOffset(2026, 11, 6, 19, 0, 0, VnOffset),
                    foodTotal: 120000m, payableTotal: 135000m,
                    new OrderLine { CategorySnapshot = "Banh Mi", UnitPriceSnapshot = 120000m, Quantity = 1 }),

                // Placed (not yet Delivered) excluded when deliveredOnly = true (AMB04)
                MakeOrder(OrderStatus.Placed, PaymentMethod.PayPal,
                    new DateTimeOffset(2026, 11, 6, 20, 0, 0, VnOffset),
                    foodTotal: 100000m, payableTotal: 115000m,
                    new OrderLine { CategorySnapshot = "Banh Mi", UnitPriceSnapshot = 100000m, Quantity = 1 })
            };

            var service = new RevenueReportService(new FakeRevenueRepository(orders));
            RevenueReportResult report = service.BuildReport(
                ActorRole.Admin,
                new DateTime(2026, 11, 1),
                new DateTime(2026, 11, 7),
                deliveredOnly: true);

            Assert.AreEqual(2, report.IncludedOrderCount);
            Assert.AreEqual(2, report.ExcludedCancelledOrFailedCount);
            Assert.AreEqual(505000m, report.TotalRevenue);
            Assert.AreEqual(480000m, report.FoodRevenue);
            Assert.AreEqual(25000m, report.FeesAndRounding);
            Assert.IsTrue(report.IsReconciled);

            // All 5 payment methods are always present.
            Assert.AreEqual(5, report.ByPaymentMethod.Count);
            Assert.AreEqual(205000m, report.ByPaymentMethod.Single(p => p.Method == PaymentMethod.MoMo).Revenue);
            Assert.AreEqual(300000m, report.ByPaymentMethod.Single(p => p.Method == PaymentMethod.ApplePay).Revenue);
            Assert.AreEqual(0m, report.ByPaymentMethod.Single(p => p.Method == PaymentMethod.VNPay).Revenue);

            // Proportional discount allocation: 180k split 50/50 -> 90k Rice, 90k Banh Mi; + 300k Noodles.
            Assert.AreEqual(90000m, report.ByCategory.Single(c => c.Category == "Vietnamese Rice").Revenue);
            Assert.AreEqual(90000m, report.ByCategory.Single(c => c.Category == "Banh Mi").Revenue);
            Assert.AreEqual(300000m, report.ByCategory.Single(c => c.Category == "Noodles & Hotpot").Revenue);
        }

        [TestMethod]
        public void TC18_BuildReport_FiltersInclusiveLocalDateRange_UTCPlus7()
        {
            var orders = new List<Order>
            {
                MakeOrder(OrderStatus.Delivered, PaymentMethod.Cod,
                    new DateTimeOffset(2026, 11, 1, 0, 0, 0, VnOffset),
                    foodTotal: 100000m, payableTotal: 115000m,
                    new OrderLine { CategorySnapshot = "Banh Mi", UnitPriceSnapshot = 100000m, Quantity = 1 }),
                MakeOrder(OrderStatus.Delivered, PaymentMethod.Cod,
                    new DateTimeOffset(2026, 11, 8, 0, 0, 0, VnOffset), // day after end date -> excluded
                    foodTotal: 200000m, payableTotal: 215000m,
                    new OrderLine { CategorySnapshot = "Banh Mi", UnitPriceSnapshot = 200000m, Quantity = 1 })
            };

            var service = new RevenueReportService(new FakeRevenueRepository(orders));
            RevenueReportResult report = service.BuildReport(
                ActorRole.Admin,
                new DateTime(2026, 11, 1),
                new DateTime(2026, 11, 7),
                deliveredOnly: true);

            Assert.AreEqual(1, report.IncludedOrderCount);
            Assert.AreEqual(115000m, report.TotalRevenue);
        }

        [TestMethod]
        public void TC18_BuildReport_NonAdmin_ThrowsFR13()
        {
            var service = new RevenueReportService(new FakeRevenueRepository(new List<Order>()));
            var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                service.BuildReport(ActorRole.Customer, DateTime.Today, DateTime.Today));
            Assert.AreEqual("FR13", ex.Rule);
        }

        private static Order MakeOrder(
            OrderStatus status,
            PaymentMethod method,
            DateTimeOffset timestamp,
            decimal foodTotal,
            decimal payableTotal,
            params OrderLine[] lines)
        {
            var order = new Order
            {
                Status = status,
                PaymentMethod = method,
                PlacedAt = timestamp,
                FoodTotal = foodTotal,
                PayableTotal = payableTotal,
                Lines = lines.ToList(),
                StatusHistory = new List<OrderStatusHistory>()
            };

            if (status == OrderStatus.Delivered)
            {
                order.StatusHistory.Add(new OrderStatusHistory
                {
                    FromStatus = OrderStatus.OutForDelivery,
                    ToStatus = OrderStatus.Delivered,
                    ChangedAt = timestamp
                });
            }

            return order;
        }

        private sealed class FakeRevenueRepository : IRevenueReportRepository
        {
            private readonly IList<Order> _orders;
            public FakeRevenueRepository(IList<Order> orders) { _orders = orders; }
            public IList<Order> GetOrdersWithLinesAndHistory() { return _orders; }
        }
    }
}
