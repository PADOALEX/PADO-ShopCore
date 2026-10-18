# Security and architecture audit

Source: `ShopProject(2).zip` received on 2026-10-18. The original contains four migrations, .NET 10 solution projects and legacy console/Web API code. Build/test artifacts were excluded from the new repository.

## Findings from the original code

| Severity | Finding | Evidence | Remediation in this repository |
|---|---|---|---|
| Critical | User could self-assign Admin | `AuthController.Register` persisted `request.Role` | Registration request has no Role; server always chooses User |
| Critical | Public customer listing and creation, EF serialization with password hash | `CustomersController` unprotected `GET` and `POST` | Admin-only list with `UserResponse`; public POST removed |
| Critical | Public product write | `ProductsController.AddProduct` lacked `Authorize` | Admin-only create/update |
| Critical | IDOR on cart | `CartsController.AddItem` trusted `CustomerId` from JSON | Identity always from validated token |
| Critical | DB password and JWT key committed | `ShopAPI.Web/appsettings.json`, `Program.cs` fallback | Removed, validated at startup; rotate old values |
| High | Overselling and promo overuse under concurrency | `OrderService.CheckoutAsync` read-then-write without isolation and stock guard | PostgreSQL serializable transaction + conditional updates; 409 on conflict |
| High | Refund overclaim and duplicate restock | `RefundsController` did not account for other claims; approve loaded mutable graph | Allocations counted; atomic state transition and stock increment in transaction |
| High | Dangerous seed | `SeedController` deleted existing orders/products | Removed network endpoint, added non-destructive explicit dev seed |
| High | Weak negative tests | Integration tests created Admin through public self-registration | Real PostgreSQL scenarios cover forbidden, ownership, conflicts and concurrency |
| Medium | Broad catch and string error responses | `CartsController`, `OrdersController` | Shared RFC 7807 ProblemDetails handler |
| Medium | Returning tracked EF graphs | `OrdersController`, `CategoriesController`, etc. | Response DTOs for all network output |
| Medium | Checkout stored `Price` but not `PriceAtPurchase` | `OrderService` | Write both legacy columns; migration backfill |
| Medium | Console client controlled HTTP server lifecycle | `Program.cs`, `ConsoleUI/ShopCli.cs` | Conventional Web API lifecycle; console menu retired |
| Medium | No DB uniqueness or stock checks | `ShopDbContext`, migrations | New additive migration and check constraints |

## Historical schema and upgrade preflight

The previous migrations are intentionally retained. The additive `SecurityAndIntegrity` migration creates uniqueness constraints for user email, cart per customer, cart line per product, promo code, and refund line. It adds a foreign key from orders to users, database CHECK constraints and a price backfill. Existing broken/duplicated records may prevent upgrade: *back up first*. Before migrating a populated database, inspect emails (case-insensitive duplicates), promo codes, carts per user, duplicate cart/refund items, orphan orders, nonpositive quantities, negative prices and overused promos. This migration intentionally fails rather than silently dropping or rewriting business data.

## Claims and nonclaims

- JWT authenticates requests; users' database role assignment is not modifiable over the API. One-off admin bootstrap is explicit and disabled after first Admin exists.
- PostgreSQL inventory/promo updates use serializable transactions and conditional DB-side updates. A concurrent transaction may correctly fail with HTTP 409 and should be retried by the caller.
- Refund approval returns stock only. This project neither charges cards nor sends money; `Paid` is an administrator-entered workflow state and **not** proof of external payment.
- There is no auth refresh-token or token revocation system. Issued tokens expire after one hour; deleting a user or changing roles will not revoke an already issued token before expiry. Rate limiting is per process/IP and is not shared across API replicas.
- Idempotency keys are not implemented; client retries after ambiguous network failures should first fetch their orders. Order operations aren't claimed to be externally exactly-once.
- The code is a portfolio sample, **not** an independently security-audited production commerce platform.

## Legacy migration warning (high priority)

The historical migration `20260506165426_AddAuthDiscountsRefunds` drops the old `Customers` table and replaces it with `User`. **This is not a lossless migration of pre-authentication customer records.** Do not apply the old migration to a populated legacy database without a bespoke plan to map or archive customer identities and re-establish correct foreign keys.

Before upgrading valuable legacy data: take a tested PostgreSQL backup, clone it into a disposable environment, review all historical migration operations, design explicit mapping for old customer IDs and associated carts/orders, execute the migration plan against the clone, and validate counts and ownership relationships. The project's automated integration tests deliberately use **fresh empty databases**, so they do not claim legacy upgrade safety.

## Verification status

GitHub Actions successfully completed .NET 10 Release build, **8/8 unit tests**, **12/12 PostgreSQL integration tests**, API publish, and **Docker image build** on 18 October 2026: [verified run](https://github.com/PADOALEX/PADO-ShopCore/actions/runs/39118258465). The expanded tests cover concurrent promotional checkouts and competing refund requests. The container was built, not deployed or runtime smoke-tested.

These tests do not establish independently audited production security, historical migration data preservation, or a deployed service. PostgreSQL and Docker do not need to be installed on a contributor's Windows PC when using GitHub Actions.
