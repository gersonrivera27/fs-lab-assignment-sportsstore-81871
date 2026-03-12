# Presentation Guide - SportsStore Assignment
## Gerson Rivera - Student 81871

---

## DEMO ORDER (follow this sequence in class)

### Step 1 - Start SEQ (before running the app)
```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq
```
Open SEQ at: http://localhost:5341

### Step 2 - Run the app
```bash
cd fs-lab-assignment-sportsstore-81871
dotnet run --project SportsStore
```
Open app at: http://localhost:5000

### Step 3 - Demo Flow
1. **Browse products** - show pagination and category filtering
2. **Add to cart** - add a Kayak (show log in SEQ with ProductId, ProductName, Price)
3. **View cart** - show cart page (logs cart item count and total)
4. **Go to Checkout** - fill in name/address
5. **FAILED PAYMENT** - use card `4000 0000 0000 0002`, any future date, any CVC
   - Show the red error alert on screen
   - Switch to SEQ and show the `Stripe payment FAILED` log with DeclineCode: generic_decline
6. **SUCCESSFUL PAYMENT** - use card `4242 4242 4242 4242`, any future date, any CVC
   - Show the green success alert
   - Show order confirmation page
   - Switch to SEQ and show the `Order created successfully` log with PaymentIntentId

### Step 4 - Show SEQ structured properties
In SEQ, click on any log entry and show the structured properties:
- `ProductId`, `ProductName`, `Price` (cart logs)
- `PaymentIntentId`, `DeclineCode`, `StripeError` (payment logs)
- `CustomerName`, `CartTotal`, `ItemCount` (order logs)
- `MachineName`, `EnvironmentName` (enrichers)

---

## PART A - UPGRADE (.NET 6 to .NET 8) - 20 marks

### What to explain:
"I upgraded the project from .NET 6 to .NET 8. Here's what I changed:"

1. **Target framework** - both `.csproj` files changed from `net6.0` to `net8.0`
   - File: `SportsStore/SportsStore.csproj` line 3
   - File: `SportsStore.Tests/SportsStore.Tests.csproj` line 3

2. **NuGet packages** - all updated to v8.0.0 compatible versions:
   - `Microsoft.EntityFrameworkCore.Sqlite` 6.x → 8.0.0
   - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 6.x → 8.0.0
   - `Microsoft.EntityFrameworkCore.Design` 6.x → 8.0.0

3. **Database change** - replaced SQL Server with SQLite for Mac compatibility
   - Connection string: `Data Source=SportsStore.db`
   - Deleted old SQL Server migrations, created new SQLite ones

4. **Removed** `global.json` that was pinning .NET 6 SDK

5. **No regressions** - all 18 tests still pass, application runs correctly

### If teacher asks: "Why .NET 8 and not .NET 10?"
".NET 8 is the current Long Term Support (LTS) version. It's stable and widely supported. The assignment allows .NET 8, 9, or 10."

### If teacher asks: "Why SQLite instead of SQL Server?"
"The assignment says Mac users should use SQLite or Docker SQL Server. I chose SQLite because it's simpler - no Docker needed, the database is just a file."

---

## PART B - SERILOG LOGGING - 20 marks

### What to explain:
"I integrated Serilog with structured logging across the entire application."

### 1. Setup (Program.cs lines 6-10):
```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .CreateLogger();
```
"Serilog is configured BEFORE the WebApplication builder is created, so we catch even startup errors."

### 2. Three sinks configured (appsettings.json):
- **Console** - for development, see logs in terminal
- **Rolling File** - daily log files in `Logs/` folder (e.g., `sportsstore-20260311.log`)
- **SEQ** - centralized structured logging server at `http://localhost:5341`

### 3. Enrichers (appsettings.json lines 35-39):
- `FromLogContext` - adds contextual properties to log entries
- `WithMachineName` - adds the server name (useful in distributed systems)
- `WithEnvironmentName` - adds Development/Production environment

### 4. Structured logging (NOT string concatenation):
```csharp
// CORRECT - structured properties (what I use)
Log.Information("Product added to cart. ProductId: {ProductId}, ProductName: {ProductName}",
    product.ProductID, product.Name);

// WRONG - string concatenation (I don't do this)
Log.Information("Product added to cart. ProductId: " + product.ProductID);
```
"The difference is that structured properties become searchable fields in SEQ."

