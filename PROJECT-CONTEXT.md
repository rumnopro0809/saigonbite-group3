# SaigonBite — Project Context Handoff

> **Mục đích của file này.** Đây là file "một-cửa" để đưa cho một AI khác (hoặc người mới) hiểu ngay dự án đang làm gì, đang ở đâu, và **cần đọc những file nào trên máy này**. Mọi đường dẫn bên dưới là đường dẫn tuyệt đối trên máy của chủ dự án (Windows 11). Hãy đọc mục 1 trước, rồi mở các file ở mục 10 theo đúng thứ tự ưu tiên.

- Cập nhật lần cuối: **19/11/2026** (Asia/Ho_Chi_Minh)
- Trạng thái: **Part 2 đã đóng gói xong và đã push GitHub**; Part 3 (implementation + test) **chưa làm**.

---

## 1. Tóm tắt 30 giây

Nhóm 3 môn Software Engineering (Sem01_2627) làm đề tài **SaigonBite** — nền tảng **cloud kitchen giao đồ ăn** cho **khách địa phương (Local Customer)** và **khách du lịch nước ngoài (Foreign Tourist)** tại TP.HCM. Bài chia 3 phần theo môn: Part 1 (requirements/proposal), **Part 2 (design, architecture, UI/UX — đã xong)**, Part 3 (implement + test — chưa làm).

Ba use case chính, mỗi bạn phụ trách một cái:

| Use case | Nội dung | Chủ sở hữu | MSSV |
|---|---|---|---|
| **UC01** | Place and Manage Orders (đặt, xem/theo dõi, sửa, hủy đơn) | Nguyễn Hải Triều | **524H0072** |
| **UC02** | Manage Menu and Brands (CRUD thương hiệu & món) | Dương Phạm Thái | **524H0189** |
| **UC03** | Manage Hubs and Capacity (CRUD bếp trung tâm & công suất) | Lê Quang Tấn | **524H0188** |

Công nghệ chốt ở **Tier 3**: ASP.NET MVC 5 / .NET Framework 4.8, EF6 Code First, SQL Server, Bootstrap. **Thanh toán, hoàn tiền, OTP, mã email đều là mô phỏng** — không có giao dịch thật, và bài **không được phép nói là đã implement/test** khi chưa làm Part 3.

---

## 2. Schema dự án (dạng máy đọc)

```yaml
project:
  name: SaigonBite
  course: Software Engineering — Sem01_2627
  team: Group 3
  members:
    - { name: Nguyễn Hải Triều, id: 524H0072, owns: [UC01, payments, pricing] }
    - { name: Dương Phạm Thái, id: 524H0189, owns: [UC02, catalog, accounts] }
    - { name: Lê Quang Tấn,   id: 524H0188, owns: [UC03, hubs, capacity] }
  domain: cloud-kitchen food delivery, Ho Chi Minh City
  actors: [Local Customer, Foreign Tourist, Kitchen Staff, Shipper, System Admin]
  customer_generalization: "Customer (abstract) <|-- Local Customer, Foreign Tourist"

deliverables:
  part1: { status: submitted, files: output/part1/* }   # có lỗi MSSV còn placeholder, xem mục 9
  part2: { status: submitted, deadline: 2026-11-20, files: [pdf, pptx, readme.txt] }
  part3: { status: not_started }

stack:
  runtime: .NET Framework 4.8
  web: ASP.NET MVC 5
  orm: Entity Framework 6 (Code First + migrations)
  db: SQL Server (local instance MSSQL$SQLEXPRESS)
  ui: Bootstrap, responsive, VI/EN switching
  payments: simulated (Tier 3) + Payment Strategy pattern
  projects_planned: [SaigonBite.Domain, SaigonBite.Application,
                     SaigonBite.Infrastructure, SaigonBite.Web, SaigonBite.Tests]

requirement_ids:
  functional: FR01–FR19 (FR14 chia 14.1–14.4)
  business_rules: BR1–BR18
  quality: NFR01–NFR06
  assumptions: AMB01–AMB06
  traceability: docs/part2/traceability.csv

order_lifecycle:
  states: [Placed, Confirmed, Preparing, Ready, Out for Delivery, Delivered, Cancelled, Failed]
  business_transitions: 8
  terminal: [Delivered, Cancelled, Failed]
  edit_allowed_only_in: Placed

payment:
  interface: IPaymentStrategy
  selector: PaymentSelector
  strategies: [MoMoStrategy, VNPayStrategy, ApplePayStrategy, PayPalStrategy, CodStrategy]
  local_customer: [MoMo, VNPay, COD]
  foreign_tourist: [Apple Pay, PayPal, COD]
  cod_limit_vnd: 1000000

pricing:
  owner_class: PricingService
  location: business/service layer (Application), NOT controller, NOT view, NOT EF entity
  order: [S, Q, V, F=S-Q-V, D, R, RainFee, raw_total, round_half_up_1000]
  rounding: once, on the final total only
```

