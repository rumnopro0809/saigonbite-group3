# Đặc tả UC01 — Đặt và quản lý đơn hàng

Ngày cập nhật: 10/11/2026. Người phụ trách: **Nguyễn Hải Triều — 524H0072 — Group 3**.

Bản thiết kế phục vụ Part 2; chưa phải bằng chứng đã triển khai hoặc kiểm thử ứng dụng. Tham chiếu đề: mục 2A–2G, BR1–BR17. Đăng ký BR18 thuộc chức năng tài khoản hỗ trợ.

## 1. Thông tin chung

| Thuộc tính | Nội dung |
|---|---|
| Mã và tên | UC01 — Place and Manage Orders |
| Mục tiêu | Khách tạo, xem, sửa và hủy đơn theo các điều kiện nghiệp vụ |
| Tác nhân chính | Customer, gồm Local Customer và Foreign Tourist |
| Chức năng phối hợp | Tài khoản/phân quyền; thực đơn UC02; bếp/tải UC03; voucher; thanh toán mô phỏng |
| Bên cập nhật trạng thái vận hành | Kitchen Staff, Shipper và Admin trong HT03; khách theo dõi kết quả qua UC01 |
| Khởi phát | Khách bắt đầu đặt món, mở lịch sử đơn hoặc yêu cầu sửa/hủy một đơn |
| Phạm vi CRUD | Create: đặt; Read: xem/theo dõi; Update: sửa món; Delete: hủy và giữ lịch sử |

Customer là tác nhân khái quát, không phải loại tài khoản thứ ba để chọn ở checkout. Loại khách được lấy từ tài khoản đã đăng nhập. Payment Strategy là thành phần bên trong hệ thống, không phải một người dùng mới.

### Tiền điều kiện

1. Khách đã đăng nhập; việc đăng ký/xác thực được chức năng tài khoản hỗ trợ thực hiện trước đó.
2. Với tạo đơn: đã có dữ liệu bếp, thương hiệu và món; bếp được chọn đang hoạt động.
3. Với xem/sửa/hủy: đơn tồn tại và thuộc khách đang đăng nhập. Hệ thống phải kiểm tra lại ở server.
4. Các điều kiện trạng thái riêng của sửa/hủy được kiểm tra trong luồng tương ứng, không giả định mọi đơn đều sửa/hủy được.

Giỏ hàng chưa lưu không phải trạng thái thứ chín của Order. Placed là trạng thái sau khi đặt thành công.

## 2. Yêu cầu chức năng — FR

FR mô tả việc hệ thống cung cấp; các ngưỡng, công thức và điều kiện chi tiết nằm ở BR bên dưới.

| ID | Hệ thống phải làm gì? | BR liên quan |
|---|---|---|
| FR01 | Cho phép chọn bếp, món, số lượng và nhập địa chỉ để tạo đơn | BR1, BR2, BR12 |
| FR02 | Kiểm tra khả năng nhận đơn theo tải bếp và đề nghị bếp khác khi quá tải | BR8 |
| FR03 | Tính và hiển thị đầy đủ bảng giá cùng tổng thanh toán | BR3–BR11 |
| FR04 | Cho chọn phương thức phù hợp loại khách, mô phỏng xử lý và lưu kết quả | BR13, BR14; quy định Tier 3 |
| FR05 | Cho xem danh sách, chi tiết và lịch sử trạng thái đơn của mình | BR15; yêu cầu Orders mục 2E |
| FR06 | Cho sửa món, kiểm tra lại và tính lại giá theo điều kiện được phép | BR17; AMB03, AMB05 |
| FR07 | Cho hủy đơn và xử lý hoàn tiền theo quy định | BR16 |

Phụ thuộc dùng chung: FR09 (duyệt menu), FR13 (phân quyền), FR15 (voucher), FR16 (cờ mưa), FR17 (vòng đời). UC01 cung cấp dữ liệu cho FR18/R1 và FR19/R2, nhưng không nhận quyền sở hữu toàn bộ hai báo cáo.

## 3. Quy tắc nghiệp vụ — BR

