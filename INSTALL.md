# Installation Guide - SolarApp

Guide complet d'installation et de configuration de l'application Blazor Server pour le suivi solaire Atmoce.

## 📋 Prérequis

- **Windows 10/11** ou **macOS 10.15+** ou **Linux**
- **.NET 9.0 SDK** ou supérieur
  - Télécharger : https://dotnet.microsoft.com/download
- **SQL Server Express** (gratuit) ou **LocalDB** (inclus avec Visual Studio)
  - Télécharger : https://www.microsoft.com/en-us/sql-server/sql-server-express
- **Visual Studio 2026** ou **Visual Studio Code**
- **Credentials Atmoce API** (app_key, app_secret)
  - Contacter le support Atmoce ou utiliser votre portail administrateur

## 🚀 Installation rapide (Windows)

### Option 1 : Script automatisé

```bash
cd D:\SolarApp
.\setup.ps1
```

Suivez les prompts pour configurer :
- URL API Atmoce
- API Key et Secret
- Chaîne de connexion SQL Server

### Option 2 : Installation manuelle

#### Étape 1 : Cloner/Télécharger le projet

```bash
cd D:\SolarApp
```

#### Étape 2 : Installer les dépendances

```bash
dotnet restore
```

#### Étape 3 : Configurer User Secrets

```bash
# Initialiser les secrets
dotnet user-secrets init

# Définir l'URL API Atmoce
dotnet user-secrets set "Atmoce:BaseUrl" "https://YOUR_ATMOCE_URL/openapi/v1"

# Définir la clé API
dotnet user-secrets set "Atmoce:ApiKey" "YOUR_API_KEY"

# Définir le secret API
dotnet user-secrets set "Atmoce:ApiSecret" "YOUR_API_SECRET"

# (Optionnel) Définir la connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=AtmoceSolarDb;Integrated Security=True;"
```

#### Étape 4 : Créer la base de données

```bash
# Si SQL Server n'a pas de LocalDB par défaut
dotnet ef database update --connection "Server=(localdb)\mssqllocaldb;Database=AtmoceSolarDb;Integrated Security=True;"

# Ou si vous avez une instance SQL Server personnalisée
dotnet ef database update --connection "YOUR_CONNECTION_STRING"
```

#### Étape 5 : Lancer l'application

```bash
dotnet run
```

L'application sera accessible sur :
- **HTTPS** : https://localhost:7000
- **HTTP** : http://localhost:5000

## 🛠️ Installation sur Linux/macOS

### Prérequis supplémentaires

```bash
# macOS (avec Homebrew)
brew install dotnet@9

# Ubuntu/Debian
sudo apt-get install -y dotnet-sdk-9.0

# Fedora
sudo dnf install dotnet-sdk-9.0
```

### Configuration de SQL Server

Pour Linux/macOS, vous pouvez utiliser :
- **SQL Server in Docker** : https://hub.docker.com/r/microsoft/mssql-server-linux
- **Azure SQL Database** : https://azure.microsoft.com/services/sql-database/

Exemple avec Docker :

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123!" \
  -p 1433:1433 \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### Installation Linux/macOS

```bash
# Cloner le projet
cd SolarApp

# Installer les dépendances
dotnet restore

# Configurer les secrets
dotnet user-secrets init
dotnet user-secrets set "Atmoce:BaseUrl" "https://YOUR_URL/openapi/v1"
dotnet user-secrets set "Atmoce:ApiKey" "YOUR_KEY"
dotnet user-secrets set "Atmoce:ApiSecret" "YOUR_SECRET"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=AtmoceSolarDb;User Id=sa;Password=YourPassword123!;"

# Créer la base de données
dotnet ef database update

# Lancer l'application
dotnet run
```

## 🔐 Configuration des credentials Atmoce

### Obtenir vos credentials

1. Accédez à votre portail Atmoce
2. Allez dans Paramètres → Intégrations API
3. Créez une nouvelle intégration/clé API
4. Copiez :
   - **App Key** (API Key)
   - **App Secret** (API Secret)
   - **URL Base** (généralement fournie par Atmoce)

### Configurer dans l'application

**Option A : User Secrets (Développement recommandé)**

```bash
dotnet user-secrets set "Atmoce:BaseUrl" "https://api.atmoce-cloud.com/openapi/v1"
dotnet user-secrets set "Atmoce:ApiKey" "sk_live_xxxxx"
dotnet user-secrets set "Atmoce:ApiSecret" "sk_secret_xxxxx"
```

**Option B : appsettings.json (DÉVELOPPEMENT UNIQUEMENT)**

⚠️ **ATTENTION** : Ne jamais commit les credentials dans le code !

```json
{
  "Atmoce": {
	"BaseUrl": "https://api.atmoce-cloud.com/openapi/v1",
	"ApiKey": "sk_live_xxxxx",
	"ApiSecret": "sk_secret_xxxxx"
  }
}
```

**Option C : Variables d'environnement (Production)**

