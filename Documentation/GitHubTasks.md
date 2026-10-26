# GitHub Backlog: Hotel Reservation System

This document classifies the work in `SoftwareSpec.md` as GitHub parent issues (epics) and child issues (tasks). It is intentionally structured so a later script can parse the issue keys, titles, labels, dependencies, and acceptance criteria.

## Issue conventions

- **Epic**: parent GitHub issue grouping one capability or cross-cutting concern.
- **Task**: implementation or validation issue belonging to an epic.
- **Labels**: suggested GitHub labels; the upload script may create or map them.
- **Dependencies**: issue keys that should be completed first.
- Each task should be independently actionable and should include tests where applicable.

## Architecture baseline: MAUI MVVM + ASP.NET Core API

The application is split into:

- **.NET MAUI Client** (MVVM presentation and interaction)
- **ASP.NET Core Web API** (application services and backend boundary)
- **SQLite data file with Docker named volume persistence**

MVVM remains the client pattern:

- **Models** represent domain and UI state in the MAUI client.
- **Views** are XAML pages/components.
- **ViewModels** expose bindable state and commands, and call service abstractions.
- ViewModels must not contain direct persistence or HTTP infrastructure implementation details.

Backend responsibilities:

- API endpoints, validation, business rules, and data-access implementation.
- Containerized runtime and environment-specific configuration.

Shared responsibilities:

- **Shared.Contracts** class library for API DTOs and contract types used by both client and backend.

Recommended feature organization:

```text
Client (MAUI)
  Features/
    UserManagement/{Models,Views,ViewModels,Services}
    HotelRooms/{Models,Views,ViewModels,Services}
    Search/{Models,Views,ViewModels,Services}
    Booking/{Models,Views,ViewModels,Services}
    Payments/{Models,Views,ViewModels,Services}
    Notifications/{Models,Views,ViewModels,Services}
  Core/
    Services/Navigation/Validation/Security/Localization

Backend (ASP.NET Core API)
  Features/
    UserManagement
    HotelRooms
    Search
    Booking
    Payments
    Notifications
  Infrastructure/
    Data
    Repositories

Shared/
  Contracts
```

---

## EPIC-00 — Establish client/server split and container platform

**Labels:** `epic`, `architecture`, `backend`, `docker`, `priority:high`

**Goal:** Bootstrap a MAUI client + ASP.NET Core API architecture, containerized backend runtime, and shared contracts.

### Tasks

- **TASK-00.01 — Add ASP.NET Core API and Shared.Contracts projects to the solution**
  - **Labels:** `task`, `architecture`, `backend`
  - **Dependencies:** none
  - **Acceptance criteria:** Solution contains MAUI client, API backend, and shared contract library projects with successful restore/build.

- **TASK-00.02 — Define client/backend boundaries and service abstractions**
  - **Labels:** `task`, `architecture`, `mvvm`
  - **Dependencies:** TASK-00.01
  - **Acceptance criteria:** MAUI ViewModels depend on interfaces and contract DTOs; direct persistence access is removed from client-side feature logic.

- **TASK-00.03 — Add API containerization and Docker Compose orchestration**
  - **Labels:** `task`, `docker`, `operations`
  - **Dependencies:** TASK-00.01
  - **Acceptance criteria:** API has Dockerfile/.dockerignore and starts through docker compose with environment configuration.

- **TASK-00.04 — Configure SQLite persistence with named volume in Compose**
  - **Labels:** `task`, `data`, `docker`, `sqlite`
  - **Dependencies:** TASK-00.03
  - **Acceptance criteria:** SQLite file persists across container restarts via named volume and API can read/write successfully.

- **TASK-00.05 — Deliver backend skeleton endpoints without auth (phase 1)**
  - **Labels:** `task`, `backend`, `api`
  - **Dependencies:** TASK-00.02, TASK-00.04
  - **Acceptance criteria:** Health endpoint and at least one sample feature endpoint are available; no authentication is required in this initial milestone.

- **TASK-00.06 — Add local setup documentation for MAUI + API + Docker workflow**
  - **Labels:** `task`, `documentation`, `dev-experience`
  - **Dependencies:** TASK-00.03, TASK-00.04
  - **Acceptance criteria:** Documentation covers prerequisites, compose startup, MAUI client configuration to API base URL, and troubleshooting basics.

## EPIC-01 — Establish the MVVM application foundation

**Labels:** `epic`, `architecture`, `mvvm`, `priority:high`

**Goal:** Create the shared architecture and delivery foundation needed by every feature.

### Tasks

