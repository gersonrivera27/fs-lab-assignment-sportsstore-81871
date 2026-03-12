# SportsStore - Full Stack Development Assignment

## Student
- **Name:** Gerson Rivera
- **Student Number:** 81871

## Overview
Modernised SportsStore application upgraded from .NET 6 to .NET 8 with Stripe payment integration, Serilog structured logging (with SEQ ingestion), and GitHub Actions CI pipeline.

## Upgrade Steps (.NET 6 → .NET 8)
1. Updated `TargetFramework` from `net6.0` to `net8.0` in both `.csproj` files
2. Updated all NuGet packages to .NET 8 compatible versions
3. Replaced SQL Server with SQLite for cross-platform Mac compatibility
4. Deleted old SQL Server migrations and created new SQLite migrations
5. Removed `global.json` that pinned .NET 6 SDK
6. Verified build (0 errors) and all 18 tests passing

## Stripe Configuration
1. Install Stripe.net package: `dotnet add package Stripe.net`
2. Configure API keys using User Secrets (keys are NOT in source code):
```bash
cd SportsStore
dotnet user-secrets init
dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_KEY"
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_YOUR_KEY"
```
3. Payment flow: Cart → Checkout → Stripe PaymentIntent → Order confirmation

### Stripe Test Cards
| Card Number | Scenario |
|---|---|
| `4242 4242 4242 4242` | Successful payment |
| `4000 0000 0000 0002` | Card declined |
| `4000 0000 0000 9995` | Insufficient funds |
| `4000 0000 0000 0069` | Expired card |

Use any future expiry date and any 3-digit CVC.

## Logging Setup
- **Serilog** with three sinks: Console, Rolling File (`Logs/` directory), and SEQ
- Configured via `appsettings.json` using `ReadFrom.Configuration`
- Enriched with: LogContext, MachineName, EnvironmentName
- Environment-specific log levels via `appsettings.Development.json` (Debug level in Development)
- Structured logging throughout the application:
  - **Application lifecycle**: startup, shutdown, fatal errors
  - **Cart operations**: add product (ProductId, ProductName, Price), remove product, cart totals
  - **Checkout flow**: cart items detail, payment intent creation, order confirmation
  - **Stripe payments**: successful payments, failed payments (with decline codes), cancelled payments
  - **Authentication**: login attempts (success/failure with username), logout events
  - **HTTP requests**: via `UseSerilogRequestLogging()` middleware

### SEQ Setup (for structured log viewing)
1. Install and run SEQ via Docker:
```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq
```
2. Open SEQ dashboard at `http://localhost:5341`
3. SEQ is already configured in `appsettings.json` as a Serilog sink
4. Run the application and structured logs will appear in SEQ automatically
5. Use SEQ filters to search by properties like `ProductId`, `CustomerName`, `PaymentIntentId`, `StripeError`, etc.

## How to Run Locally
```bash
git clone https://github.com/gersonrivera27/fs-lab-assignment-sportsstore-81871.git
cd fs-lab-assignment-sportsstore-81871
dotnet restore
dotnet build
dotnet run --project SportsStore
# Open http://localhost:5000
```

## CI Pipeline
GitHub Actions workflow runs on push to main and pull requests:
- Restores dependencies
- Builds solution in Release mode
- Runs all unit tests (18/18 passing)

## Branch Naming Convention
Branches follow the format: `gr-feature-description` (e.g., `gr-stripe-integration`)

## Technologies
- ASP.NET Core 8.0
- Entity Framework Core 8.0 with SQLite
- Serilog structured logging (Console, File, SEQ sinks)
- Stripe.net payment processing
- xUnit + Moq for unit testing
- GitHub Actions CI/CD
