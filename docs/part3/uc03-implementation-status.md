# UC03 — Quản lý bếp và công suất: trạng thái triển khai Part 3

Người phụ trách: **Lê Quang Tấn — 524H0188 — Group 3**. Cập nhật 25/11/2026 theo mã nguồn trên nhánh `feature/uc03-ui`. Tài liệu này đối chiếu thiết kế Part 2 ([use-cases.md](../part2/use-cases.md) mục 2.2.3, [CRC07 và CRC10](../part2/crc-cards.md), [sơ đồ hoạt động UC03](../part2/diagrams/uc03-hub-activity.puml), [kiến trúc](../part2/architecture.md)) với phần đã làm. Đây không phải xác nhận của giảng viên; các giả định AMB06 và "công suất > 0" vẫn là đề xuất của nhóm.

## Đối chiếu yêu cầu

| Yêu cầu | Lớp / vị trí | Kiểm thử tự động | Kết quả |
|---|---|---|---|
| FR14.1 Tạo bếp (vị trí, công suất nguyên dương, brand) | `HubService.Create`, `HubValidator`, ràng buộc `CK_Hubs_Capacity_Positive` | `HubServiceTests` (TC14.1): lưu hub và `HubBrand` cùng lúc, biên công suất 0/−1/1, tên và vị trí rỗng hoặc quá dài, brand trùng/không tồn tại/đã xóa không để lại hub dở dang | Đạt |
| FR14.2 Xem danh sách, chi tiết, tải hiện tại | `HubService.List/Get`, `HubLoadService` | TC14.2: tải = Confirmed + Preparing ÷ công suất; Placed, Ready, Cancelled… không tính; 20% Quiet, 30% và 80% Normal, 90% Full | Đạt |
| FR14.3 Sửa bếp | `HubService.Update` (kiểm tra rowversion) | TC14.3: đổi trường và brand, đơn cũ và snapshot giữ nguyên, giảm công suất làm tải > 100% nhưng không đổi trạng thái đơn, rowversion cũ/thiếu/sai định dạng bị từ chối | Đạt |
| FR14.4 Ngừng nhận đơn, xóa mềm | `HubService.SetActive/Delete`, nút trên trang chi tiết gọi REST API | TC14.4: giữ hàng, đơn và lịch sử; lần xóa thứ hai NotFound; ngừng hoạt động thì `CustomerOrderService.Quote` từ chối đơn mới (BR8) và nhận lại khi bật | Đạt |
| FR02 / BR8 Từ chối đơn mới khi tải ≥ 90% | `CustomerOrderService` dùng cùng định nghĩa tải | Báo giá bị từ chối ở 90%, được nhận ở 80% | Đạt |
| FR16 / BR5 / BR17 Cờ mưa | `RainSettingService`, `PUT /api/rain-flag` | TC16: bật/tắt có rowversion, đơn cũ giữ `HeavyRainSnapshot`, báo giá mới dùng cờ mới; cờ toàn cục được khôi phục sau mỗi test | Đạt |
| FR19 / R2, AMB06 | `HubReportService`, `/Reports/HubUtilization` | TC19: khoảng ngày theo giờ UTC+7 (bắt đầu bao gồm, kết thúc là đầu ngày sau), "đã xử lý" = từng Confirmed, thời gian chuẩn bị chỉ dùng đơn có cả Confirmed và Ready, không có mẫu thì `null` (không phải 0), lượt Quiet lấy từ snapshot kể cả đơn hủy sau đó, tải hiện tại khớp `HubService` | Đạt |
| NFR04 Quyền | `[AdminOnly]`, `[AdminOnlyApi]`, vé Forms vai trò Admin | `AdminAccountServiceTests` cho xác thực; kiểm tra trình duyệt: chưa đăng nhập thì chuyển về `/Admin/Login`, API trả 401 | Đạt (đăng nhập bếp/shipper chưa có) |
| NFR03 Song ngữ | `HubText`, `sb_lang`, `uc03.js` | Xem trên trình duyệt (VI/EN, 375 px); không có test tự động cho giao diện | Đạt trong phạm vi đã xem |

