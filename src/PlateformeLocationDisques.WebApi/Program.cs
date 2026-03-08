using Microsoft.EntityFrameworkCore;
using PlateformeLocationDisques.WebApi.Modules.Customers.Features.Login;
using PlateformeLocationDisques.WebApi.Modules.Customers.Features.Register;
using PlateformeLocationDisques.WebApi.Modules.Customers.Infrastructure;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Adapters;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.ImportMasterRelease;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.GetMasterReleaseById;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.GetReleaseById;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.SearchReleases;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.GetReleasesByGenre;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.GetReleasesByArtist;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Infrastructure;
using Wolverine;
using Wolverine.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration EF Core (In-memory pour la démo/bootstrap)
builder.Services.AddDbContext<CustomersDbContext>(options =>
    options.UseInMemoryDatabase("CustomersDb"));

// Configuration EF Core for Discogs Importation Module
var useInMemoryDb = builder.Configuration.GetValue<bool>("UseInMemoryDatabase", true);
builder.Services.AddDbContext<DiscogsDbContext>(options =>
{
    if (useInMemoryDb)
    {
        options.UseInMemoryDatabase("DiscogsDb");
    }
    else
    {
        var connectionString = builder.Configuration.GetConnectionString("DiscogsConnection")
            ?? throw new InvalidOperationException("PostgreSQL connection string 'DiscogsConnection' is not configured.");
        options.UseNpgsql(connectionString);
    }
});

// Register Discogs API Client (Adapter Pattern)
// Use FakeDiscogsClient in Development, real client in Production
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IDiscogsClient, FakeDiscogsClient>();
}
else
{
    builder.Services.AddHttpClient<IDiscogsClient, DiscogsApiClient>();
}

// Configuration Wolverine
builder.Host.UseWolverine(opts =>
{
    // Auto-découverte des handlers dans l'assemblage
    opts.UseEntityFrameworkCoreTransactions();
});

// Swagger / OpenAPI (Optionnel mais recommandé pour API First)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(); // .NET 10 style

var app = builder.Build();

// Run migrations in non-development environments
if (!app.Environment.IsDevelopment() && !useInMemoryDb)
{
    using var scope = app.Services.CreateScope();
    var discogsDb = scope.ServiceProvider.GetRequiredService<DiscogsDbContext>();
    await discogsDb.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enregistrement des Endpoints (Modular Monolith style)
// Customers Module
app.MapRegisterCustomer();
app.MapLogin();

// Discogs Importation Module
app.MapImportMasterRelease();
app.MapGetMasterReleaseById();
app.MapGetReleaseById();
app.MapSearchReleases();
app.MapGetReleasesByGenre();
app.MapGetReleasesByArtist();

app.MapGet("/", () => "Plateforme Location Disques API");

app.Run();
