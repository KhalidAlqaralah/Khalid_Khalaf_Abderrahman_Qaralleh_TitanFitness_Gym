# Titan Fitness — Staff Portal

A gym-chain management system: an **ASP.NET Core 10 Web API** built with Domain-Driven Design, Clean Architecture,
CQRS and EF Core (Code First, Fluent API), and an **Angular 22** single-page staff portal on top of it.

Front-desk staff check members in, manage members and freezes, schedule and book classes; branch managers also
maintain trainers and membership plans; members can book a class for themselves.

```
TitanFitness.slnx
├── TitanFitness.Domain/          entities, value objects, business rules (no NuGet packages)
├── TitanFitness.Application/     features: Commands / Queries / Contracts (MediatR + FluentValidation)
├── TitanFitness.Infrastructure/  EF Core DbContext, Fluent API configurations, generic repositories, seeder
├── TitanFitness.Api/             controllers, token auth, error mapping, Swagger
└── titan-fitness-web/            Angular 22 staff portal (standalone components, signals)
```

The step-by-step test plan is in **[TESTING.md](TESTING.md)**. To run every automated check at once (Windows):
`run-all-tests.cmd` — builds both apps, recreates the database, and runs the UI tests (`tests/e2e.mjs`) and API tests
(`tests/api-test.mjs`).

---

## Quick start

Prerequisites: .NET 10 SDK, SQL Server Express (or LocalDB), Node.js 24 (or 22.22.3+).

```powershell
# 1. API (creates the database and fills it with demo data on first run)
dotnet run --project TitanFitness.Api --launch-profile http
#    → http://localhost:5162/swagger

# 2. Frontend (second terminal)
cd titan-fitness-web
npm install
npm start
#    → http://localhost:4200
```

The connection string is in `TitanFitness.Api/appsettings.json` (`Server=localhost\SQLEXPRESS`). For LocalDB use
`Server=(localdb)\MSSQLLocalDB`.

### Accounts (`appsettings.json` → `Auth:Users`)

| User name | Password | Role | Can use |
|---|---|---|---|
| `frontdesk` | `Desk@123` | FrontDesk | Dashboard, Members, Classes |
| `manager` | `Manager@123` | Manager | Everything, including Trainers and Plans |
| `alex` | `Member@123` | Member (TF-1001) | Book Session — Member View only |

Sign-in returns a signed bearer token (HMAC-SHA256, 8 hours). The Angular interceptor sends it on every request;
in Swagger click **Authorize** and paste the token.

### Demo data

On an empty database the `DatabaseSeeder` creates, **relative to today's date**: 2 branches with 7 studios,
12 plans (2 retired), 12 trainers (2 inactive), 30 members covering every status (Active, Frozen, Expired,
Pending, Cancelled, no plan), check-ins for today and the same day last week, and classes for today and the
next 7 days (completed, in progress, nearly full, full with a waitlist, cancelled). Drop the database to re-seed.

---

## What changed after the backend review

| # | Review point | Where it is now |
|---|---|---|
| 1 | Fluent API relationships `HasOne().WithMany().HasForeignKey()` | `Infrastructure/Persistence/Configurations/*` — Trainer→Branch, Member→Branch, ClassSession→Branch/Trainer/Studio, Membership→Plan/Member, CheckIn→Branch/Member, Booking→Member, plus the aggregate children (Studios, Freezes, GuestPasses, Bookings) |
| 2 | Result pattern instead of exceptions; private ctor + static `Create` returning `Result<T>`; value objects with `Create` returning `Result`; studio-capacity check moved out of `ClassSession` | `Domain/Abstractions/Result.cs`, `Error.cs`; every entity and value object; capacity vs room in `Application/Features/ClassSessions/Shared/ClassSessionChecks.cs` |
| 3 | Only a generic read repository and a generic write repository | `Domain/Abstractions/Repositories.cs` (`IReadRepository<T>`, `IWriteRepository<T>`, `IUnitOfWork`), `Infrastructure/Persistence/Repositories/*` |
| 4 | No per-row subqueries in the dashboard and Get Members | `GetMembersQueryHandler` joins grouped derived tables; dashboard queries use joins and grouped tables. Verified: every list is one SQL statement (plus the paging COUNT) |
| 5 | Pagination + projection `Where → Select → OrderBy → Skip/Take → ToListAsync` | `GetPlansQueryHandler`, `GetTrainersQueryHandler`, `GetMembersQueryHandler`, `Common/Paging.cs` (`ToPagedResultAsync`) |
| 6 | Feature folders with Commands, Queries and Contracts (request + validator together) | `Application/Features/<Feature>/{Commands,Queries,Contracts}` |
| 7 | Request (API contract) separate from Command | `Contracts/*Request` → controller maps to `Commands/*Command` |
| 8 | Dashboard split into focused endpoints | `GET /api/dashboard/check-ins-today`, `/active-members`, `/upcoming-classes` |
| 9 | One `[FromQuery]` request object instead of many parameters | `GetMembersRequest`, `GetPlansRequest`, `GetTrainersRequest`, `GetClassScheduleRequest`, `DashboardRequest`… |

