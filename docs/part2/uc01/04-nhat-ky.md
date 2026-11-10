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

## Hạng mục 11/11/2026 — Activity và vòng đời đơn

Thực hiện sớm ngày 10/11/2026 theo yêu cầu tiếp tục của Triều; ngày trong tiêu đề là mốc của kế hoạch, không phải giả lập thời điểm thực hiện.

### Đã thực hiện

- Đọc đặc tả ngày 1, ambiguity log, sơ đồ cũ và bảng chuyển trạng thái mục 2C của đề.
- Rút Activity chung thành tổng quan bốn thao tác; bổ sung bốn hình CRUD chi tiết có swimlane Customer/System.
- Create làm rõ kiểm tra lại báo giá, phương thức, nhánh COD chờ thu tiền/online mô phỏng, lỗi thanh toán/lưu và đề xuất chống trùng.
- Read làm rõ dữ liệu theo chủ đơn, nhánh rỗng và kiểm tra quyền khi mở chi tiết.
- Edit dùng snapshot BR17; có xác nhận tổng mới, kiểm tra COD, điều chỉnh mô phỏng và kiểm tra trạng thái/phiên bản trước lưu.
- Cancel kiểm tra Placed/Confirmed, hoàn đầy đủ online, giữ lịch sử và tách D04 là đề xuất.
- Cập nhật state diagram dùng chung với đúng 8 trạng thái, 8 chuyển trạng thái nghiệp vụ; actor/guard rõ; AMB01/02 không trở thành quy tắc chính thức.
- Soạn bảng đối chiếu và tài liệu học riêng giữ local theo yêu cầu. Không stage, commit hoặc push.

### Bằng chứng kiểm tra

| Kiểm tra | Kết quả |
|---|---|
| PlantUML local | 6 nguồn xuất thành công thành 6 SVG và 6 PNG; không gọi dịch vụ vẽ online |
| Độ mới của hình | Mỗi SVG/PNG được sinh sau lần sửa nguồn tương ứng |
| Kiểm tra trực quan | Đã mở cả 6 PNG; chữ/nút/nhãn nhánh hiển thị, các bước và lane đọc được; SVG dùng để phóng lớn |
| State declarations | 8 |
| Business transition pairs | 8, khớp danh sách đã đối chiếu bảng 2C; không thêm đường tắt |
| Trạng thái cuối | Không có chuyển nghiệp vụ đi ra từ Delivered/Cancelled/Failed |
| Liên kết Markdown | Không có đường dẫn local hỏng trong các tài liệu UC01 và file học tại thời điểm kiểm tra |
| Tài liệu học | `git check-ignore` xác nhận thuộc quy tắc `/outputs/`; `git ls-files` không có file trong thư mục học |
| Kiểm tra diff | `git diff --check` không có lỗi khoảng trắng; các thay đổi vẫn local |

### Giới hạn và bàn giao

- Kiểm tra render/cấu trúc/đối chiếu không phải kiểm thử ứng dụng hoặc kiểm chứng NFR hiệu năng.
- Triều chưa được ghi nhận đã đọc/duyệt; thầy chưa xác nhận AMB và các quyết định D.
- Chưa thao tác Figma online, chưa sửa CRC/class/ERD/architecture, chưa tạo Word/PDF trong hạng mục này.
- Ngày tiếp theo theo lịch **12/11**: CRC, class và Payment Strategy; dùng các trách nhiệm đã hiện trong Activity làm đầu vào.

**Kết luận hạng mục 11/11:** phần việc thiết kế hành vi đã hoàn thành và có hình/đối chiếu. Phần học riêng dành cho Triều tự luyện, không phải minh chứng đã được Triều duyệt toàn bộ thiết kế.

### Chỉ dẫn Git bổ sung sau khi hoàn thành hạng mục

Triều xác nhận công việc dự án theo ngày cần được commit và push, riêng tài liệu học vẫn chỉ local. Đã tách commit ngày 10/11 `5f17f99` cho yêu cầu/đặc tả. Phần 11/11 được đóng gói riêng gồm sơ đồ, đối chiếu và cập nhật tiến độ. Các ghi chú “không stage/commit/push” phía trên mô tả thời điểm soạn ban đầu, trước chỉ dẫn bổ sung này. Không đổi ngày commit để giả lập ngày thực hiện; không đưa file học vào Git.
