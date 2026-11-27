# Rà soát Thiết kế UC02, Thực thể EF6 trên `main` và Figma V2

Ngày rà soát: 24/11/2026. Người phụ trách: **Dương Phạm Thái — 524H0189 — Group 3 (`ThaiDuong06`)**.

Tài liệu đối chiếu nhất quán giữa bốn nguồn:
1. Sơ đồ hành vi `docs/part2/diagrams/uc02-menu-activity.puml`
2. Thiết kế hướng đối tượng `CRC06 MenuService` (`docs/part2/crc-cards.md`) & ERD (`docs/part2/diagrams/database-erd.puml`)
3. Khung mã nguồn Part 3 vừa khởi tạo trên nhánh `main` (`b4a7fa2` — `SaigonBite.Domain`, `SaigonBiteDbContext.cs`, `SaigonBiteSeeder.cs`)
4. Prototype Figma V2 đã trình bày trong buổi Design Review ngày 23/11/2026 (`UC02 Thực đơn` & `R1 Doanh thu`).

---

## 1. Rà soát Activity Diagram (`docs/part2/diagrams/uc02-menu-activity.puml`)

| Nhánh trong sơ đồ | Kiểm tra khớp đặc tả `FR08–FR11`, `FR13`, `BR12` | Ghi chú triển khai Part 3 |
| :--- | :---: | :--- |
| `Authenticate and authorize Admin` | Khớp `FR13`, `NFR04` | Chặn mọi yêu cầu ghi từ vai trò khác `ActorRole.Admin` ở tầng Controller & Service. |
| `Read?` → `Query brand/menu lists and details` | Khớp `FR09` | Tách rõ 2 hàm truy vấn: danh mục đầy đủ cho Admin (có tùy chọn xem bản ghi đã xóa mềm) và danh mục theo `HubId` cho Customer (`HubBrand` + `IsAvailable` + `!IsDeleted`, `BR12`). |
| `Create?` → `Validate required fields and brand reference` → `Valid?` | Khớp `FR08` | Kiểm tra `Name`, `Category`, `Price >= 0`, và `BrandId` tồn tại + `!IsDeleted`. |
| `Update?` → `Edit record; validate fields and rowversion` → `Valid and not stale?` | Khớp `FR10`, `BR17` | **Phát hiện cần bổ sung ở Part 3:** `Brand.cs` và `MenuItem.cs` hiện tại trên `main` (`b4a7fa2`) chưa có cột `[Timestamp] byte[] RowVersion` (hiện mới có trên `Order.cs`). Cần thống nhất với nhóm thêm migration `RowVersion` trước khi bật nhánh `Reject and reload`. |
| `Delete` → `Confirm deactivate/soft-delete` → `Set inactive/deleted flag` | Khớp `FR11`, Mục 2E | Giữ nguyên khóa ngoại tới `OrderLines`, loại khỏi danh mục đặt đơn mới. |

---

## 2. Đối chiếu `CRC06 MenuService` với Scaffold EF6 trên `main` (`b4a7fa2`)

