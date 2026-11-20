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

The initial delivery includes a design baseline plus pure C# pricing and lifecycle guards. Run `scripts/check-domain.ps1` with the installed compiler; the first run passed 53 executable checks. The full MVC application, EF migration and final unit/integration test report are not yet implemented. See `docs/project-status.md` before the next task.

## Version control

Keep changes small and meaningful, link FR/BR IDs in commits where useful, review before merging, and push actual completed work. Do not fabricate dates or student contributions. Each student must make and understand their own meaningful commits; the specification requires **more than 10 per student** for Part 3. Any generative tool usage is declared in Appendix B of the report.

Repository: https://github.com/rumnopro0809/saigonbite-group3. Temporary build folders (`tmp`), local previews and unrelated workbooks are excluded via `.gitignore`.
