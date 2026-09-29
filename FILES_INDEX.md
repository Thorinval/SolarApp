# SolarApp - Index des fichiers

Structure et description de tous les fichiers du projet.

## 📁 Structure du projet

```
D:\SolarApp/
├── 📄 README.md                 # Guide d'utilisation principal
├── 📄 INSTALL.md                # Guide d'installation détaillé
├── 📄 DEVELOPMENT.md            # Guide de développement
├── 📄 SECRETS.md                # Configuration des secrets
├── 📄 .gitignore                # Fichiers à ignorer dans Git
├── 📄 Program.cs                # Point d'entrée et configuration
├── 📄 setup.bat                 # Script de configuration (Windows)
├── 📄 setup.ps1                 # Script de configuration (PowerShell)
│
├── 📂 Models/
│   ├── 📄 AtmoceDtos.cs         # DTO pour l'API Atmoce
│   └── 📄 DomainModels.cs       # Entités domaine (base de données)
│
├── 📂 Services/
│   ├── 📄 AtmocApiService.cs    # Client API Atmoce
│   └── 📄 SolarDataService.cs   # Logique métier
│
├── 📂 Data/
│   ├── 📄 AtmocDbContext.cs     # DbContext Entity Framework
│   └── 📂 Migrations/
│       └── 📄 *_InitialCreate.cs # Migration initiale
│
├── 📂 Components/
│   ├── 📂 Pages/
│   │   ├── 📄 Home.razor        # Dashboard principal
│   │   ├── 📄 Sync.razor        # Page de synchronisation
│   │   └── 📄 SiteDetail.razor  # Détails d'un site
│   ├── 📂 Layout/
│   │   └── 📄 MainLayout.razor  # Layout principal
│   ├── 📄 App.razor             # Composant racine
│   ├── 📄 Routes.razor          # Configuration des routes
│   └── 📄 ErrorBoundary.razor   # Gestion des erreurs
│
├── 📂 wwwroot/
│   ├── 📂 lib/
│   │   └── bootstrap/           # Bootstrap CSS
│   ├── 📄 app.css               # Styles personnalisés
│   └── 📄 favicon.png           # Favicon
│
├── 📂 Properties/
│   └── 📄 launchSettings.json   # Configuration de lancement
│
├── 📄 appsettings.json          # Configuration (placeholders)
├── 📄 appsettings.Development.json # Configuration développement
│
└── 📄 SolarApp.csproj     # Fichier projet
```

## 📋 Description détaillée des fichiers

### Root Files (Racine)

| Fichier | Description |
|---------|------------|
| README.md | Guide d'utilisation et fonctionnalités |
| INSTALL.md | Guide d'installation complet |
| DEVELOPMENT.md | Guide pour développeurs |
| SECRETS.md | Configuration des secrets |
| .gitignore | Fichiers à ignorer lors du git push |
| Program.cs | Configuration de l'application .NET |
| setup.bat | Script de setup automatisé (Windows) |
| setup.ps1 | Script de setup automatisé (PowerShell) |

### Models/

Contient tous les modèles de données.

**AtmoceDtos.cs**
- `ApiResponse<T>` - Wrapper de réponse API
- `AuthTokenResponse` - Réponse d'authentification
- `SiteDto` - Informations d'un site
- `SiteLastPowerDto` - Données actuelles du site
- `MicroinverterLastDataDto` - Données microinverteur
- `BatteryLastDataDto` - Données batterie
- `GatewayLastDataDto` - Données gateway
- `DeviceDto` - Informations d'appareil
- `DeviceAlertDto` - Alerte d'appareil
- `SiteEnergyDto` - Données énergétiques quotidiennes
- `PvBranchDataDto` - Données de branche PV

**DomainModels.cs**
- `Site` - Entité pour un site
- `SiteDataSnapshot` - Historique des données site
- `Device` - Entité pour un appareil
- `DeviceDataSnapshot` - Historique des données appareil
- `DeviceAlert` - Enregistrement d'alerte
- `ApiToken` - Token d'authentification

### Services/

Contient la logique métier et l'accès à l'API.

**AtmocApiService.cs** (350 lignes)
- `AuthenticateAsync()` - Authentification à l'API
- `GetSitesAsync()` - Récupérer la liste des sites
- `GetSiteAsync()` - Récupérer un site détaillé
- `GetSiteLastPowerAsync()` - Données actuelles site
- `GetSiteEnergyAsync()` - Données énergétiques
- `GetDevicesBySiteAsync()` - Appareils d'un site
- `GetMicroinvertersLastDataAsync()` - Données microinverteurs
- `GetBatteriesLastDataAsync()` - Données batteries
- `GetGatewaysLastDataAsync()` - Données gateways
- `GetAlarmsAsync()` - Alertes actives
- Gestion automatique des tokens (refresh)