---

## Backend

### Layers

- **Domain** — aggregates `Branch` (owns `Studio`), `Member`, `Plan`, `Membership` (owns `Freeze`, `GuestPass`),
  `Trainer`, `ClassSession` (owns `Booking`), `CheckIn`. Value objects `Money`, `EmailAddress`, `MembershipNumber`,
  `DateRange`, `TimeSlot`, `OperatingHours`, `MembershipTerms`. Every factory and behaviour returns `Result` /
  `Result<T>`; nothing throws for a business rule. Constructors are private; entities are created only through
  `static Create(...)` (or a named factory such as `Membership.Purchase`, `ClassSession.Schedule`, `CheckIn.Record`).
- **Application** — one folder per feature. A handler loads aggregates through the generic repositories, calls the
  domain, and saves through the unit of work. Rules that need two aggregates live here: room capacity and double
  booking (`ClassSessionChecks`), membership eligibility for check-in and booking (`MembershipSelection`),
  unique plan names and trainer emails, the next `TF-NNNN` / `TR-NNNN` number.
- **Infrastructure** — `TitanFitnessDbContext`, one configuration per entity, value objects as EF **complex types**
  (plain columns, so queries can filter and aggregate on them) or owned types where a unique index is needed
  (`MembershipNumber`, trainer `Email`), `RowVersion` concurrency tokens, the seeder.
- **Api** — thin controllers: `[FromQuery]`/body request → command/query → `Result` → HTTP.

### Error mapping

| Situation | Status | Body |
|---|---|---|
| Request contract invalid (FluentValidation) | 400 | `ValidationProblemDetails`, `errors` keyed by field |
| Business rule failed (`ErrorType.Validation`) | 422 | ProblemDetails with `code`, and `errors[field]` when the rule is about one field |
| Not found | 404 | ProblemDetails |
| Conflict (duplicate, cancelled, frozen, overlap, concurrency) | 409 | ProblemDetails with the message to show |
| No / bad / expired token | 401 | — |
| Wrong role | 403 | — |

### Main endpoints

| Area | Endpoints |
|---|---|
| Auth | `POST /api/auth/login`, `GET /api/auth/me` |
| Branches | `GET /api/branches`, `POST`, `PUT /{id}`, `POST /{id}/studios`, `PUT /{id}/studios/{studioId}` |
| Dashboard | `GET /api/dashboard/check-ins-today`, `/active-members`, `/upcoming-classes` |
| Members | `GET /api/members` (paged), `GET /{id}`, `GET /{id}/current-membership`, `GET /{id}/activity`, `POST`, `PUT /{id}` |
| Memberships | `POST /api/memberships`, `GET /{id}`, `POST /{id}/renewals`, `POST /{id}/cancel`, `POST /{id}/freezes`, `POST /{id}/freezes/{freezeId}/end`, `POST /{id}/guest-passes`, `POST /{id}/guest-passes/{passId}/use` |
| Check-ins | `POST /api/check-ins` |
| Classes | `GET /api/class-sessions`, `GET /capacity-overview`, `GET /{id}`, `GET /{id}/bookings`, `POST`, `PUT /{id}`, `POST /{id}/cancel`, `POST /{id}/bookings`, `DELETE /{id}/bookings/{bookingId}`, `POST /{id}/bookings/{bookingId}/attendance` |
| Trainers | `GET /api/trainers` (paged), `GET /specialties`, `GET /lookup`, `GET /{id}`, `POST`, `PUT /{id}` |
| Plans | `GET /api/plans` (paged), `GET /filter-options`, `GET /lookup`, `GET /{id}`, `POST`, `PUT /{id}` |
| Member self-service | `GET /api/me/eligibility`, `GET /api/me/classes`, `GET /api/me/classes/{id}`, `POST /api/me/bookings` |

