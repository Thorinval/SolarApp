# Guide de développement - SolarApp

Guide pour les développeurs qui souhaitent contribuer ou étendre l'application.

## 🏗️ Architecture de l'application

```
SolarApp/
├── Components/
│   ├── Pages/              # Pages Blazor
│   │   ├── Home.razor      # Dashboard principal
│   │   ├── Sync.razor      # Synchronisation
│   │   └── SiteDetail.razor # Détails site
│   └── Layout/             # Layout components
├── Models/
│   ├── AtmoceDtos.cs       # DTO pour API Atmoce
│   └── DomainModels.cs     # Entités domaine (DB)
├── Services/
│   ├── AtmocApiService.cs  # Client API Atmoce
│   └── SolarDataService.cs # Logique métier
├── Data/
│   ├── AtmocDbContext.cs   # DbContext EF Core
│   └── Migrations/         # Migrations DB
├── wwwroot/                # Ressources statiques
├── Program.cs              # Configuration application
├── appsettings.json        # Configuration (secrets dans User Secrets)
└── README.md               # Documentation
```

## 🔄 Flux de données

```
API Atmoce Cloud
	   ↓
AtmocApiService (HTTP Client)
	   ↓
SolarDataService (Logique métier)
	   ↓
AtmocDbContext (Entity Framework)
	   ↓
SQL Server Database
	   ↓
Blazor Components (UI)
```

## 🛠️ Stack technique

| Composant | Version | Rôle |
|-----------|---------|------|
| .NET | 9.0 | Framework principal |
| Blazor | 9.0 | Framework web (Server-side) |
| Entity Framework Core | 9.0 | ORM pour DB |
| SQL Server | Express+ | Base de données |
| Newtonsoft.Json | 13.0.3 | Sérialisation JSON |

## 🚀 Démarrer le développement

### Prérequis
- Visual Studio 2026 ou VS Code
- .NET 9.0 SDK
- SQL Server LocalDB

### Setup

```bash
# 1. Cloner le projet
cd D:\SolarApp

# 2. Installer les dépendances
dotnet restore

# 3. Configurer les secrets
dotnet user-secrets init
dotnet user-secrets set "Atmoce:BaseUrl" "https://..."
dotnet user-secrets set "Atmoce:ApiKey" "..."
dotnet user-secrets set "Atmoce:ApiSecret" "..."

# 4. Créer la DB
dotnet ef database update

# 5. Lancer en développement
dotnet run
```

## 📝 Ajouter une nouvelle page Blazor

### Exemple : Créer une page "Historique"

1. **Créer le fichier**
   ```
   Components/Pages/History.razor
   ```

2. **Ajouter la route et injection**
   ```razor
   @page "/history"
   @using SolarApp.Services
   @inject SolarDataService SolarDataService
   ```

3. **Ajouter le contenu**
   ```razor
   <div class="container">
	   <h1>Historique</h1>
	   <!-- Votre contenu -->
   </div>

   @code {
	   protected override async Task OnInitializedAsync()
	   {
		   // Chargement des données
	   }
   }
   ```

## 🔌 Ajouter une nouvelle API Atmoce

### Exemple : Ajouter une méthode pour les historiques microinverteurs

1. **Ajouter le DTO** (AtmoceDtos.cs)
   ```csharp
   public class MicroinverterHistoryDto
   {
	   [JsonPropertyName("siteId")]
	   public string? SiteId { get; set; }
	   // ... autres propriétés
   }
   ```

2. **Ajouter la méthode** (AtmocApiService.cs)
   ```csharp
   public async Task<List<MicroinverterHistoryDto>?> GetMicroinvertersHistoryAsync(
	   string siteId, string date)
   {
	   try
	   {
		   if (!await EnsureAuthenticatedAsync())
			   return null;

		   var baseUrl = _configuration["Atmoce:BaseUrl"];
		   var url = $"{baseUrl}/microInverter/getMIsHistoryData?siteId={siteId}&date={date}";

		   return await GetAsync<List<MicroinverterHistoryDto>>(url);
	   }
	   catch (Exception ex)
	   {
		   _logger.LogError($"Erreur: {ex.Message}");
		   return null;
	   }
   }
   ```

3. **Utiliser dans SolarDataService**
   ```csharp
   public async Task<List<T>> GetMicroinvertersHistoryAsync(string siteId, string date)
   {
	   return await _apiService.GetMicroinvertersHistoryAsync(siteId, date);
   }
   ```

## 💾 Ajouter un nouveau modèle de données

### Exemple : Ajouter un modèle pour les données de consommation