**SolarDataService.cs** (400 lignes)
- `SyncSitesAsync()` - Synchroniser tous les sites
- `SyncSiteDataAsync()` - Synchroniser données site
- `SyncDevicesAsync()` - Synchroniser appareils
- `SyncAlertsAsync()` - Synchroniser alertes
- `GetSitesAsync()` - Récupérer sites de la DB
- `GetSiteAsync()` - Récupérer site détaillé
- `GetSiteDataSnapshotsAsync()` - Historique site
- `GetActiveAlertsAsync()` - Alertes actives
- `GetDevicesAsync()` - Appareils d'un site
- `MarkAlertAsResolvedAsync()` - Résoudre une alerte

### Data/

Contient la configuration de la base de données.

**AtmocDbContext.cs** (100 lignes)
- DbSet pour toutes les entités
- Configuration des relations
- Contraintes et indexes

**Migrations/InitialCreate.cs**
- Création des tables
- Création des indexes
- Création des contraintes de clés étrangères

### Components/Pages/

Contient les pages Blazor.

**Home.razor** (Dashboard)
- Affiche tous les sites
- Données actuelles de production
- État des batteries
- Liens vers les détails

**Sync.razor** (Synchronisation)
- Formulaire de synchronisation
- Barre de progression
- Gestion des erreurs
- Résultats de synchronisation

**SiteDetail.razor** (Détails)
- Informations complètes du site
- Liste des appareils
- Alertes actives
- Données actuelles

### Configuration Files

**appsettings.json**
- Configuration de base de données (placeholder)
- Configuration API Atmoce (placeholder)
- Ne jamais commit de vrais secrets

**appsettings.Development.json**
- Configuration pour développement
- Logging plus détaillé
- LocalDB par défaut

**Program.cs**
- Configuration des services
- Configuration du DbContext
- Configuration du pipeline HTTP
- Injection de dépendances

## 🔄 Flux d'exécution

1. **Démarrage** → Program.cs
2. **Composant racine** → App.razor
3. **Routage** → Routes.razor
4. **Pages Blazor** → Components/Pages/
5. **Appels métier** → SolarDataService
6. **Appels API** → AtmocApiService
7. **Accès DB** → AtmocDbContext
8. **Base de données** → SQL Server

## 🔑 Dépendances principales

| Package | Version | Rôle |
|---------|---------|------|
| Microsoft.AspNetCore.Components.Web | 9.0.0 | Framework Blazor |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.0 | ORM SQL Server |
| Microsoft.EntityFrameworkCore.Design | 9.0.0 | Outils EF Core |
| Newtonsoft.Json | 13.0.3 | Sérialisation JSON |

Toutes les dépendances sont spécifiées dans `SolarApp.csproj`.

## 📊 Modèle de données

```
Site (1) ──────────── (N) SiteDataSnapshot
  │
  └──────────── (N) Device (1) ──────────── (N) DeviceDataSnapshot

DeviceAlert (N) ──────────── (1) Site (via SiteId)
ApiToken (1) ──────────── (1) Session
```

## 🔐 Points de sécurité

| Fichier | Sécurité |
|---------|----------|
| appsettings.json | Placeholders, jamais de secrets |
| AtmocApiService.cs | Gestion sécurisée des tokens |
| SECRETS.md | Instructions pour User Secrets |
| .gitignore | Exclut les fichiers sensibles |

## 📈 Tailles de fichiers

| Fichier | Lignes | Rôle |
|---------|--------|------|
| AtmocApiService.cs | ~350 | Client API |
| SolarDataService.cs | ~400 | Logique métier |
| AtmocDbContext.cs | ~100 | Configuration DB |
| Home.razor | ~120 | Dashboard |
| README.md | ~200 | Documentation |

**Total estimé** : ~2000 lignes de code

## 🚀 Points d'extension

Pour ajouter de nouvelles fonctionnalités :

1. **Nouvelle API Atmoce**
   - Ajouter DTO dans `Models/AtmoceDtos.cs`
   - Ajouter méthode dans `Services/AtmocApiService.cs`

2. **Nouveau modèle de données**
   - Ajouter entité dans `Models/DomainModels.cs`
   - Ajouter DbSet dans `Data/AtmocDbContext.cs`
   - Créer une migration

3. **Nouvelle page Blazor**
   - Créer `.razor` dans `Components/Pages/`
   - Ajouter injection de services
   - Ajouter route `@page`

4. **Nouvelle logique métier**
   - Ajouter méthode dans `Services/SolarDataService.cs`
   - Ajouter tests unitaires

## 📞 Fichiers d'aide

- **INSTALL.md** - Si problèmes d'installation
- **DEVELOPMENT.md** - Pour développement/contribution
- **SECRETS.md** - Pour configuration des credentials
- **README.md** - Pour usage général

---

**Dernière mise à jour** : 19/09/2026

