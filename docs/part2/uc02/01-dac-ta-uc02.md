# Đặc tả UC02 — Quản lý Thực đơn và Thương hiệu (Manage Menu and Brands)

Ngày cập nhật: 24/11/2026. Người phụ trách: Dương Phạm Thái — 524H0189 — Group 3.

Bản đặc tả chi tiết phục vụ đối chiếu thiết kế Part 2 và triển khai Part 3 trên khung `SaigonBite.sln` (`Brand.cs`, `MenuItem.cs`, `HubBrand.cs`, `SaigonBiteDbContext.cs`). Tham chiếu đề: mục 2A, 2B (`BR12`, `BR17`), 2E (`Brands & Menu Items`, quy tắc soft-delete), 2F (`R1 Revenue Report`), danh sách Use Case A, và các giả định mở `AMB03`, `AMB04`.

---

## 1. Thông tin chung

| Thuộc tính | Nội dung |
| :--- | :--- |
| **Mã và tên** | `UC02` — Quản lý Thực đơn và Thương hiệu (*Manage Menu and Brands*) |
| **Mục tiêu** | Cho phép System Admin tạo, xem, lọc, cập nhật, bật/tắt trạng thái phục vụ (`IsAvailable`) và xóa mềm (*soft-delete*) các thương hiệu ảo (`Brand`) cùng món ăn (`MenuItem`) của SaigonBite, đồng thời cung cấp dữ liệu danh mục chính xác theo từng Hub cho khách hàng khi đặt món (`UC01`) và cho báo cáo doanh thu theo danh mục thương hiệu (`R1`). |
| **Tác nhân chính** | `System Admin` (quản trị toàn bộ CRUD thương hiệu và món ăn). |
| **Tác nhân / Luồng phối hợp** | `Customer` (`Local Customer` và `Foreign Tourist`) ở luồng đọc (*Read*) danh mục món ăn hợp lệ theo Hub đã chọn; `OrderService` (`UC01`) kiểm tra điều kiện hợp lệ món ăn khi đặt đơn (`BR12`); `HubService` (`UC03`) liên kết thương hiệu với Hub qua `HubBrand`; `RevenueReportService` (`HT04` / `R1`) tổng hợp doanh thu theo `Brand.Category`. |
| **Khởi phát** | System Admin mở trang Quản lý Thương hiệu & Thực đơn từ trang quản trị; hoặc Customer chọn một Hub để duyệt thực đơn trên giao diện đặt món. |
| **Phạm vi CRUD** | Đầy đủ CRUD trên `Brand` và `MenuItem`: **Create** (tạo thương hiệu, tạo món), **Read** (xem danh sách/chi tiết, lọc theo brand/category/trạng thái, khách hàng duyệt menu theo hub), **Update** (sửa thông tin, danh mục, đơn giá `Price`, ảnh `ImageUrl`, bật/tắt `IsAvailable`), **Delete** (ngưng kích hoạt / *soft-delete* bằng `IsActive`/`IsDeleted` để bảo toàn lịch sử đơn hàng). |

### Tiền điều kiện
1. Đối với thao tác quản trị (Create, Read đầy đủ, Update, Soft-delete): người dùng đã đăng nhập với vai trò `ActorRole.Admin` và vượt qua bước kiểm tra phân quyền phía máy chủ (`FR13`, `NFR04`).
2. Cơ sở dữ liệu sẵn sàng; khi tạo hoặc cập nhật `MenuItem`, `BrandId` phải tham chiếu tới một `Brand` tồn tại và chưa bị xóa mềm.
3. Thương hiệu chỉ xuất hiện trên thực đơn đặt món của một Hub khi đã được liên kết với Hub đó qua bảng trung gian `HubBrands` (`HubId`, `BrandId`, phối hợp `UC03`).

---

## 2. Yêu cầu chức năng — FR

