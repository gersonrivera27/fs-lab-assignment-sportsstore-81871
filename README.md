# SportsStore - Full Stack Development Assignment

## Student
- **Name:** Gerson Rivera
- **Student Number:** 81871

## Overview
Modernised SportsStore application upgraded from .NET 6 to .NET 8 with Stripe payment integration, Serilog structured logging, and GitHub Actions CI pipeline.

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
3. Test card: `4242 4242 4242 4242`, any future expiry, any CVC
4. Payment flow: Cart → Checkout → Stripe PaymentIntent → Order confirmation

## Logging Setup
- **Serilog** with two sinks: Console and Rolling File (`Logs/` directory)
- Configured via `appsettings.json` using `ReadFrom.Configuration`
- Logs: application startup, HTTP requests, checkout flow, payment events, exceptions
- Enriched with LogContext, MachineName, EnvironmentName

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
Branches follow the format: `gr-YYYYMMDD` (e.g., `gr-20260223`)

## Technologies
- ASP.NET Core 8.0
- Entity Framework Core 8.0 with SQLite
- Serilog structured logging
- Stripe.net payment processing
- xUnit + Moq for unit testing
- GitHub Actions CI/CD
