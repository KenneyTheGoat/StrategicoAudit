#  Admin Action Audit – Demo System

A lightweight, end-to-end **Admin Action Audit** system designed for operational traceability in supply-chain environments.

This project demonstrates how **user actions that modify operational data (inventory)** are recorded in a structured audit log and later **investigated by an admin/support user** using clear SQL queries.

The goal is **traceability and investigation**, not full compliance reporting.

---

## What this demonstrates

- A **real database-backed inventory table**
- A **structured audit event model** capturing:
  - who did what
  - when it happened
  - what record changed
  - what exactly changed (old → new)
- A **transactional guarantee**:
  - no inventory change without an audit event
  - no audit event without a real change
- A simple **UI flow** to demonstrate:
  - user action
  - admin investigation
- The two **core investigation questions**:
  1. *What actions did user X perform last week?*
  2. *What changed on record Y?*

---

##  Tech stack

- **ASP.NET Core** (Web API + Razor Pages)
- **Entity Framework Core**
- **PostgreSQL**
- **Docker** (for local Postgres)
- **Razor Pages** (simple, internal admin-style UI)

No frontend framework, no authentication system — intentionally kept lightweight.

---

## Core domain tables

### `inventory_item` (source of truth)
Stores current operational state.

| Column        | Description |
|--------------|------------|
| `warehouse_id` | Warehouse identifier |
| `sku` | Product SKU |
| `on_hand` | Current stock quantity |
| `updated_at` | Last update timestamp |

---

### `audit_event` (append-only history)
Stores immutable audit records.

| Field | Purpose |
|-----|--------|
| `actor_user_id` | Who performed the action |
| `actor_role` | USER / ADMIN |
| `action` | e.g. `INVENTORY_ADJUST` |
| `entity_type` | e.g. `InventoryItem` |
| `entity_id` | e.g. `W1:ABC123` |
| `changes` | JSON diff (old → new) |
| `metadata` | Context (warehouse, sku, reason) |
| `occurred_at` | When it happened |
| `request_id` | Correlation ID |

---

##  Running the system locally

### Start PostgreSQL (Docker)

```bash
docker run --name strategico-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_DB=strategico_audit \
  -p 5432:5432 \
  -d postgres:16


## Run locally
1. Start Postgres (Docker):
  ```cmd / ps
   docker run --name strategico-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_USER=postgres -e POSTGRES_DB=strategico_audit -p 5432:5432 -d postgres:16

2. Apply migrations:
   dotnet ef database update

3. Run:
   dotnet run

Open:
- /login
- /user/inventory-adjust
- /admin/audit