| ID | Ràng buộc giữ theo đề |
|---|---|
| BR1 | Tiền món trước giảm S phải ≥ 50.000 VND |
| BR2 | Khoảng cách giao phải ≤ 8,0 km |
| BR3 | d ≤ 2 km: D = 15.000; 2 < d ≤ 5: D = 15.000 + 5.000 × ceil(d−2); 5 < d ≤ 8: D = 30.000 + 7.000 × ceil(d−5) |
| BR4 | Giờ đặt thuộc [11:00,13:00) hoặc [17:30,19:30): phụ thu R = 20% D; đầu mốc tính, cuối mốc không tính |
| BR5 | Cờ mưa lớn bật: RainFee = 10.000; tắt: 0 |
| BR6 | S ≥ 300.000: miễn D và R, vẫn áp dụng RainFee |
| BR7 | Tải bếp lúc đặt < 30%: Q = 10% S; đúng 30% không được giảm |
| BR8 | Tải = số đơn Confirmed hoặc Preparing / công suất. Tải ≥ 90% từ chối đơn mới; không đếm đơn mới vào tải để xét nhận |
| BR9 | Tối đa 1 voucher, còn hoạt động và trong hạn; kiểm tra subtotal tối thiểu trên S. PERCENT tính trên S−Q và bị giới hạn bởi cap; FIXED không vượt S−Q |
| BR10 | Tính theo thứ tự S → Q → V → F=S−Q−V → D → R → RainFee → tổng thô → làm tròn |
| BR11 | Chỉ làm tròn tổng cuối đến 1.000 gần nhất; đúng nửa làm tròn lên; không làm tròn trung gian |
| BR12 | 1–30 dòng món, mỗi dòng 1–20 đơn vị; món Available và thương hiệu được bếp đã chọn phục vụ |
| BR13 | Local: MoMo/VNPay/COD; Tourist: Apple Pay/PayPal/COD. Không dùng phương thức riêng của nhóm còn lại |
| BR14 | COD chỉ khi tổng đã làm tròn ≤ 1.000.000; shipper xác nhận thu tiền trước Delivered |
| BR15 | Chỉ chấp nhận các chuyển trạng thái được liệt kê trong đề; kiểm tra ở tầng nghiệp vụ |
| BR16 | Khách hủy ở Placed/Confirmed; bếp từ chối ở Placed phải ghi lý do; đơn bị hủy đã trả online được hoàn toàn bộ |
| BR17 | Chỉ sửa món khi Placed; tính lại bằng thời điểm đặt, tải bếp và cờ mưa đã lưu lúc đặt |

Trong tài liệu này dùng tên RainFee thay cho ký hiệu R2 của phụ thu mưa để không nhầm với báo cáo R2. Tiền tệ VND, giờ nghiệp vụ UTC+7; khoảng cách được phép lưu hoặc mô phỏng theo đề.

## 4. Luồng chính — Tạo đơn

| Bước | Khách hàng | Hệ thống |
|---|---|---|
| C1 | Chọn bếp | Hiển thị thương hiệu và món phù hợp bếp; trạng thái món và thông tin tải |
| C2 | Thêm món, số lượng; nhập địa chỉ và khoảng cách mô phỏng; có thể nhập một voucher | Tiếp nhận dữ liệu, chỉ ra trường thiếu hoặc không hợp lệ |
| C3 | Yêu cầu xem giá | Đọc giá món, công suất/tải bếp, giờ đặt dự kiến, cờ mưa; kiểm tra BR1/2/8/12 và voucher BR9 |
| C4 | Kiểm tra bảng giá | Tính đúng BR3–BR11; hiển thị S, Q, V, F, D, R, RainFee và tổng làm tròn |
| C5 | Chọn phương thức | Chỉ hiển thị phương thức hợp lệ cho loại khách; kiểm tra thêm giới hạn COD |
| C6 | Xác nhận đặt | Kiểm tra lại dữ liệu ở server tại thời điểm đặt. Nếu giá/điều kiện thay đổi so với báo giá, yêu cầu xác nhận lại (đề xuất D01) |
| C7 | Nhận kết quả | Chọn Strategy tương ứng. Online: mô phỏng thành công/thất bại. COD: ghi nhận chờ thu tiền, không coi là đã thu |
| C8 | Xem đơn mới | Khi xử lý thành công, lưu Order ở Placed, dòng món, snapshot, payment và lịch sử ban đầu trong một giao dịch; trả mã đơn |

Điểm kiểm tra lại C6 tránh dùng dữ liệu cũ trên giao diện làm quyết định cuối. Nếu thất bại thì không tạo một đơn được chấp nhận dở dang. Việc lưu payment ở đây thuộc mô phỏng Tier 3.

### Ví dụ đối chiếu tính giá từ đề

S = 200.000; tải 20%; d = 3,2 km; 15:00; không mưa, không voucher, chọn COD.

