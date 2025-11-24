# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Common commands

All commands assume the working directory is the repo root (`AirlinesReservationSystemARS`). The solution targets .NET 9 (`net9.0`).

### Build and restore

```bash path=null start=null
dotnet restore AirlinesReservationSystemARS.sln
dotnet build AirlinesReservationSystemARS.sln
```

### Run the web application

The main ASP.NET Core MVC app lives in `ARS/ARS.csproj`.

```bash path=null start=null
dotnet run --project ARS/ARS.csproj
```

For live-reload during development (if `dotnet-watch` is available):

```bash path=null start=null
dotnet watch run --project ARS/ARS.csproj
```

### Database and EF Core migrations

The app uses Pomelo EF Core for MySQL with the `DefaultConnection` connection string from `ARS/appsettings*.json`.

Typical migration workflow (requires the `dotnet-ef` global tool):

```bash path=null start=null
# Add a migration
dotnet ef migrations add <MigrationName> \
  --project ARS/ARS.csproj \
  --startup-project ARS/ARS.csproj

# Apply migrations to the configured database
dotnet ef database update \
  --project ARS/ARS.csproj \
  --startup-project ARS/ARS.csproj
```

> Note: the `ARS/Migrations` folder already contains schema and data-seed migrations, plus subfolders for Identity and seed migrations; keep new migrations consistent with this layout.

### Database maintenance / tooling

Several small CLI tools under `ARS/Tools` operate directly on the MySQL database using the same `DefaultConnection` from `appsettings.json`:

```bash path=null start=null
# Inspect seat and FlightSeat statistics / potential duplicates
dotnet run --project ARS/Tools/InspectSeats/InspectSeats.csproj

# Deduplicate Seat rows (merges duplicates and fixes related FlightSeats / Reservations)
dotnet run --project ARS/Tools/DedupeSeats/DedupeSeats.csproj

# Deduplicate FlightSeats (ensures one FlightSeat per (ScheduleId, SeatId))
dotnet run --project ARS/Tools/DedupeFlightSeats/DedupeFlightSeats.csproj

# Insert a specific EF migration record into __efmigrationshistory (used for manual recovery)
dotnet run --project ARS/Tools/MarkMigration/MarkMigration.csproj

# Quick DB sanity checks for FlightSeats / Reservations
dotnet run --project ARS/Tools/QueryDb/QueryDb.csproj

# Seed a large set of sample flights and schedules
dotnet run --project ARS/Tools/SeedFlights/SeedFlights.csproj
```

Most tools accept an optional path to `appsettings.json`; when omitted they resolve it relative to their project or by walking parent directories.

### Tests

There is currently no test project included in `AirlinesReservationSystemARS.sln`. Once test projects are added, standard .NET commands apply, for example:

```bash path=null start=null
# Run all tests (once test projects exist)
dotnet test AirlinesReservationSystemARS.sln

# Example: run a single test by name (in a test project)
dotnet test ARS.Tests/ARS.Tests.csproj --filter "Name~SomeTestMethod"
```

## High-level architecture

### Overview

This repository is a single ASP.NET Core MVC application (`ARS`) that manages airline flights, schedules, seat layouts, reservations, payments, and refunds on top of a MySQL database. It uses Entity Framework Core with the Pomelo MySQL provider, plus ASP.NET Core Identity with a custom `User` entity using integer primary keys.

Key layers:

- **Entry point & composition root**: `ARS/Program.cs`
- **Data access layer**: `ARS/Data/ApplicationDbContext.cs` and EF Core migrations under `ARS/Migrations`
- **Domain model**: entity classes in `ARS/Models`
- **Web layer**: MVC controllers in `ARS/Controllers` and Razor views in `ARS/Views`
- **View models / DTOs**: `ARS/ViewModels`
- **Background and domain services**: `ARS/Services`
- **Database utilities**: CLI tools under `ARS/Tools`

### Program startup and environment handling

`ARS/Program.cs` performs several important responsibilities:

- **.env loading**: Before building the host, a small inline `LoadDotEnv()` function reads a `.env` file in the current directory (if present), supporting both `KEY=VALUE` and `export KEY=VALUE` lines, and sets them as process environment variables. This allows overriding sensitive configuration like database connection strings and admin credentials without changing `appsettings.json`.
- **Service registration**:
  - `AddControllersWithViews()` for MVC.
  - Session state using an in-memory cache with a 30-minute idle timeout.
  - `ApplicationDbContext` configured with `UseMySql` and `ServerVersion.AutoDetect` against `DefaultConnection`.
  - `ISeatService`/`SeatService` for seat inventory operations.
  - `FlightCleanupService` as a hosted background service.
  - ASP.NET Core Identity configured with `User` and `IdentityRole<int>`, relaxed password rules for development, and unique email enforcement.
