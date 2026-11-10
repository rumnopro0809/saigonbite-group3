# UC01 — Ambiguity Log và quyết định thiết kế

Nguyễn Hải Triều — 524H0072 — Group 3. Cập nhật 10/11/2026.

## 1. Các mục đã có trong baseline Part 1

Giữ nguyên mã AMB của nhóm, không đánh số lại để tạo cảm giác là các câu hỏi mới. **Chưa ghi nhận câu trả lời của thầy; chưa xác nhận đã gửi hỏi.**

| ID | Điểm chưa rõ | Đề xuất hiện tại | Phụ thuộc và ảnh hưởng |
|---|---|---|---|
| AMB01 | Ai phân công shipper, khi nào và chọn thế nào? | Admin chọn thủ công shipper đang hoạt động khi đơn Ready; chỉ shipper được phân công xử lý việc giao đơn | HT03 phối hợp UC01; FR13/17; không thay actor/chuyển trạng thái của BR15 |
| AMB02 | Đơn đã trả online nhưng giao Failed có được hoàn không? | Không tự động hoàn khi Failed, chờ xác nhận chính sách | HT03 phối hợp thanh toán UC01; khác hoàn đầy đủ khi Cancelled đã được BR16 quy định |
| AMB03 | Khi sửa Placed mà giá menu thay đổi, giá dòng món lấy ở đâu? | Dòng đã có giữ đơn giá snapshot; dòng thêm mới dùng giá hiện tại | FR06 và UC02; thời điểm/tải/mưa vẫn theo BR17 |
| AMB05 | Sửa một đơn đã trả làm tổng tiền tăng/giảm thì xử lý thế nào? | Mô phỏng thu thêm/hoàn chênh lệch; chỉ lưu sửa khi xử lý thành công; lỗi thì giữ đơn và số tiền cũ | FR04/06; COD phải kiểm tra lại BR14 |

AMB04 (doanh thu) và AMB06 (cách tổng hợp hiệu suất bếp) thuộc phần báo cáo chung. UC01 cần lưu dữ liệu phục vụ chúng nhưng ngày 1 không tự chốt lại chính sách báo cáo của nhóm.

## 2. Các quyết định chi tiết cần rà soát khi thiết kế

Các mã D dưới đây chỉ là mã theo dõi trong phần UC01, không thay FR/BR hoặc tự thêm quy định của thầy.

| ID | Câu hỏi/đề xuất | Trạng thái |
|---|---|---|
| D01 | Báo giá đổi giữa xem checkout và bấm đặt: kiểm tra lại và cho khách xác nhận giá mới trước khi xử lý | Đề xuất thiết kế trong đặc tả chung; cần đối chiếu prototype |
| D02 | Đề nói sửa món, chưa chốt sửa bếp/địa chỉ. Phạm vi hiện tại chỉ sửa dòng món, giữ bếp/địa chỉ/khoảng cách | Giữ bếp là giả định đã có; giới hạn địa chỉ/khoảng cách cần xác nhận |
| D03 | Voucher hết hạn sau khi đặt nhưng trước khi sửa: đề xuất voucher cũ xét theo lúc đặt; voucher mới xét lúc sửa | Đã được ghi là mở trong architecture.md; chưa chốt; không ghi là BR9 chính thức |
| D04 | Nếu mô phỏng hoàn tiền khi hủy thất bại: đề xuất hoàn tác thao tác hủy, giữ trạng thái ban đầu và báo lỗi để thử lại | Quyết định thiết kế mới cần Triều/nhóm rà soát; chưa duyệt; kết quả hủy thành công vẫn bắt buộc hoàn đầy đủ theo BR16 |
| D05 | Bấm đặt trùng: dùng mã yêu cầu duy nhất theo khách và trả lại kết quả cũ nếu đã xử lý | Đề xuất thiết kế đã có; kiểm chứng khi triển khai, không coi việc ẩn nút là đủ |

## 3. Câu hỏi ngắn để Triều trao đổi với thầy

1. Khi giao Failed, đơn trả online được hoàn toàn bộ, hoàn một phần hay không tự động hoàn?
2. Khi sửa Placed, nhóm giữ giá dòng cũ và dùng giá hiện tại cho dòng thêm mới có phù hợp không?
3. Khi sửa đơn đã trả, nhóm mô phỏng thu/hoàn chênh lệch và chỉ lưu khi thành công có phù hợp không?
4. Có cho sửa địa chỉ/bếp không? Voucher cũ hết hạn lúc sửa phải xét theo thời điểm nào?
5. Nhóm đề xuất Admin phân shipper khi Ready có phù hợp không?

Đây là danh sách chuẩn bị, không phải tin nhắn đã gửi. Khi có phản hồi, ghi ngày, nội dung xác nhận và cập nhật đặc tả, sơ đồ, Figma, truy vết tương ứng.