- Q = 20.000; V = 0; F = 180.000.
- D = 15.000 + 5.000 × ceil(3,2−2) = 25.000.
- R = 0; RainFee = 0; tổng thô và tổng sau làm tròn = **205.000 VND**.
- COD hợp lệ vì tổng không vượt 1.000.000.

Đây là ví dụ tính tay để đối chiếu thiết kế, không phải kết quả chạy ứng dụng.

## 5. Luồng xem và theo dõi

1. Khách mở Đơn hàng của tôi.
2. Hệ thống chỉ truy vấn đơn theo tài khoản đang đăng nhập.
3. Khách mở chi tiết; server kiểm tra quyền sở hữu lần nữa.
4. Hiển thị món/số lượng/giá đã lưu, phân tích tổng tiền, phương thức và tình trạng thanh toán, trạng thái đơn và lịch sử theo thời gian.
5. Hiển thị thao tác sửa/hủy theo trạng thái. Server vẫn kiểm tra điều kiện khi nhận yêu cầu, dù nút đã được ẩn.

HT03 cập nhật các trạng thái vận hành; khách không có quyền đặt trực tiếp trạng thái Ready, Delivered hoặc Failed.

## 6. Luồng sửa món

1. Khách chọn sửa một đơn thuộc tài khoản mình.
2. Server kiểm tra trạng thái hiện tại phải là Placed. Nếu đã Confirmed hoặc muộn hơn thì từ chối và tải lại thông tin.
3. Khách thay đổi các dòng món/số lượng. Phạm vi ngày 1 đề xuất giữ nguyên bếp, địa chỉ và khoảng cách; khả năng sửa địa chỉ chưa được chốt trong đề (D02).
4. Kiểm tra lại số dòng, số lượng, món hợp lệ và subtotal. Dùng tải/giờ/cờ mưa lúc đặt theo BR17 khi tính lại; không thay tải mới vào snapshot.
5. Áp dụng giá dòng cũ/dòng mới theo giả định AMB03. Quy tắc voucher hết hạn khi sửa cần xác nhận riêng (D03).
6. Hiển thị bảng giá mới và phần chênh lệch. Online đã trả: mô phỏng thu thêm/hoàn bớt theo AMB05. COD: kiểm tra lại tổng làm tròn không vượt giới hạn.
7. Kiểm tra trạng thái/phiên bản đơn trước khi lưu. Nếu bếp đã xác nhận trong lúc khách sửa, từ chối lưu và yêu cầu tải lại.
8. Lưu dòng món, bảng giá và kết quả điều chỉnh cùng nhau. Nếu điều chỉnh thất bại, giữ đơn và số tiền cũ.

Sửa món không tạo thêm trạng thái Order. Snapshot giữ điều kiện tính giá ban đầu, không có nghĩa tổng tiền luôn giữ nguyên sau khi đổi số lượng.

## 7. Luồng hủy đơn

1. Khách chọn hủy đơn của mình và xác nhận thao tác.
2. Server kiểm tra lại quyền sở hữu và trạng thái mới nhất.
3. Nếu Placed hoặc Confirmed, xử lý chuyển Cancelled; online đã trả phải hoàn đầy đủ theo BR16, lưu kết quả hoàn tiền và lịch sử. COD chưa thu không có tiền đã thu để hoàn.
4. Giữ Order, dòng món và các bản ghi thanh toán/lịch sử; không xóa vật lý.
5. Nếu Preparing trở đi hoặc đơn đã kết thúc, từ chối thao tác. Kết quả lỗi mô phỏng hoàn tiền xử lý theo đề xuất D04, chưa tự coi là quy tắc của thầy.

Bếp từ chối Placed có lý do và Admin hủy Confirmed thuộc HT03, dùng cùng quy tắc trạng thái/hoàn tiền; không gộp quyền của họ vào quyền Customer.

## 8. Luồng thay thế và lỗi

