using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonBite.Domain;

namespace SaigonBite.Tests
{
    [TestClass]
    public class PricingSmokeTests
    {
        [TestMethod]
        public void Calculate_QuietOffPeakOrder_AppliesQuietDiscountAndRoundsTotal()
        {
            var service = new PricingService();
            var input = new PricingInput
            {
                Subtotal = 100000m,
                DistanceKm = 1.5m,
                HubLoad = 0.2m,
                Quantities = new[] { 1 },
                PlacedAt = new DateTimeOffset(2026, 11, 24, 15, 0, 0, TimeSpan.FromHours(7)),
                HeavyRain = false,
                AllItemsAvailableAtHub = true
            };

            PriceBreakdown result = service.Calculate(input);

            Assert.AreEqual(10000m, result.QuietDiscount);
            Assert.AreEqual(90000m, result.FoodTotal);
            Assert.AreEqual(15000m, result.DeliveryFee);
            Assert.AreEqual(105000m, result.PayableTotal);
        }

        [TestMethod]
        public void ValidatePayment_CodExceedsMillion_ThrowsBR14()
        {
            var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                PricingService.ValidatePayment(CustomerType.Local, PaymentMethod.Cod, 1001000m));

            Assert.AreEqual("BR14", ex.Rule);
        }
    }
}