| ID | Hệ thống phải làm gì? | BR / Nguồn liên quan | Nhóm kiểm thử |
| :--- | :--- | :--- | :--- |
| **FR08** | Cho phép System Admin tạo mới thương hiệu (`Brand`) và món ăn (`MenuItem`), lưu đầy đủ thương hiệu, danh mục (`Brand.Category`), đơn giá (`Price`, VND) và trạng thái `IsAvailable`. | Đề mục 2E, Use Case A | `TC08` |
| **FR09** | Cho phép System Admin xem danh sách, tìm kiếm/lọc và xem chi tiết thương hiệu, món ăn (gồm trạng thái `IsAvailable` và trạng thái xóa mềm); cho phép Customer duyệt danh mục thương hiệu và món ăn đang hoạt động của Hub đã chọn. | `BR12`, Use Case A | `TC09` |
| **FR10** | Cho phép System Admin cập nhật thông tin thương hiệu/món ăn, danh mục, đơn giá và trạng thái `IsAvailable`. Hệ thống phải từ chối đơn hàng chứa món không còn `IsAvailable`. | `BR12`, `BR17`, `AMB03`, Use Case A | `TC10` |
| **FR11** | Cho phép System Admin ngưng kích hoạt hoặc xóa mềm (*soft-delete*) thương hiệu và món ăn mà vẫn giữ nguyên khóa ngoại và thông tin món ăn trong các đơn hàng lịch sử. | Đề mục 2E, Use Case A | `TC11` |
| **FR13** | *(Liên quan)* Xác thực tài khoản và kiểm tra phân quyền phía máy chủ, đảm bảo chỉ `System Admin` được thực hiện các thao tác ghi danh mục; không cho phép Customer gọi endpoint quản trị. | Đề mục 2A, `NFR04` | `TC13` |
| **FR18** | *(Phụ thuộc báo cáo R1)* Dữ liệu danh mục thương hiệu (`Brand.Category`) từ `UC02` là nguồn phân nhóm cho báo cáo doanh thu `R1 Revenue Report`; báo cáo dùng snapshot giá/món trong `OrderLines`, không tính lại theo giá thực đơn hiện tại. | Đề mục 2F (`R1`), `AMB04` | `TC18` |

---

## 3. Quy tắc nghiệp vụ và quyết định thiết kế áp dụng cho UC02

- **BR12 — Điều kiện đặt món hợp lệ:** Chỉ các món có `IsAvailable == true`, `!IsDeleted`, thuộc `Brand` có `IsActive && !IsDeleted` và được liên kết với Hub đã chọn qua `HubBrands` mới được phép đưa vào đơn hàng.
- **Section 2E — Bắt buộc Soft-Delete:** Vì `Brands` và `MenuItems` được tham chiếu khóa ngoại từ `OrderLines` (`WillCascadeOnDelete(false)` đã cấu hình trong `SaigonBiteDbContext`), thao tác Delete **tuyệt đối không xóa vật lý**. Hệ thống đặt `IsDeleted = true` (và `IsActive = false` với `Brand`) để ẩn khỏi danh mục đặt món mới nhưng vẫn giữ nguyên lịch sử đơn hàng (`FR05`) và báo cáo `R1` (`FR18`).
- **BR17 & AMB03 — Tách biệt giá thực đơn và snapshot đơn hàng:** Khi Admin sửa `MenuItem.Price` qua `MenuService`, giá mới chỉ áp dụng cho các đơn tạo về sau (hoặc món thêm mới khi sửa đơn `Placed` theo đề xuất `AMB03`). Toàn bộ `OrderLine.UnitPriceSnapshot` của các đơn trước đó bất biến.
- **Ràng buộc dữ liệu & đồng thời (Design Assumptions):**
  - `Brand.Name`, `Brand.Category`, `MenuItem.Name` không được để trống; `MenuItem.Price` là `decimal(19,4)` và không âm (`>= 0`, không đặt trần giá không có trong đề).
  - Kiểm soát sửa đè đồng thời (*optimistic concurrency* qua `RowVersion`): cần bổ sung cột `[Timestamp] byte[] RowVersion` cho `Brand` và `MenuItem` qua migration thống nhất với nhóm trong Part 3.

---

## 4. Luồng chính — Tạo mới (Create: `FR08`)

### 4.1. Tạo Thương hiệu (`Brand`)
1. System Admin mở trang **Quản lý Thương hiệu & Thực đơn** và chọn **Thêm thương hiệu**.
2. Admin nhập `Name`, `Category` (phục vụ phân loại và báo cáo `R1`), `Description` (tùy chọn) và chọn **Lưu**.
3. Máy chủ kiểm tra vai trò `Admin` (`FR13`) và tính hợp lệ của các trường bắt buộc.
4. `MenuService` lưu bản ghi `Brand` mới với `IsActive = true`, `IsDeleted = false`.
5. Hệ thống trả về danh sách thương hiệu mới nhất kèm thông báo thành công.

