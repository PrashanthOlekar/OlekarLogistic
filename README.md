# Olekar Logistics

**Moving India. Delivering Trust.**

A technology-enabled lorry booking marketplace connecting customers who need goods moved with verified lorry owners and drivers, starting in Karnataka.

## What's in this repository

| Path | What it is |
| --- | --- |
| [`index.html`](index.html) | Clickable website prototype: animated homepage plus customer, owner, driver and admin dashboards (sample data) |
| [`database/OlekarLogistics_schema.sql`](database/OlekarLogistics_schema.sql) | SQL Server script that creates the `OlekarLogistics` database: 30 tables, keys, indexes and starting rate card |
| [`docs/TECHNICAL-BLUEPRINT.md`](docs/TECHNICAL-BLUEPRINT.md) | Architecture, database design, backend API, frontend structure, security and build roadmap |

## Try the prototype

Open `index.html` in a browser. Use **Login** in the top bar to switch between the customer, owner, driver and admin views, and **EN / ಕನ್ನಡ** to switch language.

To put it online for free: **Settings → Pages → Deploy from a branch → `main` / root → Save**. It will appear at `https://prashantholekar.github.io/OlekarLogistic/`.

## Create the database

1. Install SQL Server (Express is free) and SQL Server Management Studio or Azure Data Studio.
2. Open `database/OlekarLogistics_schema.sql` and run it.
3. It creates the database, all tables, two helper views and starting data for vehicle types, goods categories, cities and settings.

## Planned technology

- **Backend:** ASP.NET Core 8 Web API, Entity Framework Core, SignalR, SQL Server / Azure SQL
- **Frontend:** Next.js public site, React + Vite + TypeScript dashboards, React Native driver app
- **Azure:** App Service, Blob Storage, Key Vault, Application Insights
- **Integrations:** payment gateway (Razorpay / Cashfree), SMS OTP and WhatsApp, Google / Azure Maps, KYC provider

Full details are in [`docs/TECHNICAL-BLUEPRINT.md`](docs/TECHNICAL-BLUEPRINT.md).

## Notes

- All figures, testimonials, rates and the 7% commission in the prototype are sample data. Replace them before launch.
- Kannada text should be reviewed by a native speaker.
- Never commit passwords, API keys or real customer data to this repository.
