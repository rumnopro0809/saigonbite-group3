# Project status

Updated: 30 October 2026, Asia/Ho_Chi_Minh.

## Confirmed

- Group 3; Triều 524H0072 / UC01; Thái 524H0189 / UC02; Tấn 524H0188 / UC03.
- Tier 3; Agile with weekly iterations. Formal Scrum roles/events have not been confirmed.
- User requests daily useful development and Git pushes, and authorizes creation of a GitHub repository.
- Private repository name proposed: `saigonbite-group3`. Do not create a public repository without asking.
- No instructor feedback for Part 1 has been supplied as of this date.
- Source requirements: three SaigonBite PDFs, official Word template, current Part 1 FR/NFR definitions. Instructor answers supersede draft assumptions when supplied.

## Created in this work session

- Part 2 English design baseline, three detailed CRUD use cases, CRC responsibilities, schema/architecture decisions and UI/Figma specification.
- Editable UML sources: overall use case, three activities, exact eight-state Order lifecycle, payment/class model, ERD and architecture.
- Initial RTM and implementation backlog. These are proposed designs, not implementation results or student sign-off.
- Pure C# pricing/payment guards and lifecycle guards compiled and ran 53 executable checks successfully. Eight UML diagrams rendered and were visually inspected. See verification-2026-10-30.md for exact limits.

## Dependencies

1. Private GitHub repository created through the authenticated Git Credential Manager/API: https://github.com/rumnopro0809/saigonbite-group3. Browser automation stopped because it could not determine the URL safely; do not retry browser control in this turn. Repo creation succeeded through a purpose-built API workflow with no credential output. Initial remote/commit/push follow the reviewed file set.
2. Visual Studio/MSBuild discovered at D:\Visual Studio; MVC targets present. Framework reference packs include 4.7.2; .NET 4.8.1 runtime is installed (release 533325). Framework 4.8 targeting remains to verify. Initial pure C# domain compiled successfully with the installed compiler and ran 53 executable checks, all passing. These checks are not the final Part 3 unit-test runner or integration evidence.
3. MSSQL$SQLEXPRESS is running; actual application connection and migrations remain unverified.
4. A native Figma prototype and its public link remain required. A UI specification or local diagram is not a substitute.
5. Exact final deadline remains unknown; user says less than one month remains. Do not rely on conflicting dates in the guide.
6. AMB01-AMB06 from Part 1 remain proposed; no instructor answers are recorded. Read the assumptions in the design document before implementing money or report policies.

## Next execution order

1. Initial repository setup uses the verified GitHub account rumnopro0809 with its GitHub no-reply identity. Commit only this project’s intended files and verify the remote push.
2. Verify the MVC 5 build/targeting prerequisites and SQL Server connection, then scaffold the application and EF Code First migration.
3. Build one vertical flow: hub/menu selection → valid priced order → simulated payment → persisted order.
4. Add edit/cancel, kitchen/delivery transitions, voucher/admin CRUD and both reports, continuously testing boundaries and permissions.
5. Complete Figma interactions, render UML, prepare Part 2 Word/PDF and 7–10 minute review, then incorporate instructor feedback.

Daily development automation was created for 20:00 Vietnam time in this chat. Push requires a working remote, authentication and author identity. Record actual completion and actual test results; never create empty commits to satisfy a schedule.
