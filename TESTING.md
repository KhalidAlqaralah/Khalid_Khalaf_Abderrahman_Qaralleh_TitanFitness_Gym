# Titan Fitness — Test Plan

**Fast path:** after Part 0.1–0.3, run `run-all-tests.cmd` from the repository folder. It builds both apps, recreates
the database, starts the API and the frontend, and runs 80 UI checks (`tests/e2e.mjs`, Playwright) and 205 API checks
(`tests/api-test.mjs`), then prints a summary. The parts below are the same checks by hand, plus the visual ones.

Work through the parts in order. Every step says **where to go**, **what to type**, and **what you should see**.
Tick the box when it passes. Anything that does not match is a bug: note the step number.

- Part 0 — Install the new code and a fresh database
- Part 1 — Backend checks (build, database, SQL, Swagger)
- Part 2 — API tests per review point (Swagger or the `.http` file)
- Part 3 — Frontend, screen by screen
- Part 4 — Error handling (every status code)
- Part 5 — Angular requirements checklist (where each one is in the code)
- Part 6 — Commit and push

> The demo data is created **relative to the day the database is created**. If you test on a later day, drop the
> database and start the API again to get "today's" classes and check-ins back (Part 0, step 0.4).

---

## Part 0 — Install

### 0.1 Replace the old code
Old files that no longer exist (for example the old `IReadQueries.cs` or the old migrations) would break the build,
so delete the old folders first.

1. Close Visual Studio and any terminal running the API or `ng serve`.
2. Open your repository folder (the one with `TitanFitness.slnx`).
3. Delete these folders: `TitanFitness.Api`, `TitanFitness.Application`, `TitanFitness.Domain`,
   `TitanFitness.Infrastructure`, `titan-fitness-web`.
4. Extract the zip **into the repository folder** so `TitanFitness.slnx`, `README.md`, `TESTING.md` and the five
   folders sit side by side. Choose "Replace" for `README.md`, `.gitignore` and `TitanFitness.slnx`.
- [ ] `git status` lists the changed and new files; there is no `bin`, `obj` or `node_modules` in the zip.

### 0.2 Tools
Open **PowerShell** in the repository folder.
```powershell
dotnet --version        # 10.x
node --version          # v24.x (or v22.22.3 or later)
dotnet tool install --global dotnet-ef    # only if "dotnet ef" is not found
```

### 0.3 Connection string
Open `TitanFitness.Api/appsettings.json`. It uses `Server=localhost\SQLEXPRESS;Database=TitanFitness`.
If you use LocalDB, change it to `Server=(localdb)\MSSQLLocalDB;Database=TitanFitness;Trusted_Connection=True;TrustServerCertificate=True`.

### 0.4 Drop the old database (required — the migrations were rebuilt)
```powershell
dotnet ef database drop --force --project TitanFitness.Infrastructure --startup-project TitanFitness.Api
```
(Or in SSMS: right-click **Databases → TitanFitness → Delete**, tick "Close existing connections".)
- [ ] The command says the database was dropped (or that it did not exist).

### 0.5 Start the API
```powershell
dotnet run --project TitanFitness.Api --launch-profile http
```
- [ ] The console shows `Now listening on: http://localhost:5162`.
- [ ] The console shows `Seeded demo data: 30 members, 12 plans, 12 trainers.`

Leave it running.

### 0.6 Start the frontend (second PowerShell window)
```powershell
cd titan-fitness-web
npm install
npm start
```
- [ ] `npm install` finishes without errors.
- [ ] The terminal shows `Local: http://localhost:4200/`.

### Seed data you will use

| ID | Member | Branch | Status | Plan | Why it is useful |
|---|---|---|---|---|---|
| TF-1001 | Alex Rivera | Downtown | Active | Annual Pro (60 days / 3 freezes) | 2/3 freezes, 50 of 60 freeze days used, 1/5 guest passes, class attendance in Recent Activity; also the member self-service account |
| TF-1002 | Jane Doe | Downtown | Active | Annual Pro | Clean — freeze 1 or 2 months |
| TF-1003 | John Smith | Uptown | **Frozen** | Family Annual | Check-in / booking / freeze must be refused |
| TF-1004 | Alice Williams | Downtown | Active | Quarterly (15 days) | Every freeze option disabled (a month counts 30 days) |
| TF-1005 | Robert Johnson | Downtown | **Expired** | Monthly | Refused; Renew button on the profile |
| TF-1006 | Marcus Vance | Downtown | Active | Annual Pro | Second clean freeze candidate |
| TF-1007 | Hannah Brooks | Uptown | **No plan** | — | "Sell a Plan" on the profile |
| TF-1008 | Leo Martins | Uptown | **Pending** | Monthly (starts in 5 days) | Not started yet |
| TF-1009 | Grace Chen | Downtown | **Cancelled** | — | Cancelled membership |
| TF-1010 | Samir Nasser | Uptown | Active | Monthly — **Home branch only** | Check-in at Downtown refused |
| TF-1022 | Henry Harris | Downtown | Active | Semi-Annual Plus (30 days) | Only "1 Month" enabled |

Trainers: Sarah Jenkins TR-1042 (HIIT / Strength, Downtown), Marcus Lee TR-1057, Elena Rodriguez TR-1063 (Cycling, Uptown),
Mike Turner TR-1071 (**Inactive**), … 12 in total. Plans: Annual Pro $899, Quarterly $279, Monthly $99,
Student 2024 $69 (**Retired**), … 12 in total.

---

## Part 1 — Backend checks