- **TASK-01.01 — Define the MVVM solution and feature folder structure**
  - **Labels:** `task`, `architecture`, `mvvm`
  - **Dependencies:** none
  - **Acceptance criteria:** The project has consistent Models, Views, ViewModels, and service boundaries; feature code does not require direct data access from XAML code-behind.

- **TASK-01.02 — Configure dependency injection and service abstractions**
  - **Labels:** `task`, `architecture`, `mvvm`
  - **Dependencies:** TASK-01.01
  - **Acceptance criteria:** Services are registered centrally; ViewModels receive dependencies through constructors; external services can be replaced by test doubles.

- **TASK-01.03 — Implement shared navigation and shell routes**
  - **Labels:** `task`, `ui`, `mvvm`
  - **Dependencies:** TASK-01.02
  - **Acceptance criteria:** Authenticated and unauthenticated navigation flows are defined; navigation is invoked through commands or a navigation abstraction rather than feature-specific page code-behind.

- **TASK-01.04 — Define shared result, loading, error, and validation-state patterns**
  - **Labels:** `task`, `reliability`, `mvvm`
  - **Dependencies:** TASK-01.02
  - **Acceptance criteria:** ViewModels expose consistent busy, error, empty, and success states; users receive meaningful errors without exposing sensitive details.

- **TASK-01.05 — Set up configuration and environment management**
  - **Labels:** `task`, `configuration`, `security`
  - **Dependencies:** TASK-01.02
  - **Acceptance criteria:** Development, test, and production settings are separated; secrets are not committed to source control.

## EPIC-02 — Implement identity, profiles, and role-based access

**Labels:** `epic`, `user-management`, `security`, `priority:high`

**Goal:** Support registration, authentication, profile management, password recovery, and Customer/Hotel Staff/Administrator roles.

### Tasks

- **TASK-02.01 — Create user, role, permission, and session models**
  - **Labels:** `task`, `user-management`, `model`
  - **Dependencies:** TASK-01.01
  - **Acceptance criteria:** Models represent required user data and roles without storing plaintext passwords or payment information.

- **TASK-02.02 — Build registration View and ViewModel**
  - **Labels:** `task`, `ui`, `mvvm`
  - **Dependencies:** TASK-02.01, TASK-01.03
  - **Acceptance criteria:** Users can register with an email address; client-side validation displays actionable errors; successful registration has a defined next state.

- **TASK-02.03 — Build login and logout ViewModels and Views**
  - **Labels:** `task`, `ui`, `security`, `mvvm`
  - **Dependencies:** TASK-02.01, TASK-01.03
  - **Acceptance criteria:** Users can sign in and sign out; authentication failures are handled safely; the login flow targets a response time below two seconds under normal conditions.

- **TASK-02.04 — Implement token-based session management and timeout handling**
  - **Labels:** `task`, `security`
  - **Dependencies:** TASK-02.03
  - **Acceptance criteria:** Tokens are stored securely, sessions expire, expired sessions return to authentication, and session-hijacking protections are documented.

- **TASK-02.05 — Build profile management View and ViewModel**
  - **Labels:** `task`, `ui`, `mvvm`
  - **Dependencies:** TASK-02.03
  - **Acceptance criteria:** Authenticated users can view and update their own information; unauthorized profile access is rejected.

- **TASK-02.06 — Implement password recovery and secure password handling**
  - **Labels:** `task`, `security`
  - **Dependencies:** TASK-02.03
  - **Acceptance criteria:** Password recovery uses a time-limited secure flow; passwords are hashed/encrypted according to the selected identity provider; recovery events are logged without secrets.

- **TASK-02.07 — Implement role-based authorization for customer, staff, and administrator features**
  - **Labels:** `task`, `security`, `authorization`
  - **Dependencies:** TASK-02.04
  - **Acceptance criteria:** Access is enforced at service/API boundaries as well as in the UI; hidden UI elements are not treated as the sole security control.

## EPIC-03 — Implement hotel and room management

**Labels:** `epic`, `hotel-management`, `rooms`, `priority:high`

**Goal:** Manage hotel and room data and expose current availability to authorized users.

### Tasks

- **TASK-03.01 — Create hotel, room, amenity, and availability models**
  - **Labels:** `task`, `model`
  - **Dependencies:** TASK-01.01
  - **Acceptance criteria:** Models support room details, hotel details, pricing, capacity, status, and availability periods.

- **TASK-03.02 — Implement hotel and room data access services**
  - **Labels:** `task`, `data`, `service`
  - **Dependencies:** TASK-03.01, TASK-01.02
  - **Acceptance criteria:** Data access is behind interfaces; CRUD operations are covered by error handling and do not leak persistence details into ViewModels.

