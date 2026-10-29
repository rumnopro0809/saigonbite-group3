# Requirements Baseline

Group 3; extracted from the reviewed Part 1 English draft on 30 October 2026. Requirement IDs are retained; instructor approval is not claimed. The source Word document and source PDFs remain local, outside the code repository.

## Functional requirements

### FR01
The system shall allow customers to create orders with a delivery address, 1-30 item lines and a quantity of 1-20 per line. Foreign Tourists may include a hotel name and room in the address. It shall accept only Available items from brands served by the selected hub, with a pre-discount subtotal of at least VND 50,000 and a delivery distance no greater than 8.0 km.

Use case: UC01 | Rules/source: BR1, BR2, BR12 | Report: Data R1, R2

### FR02
The system shall reject a new order when hub load is 90% or higher and suggest another hub. Load is the number of Confirmed or Preparing orders divided by hub capacity; the proposed new order is excluded from the load used for acceptance.

Use case: UC01, UC03 | Rules/source: BR8 | Report: -

### FR03
The system shall calculate and display the item subtotal, Quiet Kitchen discount, voucher discount, delivery fee, rush-hour surcharge, rain surcharge and payable total in the order specified by BR10. Only the final total shall be rounded to the nearest VND 1,000, with exact half values rounded up. The formulas and conditions are provided in the pricing rules below.

Use case: UC01 | Rules/source: BR3–BR11 | Report: Data R1, R2

### FR04
The system shall offer MoMo, VNPay or COD to Local Customers and Apple Pay, PayPal or COD to Foreign Tourists, rejecting methods outside the customer's group. COD shall be allowed only when the rounded total does not exceed VND 1,000,000. Online payments shall be simulated in Tier 3 and their processing results recorded.

Use case: UC01 | Rules/source: BR13, BR14; [1] Section 2G and tier rules | Report: Data R1

### FR05
The system shall allow customers to view lists, details, price breakdowns and status histories of orders belonging to their own accounts. Each order shall retain its item lines, status, history, pricing snapshot, payment records and assigned shipper, if any.

Use case: UC01 | Rules/source: BR15; [1] Section 2E Orders | Report: -

### FR06
The system shall allow customers to edit items only while an order is Placed, revalidate the edited order and recalculate prices using the stored placement time, hub load and rain flag. Proposed rules for item prices and payment differences appear in AMB03 and AMB05.

Use case: UC01 | Rules/source: BR17; assumptions AMB03, AMB05 | Report: Data R1, R2

### FR07
The system shall allow customers to cancel orders in Placed or Confirmed and reject cancellation from Preparing onward. A cancelled order already paid online shall receive a full simulated refund, with the refund result recorded.

Use case: UC01 | Rules/source: BR16 | Report: Data R1

### FR08
The system shall allow Admin to create brands and menu items, storing each item's brand, category, unit price and availability.

Use case: UC02 | Rules/source: [1] use case A, Section 2E | Report: Data R1

### FR09
The system shall allow Admin to view brand and menu-item lists and details, and allow customers to browse the menu and brands of the selected hub.

Use case: UC02, UC01 | Rules/source: BR12; [1] use case A | Report: -

### FR10
The system shall allow Admin to edit brand and item information, prices, categories and Available status. It shall reject orders containing unavailable items.

Use case: UC02 | Rules/source: BR12; [1] use case A | Report: Data R1

### FR11
The system shall allow Admin to deactivate or soft-delete brands and menu items while preserving references and item information in existing orders.

Use case: UC02 | Rules/source: [1] Section 2E, use case A | Report: Data R1

### FR12
The system shall support Local Customer registration using a unique 10-digit phone number starting with 03, 05, 07, 08 or 09 and a simulated OTP. Foreign Tourists shall register using a unique 6-9 character alphanumeric passport identifier, nationality, a valid email and a simulated email code. Identifiers shall be unique; passwords shall contain at least 8 characters and be hashed. Both customer types shall share one schema. Users shall be able to view/edit profiles, and Admin shall manage/deactivate accounts subject to authorization.

