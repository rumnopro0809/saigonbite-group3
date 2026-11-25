# Quyết định thiết kế UC03 và HT03 (phần việc của Tấn)

Ghi lại các quyết định đã áp dụng trong mã, để đối chiếu với Part 2 khi viết Detailed Design Justification.

| # | Quyết định | Lý do |
|---|---|---|
| 1 | UC03 nằm trên nền chung `AppDbContext` (UC01/UC02 đã được gộp vào nhánh này), không port sang `SaigonBiteDbContext` của `origin/main` | Kế hoạch ban đầu giả định UC03 ở scaffold riêng; thực tế nhánh đã gộp UC01/UC02 nên không còn bước port. Nhánh `origin/main` dùng schema khác (Hub: Address/District/MaxCapacity) và **chưa được gộp**; khi hợp nhất cần quyết định với Triều/Thái |
| 2 | Tên controller là `HubsController` (không phải `AdminHubsController`) | Giữ route `/Hubs` đã dùng trong báo cáo Part 2; bảo vệ bằng `[AdminOnly]` |
| 3 | Bốn vai trò `Customer`, `KitchenStaff`, `Shipper`, `Admin` (enum `ActorRole`), lưu trong vé Forms đã mã hóa | Khớp use case tổng quan và `OrderLifecycle` |
| 4 | Nhân viên bếp gắn với một bếp qua bảng `StaffProfile(UserId, HubId)` | Tách khỏi `User`, kiểm quyền phạm vi bếp ở service |
| 5 | Admin, bếp, shipper đăng nhập ở ba trang riêng; mỗi trình duyệt giữ một phiên nhân viên | Tránh dùng nhầm quyền; tài khoản vai trò này không vào được trang vai trò kia |
| 6 | Mọi chuyển trạng thái đơn đi qua `OrderTransitionService` + `OrderLifecycle` | Luật BR14 (COD), BR15, BR16 và lịch sử chỉ viết một chỗ |
| 7 | Tải bếp = (Confirmed + Preparing) / công suất; Placed và Ready không tính | BR8 trong `architecture.md` |
| 8 | Gán shipper (AMB01): chỉ đơn Ready, Admin thao tác, kiểm tra rowversion; không tạo dòng lịch sử vì không đổi trạng thái | Giữ bảng lịch sử chỉ cho chuyển trạng thái |
| 9 | Service trả thông báo tiếng Việt; lớp web dịch sang tiếng Anh (`HubText`) | Không đổi hợp đồng service và test |
| 10 | Tài khoản mẫu `admin123`, `kitchen1..3`, `shipper1..2` dùng mật khẩu `sgbite123` | Chỉ để chạy thử; phải đổi hoặc xóa trước khi triển khai thật |

## Việc cần hỏi nhóm
- Hợp nhất với `origin/main`: schema Hub và `SaigonBiteDbContext`.
- `OrderService` của UC01 đã đọc tải bếp và cờ mưa khi báo giá (xem `CustomerOrderService`); xác nhận với Triều.