- **TASK-03.03 — Build customer room-browsing Views and ViewModels**
  - **Labels:** `task`, `ui`, `mvvm`
  - **Dependencies:** TASK-03.02, TASK-01.03
  - **Acceptance criteria:** Customers can view hotel and room information, pricing, capacity, and availability.

- **TASK-03.04 — Build staff hotel and room CRUD Views and ViewModels**
  - **Labels:** `task`, `ui`, `mvvm`, `authorization`
  - **Dependencies:** TASK-03.02, TASK-02.07
  - **Acceptance criteria:** Authorized staff can add, edit, and delete rooms; invalid data is rejected; customers cannot access staff CRUD actions.

- **TASK-03.05 — Implement availability updates and consistency rules**
  - **Labels:** `task`, `booking`, `data-integrity`
  - **Dependencies:** TASK-03.02
  - **Acceptance criteria:** Availability reflects reservations and changes safely under concurrent requests; stale availability is not used to confirm a booking.

## EPIC-04 — Implement room search and recommendations

**Labels:** `epic`, `search`, `priority:high`

**Goal:** Let users search, filter, sort, and receive recommendations for available rooms.

### Tasks

- **TASK-04.01 — Define search criteria, filter, sort, and result models**
  - **Labels:** `task`, `model`
  - **Dependencies:** TASK-03.01
  - **Acceptance criteria:** Search supports destination, dates, guest count, price, location, and star rating.

- **TASK-04.02 — Implement room search service and availability query**
  - **Labels:** `task`, `service`, `performance`
  - **Dependencies:** TASK-04.01, TASK-03.05
  - **Acceptance criteria:** Only rooms available for the requested dates are returned; the normal search target is below two seconds and the broader requirement is below three seconds.

- **TASK-04.03 — Build search View and ViewModel**
  - **Labels:** `task`, `ui`, `mvvm`
  - **Dependencies:** TASK-04.02, TASK-01.03
  - **Acceptance criteria:** Users can submit criteria, see loading/empty/error states, filter and sort results, and open room details.

- **TASK-04.04 — Add recommendation strategy and fallback behavior**
  - **Labels:** `task`, `search`
  - **Dependencies:** TASK-04.02
  - **Acceptance criteria:** Recommendations are explainable from available room criteria; lack of recommendation data does not prevent normal search.

## EPIC-05 — Implement booking and reservation management

**Labels:** `epic`, `booking`, `reservations`, `priority:critical`

**Goal:** Create, modify, cancel, and manage reservations without duplicate or overlapping bookings.

### Tasks

- **TASK-05.01 — Create booking, guest, status, and cancellation-rule models**
  - **Labels:** `task`, `model`
  - **Dependencies:** TASK-03.01, TASK-04.01
  - **Acceptance criteria:** Models represent booking numbers, dates, customer information, payment state, reservation status, and cancellation rules.

- **TASK-05.02 — Implement booking validation and overlap prevention**
  - **Labels:** `task`, `business-logic`, `data-integrity`
  - **Dependencies:** TASK-05.01, TASK-03.05
  - **Acceptance criteria:** Invalid dates, unavailable rooms, duplicate requests, and overlapping reservations are rejected atomically.

- **TASK-05.03 — Build customer booking View and ViewModel**
  - **Labels:** `task`, `ui`, `mvvm`
  - **Dependencies:** TASK-05.02, TASK-04.03
  - **Acceptance criteria:** Customers can submit a reservation, receive a booking number and status, and see meaningful validation or failure messages.

- **TASK-05.04 — Implement reservation modification and customer cancellation**
  - **Labels:** `task`, `booking`, `ui`
  - **Dependencies:** TASK-05.03
  - **Acceptance criteria:** Cancellation rules are enforced; eligible customers can modify or cancel their own bookings; availability is released correctly.

- **TASK-05.05 — Build staff reservation management Views and ViewModels**
  - **Labels:** `task`, `ui`, `mvvm`, `authorization`
  - **Dependencies:** TASK-05.03, TASK-02.07
  - **Acceptance criteria:** Authorized staff can list, view, create, edit, and remove reservations according to business rules.

- **TASK-05.06 — Add booking transaction idempotency and concurrency tests**
  - **Labels:** `task`, `testing`, `data-integrity`
  - **Dependencies:** TASK-05.02
  - **Acceptance criteria:** Repeated requests do not create duplicate bookings; concurrent booking attempts leave at most one valid reservation for a room/time period.

## EPIC-06 — Integrate payment processing

**Labels:** `epic`, `payments`, `security`, `priority:critical`

