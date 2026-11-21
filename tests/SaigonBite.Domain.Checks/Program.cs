using System;
using System.Collections.Generic;
using SaigonBite.Domain;

// Initial executable checks. This is not a substitute for the final unit-test runner/report.
internal static class Program
{
    private static int passed, failed;
    private static readonly PricingService Service = new PricingService();
    private static PricingInput Input()
    {
        return new PricingInput { Subtotal = 200000m, DistanceKm = 3.2m, HubLoad = .2m,
            Quantities = new[] { 1 }, PlacedAt = new DateTimeOffset(2026, 10, 30, 15, 0, 0, TimeSpan.FromHours(7)),
            AllItemsAvailableAtHub = true };
    }
    private static PriceBreakdown Price(Action<PricingInput> change)
    { var input = Input(); change(input); return Service.Calculate(input); }
    private static void Equal(decimal expected, decimal actual)
    { if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual); }
    private static void True(bool actual) { if (!actual) throw new Exception("Expected true"); }
    private static void Reject(string rule, Action action)
    {
        try { action(); }
        catch (BusinessRuleException e) { if (e.Rule == rule) return; throw; }
        throw new Exception("Expected rejection " + rule);
    }
    private static void Check(string id, Action body)
    {
        try { body(); passed++; Console.WriteLine("PASS " + id); }
        catch (Exception e) { failed++; Console.WriteLine("FAIL " + id + ": " + e.Message); }
    }
    private static VoucherInput Voucher(VoucherType type, decimal value, decimal cap = 30000m)
    {
        return new VoucherInput { Type = type, Value = value, Cap = cap, Active = true, MinimumSubtotal = 100000m,
            ValidFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ValidTo = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero) };
    }
    private static int Main()
    {
        Check("EXAMPLE-BR10-205000", () => Equal(205000m, Service.Calculate(Input()).PayableTotal));
        Check("BR1-49999-reject", () => Reject("BR1", () => Price(x => x.Subtotal = 49999m)));
        Check("BR1-50000-accept", () => Equal(50000m, Price(x => x.Subtotal = 50000m).Subtotal));
        Check("BR2-8-accept", () => Equal(51000m, Price(x => x.DistanceKm = 8m).DeliveryFee));
        Check("BR2-8.01-reject", () => Reject("BR2", () => Price(x => x.DistanceKm = 8.01m)));
        Check("BR3-2", () => Equal(15000m, Price(x => x.DistanceKm = 2m).DeliveryFee));
        Check("BR3-2.01", () => Equal(20000m, Price(x => x.DistanceKm = 2.01m).DeliveryFee));
        Check("BR3-5", () => Equal(30000m, Price(x => x.DistanceKm = 5m).DeliveryFee));
        Check("BR3-5.01", () => Equal(37000m, Price(x => x.DistanceKm = 5.01m).DeliveryFee));
        foreach (var pair in new[] { Tuple.Create(10,59,0m), Tuple.Create(11,0,5000m), Tuple.Create(12,59,5000m), Tuple.Create(13,0,0m), Tuple.Create(17,29,0m), Tuple.Create(17,30,5000m), Tuple.Create(19,29,5000m), Tuple.Create(19,30,0m) })
        {
            var p = pair;
            Check("BR4-" + p.Item1 + ":" + p.Item2, () => Equal(p.Item3, Price(x => x.PlacedAt = new DateTimeOffset(2026,10,30,p.Item1,p.Item2,0,TimeSpan.FromHours(7))).RushFee));
        }
        Check("BR4-offset-conversion", () => Equal(5000m, Price(x => x.PlacedAt = new DateTimeOffset(2026,10,30,4,0,0,TimeSpan.Zero)).RushFee));
        Check("BR5-rain", () => Equal(10000m, Price(x => x.HeavyRain = true).RainFee));
        Check("BR6-299999-fee", () => Equal(25000m, Price(x => x.Subtotal = 299999m).DeliveryFee));
        Check("BR6-300000-waives-rush-not-rain", () => { var r = Price(x => { x.Subtotal=300000m; x.HeavyRain=true; x.PlacedAt=x.PlacedAt.AddHours(-4); }); Equal(0m,r.DeliveryFee+r.RushFee); Equal(10000m,r.RainFee); });
        Check("BR7-29-percent", () => Equal(20000m, Price(x => x.HubLoad=.29m).QuietDiscount));
        Check("BR7-30-percent", () => Equal(0m, Price(x => x.HubLoad=.30m).QuietDiscount));
        Check("BR8-89-percent", () => Equal(200000m, Price(x => x.HubLoad=.89m).Subtotal));
        Check("BR8-90-percent-reject", () => Reject("BR8", () => Price(x => x.HubLoad=.90m)));
        Check("BR9-minimum-99999", () => Reject("BR9", () => Price(x => {x.Subtotal=99999m;x.Voucher=Voucher(VoucherType.Percent,10m);} )));
        Check("BR9-minimum-on-S-100000", () => Equal(9000m, Price(x => {x.Subtotal=100000m;x.Voucher=Voucher(VoucherType.Percent,10m);}).VoucherDiscount));
        Check("BR9-percent-cap", () => Equal(10000m, Price(x => x.Voucher=Voucher(VoucherType.Percent,10m,10000m)).VoucherDiscount));
        Check("BR9-fixed-food-cap", () => Equal(180000m, Price(x => x.Voucher=Voucher(VoucherType.Fixed,999999m)).VoucherDiscount));
        Check("BR9-inactive", () => Reject("BR9", () => Price(x => {x.Voucher=Voucher(VoucherType.Fixed,10000m);x.Voucher.Active=false;})));
        Check("BR9-expired", () => Reject("BR9", () => Price(x => {x.Voucher=Voucher(VoucherType.Fixed,10000m);x.Voucher.ValidTo=x.PlacedAt.AddSeconds(-1);} )));
        Check("BR11-below-half", () => Equal(64000m, PricingService.RoundPayable(64499m)));
        Check("BR11-half-up", () => Equal(65000m, PricingService.RoundPayable(64500m)));
        Check("BR11-above-half", () => Equal(65000m, PricingService.RoundPayable(64501m)));
        Check("BR12-zero-lines", () => Reject("BR12", () => Price(x => x.Quantities=new int[0])));
        Check("BR12-30-lines", () => { var q=new int[30];for(int i=0;i<q.Length;i++)q[i]=1;Price(x=>x.Quantities=q); });
        Check("BR12-31-lines", () => Reject("BR12", () => Price(x => x.Quantities=new int[31])));
        Check("BR12-quantity-zero", () => Reject("BR12", () => Price(x => x.Quantities=new[]{0})));
        Check("BR12-quantity-20", () => {Price(x => x.Quantities=new[]{20});});
        Check("BR12-quantity-21", () => Reject("BR12", () => Price(x => x.Quantities=new[]{21})));
        Check("BR12-unavailable", () => Reject("BR12", () => Price(x => x.AllItemsAvailableAtHub=false)));
        Check("BR13-local-PayPal-reject", () => Reject("BR13", () => PricingService.ValidatePayment(CustomerType.Local,PaymentMethod.PayPal,100000m)));
        Check("BR13-tourist-MoMo-reject", () => Reject("BR13", () => PricingService.ValidatePayment(CustomerType.Tourist,PaymentMethod.MoMo,100000m)));
        Check("BR14-COD-1000000", () => PricingService.ValidatePayment(CustomerType.Local,PaymentMethod.Cod,1000000m));
        Check("BR14-COD-1001000-reject", () => Reject("BR14", () => PricingService.ValidatePayment(CustomerType.Local,PaymentMethod.Cod,1001000m)));
        var kitchen=new TransitionContext {Role=ActorRole.KitchenStaff,OwnHub=true};
        var customer=new TransitionContext {Role=ActorRole.Customer,OwnsOrder=true};
        var shipper=new TransitionContext {Role=ActorRole.Shipper,AssignedShipper=true,IsCod=true};
        Check("STATE-Placed-Confirmed-valid", () => True(OrderLifecycle.CanTransition(OrderStatus.Placed,OrderStatus.Confirmed,kitchen)));
        Check("STATE-Preparing-Cancelled-invalid", () => True(!OrderLifecycle.CanTransition(OrderStatus.Preparing,OrderStatus.Cancelled,customer)));
        Check("STATE-Ready-Delivered-invalid", () => True(!OrderLifecycle.CanTransition(OrderStatus.Ready,OrderStatus.Delivered,shipper)));
        Check("STATE-Delivered-Placed-invalid", () => True(!OrderLifecycle.CanTransition(OrderStatus.Delivered,OrderStatus.Placed,kitchen)));
        Check("STATE-Placed-Preparing-invalid", () => True(!OrderLifecycle.CanTransition(OrderStatus.Placed,OrderStatus.Preparing,kitchen)));
        Check("STATE-COD-uncollected-invalid", () => True(!OrderLifecycle.CanTransition(OrderStatus.OutForDelivery,OrderStatus.Delivered,shipper)));
        Check("STATE-kitchen-reject-needs-reason", () => True(!OrderLifecycle.CanTransition(OrderStatus.Placed,OrderStatus.Cancelled,kitchen)));
        Check("STATE-unassigned-shipper-invalid", () => True(!OrderLifecycle.CanTransition(OrderStatus.Ready,OrderStatus.OutForDelivery,new TransitionContext {Role=ActorRole.Shipper})));
        // UC03: tải bếp (FR14.2, BR7, BR8). Kiểm tra biên 29/30/89/90% và các đơn không được tính.
        Check("HUBLOAD-0-orders", () => Equal(0m, HubLoad.Calculate(0, 20)));
        Check("HUBLOAD-29pct-quiet", () => True(HubLoad.Classify(HubLoad.Calculate(29, 100)) == HubLoadLevel.Quiet));
        Check("HUBLOAD-30pct-normal", () => True(HubLoad.Classify(HubLoad.Calculate(30, 100)) == HubLoadLevel.Normal));
        Check("HUBLOAD-89pct-normal", () => True(HubLoad.Classify(HubLoad.Calculate(89, 100)) == HubLoadLevel.Normal));
        Check("HUBLOAD-90pct-full", () => True(HubLoad.Classify(HubLoad.Calculate(18, 20)) == HubLoadLevel.Full));
        Check("HUBLOAD-over-100pct", () => Equal(1.5m, HubLoad.Calculate(30, 20)));
        Check("HUBLOAD-capacity-0-reject", () => Reject("FR14.1", () => HubLoad.Calculate(1, 0)));
        Check("HUBLOAD-capacity-negative-reject", () => Reject("FR14.1", () => HubLoad.Calculate(1, -1)));
        Check("HUBLOAD-capacity-1-valid", () => Equal(1m, HubLoad.Calculate(1, 1)));
        Check("HUBLOAD-counts-confirmed-preparing", () => True(HubLoad.CountsTowardLoad(OrderStatus.Confirmed) && HubLoad.CountsTowardLoad(OrderStatus.Preparing)));
        Check("HUBLOAD-ignores-others", () => True(!HubLoad.CountsTowardLoad(OrderStatus.Placed) && !HubLoad.CountsTowardLoad(OrderStatus.Ready) && !HubLoad.CountsTowardLoad(OrderStatus.Cancelled) && !HubLoad.CountsTowardLoad(OrderStatus.Delivered)));
        Check("HUBLOAD-pricing-uses-90-threshold", () => Reject("BR8", () => Price(x => x.HubLoad = HubLoad.Calculate(18, 20))));
        Console.WriteLine("RESULT " + passed + " passed; " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
