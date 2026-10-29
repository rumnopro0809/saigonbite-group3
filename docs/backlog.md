# Delivery backlog

Priority reflects dependency and deadline risk. Dates are planning targets, not claims of work completed. Final submission date must be confirmed.

| ID | Priority | Work | Owner of main area | Acceptance |
|---|---|---|---|---|
| P2-01 | P0 | Review three UC specifications and resolve open decisions | All three | CRUD flows and FR/BR/postconditions checked by each owner |
| P2-02 | P0 | Validate UML and schema relationships | All three | Sources render; order/payment/snapshot/soft-delete relations align |
| ENV-01 | P0 | GitHub private repo, identity and first push | Triều coordinates | Authorized repo URL; genuine author identity; remote push confirmed |
| ENV-02 | P0 | MVC 5/EF6 build tools and SQL Server | All three | Fresh build and clean database creation using migration |
| APP-01 | P0 | Hub and menu data with soft deletion | UC02 + UC03 | Admin CRUD and customer browse work; historical references remain |
| APP-02 | P0 | Pure pricing engine and boundary unit tests | UC01 | Correct BR1-BR12/BR14; half-up only once; snapshot-based edit |
| APP-03 | P0 | Payment Strategy and actor-specific selector | UC01 | Five methods simulated; invalid customer/method pairing rejected |
| APP-04 | P0 | Place/View orders end to end | UC01 integrates | Valid order + lines + payment/snapshot persist; invalid order leaves no partial order |
| P2-03 | P0 | Figma customer/admin/kitchen/shipper/report flows | All three | Native public Figma link; desktop/mobile frames; click-through paths |
| APP-05 | P1 | Edit/cancel and refund adjustments | UC01 | BR16/17; failures preserve prior order; audit/payment entries retained |
| APP-06 | P1 | Authentication and server authorization | Support: Thái proposed | Registration BR18; passwords hashed; role/ownership denied server-side |
| APP-07 | P1 | Kitchen/shipper lifecycle and history | Support: Tấn proposed | Exact BR15 transitions; own hub/assigned shipper only; COD collected before Delivered |
| APP-08 | P1 | Voucher CRUD and checkout integration | Support: Thái proposed | Active/date/min/cap checks; one voucher; soft-delete |
| APP-09 | P1 | Revenue and Hub Utilization reports | Triều + Tấn proposed | Reconciled fixture data; all mandatory dimensions and metrics |
| P2-04 | P1 | Word/PDF design report and Design Review | All three | Headings 2.1–2.6; authors; diagrams; Figma screenshots/link; 7–10 minute review |
| QA-01 | P1 | EP/BVA and invalid transition suite | All three | At least 20 EP/BVA, all numeric thresholds; ≥5 invalid transition tests; actual runner evidence |
| QA-02 | P1 | NFR checks and RTM completion | All three | Recorded environment/timings, authorization, reliability, auditability results |
| FIN-01 | P1 | Change Request impact note and final release | All three | Real instructor request, diff evidence, tagged repo/video/report |

## Definition of Done for implemented features

Requirements linked; code builds; applicable unit/integration checks pass; server authorization is enforced; no lost historical data; another member reviews; RTM and user-facing flow are updated. A design draft does not meet the implemented-feature Definition of Done.

## Initial weekly iteration

Goal: agree on the Part 2 design and produce a runnable first order flow as soon as the toolchain is available. Review progress daily. Do not assign all design to one member or defer all testing until the last week. Record completed work and blockers in `project-status.md`.