1. **Créer l'entité** (DomainModels.cs)
   ```csharp
   public class ConsumptionData
   {
	   public int Id { get; set; }
	   public int SiteId { get; set; }
	   public Site? Site { get; set; }
	   public double DailyConsumption { get; set; }
	   public double MonthlyConsumption { get; set; }
	   public DateTime RecordedAt { get; set; }
   }
   ```

2. **Ajouter au DbContext** (AtmocDbContext.cs)
   ```csharp
   public DbSet<ConsumptionData> ConsumptionDatas { get; set; }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
	   modelBuilder.Entity<ConsumptionData>()
		   .HasKey(c => c.Id);

	   modelBuilder.Entity<ConsumptionData>()
		   .HasOne(c => c.Site)
		   .WithMany()
		   .HasForeignKey(c => c.SiteId)
		   .OnDelete(DeleteBehavior.Cascade);
   }
   ```

3. **Créer une migration**
   ```bash
   dotnet ef migrations add AddConsumptionData
   dotnet ef database update
   ```

## 🧪 Tester l'application

### Tests unitaires (à mettre en place)

Créer un projet de test :
```bash
dotnet new xunit -n SolarApp.Tests
dotnet add reference SolarApp/SolarApp.csproj
```

Exemple de test :
```csharp
public class SolarDataServiceTests
{
	[Fact]
	public async Task SyncSitesAsync_ShouldReturnSites()
	{
		// Arrange
		var mockApi = new Mock<AtmocApiService>();
		var service = new SolarDataService(mockApi.Object, _dbContext, _logger);

		// Act
		var result = await service.SyncSitesAsync();

		// Assert
		Assert.NotNull(result);
	}
}
```

### Tests manuels

1. **Tester la synchronisation**
   - Aller à `/sync`
   - Vérifier que les sites sont récupérés
   - Vérifier que les appareils sont synchronisés

2. **Tester le dashboard**
   - Vérifier que les données s'affichent
   - Vérifier les formats de nombre
   - Tester le responsive design

3. **Tester les pages détails**
   - Cliquer sur un site
   - Vérifier les informations affichées
   - Vérifier les alertes

## 🐛 Debugger l'application

### Visual Studio

1. Mettre des breakpoints
2. Appuyer sur F5 pour lancer en debug
3. Utiliser la fenêtre d'inspection des variables

### VS Code

1. Installer l'extension "C# DevKit"
2. Mettre des breakpoints
3. Presser F5 ou utiliser le debugger intégré

### Logs en console

L'application écrit les logs sur la console. Cherchez :
- `[Information]` - Informations générales
- `[Warning]` - Avertissements
- `[Error]` - Erreurs

## 📦 Build et déploiement

### Créer un build de release

```bash
# Build en mode Release
dotnet build -c Release

# Publier l'application
dotnet publish -c Release -o ./publish
```

### Déployer sur Azure

```bash
# Créer une ressource web app
az appservice plan create --name myPlan --resource-group myGroup --sku Free

# Déployer
dotnet publish -c Release
az webapp up --name myapp --resource-group myGroup
```

## 🔒 Bonnes pratiques de sécurité

1. **Secrets** : Jamais commit les credentials
2. **HTTPS** : Toujours utiliser HTTPS en production
3. **Validation** : Valider les entrées utilisateur
4. **Logging** : Ne pas logger les données sensibles
5. **DB** : Utiliser des paramètres SQL pour éviter les injections
6. **Rate limiting** : Respecter les limites QPS de l'API Atmoce

## 🚀 Performance

### Optimisations à considérer

1. **Caching** : Mettre en cache les données statiques
2. **Pagination** : Utiliser la pagination pour les listes
3. **Lazy loading** : Charger les données à la demande
4. **Indexes DB** : Ajouter des indexes sur les colonnes fréquemment interrogées
5. **Compression** : Compresser les réponses HTTP

Exemple d'ajout de cache :

```csharp
services.AddMemoryCache();
// ...
app.UseResponseCaching();
```

## 📚 Ressources

- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [SQL Server](https://learn.microsoft.com/sql/sql-server)
- [Atmoce API Docs](https://api.library.loxone.com/)
- [.NET Best Practices](https://docs.microsoft.com/dotnet/standard/design-guidelines)

## 🤝 Contribuer

Les contributions sont bienvenues ! Pour contribuer :

1. Fork le projet
2. Créer une branche (`git checkout -b feature/maFeature`)
3. Commiter les changements (`git commit -m 'Ajouter maFeature'`)
4. Pousser vers la branche (`git push origin feature/maFeature`)
5. Ouvrir une Pull Request

## 📞 Support

Pour les questions ou problèmes :
- Consulter la documentation (README.md, INSTALL.md)
- Vérifier les logs de l'application
- Créer une issue GitHub

---

**Dernière mise à jour** : 19/09/2026