| Hạng mục kiểm tra | Trạng thái trên `main` (`b4a7fa2`) | Đánh giá & Hành động tiếp theo |
| :--- | :--- | :--- |
| `Brand.cs` (`Id`, `Name`, `Category`, `Description`, `IsActive`, `IsDeleted`, `HubBrands`, `MenuItems`) | Đã tạo đủ các thuộc tính chính | Khớp `FR08–FR11` và `R1` (`Brand.Category`). Ghi nhận bổ sung `[Timestamp] byte[] RowVersion` ở migration tiếp theo. |
| `MenuItem.cs` (`Id`, `BrandId`, `Name`, `Description`, `Price`, `ImageUrl`, `IsAvailable`, `IsDeleted`, `Brand`) | Đã tạo đủ các thuộc tính chính | `Price` đã cấu hình `HasPrecision(19, 4)` trong `SaigonBiteDbContext.cs`. Ghi nhận bổ sung `RowVersion`. |
| `HubBrand.cs` (Khóa ghép `HubId`, `BrandId`) | Đã cấu hình composite key & `WillCascadeOnDelete(false)` | Bảo đảm quan hệ N–N giữa Hub và Brand phục vụ lọc `BR12`. |
| Khóa ngoại `OrderLine -> MenuItem` và `MenuItem -> Brand` | Đã tắt `WillCascadeOnDelete(false)` trong `SaigonBiteDbContext.cs` | Đáp ứng chặt chẽ yêu cầu **Soft-Delete (Mục 2E)** — tránh xóa dây chuyền làm mất lịch sử đơn và sai báo cáo `R1`. |
| `OrderLine.UnitPriceSnapshot` (`decimal(19,4)`) | Đã có trong `OrderLine.cs` & `SaigonBiteDbContext.cs` | Bảo đảm cập nhật `MenuItem.Price` trong UC02 không làm thay đổi giá đơn lịch sử (`BR17`). |
| `SaigonBiteSeeder.cs` | Đã seed 3 Hubs, 4 Brands, 12 `HubBrands`, 8 `MenuItems` (ảnh thật trong `Content/images/`) và tài khoản `admin@saigonbite.vn` | Dữ liệu khởi tạo khớp món Sài Gòn trên Figma V2 (`Com Tam Calmette 95k`, `Banh Mi Huynh Hoa 105k`, `Goi Cuon Tay Son 59k`, `Com Tam De Tham 75k`, `Banh Mi Heo Quay 45k`, `Lau Thai 69k`, `Mien Xao Cua 115k`, `Combo Ga Ran KFC 89k`). |
| `SaigonBite.Application` | Đang trống (chưa có service nào) | Là nơi đặt `IMenuRepository` và `MenuService` (`CRC06`) tách hẳn khỏi EF6 và MVC Controllers (`NFR02`). |

---

## 3. Kiểm chứng Figma V2 đã trình bày ngày 23/11/2026

Link prototype công khai: `https://www.figma.com/proto/Ao6NzrJvoRxJevn1gpn9QP`

- **Flow `SaigonBite V2 · UC02 Thực đơn`:**
  - Hiển thị giao diện `Quản lý món` (tab `Món ăn` / `Thương hiệu`, nút `Thêm món +`, dòng thông báo *"Giá và tình trạng mở bán áp dụng cho đơn tạo sau khi cập nhật."*).
  - Bảng món mẫu Sài Gòn (`Cơm Tấm Calmette 95.000 ₫`, `Bánh Mì Huỳnh Hoa 105.000 ₫`, `Gỏi Cuốn Tây Sơn 59.000 ₫`) với trạng thái `Đang bán` và nút `Xem chi tiết`.
  - Ghi nhận thực tế: các nút nội bộ trong khung `UC02` là hotspot tĩnh trên bản prototype Part 2; toàn bộ thao tác Create / Edit / Toggle `IsAvailable` / Soft-Delete thực tế sẽ được cài đặt đầy đủ trong MVC 5 Views ở Part 3.
- **Flow `SaigonBite V2 · R1 Doanh thu` (phần đối soát `Brand.Category` phối hợp `CRC09`):**
  - Khoảng ngày mặc định `01–07/11/2026`, ghi chú rõ *"Doanh thu ghi nhận theo ngày giao thành công. Đơn hủy và giao thất bại được loại khỏi báo cáo."*
  - Số liệu đối soát khớp 100%:
    - `Tổng doanh thu` = `1.820.000 ₫` = `Doanh thu món (1.680.000 ₫)` + `Phí và chênh lệch (140.000 ₫)`.
    - 3 danh mục món: `Cơm tấm & Lẩu (820.000 ₫)` + `Bánh mì (590.000 ₫)` + `Gỏi & Lẩu (270.000 ₫)` = `1.680.000 ₫`.
    - 5 phương thức thanh toán: `MoMo (650.000 ₫)` + `VNPay (290.000 ₫)` + `Apple Pay (360.000 ₫)` + `PayPal (315.000 ₫)` + `COD (205.000 ₫)` = `1.820.000 ₫`.
