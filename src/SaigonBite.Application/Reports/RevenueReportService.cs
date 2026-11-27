using System;
using System.Collections.Generic;
using System.Linq;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Application.Reports
{
    public interface IRevenueReportRepository
    {
        IList<Order> GetOrdersWithLinesAndHistory();
    }

    public sealed class CategoryRevenueRow
    {
        public string Category { get; set; }
        public int ItemQuantity { get; set; }
        public decimal Revenue { get; set; }
    }

    public sealed class PaymentRevenueRow
    {
        public PaymentMethod Method { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public sealed class RevenueReportResult
    {
        public RevenueReportResult()
        {
            ByCategory = new List<CategoryRevenueRow>();
            ByPaymentMethod = new List<PaymentRevenueRow>();
        }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool DeliveredOnly { get; set; }
        public int IncludedOrderCount { get; set; }
        public int ExcludedCancelledOrFailedCount { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal FoodRevenue { get; set; }
        public decimal FeesAndRounding { get; set; }
        public bool IsReconciled { get; set; }

        public IList<CategoryRevenueRow> ByCategory { get; set; }
        public IList<PaymentRevenueRow> ByPaymentMethod { get; set; }
    }

    // HT04 / R1 Revenue Report (CRC09) — FR18, FR13, AMB04.
    // Author: Duong Pham Thai (524H0189).
    // Uses stored Order & OrderLine snapshots only; never recalculates from current MenuItem prices.
    public sealed class RevenueReportService
    {
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
        private static readonly PaymentMethod[] AllPaymentMethods =
        {
            PaymentMethod.MoMo,
            PaymentMethod.VNPay,
            PaymentMethod.ApplePay,
            PaymentMethod.PayPal,
            PaymentMethod.Cod
        };

        private readonly IRevenueReportRepository _repository;

        public RevenueReportService(IRevenueReportRepository repository)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            _repository = repository;
        }

        public RevenueReportResult BuildReport(ActorRole actorRole, DateTime fromDate, DateTime toDate, bool deliveredOnly = true)
        {
            if (actorRole != ActorRole.Admin)
            {
                throw new BusinessRuleException("FR13", "Only System Admin can view the R1 Revenue Report.");
            }

            DateTime startDate = fromDate.Date;
            DateTime endDate = toDate.Date < startDate ? startDate : toDate.Date;

            DateTimeOffset rangeStart = new DateTimeOffset(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, VietnamOffset);
            DateTimeOffset rangeEndExclusive = rangeStart.AddDays((endDate - startDate).Days + 1);

            IList<Order> allOrders = _repository.GetOrdersWithLinesAndHistory() ?? new List<Order>();

            int excludedCount = 0;
            var matchedOrders = new List<Order>();

            foreach (Order order in allOrders)
            {
                if (order == null) continue;

                // FR18: Cancelled and Failed orders are always excluded from R1 revenue.
                if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Failed)
                {
                    excludedCount++;
                    continue;
                }

                // AMB04: Default recognition includes only Delivered orders; optional toggle lets Admin inspect active orders.
                if (deliveredOnly && order.Status != OrderStatus.Delivered)
                {
                    continue;
                }

                DateTimeOffset recognitionTime = GetRecognitionTimestamp(order);
                if (recognitionTime >= rangeStart && recognitionTime < rangeEndExclusive)
                {
                    matchedOrders.Add(order);
                }
            }

            var categoryTotals = new Dictionary<string, CategoryRevenueRow>(StringComparer.OrdinalIgnoreCase);
            var paymentTotals = new Dictionary<PaymentMethod, PaymentRevenueRow>();
            foreach (PaymentMethod method in AllPaymentMethods)
            {
                paymentTotals[method] = new PaymentRevenueRow { Method = method };
            }

            decimal totalRevenue = 0m;
            decimal foodRevenue = 0m;

            foreach (Order order in matchedOrders)
            {
                totalRevenue += order.PayableTotal;
                foodRevenue += order.FoodTotal;

                PaymentRevenueRow payRow;
                if (!paymentTotals.TryGetValue(order.PaymentMethod, out payRow))
                {
                    payRow = new PaymentRevenueRow { Method = order.PaymentMethod };
                    paymentTotals[order.PaymentMethod] = payRow;
                }
                payRow.OrderCount++;
                payRow.Revenue += order.PayableTotal;

                AllocateOrderFoodByCategory(order, categoryTotals);
            }

            decimal feesAndRounding = totalRevenue - foodRevenue;
            decimal categorySum = categoryTotals.Values.Sum(c => c.Revenue);
            decimal paymentSum = paymentTotals.Values.Sum(p => p.Revenue);

            return new RevenueReportResult
            {
                FromDate = startDate,
                ToDate = endDate,
                DeliveredOnly = deliveredOnly,
                IncludedOrderCount = matchedOrders.Count,
                ExcludedCancelledOrFailedCount = excludedCount,
                TotalRevenue = totalRevenue,
                FoodRevenue = foodRevenue,
                FeesAndRounding = feesAndRounding,
                IsReconciled = Math.Abs((foodRevenue + feesAndRounding) - paymentSum) < 0.01m
                    && Math.Abs(categorySum - foodRevenue) < 0.01m,
                ByCategory = categoryTotals.Values.OrderByDescending(c => c.Revenue).ThenBy(c => c.Category).ToList(),
                ByPaymentMethod = AllPaymentMethods.Select(m => paymentTotals[m]).ToList()
            };
        }

        private static DateTimeOffset GetRecognitionTimestamp(Order order)
        {
            if (order.Status == OrderStatus.Delivered && order.StatusHistory != null)
            {
                OrderStatusHistory delivered = order.StatusHistory
                    .Where(h => h.ToStatus == OrderStatus.Delivered)
                    .OrderByDescending(h => h.ChangedAt)
                    .FirstOrDefault();

                if (delivered != null)
                {
                    return delivered.ChangedAt.ToOffset(VietnamOffset);
                }
            }

            return order.PlacedAt.ToOffset(VietnamOffset);
        }

        // AMB04: Allocate net food revenue (S - Q - V) proportionally across OrderLine snapshots.
        private static void AllocateOrderFoodByCategory(Order order, IDictionary<string, CategoryRevenueRow> categoryTotals)
        {
            if (order.Lines == null || order.Lines.Count == 0)
            {
                if (order.FoodTotal > 0m)
                {
                    AddCategoryAmount(categoryTotals, "Uncategorized", 0, order.FoodTotal);
                }
                return;
            }

            var lineAmounts = order.Lines
                .Select(l => new
                {
                    Category = string.IsNullOrWhiteSpace(l.CategorySnapshot) ? "Uncategorized" : l.CategorySnapshot.Trim(),
                    Qty = l.Quantity,
                    Amount = l.UnitPriceSnapshot * l.Quantity
                })
                .ToList();

            decimal grossSum = lineAmounts.Sum(x => x.Amount);
            if (grossSum <= 0m)
            {
                AddCategoryAmount(categoryTotals, lineAmounts[0].Category, lineAmounts.Sum(x => x.Qty), order.FoodTotal);
                return;
            }

            decimal allocatedSoFar = 0m;
            for (int i = 0; i < lineAmounts.Count; i++)
            {
                var line = lineAmounts[i];
                decimal share = (i == lineAmounts.Count - 1)
                    ? order.FoodTotal - allocatedSoFar
                    : Math.Round(order.FoodTotal * (line.Amount / grossSum), 2, MidpointRounding.AwayFromZero);

                allocatedSoFar += share;
                AddCategoryAmount(categoryTotals, line.Category, line.Qty, share);
            }
        }

        private static void AddCategoryAmount(IDictionary<string, CategoryRevenueRow> map, string category, int qty, decimal amount)
        {
            CategoryRevenueRow row;
            if (!map.TryGetValue(category, out row))
            {
                row = new CategoryRevenueRow { Category = category };
                map[category] = row;
            }
            row.ItemQuantity += qty;
            row.Revenue += amount;
        }
    }
}