### 1.1 Build
```powershell
dotnet build TitanFitness.slnx
```
- [ ] `Build succeeded` with **0 warnings, 0 errors**.

### 1.2 Domain has no packages
Open `TitanFitness.Domain/TitanFitness.Domain.csproj`.
- [ ] There is no `<PackageReference>` in it.

### 1.3 Relationships exist as foreign keys (review point 1)
In SSMS, new query on the `TitanFitness` database:
```sql
SELECT OBJECT_NAME(fk.parent_object_id) AS [Table], c.name AS [Column], OBJECT_NAME(fk.referenced_object_id) AS [References],
       fk.delete_referential_action_desc AS [OnDelete]
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
ORDER BY 1, 2;
```
- [ ] You get **14** rows: `Trainers.BranchId → Branches`, `Members.HomeBranchId → Branches`,
  `ClassSessions.BranchId → Branches`, `ClassSessions.TrainerId → Trainers`, `ClassSessions.StudioId → Studios`,
  `Memberships.PlanId → Plans`, `Memberships.MemberId → Members`, `CheckIns.BranchId → Branches`,
  `CheckIns.MemberId → Members`, `Bookings.MemberId → Members`, `Bookings.SessionId → ClassSessions` (CASCADE),
  `Freezes.MembershipId → Memberships` (CASCADE), `GuestPasses.MembershipId → Memberships` (CASCADE),
  `Studios.BranchId → Branches` (CASCADE).

### 1.4 One SQL statement per list (review point 4 — no N+1)
1. Stop the API (Ctrl+C).
2. In `TitanFitness.Api/appsettings.json` change `"Microsoft.EntityFrameworkCore.Database.Command": "Warning"` to `"Information"`.
3. Start the API again (`dotnet run --project TitanFitness.Api --launch-profile http`).
4. In the browser (signed in, Part 3) open **Members**.
- [ ] The API console shows exactly **two** `Executed DbCommand` blocks for the list: `SELECT COUNT(*)` and the page
  `SELECT ... FROM [Members] AS [m] INNER JOIN [Branches] ... LEFT JOIN (SELECT ... GROUP BY ...) ... OFFSET ... FETCH NEXT`.
- [ ] There is no `SELECT` in the column list running once per member (no correlated subquery per row).
5. Open the **Dashboard**. Each of the three cards runs its own short query (the dashboard is split, point 8).
6. Put the setting back to `"Warning"` and restart the API.

### 1.5 Swagger
Open http://localhost:5162/swagger.
- [ ] Every controller is listed: Auth, Branches, CheckIns, ClassSessions, Dashboard, Me, Members, Memberships, Plans, Trainers.
- [ ] `GET /api/members` shows **one** set of query fields (Page, PageSize, Search, SortBy, SortDirection, BranchId, Statuses) — the `[FromQuery]` request object (point 9).

---

## Part 2 — API tests (review points)

Use Swagger (or open `TitanFitness.Api/TitanFitness.Api.http` in Visual Studio and click "Send request").

### 2.1 Sign in and authorize
1. Swagger → `POST /api/auth/login` → Try it out → body `{ "userName": "manager", "password": "Manager@123" }` → Execute.
- [ ] 200 with a `token`. **Copy the token.**
2. Click **Authorize** (top right) → paste the token → Authorize → Close.
3. `GET /api/auth/me` → Execute.
- [ ] 200, `"role": "Manager"`.
4. Log out in Swagger (Authorize → Logout) and run `GET /api/members`.
- [ ] **401**. Authorize again with the token.

### 2.2 Result pattern status codes (point 2)
| # | Request | Body / query | Expected |
|---|---|---|---|
| a | `POST /api/members` | `{ "fullName": "R2-D2", "homeBranchId": null }` | **400**, `errors.fullName` and `errors.homeBranchId` |
| b | `POST /api/plans` | `{ "name": "Annual Pro", "price": 10, "durationInMonths": 1, "isPublished": true }` | **409**, "A plan named 'Annual Pro' already exists.", `errors.name` |
| c | `GET /api/members/{id}` | id `00000000-0000-0000-0000-000000000001` | **404** |
| d | `POST /api/memberships` | `{ "memberId": "<Hannah's id>", "planId": "<Monthly id>", "startDate": "2020-01-01" }` | **422**, "A membership cannot start in the past.", `errors.startDate` |
| e | `POST /api/memberships` | same with today's date (yyyy-mm-dd) | **201** |
| f | same request again | | **409** "This member already holds a membership covering those dates." |

How to get the ids: `GET /api/members?search=Hannah` → copy `items[0].id`; `GET /api/plans?search=Monthly` → copy the id of "Monthly".
- [ ] All six rows match.

### 2.3 Domain factories (code review)
Open `TitanFitness.Domain/Trainers/Trainer.cs`, `ValueObjects/Money.cs`, `Sessions/ClassSession.cs`.
- [ ] Constructors are `private`; creation is `public static Result<T> Create(...)` (`Schedule`, `Purchase`, `Record` for sessions, memberships, check-ins).
- [ ] No `throw` for business rules anywhere in `TitanFitness.Domain` (search the folder for `throw` — the only ones left are inside `Result.cs`, guarding misuse of `Value` / `Error`).
- [ ] `ClassSession.Schedule` has **no** `studioCapacity` parameter; the room check is in `Application/Features/ClassSessions/Shared/ClassSessionChecks.cs`.

### 2.4 Generic repositories only (point 3)
- [ ] `TitanFitness.Domain/Abstractions/Repositories.cs` declares only `IReadRepository<T>`, `IWriteRepository<T>` and `IUnitOfWork`.
- [ ] `TitanFitness.Infrastructure/DependencyInjection.cs` registers them with `typeof(IReadRepository<>)` / `typeof(IWriteRepository<>)`; there is no `IMemberRepository` etc.

