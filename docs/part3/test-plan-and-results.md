# Part 3 QA & Testing Report — UC01, Shared Pricing Engine, Payment Strategy, and Lifecycle Core

**Author:** Nguyễn Hải Triều (`524H0072`) | **Branch:** `feat/uc01-orders` | **Date:** 27 November 2026  
**Target Stack:** Tier 3 — ASP.NET MVC 5 (`.NET Framework 4.7.2`), Entity Framework 6.4.4 (Code First + Migrations), SQL Server 2022 Express (`.\SQLEXPRESS`), Bootstrap 5  
**Test Framework:** MSTest 2.2.10 (`vstest.console.exe` 16.11.0) + Domain Check Harness (`scripts/check-domain.ps1`)

---

## 3.4.1 Test Plan and Execution Summary

All 64 automated MSTest cases in `tests/SaigonBite.Tests` and all 53 checks in `tests/SaigonBite.Domain.Checks` were executed against the compiled solution (`0 Warnings, 0 Errors`) and live local SQL Server Express (`SaigonBiteDb`). Every database-backed test cleans up its own synthetic orders/hubs in a `finally` block, leaving the seeded database clean (`RemainingOrders = 0`).

### A. Core UC01, Payment, Lifecycle, Database, and Performance Test Cases

| Test ID | FR / BR / NFR | MSTest Method | Input Data | Expected Result | Actual Result | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :---: |
| TC00-1 | NFR02, NFR04 | `DatabaseSeedTests.MigrateAndSeed_PopulatesSqlExpressSchemaWithExpectedStarterData` | EF6 `InitialCreate` migration + `SaigonBiteSeeder` | 3 Hubs, 4 Brands, 12 HubBrands, 8 MenuItems, 2 Vouchers, 5 Users, 1 SystemSetting; Admin password verified via hash | All counts matched; `PasswordHasher.Verify("Saigon123!", hash) == true` (2 s) | Pass |
| TC01-1 | FR01, FR03, BR7, BR9 | `OrderServiceTests.Quote_AuthoritativeDbPricesWithQuietDiscountAndVoucher_CalculatesExpectedBreakdown` | Local customer, Hub 1, Com Tam + Banh Mi (`S=200,000`), `d=3.2 km`, `SAIGON15`, off-peak | `Q=20,000`, `V=27,000`, `F=153,000`, `D=25,000`, `Payable=178,000` | Exact breakdown returned from DB prices (40 ms) | Pass |
| TC01-2 | FR01, FR04, NFR05, NFR06 | `OrderServiceTests.PlaceOrder_LocalCustomerWithMoMo_SavesOrderLinesPaymentAndHistoryAtomically` | Local customer, 2 lines, `MoMo`, `ExpectedPayableTotal=205,000`, unique `RequestKey` | Single transaction persists `Order (Placed, Paid)`, 2 `OrderLines`, 1 `Charge` `PaymentRecord`, 1 `OrderStatusHistory (null -> Placed)` | All 4 tables committed atomically (109 ms) | Pass |
| TC01-3 | FR01, D05 | `OrderServiceTests.PlaceOrder_DuplicateRequestKey_ReturnsExistingOrderWithoutDoubleCharge` | Same `RequestKey` submitted twice for `VNPay` | Second call returns existing `Order.Id`; only 1 `Order` and 1 `PaymentRecord` exist | Returned original order; count = 1 (89 ms) | Pass |
| TC01-4 | FR03, D01 | `OrderServiceTests.PlaceOrder_StaleExpectedTotal_ThrowsQuoteChanged` | `ExpectedPayableTotal=150,000` vs server total `205,000` | Throws `BusinessRuleException("QUOTE_CHANGED")`; 0 rows persisted | Rejected with `QUOTE_CHANGED`; DB orders for key = 0 (13 ms) | Pass |
| TC02-1 | FR02, BR8 | `OrderServiceTests.CalculateHubLoad_CountsOnlyConfirmedAndPreparingOrders_MatchesBR8` | Temp hub (`MaxCapacity=10`) with 1 order each in `Placed, Confirmed, Preparing, Ready, Delivered, Cancelled` | Derived load = `2 / 10 = 0.20` (`Placed` and `Ready` excluded) | Returned `0.20m` (72 ms) | Pass |
| TC02-2 | FR02, BR8 | `OrderServiceTests.Quote_WhenHubLoadReaches90Percent_ThrowsBR8AndSuggestsAlternativeHubs` | Temp hub (`MaxCapacity=2`) with 1 `Confirmed` + 1 `Preparing` (`100% >= 90%`) | Throws `BusinessRuleException("BR8")` and suggests active alternative hubs | Threw `BR8` listing alternative hubs including District 1 (62 ms) | Pass |
| TC04-1 | FR04, BR13 | `PaymentStrategyTests.Select_LocalCustomer_AllowsMoMoVNPayAndCod` | `CustomerType.Local` with `MoMo`, `VNPay`, `Cod` (`205,000`) | Online returns `Paid`; COD returns `PendingCash` (`IsOnline=false`) | All 3 strategies matched expected status (< 1 ms) | Pass |
| TC04-2 | FR04, BR13 | `PaymentStrategyTests.Select_TouristCustomer_AllowsApplePayPayPalAndCod` | `CustomerType.Tourist` with `ApplePay`, `PayPal`, `Cod` (`320,000`) | Online returns `Paid`; COD returns `PendingCash` | All 3 strategies matched expected status (< 1 ms) | Pass |
| TC04-3 | FR04, BR13 | `PaymentStrategyTests.Select_WrongCustomerGroup_ThrowsBR13` | Local -> `ApplePay/PayPal`; Tourist -> `MoMo/VNPay` | All 4 cross-group attempts throw `BR13` | All 4 rejected with `BR13` (< 1 ms) | Pass |
| TC04-4 | FR04, BR13 | `OrderServiceTests.PlaceOrder_TouristWithMoMo_ThrowsBR13` | Tourist account submits `MoMo` to `OrderService.PlaceOrder` | Server rejects with `BR13` before persistence | Threw `BR13` (11 ms) | Pass |
| TC04-5 | FR04, BR14 | `PaymentStrategyTests.Cod_BoundaryAtOneMillion_AcceptsExactLimitAndRejectsAbove` | COD at `1,000,000` vs `1,001,000` via `PaymentSelector` and `CodStrategy` | `1,000,000` succeeds (`PendingCash`); `1,001,000` throws `BR14` | Exact boundary enforced (< 1 ms) | Pass |
| TC04-6 | FR07, BR16 | `PaymentStrategyTests.Refund_OnlineReturnsFullAmount_CodReturnsZero` | Refund `205,000` across all 4 online methods + COD | Online returns `Refunded` (`205,000`); COD returns `NoRefundNeeded` (`0`) | All 5 strategies matched (< 1 ms) | Pass |
| TC04-7 | NFR02 | `PaymentStrategyTests.PaymentSelector_InjectedCustomStrategy_ResolvesWithoutEditingExistingAdapters` | Inject custom `IPaymentStrategy` into `PaymentSelector` | Resolves and executes custom strategy without modifying existing adapters | Custom strategy resolved via OCP registry (< 1 ms) | Pass |
| TC05-1 | FR05, FR13, NFR04 | `OrderServiceTests.GetOrderDetails_OtherCustomer_ThrowsUnauthorized` | Local customer places order; Tourist customer requests details by ID | Server throws `BusinessRuleException("UNAUTHORIZED")` | Access denied with `UNAUTHORIZED` (31 ms) | Pass |
| TC06-1 | FR06, BR17 | `OrderEditTests.EditOrder_InPlaced_ReusesOriginalSnapshotEvenWhenSystemRainChanges` | Order placed with `HeavyRain=false`; Admin toggles rain ON; Customer edits qty 1 -> 2 | Status remains `Placed`; `HeavyRainSnapshot=false`, `RainFee=0`, original `PlacedAt` preserved | Snapshot reused; `RainFee=0`, `Subtotal=190,000` (648 ms) | Pass |
| TC06-2 | FR06, AMB05 | `OrderEditTests.EditOrder_OnlinePaidIncrease_RecordsAdditionalChargeAndKeepsPlaced` | `MoMo` order edited qty 1 -> 2 | Status stays `Placed`; appends `EditAdditionalCharge` `PaymentRecord` for positive delta | `EditAdditionalCharge` recorded with `Paid` (39 ms) | Pass |
| TC06-3 | FR06, AMB05 | `OrderEditTests.EditOrder_OnlinePaidDecrease_RecordsPartialRefundAndKeepsPlaced` | `VNPay` order edited qty 2 -> 1 | Status stays `Placed`; appends `EditPartialRefund` `PaymentRecord` for negative delta | `EditPartialRefund` recorded with `Refunded` (41 ms) | Pass |
| TC06-4 | FR06, BR17 | `OrderEditTests.EditOrder_InConfirmedStatus_ThrowsBR17` | Kitchen confirms order; customer attempts edit | Business layer throws `BusinessRuleException("BR17")` | Rejected with `BR17` (119 ms) | Pass |
| TC06-5 | FR06, BR14 | `OrderEditTests.EditOrder_CodExceedsMillionAfterEdit_ThrowsBR14` | COD order edited to 12x Mien Xao Cua (> `1,000,000`) | Throws `BR14`; original DB line qty (`1`) remains unchanged | Rejected with `BR14`; rollback verified (72 ms) | Pass |
| TC06-6 | FR06, NFR05 | `OrderEditTests.EditOrder_StaleRowVersion_ThrowsConcurrency` | Save two edits using the same original `RowVersion` token | First succeeds; second throws `BusinessRuleException("CONCURRENCY")` | Second edit rejected with `CONCURRENCY` (45 ms) | Pass |
| TC07-1 | FR07, BR16, NFR06 | `OrderTransitionTests.Transition_ConfirmedToCancelledByCustomerOnMoMo_RefundsFullAmountAtomically` | Customer cancels `Confirmed` MoMo order | Status -> `Cancelled`, `PaymentStatus -> Refunded`, `Refund` record + history saved atomically | Full simulated refund and audit persisted (34 ms) | Pass |
| TC17-1 | FR17, BR15, NFR06 | `OrderTransitionTests.Transition_PlacedToConfirmedByOwnHubKitchen_UpdatesStatusAndWritesHistory` | Hub 1 kitchen staff confirms Hub 1 `Placed` order | Status -> `Confirmed`; `OrderStatusHistory` records `Placed -> Confirmed`, actor, timestamp | Status + audit row verified (30 ms) | Pass |
| TC17-2 | FR17, BR14, NFR06 | `OrderTransitionTests.Transition_OutForDeliveryToDeliveredOnCodWithCashCollected_MarksPaid` | Assigned shipper delivers COD order with `CashCollected=true` | Status -> `Delivered`, `PaymentStatus -> Paid`, `CodCollected` record saved | Delivered + `CodCollected` verified (37 ms) | Pass |
| TN01-1 | NFR01 | `PerformanceTests.NFR01_PricingService_30LineOrderCalculation_CompletesUnder1500Ms` | 30-line `PricingInput` with Quiet + Percent Voucher + Rush + Rain (1 single + 100-run avg) | Complete calculation `< 1,500 ms` | `single = 0.002 ms`, `100-run avg = 0.001 ms` | Pass |
| TN01-2 | NFR01 | `PerformanceTests.NFR01_OrderService_QuoteWithSqlExpress_CompletesUnder1500Ms` | Full EF6 `OrderService.Quote` against `.\SQLEXPRESS` with 8 seeded dishes + `SAIGON15` + hub load query | End-to-end quote `< 1,500 ms` | Measured `7.38 ms` (detailed run) / `27 ms` (full suite) | Pass |
| SMK-1 | FR03, BR7, BR11 | `PricingSmokeTests.Calculate_QuietOffPeakOrder_AppliesQuietDiscountAndRoundsTotal` | `S=100,000`, `d=1.5 km`, `load=0.20`, off-peak | `Q=10,000`, `F=90,000`, `D=15,000`, `Payable=105,000` | Exact match (< 1 ms) | Pass |
| SMK-2 | FR04, BR14 | `PricingSmokeTests.ValidatePayment_CodExceedsMillion_ThrowsBR14` | Local, COD, `1,001,000` | Throws `BR14` | Threw `BR14` (< 1 ms) | Pass |

