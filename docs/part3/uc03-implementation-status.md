# UC03 và HT03: trạng thái triển khai Part 3

Người phụ trách: **Lê Quang Tấn (524H0188)**. Cập nhật 27/11/2026, sau khi UC01 và UC02 đã vào `main`.

Trước đây UC03 dùng khung riêng (`AppDbContext` và entity riêng) nên không merge được vào `main`. Bản này đã chuyển UC03 và HT03 sang khung chung của nhóm. Code dùng chung `SaigonBiteDbContext`, các entity trong `SaigonBite.Domain.Entities`, `OrderTransitionService` của UC01 và phiên đăng nhập HT01.

## Đối chiếu yêu cầu

| Yêu cầu | Vị trí | Kiểm thử tự động |
|---|---|---|
| FR14.1–FR14.4: tạo, xem, sửa, ngừng nhận đơn và xóa mềm bếp | `Infrastructure/Services/HubService.cs`, `Application/Hubs/HubValidator.cs`, `Web/Controllers/HubsController.cs` | `HubServiceTests` |
| FR14.3: chống ghi đè khi hai người cùng sửa | Cột `Hubs.RowVersion` (migration `Uc03HubRowVersion`) | `HubServiceTests`: rowversion cũ, thiếu hoặc sai định dạng |
| FR14.1, BR8: công suất phải lớn hơn 0 | Validator, cùng ràng buộc `CK_Hubs_MaxCapacity_Positive` | `HubServiceTests` |
| BR8: tải = đơn Confirmed và Preparing ÷ công suất, từ 90% thì từ chối đơn mới | `Domain/HubLoad.cs`, `HubLoadService` (cùng định nghĩa với `OrderService.CalculateHubLoad` của UC01) | `HubServiceTests`: 80% nhận, 90% từ chối; bếp ngừng nhận hoặc đã xóa thì UC01 báo `NOT_FOUND` |
| FR16, BR5, BR17: cờ mưa | `RainSettingService`, nút trên `/Hubs` | `RainSettingServiceTests`: đơn cũ giữ `HeavyRainSnapshot` |
| FR19 (R2), AMB06: hiệu suất bếp | `HubReportService`, `/HubReports/HubUtilization` | `HubReportServiceTests` |
| FR17, BR15, BR16: bảng đơn của bếp | `KitchenBoardService`, `/Kitchen` | `KitchenBoardServiceTests` |
| FR17, BR14, BR15: gán shipper, lấy hàng, giao, giao thất bại | `ShipperService`, `/Dispatch`, `/Shipper` | `ShipperServiceTests` |
| NFR04: phân quyền | `[AdminOnly]`, `[KitchenOnly]`, `[ShipperOnly]` dựa trên phiên HT01 (`Session["CurrentUserId"]`); service kiểm tra lại vai trò | Kiểm tra bằng trình duyệt: chưa đăng nhập hoặc sai vai trò thì chuyển về `/Account/Login` |
| NFR03: song ngữ | `HubText` và cookie `sb_lang` | Kiểm tra bằng trình duyệt |

## Thay đổi so với nhánh `feature/uc03-ui` cũ

- Hub dùng các trường của nhóm: `Address` và `District` (thay cho `Location`), `MaxCapacity` (thay cho `Capacity`). Form bếp có thêm ô Quận.
- Service UC03 chuyển từ `Application` sang `Infrastructure/Services`, vì `Application` của nhóm không phụ thuộc EF. Model và validator vẫn ở `Application/Hubs`.
- Bỏ trang đăng nhập riêng của bếp, shipper và Admin. Mọi vai trò đăng nhập ở `/Account/Login` của HT01.
- Bỏ Web API. Ba nút cờ mưa, ngừng nhận đơn và xóa bếp gửi POST có token chống giả mạo tới `HubsController` và nhận JSON.
- Báo cáo R2 nằm ở `HubReportsController` để không trùng `ReportsController` (R1) của HT02.
- Mã lỗi khi giao COD mà chưa thu tiền giữ là `BR15`, giống `OrderTransitionService` của UC01.

## Sửa thêm ở phần dùng chung

- `_Layout.cshtml`: lần gộp UC01 và UC02 trước đó làm thanh menu bị lồng sai thẻ, khiến nút Đăng nhập và mục Đặt món nằm lẫn vào nhau. Bản này sắp lại thẻ, rồi thêm menu UC03/HT03. Các mục này chỉ hiện với đúng vai trò.
- `Web.config`: thêm `globalization fileEncoding="utf-8"`. Các view lưu UTF-8 không có BOM, nên trên máy Windows dùng bảng mã khác, chữ tiếng Việt bị lỗi font.

## Kết quả

- `MSBuild SaigonBite.sln` build sạch.
- 170/170 test đạt: 95 test của UC01 và UC02, cộng 75 test UC03/HT03.
- Kiểm tra trên IIS Express bằng Chromium, ở 1280 px và 375 px:
  - Admin: danh sách bếp, bật/tắt cờ mưa, tạo bếp, công suất 0 bị từ chối, ngừng nhận đơn, xóa mềm, R2 và gán shipper.
  - Bếp và shipper vào được trang của mình nhưng bị chặn ở `/Hubs`.

## Còn mở

- Các quyết định AMB06 và "công suất > 0" vẫn chờ giảng viên.
- Bỏ một brand khỏi bếp có thể làm đơn Placed cũ khó sửa. Cách xử lý cần xác nhận (use-cases.md mục 2.2.3, bước Update 3).
