# Pilote Financier Augmenté

Module IA **autonome** de prévision financière pour établissements publics (contexte marocain) :
prévision de **trésorerie** et d'**exécution budgétaire** à partir de simples fichiers Excel/CSV,
sans intégration technique lourde. Pensé comme *produit d'appel* déployable en quelques jours.

> Implémentation du document de conception `Implémentation avec claude - Module Pilote Financier Augmenté`.
> Plan détaillé : [`PLAN.md`](PLAN.md).

## Architecture (5 couches)

```
[Excel/CSV] -> 1. Ingestion & mapping -> 2. Normalisation PCGE -> 3. Stockage tampon multi-tenant
            -> 4. Moteur IA (ML.NET / SSA x2) -> 5. API REST + Dashboard SignalR + Exports/Alertes
```

| Couche | Projet | Rôle |
|---|---|---|
| Domaine | `PilotageFinancier.Domain` | Entités & enums, sans dépendance |
| Données | `PilotageFinancier.Infrastructure` | EF Core (SQLite), **filtre global multi-tenant** |
| Métier | `PilotageFinancier.Application` | Ingestion, mapping, normalisation, agrégation |
| IA | `PilotageFinancier.Forecasting` | Moteur **SSA** (ML.NET), 2 configs, horizon, alerte plafond |
| API | `PilotageFinancier.Api` | REST + Swagger + SignalR + seed démo |

## Points de conception clés (du document)

- **Deux séries distinctes** : trésorerie (flux nets, granularité jour/semaine) vs exécution
  budgétaire (dépenses cumulées mensuelles, bornées par le budget voté).
- **Horizon configurable** à l'inférence (`horizon`), sans réentraînement.
- **Robustesse aux historiques courts** : sous 12 points, prévision *indicative (confiance faible)*
  au lieu d'un échec silencieux.
- **Alerte de dépassement** : la prévision budgétaire qui franchit le plafond voté lève une alerte
  (`alerteDepassement`) — argument de conformité **LOLF n°130-13, art. 31**.
- **Multi-tenant** : filtre global EF Core par `TenantId` -> pas de fuite de données entre clients.
- **Traçabilité** : séparation `EcritureBrute` (source) / `EcritureNormalisee` (PCGE), re-normalisable
  sans réimport.

## Démarrage

Prérequis : **.NET 10 SDK**.

```bash
dotnet build
dotnet test
dotnet run --project src/PilotageFinancier.Api
# Swagger : http://localhost:5199/swagger
```

La base SQLite (`pilotage.db`) et un **tenant de démonstration** sont créés au démarrage.

### Exemple de bout en bout (dossier `samples/`)

```bash
BASE=http://localhost:5199/api
curl -X POST $BASE/import/ecritures -F "fichier=@samples/ecritures_demo.csv"
curl -X POST $BASE/import/budget    -F "fichier=@samples/budget_demo.csv"
curl -X POST $BASE/mapping -H "Content-Type: application/json" -d @samples/mapping_demo.json
curl -X POST $BASE/recalculer
curl "$BASE/previsions/tresorerie?horizon=14&granularite=Jour"
curl "$BASE/previsions/budgetaire?horizon=6&exercice=2025"
```

Le tenant est résolu via l'en-tête `X-Tenant-Id` (à défaut, tenant démo).

## Formats d'import (templates dans `samples/`)

- **Écritures** : `code;libelle;date;debit;credit`
- **Budget voté** : `code;intitule;montant;exercice`

## Statut

MVP fonctionnel des 5 couches (build vert, 9 tests verts, pipeline validé de bout en bout).
Restitution avancée — SignalR temps réel enrichi, export PDF/Excel, alertes email — prévue en itération suivante.

## Base de données

SQLite pour le MVP (zéro configuration). Pour la production, remplacer le provider par
MySQL ou SQL Server dans `AddInfrastructure` (couche `Infrastructure`).