---

## 3.4.2 Test Design Techniques: Equivalence Partitioning (EP) & Boundary Value Analysis (BVA)

Every numeric threshold in **BR1–BR12 and BR14** was analyzed into invalid and valid equivalence partitions and tested at its exact boundary values in `tests/SaigonBite.Tests/PricingBoundaryTests.cs` (**30 explicit MSTest methods**, exceeding the required minimum of 20).

| Rule | Numeric Threshold / Condition | Equivalence Partitions (Invalid / Valid) | Exact Boundary Values Tested | Test IDs & MSTest Methods | Observed Status |
| :--- | :--- | :--- | :--- | :--- | :---: |
| **BR1** | Minimum pre-discount subtotal `S >= 50,000` | Invalid: `< 50,000`; Valid: `>= 50,000` | `49,999` (reject), `50,000` (accept) | `BVA-01` `BVA01_BR01_Subtotal49999_ThrowsBR1`<br>`BVA-02` `BVA02_BR01_Subtotal50000_Accepts` | Pass |
| **BR2** | Delivery coverage `0.0 <= d <= 8.0 km` | Invalid: `< 0`, `> 8.0`; Valid: `[0.0, 8.0]` | `-0.01` (reject), `0.0` (accept), `8.0` (accept), `8.01` (reject) | `BVA-03`..`BVA-06` `BVA03_BR02_DistanceNegative_ThrowsBR2` .. `BVA06_BR02_Distance8Point01Km_ThrowsBR2` | Pass |
| **BR3** | Tiered delivery fee `D` (`<=2`, `2<d<=5`, `5<d<=8`, ceil extra km) | Tier 1: `[0, 2.0]` (`15k`); Tier 2: `(2.0, 5.0]` (`15k + 5k*ceil(d-2)`); Tier 3: `(5.0, 8.0]` (`30k + 7k*ceil(d-5)`) | `2.0` (`15k`), `2.01` (`20k`), `5.0` (`30k`), `5.01` (`37k`), `8.0` (`51k`) | `BVA-07`..`BVA-10` `BVA07_BR03_Distance2Km_Tier1Charges15000` .. `BVA10_BR03_Distance5Point01Km_Tier3RoundsUpTo37000` | Pass |
| **BR4** | Rush-hour surcharge `R = 20% * D` in `[11:00,13:00)` or `[17:30,19:30)` UTC+7 | Off-peak: `<11:00`, `[13:00,17:30)`, `>=19:30`; Rush: `[11:00,13:00)`, `[17:30,19:30)`; UTC offset conversion | `10:59` (`0`), `11:00` (`5k`), `12:59` (`5k`), `13:00` (`0`), `17:29` (`0`), `17:30` (`5k`), `19:29` (`5k`), `19:30` (`0`), `04:00Z`/`06:00Z` | `BVA-11`..`EP-15` `BVA11_BR04_LunchRushStart_1059Off_1100On` .. `EP15_BR04_UtcTimestampConvertedToUtcPlus7_AppliesRushFee` | Pass |
| **BR5** | Heavy-rain surcharge `RainFee` | Flag Off (`0`); Flag On (`10,000`) | `false` (`0`), `true` (`10,000`) | `EP-16` `EP16_BR05_HeavyRainOnAdds10000_OffAddsZero` | Pass |
| **BR6** | Free delivery `S >= 300,000` waives `D` and `R`, keeps `RainFee` | Below threshold: `< 300,000`; Free delivery: `>= 300,000` | `299,999` (`D=25k, R=5k`), `300,000` (`D=0, R=0, RainFee=10k`) | `BVA-17` `BVA17_BR06_Subtotal299999_StillChargesDeliveryAndRush`<br>`BVA-18` `BVA18_BR06_Subtotal300000_WaivesDeliveryAndRushButKeepsRainFee` | Pass |
| **BR7** | Quiet Kitchen discount `Q = 10% * S` when `load < 30%` | Quiet: `[0%, 30%)`; Normal: `[30%, 90%)` | `0.29` (`Q=10%`), `0.30` (`Q=0`) | `BVA-19` `BVA19_BR07_HubLoad29Percent_GrantsQuietDiscount`<br>`BVA-20` `BVA20_BR07_HubLoad30Percent_NoQuietDiscount` | Pass |
| **BR8** | Hub overload protection `load >= 90%` rejects new orders | Available: `[0%, 90%)`; Overloaded: `>= 90%` | `0.89` (accept), `0.90` (reject `BR8`) | `BVA-21` `BVA21_BR08_HubLoad89Percent_AcceptsOrder`<br>`BVA-22` `BVA22_BR08_HubLoad90Percent_ThrowsBR8` | Pass |
| **BR9** | Voucher min subtotal on `S`, `PERCENT` on `S-Q` with cap, `FIXED` capped at `S-Q`, active & date window | Invalid: `S < min`, inactive, expired (`PlacedAt > ValidTo`), future (`PlacedAt < ValidFrom`); Valid: `PERCENT` capped/uncapped, `FIXED` capped at `S-Q` | `99,999` (reject), `100,000` (accept even when `S-Q=90,000`), percent cap `25k < 36k`, fixed `150k` capped at `90k`, `ValidTo = PlacedAt - 1s`, `ValidFrom = PlacedAt + 1s` | `BVA-23`..`EP-26` `BVA23_BR09_VoucherMinSubtotal_99999Rejects_100000AcceptsOnS` .. `EP26_BR09_InactiveExpiredOrFutureVoucher_ThrowsBR9` | Pass |
| **BR10** | Strict pipeline order `S -> Q -> V -> F -> D -> R -> RainFee -> Raw -> Round` | Combined scenario exercising all 9 steps together | `S=250k, Q=25k, V=22.5k, F=202.5k, D=25k, R=5k, Rain=10k -> Raw=242,500 -> Payable=243,000` | `EP-27` `EP27_BR10_CalculationOrder_CombinedDiscountsAndSurcharges` | Pass |
| **BR11** | Single final half-up rounding to nearest `1,000` | Remainder `[0, 499]` rounds down; `[500, 999]` rounds up; no intermediate rounding | `64,499 -> 64,000`, `64,500 -> 65,000`, `64,501 -> 65,000`; `S=55,500 -> Raw=64,950 -> 65,000` | `BVA-28` `BVA28_BR11_HalfUpRoundingOnceAtEnd_499Down_500Up_501Up` | Pass |
| **BR12** | Order lines `1..30`, qty `1..20`, item available at hub | Lines: `0`, `[1,30]`, `>=31`; Qty: `<=0`, `[1,20]`, `>=21`; Available: `false`/`true` | Lines `0, 1, 30, 31`; Qty `0, 1, 20, 21`; `AllItemsAvailableAtHub = false` | `BVA-29` `BVA29_BR12_OrderLinesAndQuantityBoundaries_0_1_20_21_30_31_AndAvailability` | Pass |
| **BR14** | COD cap `<= 1,000,000` on rounded payable total | COD valid: `<= 1,000,000`; COD invalid: `> 1,000,000`; Online valid: `> 1,000,000` | COD `1,000,000` (Local & Tourist accept), COD `1,001,000` (reject `BR14`), MoMo/ApplePay `1,500,000` (accept) | `BVA-30` `BVA30_BR14_CodLimit1000000Accepts_1001000Rejects_OnlineAcceptsAbove` | Pass |

