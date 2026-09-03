# Titan Fitness — Backend API

A gym chain management backend built with ASP.NET Core, following Domain-Driven Design with Clean Architecture, CQRS, and Entity Framework Core (Code First, Fluent API).

The system manages branches and studios, members and memberships, plans, freezes, guest passes, check-ins, class scheduling, bookings with a waiting list, and a staff dashboard.

---

## Table of contents

- [Architecture](#architecture)
- [Technology](#technology)
- [Getting started](#getting-started)
- [API reference](#api-reference)
- [Domain model](#domain-model)
- [Key design decisions](#key-design-decisions)
- [Known limitations](#known-limitations)

---

## Architecture

Four projects, with dependencies pointing inward only.

```
TitanFitness.Api             →  Controllers, exception handling, Swagger
        ↓
TitanFitness.Application     →  Commands, queries, handlers, validators
        ↓
TitanFitness.Domain          →  Entities, value objects, business rules
        ↑
TitanFitness.Infrastructure  →  DbContext, configurations, repositories
```

**`TitanFitness.Domain`** holds every business rule and has **zero NuGet package references**. It knows nothing about databases, HTTP, or any framework. Repository interfaces and `IUnitOfWork` are declared here and implemented in Infrastructure, which is Dependency Inversion in practice.

**`TitanFitness.Application`** contains one folder per use case, each holding a command or query, its validator, and its handler. Handlers load an aggregate, call one domain method, and save. Cross-aggregate rules that no single aggregate can enforce are checked here.

**`TitanFitness.Infrastructure`** contains the `DbContext`, eleven Fluent API configurations, repository implementations, the unit of work, and the read-side query service.

**`TitanFitness.Api`** contains thin controllers that translate HTTP into MediatR messages, plus a global exception handler that maps domain exceptions to status codes.

---

## Technology

| Concern | Choice |
|---|---|
| Framework | ASP.NET Core (.NET 10) |
| ORM | Entity Framework Core 10, Code First, Fluent API |
| Database | SQL Server Express |
| Mediation | MediatR |
| Validation | FluentValidation, via a MediatR pipeline behaviour |
| Documentation | Swagger / Swashbuckle |

---

## Getting started

### Prerequisites

- .NET 10 SDK
- SQL Server Express (or LocalDB)
- `dotnet-ef` tools: `dotnet tool install --global dotnet-ef`

### Configure the connection

`TitanFitness.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "TitanFitness": "Server=localhost\\SQLEXPRESS;Database=TitanFitness;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Replace the server name if yours differs. For LocalDB use `(localdb)\\MSSQLLocalDB`.

### Create the database

From the solution folder:

```bash
dotnet ef database update --project TitanFitness.Infrastructure --startup-project TitanFitness.Api
```

`--project` says where the migrations live; `--startup-project` says where the connection string and DI are configured.

### Run

```bash
dotnet run --project TitanFitness.Api
```

Then open the Swagger UI at `http://localhost:5162/swagger` (the port is printed in the console).

---

## API reference

Resources are nouns, verbs carry the intent, and status codes distinguish the kind of failure.

### Branches

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/branches` | Create a branch |
| `GET` | `/api/branches` | List all branches |
| `PUT` | `/api/branches/{id}` | Update name, address, opening hours |
| `POST` | `/api/branches/{id}/studios` | Add a studio to a branch |
| `PUT` | `/api/branches/{id}/studios/{studioId}` | Rename a studio or change its capacity |

### Plans

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/plans` | Create a plan |
| `GET` | `/api/plans?publishedOnly=` | List plans |
| `PUT` | `/api/plans/{id}` | Update terms, publish or retire |

### Trainers

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/trainers` | Create a trainer |
| `GET` | `/api/trainers?activeOnly=` | List trainers |
| `PUT` | `/api/trainers/{id}` | Update details, activate or deactivate |

### Members

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/members` | Create a member |
| `GET` | `/api/members?search=&branchId=&page=&pageSize=` | Paged directory with search and branch filter |
| `GET` | `/api/members/{id}` | Full profile with current plan, allowances and recent activity |
| `PUT` | `/api/members/{id}` | Update contact details, photo, home branch |

### Memberships

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/memberships` | Purchase against a plan |
| `GET` | `/api/memberships/{id}` | Detail including agreed terms, freezes and guest passes |
| `DELETE` | `/api/memberships/{id}` | Cancel (final) |
| `GET` | `/api/memberships/{id}/freezes/preview?startDate=&durationInMonths=` | Projected end date before committing |
| `POST` | `/api/memberships/{id}/freezes` | Apply a freeze |
| `POST` | `/api/memberships/{id}/freezes/{freezeId}/end` | End a freeze early |
| `POST` | `/api/memberships/{id}/guest-passes` | Issue a guest pass |
| `POST` | `/api/memberships/{id}/guest-passes/{passId}/use` | Record a guest visit |
| `GET` | `/api/memberships/{id}/plan-changes/preview?newPlanId=&timing=` | New terms and dates before committing |
| `POST` | `/api/memberships/{id}/plan-changes` | Switch plan (creates a new membership) |
| `POST` | `/api/memberships/{id}/renewals` | Renew (creates a new membership) |

### Check-ins

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/check-ins` | Record an entry attempt |

Returns **200** whether admitted or refused. A refusal is a valid recorded outcome, not a failed request, and the response body carries the reason.

### Class sessions

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/class-sessions` | Schedule a session |
| `GET` | `/api/class-sessions?branchId=&date=` | Day schedule with fill and waiting list |
| `GET` | `/api/class-sessions/{id}` | Session detail with all bookings |
| `DELETE` | `/api/class-sessions/{id}` | Cancel a session and every live booking on it |
| `POST` | `/api/class-sessions/{id}/bookings` | Book a member, or add to the waiting list |
| `DELETE` | `/api/class-sessions/{id}/bookings/{bookingId}` | Cancel a booking; returns the promoted booking id |
| `POST` | `/api/class-sessions/{id}/bookings/{bookingId}/attendance` | Mark attended or no show |

### Dashboard

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/dashboard?branchId=` | Active members, members inside, freezes, check-ins, sessions, bookings, fill rate |

### Status codes

| Code | Meaning |
|---|---|
| `200` | Read succeeded, or an action completed with a body |
| `201` | Resource created; `Location` header points to it |
| `204` | Action succeeded, nothing to return |
| `400` | Request was malformed or failed validation |
| `404` | A referenced resource does not exist |
| `409` | Request was well formed but a business rule refused it |
| `500` | Unexpected error |

The distinction between 400 and 409 is deliberate. A missing field is a client mistake; a freeze that exceeds the agreed allowance is a valid request the business declines.

---

## Domain model

Seven aggregate roots, each with its own repository. Child entities are reached only through their root and have no repository of their own.

| Aggregate root | Children | Enforces |
|---|---|---|
| `Branch` | `Studio` | Unique studio names within the branch |
| `Member` | — | Membership number format and normalisation |
| `Plan` | — | Valid terms; only published plans may be sold |
| `Membership` | `Freeze`, `GuestPass` | Freeze caps, guest pass quota, status, period extension |
| `Trainer` | — | Contact details, active state |
| `ClassSession` | `Booking` | Capacity, waiting list order, automatic promotion |
| `CheckIn` | — | Admitted and refused are mutually exclusive shapes |

### Value objects

`Money` · `MembershipNumber` · `MembershipTerms` · `DateRange` · `TimeSlot` · `OperatingHours`

All are immutable records with value equality. None has an identity, and none gets its own table — single-field value objects map through EF value converters, multi-field ones through `OwnsOne` into columns on the parent table.

---

## Key design decisions

### Agreed terms are a snapshot, not a lookup

The brief's central rule: when a plan changes, existing members keep the terms they were sold.

The obvious design — store `PlanId` on the membership and read `plan.MaxFreezeDays` when needed — fails silently. Nothing throws; the numbers simply become lies about what people bought.

The solution is one immutable value object, `MembershipTerms`, used in two places. `Plan` holds the terms it currently sells. `Membership` holds the terms it was sold, copied at purchase:

```csharp
Terms = plan.Terms;
```

No mapping code is needed because the object cannot be mutated. Editing a plan calls `ChangeTerms`, which swaps in a **new** instance; the old one, still referenced by every existing membership, is untouched. There is no code path by which a plan edit can reach a sold membership.

In the database the snapshot appears as `AgreedPrice`, `AgreedMaxFreezeDays` and so on — real columns on `Memberships`, entirely separate from `Plans`.

### Freeze and Booking are child entities, not aggregates

Rules like "total frozen days may not exceed the agreed cap" and "a session never takes more bookings than its capacity" cannot be checked from a single child — they need the whole collection in memory. So the collection lives inside the root, the backing list is private, the public view is read-only, and the child constructors are `internal`.

The result: no code path anywhere in the solution can add a freeze or a booking without passing through the root's checks.

The contrast is instructive. "A guest pass is used once" **is** enforced on `GuestPass` itself, because that rule needs nothing but the pass's own state. Same object, two rules, two different homes — decided each time by asking whether the object can answer on its own.

### Membership is its own aggregate root

"A member must never hold two memberships covering the same day" is an invariant spanning memberships, which argues for `Member` owning them. That was rejected: a five-year member would drag every historical membership, freeze and guest pass into memory just to check someone in at the door.

The rule is enforced in the purchase handler with a repository query instead — a deliberate trade of a slightly weaker guarantee on a rare operation for fast common operations.

### Cross-aggregate rules live in handlers

Trainer double-booking, studio double-booking, session capacity versus studio size, and a member booked onto overlapping sessions all span aggregates. None can be enforced by one, so they are checked in the command handlers using dedicated repository methods.

Where the rule still feels like domain logic, only the *data* comes from outside: `ClassSession.Schedule` takes `studioCapacity` as a parameter, so the comparison stays in the domain while the lookup happens in the handler.

### The domain never reads the clock

Every domain method that needs the current time takes it as a parameter. `DateTime.Now` appears only in handlers. This keeps the domain deterministic and testable — verifying "a freeze cannot begin in the past" needs no clock manipulation.

### Repositories return whole aggregates

`GetByIdAsync` on `Membership` includes freezes and guest passes; on `ClassSession` it includes bookings. An aggregate is only valid loaded whole — checking a freeze cap against a partially loaded membership would silently pass.

Reads are separate. Dashboard and list queries bypass repositories entirely and project straight into DTOs with `AsNoTracking()`, because loading full aggregates to display a count is wasted work. This is the read/write split that CQRS is for.

### Validation appears at three layers, deliberately

A maximum length is declared in the FluentValidation validator, in the EF configuration, and in the domain constructor. This is not duplication to be removed:

- The **validator** returns a clean 400 with a field name before any work happens.
- The **domain** guarantees the rule holds no matter how the object was constructed.
- The **database** is the last line of defence against anything that bypasses the application.

The `CheckIns` table carries a `CHECK` constraint enforcing that an admitted check-in has no refusal reason and a refused one does — the same rule the `Admit` and `Refuse` factory methods make unrepresentable in code.

### Guid v7 primary keys

Identity is generated in the domain, so an entity is fully valid the moment it is constructed and handlers can return the id before `SaveChanges` runs. Version 7 GUIDs are timestamp-prefixed, so they sort roughly in creation order and avoid the index fragmentation that random GUIDs cause.

---

## Known limitations

Documented rather than hidden. Each has a known fix.

**No optimistic concurrency.** Two staff booking the last seat at the same instant could both read `!IsFull` and both be confirmed. The fix is a `byte[] RowVersion` property with `.IsRowVersion()`, so the second save fails and retries. Not implemented because the project has no concurrent users.

**Membership status is stored and refreshed on read.** `Status` is a real column so the dashboard can filter in SQL, and `RefreshStatus` is the only thing that writes it. A membership that expired overnight still reads `Active` until something touches it. A production system would run a nightly job.

**Freezes extend the period at booking, not on completion.** Staff see the new end date immediately, which is what they expect, but `EndFreezeEarly` then has to claw days back. Extending on completion would need a background job.

**Freeze duration is in months while the cap is in days.** Both documents specify this. The consequence is that a one-month freeze cannot fit inside a one-month membership unless it starts on day one. Implemented as written rather than reinterpreted.

**Partial membership-number search does not work.** `MembershipNumber` maps through a value converter, so EF Core cannot translate `Contains` on it. Exact match works; `TF-000` returns nothing. The fix is mapping the raw string as a backing field and searching `EF.Property<string>`.

**"Members inside now" counts distinct members admitted today.** The entity model specified has no check-out, so nobody can be counted as leaving.

**Email, phone and address are guarded strings, not value objects.** Value objects were used where they protect a real rule. An `Address` wrapping one free-text field protects nothing. `Email` is the arguable one and would be the next to add.

**`internal` constructors do not restrict callers within the Domain assembly.** C# has no "only this one class" modifier. The restriction holds against every other layer, which is where it matters.

**Trainers are not scoped to a branch.** The screens document narrows the trainer list by branch, but the entity specification gives `Trainer` no branch. The entity specification was followed.

---

## Testing

The API was verified through a 120-step manual pass in Swagger covering every endpoint, every business rule, and the failure path for each — including duplicate membership numbers, overlapping memberships, all six freeze rules, guest pass reuse, every check-in refusal reason, trainer and studio double-booking, capacity limits, waiting list promotion, cancellation finality, and the agreed-terms snapshot.

Automated tests are not included. The domain is written to be unit-testable — no framework dependencies, no static clock, and every rule reachable through a public method — so a test project would be a natural next step.
