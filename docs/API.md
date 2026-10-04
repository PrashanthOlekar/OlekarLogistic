# ProCargo API reference

All endpoints live under **`/api/v1`** and speak JSON (file uploads use `multipart/form-data`).
Interactive docs with an **Authorize** button: run the API in Development and open `/swagger`.
The OpenAPI document is at `/openapi/v1.json` (Development only).

## Signing in

1. `POST /auth/otp` with `{ "mobile": "9845012345", "purpose": "Login" }` (or `"Signup"`). A 6-digit code is sent by SMS. In Development the reply also contains `devCode`.
2. `POST /auth/login` with `{ "mobile", "code" }`, or one of the `/registrations/*` endpoints for a new account. The reply is:

   ```json
   {
     "accessToken": "eyJ…",
     "expiresIn": 1800,
     "expiresAt": "2026-10-04T10:30:00Z",
     "refreshToken": "q3V…",
     "user": { "id": 12, "role": "Customer", "fullName": "…", "mobile": "…", "status": "Active", "detail": { … } }
   }
   ```

3. Send `Authorization: Bearer <accessToken>` on every other request.
4. Before the access token expires (30 minutes), call `POST /auth/refresh` with `{ "refreshToken" }`. You get a new pair; **the old refresh token stops working**. Using an old refresh token again is treated as theft and signs that user out everywhere.
5. `POST /auth/logout` with `{ "refreshToken" }` revokes it (204).

Refresh tokens last 14 days and are stored only as SHA-256 hashes. Blocking a user revokes all their refresh tokens, and every request from a blocked account is refused with 403 even while its access token is still valid.

## Roles

`Customer`, `Owner` (lorry owner), `Driver` and `Admin` (operations team). The API enforces every rule below; the portal's route guards only decide what to show.

## Errors

Every error is an RFC 7807 **ProblemDetails** response (`application/problem+json`) with a `traceId`:

```json
{
  "type": "https://procargo.in/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "errors": { "mobile": ["Enter a 10-digit Indian mobile number."] },
  "traceId": "0HNP1TMS5EVUB:00000001"
}
```

