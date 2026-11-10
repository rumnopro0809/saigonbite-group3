# Nhật ký thực hiện UC01

Người phụ trách: Nguyễn Hải Triều — 524H0072. AI hỗ trợ soạn/rà soát; chưa ghi nhận Triều hoặc thầy duyệt bản mới.

## Ngày 10/11/2026 — Yêu cầu và đặc tả

### Đã thực hiện

- Đọc đặc tả chung UC01, Activity và Order State hiện có, kiến trúc, truy vết và UI specification.
- Đối chiếu nội dung với bản trích đề gốc, nhất là BR1–BR17, actor và bảng vòng đời.
- Lập kế hoạch 8 ngày 10–17/11 chỉ cho phần UC01 và các điểm tích hợp.
- Soạn đặc tả tiếng Việt đủ tạo/xem/sửa/hủy, tiền/hậu điều kiện và 10 nhóm lỗi.
- Tách 7 FR, 17 BR và 6 NFR; giữ nguyên mã baseline của nhóm.
- Đối chiếu ví dụ tính tay 205.000 VND với ví dụ trong đề.
- Giữ 4 AMB liên quan; tách 5 quyết định thiết kế khỏi quy tắc đã được đề xác định.
- Lập truy vết 13 dòng FR/NFR phục vụ các ngày thiết kế tiếp theo.

### Kết quả rà soát nội dung

| Kiểm tra | Kết quả |
|---|---|
| Tên và MSSV | Nguyễn Hải Triều — 524H0072, Group 3 |
| CRUD | Có đủ bốn luồng; hủy giữ lịch sử |
| FR/BR/NFR | Tách ba bảng, có liên kết; không đổi mã BR |
| Điều kiện sửa/hủy | Sửa Placed; khách hủy Placed/Confirmed; từ Preparing không hủy |
| Giá và snapshot | Làm tròn cuối; dùng thời điểm/tải/mưa lúc đặt; không dùng tải mới khi tính lại sau sửa |
| Thanh toán | Đủ 5 phương thức trên hai nhóm; mỗi nhóm chỉ có 3 phương thức hợp lệ; COD chờ thu tiền |
| Phân quyền | Kiểm tra server và quyền sở hữu, không chỉ ẩn nút |
| Giả định | AMB01/02/03/05 chưa xác nhận; voucher khi sửa và lỗi hoàn tiền chưa chốt |
| Bằng chứng triển khai | Không suy từ tài liệu/prototype ra kiểm thử ứng dụng đã đạt |

Kiểm tra cấu trúc bằng PowerShell sau khi tạo tài liệu: 5 tệp, 13 dòng truy vết, không trùng/thiếu mã FR/NFR, không có liên kết local hỏng; bảng đặc tả có 7 FR, 17 BR, 6 NFR. Tính lại ví dụ bằng phép toán cho kết quả 205.000 VND. Đây là kiểm tra tài liệu và số học, không phải kiểm thử ứng dụng.

### Chưa làm và bàn giao ngày 11/11

- Chưa cập nhật hoặc xuất lại Activity/state diagram trong lượt ngày 1.
- Chưa chỉnh CRC, class, ERD, architecture chung.
- Chưa thao tác Figma online; không xác nhận số màn online từ QA local.
- Chưa tạo báo cáo Word/PDF hoặc slide Part 2 trong ngày 1.
- Ngày kế tiếp: rà soát và hoàn thiện Activity UC01; đối chiếu vòng đời 8 trạng thái, 8 chuyển trạng thái nghiệp vụ; xuất hình và ghi kết quả kiểm tra.

**Kết luận ngày 1:** sản phẩm tài liệu trong phạm vi ngày 10/11 đã được soạn và rà soát với nguồn đã đọc. Toàn bộ Part 2 và các phần ngày 11–17 chưa được đánh dấu hoàn thành.
