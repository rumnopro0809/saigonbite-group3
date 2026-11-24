# Part 2 & Part 3 Handoff — UC02: Quản lý Thực đơn và Thương hiệu

Người phụ trách: **Dương Phạm Thái — 524H0189 — Group 3 (`ThaiDuong06`)**.

Tài liệu rà soát và đặc tả chi tiết cho **UC02 (Manage Menu and Brands)** phục vụ hoàn thiện đối chiếu thiết kế Part 2 sau buổi thuyết trình Design Review (**23/11/2026**) và làm đầu vào triển khai **Part 3** trên khung solution `SaigonBite.sln` ở nhánh `main`.

> *Lưu ý minh bạch (Mục 7 — Declaration of Generative AI Use):* Một phần cấu trúc rà soát và biên tập tài liệu có sự hỗ trợ của công cụ AI và đã được đối chiếu trực tiếp với `docs/requirements-baseline.md`, `docs/part2/use-cases.md`, `docs/part2/crc-cards.md`, sơ đồ PlantUML, Figma V2 và mã nguồn EF6 trên `main`. Chi tiết khai báo cập nhật vào Appendix B.

## Danh mục tài liệu UC02

| File | Nội dung | Phạm vi FR / BR / NFR |
| :--- | :--- | :--- |
| [01-dac-ta-uc02.md](01-dac-ta-uc02.md) | Đặc tả chi tiết CRUD cho `Brand` và `MenuItem`, luồng đọc theo Hub, ngoại lệ `E1–E6`, tiền/hậu điều kiện | `FR08–FR11`, `FR13`, `FR18`, `BR12`, `BR17`, `AMB03`, `AMB04`, `NFR02–NFR04` |
| [02-traceability.csv](02-traceability.csv) | Bảng truy vết yêu cầu UC02 sang thiết kế, kiểm thử (`TC08–TC11`, `TC13`, `TC18`, `TN02–TN04`) | `FR08–FR11`, `FR13`, `FR18`, `NFR02–NFR04` |
| [03-design-and-scaffold-review.md](03-design-and-scaffold-review.md) | Rà soát đối chiếu `uc02-menu-activity.puml`, `CRC06`, EF6 `SaigonBiteDbContext`/`SaigonBiteSeeder` trên `main`, và kiểm chứng Figma V2 (`UC02` & `R1`) | `CRC06`, `CRC09`, `BR12`, `BR17`, `Section 2E` |
| [uc02-implementation-plan.md](../../part3/uc02-implementation-plan.md) | Kế hoạch triển khai Part 3 chi tiết theo từng lớp (`Application`, `Infrastructure`, `Web`, `Tests`) bám đúng `SaigonBite.sln` trên `main` | `APP-01`, `APP-06`, `APP-08`, `QA-01`, `QA-02` |

## Trạng thái tiến độ UC02 (24/11/2026)

- [x] Đặc tả chi tiết CRUD & bảng truy vết `FR08–FR11`, `BR12`, `BR17`
- [x] Rà soát Activity Diagram (`uc02-menu-activity.puml`), `CRC06 MenuService`, và đối chiếu thực thể EF6 trên `main`
- [x] Thuyết trình Part 2 Design Review (`Slide 8`, `Slide 12`) & demo Figma V2 (`UC02 Thực đơn`, `R1 Doanh thu`) ngày 23/11/2026
- [ ] Hoàn thiện `MenuService`, `EfMenuRepository`, `BrandsController`/`MenuController` (MVC 5 + Bootstrap song ngữ `vi`/`en`) trên nhánh `feature/uc02-menu`
- [ ] Viết và chạy MSTest cho `TC08–TC11`, `TC13`, `BR12` và bảo toàn snapshot `BR17`
- [ ] Phối hợp triển khai phần hỗ trợ (`HT01` Accounts/Auth & `HT02` Vouchers) sau khi chốt với nhóm