---

## 3. Bài toán nghiệp vụ (vì sao có các rule này)

SaigonBite là **cloud kitchen**: không có mặt bằng ăn tại chỗ, chỉ có các **hub (bếp trung tâm)** nấu và giao trong bán kính ngắn. Vì vậy thiết kế xoay quanh 3 ràng buộc thật:

1. **Bếp có công suất hữu hạn** → phải chặn nhận đơn khi bếp quá tải (BR8), và thưởng cho khách đặt lúc bếp rảnh (BR7 "Quiet Kitchen discount" 10%).
2. **Giao trong 8 km** → phí giao tính bậc thang theo khoảng cách, có phụ thu giờ cao điểm và phụ thu mưa lớn.
3. **Hai nhóm khách khác nhau về định danh và thanh toán** → Local dùng số điện thoại 10 số + OTP mô phỏng, Tourist dùng passport + email code; Local chỉ thấy MoMo/VNPay/COD, Tourist chỉ thấy Apple Pay/PayPal/COD.

Giá là trung tâm của bài: mọi con số phải tính lại được từ **snapshot lúc đặt** (thời điểm, tải bếp, cờ mưa, đơn giá món), không được lấy giá hiện tại của menu.

---

## 4. Quy tắc nghiệp vụ BR1–BR18 (bản rút gọn, đúng theo đề)

