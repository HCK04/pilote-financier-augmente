# Plan d'implémentation — Module « Pilote Financier Augmenté »

> Produit d'appel autonome de prévision financière (trésorerie + exécution budgétaire)
> pour établissements publics marocains. Basé sur le document de conception fourni.

## 0. Décisions actées

| Sujet | Décision |
|---|---|
| Stack | **.NET 10** — ASP.NET Core, **EF Core**, **ML.NET (SSA)** via `Microsoft.ML.TimeSeries`, **SignalR** |
| Base de données | **SQLite** pour le MVP (zéro configuration, portable) — swap vers MySQL/SQL Server en production |
| Périmètre V1 | **Produit complet 5 couches** |
| Livrable | Plan écrit **+ construction du MVP** |
| Nom de code solution | `PilotageFinancier` (nom produit affiché : « Pilote Financier Augmenté ») |

## 1. Architecture cible (5 couches du document)

```
[SOURCES TIERS]          [MODULE IA AUTONOME]                 [RESTITUTION]
 Excel / CSV      ->  1. INGESTION & MAPPING
 Export compta         2. NORMALISATION (PCGE générique)
 Budget voté           3. STOCKAGE tampon multi-tenant
                       4. MOTEUR IA (ML.NET / SSA x2)   ->  Dashboard (SignalR)
                       5. API RÉSULTATS (REST)               Export PDF/Excel
                                                             Alertes email
```

## 2. Structure de la solution (.NET)

```
src/
  PilotageFinancier.Domain/          # Entités + enums, aucune dépendance
  PilotageFinancier.Infrastructure/  # EF Core DbContext, migrations, tenant service, filtre global
  PilotageFinancier.Application/      # Services: ingestion, mapping, normalisation, agrégation
  PilotageFinancier.Forecasting/     # Moteur ML.NET SSA (2 pipelines), horizon, seuil confiance
  PilotageFinancier.Api/             # Web API REST + SignalR + exports
tests/
  PilotageFinancier.Tests/           # xUnit (TDD, objectif 80%)
```

## 3. Modèle de données (couche 3)

- **Tenant** — client isolé (multi-tenant)
- **ImportBatch** — un fichier importé, horodaté, typé (Écritures | Budget)
- **EcritureBrute** — ligne brute avant mapping (jamais écrasée -> re-normalisation possible)
- **MappingCompte** — `CodeClientSource -> CodePCGENormalise` (par tenant)
- **EcritureNormalisee** — CodePCGE, Date, MontantDebit/Credit, SourceBatchId
- **BudgetVote** — CodePCGE, Exercice, MontantVote, **DateValidite** (versionné pour réajustements/reventilation)
- **SerieAgregee** — cache : TenantId, TypeSerie, Granularite, Periode, Valeur

**Sécurité multi-tenant** : filtre global EF Core `HasQueryFilter(e => e.TenantId == currentTenant)` sur toutes les entités tenant-scoped -> pas de fuite entre clients par oubli de filtre.

**Traçabilité** : séparation stricte `EcritureBrute` (source) / `EcritureNormalisee` (traduite PCGE).

## 4. Moteur de prévision (couche 4) — deux séries, architecture commune

| | Trésorerie | Exécution budgétaire |
|---|---|---|
| Nature | Flux réels nets | Consommation cumulée vs enveloppe |
| Granularité | Jour / Semaine | Mois |
| Config SSA | Fenêtre courte (hebdo/mensuelle) | Fenêtre longue (annuelle) |
| Contrainte | Aucune | **Plafond = budget voté** -> alerte si dépassement prévu |

- **Horizon configurable** : paramètre `horizonPeriods` à l'**inférence** (le modèle s'entraîne une seule fois).
- **Robustesse historiques courts** : seuil min (12 points) -> prévision « indicative, faible confiance » au lieu d'échec silencieux.
- Le moteur consomme **uniquement `SerieAgregee`**, jamais les écritures normalisées -> découplage total.

## 5. Restitution (couche 5)

- **API REST** : import, mapping, déclenchement prévision, récupération résultats
- **Dashboard SignalR** : mise à jour temps réel des prévisions
- **Exports** PDF / Excel (rapports officiels secteur public)
- **Alertes email** : dépassement budgétaire anticipé (argument LOLF art. 31)

## 6. Découpage en phases (build)

- **Phase 1 — Fondations** : solution + projets, Domain (entités/enums), Infrastructure (DbContext + filtre tenant + SQLite + migration initiale). [cible de cette session]
- **Phase 2 — Ingestion & normalisation** : parseur CSV/Excel, ImportBatch, mapping, service de normalisation, agrégation -> `SerieAgregee`. [cible de cette session]
- **Phase 3 — Moteur SSA** : `Forecasting` avec 2 pipelines, horizon, seuil confiance, contrainte plafond budgétaire. [cible de cette session]
- **Phase 4 — API REST** : endpoints import/mapping/prévision/résultats + Swagger, seed d'un tenant démo. [cible de cette session]
- **Phase 5 — Restitution avancée** : SignalR temps réel, export PDF/Excel, alertes email. [V2 / itération suivante]
- **Phase 6 — Tests** : xUnit sur normalisation, agrégation, contrainte plafond, seuil confiance. [en continu]

## 7. Packaging produit (hors code)

Aspects techniques de mise en produit : modèle de déploiement à trancher (SaaS multi-tenant /
on-premise / hybride) selon les exigences de **souveraineté des données** du secteur public ;
onboarding léger basé sur 2 templates fournis (écritures + budget) ; ancrage conformité
**LOLF n°130-13, art. 31** (sincérité et prévision budgétaire).

> Les aspects commerciaux (tarification, go-to-market) restent dans le document de conception interne,
> non versionné dans ce dépôt public.

## 8. Points de vigilance

- Souveraineté des données (secteur public) -> prévoir option on-premise dès l'architecture.
- Versionnement du budget voté (reventilation / virements de crédits en cours d'exercice).
- Re-normalisation sans réimport après correction de mapping.
- Recalcul du cache `SerieAgregee` à chaque import (ou job nocturne si volume).
