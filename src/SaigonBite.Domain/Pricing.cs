using System;

namespace SaigonBite.Domain
{
    public enum CustomerType { Local, Tourist }
    public enum PaymentMethod { MoMo, VNPay, ApplePay, PayPal, Cod }
    public enum VoucherType { Percent, Fixed }

    public sealed class VoucherInput
    {
        public VoucherType Type { get; set; }
        public decimal Value { get; set; }
        public decimal Cap { get; set; }
        public decimal MinimumSubtotal { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset ValidFrom { get; set; }
        public DateTimeOffset ValidTo { get; set; }
    }

    // FR06 / BR17: callers supply the saved placement context on edits.
    public sealed class PricingInput
    {
        public decimal Subtotal { get; set; }
        public decimal DistanceKm { get; set; }
        public decimal HubLoad { get; set; }
        public int[] Quantities { get; set; }
        public DateTimeOffset PlacedAt { get; set; }
        public bool HeavyRain { get; set; }
        public bool AllItemsAvailableAtHub { get; set; }
        public VoucherInput Voucher { get; set; }
    }

    public sealed class PriceBreakdown
    {
        public decimal Subtotal { get; internal set; }
        public decimal QuietDiscount { get; internal set; }
        public decimal VoucherDiscount { get; internal set; }
        public decimal FoodTotal { get; internal set; }
        public decimal DeliveryFee { get; internal set; }
        public decimal RushFee { get; internal set; }
        public decimal RainFee { get; internal set; }
        public decimal RawTotal { get; internal set; }
        public decimal PayableTotal { get; internal set; }
    }

    public sealed class BusinessRuleException : Exception
    {
        public string Rule { get; private set; }
        public BusinessRuleException(string rule, string message) : base(message) { Rule = rule; }
    }

    // FR01-FR04, BR1-BR14. Pure decimal calculation, no database or HTTP state.
    // Persistence must compute Subtotal from authoritative item prices, not a posted client total.
    public sealed class PricingService
    {
        public PriceBreakdown Calculate(PricingInput input)
        {
            if (input == null) throw new ArgumentNullException("input");
            Require(input.Subtotal >= 50000m, "BR1", "Minimum subtotal is VND 50,000.");
            Require(input.DistanceKm >= 0m && input.DistanceKm <= 8m, "BR2", "Distance must be between 0 and 8 km.");
            Require(input.HubLoad >= 0m && input.HubLoad < .9m, "BR8", "Hub is unavailable for a new order.");
            Require(input.Quantities != null && input.Quantities.Length >= 1 && input.Quantities.Length <= 30, "BR12", "Use 1-30 order lines.");
            foreach (int quantity in input.Quantities)
                Require(quantity >= 1 && quantity <= 20, "BR12", "Quantity must be 1-20 per line.");
            Require(input.AllItemsAvailableAtHub, "BR12", "Item is unavailable or not served by this hub.");

            var result = new PriceBreakdown { Subtotal = input.Subtotal };
            result.QuietDiscount = input.HubLoad < .3m ? input.Subtotal * .1m : 0m;
            decimal baseFood = input.Subtotal - result.QuietDiscount;
            if (input.Voucher != null)
            {
                var v = input.Voucher;
                Require(v.Active && input.PlacedAt >= v.ValidFrom && input.PlacedAt <= v.ValidTo && input.Subtotal >= v.MinimumSubtotal,
                    "BR9", "Voucher is inactive, outside validity or below minimum subtotal.");
                Require(v.Value >= 0m && v.Cap >= 0m, "BR9", "Voucher value and cap must not be negative.");
                if (v.Type == VoucherType.Percent)
                {
                    Require(v.Value <= 100m, "BR9", "Percentage must be at most 100.");
                    result.VoucherDiscount = Math.Min(baseFood * v.Value / 100m, v.Cap);
                }
                else if (v.Type == VoucherType.Fixed) result.VoucherDiscount = Math.Min(v.Value, baseFood);
                else throw new BusinessRuleException("BR9", "Unsupported voucher type.");
            }
            result.FoodTotal = baseFood - result.VoucherDiscount;
            if (input.Subtotal < 300000m)
            {
                decimal d = input.DistanceKm;
                result.DeliveryFee = d <= 2m ? 15000m : d <= 5m
                    ? 15000m + 5000m * Math.Ceiling(d - 2m)
                    : 30000m + 7000m * Math.Ceiling(d - 5m);
                TimeSpan time = input.PlacedAt.ToOffset(TimeSpan.FromHours(7)).TimeOfDay;
                bool rush = InWindow(time, 11, 0, 13, 0) || InWindow(time, 17, 30, 19, 30);
                result.RushFee = rush ? result.DeliveryFee * .2m : 0m;
            }
            result.RainFee = input.HeavyRain ? 10000m : 0m;
            result.RawTotal = result.FoodTotal + result.DeliveryFee + result.RushFee + result.RainFee;
            result.PayableTotal = RoundPayable(result.RawTotal);
            return result;
        }

        public static decimal RoundPayable(decimal raw)
        {
            if (raw < 0m) throw new ArgumentOutOfRangeException("raw");
            return Math.Round(raw / 1000m, 0, MidpointRounding.AwayFromZero) * 1000m;
        }

        public static void ValidatePayment(CustomerType customer, PaymentMethod method, decimal roundedTotal)
        {
            bool allowed = customer == CustomerType.Local
                ? method == PaymentMethod.MoMo || method == PaymentMethod.VNPay || method == PaymentMethod.Cod
                : customer == CustomerType.Tourist && (method == PaymentMethod.ApplePay || method == PaymentMethod.PayPal || method == PaymentMethod.Cod);
            Require(allowed, "BR13", "Payment method is not allowed for this customer type.");
            Require(method != PaymentMethod.Cod || roundedTotal <= 1000000m, "BR14", "COD exceeds VND 1,000,000.");
        }

        private static bool InWindow(TimeSpan time, int sh, int sm, int eh, int em)
        { return time >= new TimeSpan(sh, sm, 0) && time < new TimeSpan(eh, em, 0); }
        private static void Require(bool condition, string rule, string message)
        { if (!condition) throw new BusinessRuleException(rule, message); }
    }
}