### 5. Where logging is used:
| Area | What is logged |
|------|---------------|
| **App lifecycle** | Startup, shutdown, fatal errors (Program.cs) |
| **Products** | Category browsing, page navigation (HomeController) |
| **Cart** | Add product (with ProductId, Name, Price), remove product, cart totals (Cart.cshtml.cs) |
| **Checkout** | Cart items detail, payment intent creation (OrderController) |
| **Payments** | Success, failure (with decline code), cancellation (OrderController + StripePaymentService) |
| **Auth** | Login success/failure (with username), logout (AccountController) |
| **HTTP requests** | All requests via `UseSerilogRequestLogging()` middleware |

### 6. Environment-specific log levels (appsettings.Development.json):
- Development: `Debug` level (more verbose)
- Production: `Information` level (less verbose)

### DEMO in SEQ:
Show SEQ dashboard, click on a log entry, expand properties panel to show structured data.

---

## PART C - STRIPE INTEGRATION - 30 marks

### What to explain:
"I integrated Stripe for payment processing using the PaymentIntent API."

### 1. Architecture - Clean separation:
```
Interface:    IPaymentService (Infrastructure/)
    └── Method: CreatePaymentIntent(decimal amount) → string? clientSecret

Implementation: StripePaymentService (Infrastructure/)
    └── Uses Stripe.net SDK
    └── Converts amount to cents (Stripe requirement)
    └── Returns ClientSecret for frontend

Registered in: Program.cs line 39
    └── builder.Services.AddScoped<IPaymentService, StripePaymentService>();
```

### 2. Security - API keys NOT in source code:
```bash
# Keys stored in User Secrets (never committed to Git)
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..."
```
"If you search the entire repo, you won't find any Stripe keys. They're stored in User Secrets which is a .NET feature that keeps secrets outside the project folder."

### 3. Payment flow (explain step by step):

```
1. User fills checkout form and clicks "Complete Order"
   ↓
2. JavaScript prevents default form submit
   ↓
3. POST /Order/CreatePaymentIntent
   → Server calls Stripe API
   → Returns clientSecret to frontend
   ↓
4. stripe.confirmCardPayment(clientSecret, { card })
   → Stripe validates the card
   ↓
5a. SUCCESS (status: 'succeeded')
   → PaymentIntentId saved in hidden field
   → Form submits to POST /Order/Checkout
   → Order saved to database with PaymentIntentId
   → Cart cleared
   → Redirect to /Completed page

5b. FAILURE (result.error)
   → Red alert shown to user: "Payment failed: Your card was declined"
   → POST /Order/PaymentFailed → logs error details to Serilog/SEQ
   → User can try again with different card

5c. CANCELLED (status: 'canceled')
   → Warning alert shown to user
   → POST /Order/PaymentCancelled → logs to Serilog/SEQ
   → User can try again
```

### 4. StripePaymentService.cs - Key code to explain:
```csharp
public string? CreatePaymentIntent(decimal amount)
{
    var options = new PaymentIntentCreateOptions
    {
        Amount = (long)(amount * 100),  // Stripe expects cents, not dollars
        Currency = "usd",
        PaymentMethodTypes = new List<string> { "card" },
    };

    var service = new PaymentIntentService();
    var paymentIntent = service.Create(options);
    return paymentIntent.ClientSecret;  // Frontend needs this to confirm payment
}
```

### 5. Error handling:
- `StripeException` caught separately for Stripe API errors
- General `Exception` caught for unexpected errors
- Both logged with full context (amount, error message, error code)

### Test cards for demo:
| Card | Result |
|------|--------|
| `4242 4242 4242 4242` | Success |
| `4000 0000 0000 0002` | Declined |
| `4000 0000 0000 9995` | Insufficient funds |

### If teacher asks: "What is a PaymentIntent?"
"A PaymentIntent is a Stripe object that tracks the lifecycle of a payment. It's created on the server, confirmed on the client with the card details, and ensures the payment is processed before the order is saved."

### If teacher asks: "Why not send card details to your server?"
"PCI compliance. Card details never touch our server. They go directly from the browser to Stripe's servers via their JavaScript SDK. We only receive a PaymentIntentId confirming the payment was successful."

---

## PART D - GITHUB ACTIONS CI - 10 marks

### What to explain:
"I have a GitHub Actions CI pipeline that runs on every push and pull request."

### Workflow file (.github/workflows/ci.yml):
```yaml
name: SportsStore CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    - name: Restore dependencies
      run: dotnet restore
    - name: Build
      run: dotnet build --no-restore --configuration Release
    - name: Test
      run: dotnet test --no-build --configuration Release --verbosity normal
```