Use case: HT01 | Rules/source: BR18; [1] Sections 2A, 2E, use case E | Report: -

### FR13
The system shall authenticate accounts, enforce server-side authorization and restrict data to its owner or the user's assigned scope. Customers shall not access other customers' orders; kitchen staff shall handle only orders for their own hub.

Use case: HT01; system-wide | Rules/source: [1] Section 2A; [3] Section 4B Security | Report: Access R1, R2

### FR14.1
The system shall allow Admin to create hubs with a location, integer capacity and supported brands. The team proposes requiring capacity greater than zero so that the load calculation is defined.

Use case: UC03 | Rules/source: [1] use case B, Section 2E; capacity > 0 is proposed | Report: Data R2

### FR14.2
The system shall allow Admin to view hub lists, details and current load, calculated from Confirmed and Preparing orders under BR8.

Use case: UC03 | Rules/source: BR8 | Report: Data R2

### FR14.3
The system shall allow Admin to update hub information, capacity and supported brands.

Use case: UC03 | Rules/source: [1] use case B, Section 2E | Report: Data R2

### FR14.4
The system shall allow Admin to deactivate or soft-delete hubs while preserving related orders. The team proposes that an inactive hub reject new orders but continue processing its existing orders.

Use case: UC03 | Rules/source: [1] Section 2E; team proposal in FR14.4 | Report: Data R2

### FR15
The system shall allow Admin to create, view, edit and deactivate PERCENT or FIXED vouchers with a value, cap where applicable, minimum subtotal, validity period and active flag. Each order shall use at most one valid voucher under BR9.

Use case: HT02, UC01 | Rules/source: BR9; [1] use case C | Report: Data R1

### FR16
The system shall allow Admin to enable or disable the heavy-rain flag and store its value at placement in the order snapshot for consistent surcharge calculation.

Use case: UC03, UC01 | Rules/source: BR5, BR17 | Report: Data R1

### FR17
The system shall support only the status transitions and roles in the lifecycle table below, rejecting all other transitions in the business layer. Kitchen rejection shall require a reason, and COD collection shall be confirmed before Delivered. Shipper assignment follows the proposal in AMB01.

Use case: HT03, UC01 | Rules/source: BR14-BR16; [1] Section 2C; assumption AMB01 | Report: Data R1, R2

### FR18
The system shall allow Admin to select a date range and view revenue by brand category and payment method, excluding Cancelled and Failed orders. Proposed recognition, discount-allocation and fee conventions appear in AMB04.

Use case: HT04 | Rules/source: [1] Section 2F R1; assumption AMB04 | Report: Direct R1

### FR19
The system shall allow Admin to view each hub's processed-order count, average preparation time from Confirmed to Ready, current load and number of Quiet Kitchen discounts. Proposed counting and missing-data conventions appear in AMB06.

Use case: HT04, UC03 | Rules/source: [1] Section 2F R2; BR7, BR8; assumption AMB06 | Report: Direct R2

## Quality attributes

### NFR01 Performance Mandatory
Calculate the complete price, including discounts and surcharges, in less than 1.5 seconds for an order with up to 30 item lines.

Planned verification: Measure business-layer processing on a documented environment. Propose 30 runs for each order size of 1, 15 and 30 lines. Every run must be below 1.5 seconds; record actual measurements.

### NFR02 Maintainability Mandatory
Use MVC, Entity Framework Code First and schema migrations. Separate pricing from controllers and views. Use payment Strategy with one interface and one class per method. Add a payment method through a new class and registration without modifying existing payment classes.

Planned verification: Review MVC structure; create and upgrade the database using migrations. Inspect the Git diff when adding a payment method and rerun regression tests for existing methods.

### NFR03 Usability Mandatory
Provide a responsive Bootstrap interface with Vietnamese/English switching for the main customer, Admin, kitchen and shipper workflows.

Planned verification: Propose checks at widths of 375, 768 and 1366 px: no obscured action buttons or lost content. Check labels, buttons and messages in both languages.

