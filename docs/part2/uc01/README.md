# Part 2 — UC01: Đặt và quản lý đơn hàng

Người phụ trách: **Nguyễn Hải Triều — 524H0072 — Group 3**.

Kế hoạch làm việc: **10–17/11/2026**, theo yêu cầu của Triều. Đây là lịch làm việc mới của cá nhân, không phải xác nhận thay đổi hạn nộp của môn học.

Mức thiết kế: sinh viên năm 3 Khoa học Máy tính; dùng MVC, EF6 Code First, SQL Server, Bootstrap và Payment Strategy theo đề. Ưu tiên thiết kế nhỏ, rõ trách nhiệm và giải thích được. Thanh toán/hoàn tiền là mô phỏng của Tier 3.

## Cách thực hiện từng ngày

Mỗi lần làm một ngày, đọc kết quả ngày trước, hoàn thành sản phẩm của ngày hiện tại, đối chiếu yêu cầu rồi cập nhật trạng thái. Hạng mục 11/11 được thực hiện sớm ngày 10/11. Ngày 14/11 đã kiểm tra bản Figma online và chuẩn bị gói cập nhật local.

| Ngày | Công việc | Sản phẩm và điều kiện hoàn thành | Trạng thái |
|---|---|---|---|
| 10/11 | Rà soát yêu cầu và viết đặc tả UC01 | Đủ tác nhân, tiền/hậu điều kiện, CRUD, luồng lỗi; tách FR/BR/NFR; giữ giả định chưa xác nhận | Đã hoàn thành bản tài liệu ngày 1; Triều chưa duyệt |
| 11/11 | Hoàn thiện Activity UC01 và đối chiếu vòng đời đơn | Activity khớp đặc tả; vòng đời dùng đúng 8 trạng thái và 8 chuyển trạng thái nghiệp vụ; có actor/guard; xuất hình | Đã rà soát và xuất 6 sơ đồ SVG/PNG; Triều chưa duyệt |
| 12/11 | CRC, class diagram và Payment Strategy | CRC các lớp UC01; interface thanh toán, 5 lớp phương thức và selector theo loại khách; đối chiếu lớp/dữ liệu chung | Có bản nháp chung, chờ rà soát UC01 |
| 13/11 | Dữ liệu và kiến trúc UC01 | Phần ERD đơn/dòng món/snapshot/payment/history; khóa và quan hệ; MVC và vị trí tính giá; liên kết NFR | Có bản nháp chung, chờ rà soát UC01 |
| 14/11 | Figma V2 — luồng chính | Xác minh bản online; nối chọn bếp/món → checkout → đặt thành công → chi tiết/theo dõi; đủ phương thức của từng loại khách | Đã kiểm tra 91 frame online và gói local 93 frame. Chờ đồng bộ và chạy Present |
| 15/11 | Figma V2 — sửa, hủy, lỗi và responsive | Sửa Placed, hủy Placed/Confirmed, lỗi tải bếp/COD/thanh toán; mobile; giữ bằng chứng Sketch/Wireframe/Mockup/Prototype; lưu kết quả kiểm tra | Chưa thực hiện |
| 16/11 | Ghép nội dung UC01 vào báo cáo Part 2 và chuẩn bị lời nói | Phần đóng góp UC01 theo mục 2.1–2.6 của mẫu chung, hình/chú thích/FR-BR/MSSV/link; bản Word/PDF đóng góp UC01; kịch bản trình bày | Chưa thực hiện |
| 17/11 | Kiểm tra cuối và ôn trình bày | Đối chiếu yêu cầu–sơ đồ–Figma–báo cáo; kiểm tra link xem công khai; sửa lỗi; diễn tập phần Triều trong bài nhóm 7–10 phút | Chưa thực hiện |

Ngày 16 chỉ hoàn thiện phần đóng góp UC01. Báo cáo nhóm vẫn cần nội dung UC02, UC03, R1/R2 và sự thống nhất của ba thành viên.

