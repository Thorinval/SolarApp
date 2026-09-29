# Application Web Blazor Server - Suivi Installation Solaire Atmoce

Application web permettant de monitorer et gérer votre installation solaire via l'API Atmoce-Cloud.

## 🚀 Démarrage rapide

### Prérequis
- .NET 9.0 ou supérieur
- SQL Server (local ou distant)
- Credentials Atmoce API (app_key, app_secret)

### Installation

1. **Cloner/ouvrir le projet**
   ```bash
   cd D:\SolarApp
   ```

2. **Configurer la base de données**

   Mettre à jour `appsettings.json` avec votre chaîne de connexion SQL Server :
   ```json
   "ConnectionStrings": {
	 "DefaultConnection": "Server=YOUR_SERVER;Database=AtmoceSolarDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;"
   }
   ```

3. **Configurer l'API Atmoce**

   Mettre à jour `appsettings.json` avec vos credentials Atmoce :
   ```json
   "Atmoce": {
	 "BaseUrl": "https://YOUR_ATMOCE_URL/openapi/v1",
	 "ApiKey": "your-app-key",
	 "ApiSecret": "your-app-secret"
   }
   ```

4. **Créer la base de données**
   ```bash
   dotnet ef database update
   ```

5. **Lancer l'application**
   ```bash
   dotnet run
   ```

   L'application sera accessible sur `https://localhost:7000` (ou le port configuré)

## 📋 Fonctionnalités

### Dashboard (/)
- Vue d'ensemble de tous les sites
- État actuel de la production solaire
- État de charge des batteries
- Liens vers les détails de chaque site

### Synchronisation (/sync)
- Synchronisation manuelle avec l'API Atmoce
- Récupération des :
  - Sites et leurs configurations
  - Appareils (microinverteurs, batteries, passerelles)
  - Données en temps réel
  - Alertes actives

### Détails du site (/site/{id})
- Informations complètes du site
- Données actuelles (production, consommation, batterie)
- Liste des appareils avec leurs statuts
- Alertes actives et recommendations

## 🏗️ Architecture

### Couches
```
Présentation (Blazor Components)
	↓
Services Métier (SolarDataService)
	↓
API Client (AtmocApiService)
	↓
API Atmoce-Cloud

Données (EF Core + SQL Server)
```

### Fichiers clés
- **Models/**
  - `AtmoceDtos.cs` - DTO alignés avec l'API Atmoce
  - `DomainModels.cs` - Entités domaine pour la DB

- **Services/**
  - `AtmocApiService.cs` - Client API avec authentification et gestion des tokens
  - `SolarDataService.cs` - Logique métier (synchronisation, requêtes)

- **Data/**
  - `AtmocDbContext.cs` - DbContext EF Core
  - `Migrations/` - Migrations de base de données

- **Components/Pages/**
  - `Home.razor` - Dashboard principal
  - `Sync.razor` - Page de synchronisation
  - `SiteDetail.razor` - Détails d'un site

## 🔐 Sécurité

### Gestion des credentials
- Les credentials Atmoce doivent être stockés dans :
  - `appsettings.json` pour développement LOCAL uniquement
  - `User Secrets` en développement (`dotnet user-secrets init`)
  - Variables d'environnement en production

### Exemple avec User Secrets
```bash
dotnet user-secrets init
dotnet user-secrets set "Atmoce:ApiKey" "your-api-key"
dotnet user-secrets set "Atmoce:ApiSecret" "your-api-secret"
dotnet user-secrets set "Atmoce:BaseUrl" "https://your-url"
```

### Authentification API
- Système de token Bearer
- Refresh automatique des tokens expirés
- Gestion des erreurs 401 (Unauthorized)

## 📊 Modèle de données

### Entités principales
- **Site** - Installation solaire
- **SiteDataSnapshot** - Historique des données du site
- **Device** - Appareils (gateway, microinverter, battery)
- **DeviceDataSnapshot** - Historique des données des appareils
- **DeviceAlert** - Alertes et anomalies
- **ApiToken** - Tokens d'authentification Atmoce

## 🔄 API Atmoce - Endpoints utilisés

### Authentification
- `POST /auth/auth_token` - Obtenir les tokens d'accès

### Sites
- `POST /sites/getSites` - Lister les sites
- `GET /sites/getSite` - Détails d'un site
- `GET /sites/getSitesLastPower` - Données actuelles du site
- `GET /sites/getSitesEnergy` - Données énergétiques (jour/mois/année)

### Appareils
- `GET /device/getDevicesBySite` - Appareils d'un site
- `GET /microInverter/getMIsLastData` - Données microinverteurs
- `GET /battery/getBatterysLastData` - Données batteries
- `GET /gateway/getGatewaysLastData` - Données passerelle

### Alertes
- `GET /device/getAlarmsBySite` - Alertes actives d'un site

## 🐛 Dépannage

### Erreur de connexion à la base de données
- Vérifier la chaîne de connexion dans `appsettings.json`
- S'assurer que SQL Server est en cours d'exécution
- Vérifier les permissions d'accès

### Erreur d'authentification Atmoce
- Vérifier les credentials (ApiKey, ApiSecret)
- S'assurer que l'URL de base est correcte
- Vérifier que le compte Atmoce est actif

### Pas de données affichées
- Lancer une synchronisation depuis `/sync`
- Attendre que la synchronisation soit complète
- Vérifier les logs de l'application

## 📈 Prochaines améliorations possibles

- [ ] Graphiques de production (Chart.js/Plotly)
- [ ] Export de données (CSV/Excel)
- [ ] Notifications d'alertes (Email, SMS)
- [ ] Planification de synchronisation automatique
- [ ] API REST pour intégrations tierces
- [ ] Authentification utilisateur
- [ ] Multi-utilisateurs avec permissions

## 📝 License

À définir selon vos besoins.

## 📞 Support

Pour toute question ou problème, consultez la documentation Atmoce-Cloud :
https://api.library.loxone.com/

---

**Dernière mise à jour** : 19/09/2026