```bash
# Windows (CMD)
set Atmoce__BaseUrl=https://api.atmoce-cloud.com/openapi/v1
set Atmoce__ApiKey=sk_live_xxxxx
set Atmoce__ApiSecret=sk_secret_xxxxx

# Windows (PowerShell)
$env:Atmoce__BaseUrl="https://api.atmoce-cloud.com/openapi/v1"
$env:Atmoce__ApiKey="sk_live_xxxxx"
$env:Atmoce__ApiSecret="sk_secret_xxxxx"

# Linux/macOS
export Atmoce__BaseUrl="https://api.atmoce-cloud.com/openapi/v1"
export Atmoce__ApiKey="sk_live_xxxxx"
export Atmoce__ApiSecret="sk_secret_xxxxx"
```

## 💾 Configuration de la base de données

### SQL Server LocalDB (Windows)

LocalDB est inclus avec Visual Studio. Chaîne de connexion :

```
Server=(localdb)\mssqllocaldb;Database=AtmoceSolarDb;Integrated Security=True;
```

Commande de création :

```bash
dotnet ef database update
```

### SQL Server Express

Chaîne de connexion typique :

```
Server=.\SQLEXPRESS;Database=AtmoceSolarDb;Integrated Security=True;TrustServerCertificate=True;
```

### Serveur SQL Server distant

```
Server=your-server.database.windows.net;Database=AtmoceSolarDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;
```

### Azure SQL Database

```
Server=your-server.database.windows.net;Database=AtmoceSolarDb;User Id=admin@your-server;Password=YourPassword123!;Encrypt=True;
```

## ✅ Vérification de l'installation

### Tester la connexion API

Une fois l'application lancée, allez sur `/sync` pour tester :
1. Connexion à l'API Atmoce
2. Récupération des sites
3. Synchronisation des données

### Vérifier la base de données

```bash
# Avec SQL Server Management Studio
# Connecter à (localdb)\mssqllocaldb
# Vérifier la DB "AtmoceSolarDb"

# Ou en ligne de commande
sqlcmd -S (localdb)\mssqllocaldb -d AtmoceSolarDb -Q "SELECT COUNT(*) FROM Sites"
```

## 🚀 Première utilisation

1. **Lancer l'application**
   ```bash
   dotnet run
   ```

2. **Ouvrir le navigateur**
   - https://localhost:7000

3. **Aller à la page de synchronisation**
   - Accédez à https://localhost:7000/sync

4. **Synchroniser les données**
   - Cliquez sur "Démarrer la synchronisation"
   - L'application récupérera :
	 - Vos sites Atmoce
	 - Les appareils (microinverteurs, batteries, gateways)
	 - Les données en temps réel
	 - Les alertes actives

5. **Consulter le dashboard**
   - Retournez à https://localhost:7000
   - Vous verrez vos sites et données actuelles

## 🐛 Dépannage

### Erreur : "Cannot connect to API"

**Causes possibles** :
- Credentials incorrects
- URL API invalide
- Compte Atmoce expiré ou désactivé
- Limitation QPS dépassée (max 10 000 appels/mois)

**Solutions** :
1. Vérifier les credentials : `dotnet user-secrets list`
2. Tester l'URL avec Postman
3. Vérifier l'état du compte Atmoce
4. Attendre avant de réessayer (rate limiting)

### Erreur : "Database connection failed"

**Causes possibles** :
- SQL Server n'est pas en cours d'exécution
- Chaîne de connexion incorrecte
- Permissions insuffisantes

**Solutions** :
```bash
# Vérifier la connexion
sqlcmd -S (localdb)\mssqllocaldb

# Réinitialiser la base de données
dotnet ef database drop
dotnet ef database update
```

### Erreur : "User Secrets not initialized"

**Solution** :
```bash
dotnet user-secrets init
```

### L'application démarre mais pas de données

**Causes possibles** :
- Première exécution (aucune synchronisation)
- Synchronisation échouée

**Solutions** :
1. Aller à https://localhost:7000/sync
2. Cliquer sur "Démarrer la synchronisation"
3. Attendre que la synchronisation soit complète

## 📊 Logs et débogage

Vérifier les logs dans la fenêtre de console lors de l'exécution :

```bash
# Mode verbose
dotnet run --verbosity detailed
```

Les logs incluront :
- Authentification API
- Erreurs de connexion
- Migrations de base de données
- Erreurs métier

## 🔒 Sécurité en production

Avant de déployer en production :

1. ✅ **Credentials** : Utiliser Azure Key Vault ou AWS Secrets Manager
2. ✅ **HTTPS** : Générer un certificat valide
3. ✅ **Base de données** : Utiliser une instance SQL Server sécurisée
4. ✅ **Logs** : Configurer un système de logging centralisé
5. ✅ **Firewall** : Restreindre l'accès à l'application
6. ✅ **Authentification** : Ajouter une authentification utilisateur
7. ✅ **Rate limiting** : Implémenter du throttling côté client

## 📞 Support

- Documentation Atmoce : https://api.library.loxone.com/
- .NET Documentation : https://docs.microsoft.com/dotnet/
- Issues GitHub : Créer une issue si vous rencontrez un problème

## ✨ Prochaines étapes

Après l'installation réussie :

1. [ ] Customiser le dashboard
2. [ ] Ajouter des graphiques de production
3. [ ] Mettre en place des alertes email
4. [ ] Planifier une synchronisation automatique
5. [ ] Créer un API REST pour tiers
6. [ ] Ajouter une authentification utilisateur

---

**Dernière mise à jour** : 19/09/2026

