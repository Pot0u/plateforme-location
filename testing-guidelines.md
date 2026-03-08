# Guide des Tests : Vertical Slice Architecture (VSA)

Ce document décrit la stratégie de test pour le projet, axée sur les tests d'intégration rapides utilisant EF Core In-Memory et Alba.

## 1. Principes de Test

- **Priorité aux Tests d'Intégration** : Testez la "tranche" complète (Endpoint -> Wolverine -> DB).
- **Isolation des Tests** : Chaque test doit être indépendant. Utilisez des bases de données In-Memory nommées de manière unique par test ou nettoyez la base après chaque exécution.
- **Vérification du BFF** : Les tests doivent valider que les réponses API correspondent exactement à ce qu'attend le frontend (Contrat BFF).

## 2. Infrastructure de Test

### EF Core In-Memory
Pour les tests, nous configurons le `DbContext` pour utiliser `UseInMemoryDatabase`. C'est idéal pour VSA car :
1. C'est extrêmement rapide.
2. Cela ne nécessite pas de conteneurs Docker ou de bases de données externes.
3. C'est suffisant pour valider la logique des handlers et la persistance de base.

### Alba pour les tests HTTP
Alba permet de faire tourner l'application en mémoire et de l'interroger avec un client HTTP simulé.

```csharp
[Fact]
public async Task Should_Register_New_Customer()
{
    // Arrange
    using var host = await AlbaHost.For<Program>(builder => {
        builder.ConfigureServices(services => {
            // Remplacer la DB par In-Memory si nécessaire
            services.AddDbContext<CustomersDbContext>(options => 
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        });
    });

    // Act
    var response = await host.PostJson(new RegisterCustomer("test@test.com", "password", "Test User"), "/api/customers/register");

    // Assert
    response.StatusCodeShouldBe(200);
    var result = response.ReadAsJson<CustomerRegistered>();
    result.Id.ShouldNotBe(Guid.Empty);
}
```

## 3. Structure des Tests

Organisez les tests en suivant la structure des modules et des fonctionnalités :
`PlateformeLocationDisques.Tests/Modules/[ModuleName]/Features/[FeatureName]Tests.cs`

## 4. Skills pour l'Agent

### Écrire un test de tranche (Slice Test)
1. **Initialiser l'hôte Alba** (ou utiliser une classe de base partagée).
2. **Préparer les données** (Seed) directement via le `DbContext`.
3. **Appeler l'Endpoint** via `Alba`.
4. **Vérifier le résultat JSON** (BFF Contract).
5. **Vérifier l'état de la DB** (Side Effects).

### Exemple de Test BFF (Login)
```csharp
[Fact]
public async Task Login_Should_Return_BFF_Token_On_Success()
{
    // Arrange
    // Seeder un utilisateur via DbContext ...

    // Act
    var response = await host.PostJson(new LoginRequest("user@test.com", "pass"), "/api/customers/login");

    // Assert
    response.StatusCodeShouldBe(200);
    var loginResult = response.ReadAsJson<LoginResponse>();
    loginResult.Success.Should().BeTrue();
    loginResult.Token.Should().NotBeNullOrEmpty();
}
```

## 5. Checklist de Validation des Tests
- [ ] Le test utilise-t-il EF Core In-Memory pour la rapidité ?
- [ ] Le test vérifie-t-il le format de réponse du BFF ?
- [ ] Le test couvre-t-il les cas de succès ET les cas d'erreur (ex: 401 Unauthorized) ?
- [ ] Les données de test sont-elles isolées des autres tests ?
