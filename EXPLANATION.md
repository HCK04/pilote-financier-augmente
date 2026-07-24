# EXPLANATION — Pilote Financier Augmenté (in plain detail)

This document explains what the project is, the real-world problem it solves, how it works
end to end, and how to run it. No prior knowledge assumed.

---

## 1. The one-sentence version

**You upload a boring accounting export (Excel/CSV), and the app predicts the future of your
money — your day-to-day cash and your budget spending — and warns you *months in advance* if
you're about to overspend your budget.**

---

## 2. Who it's for and why it exists

The target user is the **finance department (DAF / directeur financier) of a Moroccan public-sector
establishment** — a hospital, a university, a state agency, a municipality.

These organizations have two recurring fears:

1. **"Will I run out of cash?"** — Money flows in and out every day (salaries, supplier payments,
   subsidies, receipts). This is **trésorerie** (treasury/cash flow). It is volatile and short-term.
2. **"Am I about to blow my budget?"** — At the start of the year they are granted a *fixed* voted
   budget (an envelope they cannot legally exceed). Throughout the year they spend it down. This is
   **exécution budgétaire** (budget execution). It's more stable but has a hard ceiling.

Today they answer these questions by manually staring at spreadsheets and guessing. This tool
answers them **automatically, with a statistical forecast**, and — crucially — raises an **early
alert** when a budget overrun is coming, instead of discovering it too late.

### The strategic idea (from the design document)
The module is deliberately built as an **autonomous "produit d'appel"** (a low-friction entry
product). It presupposes **nothing** about the client's existing systems — it just needs a file
export. That means it can be deployed at a new client in *days*, building trust quickly. It is
positioned around **Article 31 of the Moroccan LOLF (Organic Budget Law n°130-13)**, which requires
budgetary *sincerity and forecasting* — so an AI-assisted forecast is not just a nice feature, it
supports a legal obligation.

---

## 3. What you put in vs. what you get out

| You provide (Excel/CSV) | The app returns |
|---|---|
| **Accounting entries**: `code; label; date; debit; credit` | A **treasury forecast** — projected net cash flow per day/week, with a confidence band |
| **Voted budget**: `code; label; amount; year` | A **budget-execution forecast** — cumulative spending vs. the ceiling, **with an overrun alert** if the projection crosses it |

The `code` in both files is an accounting account code. It gets mapped to a **PCGE** code
(*Plan Comptable Général des Entreprises* — the standard Moroccan chart of accounts). That common
code is the "join key" that lets the engine cross-reference *what was spent* (entries) against
*what was budgeted* (voted budget).

---

## 4. The big picture — 5 layers

The design (see [`PLAN.md`](PLAN.md)) splits the system into 5 decoupled layers. Data flows top to bottom:

```
[Sources tierces]        [Module IA autonome]                    [Restitution]
 Excel / CSV      ->  1. INGESTION & MAPPING
 Export comptable      2. NORMALISATION (PCGE générique)
 Budget voté           3. STOCKAGE tampon multi-tenant
                       4. MOTEUR IA (ML.NET / SSA x2)      ->  Dashboard web
                       5. API RÉSULTATS (REST)                 (+ exports, alertes)
```

1. **Ingestion & mapping** — read the client's file; store each row *raw*; let someone map the
   client's account codes to standard PCGE codes.
2. **Normalisation** — translate raw rows into a generic PCGE structure so the same engine works
   for *any* client regardless of their source system.
3. **Multi-tenant storage** — every client's data lives in an isolated space (critical for public-
   sector data confidentiality).
4. **AI engine** — two forecasting pipelines (one for treasury, one for budget) built on **SSA**.
5. **Restitution** — the results are exposed via a REST API and shown in the web dashboard.

Why decouple them? So each part can change independently. For example, the forecasting engine only
ever reads *aggregated series*, never raw data — so you could swap the algorithm later without
touching the data pipeline.

---

