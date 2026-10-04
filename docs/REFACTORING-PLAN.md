# ProCargo refactoring plan

This document records what the ProCargo code looked like before the refactor, what was wrong with it, and how it was moved to a Clean Architecture backend (.NET 10, Dapper, stored procedures) and a separate React portal. It was written before any code was changed, then kept up to date as each module moved.

## 1. Current architecture (before)

```
frontend/  (React 19 + TypeScript + Vite)
    │  fetch('/api/...')  through the Vite dev proxy
    ▼
backend/ProCargo.Api  (one ASP.NET Core 8 project)
    Endpoints/*.cs     Minimal API handlers: HTTP, validation, business rules AND database queries
    Services/*.cs      Pricing, trips, invoices, OTP, tokens, files, encryption
    Data/              Entity Framework Core DbContext + 21 entity classes
    │
    ▼
SQL Server  (database/ProCargo.sql: 30 tables, 2 views, sequences, no stored procedures)
```

### What exists

| Area | Found |
| --- | --- |
| Projects | One API project `ProCargo.Api` (net8.0), one React app `frontend/`, the static `website/`. No tests. |
| Endpoints | 11 static classes, about 45 routes under `/api`: meta, auth, bookings, owner, driver, documents, admin (dashboard, approvals, bookings/trips, money, users). |
| Services | `PricingService`, `TripService`, `InvoiceService`, `OtpService`, `SettingsService`, `TokenService`, `FileStorage`, `PersonalDataProtector`, `AuditLogger`. |
| Data access | EF Core `ProCargoDbContext`, queried directly inside endpoint handlers. |
| Stored procedures | None. |
| Database | 30 tables, 4 sequences (booking, trip, invoice and ticket numbers), views `vw_ActiveTrips` and `vw_ExpiringDocuments`, seed data for vehicle types, goods, settings and cities. |
| Authentication | Mobile number + 6-digit one-time code (OTP, stored hashed). JWT valid for 12 hours. No passwords. `RefreshTokens` table exists but is unused. |
| Authorization | Roles `Customer`, `Owner`, `Driver`, `Admin`, one policy per role. `ActiveAccountMiddleware` blocks suspended accounts on every request. |
| Errors | `ApiException` + middleware returning `{ "error": "message" }`. |
| Validation | `Guard.Require(...)` calls scattered through the handlers. |
| Configuration | `appsettings.json` (connection string, JWT, seed admin, payments mode, OTP, storage, CORS). |
| React API calls | One `fetch` wrapper `lib/api.ts`; URLs written inline in about 45 places in pages. |
| React routing | A hand-written router (`lib/router.tsx`) and a role check in `App.tsx`. |
| React auth | `AuthContext` + token and user in `localStorage`; a 401 signs the user out. |

### Business functionality that must keep working

1. Sign up as customer, lorry owner (PAN, Aadhaar last 4, bank account) or driver (licence, optional owner link), with an OTP.
2. Sign in with an OTP. Blocked accounts are stopped immediately.
3. Instant price estimate; booking with an automatic quote; re-quote after expiry; test-mode payment; cancellation with refund before loading.
4. Owners: vehicles (add, availability, regular driver), drivers, available loads, take or decline a load, trips, payouts, KYC documents.
5. Drivers: trip list, step-by-step trip (on the way → reached pickup → pickup code → start → reached destination → POD photo → delivery code).
6. Admin: dashboard and charts, approvals (owner, driver, vehicle), document review, bookings, manual truck assignment, trips, POD approval (completes the trip, issues the GST invoice, approves the payout), payments, payouts with UTR, users (block/unblock), audit trail.
7. Encryption of PAN, bank account numbers and trip handover codes; Aadhaar never stored beyond the last 4 digits.

## 2. Problems found

