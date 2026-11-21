# SaigonBite Group 3

Software Engineering final project, Sem01_2627. Cloud-kitchen food delivery for Local Customers and Foreign Tourists in Ho Chi Minh City.

| Main use case | Student | ID |
|---|---|---|
| UC01 Place and Manage Orders | Nguyễn Hải Triều | 524H0072 |
| UC02 Manage Menu and Brands | Dương Phạm Thái | 524H0189 |
| UC03 Manage Hubs and Capacity | Lê Quang Tấn | 524H0188 |

Tier 3: **ASP.NET MVC 5, Entity Framework 6 Code First, SQL Server, Bootstrap**. Target .NET Framework 4.8. Payments/refunds/OTP/email codes are simulated. Payment Strategy and all mandatory business rules still apply. This implementation choice follows the named MVC stack in the specification; no instructor approval or grading result is claimed.

## Part 2 submission (20/11/2026)

Design, architecture and UI/UX package. Everything below is the version submitted to eLearning and to the shared Drive folder.

| File | What it is |
|---|---|
| [Team3_Part2_Design.pdf](output/part2/Team3_Part2_Design.pdf) | Design report, sections 2.1–2.6, with PDF bookmarks |
| [Team3_Part2_Presentation.pptx](output/part2/Team3_Part2_Presentation.pptx) | Design Review deck (7–10 minutes) |
| [readme.txt](output/part2/readme.txt) | Figma links, GitHub link and use case owners |

- Figma prototype (public, anyone with the link can view): https://www.figma.com/proto/Ao6NzrJvoRxJevn1gpn9QP
- Figma design file (V2 screens, page `04 Prototype`): https://www.figma.com/design/Ao6NzrJvoRxJevn1gpn9QP
- Report is rebuilt from source with `scripts/build_part2_report.py --pdf`; `scripts/check_part2_deliverables.py` re-runs every submission check.
- Payments stay simulated (Tier 3); implementation and test evidence belong to Part 3 and are not claimed here.

## Start here

- [Part 2 design document](docs/part2/README.md)
- [Detailed use cases](docs/part2/use-cases.md)
- [Architecture and database decisions](docs/part2/architecture.md)
- [UI and Figma handoff](docs/part2/ui-specification.md)
- [Daily backlog](docs/backlog.md)
- [Current state and remaining dependencies](docs/project-status.md)
- [Design sources](docs/part2/diagrams)
- [Requirement traceability](docs/part2/traceability.csv)

## Development environment

Use Visual Studio with ASP.NET and web development, the .NET Framework 4.8 targeting pack, SQL Server Developer/Express or LocalDB, and Git. MSBuild/MVC targets were discovered at `D:\Visual Studio`, and SQL Server Express is running. The 4.8 targeting pack still needs verification; existing reference packs include 4.7.2. A .NET runtime alone does not establish whether MVC 5 build tools are available.

The repository holds the design baseline, pure C# pricing/lifecycle/hub-load guards (`scripts/check-domain.ps1`: 65 checks pass) and the UC03 slice of the MVC + Web API application with its EF6 migration (see Part 3 below). Other use cases, the final unit/integration test report and the remaining screens are not yet implemented. See `docs/project-status.md` before the next task.

## Part 3 — UC03 Quản lý bếp và công suất (giao diện + API)

Phần này do Lê Quang Tấn (UC03) làm, nằm trên nhánh `feature/uc03-ui`. Kiểu tổ chức: **MVC 5 kết hợp REST API (Web API 2)**, EF6 Code First, SQL Server.

### Cấu trúc thư mục

| Thư mục | Vai trò |
|---|---|
| `src/SaigonBite.Domain` | Entity (`Hub`, `HubBrand`, `SystemSetting`...) và luật thuần (`HubLoad`, `Pricing`, `OrderLifecycle`) |
| `src/SaigonBite.Application` | Service nghiệp vụ UC03: `HubService`, `HubLoadService`, `RainSettingService`, `HubReportService` |
| `src/SaigonBite.Infrastructure` | `AppDbContext`, **migrations** và **dữ liệu mẫu (seed)** |
| `src/SaigonBite.Web` | Controller MVC trả view (`Controllers/`), REST API trả JSON (`Controllers/Api/`), view Razor, CSS/JS |

Luồng: trang HTML do `HubsController` / `ReportsController` trả về; các thao tác bật/tắt cờ mưa, ngừng nhận đơn, xóa bếp trên trang gọi REST API; cả hai cùng dùng chung service nên luật nghiệp vụ chỉ viết một chỗ.

### REST API

| Method | Địa chỉ | Việc làm | Mã trả về |
|---|---|---|---|
| GET | `/api/hubs` | Danh sách bếp + tải hiện tại | 200 |
| GET | `/api/hubs/{id}` | Chi tiết một bếp | 200, 404 |
| POST | `/api/hubs` | Tạo bếp | 201 (+ `Location`), 400 |
| PUT | `/api/hubs/{id}` | Sửa bếp (cần `rowVersion`) | 200, 400, 404, 409 |
| PATCH | `/api/hubs/{id}` | Bật/ngừng nhận đơn mới | 200, 400, 404, 409 |
| DELETE | `/api/hubs/{id}?rowVersion=...` | Xóa mềm | 204, 400, 404, 409 |
| GET / PUT | `/api/rain-flag` | Xem / đổi cờ mưa | 200, 409 |
| GET | `/api/reports/hub-utilization?from=&to=` | Báo cáo R2 | 200, 400 |

