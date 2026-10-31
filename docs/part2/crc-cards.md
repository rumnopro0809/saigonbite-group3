# 2.4.1 CRC Cards

Group 3. AI-assisted planning cards for designated owners to review. Responsibilities below describe the intended design, not completed implementation. Only pure PricingService and OrderLifecycle guards have been implemented so far.

## CRC01 OrderService

Owner 524H0072; FR01–FR07/13/17, BR1–BR17, NFR05/06.

| Responsibilities | Collaborators |
|---|---|
| Coordinate authoritative placement validation and calculation | MenuService, HubLoadService, PricingService |
| Read only owned orders; edit only Placed with original context | ActorContext, Order, PriceSnapshot |
| Cancel only allowed states and mock-refund online payments | OrderLifecycle, PaymentSelector, PaymentRecord |
| Save order/lines/payment/history atomically; reject stale or duplicate writes | DbContext/unit of work, rowversion, request key |

No page rendering or duplicated pricing formulas. Server-side identity/state checks are mandatory even when the UI hides actions.

## CRC02 PricingService

Owner 524H0072; FR01–FR04/06, BR1–BR14.

| Responsibilities | Collaborators |
|---|---|
| Validate subtotal/distance/load/line/quantity eligibility | Authoritative PricingInput from OrderService |
| Calculate S/Q/V/F/D/R/RainFee in order and round once | Original placement context, VoucherInput/evaluator |
| Return decimal price breakdown without external side effects | PriceBreakdown |

No HTTP/database/payment calls. The server caller computes the subtotal from actual item prices, not posted customer totals.

## CRC03 PaymentSelector and IPaymentStrategy

Owner 524H0072; FR04/07, BR13/14/16, NFR02.

| Responsibilities | Collaborators |
|---|---|
| Reject method/customer mismatch; select registered adapter | Customer, LocalCustomer, ForeignTourist, strategy registry |
| Define stable charge/refund contract, result and correlation data | IPaymentStrategy, PaymentResult, PaymentRecord |

Concrete strategies: MoMoStrategy, VNPayStrategy, ApplePayStrategy, PayPalStrategy, CodStrategy. All simulate in Tier 3. COD initially awaits collection; it is not a completed online charge. New methods add a class and registration without editing existing strategy classes.

## CRC04 Order and PriceSnapshot

Owner 524H0072; FR05/06/17, BR15/17.

| Responsibilities | Collaborators |
|---|---|
| Retain customer/hub/lines/status and original time/load/rain | Customer, Hub, OrderLine, PriceSnapshot |
| Preserve item/category/unit-price and monetary history | OrderLine snapshots, PriceBreakdown |
| Supply tracking/report data without arbitrary public state writes | OrderService, transition service, report queries |

Cart is separate from the eight Order states. Editing does not replace saved conditions with current load/rain.

## CRC05 OrderTransitionService

Team integration owners 524H0072/524H0188; FR13/17, BR14–BR16, NFR04/06.

| Responsibilities | Collaborators |
|---|---|
| Default-deny unlisted transitions; check actor/hub/customer/shipper scope | OrderLifecycle, Order, authenticated ActorContext |
| Require rejection reason and COD cash before Delivered | Transition request, payment/cash evidence |
| Append actor/from/to/time/reason with the state mutation | OrderStatusHistory, transaction boundary |

No Preparing cancellation, Ready-to-Delivered shortcut or outgoing transition from terminal states. Shipper assignment remains AMB01.

## CRC06 MenuService and catalog objects

Owner 524H0189; FR08–FR11/13, BR12; R1 data dependency.

| Responsibilities | Collaborators |
|---|---|
| Create/read/edit brands/items, categories/prices and availability | Brand, MenuItem, DbContext |
| Scope browsing to selected hub brands | HubBrand, Hub |
| Soft-delete while retaining historical references and prices | Flags, OrderLine snapshots |
| Check Admin authorization and stale edits | ActorContext, rowversion |

Catalog changes never rewrite old order lines; order placement rechecks item eligibility.

## CRC07 HubService and HubLoadService

Owner 524H0188; FR14.1–FR14.4/02/16/19, BR5/7/8/17.

| Responsibilities | Collaborators |
|---|---|
| CRUD hub/location/positive integer capacity/supported brands | Hub, HubBrand, Brand, DbContext |
| Derive Confirmed/Preparing count divided by capacity | Order query, Hub |
| Keep acceptance, discount and R2 load definitions consistent | OrderService, PricingService, HubReportService |
| Deactivate without deleting orders; manage separate rain setting | Flags, SystemSetting |

Load is not manually edited. Capacity/rain changes do not modify old placement snapshots. Positive capacity and inactive-hub handling are documented team proposals.

## CRC08 VoucherService

Proposed support owner 524H0189, integration 524H0072; FR15, BR9.

| Responsibilities | Collaborators |
|---|---|
| CRUD type/value/cap/minimum/validity/active state | Voucher, DbContext |
| Supply authoritative terms for one eligible voucher | OrderService, PricingService/evaluator |
| Retain applied historical values after deactivation | Order, PriceSnapshot |

Minimum uses S; percent uses S−Q; fixed discount is capped by S−Q. Voucher validity on edits remains an explicitly unresolved policy in architecture.md.

## CRC09 RevenueReportService

Proposed owner 524H0072, category review 524H0189; FR18/R1, AMB04.

| Responsibilities | Collaborators |
|---|---|
| Aggregate date/category/payment-method revenue and exclude Cancelled/Failed | Order/Line/Payment snapshots and delivery timestamp |
| Reconcile food discounts, fees and rounding | Allocation policy and fixture data |

No recalculation from current menu prices. Recognition and allocation conventions remain team proposals until confirmed.

## CRC10 HubReportService

Proposed owner 524H0188; FR19/R2, BR7/8, AMB06.

| Responsibilities | Collaborators |
|---|---|
| Calculate handled count and preparation mean under declared definitions | StatusHistory, Confirmed/Ready timestamps |
| Return current load consistent with order acceptance | HubLoadService |
| Count granted Q from snapshots; label cancellation treatment | PriceSnapshot |
| Distinguish missing samples from a true zero duration | Report ViewModel |

## Object collaboration rehearsal

Placement: Controller → OrderService → Menu/HubLoad checks → PricingService → PaymentSelector/strategy → transaction storing Order/Lines/Snapshot/Payment/History. Edit reuses the snapshot; cancel applies state/refund invariants. Kitchen/shipper actions use OrderTransitionService. Reports read retained financial/lifecycle data. Each owner explains one local responsibility and one cross-UC collaboration in the Design Review.
