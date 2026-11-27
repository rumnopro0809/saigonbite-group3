# Part 3 Requirements Traceability Matrix (RTM) — UC03, HT03 and R2

**Author:** Lê Quang Tấn (`524H0188`) | **Branch:** `main` (merged via PR #3) | **Date:** 28 November 2026
**Scope:** UC03 (`FR14.1–FR14.4`), the UC03 side of `FR02` and `FR16`, HT03 (`FR17`), and R2 (`FR19`). Test IDs refer to `docs/part3/uc03-test-plan-and-results.md`. UC01 rows are in `rtm-uc01.md`.

| ID | Use Case | Business Rules / Source | Part 2 Design Element | Implemented Code Component | Test Cases | Verdict |
| :--- | :--- | :--- | :--- | :--- | :--- | :---: |
| **FR14.1** Create hub | UC03 | `BR8` denominator; capacity > 0 (team proposal) | use case 2.2.3 Create, `CRC07`, `Hub`, `HubBrand` | `HubValidator`, `HubService.Create`, `HubsController.Create`, `Views/Hubs/Create.cshtml`, `CK_Hubs_MaxCapacity_Positive` | `TC14.1-1..5`, `MT-02` | Pass |
| **FR14.2** View hubs and load | UC03 | `BR7`, `BR8` | use case 2.2.3 Read, `CRC07` | `HubLoad`, `HubLoadService`, `HubService.List/Get`, `HubsController.Index/Details` | `TC14.2-1`, `TC02-3`, `MT-01` | Pass |
| **FR14.3** Update hub | UC03 | `BR8`, `BR17`; rowversion | use case 2.2.3 Update, `CRC07` | `HubService.Update`, `Hubs.RowVersion` (`Uc03HubRowVersion`), `HubsController.Edit` | `TC14.3-1..5` | Pass |
| **FR14.4** Stop orders / soft-delete | UC03, UC01 | team proposal FR14.4; no cascade delete | use case 2.2.3 Delete, `CRC07` | `HubService.SetActive/Delete`, `HubsController.SetActive/Delete`, `uc03.js`; UC01 `OrderService.Quote` rejects inactive or deleted hubs | `TC14.4-1..4`, `TC19-10` | Pass |
| **FR02** Hub load (UC03 view) | UC01, UC03 | `BR8` | `CRC07`, `architecture.md` | `HubLoadService` (same definition as `OrderService.CalculateHubLoad`) | `TC14.2-1`, `TC02-3`, `TC17K-2`, `TC19-9` | Pass |
| **FR16** Heavy-rain flag | UC03, UC01 | `BR5`, `BR17` | `SystemSetting`, `Order.HeavyRainSnapshot` | `RainSettingService`, `HubsController.SetRain`, `uc03.js` | `TC16-1..5`, `MT-01` | Pass |
| **FR17** Kitchen board | HT03, UC01 | `BR15`, `BR16` | `order-state.puml`, `CRC05` | `KitchenBoardService`, `KitchenController`, `Views/Kitchen/Index.cshtml` → UC01 `OrderTransitionService` | `TC17K-1..16` | Pass |
| **FR17** Dispatch and shipper | HT03, UC01 | `BR14`, `BR15`, `AMB01` | `order-state.puml`, `CRC05`, `ui-specification.md` F08 | `ShipperService`, `DispatchController`, `ShipperController`, `Views/Dispatch/*`, `Views/Shipper/*` | `TC17S-1..28`, `MT-03` (to run) | Pass (automated); manual E2E pending |
| **FR19** R2 Hub Utilization | HT04, UC03 | `BR7`, `BR8`, `AMB06` | `CRC10`, `ui-specification.md` F10 | `HubReportService`, `HubReportsController.HubUtilization`, `Views/HubReports/HubUtilization.cshtml` | `TC19-1..10`, `MT-01` | Pass |
| **NFR03** Bilingual, responsive | UC03, HT03 | VI/EN, Bootstrap | `ui-specification.md` | `HubText`, `sb_lang` cookie, Bootstrap 5 views, `globalization fileEncoding="utf-8"` | `MT-01`, `MT-04` | Pass |
| **NFR04** Server-side authorization | UC03, HT03 | role and ownership checks | `architecture.md` | `[AdminOnly]`, `[KitchenOnly]`, `[ShipperOnly]` (`RoleOnlyAttribute`), service checks of hub (`StaffProfile`) and `ShipperId`, `[ValidateAntiForgeryToken]` on every POST | `TC17K-4/5`, `TC17S-6/10/12/13/28`, `MT-05`, `MT-06` | Partial: access is blocked, but a wrong role gets a redirect instead of 403 (`MT-06`) |
| **NFR05** Concurrency / atomic writes | UC03, HT03 | rowversion | `architecture.md` | `RowVersion` checks in `HubService`, `RainSettingService`, `ShipperService`; transactions in `HubService.Create` | `TC14.3-4/5`, `TC16-3/4`, `TC17K-15`, `TC17S-8/25` | Pass |
| **NFR06** Transition audit | HT03 | 100% audit | `OrderStatusHistory` | All kitchen and shipper actions go through UC01 `OrderTransitionService` | `TC17K-6`, `TC17S-14`, `TC17S-22`, `TC17S-27` | Pass |
