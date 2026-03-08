using Microsoft.EntityFrameworkCore;
using PlateformeLocationDisques.WebApi.Modules.Customers.Features.Login;
using PlateformeLocationDisques.WebApi.Modules.Customers.Features.Register;
using PlateformeLocationDisques.WebApi.Modules.Customers.Infrastructure;
using Wolverine;
using Wolverine.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration EF Core (In-memory pour la démo/bootstrap)
builder.Services.AddDbContext<CustomersDbContext>(options =>
    options.UseInMemoryDatabase("CustomersDb"));

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enregistrement des Endpoints (Modular Monolith style)
app.MapRegisterCustomer();
app.MapLogin();

app.MapGet("/", () => "Plateforme Location Disques API");

app.Run();
