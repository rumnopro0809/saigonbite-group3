# Part 3 Individual Reflection & Generative AI Usage Log — UC03, HT03 and R2

**Student:** Lê Quang Tấn (`524H0188`) | **GitHub:** `qtan1401` | **Role:** UC03 Manage Hubs and Capacity (`FR14.1–FR14.4`), heavy-rain flag (`FR16`), HT03 kitchen and shipper boards with dispatch (`FR17`), R2 Hub Utilization (`FR19`)

> **Draft note:** this draft was written from the commit history and project notes. Before submission, Tấn should reread each section, rewrite the reflection in his own words, and check the dates and tool names in the AI log against his own records.

---

## Individual Lessons Learnt — Lê Quang Tấn (`524H0188`)

### 1. What I Built
For Part 3 I built the hub management side of SaigonBite on the team's shared schema:
- Hub create, view, update, stop/resume and soft-delete with rowversion concurrency (`HubService`, `HubValidator`, `HubsController`), and a database check that capacity is positive.
- The derived hub load (`HubLoad`, `HubLoadService`), shared with UC01's order acceptance.
- The global heavy-rain flag (`RainSettingService`).
- The kitchen board and the shipper board, plus Admin shipper dispatch (`KitchenBoardService`, `ShipperService`, `KitchenController`, `ShipperController`, `DispatchController`). All of them call UC01's `OrderTransitionService`.
- The R2 Hub Utilization report (`HubReportService`, `HubReportsController`).

I also wrote 75 MSTest cases for these parts, which all pass on the separate `SaigonBiteTests` database.

### 2. Hardest Technical Problems and How I Solved Them
1. **Starting on a separate scaffold.** My first UC03 version (21 November) was built before the team's shared `SaigonBiteDbContext` existed, so it had its own `AppDbContext`, its own `Hub` fields (`Location`, `Capacity`) and its own migration. When UC01 and UC02 were merged into `main`, my branch had 17–18 add/add conflicts. Instead of resolving them file by file, I ported UC03 onto `main`:
   - Mapped `Capacity` to `MaxCapacity` and `Location` to `Address` + `District`.
   - Added `RowVersion` and the capacity check with an additive migration (`Uc03HubRowVersion`).
   - Moved the EF services to `Infrastructure`.
   - Dropped my own login pages in favour of the HT01 login.

   The port merged into `main` with zero conflicts (PR #3) and the full suite passed 170/170.
2. **One definition of hub load.** BR7, BR8 and R2 all depend on "load", and it would be easy for the hub list, the order quote and the report to count different statuses. I kept a single rule (Confirmed + Preparing ÷ capacity, thresholds in `HubLoad`) and wrote tests that cover the exact boundaries:
   - 29% vs 30% for the Quiet Kitchen discount.
   - 80% accepted vs 90% rejected.
   - Placed, Ready, Cancelled and delivered orders not counted.
   - The R2 "current load" equal to the hub list's value.
3. **Not trusting the page for permissions.** Hiding a button is not security. Each kitchen and shipper action is checked again in the service: the kitchen can only act on its own hub's orders, and a shipper only on orders assigned to them. Every POST also needs a valid anti-forgery token and the current `RowVersion`, so a double click or a stale page cannot apply an action twice.

### 3. What I Would Do Differently Next Time
- I would branch from the team scaffold on the first day and merge `main` into my branch every day. That would have avoided the late port and the risk of a deadline-week merge.
- I would write the controller-level authorization tests together with the service tests, not leave the role checks to browser testing.
- I would keep the design-justification notes while coding, not reconstruct them at the end.

---

## Appendix B: Generative AI Usage Log (Part 3 — `524H0188`)

> Full conversation prompts and outputs are kept locally and can be shown to the instructor within 48 hours on request, per Section 7 of the SaigonBite Final Project Specification.

| Tool & Date | Purpose | Prompt (Summary) | What Was Kept or Changed | How It Was Verified |
| :--- | :--- | :--- | :--- | :--- |
| Claude Code, 21 Nov 2026 | First UC03 prototype: hub load rule, hub and rain-flag services, R2 service, EF6 context and migration, MVC views | "Build UC03 Manage Hubs (FR14.1–FR14.4), the rain flag (FR16) and R2 (FR19) following the Part 2 use case and CRC07/CRC10." | Kept the `HubLoad` thresholds, the validator and the service structure. The separate `AppDbContext`, the Web API controllers and the `Location`/`Capacity` fields were later removed during the port to the team schema. | 12 domain checks of the load thresholds; manual run of the hub pages. |
| Claude Code, 25–26 Nov 2026 | HT03 kitchen board, shipper board, Admin dispatch, and the UC03/HT03 test suite | "Add the kitchen and shipper pages (FR17, BR14–BR16, AMB01) using the order state machine, with tests for wrong-hub, wrong-shipper, COD and stale-rowversion cases." | Kept the services and tests. I removed an Admin login page that belonged to HT01 (Thái's scope) and decided to keep the COD error code `BR15` to match UC01 instead of changing it to `BR14`. | MSTest run of the UC03/HT03 tests; manual run of Confirm → Preparing → Ready → Assign → Pick up → Delivered. |
| Claude Code, 27 Nov 2026 | Port UC03/HT03 onto `main` (`SaigonBiteDbContext`, HT01 login, UC01 `OrderTransitionService`); separate test database; Release config and hosting | "Port UC03 onto main without editing UC01/UC02 files except DI and csproj lines; rebuild and run all tests." | Kept the port. I reviewed the field mapping and the `Uc03HubRowVersion` migration, and asked for the R2 report to move into its own `HubReportsController` so that it does not overwrite R1. | MSBuild with 0 errors; 170/170 tests; browser check at 1280 px and 375 px for Admin, kitchen and shipper; trial merge with 0 conflicts. |
| Claude Code, 27–28 Nov 2026 | UI polish: shared button style, role-based menu, removing requirement codes from screens | "Apply one consistent button style to the admin pages and show menu links only for the right role." | Kept the style changes. I chose the colours and checked each page myself. | Build, test run, and visual check of each UC03/HT03 page. |
| Claude Code, 28 Nov 2026 | Pre-submission review and drafting of this UC03 documentation (design justification, RTM, test plan, this reflection) | "Review my UC03 work against the plan and requirements, then write the missing UC03 documents." | The review found two code issues: a signed-in user with the wrong role is sent to the login page instead of getting 403, and the AJAX buttons fail silently after the session expires. I review and edit the drafts before submission; test numbers come from a real test run on 28 Nov. | Reran the 75 UC03/HT03 tests (75/75) and the full suite (170/170). |
