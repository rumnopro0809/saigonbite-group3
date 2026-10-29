# 2.2 Use Case Specifications

Design baseline; AI-assisted drafts for the designated owners to review. Main UC ownership and IDs are unchanged from Part 1.

## 2.2.1 UC01 Place and Manage Orders

**Owner:** Nguyễn Hải Triều — 524H0072. **Primary actor:** Customer, specialized into Local Customer and Foreign Tourist. **Collaborators:** payment strategy (simulated), hub/menu/voucher services; kitchen and shipper operations are HT03.

**References:** FR01–FR07; shared FR09, FR13, FR15–FR17; BR1–BR17. Reporting consumers: FR18/FR19. Assumptions AMB01–AMB06 where stated.

**Trigger:** customer selects a hub and starts an order, opens order history or requests an allowed edit/cancellation.

**Preconditions:** account is authenticated; hub/catalog data exists; the customer is authorized for any existing order; the requested hub is active for a new order. An unsaved cart is not a persisted Order state.

### Main flow Create

1. Customer selects an active hub and browses its supported brands and Available items (FR09/BR12).
2. Customer adds 1–30 lines with 1–20 units per line, enters a delivery address/distance and optionally one voucher. Tourist addresses may include a hotel/room.
3. System reads placement time, global rain flag, current Confirmed/Preparing count and capacity for the selected hub. It rejects subtotal < 50,000, distance > 8.0 km, invalid lines or hub load ≥ 90% (BR1/2/8/12).
4. PricingService calculates S, Q, V, F, D, R, RainFee and the raw/final total in BR10 order. It rounds only once, half-up to 1,000 (BR3–BR11). The customer sees all components.
5. System presents only methods allowed by the customer's type. Customer selects one; server rechecks BR13 and the rounded COD limit of 1,000,000 (BR14).
6. Customer confirms. The system revalidates the authoritative inputs at the placement boundary; if a displayed quote has changed, it returns the updated quote for confirmation instead of silently charging a different amount (design proposal).
7. PaymentSelector invokes the matching simulated strategy. COD is recorded as awaiting collection; online methods return a simulated success/failure. No live money is moved.
8. On success, save Order in Placed, OrderLines, immutable price/condition snapshots, PaymentRecord and initial history in one transaction. Return the order ID, breakdown and status.

### Read and track

1. Customer opens own order list; system filters by authenticated customer ID.
2. Customer opens details; system rechecks ownership and shows stored lines/prices, payment result, breakdown and chronological status history.
3. Kitchen/shipper updates from HT03 are reflected in tracking; the customer cannot directly set a lifecycle state.

### Update alternate flow

1. Customer requests editing an owned Placed order; any other state is rejected (BR17).
2. Customer edits item lines; hub remains unchanged by design assumption. Recheck allowed items, counts, subtotal and distance. Use placement timestamp/load/rain snapshot when recalculating; do not substitute current load or rain.
3. Existing line prices stay fixed; new lines use current menu prices (AMB03). Apply voucher checks and calculation order consistently; preserve the original placement context.
4. For paid online orders, simulate collecting an increase/refunding a decrease (AMB05). For COD, recheck the rounded cap. If adjustment fails, retain the original order and financial amount.
5. Commit new lines/breakdown and adjustment record together. A rowversion conflict triggers reload/retry; it must not overwrite a concurrently Confirmed order.

### Delete alternate flow Cancel

1. Customer selects Cancel on an owned Placed or Confirmed order.
2. Recheck ownership/state at commit time. Change to Cancelled, append history, and fully refund any online-paid amount using a simulated refund (BR16). Retain order/line/payment history; no hard deletion.
3. From Preparing onward cancellation is rejected. Kitchen rejection from Placed requires a reason; Admin cancellation from Confirmed belongs to HT03 and follows the same financial invariant.

### Exceptions

Invalid input/voucher: show actionable validation and create no order. Hub unavailable/overloaded: offer other hubs. Wrong payment group/COD above cap: reject selection server-side. Unauthorized order: deny without exposing its data. Payment/storage error: no partial order; record diagnostic information without secrets. Duplicate submission: use a unique customer/request key to return the already-created order instead of charging twice (design proposal). Concurrent state change: reject stale mutation and reload.

### Postconditions and test derivation

- Successful Create: exactly one Placed order with valid lines, reproducible pricing, payment status and audit entry; total equals breakdown (TC01–04, NFR05/06).
- Read: only owned records returned, with no state mutation (TC05, NFR04).
- Edit: only Placed changes; original context retained; failed adjustment preserves old amount and lines (TC06).
- Cancel: permitted transition recorded, online payment fully refunded in simulation and history retained (TC07/TC17).
- Rejected action: no unauthorized or partial order/state mutation. Test every BR numeric boundary, failed payment/storage, concurrency and duplicate submission when implemented.