Mã 409 nghĩa là có người khác vừa sửa dữ liệu (rowversion không khớp): tải lại rồi thử lại.

### Cơ sở dữ liệu: clone về là có đúng schema và dữ liệu mẫu

Schema và dữ liệu mẫu nằm trong **code** (migration + seed), không gửi file `.mdf`/`.bak`.

Thành viên khác sau khi `git pull` chỉ cần:

1. Mở `SaigonBite.sln` bằng Visual Studio, để NuGet khôi phục gói.
2. Đặt `SaigonBite.Web` là Startup Project.
3. Mở *Package Manager Console*, chọn Default project là `SaigonBite.Infrastructure`, chạy:

   ```powershell
   Update-Database
   ```

   Lệnh này tạo database `SaigonBite`, chạy mọi migration và seed dữ liệu mẫu. (Chỉ cần chạy ứng dụng cũng được: `Global.asax` tự áp dụng migration lúc khởi động.)

Chuỗi kết nối nằm trong `src/SaigonBite.Web/Web.config`, mặc định dùng SQL Server Express (`Server=.\SQLEXPRESS`) và đăng nhập Windows (không có mật khẩu). Nếu máy bạn dùng LocalDB thì đổi thành `Server=(localdb)\MSSQLLocalDB`.

**Khi bạn sửa model** (thêm cột, thêm bảng):

```powershell
Add-Migration TenThayDoi      # tạo file migration trong src/SaigonBite.Infrastructure/Migrations
Update-Database               # áp dụng vào database của bạn
```

Rồi **commit cả file migration** (`.cs`, `.Designer.cs`, `.resx`). Thành viên khác pull về và chạy `Update-Database` là có thay đổi đó. Đừng sửa file migration đã được commit, hãy tạo migration mới. Không dùng migration tự động (đã tắt). Seed chỉ thêm dữ liệu còn thiếu nên chạy lại nhiều lần vẫn an toàn.

Muốn làm lại database sạch: `Update-Database -TargetMigration:0` rồi `Update-Database` (hoặc xóa database `SaigonBite` trong SQL Server Object Explorer).

### Build và chạy bằng dòng lệnh (đã chạy thử)

```powershell
dotnet build SaigonBite.sln          # cần .NET SDK 8 (tự lấy gói tham chiếu .NET Framework 4.8); không cần Visual Studio
scripts/check-domain.ps1             # 65 check domain (53 cũ + 12 check HubLoad), tất cả PASS
```

Cả 4 project đều là project kiểu SDK (`net48`, `PackageReference`). Riêng `SaigonBite.Web` xuất DLL ra `bin\` để IIS Express / Visual Studio chạy được. Migration `InitialUc03` đã tạo sẵn trong `src/SaigonBite.Infrastructure/Migrations` (có thêm ràng buộc `CK_Hubs_Capacity_Positive`: `Capacity > 0`).

**Kết quả chạy thử trên SQL Server Express:** từ database trống, `Update-Database` (hoặc chạy web lần đầu) tạo 3 bếp (Bếp Quận 1 tải 20%, Bếp Bình Thạnh 90%, Bếp Thủ Đức 20%), 5 brand, 1 dòng `SystemSettings`; chạy lần 2 không nhân đôi dữ liệu. API đã thử các mã 200/201/204/400/404/409 (409 bằng `rowVersion` cũ). Các trang `/Hubs`, `/Hubs/Create`, `/Hubs/Details/1`, `/Hubs/Edit/1`, `/Reports/HubUtilization` và nút cờ mưa chạy đúng.

**Chưa kiểm tra:** chạy bằng Visual Studio / IIS Express thật (lần thử dùng một host ASP.NET tối giản), trình duyệt khác ngoài trình duyệt tích hợp của công cụ, và đăng nhập Admin (HT01 chưa có nên đang bật `Uc03:BypassAdminLogin`).
### Phần làm tạm (sẽ thay khi UC khác xong)

- `Brand` (UC02), `Order`, `OrderStatusHistory` (UC01/HT03) là bản **rút gọn** trong `src/SaigonBite.Domain/Entities`. Khi UC khác thêm bản đầy đủ, hãy giữ nguyên các cột UC03 đang dùng và tạo migration mới.
- Chưa có đăng nhập (HT01): `Web.config` có `Uc03:BypassAdminLogin = true` cho qua quyền Admin. Khi HT01 xong thì đặt `false`.
- Giao diện UC03 hiện chỉ có tiếng Việt, chưa có EN/VI (NFR03).
- Màn hình bám theo frame UC03 và R2 của Figma V2 (lấy từ ảnh trong `Team3_Part2_Design.pdf`).

### Quy tắc comment

Xem [docs/part3/quy-tac-comment.md](docs/part3/quy-tac-comment.md): comment tiếng Việt đơn giản, giải thích *vì sao*, ghi mã FR/BR.

## Version control

Keep changes small and meaningful, link FR/BR IDs in commits where useful, review before merging, and push actual completed work. Do not fabricate dates or student contributions. Each student must make and understand their own meaningful commits; the specification requires **more than 10 per student** for Part 3. Any generative tool usage is declared in Appendix B of the report.

Repository: https://github.com/rumnopro0809/saigonbite-group3. Temporary build folders (`tmp`), local previews and unrelated workbooks are excluded via `.gitignore`.
