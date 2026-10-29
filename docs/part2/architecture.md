# Architecture and Database Decisions

Design owners: 524H0072 orders/payments, 524H0189 catalog/accounts, 524H0188 hubs. Team review pending; AI-assisted design.

## Runtime and layers

ADR-001: ASP.NET MVC 5 / .NET Framework 4.8, EF6 Code First, SQL Server and Bootstrap. Business rules are independent of System.Web and EF. No Database First or SPA substitution.

| Planned project | Responsibility | References |
|---|---|---|
| SaigonBite.Domain | Entities, value objects, lifecycle enums and contracts | None |
| SaigonBite.Application | Order/menu/hub/voucher/state/report services, pricing and payment selection | Domain |
| SaigonBite.Infrastructure | EF6 mappings, migrations, persistence and simulated adapters | Domain/Application |
| SaigonBite.Web | MVC controllers, ViewModels, Razor views, Bootstrap, EN/VI resources and composition | Application/Infrastructure |
| SaigonBite.Tests | Unit and integration checks including EP/BVA and authorization | Relevant tested projects |

These are planned components, not implementation evidence. Avoid adding an unnecessary repository wrapper around every EF entity; use clear transaction/query boundaries.

## Relational schema

| Entity | Important fields/constraints | Deletion/history |
|---|---|---|
| User | PK Id, unique Identifier, PasswordHash, Role, flags, RowVersion | Soft-delete; retain order/audit references |
| LocalProfile | PK/FK UserId, phone under BR18 | One optional profile; role/type consistency |
| TouristProfile | PK/FK UserId, Passport, Nationality, Email | Same schema as Local |
| StaffProfile | PK/FK UserId, HubId for Kitchen Staff | Own-hub scope enforced |
| Hub | PK Id, location, Capacity > 0, active/deleted flags, RowVersion | Soft-delete; load derived |
| Brand | PK Id, category, flags | Soft-delete |
| HubBrand | Composite PK HubId/BrandId | Membership for new orders |
| MenuItem | PK Id, BrandId, name/category, UnitPrice, Available, flags | Snapshot historical values in lines |
| Voucher | PK Id, unique Code, Type/Value/Cap/MinSubtotal, dates, flags | Soft-delete; applied values snapshotted |
| SystemSetting | Global heavy-rain flag, version | Affects new placement snapshots only |
| Order | PK Id, CustomerId/HubId, nullable VoucherId/ShipperId, Status, address/distance, context/breakdown snapshot, RowVersion, RequestKey | Cancel through state transition; retain records |
| OrderLine | OrderId/MenuItemId, Quantity, unit-price/name/category snapshots | Preserve history |
| PaymentRecord | OrderId, method, operation, amount, result, timestamp, correlation key | Append charge/refund/adjustments |
| OrderStatusHistory | OrderId, From/To, ActorId, ChangedAt, reason/cash confirmation | Append successful transitions |

Customer generalization exists in the domain; Users plus customer-specific profiles persist in one schema. Server selects customer type from the authenticated account, not a posted field.

## Money and time

Use C# decimal / SQL decimal(19,4) for intermediate amounts. Persist raw total, rounded payable total and breakdown. Round half-up once to 1,000 under BR11; do not round components or use binary floating-point for money. Distance is decimal with enough precision to distinguish 8.0/8.01.

Store timestamps as DateTimeOffset / SQL datetimeoffset; interpret rush windows, report dates and UI in UTC+7. Proposed report range implementation: inclusive Vietnam-local start, exclusive start of the day after the selected end date.

## Snapshots and edits

Store placement time, load and rain flag (BR17), line unit prices and applied discount/fee breakdown. Retain voucher parameters and the actual applied amount to explain historical totals after edits to vouchers. Existing lines retain prices; new lines use their price at addition (AMB03). Recalculation uses original time/load/rain. Proposed voucher-expiry handling on edit needs instructor confirmation: original voucher evaluated at placement time, newly selected voucher at edit time. Do not silently treat that unresolved policy as an official rule.

## Transactions and concurrency

Save order, lines, mock payment result and history atomically (NFR05). A failed mock payment/storage action leaves no partial accepted order. This guarantee covers the Tier 3 simulated gateway; a real gateway would require separate reconciliation/idempotency/outbox handling.

Use rowversion on mutable records and recheck current state before edit/cancel/transition. Revalidate hub active status, item availability and bounds at authoritative placement. Serialize relevant hub acceptance/capacity decisions with a short database transaction or hub lock, then verify the technique under integration tests. BR8 counts Confirmed/Preparing only: an accepted Placed order does not count until Confirmed. Do not invent an extra confirmation rejection rule to force load below 90%.

Unique customer/request keys prevent repeated submissions from creating duplicate orders/payments. A changed quote is returned for explicit customer confirmation rather than silently charging another amount (design proposal).

## Historical integrity and reports

Disable cascade deletion for referenced hubs/menu/users/vouchers. Active queries filter flags; historical queries deliberately retain inactive references. Inactive hub handling follows the documented proposal: no new orders, existing work continues.

R1 follows AMB04: Delivered-only, successful-delivery date, net food revenue by snapshotted category, final totals by method, separate fees/rounding reconciliation. Proportional discount allocation needs an explicit residual/tie rule and fixture tests before implementation. R2 follows AMB06: handled means ever Confirmed, preparation mean uses complete Confirmed/Ready pairs, granted Q count includes later cancellations with clear labels, current load always follows BR8.

## Migrations

Track model changes and EF6 migrations together. Verify empty-database creation and upgrade from previous schema with data. Never commit passwords/production connection strings or destructively reset a shared database. Sample seeds cover both customer types, all roles, multiple hubs/brands, vouchers and report/payment fixtures and are clearly marked as sample data.
