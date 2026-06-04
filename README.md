# Wex Card API

A C# / ASP.NET Core (.NET 10) service for cards, purchase transactions, and currency-converted
reads backed by the U.S. Treasury Reporting Rates of Exchange API.

> **Status: skeleton.** The schema and the full API surface are in place; the four endpoints are
> stubbed and return `501 Not Implemented`. Business logic and tests are added in later iterations.

## Prerequisites

- **.NET 10 SDK** — to build, run, and test locally.
- **Docker** (with Compose) — to run PostgreSQL and the containerised app, and to run the
  integration tests (they spin up a throwaway Postgres container via Testcontainers).

That's it — no local PostgreSQL install needed.

## Run it

```bash
docker compose up --build
```

Or use the helper scripts, which start the stack, wait for the API to be healthy, and open the
console UI automatically:

```bash
./run.ps1                            # Windows (PowerShell); -Compose "podman compose" for Podman
./run.sh                             # Linux/macOS;          COMPOSE="podman compose" ./run.sh
```

Then open:

- Console UI: http://localhost:8080/
- Swagger UI: http://localhost:8080/swagger
- Health check: http://localhost:8080/health

To run the API locally with `dotnet` instead, start just the database and run the app:

```bash
docker compose up -d db
dotnet run --project src/Wex.CardApi
```

## Tests

With Docker running:

```bash
dotnet test
```

- **Wex.CardApi.UnitTests** — fast, no I/O (conversion math, rate selection, balance, validation).
- **Wex.CardApi.IntegrationTests** — host the app via `WebApplicationFactory` against a real
  PostgreSQL container (Testcontainers), firing HTTP at the actual endpoints. The Treasury API is
  faked so tests are deterministic.

The test projects use **xUnit.net v3**, running through `dotnet test` as usual.

**Contract tests** (`TreasuryApiContractTests`) hit the *live* Treasury API to verify our
assumptions about its contract. They are marked `[Fact(Explicit = true)]`, so a normal
`dotnet test` skips them; they run when selected by a filter, and need network access (not
Docker):

```bash
dotnet test --filter "FullyQualifiedName~TreasuryApiContractTests"
```

## API surface

| Requirement | Method & route                          | Notes                                                 |
|-------------|-----------------------------------------|-------------------------------------------------------|
| #1          | `POST /cards`                           | Create a card with a credit limit.                    |
| #2          | `POST /cards/{cardId}/transactions`     | Store a purchase transaction (USD).                   |
| #3          | `GET /transactions/{transactionId}?currency=` | Transaction converted to a currency.           |
| #4          | `GET /cards/{cardId}/balance?currency=` | Available balance converted to a currency.            |

## Additional read endpoints

Beyond the four required endpoints, two read-only endpoints back the console UI (listing,
not part of the brief):

| Method & route | Notes |
|---|---|
| `GET /cards` | List all cards (populates the card chooser). |
| `GET /transactions?cardId=&page=&pageSize=&currency=` | A card's transactions, newest first, paged (10/page). With `currency`, each row is converted at the rate for its own purchase date. |
| `GET /currencies` | Currencies available for conversion, sourced live from the Treasury dataset. |

## Notes / decisions

- **Base currency is USD.** Treasury rates are USD-relative, so stored amounts are treated as USD
  and conversion is `amount × rate`.
- **`currency` is the Treasury `country_currency_desc`** (e.g. `Canada-Dollar`, `Euro Zone-Euro`),
  not an ISO code — it matches the dataset exactly and avoids a brittle ISO mapping.
- **Schema** is managed by an EF Core migration (`src/Wex.CardApi/Migrations`) applied on
  startup via `Database.Migrate()`. `schema.sql` is a human-readable reference. If you ran an
  earlier build that used `EnsureCreated()`, reset the dev database first so the migration can
  apply cleanly: `docker compose down -v`.
- **EF tooling (optional):** `dotnet tool install --global dotnet-ef`, then e.g.
  `dotnet ef migrations add <Name> -p src/Wex.CardApi` or `dotnet ef migrations script -p src/Wex.CardApi`.
- **NuGet versions** are pinned to .NET 10-era releases; `dotnet restore` will flag any that need a
  minor bump in your environment.