| # | Problem | Why it matters |
| --- | --- | --- |
| 1 | Business rules, validation and SQL (LINQ) live inside endpoint handlers, some over 400 lines. | Hard to test, hard to reuse, every change touches HTTP code. |
| 2 | Entity Framework for every read and write; no stored procedures. | Does not meet the "stored procedures only" rule; query plans and permissions can't be managed by the DBA. |
| 3 | Multi-step writes (assign truck, deliver, approve POD) are several `SaveChangesAsync` calls with no explicit transaction in places (owner registration saves twice). | A failure half-way leaves inconsistent data. |
| 4 | Lists use `Take(200)` with no paging, search or sorting in SQL. | Pages will get slow and silently cut off records. |
| 5 | Routes are verb-style (`/pay`, `/cancel`, `/approve-pod`, `/block`) and not versioned. | Breaks REST conventions; no safe way to change the API later. |
| 6 | Errors are `{ error }`, not ProblemDetails; 200 is returned for creates. | Clients can't read validation errors per field; wrong status codes. |
| 7 | 12-hour access tokens, no refresh or logout. | A stolen token stays valid all day and can't be revoked. |
| 8 | No rate limiting, no security headers; Swagger uses the older Swashbuckle generator. | Weak protection against OTP brute force and common browser attacks. |
| 9 | No tests. | Every change is a manual retest of four roles. |
| 10 | React: hand-written router, `fetch` wrapper, URLs repeated in pages, `VITE_API_URL` prefix rather than a full base URL. | Spec requires React Router, Axios, feature API modules and `VITE_API_BASE_URL`. |

## 3. What stays and what changes

**Stays (reused as-is or moved):** the database tables, sequences, views and seed data; the four roles; OTP sign-in; pricing formula; trip step rules; GST invoice rules; encryption with ASP.NET Data Protection; file storage on disk (Azure Blob later); the React pages, components, styles and look.

**Changes:**

| From | To |
| --- | --- |
| One `ProCargo.Api` project | `ProCargo.Domain`, `ProCargo.Application`, `ProCargo.Infrastructure`, `ProCargo.API` + two test projects, all `net10.0` |
| EF Core LINQ in handlers | Repositories → Dapper → stored procedures (`CommandType.StoredProcedure`) |
| Minimal API static classes | Thin controllers calling application services |
| `Guard.Require` | FluentValidation validators + domain rule checks in services |
| `{ error }` | RFC 9457 ProblemDetails, with `errors` per field for 400 |
| 12 h JWT only | 30 min access token + rotating refresh token (`RefreshTokens` table), logout revokes |
| `/api/...` verb routes | `/api/v1/...` resource routes (table in section 6) |
| Unpaged lists | `pageNumber`, `pageSize`, `search`, filters and `sort` handled by the procedures |
| Custom router + `fetch` | React Router + one Axios instance with interceptors + one API module per feature |

## 4. Target folder structure

```
backend/
  ProCargo.sln
  Directory.Build.props, Directory.Packages.props, global.json
  src/
    ProCargo.Domain/            entities, constants, pricing / trip / invoice rules (no dependencies)
    ProCargo.Application/       services, DTOs, validators, repository + security interfaces, exceptions
    ProCargo.Infrastructure/    Dapper repositories, SQL connection factory, JWT, data protection, file storage, SMS
    ProCargo.API/               controllers, auth, Swagger, CORS, rate limiting, ProblemDetails, middleware
  tests/
    ProCargo.UnitTests/
    ProCargo.IntegrationTests/
database/
  ProCargo.sql                  tables, views, seed data (unchanged)
  procedures/*.sql              stored procedures, one file per module
frontend/
  src/ api/ auth/ components/ features/ hooks/ layouts/ pages/ routes/ services/ styles/ types/ utils/
```

## 5. Dependency rules

```
ProCargo.API ──► ProCargo.Application ──► ProCargo.Domain
     │                    ▲
     └──► ProCargo.Infrastructure (registration only)
```

- **Domain** references nothing.
- **Application** references Domain only. It defines the interfaces (`IBookingRepository`, `ITokenService`, `IFileStorage`…).
- **Infrastructure** implements those interfaces with Dapper, SQL Server, JWT and the file system.
- **API** calls Application services; it references Infrastructure only to register services in `Program.cs`.

## 6. API changes

All routes move under `/api/v1`. Lists accept `pageNumber`, `pageSize` (max 100), `search`, `sort` and filters, and return `{ items, pageNumber, pageSize, totalRecords, totalPages }`.