- **Development-only bootstrap logic** (guarded by `app.Environment.IsDevelopment()`):
  - Ensures an `Admin` role and a seeded admin user exist, using `ADMIN_EMAIL`, `ADMIN_USERNAME`, and `ADMIN_PASSWORD` from configuration or environment variables, with safe defaults for development.
  - Runs a best-effort MySQL compatibility/repair routine that:
    - Adds missing seat-related columns to `Flights` and `Reservations`.
    - Creates `SeatLayouts` and `Seats` tables if they are missing.
    - Adds helpful indexes and a foreign key from `Reservations.SeatId` to `Seats.SeatId` if absent, using `INFORMATION_SCHEMA` to avoid duplicate FK creation.
    - Seeds a default `SeatLayout` (`Default-6x40`) plus 6×40 seats, and backfills `Flights.SeatLayoutId` where null.
- **HTTP pipeline**:
  - Uses exception handler plus HSTS in non-development environments.
  - Enables HTTPS redirection, routing, session, authentication, and authorization.
  - Uses `MapStaticAssets()` / `.WithStaticAssets()` for static file handling.
  - Configures the default route `{controller=Home}/{action=Index}/{id?}`.

### Data access and domain model

`ApplicationDbContext` (`ARS/Data/ApplicationDbContext.cs`) extends `IdentityDbContext<User, IdentityRole<int>, int>`, integrating Identity with the domain entities. It defines `DbSet<T>`s for core concepts:

- **Cities & routes**: `City`, with self-referencing relationships from `Flight` via `OriginCity` / `DestinationCity` and restricted delete behavior.
- **Pricing**: `PricingPolicy` with a `Flights` navigation and a `PriceMultiplier`/`DaysBeforeDeparture` based pricing scheme.
- **Flights & schedules**:
  - `Flight` represents a logical route plus aircraft and base fare.
  - `Schedule` represents a specific departure date for a flight.
- **Seat layouts and inventory**:
  - `SeatLayout` holds an aircraft-wide layout with a `Seats` collection.
  - `Seat` is a physical seat definition (layout, row, column, label, cabin class, flags for exit row/premium, price modifiers).
  - `FlightSeat` is a per-schedule seat inventory row (linking a `Seat` to a `Schedule` with status, price, and reservation linkage).
- **Bookings & payments**:
  - `Reservation` captures a booking with passenger counts, class, dates, status, confirmation and blocking numbers, and optional seat assignment data.
  - `Payment` and `Refund` track financial transactions tied to reservations.

`OnModelCreating` wires these together:

- Configures all key relationships (flight–city, flight–pricing policy, schedule–flight, schedule–city, reservation–user/flight/schedule/seat/flightseat, payment–reservation, refund–reservation).
- Adds indexes and uniqueness constraints for email, flight numbers, confirmation numbers, airport codes, and composite keys enforcing one `Reservation` per `(ScheduleID, FlightSeatId)` and one `FlightSeat` per `(ScheduleId, SeatId)`.
- Calls a private `SeedData` method to populate reference data:
  - A small set of `City` rows (e.g., Manila, Cebu, Tokyo, Singapore, Hong Kong).
  - Several `PricingPolicy` entries implementing early-bird and last-minute pricing tiers.

The entity classes in `ARS/Models` mirror this structure. Two that are central to the booking flow:

- **`Flight`**: includes route info, aircraft type, `TotalSeats`, `BaseFare`, optional `SeatLayoutId`, and navigation properties to `City`, `PricingPolicy`, `Schedule`, and `Reservation`.
- **`Reservation`**: links a `User`, `Flight`, and `Schedule` with booking metadata, plus both a legacy `SeatLabel` string and modern `SeatId` / `FlightSeatId` navigation properties for seat assignment.

### Web layer: controllers and views

Controllers in `ARS/Controllers` implement the main business workflows; Razor views under `ARS/Views` render the UI for each feature area.

Key pieces:

- **FlightController**
  - Provides the primary flight search and listing at `/Flight` via `Index`, backed by `FlightSearchViewModel` and `FlightSearchResultViewModel`.
  - Supports one-way, round-trip, and multi-city searches (`Search` action) with reusable logic to query flights and schedules, compute available seats from existing `Reservation` records, and apply dynamic pricing.
  - Normalizes search inputs (default passengers, class, travel date) and filters flights based on origin/destination and whether they have schedules on the requested date.
  - For each candidate flight, derives a concrete departure/arrival `DateTime` using `Schedule.Date` plus the flight’s stored times.
  - Admin-only actions (`Create`, `Edit`, `Delete`) manage `Flight` entities; `Create` also tries to auto-create an initial `Schedule` for the departure date.
  - `All` acts as a permanent redirect shim from older `/Flight/All` routes to the new `/Flight` endpoint.

