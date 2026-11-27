# Part 3 Report Notes — HT01 (Auth), UC02 (Menu & Brands), HT02 (Vouchers), and R1 (Revenue)

Author: **Dương Phạm Thái (524H0189)** | Branch: `feature/uc02-menu` | Date: 27/11/2026

## 1. Detailed Design vs. Part 2 Justification

| Item | Part 2 Design | Part 3 Implementation (`feature/uc02-menu`) | Reason for keeping or adjusting |
| :--- | :--- | :--- | :--- |
| Service separation (`CRC06`, `CRC08`, `CRC09`, `HT01`) | Services in Application layer separated from EF6 and MVC | Added `MenuService`, `VoucherService`, `AuthService`, and `RevenueReportService` in `SaigonBite.Application` backed by repository interfaces; EF6 repos in `SaigonBite.Infrastructure`. | Keeps `SaigonBite.Application` free of EF6 and `System.Web` (`NFR02`), allowing all 31 UC02/HT01/HT02/R1 MSTest cases to run in memory without SQL Server. |
| Single schema for Customers (`BR18`) | Shared `Users` table + `LocalProfile` & `TouristProfile` | `AuthService` validates VN phone (10 digits `03/05/07/08/09`) + simulated OTP `123456` for Local, and Passport (6–9 chars) + nationality + email + code `654321` for Tourist; hashes password (`>= 8` chars) via `PasswordHasher`. | Matches single-schema generalization in `SaigonBiteDbContext`; `AccountController` sets `Session["CurrentUserId"]` so it integrates cleanly with UC01 `DemoSessionHelper`. |
| R1 Revenue Report (`FR18`, `AMB04`) | Revenue by brand category & 5 payment methods, excluding Cancelled/Failed | `RevenueReportService` filters `UTC+7` date range, excludes `Cancelled`/`Failed`, allocates `FoodTotal` proportionally by `OrderLine.CategorySnapshot` & `UnitPriceSnapshot`, and reconciles `FoodRevenue + FeesAndRounding == TotalRevenue`. | Never recalculates from live `MenuItem.Price`; reconciles cleanly across all 5 payment methods (`MoMo`, `VNPay`, `ApplePay`, `PayPal`, `Cod`). |
| Soft-delete (`FR11`, `FR12`, `FR15`, Section 2E) | `IsActive` / `IsDeleted` flags, no hard delete | `EfMenuRepository`, `EfVoucherRepository`, and `EfUserRepository` only toggle flags; `SaigonBiteDbContext` disables cascade delete (`WillCascadeOnDelete(false)`). | Protects historical `OrderLines` (`UnitPriceSnapshot`), past `Orders.VoucherId`, `Orders.CustomerId`, and `R1` revenue totals. |
| Server-side Admin role check (`FR13`, `NFR04`) | Role check in controller and service | `CurrentActorHelper` reads `Session["CurrentUserId"]` and returns HTTP 403 for non-Admin; Application services also throw `BusinessRuleException("FR13")`. | Defense-in-depth; set `Uc02:DevAdminFallback = false` in `Web.config` before final demo once login/switcher is merged. |
| Concurrency token (`RowVersion`) | Planned on `Order`, `Hub`, `Brand`, `MenuItem` | Currently on `Order` in `InitialCreate` migration; `Brand`/`MenuItem` update uses server-side validation without `RowVersion` yet. | Avoided changing shared `InitialCreate` migration independently on a feature branch; can be added cleanly in one shared migration if the team agrees. |

## 2. QA Test Plan Summary (Executed via MSTest on `.NET Framework 4.7.2`)

Build verified: `Domain`, `Application`, `Infrastructure`, `Web`, `Tests` — **0 Warnings, 0 Errors**.
Test run verified: **33 / 33 Passed** (`15 MenuServiceTests` + `7 VoucherServiceTests` + `6 AuthServiceTests` + `3 RevenueReportServiceTests` + `2 PricingSmokeTests`).

