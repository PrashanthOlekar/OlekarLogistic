<p align="center"><img src="brand/procargo-logo.svg" alt="ProCargo" height="64"></p>

<p align="center"><b>Moving India. Delivering Trust.</b></p>

A technology-enabled lorry booking marketplace that connects customers who need goods moved with verified lorry owners and drivers, starting in Karnataka.

![How ProCargo works](docs/business-flow.svg)

## What's in this repository

| Folder | What it is |
| --- | --- |
| [`website/`](website) | The animated **marketing website**: plain HTML, with CSS and JavaScript split into one file per section. Its "Book a truck" and "Login" buttons open the portal. |
| [`frontend/`](frontend) | The **ProCargo Portal**: React + TypeScript pages for customers, lorry owners, drivers and the operations team. Everything they do is saved to SQL Server through the API. |
| [`backend/ProCargo.Api/`](backend/ProCargo.Api) | The **ASP.NET Core 8 Web API** with Entity Framework Core on SQL Server: OTP sign-in, pricing, bookings, trips, documents, invoices and payouts. |
| [`database/ProCargo.sql`](database/ProCargo.sql) | Creates the `ProCargo` database: 30 tables in 8 modules, keys, indexes, two views and starting data. |
| [`brand/`](brand) | The ProCargo logo as SVG and PNG, for light and dark backgrounds. |
| [`docs/`](docs) | The business flow (the picture above) and the [technical blueprint](docs/TECHNICAL-BLUEPRINT.md). |

## How the code is organised

Every file holds one thing, so you can find code by its name:

- **Database:** `ProCargo.sql` has a table of contents at the top. Each table lists its columns with a comment, then its keys and checks.
- **API:** one C# class per file. `Endpoints/` has one file per area (bookings, owner, driver, admin…), and every URL points to a named method such as `CreateBookingAsync`. Status words like `InTransit` live in `Domain/Statuses.cs`.
- **Portal:** one React component per file. Pages are grouped by role in `src/pages/`. Shared pieces are in `src/components/`, and styles are in `src/styles/`, split by area. There are no inline styles.
- **Website:** `index.html` holds the markup only. Styles are in `css/` and scripts in `js/`, one file per page section. `js/config.js` holds the portal address.

The [technical blueprint](docs/TECHNICAL-BLUEPRINT.md#code-layout-as-built) has the full folder map.

## Run it on your computer

You need **SQL Server** (Express is free), the **.NET 8 SDK** and **Node.js 20+**. VS Code and SQL Server Management Studio are recommended.

### 1. Create the database

Open `database/ProCargo.sql` in SQL Server Management Studio and press **Execute**. It creates the `ProCargo` database with every table and the starting rate card.

### 2. Start the API

```bash
cd backend/ProCargo.Api

# Only if your SQL Server is not the default instance (for example SQL Server Express):
dotnet user-secrets set "ConnectionStrings:ProCargo" "Server=.\SQLEXPRESS;Database=ProCargo;Trusted_Connection=True;TrustServerCertificate=True"

dotnet run
```

The API starts on **http://localhost:5080**. Open http://localhost:5080/swagger to see and try every endpoint. On first start it creates an admin user with mobile **9999999999**. To use another number, change `Seed:AdminMobile` in `appsettings.json` before the first run.

### 3. Start the portal

```bash
cd frontend
npm install
npm run dev
```

Open **http://localhost:5173**. The portal forwards every `/api` call to the API. Run `npm run format` to tidy the code with Prettier after making changes.

### 4. Open the website

Open `website/index.html` in your browser, or serve the folder with any static web server. If the portal runs somewhere other than `http://localhost:5173`, change `PORTAL_URL` in `website/js/config.js`.

### 5. Try the whole business in 10 minutes

In development, the sign-in code is shown on screen (no SMS needed), and payments run in **test mode**.

1. **Owner:** Register → *I own lorries* → upload KYC documents → *Vehicles* → add a 14 FT truck and upload its RC.
2. **Driver:** Register → *I drive* → enter the owner's mobile number.
3. **Customer:** Register → *I need transport* → book Bengaluru → Hubballi → pay (test mode).
4. **Admin** (mobile 9999999999): *Approvals* → approve the owner, driver and vehicle.
5. **Owner:** *Loads & overview* → *Take this load* → pick the truck and driver.
6. **Customer:** open the booking to see the truck, the driver, and the pickup and delivery codes.
7. **Driver:** *My trips* → on the way → reached pickup → enter the pickup code → start → reached destination → upload POD → enter the delivery code.
8. **Admin:** *Trips & POD* → approve POD (the GST invoice is issued) → *Owner payouts* → record the bank UTR.

Every step is stored in SQL Server: `Bookings`, `Quotes`, `Payments`, `Trips`, `TripEvents`, `Documents`, `Invoices`, `Settlements` and `AuditLogs`.

## Before going live

| Item | Where |
| --- | --- |
| Set a strong JWT secret (32+ characters) | `Jwt:Key` through user-secrets or the `Jwt__Key` environment variable, never in Git |
| Send OTPs by SMS (MSG91 or Twilio India, DLT registered) | `Endpoints/AuthEndpoints.cs`, the `TODO before launch` line; then set `Otp:ShowCodeInDevelopment` to `false` |
| Connect a payment gateway (Razorpay or Cashfree) | `Endpoints/BookingEndpoints.cs` → `PayAsync`; set `Payments:Mode` |
| Move uploads to Azure Blob Storage | `Services/FileStorage.cs` |
| Keep encryption keys in Azure Key Vault | `Configuration/ServiceRegistration.cs` → Data Protection |
| Use real road distances (Google or Azure Maps) | `Services/PricingService.cs` → `EstimateRoadKm` |
| Confirm rates, the 7% commission, GST and TDS with your CA | The `VehicleTypes` and `Settings` tables |

## Notes

- Aadhaar numbers are never stored, only the last 4 digits. PAN and bank account numbers are encrypted.
- Blocking a user in *Admin → Users* signs them out immediately.
- Never commit passwords, API keys or real customer data to this repository.