---

## 3.4.3 Invalid Order-State Transition Tests (`BR15` — 6 Business-Layer Tests)

All 6 negative transition tests in `tests/SaigonBite.Tests/OrderTransitionTests.cs` invoke `OrderTransitionService.Transition(...)` directly against persisted SQL Server Express orders, bypassing all MVC views and controllers. Every test verifies both (a) `BusinessRuleException("BR15")` is thrown and (b) the database order status and `OrderStatusHistories` count remain unchanged.

| Test ID | Invalid Transition Attempted | Actor & Context | Expected Behavior (`BR15`) | Actual Result | Status |
| :--- | :--- | :--- | :--- | :--- | :---: |
| INV-01 | `Preparing -> Cancelled` | Owning Customer (`local@saigonbite.vn`) | Reject `BR15` (BR16 forbids cancellation after Preparing); DB stays `Preparing`, 0 history rows added | Threw `BR15`; DB unchanged (35 ms) | Pass |
| INV-02 | `Ready -> Delivered` | Assigned Shipper (`shipper1@saigonbite.vn`) | Reject `BR15` (cannot skip `OutForDelivery`); DB stays `Ready` | Threw `BR15`; DB unchanged (18 ms) | Pass |
| INV-03 | `Delivered -> Placed` | System Admin (`admin@saigonbite.vn`) | Reject `BR15` (`Delivered` is terminal); DB stays `Delivered` | Threw `BR15`; DB unchanged (18 ms) | Pass |
| INV-04 | `Placed -> Preparing` | Own-hub Kitchen Staff (`kitchen.d1@saigonbite.vn`) | Reject `BR15` (cannot skip `Confirmed`); DB stays `Placed` | Threw `BR15`; DB unchanged (16 ms) | Pass |
| INV-05 | `OutForDelivery -> Delivered` | Assigned Shipper on COD order with `CashCollected = false` | Reject `BR15` / `BR14` (COD cash collection not confirmed); DB stays `OutForDelivery` | Threw `BR15`; DB unchanged (16 ms) | Pass |
| INV-06 | `Placed -> Cancelled` | Own-hub Kitchen Staff with blank `Reason = "   "` | Reject `BR15` / `BR16` (kitchen rejection requires non-empty reason); DB stays `Placed` | Threw `BR15`; DB unchanged (17 ms) | Pass |