**Goal:** Integrate a trusted third-party payment provider while keeping payment data outside the application's database. The first version may use a mock provider.

### Tasks

- **TASK-06.01 — Define payment provider abstraction and payment models**
  - **Labels:** `task`, `architecture`, `payments`
  - **Dependencies:** TASK-05.01, TASK-01.02
  - **Acceptance criteria:** The abstraction supports pending, successful, and failed states without accepting or persisting raw card details.

- **TASK-06.02 — Implement mock payment provider for the first release**
  - **Labels:** `task`, `payments`, `testing`
  - **Dependencies:** TASK-06.01
  - **Acceptance criteria:** Test scenarios can simulate success, failure, pending status, timeout, and provider errors.

- **TASK-06.03 — Implement secure external payment adapter boundary**
  - **Labels:** `task`, `payments`, `security`
  - **Dependencies:** TASK-06.01
  - **Acceptance criteria:** Provider communication uses HTTPS; provider credentials are configuration secrets; the application database stores only provider references and payment status.

- **TASK-06.04 — Build payment View and ViewModel**
  - **Labels:** `task`, `ui`, `mvvm`, `payments`
  - **Dependencies:** TASK-06.02, TASK-05.03
  - **Acceptance criteria:** Users can select a supported payment method, see pending/success/failure states, and cannot submit payment for an invalid booking.

- **TASK-06.05 — Update booking status from payment results**
  - **Labels:** `task`, `booking`, `payments`
  - **Dependencies:** TASK-06.03, TASK-05.02
  - **Acceptance criteria:** Successful payment confirms the booking; failed payments notify the user and do not leave an incorrectly confirmed reservation; callbacks/retries are idempotent.

## EPIC-07 — Implement notifications

**Labels:** `epic`, `notifications`, `priority:medium`

**Goal:** Notify customers about confirmations, cancellations, modifications, reminders, and payment outcomes.

### Tasks

- **TASK-07.01 — Define booking-event and notification models**
  - **Labels:** `task`, `model`, `notifications`
  - **Dependencies:** TASK-05.01, TASK-06.01
  - **Acceptance criteria:** Events and delivery states support confirmation, cancellation, modification, reminder, and payment failure notifications.

- **TASK-07.02 — Implement notification service abstraction and provider adapter**
  - **Labels:** `task`, `service`, `notifications`
  - **Dependencies:** TASK-07.01, TASK-01.02
  - **Acceptance criteria:** Email and future push providers are behind interfaces; delivery failures can be retried and recorded.

- **TASK-07.03 — Publish booking and payment events**
  - **Labels:** `task`, `booking`, `payments`, `notifications`
  - **Dependencies:** TASK-05.04, TASK-06.05
  - **Acceptance criteria:** Confirmation and cancellation events are emitted exactly once from the business workflow, with safe retry handling.

- **TASK-07.04 — Implement notification templates, preferences, and delivery monitoring**
  - **Labels:** `task`, `notifications`, `observability`
  - **Dependencies:** TASK-07.02
  - **Acceptance criteria:** Messages use localized templates; delivery is targeted within one minute and reliability is measurable against the 99% requirement.

## EPIC-08 — Secure, operate, and monitor the system

**Labels:** `epic`, `security`, `operations`, `priority:high`

**Goal:** Address security, privacy, administration, observability, availability, and disaster recovery requirements.

### Tasks

- **TASK-08.01 — Implement input validation and secure data-access boundaries**
  - **Labels:** `task`, `security`, `validation`
  - **Dependencies:** TASK-01.04, TASK-02.01
  - **Acceptance criteria:** User input is validated at UI and service boundaries; parameterized data access prevents injection attacks.

- **TASK-08.02 — Configure encryption in transit and at rest**
  - **Labels:** `task`, `security`, `privacy`
  - **Dependencies:** TASK-01.05
  - **Acceptance criteria:** Sensitive transport uses HTTPS/TLS; protected data and secrets use approved encryption/key-management mechanisms.

- **TASK-08.03 — Implement privacy and data-retention controls**
  - **Labels:** `task`, `privacy`, `gdpr`
  - **Dependencies:** TASK-02.05, TASK-08.01
  - **Acceptance criteria:** Personal-data access, correction, deletion/retention, and consent behavior are documented and testable for applicable regulations.

- **TASK-08.04 — Build administrator user and permission management**
  - **Labels:** `task`, `admin`, `authorization`, `ui`
  - **Dependencies:** TASK-02.07
  - **Acceptance criteria:** Administrators can manage user roles and reset passwords through authorized Views and ViewModels; every change is audited.

