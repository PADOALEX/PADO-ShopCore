# PADO ShopCore

[![.NET 10 CI](https://github.com/PADOALEX/PADO-ShopCore/actions/workflows/ci.yml/badge.svg)](https://github.com/PADOALEX/PADO-ShopCore/actions/workflows/ci.yml)

**ASP.NET Core 10 E-commerce REST API** · PostgreSQL · EF Core 10 · JWT RBAC · transactional inventory · xUnit

PADO ShopCore is a portfolio-focused backend for an online electronics store, evolved from an existing `ShopProject` codebase. It models a product catalog, authenticated carts, orders, promotional pricing, inventory reservations at checkout, order lifecycle transitions, and inventory-only returns. The original four EF Core migrations remain in history, with one additive integrity migration.

> **Scope:** This is a backend demonstration, not a payment gateway or production commerce deployment. The `Paid` status is set by a privileged operator; no real money moves in this application. See [limitations](#known-limitations).

## Features

- Public product listing and categories; pagination and optional category filtering.
- User registration/login with BCrypt password hashes and short-lived signed JWTs.
- `User` and `Admin` roles, server-validated resource ownership, admin-only catalog and promotion changes.
- Cart management with stock validation. The server obtains the customer ID from the JWT, not client input.
- PostgreSQL **serializable checkout transaction** with conditional atomic stock decrement, best personal discount, limited-use promo code, order line price snapshots, cart clear, and rollback on failure.
- Cancellation restores inventory exactly once while orders are in `Created` state.
- Admin order lifecycle: `Created → Paid → Shipped → Delivered` (manual workflow only).
- Delivered-order refund request validation; prevent claiming more than purchased (including previous pending and approved requests); approve/reject once. Approval restores inventory; it is **not** financial reimbursement.
- DTO-based requests/responses, DataAnnotations validation, RFC 7807 `ProblemDetails`, 401/403/404/409 error handling.
- Database constraints and unique indexes for key invariants; explicit one-off admin bootstrapping.
- Development Swagger UI; xUnit unit tests and PostgreSQL-backed integration tests; GitHub Actions pipeline.

## Architecture

```text
PADO-ShopCore/
├── ShopAPI.Core/                Domain entities, pricing rules, domain exceptions
├── ShopAPI.Infrastructure/      EF Core context, PostgreSQL services, migrations
│   ├── Services/               Cart, Order, Refund services
│   └── Migrations/             Four original migrations + new integrity migration
├── ShopAPI.Web/                 API host, controllers, DTOs, exception handling
│   ├── Contracts/              Request and response DTOs
│   ├── Controllers/            HTTP entry points
│   └── Infrastructure/         ProblemDetails handler, identity, demo seeder
├── ShopAPI.UnitTests/           Unit tests: pricing and cart invariants
├── ShopAPI.IntegrationTests/    PostgreSQL + HTTP integration tests
├── docs/                       Audit, screenshots policy
├── .github/workflows/ci.yml
├── compose.yaml                Local PostgreSQL only
└── Dockerfile                  Optional API container image
```

This keeps the original three-layer architecture and avoids an unnecessary generic repository or mediator framework. The source projects retain their historical `ShopAPI.*` namespace/project names so old EF Core migrations remain traceable; the published product name is **PADO ShopCore**.

### Technology

.NET 10, ASP.NET Core Web API, EF Core 10 + Npgsql, PostgreSQL 16, BCrypt.Net-Next, JWT Bearer Authentication, Swashbuckle OpenAPI, xUnit, GitHub Actions, Docker Compose.

## Local setup

Prerequisites: [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0), Docker Engine/Desktop with Compose, a shell and a free port `5432`.

**1. Prepare PostgreSQL**

```powershell
Copy-Item .env.example .env
# Edit .env: replace POSTGRES_PASSWORD with a long random password.
docker compose up -d
docker compose ps
```

**2. Configure development secrets (PowerShell)**

Do not commit connection strings, JWT keys, or admin passwords. Run these from the repository root. Replace `<local-db-password>` with the exact value you put in `.env`.

```powershell
dotnet user-secrets init --project ShopAPI.Web
$jwtKey = [Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
dotnet user-secrets set "Jwt:Key" "$jwtKey" --project ShopAPI.Web
dotnet user-secrets set "Jwt:Issuer" "PADO-ShopCore" --project ShopAPI.Web
dotnet user-secrets set "Jwt:Audience" "PADO-ShopCore-Clients" --project ShopAPI.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=shopcore;Username=shopcore;Password=<local-db-password>" --project ShopAPI.Web
```

**3. Restore, build, migrate and optionally seed demo catalog**

```powershell
dotnet restore ShopAPI.slnx
dotnet build ShopAPI.slnx -c Release
dotnet run --project ShopAPI.Web --launch-profile http -- --migrate
dotnet run --project ShopAPI.Web --launch-profile http -- --seed-demo
```

`--migrate` and `--seed-demo` run as **one-shot commands** and exit. Demo seed loads the original 10 categories / 50 example products only if the catalog is empty; it will never delete existing records. Do not run the seed command in non-Development environments.

**4. Create the first administrator (optional; one-time)**

Public registration **always creates a User**, even if JSON includes an unexpected `role` field. To create the only initial Admin, explicitly set temporary process-level variables and bootstrap once:

```powershell
$env:AdminBootstrap__Email = "admin@example.test"
$env:AdminBootstrap__Password = Read-Host "Enter a unique admin password (12-72 characters)"
dotnet run --project ShopAPI.Web --launch-profile http -- --bootstrap-admin
Remove-Item Env:AdminBootstrap__Email
Remove-Item Env:AdminBootstrap__Password
```

Bootstrap refuses a second Admin and refuses an email already used by any other account. Never expose the admin bootstrap variables to an API container as persistent environment configuration.

**5. Run the API**

```powershell
dotnet run --project ShopAPI.Web --launch-profile http
```

- Local API: `http://localhost:5000`
- OpenAPI / Swagger UI (Development only): `http://localhost:5000/swagger`
- Liveness: `GET http://localhost:5000/health`

Set `ASPNETCORE_ENVIRONMENT=Development` when not using the launch profile. In a deployed environment, supply signing credentials and DB connection via protected environment secrets and terminate TLS at the ingress, and override `AllowedHosts` with your real API hostnames (the checked-in default allows only localhost). Run migrations as a **separate, authorized deployment operation** rather than automatically on every startup.

## API examples

The examples use HTTP JSON bodies and header `Authorization: Bearer <JWT>` for authorized operations.

| Method | Endpoint | Access | Result |
|---|---|---|---|
| POST | `/api/auth/register` | Public | 201 user profile (no hash) |
| POST | `/api/auth/login` | Public | 200 JWT + expiry |
| GET | `/api/auth/me` | User/Admin | 200 own profile |
| GET | `/api/categories` | Public | 200 categories |
| POST | `/api/categories` | Admin | 201 category |
| GET | `/api/products?page=1&pageSize=20&categoryId=1` | Public | 200 active products |
| GET | `/api/products/{id}` | Public | 200 active product |
| POST / PUT | `/api/products` / `/api/products/{id}` | Admin | 201 / 200 |
| GET | `/api/carts` | User/Admin | 200 own cart |
| POST | `/api/carts/add-item` | User/Admin | 200 own cart |
| DELETE | `/api/carts/items/{productId}` | User/Admin | 204 |
| GET | `/api/orders` / `/api/orders/{id}` | User (own) / Admin (all) | 200 |
| POST | `/api/orders/checkout` | User/Admin | 201 order |
| POST | `/api/orders/{id}/cancel` | Owner/Admin | 204 (only Created) |
| PATCH | `/api/orders/{id}/status` | Admin | 204 |
| POST | `/api/discounts` | Admin | 201 |
| GET / POST | `/api/promos` | Admin | 200 / 201 |
| GET / POST | `/api/refunds` / `/api/refunds/request` | User (own) / Admin (all) | 200 / 201 |
| POST | `/api/refunds/{id}/approve` or `/reject` | Admin | 204 |
| GET | `/api/customers` | Admin | 200 sanitized users (max 100) |

**Register** (role is not accepted):

```http
POST /api/auth/register
Content-Type: application/json

{"firstName":"Alex","lastName":"Example","email":"alex@example.test","password":"A-Long-Unique-Password#2026"}
```

**Login:**

```http
POST /api/auth/login
Content-Type: application/json

{"email":"alex@example.test","password":"A-Long-Unique-Password#2026"}
```

**Cart and checkout:**

```http
POST /api/carts/add-item
Authorization: Bearer <JWT>
Content-Type: application/json

{"productId":1,"quantity":2}
```

```http
POST /api/orders/checkout
Authorization: Bearer <JWT>
Content-Type: application/json

{"promoCode":null}
```

**Example error** (RFC 7807; wording may vary):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "Insufficient stock for product 1."
}
```

A `409` during checkout can mean insufficient stock, exhausted promo inventory or a PostgreSQL serialization collision. Read the error and re-fetch current state before retrying; do **not** blindly retry ambiguous timed-out purchases.

## Tests and CI

**Unit tests (no database server):**

```powershell
dotnet test ShopAPI.UnitTests/ShopAPI.UnitTests.csproj -c Release
```

**Integration tests (real PostgreSQL):** Tests create and drop isolated PostgreSQL databases; the connection below must have `CREATEDB` privilege. This requires a *separate* test instance or a disposable local development server, not a production cluster.

Before testing locally, create `.env` from `.env.example`, set its `POSTGRES_PASSWORD`, and run `docker compose up -d` from the repository root. Verify the container using `docker compose ps`. The Compose container creates a `shopcore` PostgreSQL superuser that can create the temporary databases. Use the same local password in `TEST_POSTGRES_CONNECTION`; the **maintenance database must be `postgres`**, not `shopcore`. Never point the test connection at production PostgreSQL.

```powershell
$env:TEST_POSTGRES_CONNECTION = "Host=localhost;Port=5432;Database=postgres;Username=shopcore;Password=<local-db-password>"
dotnet test ShopAPI.IntegrationTests/ShopAPI.IntegrationTests.csproj -c Release
Remove-Item Env:TEST_POSTGRES_CONNECTION
```

```powershell
dotnet test ShopAPI.slnx -c Release
```

Integration checks cover: role elevation prevention, password hash exclusion, anonymous/forbidden endpoints, duplicate email, forged customer IDs and order ownership, invalid input, concurrent purchases of the last inventory unit, and duplicate refund/restock prevention. The GitHub Actions job uses a short-lived PostgreSQL service with **CI-only trust authentication** on the isolated runner and never reuses that setting in `compose.yaml`.

### Verification status

**Verified on GitHub Actions (18 October 2026):** .NET 10 restore, Release build, 8/8 unit tests, 9/9 real PostgreSQL integration tests, and API publish **all succeeded**. [View the successful CI run](https://github.com/PADOALEX/PADO-ShopCore/actions/runs/39117243394).

The PostgreSQL tests use disposable databases on a GitHub-hosted runner. Docker and PostgreSQL are **not required on your own computer** to obtain this CI verification. Local database startup and production deployment have not been separately tested; this repository remains a portfolio demonstration, not an audited production commerce service.

## Screenshots

The API has no graphical front end. Actual Swagger UI screenshots may be placed in [`docs/screenshots/`](docs/screenshots/README.md) after a verified local run. No invented screenshots are included.

## Known limitations

- No real payments, refunds-to-cards, shipping provider integration, taxes, multi-currency, or email delivery.
- No checkout idempotency key, refresh tokens, revocation list, MFA or distributed rate limit store.
- Price/current stock updates are admin-controlled; historical order items capture purchase unit prices. Promotions are not stackable except the one best personal discount **plus** one promo.
- Inventory decrements at checkout (there is no pre-checkout reservation timeout). Cancel only before Paid; returns only after Delivered.
- Customer self-service is limited to cart/order/refund workflows; product/category writes and returns decisions are Admin-only.
- Pagination is intentionally basic; no comprehensive filtering/search, order history audit trail or observability stack.
- Historical DB migrations may fail against inconsistent legacy data. Back up and run the preflight described in [SECURITY_AUDIT.md](docs/SECURITY_AUDIT.md) before upgrading an existing database.
- Connection strings are injected via configuration rather than embedded in source. This project has not undergone independent penetration testing.

## License

See [`LICENSE`](LICENSE). This is an educational portfolio project.