## 5. The data model (layer 3) — and why it's shaped this way

| Entity | Purpose |
|---|---|
| **Tenant** | One client. Root of all data isolation. |
| **ImportBatch** | One uploaded file, timestamped and typed (Entries vs. Budget). Traceability. |
| **EcritureBrute** (*raw entry*) | A line exactly as it came from the client's file, **before** mapping. **Never overwritten.** |
| **MappingCompte** | The rule `client code -> PCGE code`, per tenant. |
| **EcritureNormalisee** (*normalized entry*) | The PCGE-translated version, rebuilt from raw + mapping. |
| **BudgetVote** (*voted budget*) | A budget line, **versioned by `DateValidite`** so mid-year adjustments (crédits/reventilation) keep history. |
| **SerieAgregee** (*aggregated series*) | A cache of time-series points. The **only** thing the engine reads. |

**Two design choices worth understanding:**

- **Raw vs. normalized are kept separate.** The first mapping a client gives you is rarely perfect.
  Because raw entries are never destroyed, you can **re-normalize without re-importing the file**
  when the mapping is corrected. It also gives the client transparency: "here is your raw data, and
  here is how we translated it."
- **Multi-tenant safety via a global filter.** Every tenant-scoped table has an EF Core
  *global query filter* `WHERE TenantId == currentTenant`. This means a developer *cannot
  accidentally* leak one client's data into another's by forgetting a filter — the filter is
  automatic on every query. For public-sector financial data, that's a hard requirement.

---

## 6. The forecasting engine (layer 4) — the heart

The engine uses **SSA (Singular Spectrum Analysis)** via **ML.NET** (`Microsoft.ML.TimeSeries`).
SSA is a time-series technique that decomposes a signal into trend + seasonality + noise and
projects it forward — well suited to financial series with weekly/seasonal patterns.

There are **two pipelines that share one architecture** but use different settings:

| | Treasury (Trésorerie) | Budget execution (Budgétaire) |
|---|---|---|
| Nature | Net flows (in - out) | Cumulative spending vs. a fixed envelope |
| Granularity | Daily / weekly | Monthly |
| SSA window | Short (weekly/monthly seasonality) | Long (annual) |
| Special rule | none | **Ceiling constraint** = the voted budget |

Four important behaviors, each solving a real problem:

1. **Configurable horizon.** You ask for "N periods ahead" *at inference time*. The model trains
   once on the available history; you can then request 14 days, 30 days, or end-of-year without
   retraining. (In the UI, the horizon slider.)
2. **Robustness to short history.** New autonomous clients often have little data. If there are
   fewer than **12 data points**, the engine does **not crash or lie** — it falls back to an
   *indicative* projection and flags **low confidence**. Credibility matters in a sales context.
3. **Budget ceiling alert (the killer feature).** For the budget series, the forecast is compared to
   the voted ceiling. Any projected point above the ceiling is flagged `alerteDepassement = true`.
   The value proposition: you see a red flag in **June** instead of discovering the overrun in
   December. This is the tangible LOLF Article 31 argument.
4. **Decoupling.** The engine reads only `SerieAgregee` (the aggregated cache), never the entries.

> Example from the live demo: with a 220 000 MAD ceiling, once the cumulative projection reaches
> ~250 000, the app returns `alerteDepassement: true` and the dashboard lights up the alert card
> ("Dépassement anticipé — Mai").

---

## 7. The restitution layer (layer 5) — the UI

A single-page web dashboard, served directly by the .NET API from `wwwroot`, so one command runs
both the API and the UI. It talks to the REST endpoints and renders **custom animated SVG charts**.

Design language: clean, mostly monochrome dark, one restrained accent (no rainbow gradients), with
premium **Anime.js** motion — cinematic boot, kinetic title, 3D card tilt with light-sheen, magnetic
buttons, a cursor spotlight, staggered reveals, animated chart path-drawing, spring-based counters,
and elastic toasts. It aims for an Awwwards-level feel while staying fast and usable.