`TitanFitness.Api/TitanFitness.Api.http` has a ready request for each of them.

### Business rules worth knowing

- A member's **current membership** is the latest non-cancelled one that has started (otherwise the next one to
  start). Status: Active, Frozen (a freeze covers today), Expired, Pending, Cancelled, or none.
- **Freezes**: 1–3 months, start today or later and before the current end date, no overlap with another freeze,
  at most the plan's number of freezes. Each month counts as **30 days** against the plan's freeze-day allowance
  (so 60 days allows 2 months). The end date moves out by the calendar days frozen.
- **Classes**: 30/45/60 minutes, capacity 1–100 (default 20) and not above the room; trainer must be active, at
  that branch and free; room must be free. A full class puts new bookings on the waitlist; a cancelled booking
  or a larger capacity promotes the waitlist. The branch cannot change once the class has bookings.
- **Check-in**: membership must allow entry at that branch on that date (Home-branch-only plans), date not older
  than 7 days and date + time not in the future.
- **Plans**: editing a plan never changes memberships already sold — each membership keeps its own copy of the terms.

---

## Frontend (`titan-fitness-web`)

Angular 22, standalone components only, zoneless change detection, Bootstrap 5 + Bootstrap Icons, Angular Material
for dialogs, autocomplete, date and time pickers, selects and the price slider.

```
src/app/
├── core/          models, services (one per feature + auth, branch context, toasts), interceptors, guards
├── shared/        status badge, paginator, row menu, data table, confirm dialog, toasts,
│                  appAutofocus + appClickOutside directives, highlight / freezeAllowance / relativeDay pipes
├── layout/        shell: side menu, top bar (branch picker, search, calendar, user menu)
└── features/      auth, dashboard, check-in, members, classes, trainers, plans, member-portal, errors
```

| Requirement | Where |
|---|---|
| Signals (`signal`, `computed`, `effect`) | current branch (`BranchContextService`), list state of every directory, dashboard filtered classes (`computed`), Projected Impact New End Date (`computed`) |
| Routing | lazy `loadChildren` per feature, route params (`/members/:id` bound with `withComponentInputBinding`), query params for page/sort/search/filters, `''` → `dashboard`, `**` → not-found page, guards (`authGuard`, `roleGuard`, `unsavedChangesGuard`) |
| Directives | `@if`/`@for`/`@switch`, `[class]`/`[style]`, custom `appAutofocus` and `appClickOutside` |
| Pipes | `date`, `currency`, `titlecase`, `percent`, custom pure `highlight`, `freezeAllowance`, `relativeDay`, `label` |
| Parent / child | Trainer Directory → data table, status badge, filter dialog, paginator; Member Profile → identity card, plan card, usage cards, activity list |
| Services + HttpClient | `MemberService`, `CheckInService`, `ClassService`, `TrainerService`, `PlanService` (+ `DashboardService`, `MembershipService`, `SelfServiceService`) |
| Interceptor | `authInterceptor` (token) + `errorInterceptor` (0/400/401/403/404/409/422/5xx in one place), registered with `withInterceptors()` |
| `input()` / `output()` / `model()` | status badge `status`, paginator `total`/`page` → `pageChange`, filter dialogs → `filtersApplied`, row menu → `view`/`edit`, member picker `[(member)]` |

The API address is in `src/environments/environment.ts` (`http://localhost:5162/api`).
