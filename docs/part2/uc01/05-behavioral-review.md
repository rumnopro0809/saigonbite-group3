# Hạng mục 11/11 — Activity UC01 và vòng đời đơn

Nguyễn Hải Triều — 524H0072 — Group 3. Hạng mục theo lịch 11/11/2026, thực hiện sớm ngày 10/11/2026 theo yêu cầu tiếp tục. Bản AI hỗ trợ, Triều/thầy chưa xác nhận duyệt.

## 1. Bộ sơ đồ

| Sơ đồ | Nguồn sửa được | SVG | PNG |
|---|---|---|---|
| UC01 tổng quan | [PlantUML](../diagrams/uc01-order-activity.puml) | [SVG](../diagrams/uc01-order-activity.svg) | [PNG](../diagrams/uc01-order-activity.png) |
| Create | [PlantUML](../diagrams/uc01-create-activity.puml) | [SVG](../diagrams/uc01-create-activity.svg) | [PNG](../diagrams/uc01-create-activity.png) |
| Read | [PlantUML](../diagrams/uc01-read-activity.puml) | [SVG](../diagrams/uc01-read-activity.svg) | [PNG](../diagrams/uc01-read-activity.png) |
| Edit | [PlantUML](../diagrams/uc01-edit-activity.puml) | [SVG](../diagrams/uc01-edit-activity.svg) | [PNG](../diagrams/uc01-edit-activity.png) |
| Cancel | [PlantUML](../diagrams/uc01-cancel-activity.puml) | [SVG](../diagrams/uc01-cancel-activity.svg) | [PNG](../diagrams/uc01-cancel-activity.png) |
| Order lifecycle dùng chung | [PlantUML](../diagrams/order-state.puml) | [SVG](../diagrams/order-state.svg) | [PNG](../diagrams/order-state.png) |

Một UC01 được thể hiện bằng tổng quan và bốn hình chi tiết, không đổi thành bốn UC chính mới. Các hình dùng nhãn tiếng Anh gọn cho báo cáo; đặc tả/đối chiếu giữ tiếng Việt.

## 2. Phân biệt trách nhiệm

- Activity: thứ tự hành động khi xử lý một yêu cầu; các hình chi tiết có swimlane Customer/System.
- State: tám trạng thái của Order qua thời gian; bao gồm cả thao tác vận hành HT03 để giải thích tracking.
- Customer không được cập nhật trạng thái bếp/shipper. Kitchen Staff đúng bếp; Shipper được phân công; yêu cầu khách phải đúng chủ đơn.
- Kết thúc Activity không có nghĩa đơn đã kết thúc. Sửa Placed không tạo trạng thái Editing; thất bại thanh toán trước đặt không tạo chuyển Placed → Failed.

## 3. Bảng chuyển trạng thái đối chiếu đề, mục 2C

| Từ | Đến | Actor/guard |
|---|---|---|
| Placed | Confirmed | Kitchen Staff thuộc bếp của đơn |
| Placed | Cancelled | Customer sở hữu đơn, hoặc Kitchen Staff thuộc bếp và ghi lý do |
| Confirmed | Preparing | Kitchen Staff thuộc bếp |
| Confirmed | Cancelled | Customer sở hữu đơn hoặc Admin |
| Preparing | Ready | Kitchen Staff thuộc bếp |
| Ready | Out for Delivery | Shipper được phân công |
| Out for Delivery | Delivered | Shipper được phân công; COD phải xác nhận đã thu tiền |
| Out for Delivery | Failed | Shipper được phân công; khách không liên lạc được |

Ba trạng thái cuối: Delivered, Cancelled, Failed. Các mũi tên từ/đến pseudostate bắt đầu/kết thúc không tính trong tám chuyển trạng thái nghiệp vụ. Mọi chuyển khác bị từ chối ở tầng nghiệp vụ BR15.

## 4. Đối chiếu đặc tả và nhánh xử lý

| Yêu cầu | Bằng chứng trong sơ đồ |
|---|---|
| FR01/02 | Create kiểm tra giỏ, bếp/tải, khoảng cách; lỗi trả giỏ hoặc đề nghị bếp khác |
| FR03 | Create hiển thị S/Q/V/F/D/R/RainFee và tổng; thứ tự BR10, làm tròn BR11 |
| FR04 | Create phân COD/online; giới hạn và nhóm phương thức; lỗi thanh toán không tạo đơn được chấp nhận |
| FR05/FR13 | Read lọc danh sách, kiểm tra lại quyền sở hữu khi mở chi tiết; có trạng thái rỗng |
| FR06/BR17 | Edit chỉ Placed, dùng snapshot, hiển thị tổng mới cho khách xác nhận, kiểm tra lại trạng thái/phiên bản khi lưu |
| FR07/BR16 | Cancel chỉ Placed/Confirmed; hoàn đầy đủ online, COD chưa thu không hoàn; giữ lịch sử |
| NFR04 | Các thao tác với đơn đều có kiểm tra quyền server; ẩn nút không thay kiểm tra |
| NFR05 | Lỗi lưu/điều chỉnh được mô tả hoàn tác, không lưu một phần; áp dụng thanh toán mô phỏng Tier 3 |
| NFR06 | Create ghi lịch sử ban đầu; Cancel ghi from/to, actor, thời gian; vòng đời chung yêu cầu ghi lịch sử chuyển trạng thái |

## 5. Giả định không được nhầm thành quy tắc chính thức

AMB01 phân shipper; AMB02 tiền sau Failed; AMB03 giá dòng cũ/mới; AMB05 thu/hoàn chênh lệch. D01 báo giá đổi, D02 giới hạn sửa bếp/địa chỉ, D03 voucher khi sửa, D04 hoàn tiền mô phỏng lỗi, D05 chống đặt trùng vẫn là đề xuất/điểm mở. Xem [log](02-ambiguity-log.md).

Nhánh voucher khi sửa cố ý ghi D03 chưa chốt; hình không chứng minh đã có một chính sách được phê duyệt. Việc chuẩn bị/lưu kết quả thanh toán trong hình là mô phỏng; không suy ra bảo đảm hoàn tác giao dịch của cổng thanh toán thật.

## 6. Phạm vi kết quả ngày này

Sơ đồ đã được đối chiếu với đặc tả ngày 1 và bảng vòng đời của đề. Kết quả xuất hình/kiểm tra cấu trúc/kiểm tra trực quan được ghi trong [nhật ký](04-nhat-ky.md). Đây là kiểm tra thiết kế, không phải kết quả kiểm thử phần mềm, duyệt của thầy hoặc xác minh Figma online.

CRC, class/Strategy, ERD và architecture sẽ được rà soát trong các hạng mục 12–13/11. Figma V2, báo cáo Word/PDF và diễn tập thuộc các ngày tiếp theo.
