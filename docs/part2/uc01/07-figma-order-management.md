# UC01 ngày 15/11/2026 — Sửa, hủy và các trường hợp lỗi

Nguyễn Hải Triều · 524H0072 · Group 3.

Bản V2 ngày 15/11 bổ sung 14 màn thuộc luồng quản lý đơn. Điểm bắt đầu **SaigonBite V2 · UC01 Sửa, hủy và lỗi** mở sáu tình huống dựng sẵn. Các trạng thái Confirmed và Preparing mô phỏng kết quả do bếp cập nhật. Người xem chọn tình huống để kiểm tra giao diện tại trạng thái tương ứng.

| Tình huống | Đường đi và kết quả cần thấy | Đối chiếu |
|---|---|---|
| Sửa khi Placed | Đơn vừa đặt → Sửa món → Lưu và tính lại. Bỏ phở, giữ bánh mì. Đơn vẫn Placed, tổng từ 205.000 xuống 120.000 đồng và hoàn chênh lệch mô phỏng 85.000 đồng. | FR06, BR17, AMB05 |
| Hủy khi Placed | Hủy trên đơn MoMo gốc để thấy hoàn 205.000 đồng. Hủy sau khi sửa để thấy hoàn phần tiền còn lại là 120.000 đồng. | FR07, BR16, AMB05 |
| Hủy khi Confirmed | Bếp đã xác nhận → Hủy đơn → Xác nhận hủy. Lịch sử giữ mốc xác nhận và kết quả hoàn 205.000 đồng. Màn Confirmed chỉ hiện nút hủy. | FR05, FR07, BR15–17 |
| Theo dõi khi Preparing | Bếp đang làm món. Giao diện hiện lý do và ẩn cả sửa lẫn hủy. | FR05–07, BR16–17 |
| Bếp đủ tải | Tải 90% tại lúc xác nhận. Đơn mẫu chưa được đặt. Nút **Chọn bếp khác** quay về danh sách bếp. | FR02, BR8 |
| COD vượt hạn mức | Tổng mẫu sau làm tròn là 1.026.000 đồng, cao hơn mức 1.000.000 đồng. Màn hướng dẫn về thực đơn để đổi giỏ món. | FR04, BR14 |
| Thanh toán thất bại | Mở lỗi thanh toán rồi bấm **Thử lại thanh toán** để về checkout với giỏ mẫu. | FR04, ngoại lệ E06 |
| Mobile 390 px | Checkout → Đặt đơn → Theo dõi → Sửa hoặc hủy. Sau khi sửa còn 120.000 đồng và vẫn có thể hủy. Hai nhánh hủy hiện đúng khoản hoàn tương ứng. | FR05–07, NFR03 |

Số tiền trên là dữ liệu prototype. Khi sửa, snapshot giữ giờ 15:00, tải 20% và không mưa. Tổng sau sửa gồm 105.000 đồng tiền món, trừ 10.500 đồng ưu đãi và cộng 25.000 đồng giao hàng. Kết quả 119.500 được làm tròn thành 120.000 đồng.

## Bốn giai đoạn

| Giai đoạn | Bằng chứng lưu trong repo |
|---|---|
| Sketch | Phác ba trạng thái và thao tác của khách. [Sketch](images/uc01-sketch-15.svg) |
| Wireframe | Xác định vị trí trạng thái, chi tiết đơn và nút chính. [Wireframe](images/uc01-wireframe-15.svg) |
| Mockup | Áp dụng giao diện V2 cho Confirmed desktop và Placed mobile. [Desktop](images/uc01-mockup-confirmed.png) · [Mobile](images/uc01-mockup-mobile-placed.png) |
| Prototype | Bản HTML tương tác và plugin nhập Figma nằm trong gói local. Hình dưới lưu điểm bắt đầu desktop và kết quả mobile sau khi hủy đơn đã sửa. [Kịch bản](images/uc01-scenarios-15.png) · [Hủy mobile ở 375 px](images/uc01-mobile-cancel-375.png) |

## Kiểm tra và trạng thái đồng bộ

- Gói local có 107 màn, trong đó có 21 màn mobile. 772 liên kết và 63 đường đi bắt buộc đã qua kiểm tra tự động. Cả 107 màn đều truy cập được từ các điểm bắt đầu.
- Mô phỏng nhập tăng dần từ 91 màn thêm 16 màn. Từ 93 màn thêm 14 màn. Chạy lại bản 107 màn thêm 0. Kiểm tra giữ ID của 91 màn cũ và đổi 4 liên kết đã chỉ định.
- Sơ đồ quyền trên desktop đã được kiểm tra theo từng trạng thái. Nút hành động mới trên mobile đạt ít nhất 44 × 44 px khi thu về 375 px. Có ảnh render đối chiếu ở chiều rộng này. [Kết quả kiểm tra](15-figma-qa.json)
- Đã chạy JavaScript của bản HTML và lần theo liên kết SVG cho các nhánh Confirmed, Preparing, ba lỗi và sửa rồi hủy trên mobile. Kiểm tra số tiền hoàn ở hai màn kết quả.
- Mốc online gần nhất xác minh được là 91 màn ngày 14/11. Ngày 15/11, Figma Starter từ chối cả hai lượt đọc do hết hạn mức. Còn bước nhập gói và kiểm tra Present trên file nhóm. [Figma SaigonBite](https://www.figma.com/design/Ao6NzrJvoRxJevn1gpn9QP)

Sau khi nhập, mở flow **UC01 Sửa, hủy và lỗi** và đi theo các hàng trong bảng. Mở **SaigonBite V2 · Mobile**, chọn phở và bánh mì để đến checkout hai món, rồi thử sửa và hủy. Các hình Sketch, Wireframe và Mockup dành cho phần minh họa báo cáo.
