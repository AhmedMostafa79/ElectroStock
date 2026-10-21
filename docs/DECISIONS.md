# Architecture Decision Records

Key technical decisions in ElectroStock, with the reasoning behind them.

---

## ADR-001: BCrypt for Password Hashing

**Status:** Accepted

**Decision:** Use BCrypt.Net-Next instead of ASP.NET Core Identity.

**Why:**
- **Simple** — Identity brings cookies, claims, role manager; a JWT-only API doesn't need them.
- **Portable** — BCrypt is a cross-language standard.

---

## ADR-002: Two DbContexts (App + Audit)

**Status:** Accepted

**Context:** Audit logs must be written in two cases:
- Inside a successful transaction → commit with the business changes.
- After a failed transaction → survive the rollback.

**Problem with one context:** After `RollbackTransactionAsync()`, `AppDbContext`'s change tracker still holds the rolled-back changes. Calling `SaveChanges()` to write the failure log would re-attempt to save them.

**Decision:**
- `AppDbContext` — all business entities, used by repos + `UnitOfWork`.
- `AuditDbContext` — failure audit logs only, with a clean change tracker.

Flag selects the context:
- `isInTransaction = true` → `AppDbContext` (saved by `UnitOfWork`)
- `isInTransaction = false` → `AuditDbContext` (saved immediately)

**Why:** A separate context gives a clean tracker so failure logs persist independently.

**Alternative considered:** `ChangeTracker.Clear()` after rollback — works, but relies on every rollback site remembering to call it. Structural beats convention.

**Trade-off:** Two contexts to maintain (same DB). Unusual pattern — this doc exists because of that.

---

## ADR-003: Table-Per-Hierarchy (TPH)

**Status:** Accepted

**Decision:** TPH for `Product` → `QuantifiedProduct` / `SerializedProduct`, and `User` → `RegisteredUser` / `GuestUser`.

**Why:** Simpler schema — one table per hierarchy, no joins.

**Trade-off:**
- Subtype columns must be nullable (e.g. `GuestUser` rows have `NULL` email / password / status).
- Queries filter by discriminator (`.OfType<SerializedProduct>()`).

**Acceptable for MVP.** TPT/TPC would be cleaner if subtype columns grew.

---

## ADR-004: Asymmetric Product Creation

**Status:** Accepted

**Decision:**
- **Quantified products** — Admin creates catalog entry; Warehouse receives stock later.
- **Serialized products** — created atomically with the first receipt.

**Why:** Quantified products exist meaningfully without stock. Serialized products are units with unique IMEI — a catalog shell without units adds little value.

**Trade-off:** No pre-order / catalog shell for serialized products. A production system would likely support it.

---

## ADR-005: Integration Tests for Transactional Flows

**Status:** Accepted

**Decision:** ~11 integration tests (Testcontainers + real SQL Server) cover the flows where mapping and rollback matter:
- Invoice creation
- Reservation confirmation
- Stock receiving
- Invoice cancellation
- Remove-item-from-invoice

Unit tests (~200) cover domain rules where mocks are sufficient.

**Why mocks aren't enough here:**
- **TPH mapping errors** — need a real DB to surface.
- **Transaction rollback** — only a real DB proves atomicity.
- **Concurrency** — real locking behaves differently from in-memory.

**Trade-off:** Slower (~60–90s with container startup), requires Docker. No API-level HTTP tests in MVP.