### NFR04 Security Mandatory
Store hashed passwords, never plaintext passwords. Enforce role-based and data-level authorization on the server.

Planned verification: Inspect stored data and authentication code. Attempt direct Admin access as a customer, access to another customer's order, and access to another hub's order. All unauthorized attempts must be rejected.

### NFR05 Reliability Team-added
Save each order and its item lines atomically: either all changes succeed or all are rolled back. A failure partway through saving must leave no partially saved new order.

Planned verification: Inject failures while saving item lines and before commit. Verify that no partially saved new records remain.

### NFR06 Auditability Team-added
Record the order ID, previous and new status, actor and timestamp for 100% of successful status transitions. Record a reason for kitchen rejection.

Planned verification: Compare tested transitions against history records. Verify that no event, actor or timestamp is missing, and check rejection reasons.

## Ambiguities and proposed assumptions

### AMB01
Shipper assignment: the specification does not identify who assigns a shipper, when assignment occurs or how the shipper is selected.

Proposal: Propose manual assignment by Admin to an active shipper when the order is Ready. Only the assigned shipper may move it to Out for Delivery and record the delivery outcome. Related: FR17, FR13; HT03

Instructor status: No answer recorded; enquiry date not confirmed.

### AMB02
Payment after failed delivery: the specification requires refunds for Cancelled orders but does not define payment handling when delivery is Failed because the customer cannot be reached.

Proposal: Propose no automatic online refund for Failed orders because food has been prepared and delivery attempted; record COD as uncollected. Instructor confirmation is required. This proposal does not apply to Cancelled orders, which receive a full refund under BR16. Related: FR17, FR18; UC01

Instructor status: No answer recorded; enquiry date not confirmed.

### AMB03
Item prices during edits: BR17 preserves time, load and rain snapshots but does not specify item prices after menu prices change.

Proposal: Propose retaining stored unit prices for existing lines when quantities change; newly added items use the price at addition. Discounts and surcharges continue to use the BR17 snapshot. Related: FR06, FR10; UC01, UC02

Instructor status: No answer recorded; enquiry date not confirmed.

### AMB04
Revenue recognition: it is unclear whether undelivered orders count, which timestamp defines the reporting period, and how discounts and fees are allocated.

Proposal: Propose including only Delivered orders, using the successful-delivery timestamp. Category revenue is food revenue after discounts, allocated in proportion to item amounts. Payment-method totals use the final total, with food, fees and rounding adjustments shown separately for reconciliation. Related: FR18; report R1

Instructor status: No answer recorded; enquiry date not confirmed.

### AMB05
Editing a paid order: Placed orders may be edited, but the specification does not define additional charges or refunds of differences.

Proposal: Propose simulating an additional charge when the total rises or a partial refund when it falls. Commit the edit only if adjustment succeeds; otherwise retain the old order and amount. Recheck BR14 for COD. Related: FR04, FR06; UC01

Instructor status: No answer recorded; enquiry date not confirmed.

### AMB06
Hub-report aggregation: the specification does not define which orders count as handled, how to treat orders without a Ready timestamp, or whether cancelled orders retain a granted Quiet Kitchen discount in the count.

Proposal: Propose counting orders that have reached Confirmed. Calculate average preparation time only from orders with both Confirmed and Ready timestamps. Count granted Quiet Kitchen discounts from the placement snapshot, including later-cancelled orders, and label this clearly. Display no data when there are no valid samples. Current load always follows BR8. Related: FR19; report R2

Instructor status: No answer recorded; enquiry date not confirmed.

## Source documents

- SE_FinalProject_Requirements_SaigonBite_Sem01_2627.pdf
- SE_FP_SaigonBite_CaseStudy_Sem01_2627.pdf
- SE_FP_SaigonBite_StudentSurvivalGuide_Sem01_2627.pdf
- SE_Sem01_2627_Report_Template.docx

Specification controls fixed BR values. AMB policies are team proposals until instructor answers are recorded.