| Test ID | FR / BR | Description | Input Data | Expected Result | Actual Result | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :---: |
| TC08-1 | FR08 | Create valid Brand + Hub links | Name="Pho Express", Category="Noodles & Hotpot", HubIds=[1,2,999] | Saved active, 2 valid HubBrand links | Passed (15/15 suite) | Pass |
| TC08-2 | FR08 | Reject blank Brand name/category | Name="   ", Category="" | Validation errors on Name & Category | Passed | Pass |
| TC08-3 | FR08 | Brand length BVA (120/121, 80/81) | Name len 120 vs 121; Category len 80 vs 81 | 120/80 Pass; 121/81 Fail | Passed | Pass |
| TC08-4 | FR08 | Create MenuItem + Price BVA (-1 / 0) | Price=-1 (reject), Price=0 (accept), BrandId=999 (reject) | Rejects -1 and missing brand; accepts 0 | Passed | Pass |
| TC09-1 | FR09, BR12 | Customer browse scoped by Hub | Hub 1 (Brand 1) vs Hub 2 (Brand 1 & 2) | Hub 1 returns 1 item; Hub 2 returns 2 items | Passed | Pass |
| TC09-2 | FR09 | Admin search & soft-deleted filter | Search="Calmette", IncludeDeleted=false vs true | Hidden when false, visible when true | Passed | Pass |
| TC10-1 | FR10, BR17 | Price edit keeps OrderLine snapshot | Update item 1 price 95k -> 110k | Item price=110k; old OrderLine snapshot stays 95k | Passed | Pass |
| TC10-2 | FR10, BR12 | Pause item blocks placement | Set IsAvailable=false, call EnsureItemOrderableAtHub | Throws BusinessRuleException("BR12") | Passed | Pass |
| TC10-3 | FR10 | Reject editing soft-deleted item | Soft-delete item 1 then call UpdateMenuItem | Returns NOT_FOUND error | Passed | Pass |
| TC11-1 | FR11, 2E | Soft-delete MenuItem | SoftDeleteMenuItem(1) | Blocked for new orders; row still in repo with IsDeleted=true | Passed | Pass |
| TC11-2 | FR11, BR12 | Soft-delete Brand | SoftDeleteBrand(1) | Brand items ineligible; creating new item under brand fails | Passed | Pass |
| TC13-1 | FR13, NFR04 | Block non-Admin writes | Roles: Customer, KitchenStaff, Shipper | All throw BusinessRuleException("FR13") | Passed | Pass |
| TC13-2 | BR12 | Cross-hub item check | Item 2 (Brand 2) checked at Hub 1 vs Hub 2 | Throws BR12 at Hub 1; succeeds at Hub 2 | Passed | Pass |
| TC15-1 | FR15, BR9 | Create Percent voucher + BVA (0/100/101) | Value=0 (fail), 100 (pass), 101 (fail), Code=" saigon10 " | Normalized "SAIGON10"; enforces (0, 100] | Passed | Pass |
| TC15-2 | FR15, BR9 | Duplicate code & date range check | Code="saigon15", ValidTo < ValidFrom | Validation errors on Code and ValidTo | Passed | Pass |
| TC15-3 | FR15, BR9 | Min subtotal BVA (99,999 / 100,000) & expiry | SAIGON15 min=100,000; date in range vs 2028 | 99,999=null; 100,000=valid; expired=null | Passed | Pass |
| TC15-4 | FR15, FR13 | Voucher Soft-delete & non-Admin block | SoftDelete(Admin, 1); SoftDelete(Customer, 1) | Soft-deleted kept in repo; Customer throws FR13 | Passed | Pass |
| TC12-1 | FR12, BR18 | Register Local Customer + hash & login | Phone="0901234567", Pass="Password123", OTP="123456" | Creates LocalProfile, stores hashed password, Login succeeds | Passed | Pass |
| TC12-2 | FR12, BR18 | Local phone prefix & password BVA (7 vs 8) | Phone="0201234567" (fail) vs "0387654321" (pass); Pass len 7 vs 8 | Enforces valid prefix + min 8-char password | Passed | Pass |
| TC12-3 | FR12, BR18 | Tourist passport length BVA (5/6/9/10) | Passport len 5 & 10 (fail); len 6 & 9 (pass) | Creates TouristProfile in shared Users schema | Passed | Pass |
| TC12-4 | FR12, FR13 | Duplicate phone & locked account | Duplicate "0701112222"; lock user then Login | Rejects duplicate; locked user fails login; non-Admin throws FR13 | Passed | Pass |
| TC18-1 | FR18, AMB04 | R1 excludes Cancelled/Failed & reconciles | 2 Delivered (MoMo, ApplePay), 1 Cancelled, 1 Failed, 1 Placed | Total=505k (Food 480k + Fees 25k); 5 methods & 3 categories reconcile | Passed | Pass |
| TC18-2 | FR18, FR13 | R1 UTC+7 date window & Admin role guard | Range Nov 1–7 (excludes Nov 8 00:00); Customer call | Includes only Nov 1–7 orders; Customer throws FR13 | Passed | Pass |