---

## 3.4.4 Execution Evidence

### 1. MSTest Runner Output (`vstest.console.exe` on 27 November 2026)

```text
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
  Passed MigrateAndSeed_PopulatesSqlExpressSchemaWithExpectedStarterData [2 s]
  ... (all 64 test methods passed, 0 failed, 0 skipped) ...
  Passed BVA01_BR01_Subtotal49999_ThrowsBR1 [< 1 ms]
  ...
  Passed BVA30_BR14_CodLimit1000000Accepts_1001000Rejects_OnlineAcceptsAbove [< 1 ms]
  Passed NFR01_PricingService_30LineOrderCalculation_CompletesUnder1500Ms [1 ms]
  Passed NFR01_OrderService_QuoteWithSqlExpress_CompletesUnder1500Ms [27 ms]

Test Run Successful.
Total tests: 64
     Passed: 64
 Total time: 5.7322 Seconds
```

### 2. NFR01 Detailed Timing Output (`PerformanceTests`)

```text
NFR01 PricingService 30-line timing: single=0.002 ms, 100-run avg=0.001 ms (limit: 1500 ms)
NFR01 OrderService.Quote (SQL Server Express): 7.38 ms (limit: 1500 ms)
```

### 3. Domain Rule Verification Harness (`scripts/check-domain.ps1`)

```text
RESULT 53 passed; 0 failed
```

### 4. Post-Test Database Cleanliness (`sqlcmd -S .\SQLEXPRESS -E -d SaigonBiteDb`)

```text
Hubs RemainingOrders MenuItems IsHeavyRainOn
---- --------------- --------- -------------
3    0               8         0
```

> **Note for Final Consolidated PDF:** Capture a screenshot of Visual Studio Test Explorer (or PowerShell `vstest.console.exe` pass summary) after merging all three team branches (`feat/uc01-orders`, `feature/uc02-menu`, `feature/uc03-ui`) and embed it in Section 3.4.4 of the final PDF report.
