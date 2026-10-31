# Part 2 One Week Delivery Plan

Updated 31 October 2026. The user confirms approximately one week remains for Part 2. This is an internal seven-day execution plan, not an exact official submission timestamp. Prioritize design, native Figma, report and presentation this week; retain existing code for the Week 5 skeleton.

| Day | Work | Exit criterion |
|---|---|---|
| 1 | Review three UC specifications, CRC and schema with the assigned owners | Stable responsibilities and FR/BR coverage; assumptions labeled |
| 2 | Finalize/render UML; author Figma Sketch/Wireframe stages | CRUD/navigation agree with all three UCs |
| 3 | Figma Mockup: shared components, desktop grid and mobile layouts | Checkout, tracking, Admin flows and both reports visually defined |
| 4 | Connect Interactive Prototype paths and error/dialog states | Demonstrate customer/Admin/kitchen/shipper/report paths without dead ends |
| 5 | Assemble official-template report Sections 2.1–2.6, figures, author IDs and Figma screenshots/link | Complete report draft; no placeholders or invented approvals/results |
| 6 | Slides, timed rehearsal and cross-check | All three members present in 7–10 minutes; PDF bookmarks and prototype access verified |
| 7 | Submission buffer | Correct files ready before actual assignment closing time |

## Figma stage organization

Create `00 Components and Notes`, `01 Sketch`, `02 Wireframe`, `03 Mockup`, `04 Prototype`. The supplied guide specifies four stages from Sketch to Prototype but does not name the middle stages; Wireframe/Mockup are the team's proposed organization, subject to classroom terminology.

Preserve early-stage frames rather than overwriting them. Annotate owners/FR/BR outside the customer-facing UI. Use Bootstrap-style responsive grids and desktop/mobile evidence for the main order flow. The native public prototype link remains mandatory.

| Flow | Required evidence | Owner review |
|---|---|---|
| Hub/Menu → Checkout → Placed → Edit/Cancel → Tracking | Price breakdown, one voucher, hub rejection, COD cap, original-context edit | 524H0072 |
| Brand/Menu CRUD | Create/read/edit/deactivate, availability and retained order history | 524H0189 |
| Hub/Capacity CRUD | Create/read/edit/deactivate, positive integer capacity, load and rain setting | 524H0188 |
| Kitchen → Ready → Assignment → Pickup → Delivered/Failed | Own-hub/assigned-shipper guards, rejection reason and COD confirmation | Team |
| R1 and R2 | Date filters, mandatory metrics, populated/empty data and labeled assumptions | Team |

Checkout needs separate Local/Tourist variants: Local MoMo/VNPay/COD; Tourist Apple Pay/PayPal/COD. Across the prototype show all five methods, while each actor sees only permitted methods under BR13.

## Acceptance before submission

- [ ] CRC collaborations match the class design.
- [ ] Three main UCs have full CRUD specs, postconditions and one behavioral diagram each.
- [ ] Use case generalization, shared-schema ERD, five Payment Strategy classes/selector and exact eight-state lifecycle are reviewed.
- [ ] Architecture explains SOLID/separation/cohesion/coupling and where pricing lives, linked to NFRs.
- [ ] Figma preserves four stages, interactions, responsive layouts, pricing/payment/tracking and both reports.
- [ ] Word/PDF retains official headings 2.1–2.6, author IDs and FR/BR traceability.
- [ ] Native public Figma link works outside an editor-only session.
- [ ] Actual feedback is recorded if received; unresolved assumptions remain labeled.
- [ ] All members can explain their own UC in a 7–10 minute Design Review.

The initial 53 pure-code checks are supporting work, not a substitute for the design or Figma deliverables. Do not mark Part 2 complete while prototype/report/presentation evidence is missing.
