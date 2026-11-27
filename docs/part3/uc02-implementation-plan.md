# Kế hoạch Triển khai Part 3 — UC02 (Manage Menu and Brands) & Phần Hỗ trợ

Ngày lập: 24/11/2026. Người phụ trách: **Dương Phạm Thái — 524H0189 — Group 3 (`ThaiDuong06`)**.
Nhánh làm việc: **`feature/uc02-menu`** *(tách từ `main` `b4a7fa2`, chỉ mở Pull Request để nhóm rà soát trước khi merge vào `main` ở mốc chốt).*

---

## 1. Môi trường & Nền tảng `main` hiện có (`b4a7fa2`)

- **Solution:** `SaigonBite.sln` (5 project target **`.NET Framework 4.7.2`**: `Domain`, `Application`, `Infrastructure`, `Web`, `Tests`).
- **Packages chuẩn trên `main`:** `EntityFramework 6.4.4`, `Microsoft.AspNet.Mvc 5.2.9`, `Newtonsoft.Json 13.0.3`, `MSTest 2.2.10`.
- **Database:** SQL Server `.\SQLEXPRESS`, database `SaigonBiteDb`, migration khởi tạo `202611240719260_InitialCreate`, `WillCascadeOnDelete(false)` toàn bộ quan hệ tham chiếu từ `OrderLines`.
- **UI & Song ngữ sẵn có:** `_Layout.cshtml` dùng Bootstrap 5.3.3 CDN, bảng màu SaigonBite (`--sb-paper`, `--sb-red`, `--sb-butter`), và cookie `sb_lang` (`vi`/`en`) qua `HomeController.SetLanguage`.
- **Yêu cầu cài đặt máy cục bộ để build & chạy test:**
  1. Visual Studio 2022 Community kèm workload **ASP.NET and web development** và **.NET Framework 4.7.2 targeting pack**.
  2. SQL Server Express (`.\SQLEXPRESS`) đang chạy để vượt qua `DatabaseSeedTests` và chạy `Update-Database`.

---

## 2. Thiết kế tệp mã nguồn cần triển khai cho UC02 (`APP-01`, `FR08–FR11`, `FR13`, `BR12`)

### 2.1. Tầng nghiệp vụ — `src/SaigonBite.Application` *(không tham chiếu `System.Web` hay EF6 — `NFR02`)*
- `Menu/IMenuRepository.cs`: Hợp đồng truy xuất dữ liệu (`Brand`, `MenuItem`, `HubBrand`) giúp unit test `MenuService` chạy nhanh trong bộ nhớ mà không phụ thuộc SQL Server.
- `Menu/MenuDtos.cs`: `BrandUpsertRequest`, `MenuItemUpsertRequest`, `MenuFilterQuery`, `MenuOperationResult` (trả mã lỗi theo từng trường nhập liệu thay vì throw thô ra View).
- `Menu/MenuService.cs` *(đối chiếu `CRC06`)*:
  - `CreateBrand` / `CreateMenuItem` (`FR08`): kiểm tra vai trò `ActorRole.Admin`, tên/category không rỗng, `Price >= 0`, `BrandId` tồn tại và `!IsDeleted`.
  - `GetAdminCatalog` / `GetBrandDetails` / `GetMenuItemDetails` (`FR09`): tìm kiếm, lọc theo Brand, Category, `IsAvailable`, tùy chọn hiển thị bản ghi đã xóa mềm.
  - `GetCustomerHubMenu(int hubId)` (`FR09`, `BR12`): chỉ trả các món `!IsDeleted && IsAvailable` thuộc Brand có `IsActive && !IsDeleted` và được liên kết với `hubId` trong `HubBrands`.
  - `IsItemOrderableAtHub(int hubId, int menuItemId)` (`BR12`): hàm dùng chung để `OrderService` (UC01) kiểm tra lại phía máy chủ khi chốt đơn (chặn stale UI).
  - `UpdateBrand` / `UpdateMenuItem` / `SetItemAvailability` (`FR10`, `BR17`): xác thực quyền Admin, `BrandId`, giá `>= 0` (và `RowVersion` sau khi nhóm chốt migration bổ sung); không bao giờ sửa `OrderLines`.
  - `SoftDeleteBrand` / `SoftDeleteMenuItem` (`FR11`, Mục 2E): đặt `IsDeleted = true` (và `IsActive = false` với `Brand`), giữ nguyên toàn bộ khóa ngoại.

### 2.2. Tầng dữ liệu — `src/SaigonBite.Infrastructure`
- `Repositories/EfMenuRepository.cs`: Triển khai `IMenuRepository` trên `SaigonBiteDbContext`, truy vấn `.Include(m => m.Brand)` và `.Include(b => b.HubBrands)`.
- *(Phối hợp nhóm trước khi chạy migration)*: Nếu cả nhóm thống nhất bật optimistic concurrency cho danh mục như thiết kế Part 2, thêm `[Timestamp] public byte[] RowVersion { get; set; }` cho `Brand.cs` & `MenuItem.cs` và tạo migration `AddCatalogRowVersion`.

