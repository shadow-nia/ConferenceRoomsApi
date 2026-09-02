# Conference Rooms API

REST API for managing conference halls, finding availability, creating bookings with time-based pricing, and reporting revenue. The solution is intentionally a small modular monolith: it covers the requested business flows without introducing microservices or infrastructure that the task does not need.

## Technology

- ASP.NET Core 10 Web API
- Entity Framework Core 10
- PostgreSQL 17
- Swagger / OpenAPI
- xUnit
- Docker Compose

## Run with Docker

Requirements: Docker with Compose support.

```bash
docker compose up --build
```

The API applies migrations and inserts initial data during startup.

- Swagger UI: <http://localhost:8080/swagger>
- Health check: <http://localhost:8080/health>

To stop the application:

```bash
docker compose down
```

Use `docker compose down -v` only when the local database volume should also be deleted.

## Run locally

Requirements: .NET 10 SDK and PostgreSQL.

```bash
docker compose up -d database
dotnet restore
dotnet run --project ConferenceRooms.Api
```

Swagger is then available at <http://localhost:5080/swagger>. The development connection string is in `ConferenceRooms.Api/appsettings.json` and can be overridden with the `ConnectionStrings__Database` environment variable.

## Tests

```bash
dotnet test
```

The unit tests focus on the risky domain rules: tariff boundaries, bookings spanning several tariff periods, partial hours, invalid working hours, and every important interval-overlap case.

## API

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/halls` | List active halls |
| `GET` | `/api/halls/{id}` | Get one hall |
| `POST` | `/api/halls` | Create a hall with services |
| `PATCH` | `/api/halls/{id}` | Update hall data or replace its service list |
| `DELETE` | `/api/halls/{id}` | Soft-delete a hall |
| `GET` | `/api/halls/available` | Find a free hall by time and capacity |
| `POST` | `/api/bookings` | Book a hall and calculate its cost |
| `GET` | `/api/bookings/{id}` | Read a booking and its price snapshot |
| `GET` | `/api/reports/revenue` | Revenue and service-popularity report |

Swagger contains request schemas, response schemas, and status codes. `ConferenceRooms.Api/ConferenceRooms.Api.http` contains ready-to-edit example requests.

## Pricing rules

All money values are in UAH. Additional services are fixed-price items charged once per booking. Room price is proportional to minutes and split when a booking crosses a tariff boundary.

| Local time | Multiplier |
|---|---:|
| 06:00–09:00 | 0.90 |
| 09:00–12:00 | 1.00 |
| 12:00–14:00 | 1.15 |
| 14:00–18:00 | 1.00 |
| 18:00–23:00 | 0.80 |

For example, an 08:00–13:00 booking is calculated as one morning hour, three standard hours, and one peak hour. Segment amounts use decimal arithmetic and are rounded to two digits away from zero.

## Initial data

- Зал А: 50 people, 2000 UAH/hour
- Зал B: 100 people, 3500 UAH/hour
- Зал C: 30 people, 1500 UAH/hour
- Each seeded hall offers Проєктор (500 UAH), Wi-Fi (300 UAH), and Звук (700 UAH)

The seeder is idempotent and runs only when there are no halls.

## Business and technical decisions

- Requests use ISO 8601 timestamps with an explicit UTC offset. Start and end must have the same offset.
- A booking must fit into one local calendar day and the defined 06:00–23:00 working window. The task defines no night tariff, so night bookings are rejected instead of silently applying an invented price.
- Time intervals are half-open: `[start, end)`. A 10:00–12:00 booking does not conflict with a booking starting at 12:00.
- Availability is checked in the application for a useful error. A PostgreSQL exclusion constraint also prevents overlapping active bookings during concurrent requests.
- Hall deletion is soft. Historical bookings and reports remain valid.
- A booking stores the hall rate and selected service names/prices as snapshots. Later hall edits never alter historical totals.
- Updating `services` replaces the whole current list. Pass an existing service ID to update or preserve it, omit the ID to add a service, and omit an old service from the array to remove it.
- The revenue report includes active bookings whose start time falls in `[from, to)`.

## Validation and security

- DTO and business-rule validation
- parameterized database access through EF Core
- database constraints for uniqueness, valid intervals, and concurrent booking conflicts
- standardized RFC 7807 error responses with trace IDs
- no stack traces or database details in client responses
- global rate limit of 100 requests per minute per IP
- secrets and connection strings can be supplied through environment variables
- no permissive CORS policy is enabled

Authentication and customer accounts were not included because the supplied API requirements define neither users nor roles. In a production version, administrative hall operations would require an administrator policy and booking operations would be tied to an authenticated customer.

## Project structure

```text
ConferenceRooms.Api/
├── Contracts/     HTTP request and response models
├── Controllers/   REST endpoints
├── Data/          EF Core context, migration, and seed data
├── Domain/        entities and pricing/interval rules
├── Exceptions/    expected business exceptions
├── Middleware/    consistent API error handling
└── Services/      application use cases

ConferenceRooms.Tests/
└── Domain/        unit tests for pricing and overlap rules
```

This separation keeps controllers thin, the pricing logic independently testable, and persistence details isolated without adding redundant repository abstractions over EF Core.
