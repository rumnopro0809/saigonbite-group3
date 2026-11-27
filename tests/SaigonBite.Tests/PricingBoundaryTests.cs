using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonBite.Domain;

namespace SaigonBite.Tests
{
    /// <summary>
    /// Systematic Equivalence Partitioning (EP) and Boundary Value Analysis (BVA)
    /// test suite covering every numeric threshold in BR1-BR12 and BR14.
    /// </summary>
    [TestClass]
    public class PricingBoundaryTests
    {
        private readonly PricingService _service = new PricingService();

        private static PricingInput CreateBaseInput()
        {
            return new PricingInput
            {
                Subtotal = 200000m,
                DistanceKm = 3.2m,
                HubLoad = 0.20m,
                Quantities = new[] { 1 },
                PlacedAt = new DateTimeOffset(2026, 11, 27, 15, 0, 0, TimeSpan.FromHours(7)),
                HeavyRain = false,
                AllItemsAvailableAtHub = true,
                Voucher = null
            };
        }

        private static VoucherInput CreateVoucher(
            VoucherType type,
            decimal value,
            decimal cap = 30000m,
            decimal minSubtotal = 100000m)
        {
            return new VoucherInput
            {
                Type = type,
                Value = value,
                Cap = cap,
                MinimumSubtotal = minSubtotal,
                Active = true,
                ValidFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(7)),
                ValidTo = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.FromHours(7))
            };
        }

        // BVA-01 (BR1): Subtotal 49,999 VND is below the 50,000 minimum.
        [TestMethod]
        public void BVA01_BR01_Subtotal49999_ThrowsBR1()
        {
            PricingInput input = CreateBaseInput();
            input.Subtotal = 49999m;

            var ex = Assert.ThrowsException<BusinessRuleException>(() => _service.Calculate(input));
            Assert.AreEqual("BR1", ex.Rule);
        }

        // BVA-02 (BR1): Subtotal 50,000 VND is accepted at the lower boundary.
        [TestMethod]
        public void BVA02_BR01_Subtotal50000_Accepts()
        {
            PricingInput input = CreateBaseInput();
            input.Subtotal = 50000m;
            input.HubLoad = 0.50m;
            input.DistanceKm = 1.0m;

            PriceBreakdown result = _service.Calculate(input);
            Assert.AreEqual(50000m, result.Subtotal);
            Assert.AreEqual(65000m, result.PayableTotal);
        }

        // BVA-03 (BR2): Negative distance is rejected.
        [TestMethod]
        public void BVA03_BR02_DistanceNegative_ThrowsBR2()
        {
            PricingInput input = CreateBaseInput();
            input.DistanceKm = -0.01m;

            var ex = Assert.ThrowsException<BusinessRuleException>(() => _service.Calculate(input));
            Assert.AreEqual("BR2", ex.Rule);
        }

        // BVA-04 (BR2/BR3): Distance 0.0 km is accepted at Tier 1 base fee 15,000 VND.
        [TestMethod]
        public void BVA04_BR02_DistanceZero_AcceptsWithBaseDeliveryFee()
        {
            PricingInput input = CreateBaseInput();
            input.DistanceKm = 0m;

            PriceBreakdown result = _service.Calculate(input);
            Assert.AreEqual(15000m, result.DeliveryFee);
        }

        // BVA-05 (BR2/BR3): Distance 8.0 km is accepted at maximum Tier 3 fee 51,000 VND.
        [TestMethod]
        public void BVA05_BR02_Distance8Km_AcceptsWithMaxTierDeliveryFee()
        {
            PricingInput input = CreateBaseInput();
            input.DistanceKm = 8.0m;

            PriceBreakdown result = _service.Calculate(input);
            Assert.AreEqual(51000m, result.DeliveryFee);
        }

        // BVA-06 (BR2): Distance 8.01 km exceeds coverage limit and is rejected.
        [TestMethod]
        public void BVA06_BR02_Distance8Point01Km_ThrowsBR2()
        {
            PricingInput input = CreateBaseInput();
            input.DistanceKm = 8.01m;

            var ex = Assert.ThrowsException<BusinessRuleException>(() => _service.Calculate(input));
            Assert.AreEqual("BR2", ex.Rule);
        }

        // BVA-07 (BR3): Distance 2.0 km stays in Tier 1 (15,000 VND).
        [TestMethod]
        public void BVA07_BR03_Distance2Km_Tier1Charges15000()
        {
            PricingInput input = CreateBaseInput();
            input.DistanceKm = 2.0m;

            PriceBreakdown result = _service.Calculate(input);
            Assert.AreEqual(15000m, result.DeliveryFee);
        }

        // BVA-08 (BR3): Distance 2.01 km enters Tier 2 and rounds up to 1 extra km (20,000 VND).
        [TestMethod]
        public void BVA08_BR03_Distance2Point01Km_Tier2RoundsUpTo20000()
        {
            PricingInput input = CreateBaseInput();
            input.DistanceKm = 2.01m;

            PriceBreakdown result = _service.Calculate(input);
            Assert.AreEqual(20000m, result.DeliveryFee);
        }

        // BVA-09 (BR3): Distance 5.0 km is upper edge of Tier 2 (30,000 VND).
        [TestMethod]
        public void BVA09_BR03_Distance5Km_Tier2MaxCharges30000()
        {
            PricingInput input = CreateBaseInput();
            input.DistanceKm = 5.0m;

            PriceBreakdown result = _service.Calculate(input);
            Assert.AreEqual(30000m, result.DeliveryFee);
        }

        // BVA-10 (BR3): Distance 5.01 km enters Tier 3 and rounds up to 1 extra km (37,000 VND).
        [TestMethod]
        public void BVA10_BR03_Distance5Point01Km_Tier3RoundsUpTo37000()
        {
            PricingInput input = CreateBaseInput();
            input.DistanceKm = 5.01m;

            PriceBreakdown result = _service.Calculate(input);
            Assert.AreEqual(37000m, result.DeliveryFee);
        }

        // BVA-11 (BR4): Lunch rush starts inclusively at 11:00 UTC+7 (10:59 = 0, 11:00 = 20% of D).
        [TestMethod]
        public void BVA11_BR04_LunchRushStart_1059Off_1100On()
        {
            PricingInput before = CreateBaseInput();
            before.PlacedAt = new DateTimeOffset(2026, 11, 27, 10, 59, 0, TimeSpan.FromHours(7));
            Assert.AreEqual(0m, _service.Calculate(before).RushFee);

            PricingInput atStart = CreateBaseInput();
            atStart.PlacedAt = new DateTimeOffset(2026, 11, 27, 11, 0, 0, TimeSpan.FromHours(7));
            Assert.AreEqual(5000m, _service.Calculate(atStart).RushFee);
        }

        // BVA-12 (BR4): Lunch rush ends exclusively at 13:00 UTC+7 (12:59 = 20% of D, 13:00 = 0).
        [TestMethod]
        public void BVA12_BR04_LunchRushEnd_1259On_1300Off()
        {
            PricingInput lastMinute = CreateBaseInput();
            lastMinute.PlacedAt = new DateTimeOffset(2026, 11, 27, 12, 59, 0, TimeSpan.FromHours(7));
            Assert.AreEqual(5000m, _service.Calculate(lastMinute).RushFee);

            PricingInput atEnd = CreateBaseInput();
            atEnd.PlacedAt = new DateTimeOffset(2026, 11, 27, 13, 0, 0, TimeSpan.FromHours(7));
            Assert.AreEqual(0m, _service.Calculate(atEnd).RushFee);
        }

        // BVA-13 (BR4): Dinner rush starts inclusively at 17:30 UTC+7 (17:29 = 0, 17:30 = 20% of D).
        [TestMethod]
        public void BVA13_BR04_DinnerRushStart_1729Off_1730On()
        {
            PricingInput before = CreateBaseInput();
            before.PlacedAt = new DateTimeOffset(2026, 11, 27, 17, 29, 0, TimeSpan.FromHours(7));
            Assert.AreEqual(0m, _service.Calculate(before).RushFee);

            PricingInput atStart = CreateBaseInput();
            atStart.PlacedAt = new DateTimeOffset(2026, 11, 27, 17, 30, 0, TimeSpan.FromHours(7));
            Assert.AreEqual(5000m, _service.Calculate(atStart).RushFee);
        }

        // BVA-14 (BR4): Dinner rush ends exclusively at 19:30 UTC+7 (19:29 = 20% of D, 19:30 = 0).
        [TestMethod]
        public void BVA14_BR04_DinnerRushEnd_1929On_1930Off()
        {
            PricingInput lastMinute = CreateBaseInput();
            lastMinute.PlacedAt = new DateTimeOffset(2026, 11, 27, 19, 29, 0, TimeSpan.FromHours(7));
            Assert.AreEqual(5000m, _service.Calculate(lastMinute).RushFee);

            PricingInput atEnd = CreateBaseInput();
            atEnd.PlacedAt = new DateTimeOffset(2026, 11, 27, 19, 30, 0, TimeSpan.FromHours(7));
            Assert.AreEqual(0m, _service.Calculate(atEnd).RushFee);
        }

        // EP-15 (BR4): UTC timestamp is converted to Vietnam time (UTC+7) before rush window check.
        [TestMethod]
        public void EP15_BR04_UtcTimestampConvertedToUtcPlus7_AppliesRushFee()
        {
            PricingInput utcRush = CreateBaseInput();
            utcRush.PlacedAt = new DateTimeOffset(2026, 11, 27, 4, 0, 0, TimeSpan.Zero); // 11:00 +07
            Assert.AreEqual(5000m, _service.Calculate(utcRush).RushFee);

            PricingInput utcAfterRush = CreateBaseInput();
            utcAfterRush.PlacedAt = new DateTimeOffset(2026, 11, 27, 6, 0, 0, TimeSpan.Zero); // 13:00 +07
            Assert.AreEqual(0m, _service.Calculate(utcAfterRush).RushFee);
        }

        // EP-16 (BR5): Heavy rain flag adds flat 10,000 VND when on and 0 when off.
        [TestMethod]
        public void EP16_BR05_HeavyRainOnAdds10000_OffAddsZero()
        {
            PricingInput dry = CreateBaseInput();
            dry.HeavyRain = false;
            Assert.AreEqual(0m, _service.Calculate(dry).RainFee);

            PricingInput rainy = CreateBaseInput();
            rainy.HeavyRain = true;
            Assert.AreEqual(10000m, _service.Calculate(rainy).RainFee);
        }

        // BVA-17 (BR6): Subtotal 299,999 VND still pays delivery fee D and rush fee R.
        [TestMethod]
        public void BVA17_BR06_Subtotal299999_StillChargesDeliveryAndRush()
        {
            PricingInput input = CreateBaseInput();
            input.Subtotal = 299999m;
            input.PlacedAt = new DateTimeOffset(2026, 11, 27, 11, 30, 0, TimeSpan.FromHours(7));

            PriceBreakdown result = _service.Calculate(input);
            Assert.AreEqual(25000m, result.DeliveryFee);
            Assert.AreEqual(5000m, result.RushFee);
        }

        // BVA-18 (BR6): Subtotal 300,000 VND waives D and R, while RainFee still applies.
        [TestMethod]
        public void BVA18_BR06_Subtotal300000_WaivesDeliveryAndRushButKeepsRainFee()
        {
            PricingInput input = CreateBaseInput();
            input.Subtotal = 300000m;
            input.PlacedAt = new DateTimeOffset(2026, 11, 27, 11, 30, 0, TimeSpan.FromHours(7));
            input.HeavyRain = true;

            PriceBreakdown result = _service.Calculate(input);
            Assert.AreEqual(0m, result.DeliveryFee);
            Assert.AreEqual(0m, result.RushFee);
            Assert.AreEqual(10000m, result.RainFee);
            Assert.AreEqual(280000m, result.PayableTotal);
        }

        // BVA-19 (BR7): Hub load 29% (< 30%) grants 10% Quiet Kitchen discount.
        [TestMethod]
        public void BVA19_BR07_HubLoad29Percent_GrantsQuietDiscount()
        {
            PricingInput input = CreateBaseInput();
            input.HubLoad = 0.29m;

            PriceBreakdown result = _service.Calculate(input);
            Assert.AreEqual(20000m, result.QuietDiscount);
        }

        // BVA-20 (BR7): Hub load exactly 30% receives no Quiet Kitchen discount.
        [TestMethod]
        public void BVA20_BR07_HubLoad30Percent_NoQuietDiscount()
        {
            PricingInput input = CreateBaseInput();
            input.HubLoad = 0.30m;

            PriceBreakdown result = _service.Calculate(input);
            Assert.AreEqual(0m, result.QuietDiscount);
        }

        // BVA-21 (BR8): Hub load 89% (< 90%) is accepted for a new order.
        [TestMethod]
        public void BVA21_BR08_HubLoad89Percent_AcceptsOrder()
        {
            PricingInput input = CreateBaseInput();
            input.HubLoad = 0.89m;

            PriceBreakdown result = _service.Calculate(input);
            Assert.AreEqual(200000m, result.Subtotal);
            Assert.AreEqual(225000m, result.PayableTotal);
        }

        // BVA-22 (BR8): Hub load 90% rejects new orders with BR8.
        [TestMethod]
        public void BVA22_BR08_HubLoad90Percent_ThrowsBR8()
        {
            PricingInput input = CreateBaseInput();
            input.HubLoad = 0.90m;

            var ex = Assert.ThrowsException<BusinessRuleException>(() => _service.Calculate(input));
            Assert.AreEqual("BR8", ex.Rule);
        }

        // BVA-23 (BR9): Voucher min subtotal is checked against S (99,999 rejects; 100,000 accepts even if S-Q < 100,000).
        [TestMethod]
        public void BVA23_BR09_VoucherMinSubtotal_99999Rejects_100000AcceptsOnS()
        {
            PricingInput below = CreateBaseInput();
            below.Subtotal = 99999m;
            below.Voucher = CreateVoucher(VoucherType.Percent, 10m, 30000m, 100000m);

            var ex = Assert.ThrowsException<BusinessRuleException>(() => _service.Calculate(below));
            Assert.AreEqual("BR9", ex.Rule);

            PricingInput exact = CreateBaseInput();
            exact.Subtotal = 100000m;
            exact.HubLoad = 0.20m; // Q = 10,000 -> S - Q = 90,000
            exact.Voucher = CreateVoucher(VoucherType.Percent, 10m, 30000m, 100000m);

            PriceBreakdown result = _service.Calculate(exact);
            Assert.AreEqual(10000m, result.QuietDiscount);
            Assert.AreEqual(9000m, result.VoucherDiscount);
            Assert.AreEqual(81000m, result.FoodTotal);
        }

        // BVA-24 (BR9): PERCENT voucher applies to (S - Q) and respects maximum cap.
        [TestMethod]
        public void BVA24_BR09_PercentVoucher_RespectsCapOnSubtotalMinusQuiet()
        {
            PricingInput input = CreateBaseInput();
            input.Subtotal = 200000m;
            input.HubLoad = 0.20m; // S - Q = 180,000 -> 20% = 36,000, capped at 25,000
            input.Voucher = CreateVoucher(VoucherType.Percent, 20m, 25000m, 100000m);

            PriceBreakdown result = _service.Calculate(input);
            Assert.AreEqual(25000m, result.VoucherDiscount);
            Assert.AreEqual(155000m, result.FoodTotal);
        }

        // BVA-25 (BR9): FIXED voucher is capped at (S - Q) so FoodTotal never drops below 0.
        [TestMethod]
        public void BVA25_BR09_FixedVoucher_CappedAtFoodAfterQuietDiscount()
        {
            PricingInput input = CreateBaseInput();
            input.Subtotal = 100000m;
            input.HubLoad = 0.20m; // S - Q = 90,000
            input.Voucher = CreateVoucher(VoucherType.Fixed, 150000m, 0m, 50000m);

            PriceBreakdown result = _service.Calculate(input);
            Assert.AreEqual(90000m, result.VoucherDiscount);
            Assert.AreEqual(0m, result.FoodTotal);
        }

        // EP-26 (BR9): Inactive, expired, or not-yet-valid vouchers are rejected with BR9.
        [TestMethod]
        public void EP26_BR09_InactiveExpiredOrFutureVoucher_ThrowsBR9()
        {
            PricingInput inactive = CreateBaseInput();
            inactive.Voucher = CreateVoucher(VoucherType.Fixed, 10000m);
            inactive.Voucher.Active = false;
            Assert.AreEqual("BR9", Assert.ThrowsException<BusinessRuleException>(() => _service.Calculate(inactive)).Rule);

            PricingInput expired = CreateBaseInput();
            expired.Voucher = CreateVoucher(VoucherType.Fixed, 10000m);
            expired.Voucher.ValidTo = expired.PlacedAt.AddSeconds(-1);
            Assert.AreEqual("BR9", Assert.ThrowsException<BusinessRuleException>(() => _service.Calculate(expired)).Rule);

            PricingInput future = CreateBaseInput();
            future.Voucher = CreateVoucher(VoucherType.Fixed, 10000m);
            future.Voucher.ValidFrom = future.PlacedAt.AddSeconds(1);
            Assert.AreEqual("BR9", Assert.ThrowsException<BusinessRuleException>(() => _service.Calculate(future)).Rule);
        }

        // EP-27 (BR10): Verifies full calculation sequence S -> Q -> V -> F -> D -> R -> RainFee -> Raw -> Round.
        [TestMethod]
        public void EP27_BR10_CalculationOrder_CombinedDiscountsAndSurcharges()
        {
            PricingInput input = CreateBaseInput();
            input.Subtotal = 250000m;
            input.HubLoad = 0.20m;      // Q = 25,000; S - Q = 225,000
            input.DistanceKm = 3.2m;    // D = 25,000
            input.PlacedAt = new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.FromHours(7)); // R = 5,000
            input.HeavyRain = true;     // RainFee = 10,000
            input.Voucher = CreateVoucher(VoucherType.Percent, 10m, 30000m, 100000m); // V = 22,500

            PriceBreakdown result = _service.Calculate(input);
            Assert.AreEqual(250000m, result.Subtotal);
            Assert.AreEqual(25000m, result.QuietDiscount);
            Assert.AreEqual(22500m, result.VoucherDiscount);
            Assert.AreEqual(202500m, result.FoodTotal);
            Assert.AreEqual(25000m, result.DeliveryFee);
            Assert.AreEqual(5000m, result.RushFee);
            Assert.AreEqual(10000m, result.RainFee);
            Assert.AreEqual(242500m, result.RawTotal);
            Assert.AreEqual(243000m, result.PayableTotal);
        }

        // BVA-28 (BR11): Rounds once at the end, half-up to nearest 1,000 (499 down, 500 up, 501 up; 64,950 -> 65,000).
        [TestMethod]
        public void BVA28_BR11_HalfUpRoundingOnceAtEnd_499Down_500Up_501Up()
        {
            Assert.AreEqual(64000m, PricingService.RoundPayable(64499m));
            Assert.AreEqual(65000m, PricingService.RoundPayable(64500m));
            Assert.AreEqual(65000m, PricingService.RoundPayable(64501m));

            // Spec example without intermediate rounding: S=55,500, Q=5,550, F=49,950, D=15,000 -> Raw=64,950 -> 65,000.
            PricingInput input = CreateBaseInput();
            input.Subtotal = 55500m;
            input.HubLoad = 0.20m;
            input.DistanceKm = 1.0m;

            PriceBreakdown result = _service.Calculate(input);
            Assert.AreEqual(5550m, result.QuietDiscount);
            Assert.AreEqual(49950m, result.FoodTotal);
            Assert.AreEqual(64950m, result.RawTotal);
            Assert.AreEqual(65000m, result.PayableTotal);
        }

        // BVA-29 (BR12): Order line counts (0/1/30/31), per-line quantity (0/1/20/21), and item availability.
        [TestMethod]
        public void BVA29_BR12_OrderLinesAndQuantityBoundaries_0_1_20_21_30_31_AndAvailability()
        {
            PricingInput zeroLines = CreateBaseInput();
            zeroLines.Quantities = new int[0];
            Assert.AreEqual("BR12", Assert.ThrowsException<BusinessRuleException>(() => _service.Calculate(zeroLines)).Rule);

            PricingInput oneLine = CreateBaseInput();
            oneLine.Quantities = new[] { 1 };
            Assert.AreEqual(205000m, _service.Calculate(oneLine).PayableTotal);

            PricingInput thirtyLines = CreateBaseInput();
            thirtyLines.Quantities = Enumerable.Repeat(1, 30).ToArray();
            Assert.AreEqual(205000m, _service.Calculate(thirtyLines).PayableTotal);

            PricingInput thirtyOneLines = CreateBaseInput();
            thirtyOneLines.Quantities = Enumerable.Repeat(1, 31).ToArray();
            Assert.AreEqual("BR12", Assert.ThrowsException<BusinessRuleException>(() => _service.Calculate(thirtyOneLines)).Rule);

            PricingInput zeroQty = CreateBaseInput();
            zeroQty.Quantities = new[] { 0 };
            Assert.AreEqual("BR12", Assert.ThrowsException<BusinessRuleException>(() => _service.Calculate(zeroQty)).Rule);

            PricingInput maxQty = CreateBaseInput();
            maxQty.Quantities = new[] { 20 };
            Assert.AreEqual(205000m, _service.Calculate(maxQty).PayableTotal);

            PricingInput overQty = CreateBaseInput();
            overQty.Quantities = new[] { 21 };
            Assert.AreEqual("BR12", Assert.ThrowsException<BusinessRuleException>(() => _service.Calculate(overQty)).Rule);

            PricingInput unavailable = CreateBaseInput();
            unavailable.AllItemsAvailableAtHub = false;
            Assert.AreEqual("BR12", Assert.ThrowsException<BusinessRuleException>(() => _service.Calculate(unavailable)).Rule);
        }

        // BVA-30 (BR14): COD accepts 1,000,000 VND and rejects 1,001,000 VND; online methods allow totals above 1,000,000.
        [TestMethod]
        public void BVA30_BR14_CodLimit1000000Accepts_1001000Rejects_OnlineAcceptsAbove()
        {
            PricingService.ValidatePayment(CustomerType.Local, PaymentMethod.Cod, 1000000m);
            PricingService.ValidatePayment(CustomerType.Tourist, PaymentMethod.Cod, 1000000m);

            var localOver = Assert.ThrowsException<BusinessRuleException>(() =>
                PricingService.ValidatePayment(CustomerType.Local, PaymentMethod.Cod, 1001000m));
            Assert.AreEqual("BR14", localOver.Rule);

            var touristOver = Assert.ThrowsException<BusinessRuleException>(() =>
                PricingService.ValidatePayment(CustomerType.Tourist, PaymentMethod.Cod, 1001000m));
            Assert.AreEqual("BR14", touristOver.Rule);

            PricingService.ValidatePayment(CustomerType.Local, PaymentMethod.MoMo, 1500000m);
            PricingService.ValidatePayment(CustomerType.Tourist, PaymentMethod.ApplePay, 1500000m);
        }
    }
}
