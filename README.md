# Product Catalog – .NET & Angular assessment

A product management API (ASP.NET Core 10, EF Core 10, SQL Server) and an Angular 22 frontend that consumes it.

- All 9 required endpoints, plus `GET /api/categories` for the UI.
- Unique **6-digit product ids that stay unique across multiple running instances** (SQL Server `SEQUENCE`).
- Code-first EF Core migrations and database seeding.
- Validation, RFC 9457 problem details for every error, optimistic concurrency on stock movements.
- 102 backend tests (unit with mocks and the EF Core InMemory provider, HTTP integration, SQL Server, BDD) and 20 frontend tests.

---

## Contents

1. [Quick start](#quick-start)
2. [API](#api)
3. [Architecture](#architecture)
4. [Key design decisions](#key-design-decisions)
5. [Tests](#tests)
6. [Possible next steps](#possible-next-steps)

---

## Quick start

### Option A – Docker (no local SDKs needed)

```bash
docker compose up --build
```

| What | URL |
| --- | --- |
| Web app | http://localhost:8080 |
| Swagger UI | http://localhost:8080/swagger |
| SQL Server | `localhost,14333` (user `sa`, password from `SQL_SA_PASSWORD`, default `ProductCatalog!2026`) |

Run several API instances behind the same database with `docker compose up --build --scale api=3`.

### Option B – Local development

Prerequisites: **.NET 10 SDK**, **Node.js 22+**, and **SQL Server** (SQL Server Express **LocalDB**, installed with Visual Studio, is the default; any SQL Server works).

**1. API** – http://localhost:5011, Swagger UI at http://localhost:5011/swagger

```bash
cd backend
dotnet run --project src/ProductCatalog.Api --launch-profile http
```

In the `Development` environment the API applies pending migrations and seeds sample data at startup
(`Database:ApplyMigrationsOnStartup` / `Database:SeedSampleData`). To use another SQL Server, override the connection string:

```bash
dotnet user-secrets set "ConnectionStrings:ProductCatalog" "Server=...;Database=ProductCatalog;..." --project src/ProductCatalog.Api
```

To manage migrations manually (the `dotnet-ef` tool is pinned in `backend/dotnet-tools.json`):

```bash
cd backend
dotnet tool restore
dotnet ef database update -p src/ProductCatalog.Infrastructure -s src/ProductCatalog.Api
dotnet ef migrations add <Name> -p src/ProductCatalog.Infrastructure -s src/ProductCatalog.Api -o Persistence/Migrations
```

**2. Frontend** – http://localhost:4200 (the dev server proxies `/api` to the API, see `frontend/proxy.conf.json`)

```bash
cd frontend
npm install
npm start
```

---

## API

| Method | Route | Success | Errors |
| --- | --- | --- | --- |
| `GET` | `/api/products` | 200 – all products, with stock | |
| `POST` | `/api/products` | 201 – created product + `Location` | 400 validation, 409 duplicated SKU |
| `GET` | `/api/products/{id}` | 200 | 404 |
| `PUT` | `/api/products/{id}` | 200 – updated product | 400, 404, 409 |
| `DELETE` | `/api/products/{id}` | 204 | 404 |
| `POST` | `/api/products/{id}/decrement-stock/{quantity}` | 200 – updated product | 400 quantity ≤ 0, 404, 409 insufficient stock / concurrent change |
| `POST` | `/api/products/{id}/add-to-stock/{quantity}` | 200 – updated product | 400, 404, 409 |
| `GET` | `/api/products/search?name={name}` | 200 – partial, case-insensitive match | 400 empty term |
| `GET` | `/api/products/stock-level?min={min}&max={max}` | 200 – inclusive range, either bound optional | 400 invalid range |
| `GET` | `/api/categories` | 200 | |

Every list response includes `stockQuantity` for each product. Errors use `application/problem+json`; validation errors carry an
`errors` dictionary keyed by camelCase field name, which the frontend maps back to the form fields.

```jsonc
// POST /api/products
{ "sku": "LNS-PRG-160", "name": "Progressive Lens 1.60", "description": "…", "price": 349.00, "stockQuantity": 40, "categoryId": 1 }
```

**Product** fields: `id` (6 digits, server generated), `sku` (unique, normalised to upper case), `name`, `description`, `price`, `stockQuantity`,
`categoryId` / `categoryName`, `createdAtUtc`, `updatedAtUtc`. Products belong to a **Category** (one-to-many).

---

## Architecture

### Backend – Clean Architecture

Dependencies point inwards: `Api → Infrastructure → Application → Domain`.

```
backend/
├── src/
│   ├── ProductCatalog.Domain/            # Entities and business rules – no dependencies
│   │   ├── Products/                     # Product aggregate, ProductId rules
│   │   ├── Categories/
│   │   └── Exceptions/                   # DomainException, InsufficientStockException, …
│   ├── ProductCatalog.Application/       # Use cases – depends only on Domain
│   │   ├── Abstractions/                 # Ports: IProductRepository, IUnitOfWork, IProductIdGenerator
│   │   ├── Common/Exceptions/            # NotFound, Conflict, ConcurrencyConflict, …
│   │   ├── Products/
│   │   │   ├── Contracts/                # Request/response DTOs (one type per file)
│   │   │   ├── Validation/               # FluentValidation validators
│   │   │   ├── Mapping/
│   │   │   └── Services/                 # ProductService (CRUD/queries), ProductStockService (stock movements)
│   │   └── Categories/
│   ├── ProductCatalog.Infrastructure/    # EF Core + SQL Server adapters
│   │   ├── Persistence/
│   │   │   ├── Configurations/           # Fluent entity mappings
│   │   │   ├── Migrations/               # Code-first migrations
│   │   │   ├── Repositories/
│   │   │   └── Seeding/                  # Seed data + DatabaseInitializer
│   │   └── IdGeneration/                 # SqlSequenceProductIdGenerator
│   └── ProductCatalog.Api/               # HTTP only: controllers, global exception handler, composition root
└── tests/
    ├── ProductCatalog.UnitTests/         # Domain, services (NSubstitute mocks), validators, repositories (EF InMemory)
    ├── ProductCatalog.IntegrationTests/  # Full HTTP pipeline (WebApplicationFactory + EF InMemory) and SQL Server tests
    ├── ProductCatalog.AcceptanceTests/   # BDD scenarios (Reqnroll / Gherkin)
    └── ProductCatalog.Testing/           # Shared test infrastructure (API factory, typed API client, fakes)
```

- **Domain** – `Product` is an aggregate with private setters; state changes go through behaviour methods
  (`DecrementStock`, `AddToStock`, `Update`) that enforce invariants (6-digit id, non-negative stock and price, etc.).
- **Application** – services orchestrate validation, repositories and the unit of work, and depend only on interfaces.
  DTOs live here as the application's contracts, separate from the entities.
- **Infrastructure** – implements the ports with EF Core. EF exceptions are translated into application exceptions in `UnitOfWork`,
  so no persistence details leak upwards.
- **Api** – thin controllers; an `IExceptionHandler` maps exceptions to problem details (400/404/409/503/500) in one place.

### Frontend – feature-based Angular

```
frontend/src/app/
├── core/                                 # App-wide singletons
│   ├── config/api.config.ts              # API_BASE_URL injection token
│   ├── http/                             # ApiError + interceptor (HttpErrorResponse → ApiError)
│   ├── notifications/                    # Snackbar notifications
│   └── layout/                           # Shell (toolbar + router outlet)
├── shared/ui/confirm-dialog/             # Reusable, feature-agnostic UI
└── features/
    ├── categories/
    │   ├── models/                       # Category interface
    │   └── data-access/                  # CategoriesApiService (cached reference data)
    └── products/
        ├── models/                       # Product, request and filter models (no logic in services)
        ├── data-access/                  # ProductsApiService – HTTP only, stateless
        ├── state/                        # ProductsStore – signals-based state, provided per feature route
        ├── components/                   # Presentational: filters, table, form, stock dialog, status chip
        ├── pages/                        # Containers: list page, create/edit page
        └── products.routes.ts            # Lazy-loaded feature routes
```

- **Separation of concerns**: models ↔ HTTP access ↔ state ↔ presentational components ↔ container pages. Categories are their own feature
  with their own service.
- **Async data**: RxJS for HTTP (`switchMap` cancels outdated searches), Angular signals for state, `rxResource` to load the product being edited,
  `takeUntilDestroyed` everywhere a subscription outlives a call.
- **Reflecting API changes**: every mutation returns the updated product, which replaces the row immediately; the list is then re-synced
  with the server. An auto-refresh (every 15 s while the tab is visible, can be switched off) picks up changes made by other clients.
- **UX**: server-side search and stock-range filters, client-side category filter, "Low stock" shortcut, sortable/paginated table,
  stock status chips and summary cards, confirmation before deleting, client validation mirroring the API rules and API field errors
  shown inline.
- Standalone components, `OnPush` change detection, signal inputs/outputs, new control flow, Angular Material (Material 3).

---

## Key design decisions

### Unique 6-digit ids across multiple instances

Ids come from a SQL Server **sequence** (`catalog.ProductIds`, `START 100000`, `MIN 100000`, `MAX 999999`, `NO CYCLE`) created by the
migration. `NEXT VALUE FOR` is atomic inside the database that every instance shares, so concurrent instances never receive the same
value and need no coordination with each other – unlike "read max id + 1" or in-process counters. A primary key and a
`CHECK (Id BETWEEN 100000 AND 999999)` constraint are a second line of defence. When the 900,000 ids run out, the API returns
`503` with a clear message instead of an opaque database error.

The generator sits behind `IProductIdGenerator`, so tests can use an in-memory implementation.
`SqlSequenceProductIdGeneratorTests` starts 4 independent service providers (simulating 4 instances) against a real SQL Server and checks that
200 concurrently allocated ids are unique.

### Concurrency on stock

`Product.RowVersion` is a SQL Server `rowversion` concurrency token. If two requests (possibly on different instances) change the same
product, the second save fails with `409 Conflict` instead of silently overwriting the first change. Insufficient stock is a domain rule
(`InsufficientStockException` → `409`), backed by a `CHECK (StockQuantity >= 0)` constraint.

### Database

SQL Server is used for every running environment (LocalDB for local development, a container in Docker). The EF Core InMemory provider is
used **only in tests**. Reference data (categories) is seeded through the migration with `HasData`; sample products are seeded at startup
by `DatabaseInitializer`, because their ids must come from the sequence. Seeding is idempotent and safe if several instances start together.

### Validation

FluentValidation in the Application layer (required fields, lengths, SKU format, price > 0 with max 2 decimals, stock ≥ 0, valid stock
range), plus business checks (category exists, SKU unique) and domain invariants in the entity.

---

## Tests

```bash
cd backend
dotnet test                                            # all backend tests
dotnet test --filter "Category!=SqlServer"             # skip the test that needs SQL Server

cd frontend
npm test -- --watch=false
```

| Project | What it covers |
| --- | --- |
| `UnitTests` | Domain rules; `ProductService` and `ProductStockService` with **NSubstitute** mocks and a fake `TimeProvider`; validators; repositories and seeding against the **EF Core InMemory** provider |
| `IntegrationTests` | Every endpoint through the real HTTP pipeline (`WebApplicationFactory`, EF InMemory): status codes, problem details, routing, parallel creation. Plus the multi-instance id test on SQL Server (LocalDB by default, or set `PRODUCTCATALOG_TEST_SQLSERVER`; skipped when unavailable) |
| `AcceptanceTests` | BDD scenarios in Gherkin (Reqnroll) for stock management and product discovery |
| Frontend (Vitest) | API service (URLs, params, error mapping), store (endpoint selection, state updates), product form and stock dialog behaviour |

---

## Possible next steps

- Authentication/authorisation (e.g. JWT with a read-only and an operator role).
- Server-side pagination and sorting for large catalogues.
- `ETag` / `If-Match` on `PUT` to expose optimistic concurrency to clients.
- Push updates (SignalR) instead of polling for changes made by other clients.
- CI pipeline (build, tests, container images) and end-to-end UI tests (Playwright).
