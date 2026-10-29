# Initial verification on 30 October 2026

Group 3. These are actual local checks of the initial code and design, not a completed Part 3 QA report.

| Check | Actual result | Reproduce |
|---|---|---|
| Pure C# domain compile | Passed using installed Roslyn csc | scripts/check-domain.ps1 |
| Initial executable rule checks | 53 passed, 0 failed | scripts/check-domain.ps1 |
| PlantUML syntax/render | 8 sources rendered successfully to SVG and PNG | scripts/render-diagrams.ps1 |
| Diagram visual review | All eight PNGs inspected; labels, actors and state guards readable | docs/part2/diagrams/*.png |
| Requirements baseline | 22 FR, 6 NFR, 6 AMB | docs/requirements-baseline.md |
| Design RTM | 28 unique FR/NFR rows | docs/part2/traceability.csv |

The executable checks cover the worked pricing example; subtotal, distance, delivery bands, rush windows and timezone conversion, rain/free delivery, load thresholds, voucher minimum/cap/food cap, rounding, line/quantity limits, unavailable items, wrong payment groups, COD cap and invalid lifecycle guards. They do not prove complete CRUD, persistence, EF migrations, authorization integration, refunds, reports, UI usability, concurrency or NFR performance. Those remain backlog items. The final project still requires formal EP/BVA partitions with traceability, unit-test runner screenshots and real execution evidence.

Source validation assumptions currently implemented defensively include non-negative distance/load/voucher values and a percentage at most 100. Voucher validity uses inclusive supplied timestamps in this initial helper; the UI/date-only interpretation must be confirmed before integration. Editing and voucher expiry policy remain unresolved as recorded in the design.

No real payment, external customer data or production database is used. Named owner attribution on diagrams identifies responsibility for review/development, not a claim that the owner independently produced this AI-assisted initial draft.