### 2.5 Paging + projection (point 5)
1. `GET /api/plans?page=1&pageSize=10&sortBy=price&sortDirection=Desc`
- [ ] `totalCount: 12`, `totalPages: 2`, 10 items, first item **Two-Year Elite**.
2. `GET /api/plans?page=2&pageSize=10&sortBy=price&sortDirection=Desc` → 2 items.
3. `GET /api/plans?durations=1&access=HomeBranchOnly&statuses=Published` → **Monthly, Senior Monthly, Weekend Warrior**.
4. `GET /api/plans?minPrice=300&maxPrice=100` → **400** "Max price cannot be below min price."
5. `GET /api/trainers?page=1&pageSize=5&sortBy=branch` → 5 items, `totalCount: 12`.
6. `GET /api/trainers?specialties=Cycling&statuses=Active` → **Elena Rodriguez, Nora Ali**.
7. `GET /api/members?pageSize=500` → **400** (page size limit 100). `GET /api/members?sortBy=shoe` → **400**.
- [ ] Code check: `GetPlansQueryHandler` goes `Where` → `Select` → `OrderBy` → `ToPagedResultAsync` (`Skip`/`Take` → `ToListAsync`).

### 2.6 Feature folders, Request vs Command (points 6, 7)
- [ ] `TitanFitness.Application/Features/Members/` contains `Commands/`, `Queries/`, `Contracts/`.
- [ ] `Contracts/MemberRequests.cs` holds `CreateMemberRequest` **and** its `CreateMemberRequestValidator`.
- [ ] `MembersController.Create` receives `CreateMemberRequest` and sends a `CreateMemberCommand`.

### 2.7 Split dashboard (point 8)
- [ ] `GET /api/dashboard/check-ins-today` → `{ today, sameDayLastWeek, changePercent }`.
- [ ] `GET /api/dashboard/active-members` → `{ activeMembers, onFloor, onFloorWindowMinutes: 120 }`.
- [ ] `GET /api/dashboard/upcoming-classes?take=5` → up to 5 of today's classes that have not finished.
- [ ] `GET /api/dashboard/upcoming-classes?take=0` → **400**.

### 2.8 Roles
1. Sign in as `frontdesk` / `Desk@123`, authorize with that token.
- [ ] `GET /api/trainers` → **403**; `GET /api/plans` → **403**; `GET /api/trainers/lookup` → **200**; `GET /api/plans/lookup` → **200**.
2. Sign in as `alex` / `Member@123`.
- [ ] `GET /api/members` → **403**; `GET /api/me/eligibility` → 200 with `"eligible": true`.

---

## Part 3 — Frontend

Open **http://localhost:4200**. Use Chrome. Keep DevTools (F12) → Console open: there must be **no red errors** during the whole part.

### 3.1 Sign in
1. Go to http://localhost:4200.
- [ ] You land on the **sign-in** page (default redirect → dashboard → guard → sign-in).
2. Type `manager` / `wrong` → **Sign in**.
- [ ] Red message "Wrong user name or password." under the form.
3. Leave both fields empty → Sign in.
- [ ] "User name is required." and "Password is required." under the fields.
4. Click the `frontdesk / Desk@123` demo line (fills the form) → **Sign in**.
- [ ] Dashboard opens. Side menu shows Dashboard, Members, Classes — **no Trainers, no Plans**. Bottom left: "Front Desk / Front desk".
5. In the address bar go to http://localhost:4200/trainers.
- [ ] **Access denied** page.
6. Click the avatar (top right) → **Sign out** → sign in as **manager**.
- [ ] Trainers and Plans appear in the side menu.
7. Go to http://localhost:4200/members/abc while signed out (sign out first), then sign in.
- [ ] After signing in you are sent back to `/members/abc` (and see "Member not found").

### 3.2 Header (any screen)
- [ ] Top bar shows **Downtown Branch** with a ▾. Click it → choose **Uptown Branch** → the dashboard numbers change. Refresh (F5) → still Uptown (remembered). Switch back to **Downtown**.
- [ ] The calendar icon opens **Classes**.
- [ ] The search box appears on Dashboard, Members, Member Profile, Classes, Trainers, Plans — and not on Trainer/Plan details.

### 3.3 Dashboard (Figure 1)
1. Click **Dashboard**.
- [ ] Title "Dashboard", subtitle "Live floor statistics and quick actions."
- [ ] **CHECK-INS TODAY**: a number and "+N% vs last week" / "−N% vs last week". **Write the number down.**
- [ ] **ACTIVE MEMBERS**: a number and "Currently on floor: N".
- [ ] **Upcoming Classes**: rows with start time (hh:mm AM/PM), class name, "Studio • Trainer J.", "N/M Enrolled" and a badge: **Active** (green, running now), **Upcoming** (grey outline), **Full** (red), **Cancelled** (red outline). No Completed classes. (Late in the evening only a few remain.)
2. Type `yoga` in the top search box.
- [ ] Only yoga classes stay; "yoga" is highlighted. Clear the box.
3. Click the **Upcoming** chip, then **All**.
- [ ] The list filters instantly (no reload).
4. **View Schedule** → Class Schedule opens. Go back.
5. **Quick Actions**: **New Member** opens the Add Member dialog (close it); **Manual Check-in** opens New Check-in (close it); **Register Class** opens Classes.