The dashboard flow:
1. Click **"Lancer la démonstration"** (or drag in your own two files).
2. It seeds/imports data, then normalizes + aggregates.
3. It requests both forecasts and animates the charts, KPIs, and any overrun alert into view.
4. The **horizon slider** re-runs the projection instantly.

---

## 8. The API (what the UI calls)

Base path `/api`. Swagger UI is at `/swagger`.

| Method & path | What it does |
|---|---|
| `POST /import/ecritures` | Upload an accounting-entries file |
| `POST /import/budget` | Upload a voted-budget file |
| `POST /mapping` | Set/update `source code -> PCGE` mappings |
| `POST /recalculer` | Re-normalize, then rebuild the aggregated series cache |
| `GET  /previsions/tresorerie?horizon=N&granularite=Jour` | Treasury forecast |
| `GET  /previsions/budgetaire?horizon=N&exercice=YYYY` | Budget forecast (with overrun flags) |
| `GET  /etat` | Status summary (counts, ceiling, date range) — UI convenience |
| `POST /demo/seed` | Load a realistic demo dataset in one click — UI convenience |

The tenant is resolved from the `X-Tenant-Id` header; without it, a built-in **demo tenant** is used.

---

## 9. Technology stack

- **.NET 10 / ASP.NET Core** — API + static hosting
- **EF Core** — data access, global multi-tenant filter, migrations
- **SQLite** — zero-config database for the MVP (swap for MySQL/SQL Server in production via one line
  in `AddInfrastructure`)
- **ML.NET (`Microsoft.ML.TimeSeries`, SSA)** — the forecasting engine
- **ClosedXML** — Excel parsing
- **Vanilla JS + Anime.js** — the animated front end (no build step; anime.js bundled locally)
- **xUnit** — tests (9 tests: forecasting rules, aggregation, and tenant isolation)

### Project structure
```
src/
  PilotageFinancier.Domain/          # entities + enums (no dependencies)
  PilotageFinancier.Infrastructure/  # EF Core DbContext, tenant filter, migrations
  PilotageFinancier.Application/      # ingestion, mapping, normalization, aggregation, orchestration
  PilotageFinancier.Forecasting/     # ML.NET SSA engine (2 configs, horizon, ceiling alert)
  PilotageFinancier.Api/             # REST + Swagger + static SPA (wwwroot)
tests/
  PilotageFinancier.Tests/           # xUnit
```

---

## 10. How to run it

Prerequisite: **.NET 10 SDK**.

```bash
dotnet build
dotnet test                                   # 9 tests should pass
dotnet run --project src/PilotageFinancier.Api
```

Then open **http://localhost:5199/** and click **"Lancer la démonstration."**
Swagger (raw API) is at **http://localhost:5199/swagger**.

The SQLite database (`pilotage.db`) and a demo tenant are created automatically on first run.

### Sample data (in `samples/`)
- `ecritures_demo.csv`, `budget_demo.csv` — ready-to-upload examples
- `template_ecritures.csv`, `template_budget.csv` — the blank formats you'd hand a client
- Formats: entries `code;libelle;date;debit;credit` · budget `code;intitule;montant;exercice`

---

## 11. What's built vs. what's next

**Built and verified (MVP of all 5 layers):** ingestion, mapping, re-normalization, multi-tenant
storage with global filter, both SSA pipelines (horizon, low-confidence fallback, ceiling alert),
the REST API, and the animated dashboard. Build is green; 9 tests pass; the pipeline is validated
end to end in the browser.

**Deferred to the next iteration:** richer real-time SignalR streaming, PDF/Excel report export,
and email alerts. On the product side (out of code): deployment model (SaaS vs. on-premise for data
sovereignty), onboarding kit, and pricing — these live in the internal design document.

---

*Related docs: [`README.md`](README.md) (quick start) · [`PLAN.md`](PLAN.md) (implementation plan).*