### 4.2. Tạo Món ăn (`MenuItem`)
1. System Admin chọn **Thêm món**.
2. Admin chọn `BrandId` đang hoạt động, nhập `Name`, `Description`, `Price` (`>= 0`, VND), `ImageUrl` (tùy chọn) và trạng thái `IsAvailable`, rồi chọn **Lưu**.
3. Máy chủ kiểm tra quyền `Admin`, xác nhận `Brand` tồn tại và chưa bị xóa mềm, kiểm tra đơn giá `decimal` hợp lệ.
4. `MenuService` lưu bản ghi `MenuItem` mới gắn với `BrandId`.
5. Hệ thống cập nhật danh sách món ăn.
6. *(Phối hợp `UC03`)* Admin gán `Brand` vào một hoặc nhiều Hub qua `HubBrands`; món chỉ đặt được tại các Hub có liên kết đó (`BR12`).

---

## 5. Luồng xem và duyệt thực đơn (Read: `FR09`)

### 5.1. Luồng quản trị (System Admin)
1. System Admin mở danh sách Thương hiệu hoặc danh sách Món ăn.
2. Admin tìm kiếm theo từ khóa, lọc theo thương hiệu, danh mục, trạng thái `IsAvailable`, hoặc bật tùy chọn xem cả bản ghi đã ngưng hoạt động/xóa mềm.
3. Admin mở trang chi tiết của thương hiệu/món để xem đầy đủ giá hiện hành, trạng thái `IsAvailable`, `IsActive`/`IsDeleted`, và danh sách Hub đang vận hành (`HubBrands`).

### 5.2. Luồng khách hàng duyệt thực đơn (Related Read Flow)
1. Customer chọn một Hub giao hàng trong luồng đặt món (`UC01`).
2. `MenuService` chỉ truy vấn các `Brand` có `IsActive && !IsDeleted` liên kết với Hub qua `HubBrands`, cùng các `MenuItem` có `!IsDeleted`.
3. Chỉ các món có `IsAvailable == true` mới cho phép thêm vào đơn hàng; `OrderService` luôn kiểm tra lại `BR12` phía máy chủ khi chốt đơn để chặn dữ liệu từ trang cũ (*stale page*).

---

## 6. Luồng cập nhật (Update: `FR10`)

1. System Admin chọn một `Brand` hoặc `MenuItem` và nhấn **Chỉnh sửa** (hoặc thao tác nhanh bật/tắt `IsAvailable`).
2. Hệ thống tải thông tin hiện tại (kèm token đồng thời `RowVersion` khi triển khai migration Part 3).
3. Admin cập nhật thông tin, danh mục, đơn giá hoặc `IsAvailable` và nhấn **Lưu**.
4. Máy chủ kiểm tra quyền `Admin` (`FR13`), xác thực dữ liệu đầu vào, kiểm tra `BrandId` hợp lệ và xung đột sửa đè.
5. `MenuService` lưu thay đổi:
   - Giá và trạng thái mới chỉ áp dụng cho đơn tạo về sau; toàn bộ `OrderLine.UnitPriceSnapshot` của các đơn cũ giữ nguyên (`BR17`).
   - Nếu `IsAvailable` chuyển sang `false`, các yêu cầu đặt đơn mới chứa món này bị `OrderService` từ chối (`BR12`).
6. Hệ thống hiển thị thông báo thành công và tải lại danh sách.

---

## 7. Luồng xóa mềm / Ngưng kích hoạt (Delete: `FR11`)

1. System Admin chọn **Ngưng hoạt động / Xóa mềm** trên một `Brand` hoặc `MenuItem`.
2. Hệ thống hiển thị hộp thoại xác nhận, giải thích bản ghi sẽ bị ẩn khỏi luồng đặt món mới nhưng giữ nguyên dữ liệu cho lịch sử đơn hàng và báo cáo.
3. Admin xác nhận.
4. Máy chủ kiểm tra quyền `Admin` (`FR13`).
5. `MenuService` cập nhật cờ `IsDeleted = true` (và `IsActive = false` đối với `Brand`), không xóa vật lý:
   - Nếu xóa mềm `Brand`: toàn bộ `MenuItem` thuộc thương hiệu đó không còn hợp lệ cho đơn hàng mới (`BR12`).
   - Mọi khóa ngoại từ `OrderLines` và snapshot giá/tên/category được bảo toàn cho lịch sử đơn (`FR05`) và báo cáo `R1` (`FR18`).