### Key points:
- **Triggers**: runs on push to `main` and on pull requests to `main`
- **Steps**: restore → build → test
- **If tests fail, the pipeline fails** (blocks merging)
- Uses latest .NET 8 SDK
- 18/18 tests passing

### DEMO: Show the Actions tab in GitHub repo, show a green checkmark on a recent commit.

---

## PART E - CODE QUALITY - 10 marks

### Clean separation of concerns:
- **Models** - only data, no logic (Product, Order, Cart)
- **Controllers** - handle HTTP, delegate to services
- **Infrastructure** - payment service, tag helpers
- **Repositories** - data access (EFStoreRepository, EFOrderRepository)
- **Views** - presentation only

### No hardcoded secrets:
- Stripe keys in User Secrets
- Connection strings in appsettings.json (SQLite files, no passwords)

### Professional naming:
- PascalCase for classes and methods
- camelCase for local variables
- Interfaces prefixed with `I` (IPaymentService, IStoreRepository)

---

## PART F - PROFESSIONAL PRACTICE - 10 marks

### Branch naming:
Format: `gr-feature-description` (e.g., `gr-stripe-integration`, `gr-serilog-setup`)

### Commit messages:
Every commit includes student name: "Gerson Rivera"

### Example commit:
```
Add Stripe payment integration - Gerson Rivera

- Implemented StripePaymentService with PaymentIntent API
- Added checkout form with Stripe Elements
- Handle success, failure, and cancellation
```

---

## COMMON VIVA QUESTIONS AND ANSWERS

### Q: "Walk me through what happens when a user clicks 'Complete Order'."
"The JavaScript prevents the default form submit. It sends a POST to `/Order/CreatePaymentIntent` which calls our `StripePaymentService`. The service creates a PaymentIntent on Stripe and returns a `clientSecret`. The frontend uses that secret with `stripe.confirmCardPayment()` to charge the card. If the payment succeeds, the PaymentIntentId is stored in a hidden field and the form submits normally to save the order."

### Q: "How do you handle failed payments?"
"When Stripe returns an error, we show the user a red alert with the error message. We also POST the error details to `/Order/PaymentFailed` so it gets logged to Serilog and SEQ with the decline code, PaymentIntentId, and customer name. The user can try again with a different card."

### Q: "Where are your Stripe keys stored?"
"In .NET User Secrets. They're stored in `~/.microsoft/usersecrets/` on the machine, completely outside the project folder. They're never committed to Git. The app reads them through `IConfiguration` like any other config value."

### Q: "What is structured logging?"
"Instead of logging plain text strings, I log named properties like `{ProductId}`, `{CustomerName}`, `{PaymentIntentId}`. These become searchable, filterable fields in SEQ. So I can search for all logs related to a specific ProductId or find all failed payments."

### Q: "What sinks do you use?"
"Three sinks: Console for development, Rolling File for persistent logs (one file per day in the Logs folder), and SEQ for centralized structured log viewing and searching."

### Q: "What enrichers do you use?"
"I use `FromLogContext` to add contextual properties, `WithMachineName` to add the server name, and `WithEnvironmentName` to distinguish Development from Production logs."

### Q: "How does your CI pipeline work?"
"GitHub Actions runs on every push to main and every pull request. It restores NuGet packages, builds in Release mode, and runs all 18 unit tests. If any test fails, the pipeline fails and the PR can't be merged."

### Q: "Why SQLite?"
"The assignment recommends SQLite for Mac users. It's a file-based database - no server needed. The app uses two SQLite files: `SportsStore.db` for products and orders, `Identity.db` for user authentication."

### Q: "Explain dependency injection in your project."
"In `Program.cs`, I register services with the DI container. For example, `AddScoped<IPaymentService, StripePaymentService>()` means every HTTP request gets its own instance of StripePaymentService. Controllers declare their dependencies in the constructor, and ASP.NET Core automatically injects them."

### Q: "What is the Repository pattern?"
"I have `IStoreRepository` and `IOrderRepository` interfaces that define data access methods. The implementations (`EFStoreRepository`, `EFOrderRepository`) use Entity Framework Core. This separates data access from business logic and makes testing easier - in tests I use Moq to create fake repositories."

### Q: "Show me a failed payment in SEQ."
Demo: Use card `4000 0000 0000 0002` → Go to SEQ → Filter for `Stripe payment FAILED` → Show the structured properties: ErrorMessage, DeclineCode, PaymentIntentId, CartTotal, CustomerName.