### 2.3. Tầng giao diện MVC 5 — `src/SaigonBite.Web`
- `Models/MenuViewModels.cs`: ViewModels cho danh sách món, danh sách thương hiệu, form Create/Edit và hộp thoại xác nhận Soft-Delete.
- `Controllers/BrandsController.cs` & `Controllers/MenuController.cs`:
  - Kiểm tra quyền `ActorRole.Admin` phía server (`FR13`, `NFR04`), bảo vệ chống CSRF bằng `[ValidateAntiForgeryToken]`.
  - Gọi `MenuService`, hiển thị `ModelState` lỗi theo từng trường khi validation thất bại.
- `Views/Menu/` & `Views/Brands/` (`Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Details.cshtml`):
  - Bám bố cục Figma V2 (`Quản lý món` — tab `Món ăn`/`Thương hiệu`, dòng cảnh báo xanh *"Giá và tình trạng mở bán áp dụng cho đơn tạo sau khi cập nhật."*).
  - Bootstrap 5 responsive (`375 / 768 / 1366 px`) và song ngữ Việt/Anh qua cookie `sb_lang` (`NFR03`).

### 2.4. Bộ kiểm thử tự động — `tests/SaigonBite.Tests`
- `MenuServiceTests.cs` (MSTest — đối chiếu trực tiếp `02-traceability.csv`):
  - **`TC08` (`FR08`):** Tạo Brand/MenuItem hợp lệ; từ chối tên rỗng, giá âm (`Price < 0`), hoặc `BrandId` không tồn tại / đã soft-delete.
  - **`TC09` (`FR09`, `BR12`):** Lọc danh mục theo `HubId` chỉ trả món `IsAvailable == true` thuộc Brand của Hub đó; loại món không có trong `HubBrands`.
  - **`TC10` (`FR10`, `BR12`, `BR17`):** Đổi giá và tắt `IsAvailable`; xác nhận `IsItemOrderableAtHub` trả `false` sau khi tắt `IsAvailable` và không làm thay đổi `OrderLine.UnitPriceSnapshot` của đơn cũ.
  - **`TC11` (`FR11`, Mục 2E):** Soft-delete `Brand` hoặc `MenuItem` loại món khỏi danh mục đặt mới nhưng vẫn giữ bản ghi trong DB cho lịch sử đơn và báo cáo `R1`.
  - **`TC13` (`FR13`, `NFR04`):** Gọi thao tác Create/Update/SoftDelete bằng vai trò `Customer` / `KitchenStaff` / `Shipper` bị từ chối ở tầng Service/Controller.

---

## 3. Hạng mục hỗ trợ dự kiến (Chốt lại với nhóm đầu Tuần 5)

Theo `docs/backlog.md`, ngoài UC02 chính, các phần hỗ trợ đề xuất cho `524H0189`:
1. **`APP-06` — `HT01` Authentication & Authorization (`FR12–FR13`, `BR18`):** Dùng `PasswordHasher.cs` có sẵn trong `SaigonBite.Infrastructure`; đăng ký Local (SĐT 10 số đầu `03/05/07/08/09`, OTP mô phỏng) và Tourist (Passport 6–9 ký tự chữ/số, quốc tịch, email, mã mô phỏng); đăng nhập tạo session/cookie phân quyền vai trò thật cho cả `UC01`, `UC02`, `UC03`.
2. **`APP-08` — `HT02` Voucher CRUD (`FR15`, `BR9`, `CRC08`):** Quản lý mã giảm giá `PERCENT`/`FIXED`, trần giảm giá `Cap`, `MinimumSubtotal`, ngày hiệu lực và soft-delete để `PricingService` (UC01) gọi.
3. **Phối hợp `R1` (`CRC09`) & QA (`QA-01`, `QA-02`, `FIN-01`):** Đối chiếu gom nhóm `Brand.Category` cho `R1`, cập nhật dòng `FR08–FR13`/`FR15` trong RTM, ký Contribution Statement, và viết Lessons Learnt cá nhân.

---

## 4. Lộ trình Commit chuẩn trên nhánh `feature/uc02-menu` (mục tiêu >10 meaningful commits)

1. *(Đã commit)* `docs(uc02): add detailed UC02 specification, README and FR08-FR11 traceability`
2. *(Đã commit)* `docs(uc02): review UC02 activity, CRC06, main EF6 scaffold and Figma V2`
3. *(Commit này)* `docs(part3): add UC02 implementation blueprint and test checklist for main scaffold`
4. `feat(uc02): add IMenuRepository and MenuService business rules (FR08-FR11, BR12)`
5. `feat(uc02): implement EfMenuRepository on SaigonBiteDbContext`
6. `feat(uc02): add BrandsController and bilingual Bootstrap views (FR08-FR11)`
7. `feat(uc02): add MenuController, availability toggle and soft-delete confirmation UI`
8. `test(uc02): add MSTest suite covering TC08-TC11, TC13 and BR12 eligibility`
9. *(Nếu nhận HT01)* `feat(auth): add BR18 registration, login and server-side Admin authorization (FR12-FR13)`
10. *(Nếu nhận HT01)* `test(auth): add BR18 validation and role authorization tests (TC12-TC13)`
11. *(Nếu nhận HT02)* `feat(voucher): add VoucherService and Admin Voucher CRUD (FR15, BR9)`
12. `docs(rtm): update UC02 traceability and Part 3 QA verification evidence`