| Status | When |
| --- | --- |
| 400 | Validation failed (`errors` lists each field) or a business rule was broken (`detail` explains) |
| 401 | No token, or the token is invalid or expired |
| 403 | Signed in but not allowed (wrong role, someone else's record, or a blocked account) |
| 404 | The record doesn't exist |
| 409 | Conflict: duplicate mobile, licence or registration number; the record changed state (already paid, already assigned) |
| 429 | Too many requests: sign-in endpoints allow 10 a minute per IP, everything else 300 |
| 500 | Unexpected error; the `traceId` finds it in the logs. No internal details are returned |

## Lists: paging, search, filters and sort

Every list endpoint takes `pageNumber` (from 1), `pageSize` (1–100, default 20) and `search`, plus the filters shown below, and returns:

```json
{ "items": [ … ], "pageNumber": 1, "pageSize": 20, "totalRecords": 63, "totalPages": 4 }
```

Paging, filtering and sorting run in SQL Server stored procedures, never in memory.

## Endpoints

| Method and path | Who | Success | What it does |
| --- | --- | --- | --- |
| **Public** | | | |
| `GET /reference-data` | Anyone | 200 | Cities, vehicle types and goods categories |
| `POST /price-estimates` | Anyone | 200 | Price for `{ pickupCityId, dropCityId, vehicleTypeId, weightKg }`, with a bigger vehicle suggested if overloaded |
| `GET /health` (no `/api/v1`) | Anyone | 200 | API and database are up |
| **Sign-in and accounts** | | | |
| `POST /auth/otp` | Anyone | 200 | Sends a sign-in or sign-up code |
| `POST /auth/login` | Anyone | 200 | Code → tokens |
| `POST /auth/refresh` | Anyone with a refresh token | 200 | New token pair (rotation) |
| `POST /auth/logout` | Signed in | 204 | Revokes the refresh token |
| `GET /auth/me` | Signed in | 200 | Profile, with the role's details (KYC status, licence…) |
| `POST /registrations/customers` | Anyone | 201 | New customer; signs them in |
| `POST /registrations/owners` | Anyone | 201 | New lorry owner with PAN and bank account (stored encrypted) |
| `POST /registrations/drivers` | Anyone | 201 | New driver, optionally linked to an owner by mobile |
| **Bookings** | | | |
| `GET /bookings?status=&sort=Newest\|PickupDate` | Customer (own), Admin | 200 | Paged list |
| `GET /bookings/{id}` | Customer (own), Admin | 200 | Detail with quote, payment, trip, timeline and invoice. Pickup and delivery codes only for the customer |
| `POST /bookings` | Customer | 201 | Creates the booking and its first quote |
| `POST /bookings/{id}/quotes` | Customer (own) | 201 | Fresh quote after the old one expired |
| `POST /bookings/{id}/payments` | Customer (own) | 201 | Pays the quote (`{ method: "UPI" }`; test mode until a gateway is connected) |
| `POST /bookings/{id}/cancellation` | Customer (own) | 204 | Cancels before loading; refunds if paid |
| `GET /bookings/{id}/assignable-vehicles` | Admin | 200 | Verified, free trucks of the right type, each with its owner's free drivers |
| **Loads (owner)** | | | |
| `GET /loads` | Owner | 200 | Paid bookings that fit one of the owner's verified, available trucks |
| `DELETE /loads/{bookingId}` | Owner | 204 | "Not interested": hides that load |
| **Trips** | | | |
| `GET /trips?status=&sort=Newest\|ActiveFirst` | Owner (own), Driver (own), Admin | 200 | Paged list |
| `GET /trips/{id}` | Owner (own), Driver (own), Admin | 200 | Detail for the driver's screen |
| `POST /trips` | Owner (takes a load), Admin (assigns by hand) | 201 | `{ bookingId, vehicleId, driverId }` |
| `POST /trips/{id}/events` | Driver (own) | 204 | `{ action: EnRoute \| ReachedPickup \| StartTrip \| ReachedDestination, latitude?, longitude? }` |
| `POST /trips/{id}/handovers` | Driver (own) | 204 | `{ kind: Pickup \| Delivery, code }` – the customer's 4-digit codes |
| `POST /trips/{id}/photos` | Driver (own) | 201 | Multipart: `kind` (`PickupPhoto` or `POD`), `file`, `latitude?`, `longitude?` |
| `PUT /trips/{id}/pod-approval` | Admin | 204 | Approves the POD: completes the trip, issues the GST invoice, approves the owner's settlement |
| **Fleet** | | | |
| `GET /vehicles?vehicleTypeId=&availabilityStatus=&verificationStatus=` | Owner (own), Admin | 200 | Paged list with each vehicle's documents |
| `POST /vehicles` | Owner | 201 | Adds a vehicle |
| `PATCH /vehicles/{id}` | Owner (own) | 204 | `{ availabilityStatus?, currentDriverId?, removeDriver? }` |
| `PUT /vehicles/{id}/verification` | Admin | 204 | `{ approve, reason? }` (reason required to reject) |
| `GET /drivers?kycStatus=&dutyStatus=` | Owner (own), Admin | 200 | Paged list |
| `PUT /drivers/{id}/verification` | Admin | 204 | Approve or reject a driver's KYC |
| `PUT /owners/{id}/verification` | Admin | 204 | Approve or reject an owner's KYC |
| `GET /approvals` | Admin | 200 | Owners, drivers and vehicles waiting for verification |
| **Documents** | | | |
| `POST /documents` | Customer, Owner, Driver | 201 | Multipart: `entityType`, `entityId` (vehicles only), `docType`, `documentNumber?`, `expiryDate?`, `file` (PDF or image, 10 MB) |
| `GET /documents?status=` | Everyone (own); Admin (all) | 200 | Paged list |
| `GET /documents/{id}/file` | Uploader, Admin | 200 | The file itself |
| `PUT /documents/{id}/review` | Admin | 204 | Verify or reject with a reason |
| **Money** | | | |
| `GET /payments?status=` | Admin | 200 | Customer payments |
| `GET /settlements?status=` | Owner (own), Admin | 200 | Owner payouts per trip |
| `PUT /settlements/{id}/payout` | Admin | 204 | Records a payout already sent: `{ utr }` |
| **Dashboards and administration** | | | |
| `GET /dashboards/owner` | Owner | 200 | Earnings, pending payout, fleet counts, bank (last 4 digits only) |
| `GET /dashboards/admin` | Admin | 200 | Today's numbers, queues and the last 7 days |
| `GET /users?role=&status=` | Admin | 200 | Paged list |
| `PATCH /users/{id}` | Admin | 204 | `{ status: Active \| Blocked }`; blocking signs the user out everywhere |
| `GET /audit-logs` | Admin | 200 | Who did what, newest first |

## Routes in the previous API

| Before | Now |
| --- | --- |
| `GET /api/meta` | `GET /api/v1/reference-data` |
| `POST /api/quotes/estimate` | `POST /api/v1/price-estimates` |
| `POST /api/auth/otp/send` | `POST /api/v1/auth/otp` |
| `POST /api/auth/register/{kind}` | `POST /api/v1/registrations/{customers\|owners\|drivers}` |
| `POST /api/bookings/{id}/requote`, `/pay`, `/cancel` | `POST /api/v1/bookings/{id}/quotes`, `/payments`, `/cancellation` |
| `GET /api/owner/summary` | `GET /api/v1/dashboards/owner` |
| `GET /api/owner/loads`, `POST …/{id}/decline` | `GET /api/v1/loads`, `DELETE /api/v1/loads/{id}` |
| `POST /api/owner/loads/{id}/accept` | `POST /api/v1/trips` |
| `GET/POST /api/owner/vehicles`, `PATCH …/availability`, `…/driver` | `GET/POST /api/v1/vehicles`, `PATCH /api/v1/vehicles/{id}` |
| `GET /api/owner/drivers`, `/owner/trips`, `/owner/settlements` | `GET /api/v1/drivers`, `/trips`, `/settlements` |
| `GET /api/driver/trips[/{id}]` | `GET /api/v1/trips[/{id}]` |
| `POST /api/driver/trips/{id}/advance`, `/verify-otp`, `/photo` | `POST /api/v1/trips/{id}/events`, `/handovers`, `/photos` |
| `GET /api/documents/mine` | `GET /api/v1/documents` |
| `GET /api/admin/summary` | `GET /api/v1/dashboards/admin` |
| `POST /api/admin/approvals/{kind}/{id}` | `PUT /api/v1/{owners\|drivers\|vehicles}/{id}/verification` |
| `POST /api/admin/documents/{id}/review` | `PUT /api/v1/documents/{id}/review` |
| `GET /api/admin/bookings/{id}/assignable`, `POST …/assign` | `GET /api/v1/bookings/{id}/assignable-vehicles`, `POST /api/v1/trips` |
| `POST /api/admin/trips/{id}/approve-pod` | `PUT /api/v1/trips/{id}/pod-approval` |
| `POST /api/admin/settlements/{id}/release` | `PUT /api/v1/settlements/{id}/payout` |
| `POST /api/admin/users/{id}/block` | `PATCH /api/v1/users/{id}` |
| Errors as `{ "error": "…" }` | ProblemDetails |