6. Hệ thống cập nhật giao diện quản trị và báo thành công.

---

## 8. Luồng thay thế và ngoại lệ (Alternate & Exception Flows)

| Mã | Tình huống | Xử lý của hệ thống |
| :--- | :--- | :--- |
| **A1** | Admin nhấn **Hủy** trên biểu mẫu tạo/sửa hoặc hộp thoại xóa mềm. | Không ghi cơ sở dữ liệu, quay lại danh sách trước đó. |
| **E1** | Thiếu trường bắt buộc (`Name`, `Category`) hoặc `Price < 0` / sai định dạng số. | Trả về lỗi chi tiết tại từng trường nhập liệu, không lưu DB. |
| **E2** | Người dùng chưa đăng nhập hoặc không phải `ActorRole.Admin` gọi URL/action quản trị. | Từ chối phía máy chủ (`401`/`403`), không thực thi ghi (`FR13`, `NFR04`). |
| **E3** | Xung đột cập nhật đồng thời (*stale edit* — `RowVersion` không khớp). | Từ chối ghi đè, thông báo bản ghi vừa bị thay đổi và yêu cầu tải lại dữ liệu mới nhất. |
| **E4** | Khách hàng gửi đơn từ trang cũ chứa món vừa bị tắt `IsAvailable` hoặc xóa mềm. | `OrderService` kiểm tra lại qua `MenuService` và từ chối đơn theo `BR12`. |
| **E5** | Tạo/sửa `MenuItem` với `BrandId` không tồn tại hoặc đã bị xóa mềm. | Trả về lỗi tham chiếu không hợp lệ, không lưu món mồ côi. |
| **E6** | Lịch sử đơn (`FR05`) hoặc báo cáo `R1` (`FR18`) đọc đơn cũ có `Brand`/`MenuItem` đã xóa mềm. | Đọc thành công từ khóa ngoại và snapshot mà không lỗi hoặc sai lệch doanh thu. |

---

## 9. Hậu điều kiện (Postconditions)

- **Create (`TC08`):** `Brand` hoặc `MenuItem` mới được lưu bền vững, liên kết đúng `Brand` và sẵn sàng gán vào Hub qua `HubBrands`.
- **Read (`TC09`):** Admin thấy đủ danh mục theo bộ lọc; Customer chỉ thấy các thương hiệu và món hợp lệ thuộc Hub đã chọn (`BR12`).
- **Update (`TC10`):** Thay đổi về thông tin, giá hoặc `IsAvailable` chỉ có hiệu lực cho đơn mới, không làm biến đổi `OrderLine` snapshot cũ (`BR17`).
- **Soft-Delete (`TC11`):** Bản ghi bị loại khỏi luồng đặt món mới nhưng giữ nguyên toàn vẹn tham chiếu cho lịch sử đơn hàng và báo cáo `R1`.
- **Bảo mật (`TC13`, `NFR04`):** Không thao tác nào cấp quyền ghi danh mục cho vai trò ngoài `Admin`.

---

## 10. NFR áp dụng cho UC02

| ID | Yêu cầu phi chức năng | Cách áp dụng trong UC02 |
| :--- | :--- | :--- |
| **NFR02** | **Maintainability** (MVC, EF6 Code First, tách nghiệp vụ khỏi Controller/View) | Đóng gói quy tắc xác thực, truy vấn theo Hub và soft-delete vào `MenuService` (`SaigonBite.Application`), tách biệt với `SaigonBiteDbContext` (`Infrastructure`) và Controllers/Views (`Web`). |
| **NFR03** | **Usability** (Bootstrap responsive `375/768/1366 px`, song ngữ Việt/Anh) | Giao diện quản trị Brand/Menu dùng Bootstrap 5 (khớp `_Layout.cshtml`), hỗ trợ chuyển đổi `vi`/`en` qua cookie `sb_lang`. |
| **NFR04** | **Security** (Phân quyền phía máy chủ) | Áp dụng kiểm tra vai trò `ActorRole.Admin` phía server trên mọi action quản trị; không chỉ ẩn nút trên giao diện. |