| Mã | Tình huống | Kết quả thiết kế |
|---|---|---|
| E01 | Subtotal dưới mức tối thiểu; khoảng cách quá giới hạn; sai số dòng/số lượng | Báo lỗi cụ thể; cho chỉnh giỏ; không tạo đơn |
| E02 | Bếp ngừng hoạt động hoặc tải ≥ 90% lúc tạo | Thông báo không nhận đơn, đề nghị bếp khác |
| E03 | Món hết bán hoặc không thuộc bếp | Chỉ ra món không hợp lệ để khách chỉnh lại |
| E04 | Voucher không hợp lệ | Nêu nguyên nhân, cho bỏ/đổi voucher và xem lại giá; không âm thầm tính theo một giá khác |
| E05 | Sai nhóm thanh toán hoặc COD vượt giới hạn | Từ chối ở server, cho chọn phương thức hợp lệ |
| E06 | Thanh toán mô phỏng thất bại | Không tạo đơn được chấp nhận; thông báo thất bại và cho thử lại |
| E07 | Truy cập đơn của người khác | Từ chối truy cập, không trả chi tiết đơn |
| E08 | Trạng thái đổi trong lúc sửa/hủy | Từ chối thao tác cũ, tải lại trạng thái; không ghi đè kết quả của bếp |
| E09 | Lỗi khi lưu Order/OrderLine/payment/history | Hoàn tác giao dịch; không để lại đơn lưu một phần |
| E10 | Khách bấm xác nhận nhiều lần | Đề xuất D05 dùng mã yêu cầu để trả lại đơn đã tạo, tránh tạo trùng |

## 9. Hậu điều kiện

| Kết quả | Điều phải đúng |
|---|---|
| Tạo thành công | Một đơn Placed với đủ dòng món, snapshot, bảng giá, payment và lịch sử; khách nhận được mã đơn |
| Xem | Chỉ trả dữ liệu được phép; không đổi trạng thái |
| Sửa thành công | Đơn vẫn Placed; dữ liệu mới hợp lệ, tính theo snapshot; có kết quả điều chỉnh nếu có |
| Hủy thành công | Đơn Cancelled, có lịch sử; nếu đã trả online thì có hoàn đầy đủ theo BR16 |
| Từ chối/lỗi | Không có thay đổi trái phép hoặc đơn lưu dở dang; lỗi sửa không làm mất dữ liệu cũ |

## 10. NFR áp dụng cho UC01

| ID | Mục tiêu chất lượng | Cách dự kiến kiểm chứng ở giai đoạn triển khai |
|---|---|---|
| NFR01 | Tính giá < 1,5 giây với tối đa 30 dòng món | Đo thời gian xử lý tính giá trên môi trường được ghi rõ; không suy từ thời gian bấm prototype |
| NFR02 | MVC, EF Code First; tách tính giá; thêm phương thức thanh toán bằng lớp mới và đăng ký, không sửa các lớp thanh toán cũ | Đọc cấu trúc lớp và diff; kiểm tra migration tạo/nâng cấp DB |
| NFR03 | Responsive Bootstrap, hỗ trợ Việt/Anh | Kiểm tra menu/checkout/chi tiết/sửa/hủy tại 375, 768, 1366 px theo kế hoạch nhóm; hai ngôn ngữ |
| NFR04 | Mật khẩu băm trong phần tài khoản; kiểm tra vai trò và quyền sở hữu ở server | Thử mở đơn người khác, gọi trực tiếp thao tác bị cấm; đọc phần lưu mật khẩu |
| NFR05 | Lưu đơn và dòng món nguyên tử | Gây lỗi lưu giữa chừng và kiểm tra hoàn tác |
| NFR06 | Mọi chuyển trạng thái thành công có OrderId, trạng thái trước/sau, actor và thời gian; bếp từ chối có lý do | Đối chiếu thao tác với OrderStatusHistory |

NFR01–04 là baseline bắt buộc; NFR05–06 là hai mục nhóm bổ sung. Đây là mục tiêu và kế hoạch kiểm chứng, chưa phải kết quả đạt.

## 11. Đầu ra cho các ngày tiếp theo

- Activity phải thể hiện CRUD, nhánh lỗi và thanh toán/snapshot; state diagram chung giữ đúng BR15.
- CRC/class dùng các trách nhiệm đặt đơn, tính giá, chọn payment, truy vấn, sửa/hủy và lưu dữ liệu; chưa tăng số lớp chỉ để sơ đồ trông phức tạp.
- Dữ liệu tối thiểu: Order, OrderLine, snapshot điều kiện/giá, PaymentRecord, OrderStatusHistory; tham chiếu User/Hub/MenuItem/Voucher của nhóm.
- Figma V2 cần luồng tạo/xem/sửa/hủy, Local/Tourist, COD vượt ngưỡng, tải bếp và lỗi; còn phải kiểm tra online.
- Các giả định và quyết định mở: xem [Ambiguity Log](02-ambiguity-log.md). Không đánh dấu thầy đã duyệt khi chưa có phản hồi.