## 2.2.2 UC02 Manage Menu and Brands

**Owner:** Dương Phạm Thái — 524H0189. **Primary actor:** System Admin. Customer browsing is a related read flow.

**References:** FR08–FR11, FR09/FR01/FR13, BR12; Requirements Section 2E and use case A. Report dependency FR18/R1. **Trigger:** Admin opens Brand/Menu management.

**Preconditions:** authenticated Admin; server-side role check; database available. MenuItem must reference an existing brand. A brand is linked to serving hubs via HubBrand.

### Main flow Create

1. Admin chooses Add Brand, enters name/category and saves. System validates required fields and saves an active brand.
2. Admin chooses Add Item, selects brand/category, enters name/unit price and availability.
3. System validates fields and decimal monetary value, saves item and returns the refreshed list. Non-negative price and required names are design assumptions to confirm; no unsupported price maximum is invented.
4. Admin or hub management links the brand to hubs; an item cannot be ordered from an unrelated hub.

### Read

Admin filters/paginates brands/items and opens details including availability/deleted status. Customers see active brands/items served by the selected hub; unavailable items cannot enter a confirmed order even if a stale page displayed them (BR12).

### Update

Admin edits metadata, category, price or availability. Server validates authorization, referenced brand and rowversion before saving. Future orders use the new price; stored existing OrderLine snapshots remain unchanged. Editing an existing Placed order follows AMB03, not direct modification by MenuService.

### Delete

Admin chooses deactivate/soft-delete and confirms. Set IsDeleted/IsActive appropriately; retain all foreign keys and historical order names/categories/prices. A brand deactivation makes its items ineligible for new orders. Existing orders can still be read and processed.

### Alternate and exception flows

Cancelled form: no write. Invalid or negative price/missing fields: return field errors. Unauthorized user: reject server-side. Stale edit: reload instead of overwriting. Attempted order after item becomes unavailable: OrderService rejects it. Soft-deleted references remain queryable for historical reports. Duplicate-name handling is an open validation decision; do not impose a unique-name database index without team agreement.

### Postconditions

Create persists a correctly linked brand/item (TC08); Read returns correct scoped catalog (TC09); Update changes future availability/price while preserving old snapshots (TC10); Delete hides new-order eligibility while retaining historical references (TC11). No operation grants customers Admin write access (FR13/NFR04).

## 2.2.3 UC03 Manage Hubs and Capacity

**Owner:** Lê Quang Tấn — 524H0188. **Primary actor:** System Admin. **References:** FR14.1–FR14.4, FR02, FR16, FR19; BR5/7/8/17; Requirements use case B/Section 2E. Reports: R2; AMB06.

**Trigger:** Admin opens hub management. **Preconditions:** authenticated Admin, server authorization, existing brands for membership selection.

### Main flow Create

1. Admin enters hub name, location/address, positive whole-number kitchen capacity and supported brands.
2. Server validates capacity > 0 and integer membership references (team assumption supporting the BR8 denominator).
3. Save Hub and HubBrand links in one transaction and display the new hub with zero derived load if no active orders exist.

### Read

Admin views hub list/details. HubLoadService counts only Confirmed and Preparing orders and divides by capacity; display current percentage. Load is not entered or manually edited. R2 additionally shows handled count, valid Confirmed-to-Ready mean and granted Quiet Kitchen count under AMB06. A load of 30% receives no Q; 90% rejects new orders.

### Update

1. Admin changes location, capacity or supported brands; recheck positive integer capacity and referenced brands.
2. Save with a rowversion check. Recompute current load from current data; a lower capacity may yield load > 100% and must not rewrite existing order states or historic placement load.
3. Existing orders retain hub/item/price context. Brand membership changes affect new orders only; treatment of adding a new line to an older Placed order must satisfy BR12 and be confirmed in detailed implementation review.
4. Global rain flag is a separate Admin setting; updates affect only new placement snapshots, never historical recalculations (FR16/BR5/17).

### Delete

Admin deactivates/soft-deletes a hub. Stop new orders; retain referenced data and allow existing kitchen/shipper work to finish. Do not cascade-delete orders, payments or history. This operational behavior is a documented proposal pending instructor confirmation.

### Exceptions

Zero/negative/non-integer capacity or missing required location: validation failure, no partial save. Invalid brand ID: reject membership transaction. Stale rowversion: reload. Unauthorized update: deny. Report without completed preparation samples: show no data, not misleading zero minutes. Concurrent hub/order operations must recheck active status and load at the authoritative write boundary.

### Postconditions

Create yields a valid hub and membership set (TC14.1); Read load equals BR8 query (TC14.2); Update preserves historical snapshots (TC14.3/TC16); Delete preserves history and rejects new placement (TC14.4). R2 is consistent with the order lifecycle and stated aggregation definitions (TC19).
