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

Then open:

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

## API surface

| Requirement | Method & route                          | Notes                                                 |
|-------------|-----------------------------------------|-------------------------------------------------------|
| #1          | `POST /cards`                           | Create a card with a credit limit.                    |
| #2          | `POST /cards/{cardId}/transactions`     | Store a purchase transaction (USD).                   |
| #3          | `GET /transactions/{transactionId}?currency=` | Transaction converted to a currency.           |
| #4          | `GET /cards/{cardId}/balance?currency=` | Available balance converted to a currency.            |

## Notes / decisions

- **Base currency is USD.** Treasury rates are USD-relative, so stored amounts are treated as USD
  and conversion is `amount × rate`.
- **`currency` is the Treasury `country_currency_desc`** (e.g. `Canada-Dollar`, `Euro Zone-Euro`),
  not an ISO code — it matches the dataset exactly and avoids a brittle ISO mapping.
- **Schema** lives in `AppDbContext`; the skeleton creates it via `EnsureCreated()`. This is
  replaced by an EF Core migration once the model stabilises. `schema.sql` documents the result.
- **NuGet versions** are pinned to .NET 10-era releases; `dotnet restore` will flag any that need a
  minor bump in your environment.