### 3.4 New Check-in dialog (section 3)
1. Side menu → **+ New Check-in**.
- [ ] The cursor is already in the member search (appAutofocus).
- [ ] Date = today, Time = now rounded **down** to 5 minutes, Branch = "Downtown Branch" (read-only), Notes empty with "0 / 250".
2. Click **Save Check-in** with no member.
- [ ] "Member is required."
3. Type `TF-1006` → pick **Marcus Vance**.
- [ ] Options show avatar initials, name, "#TF-1006 · Downtown" and an **Active** badge. The chosen member shows as a card with ✕.
4. Open the time picker (clock icon).
- [ ] Times go in **5-minute** steps.
5. Open the date picker.
- [ ] Days after today and more than 7 days ago are greyed out.
6. Type `11:55 PM` in the time (only if it is before 11:55 PM now) → click Notes.
- [ ] "The check-in date and time cannot be in the future." Put the time back to a past time.
7. Notes: type `Front door` → **Save Check-in**.
- [ ] Toast **"Marcus Vance checked in at hh:mm AM/PM"**, dialog closes.
- [ ] If you are on the Dashboard, CHECK-INS TODAY went up by **1** without refreshing.
8. **+ New Check-in** → type `John` → pick **John Smith** (Frozen).
- [ ] Red text "John Smith's membership is frozen — they can't check in." and Save does nothing.
9. ✕ on the member card → pick **Samir Nasser** (Uptown, home-branch-only) → Save.
- [ ] Red message "Samir Nasser cannot check in: Membership covers the home branch only." (409 from the API) plus the same toast.

### 3.5 Member Directory (Figure 2)
1. Side menu → **Members**.
- [ ] Title "Member Directory", columns MEMBER NAME, ID, STATUS, BRANCH, LAST VISIT, ACTIONS.
- [ ] Footer "Showing 1 to 10 of 30 entries" and pages 1 2 3.
- [ ] Last Visit shows "Today, 09:40 PM", "Yesterday" or "Oct 01, 2026" style dates, "—" when never.
2. Click page **2**.
- [ ] URL shows `?page=2`; 10 different members.
3. Click the **ID** header twice.
- [ ] Sorted by ID ascending, then descending; URL has `sortBy=number&dir=Desc`.
4. Top search: type `smith`.
- [ ] Back on page 1, only John Smith, "Smith" highlighted in yellow; URL has `q=smith`.
5. Search `frozen` → John Smith (search matches the status). Search `uptown` → only Uptown members. Search `tf-1004` → Alice Williams. Clear the search.
6. **Filter** → Status: tick **Expired** → **Apply Filters**.
- [ ] Only Expired members; the Filter button shows a badge "1"; URL has `status=Expired`.
7. Filter → choose Branch **Uptown**, keep Expired → Apply.
- [ ] Only Uptown + Expired. Filter → **Clear all** → Apply → all 30 again.
8. Row ⋮ on **John Smith** (Frozen).
- [ ] Menu: View Profile, Check-In, Book Class, Freeze Membership. Check-In, Book Class and Freeze are **greyed out**; hovering shows "Not available: membership is frozen."
9. Click anywhere outside the menu.
- [ ] The menu closes (appClickOutside).
10. Row ⋮ on **Jane Doe** → **Check-In**.
- [ ] New Check-in opens with Jane **pre-selected and locked** (no ✕), date today, time now.
- [ ] Save → toast "Jane Doe checked in at …"; Jane's Last Visit becomes "**Today, <time>**".
11. Row ⋮ on **Alice Williams** → Freeze Membership is enabled (1 freeze remains); Row ⋮ on **Robert Johnson** → disabled.
12. Go to page 2, filter Expired, then click a member name.
- [ ] The profile opens at `/members/<id>`; **Members** stays highlighted in the side menu.
13. Click the **←** back arrow.
- [ ] Back on the directory with the **same page and filter** (URL unchanged).

### 3.6 Add Member dialog (5.1.2)
1. **+ Add Member**.
- [ ] Fields: Member name (focused), Branch = Downtown Branch, Status = read-only **Active** badge.
2. Click **Add Member** with an empty name.
- [ ] "Member name is required."
3. Type `J` → "Member name must be 2–80 characters." Type `R2-D2` → "Use letters, spaces, hyphens and apostrophes only."
4. Type `  Nadia   O'Brien-Haddad  ` (extra spaces) → Branch **Uptown** → **Add Member**.
- [ ] Toast "Nadia O'Brien-Haddad added as #TF-1031" and her profile opens.
- [ ] Profile: "ID: #TF-1031", "Joined: <today>", Home branch Uptown, Current Plan "No current membership" + **Sell a Plan**.
5. **Sell a Plan** → Plan **Quarterly** → Start date today → **Sell Plan**.
- [ ] Toast "Quarterly sold to Nadia O'Brien-Haddad"; the plan card shows Quarterly, $279, "/3 months", Active badge.
- [ ] Members list now shows 31 entries.

### 3.7 Member Profile (Figure 3)
1. Members → search `alex` → click **Alex Rivera**.
- [ ] Left: photo placeholder "AR", **Active** badge, "Alex Rivera", "ID: #TF-1001", email, phone, address, home branch, "Joined: …".
- [ ] Current Plan: **Annual Pro**, $899 /year, "Renews: <date>", Active, **Freeze Membership** button.
- [ ] **Freezes Used 2 / 3** with bar, "1 freeze remaining". **Guest Passes 1 / 5**, "4 passes remaining".
- [ ] **Recent Activity**: up to 7 rows, newest first, mixing "Facility Check-in / Downtown Branch" and "Class Attendance / HIIT Bootcamp with Sarah", each with Today / date and time.
2. Press **F5**.
- [ ] Same member loads again (bookmarkable URL).
3. Change the last part of the URL to `00000000-0000-0000-0000-000000000001` → Enter.
- [ ] "**Member not found.**" with a Back button (no error toast).
4. Open Robert Johnson's profile (Expired).
- [ ] Freeze Membership is disabled (hover: "This membership has expired.") and a **Renew Monthly** button is shown. Click it → toast "Monthly renewed"; the card shows the new period.

