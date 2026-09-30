# Olekar Logistics

**Moving India. Delivering Trust.**

A technology-enabled lorry booking marketplace connecting customers who need goods moved with verified lorry owners and drivers, starting in Karnataka.

![How Olekar Logistics works](docs/business-flow.svg)

## What's in this repository

| Path | What it is |
| --- | --- |
| [`frontend/`](frontend) | **Olekar Portal**: React + TypeScript pages for customers, lorry owners, drivers and the admin team. Every action saves to SQL Server through the API. |
| [`backend/Olekar.Api/`](backend/Olekar.Api) | **ASP.NET Core 8 Web API** with Entity Framework Core on SQL Server: OTP sign-in, pricing, bookings, trips, documents, payouts, admin. |
| [`database/OlekarLogistics_schema.sql`](database/OlekarLogistics_schema.sql) | Creates the `OlekarLogistics` database: 30 tables, keys, indexes and starting data. |
| [`docs/business-flow.svg`](docs/business-flow.svg) | The business, start to end (the picture above). |
| [`docs/TECHNICAL-BLUEPRINT.md`](docs/TECHNICAL-BLUEPRINT.md) | Architecture, database design, API list, security and roadmap. |
| [`index.html`](index.html) | The animated marketing website prototype (sample data, no database). |

## Run it on your computer

You need: **SQL Server** (Express is free), **.NET 8 SDK**, **Node.js 20+**.

### 1. Create the database

Open `database/OlekarLogistics_schema.sql` in SQL Server Management Studio (or Azure Data Studio) and run it. It creates the `OlekarLogistics` database with every table.

### 2. Start the API

```bash
cd backend/Olekar.Api
# If your SQL Server is not the default local instance, set the connection string first:
dotnet user-secrets set "ConnectionStrings:Olekar" "Server=.\SQLEXPRESS;Database=OlekarLogistics;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run
```

The API starts on **http://localhost:5080**. Open http://localhost:5080/swagger to see and try every endpoint. On first start it creates an admin user with mobile **9999999999** (change it in `appsettings.json` → `Seed:AdminMobile` before the first run).

### 3. Start the portal

```bash
cd frontend
npm install
npm run dev
```

Open **http://localhost:5173**. The portal forwards `/api` calls to the API.

### 4. Try the whole business in 10 minutes

In development, the sign-in code is shown on screen (no SMS needed), and payments run in **test mode**.

1. **Owner:** Register → *I own lorries* → upload KYC documents → *Vehicles* → add a 14 FT truck and upload its RC.
2. **Driver:** Register → *I drive* → enter the owner's mobile number.
3. **Customer:** Register → *I need transport* → book Bengaluru → Hubballi → pay (test mode).
4. **Admin** (mobile 9999999999): *Approvals* → approve the owner, driver and vehicle.
5. **Owner:** *Loads & overview* → *Take this load* → pick the truck and driver.
6. **Customer:** open the booking to see the truck, driver and the pickup / delivery codes.
7. **Driver:** *My trips* → on the way → reached pickup → enter the pickup code → start → reached destination → upload POD → enter the delivery code.
8. **Admin:** *Trips & POD* → approve POD (invoice issued) → *Owner payouts* → record the bank UTR.

Every step is stored in SQL Server: `Bookings`, `Quotes`, `Payments`, `Trips`, `TripEvents`, `Documents`, `Invoices`, `Settlements`, `AuditLogs`.

## Before going live

| Item | Where |
| --- | --- |
| Set a strong JWT secret (32+ characters) | `Jwt:Key` via user-secrets or the `Jwt__Key` environment variable, never in Git |
| Connect SMS for OTPs (MSG91 / Twilio India, DLT registered) | `AuthEndpoints.cs`, the `TODO production` line; set `Otp:ShowCodeInDevelopment` to `false` |
| Connect a payment gateway (Razorpay / Cashfree) | Replace the test payment in `CustomerEndpoints.cs` → `/pay`; set `Payments:Mode` |
| Move uploads to Azure Blob Storage | `FileStorage` in `Services/CoreServices.cs` |
| Store encryption keys in Azure Key Vault | Data Protection setup in `Program.cs` |
| Real road distances | `PricingService.RoadKm` (Google / Azure Maps) |
| Confirm rates, 7% commission, GST and TDS with your CA | `VehicleTypes` and `Settings` tables |

## Notes

- Aadhaar numbers are never stored, only the last 4 digits. PAN and bank account numbers are encrypted.
- Blocking a user in *Admin → Users* signs them out immediately.
- Never commit passwords, API keys or real customer data to this repository.
