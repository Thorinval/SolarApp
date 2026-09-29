## Configuration des secrets pour le développement

### Initialiser User Secrets

```bash
cd D:\SolarApp
dotnet user-secrets init
```

### Définir les secrets

Remplacez les valeurs par vos credentials réels :

```bash
dotnet user-secrets set "Atmoce:BaseUrl" "https://YOUR_ATMOCE_CLOUD_URL/openapi/v1"
dotnet user-secrets set "Atmoce:ApiKey" "YOUR_API_KEY_HERE"
dotnet user-secrets set "Atmoce:ApiSecret" "YOUR_API_SECRET_HERE"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=AtmoceSolarDb;Integrated Security=True;TrustServerCertificate=True;"
```

### Vérifier les secrets

```bash
dotnet user-secrets list
```

### Supprimer un secret

```bash
dotnet user-secrets remove "Atmoce:ApiKey"
```

### Nettoyer tous les secrets

```bash
dotnet user-secrets clear
```

## ⚠️ IMPORTANT - Sécurité en production

**NE JAMAIS** commit les secrets dans le code source !

Les User Secrets sont stockés localement dans `%APPDATA%\Microsoft\UserSecrets\` (Windows).

En production, utilisez :
- Azure Key Vault
- AWS Secrets Manager
- Variables d'environnement
- Fichiers de configuration externes sécurisés

## 📋 Checklist de déploiement

- [ ] User Secrets configurés avec les bonnes valeurs
- [ ] appsettings.json sans secrets sensibles (valeurs de placeholder)
- [ ] Base de données migratée
- [ ] Connection string production configurée
- [ ] Logs configurés pour production
- [ ] HTTPS activé
- [ ] CORS configuré si nécessaire