### 3.8 Edit Member dialog (5.2.1)
1. On Alex's profile → **Edit Profile**.
- [ ] Pre-filled name and branch; read-only "Member ID #TF-1001" and the **Active** badge; **Save Changes** disabled until something changes.
2. Clear the name → Save → "Member name is required." Type `Alex Rivera-Stone`, Branch **Uptown** → **Save Changes**.
- [ ] Toast "Member updated"; the name and "Home branch: Uptown" change. (Change it back the same way.)

### 3.9 Freeze Membership (Figure 4)
1. Members → Jane Doe → ⋮ → **Freeze Membership**.
- [ ] URL `/members/<id>/freeze`; Members highlighted; "← Back to Member Profile", title, subtitle.
- [ ] Summary card: "JD", Jane Doe, "ID: TF-1002 • Annual Pro", **Active**, "Current End Date: <date>". **Write the date down.**
- [ ] Start Date = **tomorrow**; Duration **1 Month** selected; Reason "Select a reason"; Notes empty with "0 / 500"; required labels have a red *.
- [ ] Projected Impact: Original End Date = the date above, Freeze Duration "1 Month", **New End Date** in the blue box, the billing note.
- [ ] **Confirm Freeze** is disabled (reason missing).
2. Click **2 Months**.
- [ ] Navy border on 2 Months; Freeze Duration "2 Months"; New End Date moves out ~2 months **immediately** (computed()).
- [ ] **3 Months** is disabled; hover: "3 months uses 90 freeze days; only 60 remain."
3. Clear the Start Date.
- [ ] New End Date shows **"—"**.
4. Type yesterday's date → "The freeze must start today or later." Type a date after the Current End Date → "The freeze must start before the current end date." Type **today**.
5. Reason → **Extended Travel**; Notes `Summer trip`.
- [ ] Confirm Freeze becomes enabled.
6. **Confirm Freeze**.
- [ ] Toast "Membership frozen from … to …. New end date: …" and the profile opens: Freezes Used **1 / 3**, plan card status **Frozen**, Freeze button disabled ("already frozen").
7. Members → Jane's row badge is now **Frozen**.
8. Open the freeze screen for Jane again (address bar: add `/freeze` to her profile URL).
- [ ] Read-only screen: yellow note "This membership can't be frozen: This membership is already frozen." and everything disabled.
9. Alice Williams (Quarterly, 15 days) → Freeze Membership.
- [ ] All three duration buttons are disabled (each month counts as 30 days; only 15 remain).
10. Henry Harris (TF-1022, Semi-Annual Plus, 30 days) → Freeze.
- [ ] Only **1 Month** is enabled.
11. On Henry's freeze screen type something in Notes → click **← Back to Member Profile**.
- [ ] Dialog "**Discard changes?**" → **Keep editing** stays; **Discard** leaves.

### 3.10 Class Schedule (Figure 5)
1. Side menu → **Classes**.
- [ ] Title "Class Schedule", subtitle "Manage and monitor today's sessions."; toolbar: branch **Downtown**, today's date with ‹ ›, **Day**/Week, **Add Class**.
- [ ] Card "Today's Sessions" with a count badge "N Classes"; rows show the time block, class name, trainer, studio, state badge (Completed / In Progress / Upcoming / Full / Cancelled), "enrolled / capacity" with a bar, "Waitlist: N" when people wait.
- [ ] **HIIT Core Blast** (28/30) has a **red dot** (≥ 90 % full). (If it is late, use › to go to tomorrow and look for it at 06:00 AM: 3/30 — no dot.)
- [ ] Capacity Overview: Total Bookings and Avg Fill Rate.
2. Click **›**.
- [ ] Subtitle "Sessions for <tomorrow>", title "<Weekday, Mon d>", URL has `date=`.
3. Branch → **All Branches**.
- [ ] Each row also shows the branch (Downtown / Uptown).
4. **Week**.
- [ ] Rows grouped under day headings for 7 days, each row shows "06:00–06:45".
5. Top search: `zen` → only Zen Room classes. `elena` → Elena's classes. Clear it.
6. Hover a time block → tooltip with start–end. Go back to **Day**, branch **Downtown**, today (‹ until today).