| Old | New | Who |
| --- | --- | --- |
| `GET /api/meta` | `GET /api/v1/reference-data` | anyone |
| `POST /api/quotes/estimate` | `POST /api/v1/price-estimates` | anyone |
| `POST /api/auth/otp/send` | `POST /api/v1/auth/otp` | anyone |
| `POST /api/auth/login` | `POST /api/v1/auth/login` | anyone |
| — | `POST /api/v1/auth/refresh`, `POST /api/v1/auth/logout` | anyone / signed in |
| `GET /api/auth/me` | `GET /api/v1/auth/me` | signed in |
| `POST /api/auth/register/{kind}` | `POST /api/v1/registrations/{customers\|owners\|drivers}` → 201 | anyone |
| `GET /api/bookings`, `GET /api/admin/bookings` | `GET /api/v1/bookings` (customers see their own) | Customer, Admin |
| `POST /api/bookings` | `POST /api/v1/bookings` → 201 | Customer |
| `GET /api/bookings/{id}` | `GET /api/v1/bookings/{id}` | Customer (own), Admin |
| `POST /api/bookings/{id}/requote` | `POST /api/v1/bookings/{id}/quotes` → 201 | Customer |
| `POST /api/bookings/{id}/pay` | `POST /api/v1/bookings/{id}/payments` → 201 | Customer |
| `POST /api/bookings/{id}/cancel` | `POST /api/v1/bookings/{id}/cancellation` → 204 | Customer |
| `GET /api/admin/bookings/{id}/assignable` | `GET /api/v1/bookings/{id}/assignable-vehicles` | Admin |
| `GET /api/owner/loads` | `GET /api/v1/loads` | Owner |
| `POST /api/owner/loads/{id}/decline` | `DELETE /api/v1/loads/{bookingId}` (hide it) → 204 | Owner |
| `POST /api/owner/loads/{id}/accept`, `POST /api/admin/bookings/{id}/assign` | `POST /api/v1/trips` `{ bookingId, vehicleId, driverId }` → 201 | Owner (own trucks), Admin |
| `GET /api/owner/trips`, `/api/driver/trips`, `/api/admin/trips` | `GET /api/v1/trips` (scoped by role) | Owner, Driver, Admin |
| `GET /api/driver/trips/{id}` | `GET /api/v1/trips/{id}` | Driver (own), Owner (own), Admin |
| `POST /api/driver/trips/{id}/advance` | `POST /api/v1/trips/{id}/events` → 204 | Driver |
| `POST /api/driver/trips/{id}/verify-otp` | `POST /api/v1/trips/{id}/handovers` → 204 | Driver |
| `POST /api/driver/trips/{id}/photo` | `POST /api/v1/trips/{id}/photos` → 201 | Driver |
| `POST /api/admin/trips/{id}/approve-pod` | `PUT /api/v1/trips/{id}/pod-approval` → 204 | Admin |
| `GET /api/owner/vehicles` | `GET /api/v1/vehicles` (owners see their own) | Owner, Admin |
| `POST /api/owner/vehicles` | `POST /api/v1/vehicles` → 201 | Owner |
| `PATCH /api/owner/vehicles/{id}/availability`, `/driver` | `PATCH /api/v1/vehicles/{id}` | Owner |
| `GET /api/owner/drivers` | `GET /api/v1/drivers` | Owner, Admin |
| `GET /api/admin/approvals` | `GET /api/v1/approvals` | Admin |
| `POST /api/admin/approvals/{kind}/{id}` | `PUT /api/v1/{owners\|drivers\|vehicles}/{id}/verification` → 204 | Admin |
| `GET /api/documents/mine`, `GET /api/admin/documents` | `GET /api/v1/documents` (others see their own) | signed in |
| `POST /api/documents` | `POST /api/v1/documents` → 201 | Owner, Driver, Customer |
| `GET /api/documents/{id}/file` | `GET /api/v1/documents/{id}/file` | uploader, Admin |
| `POST /api/admin/documents/{id}/review` | `PUT /api/v1/documents/{id}/review` → 204 | Admin |
| `GET /api/owner/summary` | `GET /api/v1/dashboards/owner` | Owner |
| `GET /api/admin/summary` | `GET /api/v1/dashboards/admin` | Admin |
| `GET /api/admin/payments` | `GET /api/v1/payments` | Admin |
| `GET /api/owner/settlements`, `/api/admin/settlements` | `GET /api/v1/settlements` (owners see their own) | Owner, Admin |
| `POST /api/admin/settlements/{id}/release` | `PUT /api/v1/settlements/{id}/payout` → 204 | Admin |
| `GET /api/admin/users` | `GET /api/v1/users` | Admin |
| `POST /api/admin/users/{id}/block` | `PATCH /api/v1/users/{id}` `{ status }` → 204 | Admin |
| `GET /api/admin/audit` | `GET /api/v1/audit-logs` | Admin |
| `GET /api/health` | `GET /health` | anyone |