- **TASK-08.05 — Implement centralized logging, monitoring, and audit trails**
  - **Labels:** `task`, `observability`, `audit`
  - **Dependencies:** TASK-01.04, TASK-02.04
  - **Acceptance criteria:** System errors, performance bottlenecks, security events, data changes, and configuration changes produce structured, privacy-safe records.

- **TASK-08.06 — Define backups, restoration, failover, and recovery procedures**
  - **Labels:** `task`, `availability`, `operations`
  - **Dependencies:** TASK-08.05
  - **Acceptance criteria:** Backup frequency, restoration steps, failover behavior, recovery objectives, and an operational runbook are documented and tested.

- **TASK-08.07 — Create admin log and operational status Views and ViewModels**
  - **Labels:** `task`, `admin`, `ui`, `mvvm`
  - **Dependencies:** TASK-08.04, TASK-08.05
  - **Acceptance criteria:** Authorized administrators can inspect relevant logs and service status without exposing secrets or personal data unnecessarily.

## EPIC-09 — Deliver quality, localization, and release readiness

**Labels:** `epic`, `quality`, `testing`, `release`, `priority:high`

**Goal:** Make the application maintainable, localized, tested, documented, and deployable.

### Tasks

- **TASK-09.01 — Add localization and locale-aware formatting**
  - **Labels:** `task`, `localization`, `ui`
  - **Dependencies:** TASK-01.03, TASK-03.03
  - **Acceptance criteria:** UI strings support multiple languages; currency, dates, and times use the user's locale consistently.

- **TASK-09.02 — Add unit tests for Models, ViewModels, validation, and business rules**
  - **Labels:** `task`, `testing`, `unit-test`
  - **Dependencies:** TASK-01.04, TASK-05.02
  - **Acceptance criteria:** Tests cover ViewModel commands/states, validation, role checks, overlap prevention, payment outcomes, and notification event creation.

- **TASK-09.03 — Add integration tests for repositories and external-service adapters**
  - **Labels:** `task`, `testing`, `integration-test`
  - **Dependencies:** TASK-03.02, TASK-06.02, TASK-07.02
  - **Acceptance criteria:** Tests cover persistence, authentication integration, mock payments, notification retries, and failure handling.

- **TASK-09.04 — Add end-to-end tests for core customer and staff journeys**
  - **Labels:** `task`, `testing`, `e2e`
  - **Dependencies:** TASK-05.03, TASK-06.05, TASK-07.03
  - **Acceptance criteria:** Automated journeys cover registration/login, search, booking, payment, confirmation, cancellation, and staff reservation management.

- **TASK-09.05 — Add performance and concurrency verification**
  - **Labels:** `task`, `testing`, `performance`
  - **Dependencies:** TASK-04.02, TASK-05.06
  - **Acceptance criteria:** Measurements cover login under two seconds, search under two seconds (and maximum three seconds), reservation processing under five seconds, and concurrent-user behavior.

- **TASK-09.06 — Maintain architecture, API, deployment, and user documentation**
  - **Labels:** `task`, `documentation`
  - **Dependencies:** TASK-01.01, TASK-08.06
  - **Acceptance criteria:** Documentation describes MAUI MVVM client boundaries, API contracts, Docker Compose deployment, configuration, recovery, user guides, FAQs, and help articles.

- **TASK-09.07 — Create CI validation and release checklist**
  - **Labels:** `task`, `ci`, `release`
  - **Dependencies:** TASK-09.02, TASK-09.03, TASK-09.04
  - **Acceptance criteria:** CI builds the application, runs automated tests, reports failures, checks basic quality/security rules, and uses a repeatable release checklist.

## Suggested implementation order

1. EPIC-00 — Client/server split and container platform
2. EPIC-01 — MVVM foundation
3. EPIC-02 — Identity and roles
4. EPIC-03 — Hotel and room management
5. EPIC-04 — Search and recommendations
6. EPIC-05 — Booking management
7. EPIC-06 — Payment processing
8. EPIC-07 — Notifications
9. EPIC-08 — Security and operations (security tasks should begin with the foundation)
10. EPIC-09 — Quality, localization, and release readiness (testing should begin alongside each epic)

## Script parsing notes

- Create one GitHub issue per `EPIC-*` entry and mark it as the parent issue.
- Create one issue per `TASK-*` entry and link it to its listed epic.
- Preserve issue keys in titles or metadata so rerunning the upload script can be idempotent.
- Convert **Labels**, **Dependencies**, **Goal**, and **Acceptance criteria** into structured issue fields or the issue body.
- GitHub Projects can use the epic/task distinction, labels, and dependencies to create views for status, priority, and subsystem.