### 3.11 Add New Class (Figure 6)
1. **Add Class**.
- [ ] Title "Add New Class"; Class Name focused (empty), Branch "Downtown Branch", Trainer "Select an instructor", Studio "Assign a room", Date empty "mm/dd/yyyy", Start Time empty, Capacity placeholder "20 spots", Duration **45 min** selected, Status **"● Active (Booking Open)"**, Description placeholder; footer Cancel · **Schedule Class**.
2. **Schedule Class** at once.
- [ ] "Class name is required.", "Date is required.", "Start time is required."
3. Class Name `AB` → "Class name must be 3–80 characters." → type `QA Bootcamp`.
4. Open the Trainer list.
- [ ] Only **active Downtown** trainers (no Mike Turner, no Elena).
5. Change Branch to **Uptown** → the Trainer and Studio lists change to Uptown ones → set Branch back to **Downtown**.
6. Date: **day after tomorrow**; Start Time `07:00 AM`; Studio **Zen Room (20 spots)**; Capacity `25` → click Description.
- [ ] "Not above the room's capacity (20)."
7. Capacity `2`; Trainer **Lina Saleh**; Duration **60 min** → **Schedule Class**.
- [ ] Toast "QA Bootcamp scheduled". Go to that date (date box) → the class is in the list.
8. **Add Class** again: `Clash Class`, same date, `07:30 AM`, Trainer **Lina Saleh** → Schedule.
- [ ] Under Trainer: "This trainer already teaches a class at an overlapping time." (409 mapped to the field).
9. Change Trainer to none, Studio **Zen Room** → Schedule.
- [ ] Under Studio: "This room is already booked for an overlapping slot."
10. Start Time `06:15 AM` (45 min → ends 07:00, exactly when QA Bootcamp starts) → Schedule → succeeds: back-to-back slots do not overlap.
11. Add Class with today's date and a time 1 hour **ago** → "The start time must be later than now."
12. Add Class → type a name → **Cancel** → "Discard changes?" → Discard.

### 3.12 Book Session (Figure 7)
1. On QA Bootcamp ⋮ → **Book Session**.
- [ ] Header: "QA Bootcamp", "<day>, 07:00 - 08:00", "Trainer: Lina Saleh", "Zen Room", badge "2 / 2 spots remaining".
2. Search `jane` → Jane is **Frozen** (from 3.9) → "Jane Doe can't book: membership is frozen." Change to **Marcus Vance** → Notes `Knee` → **Confirm Booking**.
- [ ] Toast "Marcus Vance booked on QA Bootcamp"; the row shows 1 / 2.
3. Book **Alex Rivera** → 2 / 2, badge **Full**.
4. Book **Liam Wilson**.
- [ ] The dialog warns "This class is full. Confirming adds the member to the waitlist (0 waiting)." Toast "Liam Wilson added to the waitlist for QA Bootcamp (#1)". Row shows **Waitlist: 1**.
5. Book **Alex Rivera** again → "This member already holds a place on this class."
6. Members → Alice Williams ⋮ → **Book Class**.
- [ ] Classes opens with a blue banner "Booking for Alice Williams (#TF-1004)…". ⋮ → Book Session on any upcoming class → Alice is pre-selected → Confirm → the banner disappears.

### 3.13 View / Edit Class (6.2.1)
1. Click the class name **QA Bootcamp**.
- [ ] Title "Class Details", all fields filled and **disabled**, "2 / 2 enrolled · Waitlist: 1" under Capacity, Status badge **Full**, footer Close · **Edit**.
2. **Edit**.
- [ ] Title "Edit Class", fields enabled, **Branch disabled** with "The branch can't change once the class has bookings.", footer Cancel · Save Changes.
3. Capacity `1` → click Description → "Not below the current enrolment (2)."
4. Capacity `3` → **Save Changes**.
- [ ] Toast "Class updated"; the row shows 3 / 3 and **no waitlist** (Liam was promoted).
5. Edit again → change the name → **Cancel** → "Discard changes?" → Discard → back to **View** mode with the saved values.
6. ⋮ → **Cancel Class** → "Cancel class?" → **Cancel Class**.
- [ ] Toast "QA Bootcamp cancelled"; row struck through with **Cancelled**; ⋮ → Edit Class and Book Session are disabled.
7. A **Completed** class today (e.g. Morning Strength) → ⋮ → Edit / Book / Cancel disabled.

