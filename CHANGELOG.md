# Changelog - AtmoceSolarApp

Toutes les modifications et versions de l'application.

## [1.0.0] - 2026-09-19

### ✨ Nouvelles fonctionnalités

- **Dashboard principal** (Home.razor)
  - Vue d'ensemble de tous les sites
  - Affichage des données actuelles de production solaire
  - État de charge des batteries
  - Accès rapide aux détails de chaque site

- **Page de synchronisation** (Sync.razor)
  - Synchronisation manuelle avec l'API Atmoce
  - Récupération des sites et leurs configurations
  - Récupération des appareils (microinverteurs, batteries, gateways)
  - Récupération des données en temps réel
  - Récupération des alertes actives

- **Page de détails du site** (SiteDetail.razor)
  - Informations complètes du site
  - Données actuelles (production, consommation, batterie)
  - Liste des appareils avec leurs statuts
  - Affichage des alertes actives

- **Service API Atmoce** (AtmocApiService.cs)
  - Authentification avec tokens
  - Gestion automatique du refresh des tokens
  - Appels à tous les endpoints principales d'Atmoce
  - Gestion des erreurs et retry

- **Service métier** (SolarDataService.cs)
  - Synchronisation des sites depuis l'API
  - Synchronisation des appareils
  - Création de snapshots de données
  - Gestion des alertes
  - Requêtes sur la base de données

- **Base de données**
  - Modèle de données complet pour sites, appareils, alertes
  - Historique des données avec snapshots
  - Migrations Entity Framework Core
  - SQL Server support

- **Documentation**
  - README.md - Guide d'utilisation
  - INSTALL.md - Guide d'installation détaillé
  - DEVELOPMENT.md - Guide de développement
  - SECRETS.md - Configuration des secrets
  - FILES_INDEX.md - Index des fichiers

- **Scripts d'installation**
  - setup.bat - Script Windows CMD
  - setup.ps1 - Script PowerShell

### 🏗️ Architecture

- Blazor Server (C#/.NET 9.0)
- Entity Framework Core 9.0
- SQL Server pour la persistance
- Architecture en couches (Présentation, Métier, API, Data)

### 📦 Dépendances

- Microsoft.EntityFrameworkCore.SqlServer 9.0.0
- Microsoft.EntityFrameworkCore.Design 9.0.0
- Newtonsoft.Json 13.0.3

### 🔐 Sécurité

- Authentification API Token Atmoce
- Gestion des tokens avec refresh automatique
- User Secrets pour les credentials en développement
- .gitignore pour éviter les leaks de secrets
- HTTPS par défaut

### 📋 Configuration

- appsettings.json pour configuration de base
- appsettings.Development.json pour développement
- User Secrets pour les credentials sensibles

---

## Prochaines versions planifiées

### [1.1.0] - Graphiques et visualisations
- Graphiques de production (Chart.js)
- Graphiques de consommation
- Courbes de température
- Analyse tendances

### [1.2.0] - Notifications et alertes
- Email pour alertes critiques
- SMS pour alertes urgentes
- Dashboard de gestion d'alertes
- Historique des alertes résolues

### [1.3.0] - Export et rapports
- Export CSV de données
- Export Excel avec formules
- Rapports PDF
- Rapports par période

### [1.4.0] - Synchronisation automatique
- Planificateur de tâches (Quartz.NET)
- Synchronisation toutes les heures
- Synchronisation temps réel via WebSocket
- Gestion des erreurs persistantes

### [1.5.0] - Authentification utilisateur
- Authentification ASP.NET Identity
- Autorisations par rôle
- Multi-utilisateurs
- Profils utilisateur

### [2.0.0] - API REST publique
- Endpoints API pour accès externe
- Documentation Swagger/OpenAPI
- Rate limiting
- Clés API pour clients externes

---

## Format du changelog

Ce changelog suit [Keep a Changelog](https://keepachangelog.com/).

### Sections
- **Added** - Nouvelles fonctionnalités
- **Changed** - Changements fonctionnels
- **Deprecated** - Fonctionnalités à abandonner
- **Removed** - Fonctionnalités supprimées
- **Fixed** - Bugs corrigés
- **Security** - Mises à jour de sécurité

### Exemple de format pour futures versions

```markdown
## [X.Y.Z] - YYYY-MM-DD

### Added
- Nouvelle fonctionnalité A
- Nouvelle fonctionnalité B

### Changed
- Modification X
- Modification Y

### Fixed
- Correction du bug A
- Correction du bug B

### Security
- Mise à jour de dépendance sensible
```

---

## 🐛 Issues connues et limitations

### Version 1.0.0

**Limitations**
- Aucune authentification utilisateur (TODO v1.5)
- Pas de graphiques (TODO v1.1)
- Pas de notifications (TODO v1.2)
- Pas de synchronisation automatique (TODO v1.4)
- Pas d'export de données (TODO v1.3)

**Limitations API Atmoce**
- Maximum 10 000 appels par mois
- Maximum 5 appels simultanés
- Délai de 30 jours pour les tokens
- Pas de websocket pour données temps réel

---

## 🔄 Politique de versioning

Nous utilisons [Semantic Versioning](https://semver.org/):

- **MAJOR** - Changements incompatibles (nouvelle architecture)
- **MINOR** - Nouvelles fonctionnalités compatibles
- **PATCH** - Corrections de bugs

Format: `MAJOR.MINOR.PATCH`

Exemple: `1.0.0` → `1.1.0` (nouvelles fonctionnalités) → `1.1.1` (patch bug)

---

## 📊 Statistiques du code

### Version 1.0.0

| Catégorie | Nombre |
|-----------|--------|
| Fichiers C# | 4 |
| Fichiers Razor | 3 |
| Lignes de code | ~2000 |
| Services | 2 |
| Pages Blazor | 3 |
| Entités DB | 6 |
| Endpoints API | 11 |

---

## 🙏 Contribuants

- Développeur principal - Création initiale (v1.0.0)

---

Dernière mise à jour : 2026-09-19