## 7. Database access changes

- Every read and write is a stored procedure named `usp_<Entity>_<Action>`, for example `usp_Booking_Create`, `usp_Booking_GetPaged`, `usp_Trip_Assign`.
- Procedures that change several tables (registration, payment, cancellation, assignment, delivery, POD approval) run in one transaction with `UPDLOCK` on the rows they check, so two owners can't take the same load.
- Procedures raise business conflicts with `THROW 50409` (→ 409) and rule failures with `THROW 50400` (→ 400). Unique-index violations (2601, 2627) also become 409. Nothing else from SQL reaches the client.
- Paged procedures return two result sets: the page of rows and the total count, read with Dapper's `QueryMultipleAsync`.
- The tables are unchanged, so an existing `ProCargo` database keeps its data. Only the procedures are added.

## 8. React changes

- `src/api/apiClient.ts`: one Axios instance with `baseURL = VITE_API_BASE_URL`, a request interceptor adding `Authorization: Bearer`, and a response interceptor that refreshes an expired token once and otherwise signs the user out on 401.
- One API module per feature: `authApi`, `referenceDataApi`, `bookingApi`, `loadApi`, `tripApi`, `vehicleApi`, `driverApi`, `documentApi`, `dashboardApi`, `approvalApi`, `paymentApi`, `settlementApi`, `userApi`.
- React Router routes with `ProtectedRoute` (signed in) and `RoleProtectedRoute` (right role).
- `auth/` holds `AuthContext`, `AuthProvider`, `useAuth`, the route guards.
- `features/` holds feature pieces that aren't pages (dialogs, cards, trip steps); `pages/` stays grouped by role.
- Lists use the paged responses with a shared `Pagination` component; errors read ProblemDetails.
- Screens, styles and wording stay the same.

## 9. Authentication changes

- Sign-in stays mobile + OTP (customers, owners and drivers don't use passwords, and the existing app is built around this). Codes stay hashed; a code locks after 5 wrong tries; 5 codes per 10 minutes per number; the endpoint is also rate-limited per IP.
- Access tokens last 30 minutes (configurable). A refresh token (random 64 bytes, stored only as a SHA-256 hash in `RefreshTokens`) lasts 14 days and is replaced every time it is used. Re-using an old refresh token revokes all of that user's tokens.
- `POST /auth/logout` revokes the refresh token.
- The JWT signing key comes from user-secrets or the `Jwt__SigningKey` environment variable and must be at least 32 characters; the app refuses to start otherwise.

## 10. Migration sequence

Each step was committed separately on the `refactor/clean-architecture` branch, so `main` keeps working until the branch is merged.

1. This plan.
2. Stored procedures for every module, next to the unchanged tables.
3. Solution skeleton and Domain layer (entities, constants, pricing, trip and invoice rules moved from `Services/` and `Domain/`).
4. Application layer, module by module: reference data and pricing → auth and registration → bookings → fleet and loads → trips → documents → money → users, approvals and dashboards.
5. Infrastructure: Dapper repositories for the same modules, JWT, data protection, storage.
6. API: controllers for the same modules, then cross-cutting pieces (ProblemDetails, Swagger, CORS, rate limiting, headers).
7. Tests.
8. React: Axios client and auth first, then each role's pages moved to the new routes.
9. Docs and Docker.
10. Remove the old `backend/ProCargo.Api` project once the new solution builds and its tests pass.

## 11. Risks and how they were handled

| Risk | Handling |
| --- | --- |
| Behaviour drift while moving rules out of handlers | Every rule and message was moved verbatim; unit tests pin the important ones. |
| Race conditions now that EF `RowVersion` checks are gone | Procedures lock and re-check status inside the transaction. |
| Front-end breaking during the route change | The branch changes the API and the portal together; `main` stays on the old pair until merge. |
| Existing data | No table changes; procedures only. |