### 3.14 Trainer Directory (Figure 9)
1. Side menu → **Trainers**.
- [ ] Title "Trainer Directory", subtitle "Manage and view all trainers.", **Filter** and **+ Add Trainer**.
- [ ] Columns TRAINER NAME (initials avatar), ID (#TR-1042), SPECIALTY, BRANCH, STATUS (Active green / Inactive grey), ACTIONS. "Showing 1 to 10 of 12 entries".
2. Sort by each sortable column (Name, ID, Specialty, Branch, Status) asc/desc.
3. Search `pil` → Lina Saleh, Mike Turner, "Pil" highlighted.
4. **Filter** → Branch **Uptown** (multi-select) → Specialty: type `cyc` in the box at the top of the list → tick **Cycling** → Status **Active** → **Apply Filters**.
- [ ] Only **Elena Rodriguez**; Filter badge "3"; URL keeps the filters.
5. Open Filter again → selections are still ticked → **Clear all** → Apply.
6. Go to page 2 → ⋮ on a trainer → **View Trainer** → **Back to list**.
- [ ] Back on page 2 with the same sort and filters.

### 3.15 Trainer Details — View / Update (Figures 10, 12)
1. Search `sarah` → click **Sarah Jenkins**.
- [ ] `/trainers/<id>`, title "Sarah Jenkins" + badge "**View mode**", "Trainer #TR-1042", **Edit Trainer** button; card "Trainer Details" with Trainer name · Specialty, Branch · Email, Phone · Status (IsActive ticked), all grey and not editable; no hint text.
2. **Edit Trainer**.
- [ ] Same page (no reload), URL becomes `/trainers/<id>/edit`, badge "**Update mode**", fields enabled, hint "Ticked means true, unticked means false.", Cancel · **Save Changes**.
3. Email `m.lee@titanfitness.com` → Save.
- [ ] Under Email: "A trainer with this email already exists (m.lee@titanfitness.com)." (409).
4. Email `s.jenkins@titanfitness.com`, Phone `abc` → "Enter a valid phone number…". Phone `+1 (555) 019-9999` → **Save Changes**.
- [ ] Toast **"Trainer updated"**, back to **View mode** with the new phone, URL `/trainers/<id>`.
5. Edit Trainer → change the name → **Cancel** → "Discard changes?" → Discard.
- [ ] Back in View mode (opened from View), old name.
6. Back to list → ⋮ on Marcus Lee → **Update Trainer**.
- [ ] Opens directly in Update mode at `/trainers/<id>/edit`. Change nothing → **Cancel** → back to the **directory**. Do it again, change a field, then click **Trainers** in the side menu → "Discard changes?" appears.
7. Address bar `/trainers/00000000-0000-0000-0000-000000000001` → "**Trainer not found.**"

### 3.16 Add Trainer (Figure 11)
1. Trainers → **+ Add Trainer**.
- [ ] `/trainers/new`, "Back to list", title "**New Trainer**" + "**Add mode**", "Add a trainer to the roster.", placeholders "e.g., Sarah Jenkins", "e.g., HIIT / Strength", "Select a branch", "name@titanfitness.com", "+1 (555) 000-0000"; **IsActive ticked**; Cancel · **Save Trainer**.
2. **Save Trainer** empty → "Trainer name is required.", "Branch is required.", "Email is required."
3. Name `Rami Aziz`, Specialty `Kettlebells`, Branch **Uptown**, Email `r.aziz@titanfitness` (no .com) → "Enter a valid email address." → `r.aziz@titanfitness.com` → **Save Trainer**.
- [ ] Toast "Rami Aziz added to the roster"; View mode with "Trainer #TR-1119".
4. Classes → Add Class → Branch Uptown → the instructor list contains **Rami Aziz**. Close.

### 3.17 Plan Catalogue (Figure 13)
1. Side menu → **Plans**.
- [ ] Title "Plan Catalogue"; columns PLAN NAME, PRICE ($899.00), DURATION (12 months), FREEZE ALLOWANCE ("60 days / 3 freezes", "15 days / 1 freeze", "None"), GUEST PASSES, ACCESS ("All branches" / "Home branch only"), STATUS (Published green / Retired red), ACTIONS; "Showing 1 to 10 of 12 entries".
2. Click **PRICE** twice → highest first (Two-Year Elite $1,599.00).
3. **Filter** → Duration chips (1 month, 3 months, 6 months, 12 months, 24 months) — click **1 month**; Access **Home branch only**; Status **Published** → Apply.
- [ ] Monthly, Senior Monthly, Weekend Warrior.
4. Filter → Clear all → Price Min `100`, Max `300` → Apply → only plans between $100 and $300. Min `300`, Max `100` → "Max price cannot be below min price." and Apply disabled.
5. Drag the slider thumbs → the Min/Max boxes follow.

### 3.18 Plan Details — View / Add / Update (Figures 14–16)
1. Click **Annual Pro**.
- [ ] "Annual Pro" + **View mode**; cards **Plan Details** (Plan name, Price 899, Duration 12, IsPublished ticked) and **Terms Offered** (60, 3, 5, "All branches"), all disabled; **Edit Plan**.
2. **Edit Plan** → URL `/plans/<id>/edit`, badge Update mode, the Access scope shows two cards with **All branches** highlighted.
3. Plan name `Monthly` → Save → "A plan named 'Monthly' already exists." → **Cancel** → "Discard changes?" → **Discard** → back to View mode with "Annual Pro".
4. **Back to list** → **+ Add Plan**.
- [ ] "New Plan" + Add mode, all empty with placeholders ("e.g., Annual Pro", "0.00", "e.g., 12", "0"), IsPublished unticked, neither access card selected.
5. Save empty → "Plan name is required.", "Price is required.", "Duration is required."
6. Price `-1` → "Price cannot be negative."; Price `10.123` → "Use at most 2 decimals."; Duration `40` → "Duration must be a whole number from 1 to 36."
7. Name `QA Flex`, Price `45.50`, Duration `2`, Maximum number of freezes `1` (freeze days empty) → Save.
- [ ] "Must be 0 when maximum freeze days is 0."
8. Maximum freeze days `10`, leave Guest pass quota empty, choose no access card, tick IsPublished → **Save Plan**.
- [ ] Toast "**QA Flex created**" and its View screen: Guest pass quota **0**, Access scope **Home branch only** (empty saved as defaults).
9. Edit Plan → Price `49` → **Save Changes** → toast "**Plan updated**", View mode.
10. Sold memberships keep their terms: Edit **Annual Pro** price to `999` → Save → open Alex Rivera's profile → still **$899**. (Set Annual Pro back to 899.)
11. `/plans/00000000-0000-0000-0000-000000000001` → "**Plan not found.**"

### 3.19 Member self-service (Figure 8)
1. Sign out → sign in as `alex` / `Member@123`.
- [ ] Member layout: "Hi, Alex Rivera", **Active Member** badge, Sign out. "Book a Class" with the next 7 days grouped by day.
2. Click **Book** on an upcoming class.
- [ ] "Class Booking", breadcrumb "Classes › Booking", class name, "<day>, h:mm AM - h:mm AM", TRAINER, STUDIO, **AVAILABILITY** "N spots left (x/y booked)" with a bar, **ELIGIBILITY STATUS** ✓ "Active Membership — Cleared for booking", notes box, Cancel · **Confirm Booking ✓**.
3. Notes `Shoulder` → **Confirm Booking**.
- [ ] Green "You're booked! See you at …". Open the same class again → "You already hold a place on this class (Confirmed)."
4. In the address bar open `/dashboard`.
- [ ] You are sent back to `/member/classes` (members can't open staff screens).
5. Sign out, sign in as **manager** → Classes → that class → ⋮ View → enrolment went up by one.

---

## Part 4 — Error handling (interceptor)

| # | How to cause it | Expected |
|---|---|---|
| 0 | Stop the API (Ctrl+C in its window), then click **Members** | Toast **"Can't reach the server. Check your connection."** with **Retry**; you stay on the page. Start the API again → click **Retry** → the list loads. |
| 400 | Freeze screen: the API also validates — Swagger `POST /api/memberships/{id}/freezes` with `{ "durationInMonths": 4 }` | 400 with `errors.startDate`, `errors.durationInMonths`, `errors.reason`. In the UI, 400 field errors show under the fields (Add Member with a digit in the name is caught in the browser first). |
| 401 | DevTools → Application → Local Storage → `http://localhost:4200` → key `tf.session` → edit the `"token"` value to `x` → go to Members page 2 | Toast "Your session has ended…", sign-in page with "Your session has ended…" note; after signing in you return to **Members page 2**. |
| 403 | Signed in as **frontdesk**, open `/plans` | Access denied page. (A 403 from the API on a page load also shows this page and the toast "You don't have permission to do this.") |
| 404 | `/members/00000000-0000-0000-0000-000000000001` | "Member not found." (no toast). For a non-page request, e.g. booking a class someone deleted: toast "The item no longer exists." |
| 409 | Trainer email duplicate (3.15 step 3), plan name duplicate (3.18 step 3), check-in of a home-branch-only member elsewhere (3.4 step 9) | The server message is shown. |
| 422 | Most 422 rules are also checked in the browser, so make the browser miss one: Classes → **Add Class** → name `Late Class`, **today**, Start Time = the next 5 minutes (e.g. now 14:02 → `02:05 PM`), then **wait until that time has passed** and click **Schedule Class** | The API answers 422 with `errors.startTime`; the interceptor leaves it to the form and "The start time must be later than now." appears **under Start Time**. Same mapping in Swagger: `POST /api/memberships` with a past `startDate` → `errors.startDate`. |
| 500 | Stop the **SQL Server** service (Windows: Services → SQL Server (SQLEXPRESS) → Stop) while the API runs, then open Trainers (the API may take up to 30 s to give up) | Toast **"Something went wrong. Please try again."** with **Retry**, and the list shows "Couldn't load trainers." with a Retry button. Start the service again → Retry works. |
| — | Address bar `/this-does-not-exist` | **Page not found** (404 fallback route). |
| — | Address bar `/` | Redirects to `/dashboard`. |

---

## Part 5 — Angular requirements checklist

Open the files and tick each one.

| # | Feature | Where to look |
|---|---|---|
| 1 | Angular 22, standalone, no NgModules | `package.json`; no `@NgModule` anywhere (`Ctrl+Shift+F` → `NgModule` → 0 results) |
| 2 | `signal()` / `computed()` / `effect()` | `core/services/branch-context.service.ts` (current branch + `effect` saving it); `features/members/directory/member-directory.ts` (page, sort, filters, search → URL + reload in `effect`); `features/dashboard/dashboard.ts` (`filteredClasses = computed`); `features/members/freeze/freeze-membership.ts` (`newEndDate = computed`) |
| 3 | Lazy routes, params, query params, redirect, not-found | `app.routes.ts` (`loadChildren`, `redirectTo: 'dashboard'`, `'**'`), `features/*/…routes.ts`, `id = input.required()` in profile / freeze / details, `queryParams` in every directory |
| 4 | `@if` `@for` `@switch`, `[class]` `[style]`, custom directives | `shared/components/status-badge/status-badge.ts` (`@switch`, `[class]`), `profile/usage-card.ts` (`[style.width.%]`), `shared/directives/autofocus.directive.ts`, `click-outside.directive.ts` |
| 5 | Pipes | `date`, `currency`, `titlecase` (side menu name), `percent`; `shared/pipes/highlight.pipe.ts`, `freeze-allowance.pipe.ts` |
| 6 | Parent / child | `features/trainers/trainer-directory.ts` uses `app-data-table`, `app-status-badge`, `app-paginator`, `TrainerFilterDialogComponent`; `features/members/profile/member-profile.html` uses `app-identity-card`, `app-plan-card`, `app-activity-list` |
| 7 | One service per feature with HttpClient | `core/services/member.service.ts`, `check-in.service.ts`, `class.service.ts`, `trainer.service.ts`, `plan.service.ts` (list with paging & filters, getById, create, update) |
| 8 | Functional interceptor | `core/interceptors/error.interceptor.ts`, `auth.interceptor.ts`, registered in `app.config.ts` with `withInterceptors([...])` |
| 9 | `input()` / `input.required()` / `output()` / `model()` | `status-badge.ts` (`input.required`), `paginator.ts` (`total`, `page`, `pageChange`), `trainer-filter-dialog.ts` (`filtersApplied`), `row-menu.ts` (`view`, `edit`), `features/shared/member-picker.ts` (`model()` → `[(member)]`) |

Build check (in `titan-fitness-web`):
```powershell
npx ng build
```
- [ ] "Application bundle generation complete." with no errors or warnings.

---

## Part 6 — Commit and push

From the repository folder:
```powershell
git checkout -b review-fixes-and-frontend
git add .
git status          # check: no bin/, obj/, node_modules/, .angular/ listed
git commit -m "Apply backend review fixes and add the Angular staff portal"
git push -u origin review-fixes-and-frontend
```
Open the pull request on GitHub and merge it into `main` when every box above is ticked.