## 3. RTM Rows for HT01, UC02, HT02 and R1

| ID | Use Case | Design Element | Code Component | Test Cases | Verdict |
| :--- | :--- | :--- | :--- | :--- | :---: |
| FR08 | UC02 | `uc02-menu-activity`, `CRC06`, `Brand`/`MenuItem` | `MenuService`, `EfMenuRepository`, `BrandsController`, `MenuController` | TC08-1..4 (`MenuServiceTests`) | Pass |
| FR09 | UC02, UC01 | `uc02-menu-activity`, `HubBrand` (`BR12`) | `MenuService.GetCustomerMenuByHub`, `Menu/Browse.cshtml` | TC09-1..2 (`MenuServiceTests`) | Pass |
| FR10 | UC02 | `CRC06`, `BR12`, `BR17` | `MenuService.Update*`, `SetMenuItemAvailability`, `EnsureItemOrderableAtHub` | TC10-1..3 (`MenuServiceTests`) | Pass |
| FR11 | UC02 | Section 2E Soft-delete, `SaigonBiteDbContext` | `MenuService.SoftDelete*`, `EfMenuRepository.SoftDelete*` | TC11-1..2 (`MenuServiceTests`) | Pass |
| FR12 | HT01 | Single schema `Users` + `Local/TouristProfile` (`BR18`) | `AuthService`, `EfUserRepository`, `AccountController`, `Views/Account/*` | TC12-1..4 (`AuthServiceTests`) | Pass |
| FR13 | System-wide | Server-side RBAC (`NFR04`) | `CurrentActorHelper`, `RequireAdmin` across all services | TC12-4, TC13-1, TC15-4, TC18-2 | Pass |
| FR15 | HT02 | `CRC08`, `Voucher` (`BR9`) | `VoucherService`, `EfVoucherRepository`, `VouchersController` | TC15-1..4 (`VoucherServiceTests`) | Pass |
| FR18 | HT04 / R1 | `CRC09`, `AMB04`, OrderLine snapshots | `RevenueReportService`, `EfRevenueReportRepository`, `ReportsController` | TC18-1..2 (`RevenueReportServiceTests`) | Pass |
| NFR02 | System-wide | 4-layer MVC + Repository DIP | `SaigonBite.Application` separated from EF6 & Web | .NET 4.7.2 build (0 errors) + 33 tests | Pass |
| NFR03 | UC02, HT01, HT02, R1 | Bootstrap 5 responsive + `sb_lang` EN/VI | 17 Razor Views (`Account`, `Menu`, `Brands`, `Vouchers`, `Reports`) | Manual UI check pending SQL Express run | Pending UI run |
| NFR04 | System-wide | Hashed passwords, Server role check + AntiForgeryToken | `PasswordHasher`, `[ValidateAntiForgeryToken]`, `403 Forbidden` | TC12-1, TC12-4, TC13-1, TC15-4, TC18-2 | Pass |
