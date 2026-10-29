# 2.6 UI UX Design and Figma Handoff

Owners: 524H0072 customer/orders/payments; 524H0189 menu/accounts/vouchers; 524H0188 hubs/kitchen/delivery. Reports shared. AI-assisted baseline; native Figma public link and screenshots are still pending.

## Layout

Desktop 1440 px; mobile 390 px with a check at 375 px. Bootstrap-style 12-column grid, menu/cart 8/4 on desktop and stacked on mobile. Cream background, deep green navigation, orange primary actions and readable charcoal text. Persistent hub/customer context and EN/VI toggle. Keyboard-visible focus, labeled errors, text plus icons for statuses. Do not put tier or implementation details in customer screens.

| Frame | Content/actions | Errors and guards | Traceability |
|---|---|---|---|
| F01 Register/Login | Local phone or Tourist passport/nationality/email; password; mock verification | BR18 format/uniqueness; invalid credentials | FR12/13, BR18 |
| F02 Hub/Menu | Hubs/load, brand tabs, available dishes, quantities/cart | Unavailable item, invalid quantities, overloaded hub offers alternatives | FR01/02/09/14.2, BR1/2/8/12 |
| F03 Checkout | Address/distance, one voucher, S/Q/V/F/D/R/RainFee, rounded total and allowed payment methods | Voucher error, COD limit, quote change, mock payment failure | FR03/04/15/16, BR3–BR14 |
| F04 Tracking | Stored breakdown/payment/timeline; edit/cancel controls | Edit Placed only; cancel Placed/Confirmed; stale-state error | FR05–07/17, BR15–17 |
| F05 Menu Admin | Brand/item CRUD, search, availability and deactivate confirmation | Required fields/price; no customer write access | FR08–11/13 |
| F06 Hub Admin | CRUD, location, integer capacity, supported brands, load; global rain setting | Capacity > 0; stale record; warn inactive hubs reject new orders | FR14.1–14.4/16, BR5/8/17 |
| F07 Kitchen | Own-hub board; Confirm, Reject with reason, Preparing, Ready | No arbitrary status dropdown or other-hub actions | FR13/17, BR15/16 |
| F08 Shipper | Assigned tasks/address; pickup, COD collected, Delivered/Failed | Cannot deliver before pickup; COD guard; unreachable-customer failure | FR13/17, BR14/15 |
| F09 R1 Revenue | Date filter, net food by category, totals by method, fee/rounding reconciliation | Empty data; defined Delivered-only assumption; exclude Cancelled/Failed | FR18, AMB04 |
| F10 R2 Utilization | Count, preparation mean, current load, Q grants per hub | No data for incomplete durations; clearly label current/time-range metrics | FR19, BR7/8, AMB06 |
| F11 Vouchers | CRUD; code/type/value/cap/minimum/date/active | Invalid dates/values; deactivate | FR15, BR9 |
| F12 Profile/User Admin | Own profile; separate Admin user management | Role and ownership authorization | FR12/13, BR18 |

## Required prototype paths

1. Local registration → hub/menu → MoMo/COD checkout → Placed → edit → cancel/refund result.
2. Tourist registration → PayPal checkout → tracking; MoMo/VNPay choices absent.
3. Kitchen Confirmed → Preparing → Ready → Admin shipper assignment → pickup → COD collected → Delivered.
4. Pickup → Failed due to unreachable customer; no terminal-state actions.
5. Menu and hub CRUD, availability and soft-delete; changed catalog does not rewrite order history.
6. R1/R2 populated and empty results; mobile checkout/tracking; language toggle.

## Before submission

Author native editable Figma frames, connect the paths, add owner IDs/FR/BR annotations, publish an accessible prototype and check its access without the editor session. Insert actual screenshots and URL into the official Part 2 Word/PDF. This specification and UML sources do not satisfy the public Figma deliverable alone.
