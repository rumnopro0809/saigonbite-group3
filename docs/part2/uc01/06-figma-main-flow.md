# UC01 ngày 14/11/2026 — Luồng đặt món trên Figma V2

Nguyễn Hải Triều · 524H0072 · Group 3.

## Tiến độ đến ngày 14/11

| Ngày | Kết quả có thể kiểm tra |
|---|---|
| 10/11 | Đặc tả, FR/BR/NFR, truy vết và Ambiguity Log đã có trên GitHub tại commit `5f17f99`. |
| 11/11 | Activity tổng quan, bốn Activity chi tiết và vòng đời đơn đã có trên GitHub tại commit `891afa6`. |
| 12/11 | CRC và class/Strategy đã có bản nháp chung. Việc đối chiếu riêng cho UC01 theo kế hoạch ngày 12/11 còn chờ. |
| 13/11 | ERD và kiến trúc MVC đã có bản nháp chung. Việc rà soát riêng cho UC01 theo kế hoạch ngày 13/11 còn chờ. |
| 14/11 | Đã đọc các frame và liên kết online. Bản cập nhật local đã xuất thành gói nhập và qua kiểm tra mô phỏng. Đồng bộ bản sửa lên Figma còn chờ. |

## Kiểm tra trực tiếp file Figma

Trang **04 Prototype** có 91 frame V2. Danh sách điểm bắt đầu có luồng khách desktop, mobile, Tourist checkout, cùng Quản trị, UC02, UC03, R1 và R2. Kiểm tra dưới đây đọc liên kết từ các điểm bấm trong file online. Thao tác trình chiếu sau khi nhập bản mới vẫn cần thực hiện. [File SaigonBite của nhóm](https://www.figma.com/design/Ao6NzrJvoRxJevn1gpn9QP)

Luồng desktop chọn địa chỉ, mở thực đơn, vào chi tiết phở, thêm một phần, thêm combo bánh mì và tiếp tục checkout có đích đến hợp lệ. Bếp có thể được chọn qua nút **Đổi bếp** trong thực đơn. Nút xác nhận địa chỉ hiện mở thẳng thực đơn trên bản online trước cập nhật. [Bắt đầu tại màn khám phá](https://www.figma.com/design/Ao6NzrJvoRxJevn1gpn9QP?node-id=39-2)

| Khách | Phương thức | Checkout online | Đích trước cập nhật | Đích trong bản cập nhật local |
|---|---|---|---|---|
| Việt Nam | MoMo | `39:1363` | `39:2403` | Màn Placed có nhãn MoMo đã thanh toán mô phỏng |
| Việt Nam | VNPay | `39:1462` | `39:2403` | Màn Placed riêng cho VNPay |
| Việt Nam | COD | `39:1561` | `39:2490` | Màn Placed có ghi thu tiền mặt khi giao |
| Du lịch | PayPal | `39:1663` | `39:2573` | Màn Placed có nhãn PayPal đã thanh toán mô phỏng |
| Du lịch | Apple Pay | `39:1761` | `39:2573` | Màn Placed riêng cho Apple Pay |
| Du lịch | COD | `39:1860` | `39:2655` | Màn Placed có ghi thu tiền mặt khi giao |

Checkout của khách Việt hiển thị MoMo, VNPay và COD. Tourist checkout hiển thị Apple Pay, PayPal và COD. Đây là hai kịch bản đặt món với dữ liệu dựng sẵn. Flow Tourist trong V2 bắt đầu ở checkout. [Checkout khách Việt](https://www.figma.com/design/Ao6NzrJvoRxJevn1gpn9QP?node-id=39-1363) · [Tourist checkout](https://www.figma.com/design/Ao6NzrJvoRxJevn1gpn9QP?node-id=39-1663)

## Thay đổi đã chuẩn bị trong gói V2

1. Xác nhận địa chỉ chuyển sang màn chọn bếp. Từ bếp Quận 1, nút **Xem thực đơn** tiếp tục tới danh mục món.
2. Checkout VNPay và Apple Pay có frame theo dõi riêng. MoMo và PayPal cũng hiển thị tên phương thức và kết quả mô phỏng trong chi tiết đơn.
3. Các phương thức COD giữ ghi chú thu tiền khi giao. Luồng tạo thành công dùng trạng thái **Placed** và tổng mẫu **205.000 đồng**.

Đã xuất 93 frame và 683 liên kết vào bản xem thử và gói nhập. Cả 93 frame đều có đường truy cập từ các điểm bắt đầu. Kiểm tra 45 đường bắt buộc đã đạt. Với bản 91 frame mô phỏng, gói cập nhật giữ các frame hiện có, thêm 2 frame, cập nhật 3 liên kết và không thêm frame ở lần chạy tiếp theo.

Hai hình sau lấy từ bản render local. [Theo dõi VNPay](images/uc01-theo-doi-vnpay.png) · [Theo dõi Apple Pay](images/uc01-theo-doi-apple-pay.png)

## Xác nhận sau khi nhập vào Figma Desktop

1. Mở file nhóm và chạy plugin nhập V2 từ gói ngày 14/11. Kết quả mong đợi là thêm đúng 2 frame vào bản 91 màn.
2. Trong Present, vào flow **SaigonBite V2 · Khám phá món**. Chọn địa chỉ → dùng địa chỉ này → chọn bếp → chọn món → giỏ → thanh toán → đặt đơn.
3. Lặp lại checkout với MoMo, VNPay và COD. Đối chiếu nhãn phương thức, tiền mẫu 205.000 đồng, trạng thái Placed và ghi chú COD.
4. Mở **SaigonBite V2 · Tourist checkout** và thử PayPal, Apple Pay, COD. Đối chiếu từng đích với bảng trên.

Lệnh ghi vào Figma MCP bị từ chối vì hết hạn mức Starter. Figma Desktop mở được tab nhóm, nhưng vùng thiết kế chưa truy cập được qua công cụ điều khiển. Bản sửa đang chờ chạy plugin và xác nhận trong Present.