Chạy `dotnet test SaigonBite.sln`: 151 test đạt, trong đó 31 test UC03, 29 test bếp và 40 test shipper/gán shipper (HT03). Các test dùng database SQL Express riêng `SaigonBiteIntegrationTests`, tự tạo dữ liệu có hậu tố GUID và chỉ xóa dữ liệu của chính test.

## Đối chiếu sơ đồ hoạt động UC03

| Bước trong sơ đồ | Thực hiện |
|---|---|
| Xác thực và phân quyền Admin | Đăng nhập `/Admin/Login`, kiểm tra vai trò ở server |
| Đọc: đếm Confirmed/Preparing, suy ra tải, hiển thị cùng R2 | `HubLoadService`, trang `/Hubs`, `/Reports/HubUtilization` |
| Tạo: vị trí, công suất nguyên dương, brand; lưu `Hub` và `HubBrand` nguyên tử; lỗi thì không ghi dở | Một lần `SaveChanges`; test không để lại hub khi có lỗi |
| Sửa: kiểm tra rowversion, giữ đơn và snapshot | `HubService.Update`; trả Conflict và tải lại khi dữ liệu cũ |
| Xóa: xác nhận, xóa mềm, không nhận đơn mới, giữ đơn cũ | Nút xóa cần bấm hai lần để xác nhận |
| Cờ mưa chỉ ảnh hưởng snapshot mới | `RainSettingService`; đơn lưu `HeavyRainSnapshot` |

## Đối chiếu kiến trúc

- Sơ đồ kiến trúc nối `Services → EF`: các service UC03 nằm ở `SaigonBite.Application` và đọc dữ liệu qua `IAppDbContext` (`IDbSet`), nên khớp sơ đồ. Văn bản ADR-001 viết nghiệp vụ "độc lập với EF"; phần này chỉ đúng một nửa cho UC03 (không phụ thuộc `System.Web`, nhưng project `Application` có tham chiếu gói EntityFramework). UC02 đi qua `IMenuRepository` nên tách hoàn toàn. Đây là điểm cần cả nhóm thống nhất; chưa đổi để không phá vỡ mã đang chạy.
- Tải bếp luôn được tính, không lưu cột (đúng CRC07). Định nghĩa tải dùng chung với đặt đơn và R2.
- Thông báo của service giữ tiếng Việt làm gốc; lớp web dịch sang tiếng Anh. Thông báo mới của service phải được thêm vào bảng dịch `HubText`, nếu không sẽ hiện tiếng Việt trong chế độ tiếng Anh.

## So với Figma (F06, F10)

Đối chiếu bằng ảnh các frame UC03 (F06) và R2 (F10) trích từ báo cáo Part 2 (mục 2.6) với ảnh chụp trang đang chạy bằng Chromium có Bootstrap, bằng tiếng Anh và tiếng Việt, ở 1365 px và 375 px. Tôi không mở từng frame trên Figma trực tuyến. Khớp: tiêu đề, nút cờ mưa và "Thêm bếp", khung ghi chú xanh, cột bảng, nhãn trạng thái, ba thẻ số liệu và bảng R2.

Khác Figma, chưa làm: tab "Bảng điều khiển" và nút "Mở luồng đặt món" trên thanh đầu trang (thay bằng "Trang khách" và "Thương hiệu"); R1 vẫn chưa có. Thêm so với Figma: số mẫu dưới thời gian chuẩn bị và chú thích phân biệt chỉ số theo kỳ với tải tại thời điểm xem (theo yêu cầu F10). Chưa kiểm tra bằng Visual Studio hay trình duyệt khác ngoài Chromium và WebKit.

## Còn mở

- Trên nhánh `feature/uc03-ui` hiện chỉ có phần UC03/HT03. Bếp, shipper và các test cần `OrderTransitionService`, `PaymentSelector` (UC01) cùng các entity dùng chung, nên nhánh chưa build độc lập; sẽ gộp lại khi UC01 và UC02 đã vào `main`.
- Các quyết định AMB06 và "công suất > 0" chờ giảng viên.
- Khi nhóm gộp nhánh UC01/UC02 gốc (`SaigonBiteDbContext`) cần thống nhất lại migration, vì UC03 dùng `AppDbContext`.
- Brand bị bỏ khỏi bếp có thể làm đơn Placed cũ khó sửa; cách xử lý cần xác nhận (use-cases.md mục 2.2.3, bước Update 3).