- **ReservationController**
  - Orchestrates the booking lifecycle: create, confirm, view, list, reschedule, and seat map retrieval.
  - Uses `UserManager<User>` to drive everything through Identity and ensures the current user owns the reservation for detail and reschedule actions.
  - In `Create` (GET/POST):
    - Validates login and computes pricing based on time to departure and selected cabin class, using `BookingViewModel`.
    - Enforces a hard booking cutoff 60 minutes before departure using `Schedule.Date` + `Flight.DepartureTime`.
    - Ensures a `Schedule` exists for the chosen date (creating if necessary) and then creates a `Reservation` with generated confirmation and blocking numbers.
    - Integrates seat selection in two modes:
      - **Persisted seat model**: uses `Seat` and `SeatLayout` (if the `Flight` has a `SeatLayoutId`) and ensures the chosen seat belongs to that layout and is not already booked for the same schedule.
      - **Legacy label mode**: stores a raw `SeatLabel` string for older flows.
    - Performs a compatibility insert into a legacy `Users` table (if present) using an idempotent `INSERT ... WHERE NOT EXISTS` to keep existing foreign keys satisfied while the system uses Identity’s `AspNetUsers` for real authentication.
  - `GetSeatMap` returns a JSON seat-map description for a given `flightId` and `travelDate`:
    - If the flight uses a `SeatLayout`, it queries `Seats` for that layout and marks seats as unavailable if any non-cancelled `Reservation` references them.
    - Otherwise it falls back to a heuristic 6‑across legacy layout based on `Flight.TotalSeats`, filling seats back-to-front and mixing in explicit `SeatLabel`s from reservations to mark occupied seats.
  - `MyReservations` and `Details` provide user-scoped views of current and past bookings, including payments and refunds.
  - `Reschedule`, `RescheduleSearch`, and `ConfirmReschedule`/`ConfirmReschedulePost` implement a rescheduling flow that:
    - Finds candidate replacement flights with enough capacity for existing passengers.
    - Recomputes pricing for the new date and class.
    - Calculates fare differences vs. amounts already paid, creating additional `Payment` records or `Refund` entries as needed.

- **Other controllers** (not exhaustively listed):
  - `AccountController` handles registration, login, profile, and password changes on top of Identity.
  - `PaymentController`, `RefundController`, `ScheduleController`, `SeatController`, `SeedController`, and `HomeController` each manage their respective feature areas using the same MVC + EF patterns.

Views under `ARS/Views` follow standard ASP.NET Core MVC conventions (per-controller subfolders plus shared layout and error views). `Views/Reservation` and `Views/Flight` contain the main booking and search pages.

### Services and background processing

- **SeatService (`ARS/Services/SeatService.cs`)**
  - `GenerateFlightSeatsAsync(scheduleId)` populates `FlightSeats` for a given schedule by copying rows from `Seats` for the flight’s associated `SeatLayout`. It uses raw SQL to insert missing `FlightSeats` rows and avoids duplicate inserts via a `NOT EXISTS` subquery.
  - `GetAvailableSeatsAsync(scheduleId)` queries `FlightSeats` where status is `Available`, includes the underlying `Seat` (`AircraftSeat`), and projects to a `FlightSeatDto` for the UI.
  - `ReserveSeatAsync(flightSeatId, reservationId)` uses a database transaction and conditional `UPDATE` to atomically change seat status from `Available` to `Reserved` and associate it with a reservation, rolling back if anything fails or concurrency conflicts arise.
  - `CancelReservationSeatAsync(reservationId)` reverses this mapping by clearing the `ReservedByReservationID` on `FlightSeats` and the `FlightSeatId` on `Reservation` in a transaction.

- **FlightCleanupService (`ARS/Services/FlightCleanupService.cs`)**
  - A `BackgroundService` that runs once at startup and then every 15 minutes.
  - Computes departure datetimes (`Schedule.Date` + `Flight.DepartureTime`) and finds schedules whose departures are in the past.
  - For expired schedules, deletes associated `Reservation` rows and then the schedules themselves using raw SQL.
  - After schedule cleanup, deletes any `Flight` that no longer has any schedules, again removing related reservations defensively.
  - All work is done in a scoped `ApplicationDbContext`, with logging around errors and actions.

### Migrations and data repair utilities

The EF Core migrations under `ARS/Migrations` define the schema evolution, including initial create, seat layout introduction, and subsequent seeding changes. Two notable patterns interact with these migrations:

- **Development-time schema repair in `Program.cs`**: uses raw `ALTER TABLE` / `CREATE TABLE` / `CREATE INDEX` statements wrapped in try/catch to make the app more resilient against partially applied or inconsistent migrations in development environments.
- **Standalone tooling under `ARS/Tools`**: each tool encapsulates a focused data-repair or inspection task (deduplicating seats/flight seats, inserting a specific migration row into `__efmigrationshistory`, inspecting mappings), sharing a common pattern of reading the `DefaultConnection` string from `appsettings.json`.

## Configuration and environment

- The primary configuration files are `ARS/appsettings.json` and `ARS/appsettings.Development.json`. They define the `DefaultConnection` MySQL connection string and logging levels.
- Environment variables loaded from `.env` or the host environment can override configuration values, including `DefaultConnection` and the admin seeding settings used in development.
- Identity runs with integer keys via `User` (under `ARS/Models`), and the app maintains compatibility with a legacy `Users` table in the database for some deployments by inserting shadow records when new `Reservation` rows are created.

These details should give future Warp agents enough context to navigate the codebase, understand how core booking and seat-management flows are wired together, and run the app plus supporting tools effectively.