## Lưu công việc trên GitHub

Theo chỉ dẫn bổ sung của Triều: sau khi hoàn thành và kiểm tra một hạng mục ngày, commit phần việc dự án theo hạng mục rồi push lên repository hiện tại. Tên commit ghi mốc công việc; ngày tác giả/commit giữ thời điểm thực tế, không giả lập ngày tương lai. Riêng tài liệu hướng dẫn học cá nhân giữ local trong thư mục đã được Git bỏ qua, không stage, commit hoặc push. Không đưa file đầu vào riêng, bản nháp local và công cụ tải về vào commit.

## Sản phẩm ngày 10/11

- [Đặc tả UC01 tiếng Việt](01-dac-ta-uc01.md).
- [Ambiguity Log và quyết định thiết kế cần xác nhận](02-ambiguity-log.md).
- [Đối chiếu FR/NFR với thiết kế dự kiến](03-traceability.csv).
- [Nhật ký và kiểm tra ngày đầu tiên](04-nhat-ky.md).

## Sản phẩm hạng mục 11/11

- [Bộ sơ đồ và bảng đối chiếu hành vi](05-behavioral-review.md): tổng quan UC01, bốn Activity CRUD chi tiết, vòng đời đơn dùng chung.
- Đã xuất SVG/PNG, xem cả 6 hình và kiểm tra trạng thái/chuyển trạng thái; xem kết quả trong nhật ký.
- Chưa thực hiện CRC/class/ERD/Figma/báo cáo của các ngày tiếp theo. Việc kiểm tra thiết kế không thay kiểm thử ứng dụng hoặc sự duyệt của thầy.

## Nguồn và nguyên tắc đối chiếu

- Đề gốc: `SE_FinalProject_Requirements_SaigonBite_Sem01_2627.pdf`, mục 2A–2G, BR1–BR18 và phần Part 2.
- Guide: `SE_FP_SaigonBite_StudentSurvivalGuide_Sem01_2627.pdf`, phần Part 2 và Design Review.
- [Baseline FR/NFR của nhóm](../../requirements-baseline.md).
- [Đặc tả chung ba UC](../use-cases.md), [sơ đồ hiện có](../diagrams/) và [kiến trúc chung](../architecture.md).

BR giữ nguyên mã và nội dung của đề. FR/NFR giữ mã của nhóm từ Part 1. Đặc tả tiếng Việt này là phần bổ sung UC01 để tiếp tục hoàn thiện Part 2, không tự thay thế thiết kế chung hay giả lập sự đồng ý của thầy.

## Trạng thái Figma ngày 14/11

[File SaigonBite của nhóm](https://www.figma.com/design/Ao6NzrJvoRxJevn1gpn9QP).

Đã đọc frame và liên kết trong bản online 91 màn. Gói local 93 màn bổ sung theo dõi VNPay và Apple Pay, với 683 liên kết và 45 đường bắt buộc đã qua kiểm tra mô phỏng. Xem bảng đối chiếu và các bước xác nhận trên Figma Desktop. [Kết quả 14/11](06-figma-main-flow.md)

## Điều kiện chốt phần UC01 ngày 17/11

- [ ] Triều đọc và giải thích được đặc tả, FR/BR/NFR và giả định.
- [ ] Activity, state, CRC, class/Strategy, ERD và kiến trúc nhất quán.
- [ ] Các luồng UC01 dùng V2 và có bằng chứng thao tác trên Figma online.
- [ ] Có 4 giai đoạn thiết kế và bằng chứng desktop/mobile.
- [ ] Báo cáo UC01 có hình, MSSV, nguồn, truy vết và link xem được.
- [ ] Kết quả chưa kiểm tra và giả định chưa được duyệt được ghi đúng trạng thái.
- [ ] Đã kiểm tra điểm nối với UC02/UC03 và báo cáo R1/R2 của nhóm.