| ID | Nội dung |
|---|---|
| BR1 | Tiền món trước giảm giá `S` ≥ 50.000 VND |
| BR2 | Khoảng cách giao ≤ 8,0 km |
| BR3 | Phí giao `D`: d ≤ 2 km → 15.000; 2 < d ≤ 5 → 15.000 + 5.000×ceil(d−2); 5 < d ≤ 8 → 30.000 + 7.000×ceil(d−5) |
| BR4 | Giờ cao điểm [11:00,13:00) hoặc [17:30,19:30) → phụ thu `R` = 20% × D (đầu mốc tính, cuối mốc không) |
| BR5 | Cờ mưa lớn bật → `RainFee` = 10.000, tắt → 0 |
| BR6 | `S` ≥ 300.000 → miễn `D` và `R`, nhưng vẫn áp dụng `RainFee` |
| BR7 | Tải bếp lúc đặt < 30% → `Q` = 10% × S; đúng 30% **không** được giảm |
| BR8 | Tải = (# đơn Confirmed + Preparing) / capacity. Tải ≥ 90% → từ chối đơn mới và gợi ý hub khác. Không tính đơn mới vào tải khi xét nhận |
| BR9 | Tối đa 1 voucher, phải còn hiệu lực; min-subtotal kiểm trên `S`; PERCENT tính trên `S−Q` và bị cap; FIXED không vượt `S−Q` |
| BR10 | Thứ tự tính: S → Q → V → F=S−Q−V → D → R → RainFee → tổng thô → làm tròn |
| BR11 | Chỉ làm tròn **một lần** ở tổng cuối, half-up về bội số 1.000; không làm tròn trung gian |
| BR12 | 1–30 dòng món, mỗi dòng 1–20 đơn vị; món phải Available và thuộc brand mà hub đã chọn phục vụ |
| BR13 | Local: MoMo/VNPay/COD; Tourist: Apple Pay/PayPal/COD; không dùng chéo phương thức của nhóm kia |
| BR14 | COD chỉ khi tổng **đã làm tròn** ≤ 1.000.000; shipper xác nhận đã thu tiền trước khi Delivered |
| BR15 | Chỉ chấp nhận đúng các chuyển trạng thái trong vòng đời; kiểm ở business layer |
| BR16 | Khách hủy được ở Placed/Confirmed; bếp từ chối đơn ở Placed phải ghi lý do; đơn đã trả online khi bị hủy phải hoàn 100% (mô phỏng) |
| BR17 | Chỉ sửa món khi đơn ở Placed; tính lại giá bằng thời điểm đặt / tải bếp / cờ mưa **đã lưu lúc đặt** |
| BR18 | Đăng ký: Local = SĐT 10 số duy nhất bắt đầu 03/05/07/08/09 + OTP mô phỏng; Tourist = passport 6–9 ký tự chữ-số duy nhất + quốc tịch + email hợp lệ + email code mô phỏng; mật khẩu ≥ 8 ký tự và phải hash |

## 5. Yêu cầu chức năng FR01–FR19 (một dòng mỗi cái)

| ID | Nội dung ngắn | UC |
|---|---|---|
| FR01 | Tạo đơn: địa chỉ, 1–30 dòng, SL 1–20, S ≥ 50k, ≤ 8 km, món Available thuộc hub | UC01 |
| FR02 | Từ chối khi tải bếp ≥ 90% và gợi ý hub khác | UC01, UC03 |
| FR03 | Tính & hiển thị đủ S/Q/V/F/D/R/RainFee/tổng, làm tròn 1 lần | UC01 |
| FR04 | Cung cấp đúng phương thức theo loại khách, COD ≤ 1.000.000, thanh toán mô phỏng | UC01 |
| FR05 | Xem danh sách/chi tiết/lịch sử đơn **của chính mình** | UC01 |
| FR06 | Sửa món khi Placed, validate lại, tính lại theo snapshot | UC01 |
| FR07 | Hủy ở Placed/Confirmed; hoàn tiền mô phỏng nếu đã trả online | UC01 |
| FR08 | Admin tạo brand & menu item (brand, category, giá, availability) | UC02 |
| FR09 | Xem danh sách/chi tiết brand & món; khách duyệt menu theo hub | UC02, UC01 |
| FR10 | Admin sửa brand/món/giá/category/trạng thái; từ chối món unavailable | UC02 |
| FR11 | Admin ngưng/soft-delete brand & món, giữ tham chiếu trong đơn cũ | UC02 |
| FR12 | Đăng ký/đăng nhập 2 loại khách, hồ sơ cá nhân, Admin quản lý tài khoản | HT01 |
| FR13 | Xác thực + phân quyền server-side + giới hạn dữ liệu theo owner/scope | HT01, toàn hệ thống |
| FR14.1 | Admin tạo hub (vị trí, capacity nguyên > 0, brand hỗ trợ) | UC03 |
| FR14.2 | Admin xem hub & tải hiện tại (theo BR8) | UC03 |
| FR14.3 | Admin sửa hub/capacity/brand | UC03 |
| FR14.4 | Admin ngưng/soft-delete hub, giữ đơn cũ; hub inactive không nhận đơn mới | UC03 |
| FR15 | Admin CRUD voucher PERCENT/FIXED (value, cap, min subtotal, hạn, active) | HT02, UC01 |
| FR16 | Admin bật/tắt cờ mưa lớn toàn hệ thống; lưu vào snapshot khi đặt | UC03, UC01 |
| FR17 | Chỉ đúng các transition + role trong lifecycle, reject phần còn lại | HT03, UC01 |
| FR18 | Báo cáo R1: doanh thu theo ngày/category/phương thức, loại Cancelled/Failed | HT04 |
| FR19 | Báo cáo R2: số đơn đã xử lý, thời gian chuẩn bị trung bình, tải hiện tại, số lượt giảm Q | HT04, UC03 |

## 6. NFR01–NFR06

| ID | Loại | Nội dung |
|---|---|---|
| NFR01 | Performance (mandatory) | Tính xong toàn bộ giá cho đơn 30 dòng trong **< 1,5 giây** |
| NFR02 | Maintainability (mandatory) | MVC + EF Code First + migrations; tách pricing khỏi controller/view; **Payment Strategy**: thêm phương thức mới = thêm 1 class + đăng ký, không sửa class cũ |
| NFR03 | Usability (mandatory) | Bootstrap responsive (375 / 768 / 1366 px), chuyển VI/EN cho các luồng chính |
| NFR04 | Security (mandatory) | Hash mật khẩu, không lưu plaintext; phân quyền role + data-level ở server |
| NFR05 | Reliability (team-added) | Lưu order + order lines **atomic**, không để lại đơn lưu dở |
| NFR06 | Auditability (team-added) | Ghi 100% transition: order id, from/to, actor, timestamp; lý do khi bếp từ chối |

## 7. Kiến trúc & mô hình

### 7.1 Layers

| Project (planned) | Trách nhiệm |
|---|---|
| `SaigonBite.Domain` | Entities, value objects, lifecycle enums, contracts — **không** phụ thuộc System.Web/EF |
| `SaigonBite.Application` | Order/menu/hub/voucher/state/report services, pricing, payment selection |
| `SaigonBite.Infrastructure` | EF6 mappings, migrations, persistence, simulated adapters |
| `SaigonBite.Web` | MVC controllers, ViewModels, Razor, Bootstrap, EN/VI resources, composition |
| `SaigonBite.Tests` | Unit + integration checks (EP/BVA, authorization) |

**Pricing nằm ở Application/Domain service (`PricingService`)**, không nằm trong controller, view hay EF entity — vì mọi caller (đặt đơn, sửa đơn, báo cáo, test) phải dùng đúng một bộ công thức, và test phải chạy được không cần browser.

### 7.2 Payment Strategy

`PaymentSelector` nhận `Customer` + phương thức khách chọn, từ chối nếu sai nhóm khách, rồi trả về đúng `IPaymentStrategy`. Mỗi phương thức là một class riêng (`MoMoStrategy`, `VNPayStrategy`, `ApplePayStrategy`, `PayPalStrategy`, `CodStrategy`). COD chưa phải "đã thu tiền" — nó chờ shipper xác nhận trước Delivered.

### 7.3 Vòng đời đơn — 8 trạng thái / 8 transition nghiệp vụ

`Placed → Confirmed → Preparing → Ready → Out for Delivery → Delivered`, nhánh `Placed/Confirmed → Cancelled`, nhánh `Out for Delivery → Failed`. `Delivered`, `Cancelled`, `Failed` là trạng thái kết thúc. **Không** có `Preparing → Cancelled`, **không** có tắt `Ready → Delivered`. Sửa món chỉ khi `Placed` và **không** đổi trạng thái.

### 7.4 Dữ liệu (14 bảng)

`User`, `LocalProfile`, `TouristProfile`, `StaffProfile`, `Hub`, `Brand`, `HubBrand`, `MenuItem`, `Voucher`, `SystemSetting`, `Order`, `OrderLine`, `PaymentRecord`, `OrderStatusHistory`.
Điểm quan trọng: customer generalization nằm ở domain, nhưng SQL dùng **một bảng Users + bảng profile riêng**; loại khách do **server** suy ra từ tài khoản đã đăng nhập, không tin field client gửi lên. Tiền dùng `decimal(19,4)`; thời gian `datetimeoffset`, nghiệp vụ theo UTC+7.

### 7.5 Giả định còn mở AMB01–AMB06 (đề chưa trả lời)

Shipper do Admin gán thủ công khi Ready (AMB01); đơn Failed không tự hoàn tiền (AMB02); sửa đơn giữ đơn giá cũ, món thêm dùng giá lúc thêm (AMB03); R1 chỉ tính Delivered theo thời điểm giao thành công (AMB04); chênh lệch khi sửa đơn đã trả → charge/refund mô phỏng (AMB05); R2: "đã xử lý" = từng đạt Confirmed (AMB06). **Đây là đề xuất của nhóm, không phải rule chính thức** — không được viết như đã được duyệt.

---

## 8. Sản phẩm Part 2 (đã nộp)

Thư mục: `C:\Users\rumno\OneDrive\Tài liệu\ChatGPT\Công Nghệ Phần Mềm (Cuối Kì)\output\part2\`

| File | Nội dung | Kiểm chứng |
|---|---|---|
| `Team3_Part2_Design.pdf` | Báo cáo thiết kế 18 trang, mục 2.1–2.6 + Appendix A/B/C, **26 PDF bookmarks** | 61 refs FR · 48 BR · 25 NFR · 23 ảnh · đủ 3 MSSV |
| `Team3_Part2_Presentation.pptx` | Deck Design Review **12 slide**, có speaker notes và phân thời gian 0:00→9:40 cho 3 người | 12/12 slide có notes |
| `readme.txt` | Link Figma prototype + design, link GitHub, 3 chủ sở hữu UC |  |

**Figma (chỉ bản V2, đúng yêu cầu của chủ dự án):**

- Prototype công khai: `https://www.figma.com/proto/Ao6NzrJvoRxJevn1gpn9QP`
- File thiết kế: `https://www.figma.com/design/Ao6NzrJvoRxJevn1gpn9QP`
- File key `Ao6NzrJvoRxJevn1gpn9QP`; trang `04 Prototype` chứa toàn bộ frame V2; quyền **Anyone with the link → can view**; đã test bằng trình duyệt không đăng nhập.
- Start point trong prototype: `SaigonBite V2 · Khám phá món`, `SaigonBite V2 · Quản trị`, `SaigonBite V2 · UC01 Sửa, hủy và lỗi`.

**GitHub:** `https://github.com/rumnopro0809/saigonbite-group3` (nhánh `main`) — commit cuối `4477b58`.

---

## 9. Tình trạng & việc còn lại

**Đã xong**
- Part 1 nộp xong (bản Tier 3, template chính thức).
- Part 2: báo cáo + slide + readme + link Figma công khai + checker tự động pass toàn bộ.
- Code duy nhất đã viết: logic thuần C# cho pricing và lifecycle — `src\SaigonBite.Domain\Pricing.cs`, `src\SaigonBite.Domain\OrderLifecycle.cs`, kiểm tra trong `tests\SaigonBite.Domain.Checks\Program.cs` (**53 check chạy pass**). Đây **không phải** ứng dụng MVC hoàn chỉnh và không phải bằng chứng Part 3.

**Chưa xong / rủi ro**
1. ⚠️ **Repo GitHub đang private** — người ngoài (giảng viên) mở link sẽ 404. Cần chuyển public hoặc mời giảng viên làm collaborator.
2. Chưa upload lên eLearning và thư mục Google Drive chung (việc của nhóm, không tự động hoá được).
3. Part 3 chưa làm: chưa có project MVC 5, chưa có migration, chưa có test runner/integration test.
4. `output\part1\` là bản local (bị gitignore) vẫn còn dòng **"MSSV chờ bổ sung"** ở phần của Tấn — nếu giảng viên xem lại Part 1 thì phải xuất lại.
5. AMB01–AMB06 vẫn là đề xuất, chưa có trả lời của giảng viên.

---

## 10. Đường dẫn file trên máy — đưa AI đọc theo thứ tự này

**Gốc dự án:** `C:\Users\rumno\OneDrive\Tài liệu\ChatGPT\Công Nghệ Phần Mềm (Cuối Kì)`

### Ưu tiên 1 — hiểu đề và rule (bắt buộc đọc)

| # | File | Vì sao cần |
|---|---|---|
| 1 | `...\docs\requirements-baseline.md` | **Nguồn chính thức** của FR01–FR19, NFR01–NFR06, AMB01–AMB06 (13 KB). Mọi tranh luận về requirement lấy file này làm chuẩn |
| 2 | `...\docs\part2\uc01\01-dac-ta-uc01.md` | Đặc tả UC01 tiếng Việt + **bảng BR1–BR18 đầy đủ** + luồng chính/ngoại lệ |
| 3 | `...\docs\part2\use-cases.md` | Đặc tả 3 use case theo format báo cáo (main/alternate/exception/postconditions) |
| 4 | `...\docs\part2\README.md` | Bản tổng hợp Part 2: sơ đồ UC, bảng CRC rút gọn, principles → NFR, kế hoạch thuyết trình |

### Ưu tiên 2 — thiết kế chi tiết

| # | File | Vì sao cần |
|---|---|---|
| 5 | `...\docs\part2\architecture.md` | ADR, layers, schema 13 bảng, money/time, snapshot, transaction/concurrency, báo cáo |
| 6 | `...\docs\part2\crc-cards.md` | 10 CRC card đầy đủ (OrderService, PricingService, PaymentSelector/IPaymentStrategy, OrderTransitionService, MenuService, HubService/HubLoadService, VoucherService, 2 report service) |
| 7 | `...\docs\part2\traceability.csv` | FR → UC → BR → design → component → test group → trạng thái implement |
| 8 | `...\docs\part2\ui-specification.md` | Spec màn hình + checklist Figma |
| 9 | `...\docs\part2\diagrams\` | Toàn bộ UML: `.puml` (nguồn) + `.svg` + `.png` — overall UC, 3 activity, 4 UC01 CRUD activity, order state, class/payment, ERD, architecture |

### Ưu tiên 3 — lịch sử và bằng chứng

| # | File | Vì sao cần |
|---|---|---|
| 10 | `...\docs\project-status.md` | Trạng thái dự án, dependency, thứ tự thực thi đề xuất |
| 11 | `...\docs\part2\uc01\04-nhat-ky.md` | Nhật ký theo ngày 10/11 → 19/11 của UC01 (bằng chứng tiến độ) |
| 12 | `...\docs\part2\uc01\05-behavioral-review.md`, `06-figma-main-flow.md`, `07-figma-order-management.md` | Đối chiếu hành vi và kiểm tra Figma |
| 13 | `...\docs\part2\uc01\03-traceability.csv` | Truy vết riêng FR01–FR07 |
| 14 | `...\outputs\instructor-source\` | **Đề gốc của giảng viên**: `SE_FinalProject_Requirements_SaigonBite_Sem01_2627.pdf`, `SE_FP_SaigonBite_CaseStudy_Sem01_2627.pdf`, `SE_FP_SaigonBite_StudentSurvivalGuide_Sem01_2627.pdf`, `SE_Sem01_2627_Report_Template.docx`, marking sheet & workbook |

### Ưu tiên 4 — sản phẩm nộp

| # | File | Vì sao cần |
|---|---|---|
| 15 | `...\output\part2\Team3_Part2_Design.pdf` | Báo cáo nộp chính (18 trang, có bookmark) |
| 16 | `...\output\part2\Team3_Part2_Presentation.pptx` | Slide nộp chính (12 slide + notes) |
| 17 | `...\output\part2\readme.txt` | Link Figma/GitHub + owner |
| 18 | `...\README.md` | Trang chủ repo, tóm tắt stack + link Part 2 |

### Ưu tiên 5 — code hiện có (rất ít)

| # | File | Nội dung |
|---|---|---|
| 19 | `...\src\SaigonBite.Domain\Pricing.cs` | Pricing thuần C#: BR1–BR14, không phụ thuộc HTTP/EF |
| 20 | `...\src\SaigonBite.Domain\OrderLifecycle.cs` | Guard cho 8 trạng thái / 8 transition |
| 21 | `...\tests\SaigonBite.Domain.Checks\Program.cs` | 53 check thực thi được (không phải test framework chính thức) |

### Bộ dựng và kiểm tra (nếu cần sửa báo cáo/slide)

| # | File | Việc |
|---|---|---|
| 22 | `...\scripts\build_part2_report.py` | Dựng DOCX/PDF báo cáo; hằng số `FIGMA_FILE`, `FIGMA_PROTO`, `GITHUB` ở ~dòng 29 |
| 23 | `...\scripts\build_part2_slides.py` | Dựng 12 slide + speaker notes |
| 24 | `...\scripts\check_part2_deliverables.py` | Chạy lại toàn bộ checklist nộp bài |
| 25 | `...\scripts\export-docx-pdf.ps1`, `export-pptx-pdf.ps1` | Xuất PDF qua Word/PowerPoint COM (bắt buộc đường dẫn tuyệt đối) |
| 26 | `...\scripts\render-diagrams.ps1` | Render `.puml` → `.svg`/`.png` |

### Bộ thiết kế V2 (local, bị gitignore)

`...\outputs\saigonbite-v2\` chứa: `SaigonBite-V2-Figma.zip` (plugin import), `delivery\SaigonBite V2 - Xem thu.html` (xem offline toàn bộ flow), `delivery\Figma import\{manifest.json, code.js}`, `previews\*.png` (12 ảnh màn hình), `qa.json`.

---

## 11. Quy ước bắt buộc khi viết tiếp

1. **Ngôn ngữ**: báo cáo và tài liệu kỹ thuật viết **tiếng Anh**; ghi chú/nhật ký nội bộ viết tiếng Việt. Không trộn trong cùng một đoạn.
2. **Mọi use case, sơ đồ, model phải ghi MSSV tác giả** — đây là yêu cầu chấm điểm, không phải trang trí.
3. **Mọi đặc tả UC phải tham chiếu FR/BR id**; bảng/ma trận phải dẫn NFR id.
4. **Không nói quá**: chưa implement thì viết "design planned", "not implemented yet". Không được tạo số liệu test giả. Thanh toán là **simulated (Tier 3)**.
5. **Thanh toán phải thể hiện bằng Strategy pattern** trong class diagram và ERD, không chỉ là một bảng `Payments`.
6. **Pricing phải nói rõ nằm ở đâu** (Application/Domain service) và **map principle → NFR** trong phần kiến trúc.
7. **Không sửa/xoá file trong `outputs\instructor-source\`** — đó là đề gốc.
8. Trước khi động vào báo cáo/slide, chạy `check_part2_deliverables.py` để biết baseline hiện tại.

---

## 12. Lệnh hay dùng

```powershell
# đứng tại gốc dự án
$env:PYTHONUTF8='1'

python scripts\build_part2_report.py --pdf     # dựng lại PDF (18 trang, 26 bookmark)
python scripts\build_part2_slides.py           # dựng lại PPTX (12 slide + notes)
python scripts\check_part2_deliverables.py     # kiểm tra toàn bộ điều kiện nộp

# xuất PDF từ Word/PowerPoint (cần đường dẫn tuyệt đối)
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-docx-pdf.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-pptx-pdf.ps1

# kiểm tra logic domain (53 check)
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-domain.ps1

git log --oneline -5
```

Môi trường đã xác nhận: Python có `pymupdf`, `pypdf`, `python-docx`, `python-pptx`, `PIL` (**không** có `pywin32`) → xuất PDF phải qua script PowerShell COM. Có Word 16 và PowerPoint; SQL Server Express đang chạy; Visual Studio ở `D:\Visual Studio`.

---

## 13. Nếu AI kế tiếp cần trả lời câu hỏi gì, đọc file nào

| Câu hỏi | Đọc |
|---|---|
| "Đề bài yêu cầu gì?" | `outputs\instructor-source\SE_FinalProject_Requirements_*.pdf` |
| "FR/BR/NFR này nghĩa là gì?" | `docs\requirements-baseline.md` + `docs\part2\uc01\01-dac-ta-uc01.md` |
| "Luồng đặt đơn chạy thế nào?" | `docs\part2\use-cases.md` §2.2.1 + `docs\part2\diagrams\uc01-create-activity.puml` |
| "Giá tính ra sao?" | `docs\part2\uc01\01-dac-ta-uc01.md` §3 + `src\SaigonBite.Domain\Pricing.cs` |
| "Class nào chịu trách nhiệm gì?" | `docs\part2\crc-cards.md` |
| "Vì sao chọn kiến trúc này?" | `docs\part2\architecture.md` (ADR-001, money/time, snapshot, concurrency) |
| "Màn hình nào đã thiết kế?" | `docs\part2\ui-specification.md` + Figma prototype (link ở mục 8) |
| "Đã làm tới đâu?" | `docs\project-status.md` + `docs\part2\uc01\04-nhat-ky.md` + `docs\part2\traceability.csv` |
| "Báo cáo nộp có gì?" | `output\part2\Team3_Part2_Design.pdf` |
