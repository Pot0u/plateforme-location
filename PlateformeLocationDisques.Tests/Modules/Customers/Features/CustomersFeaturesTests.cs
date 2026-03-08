using Alba;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PlateformeLocationDisques.WebApi.Modules.Customers.Features.Login;
using PlateformeLocationDisques.WebApi.Modules.Customers.Features.Register;
using PlateformeLocationDisques.WebApi.Modules.Customers.Infrastructure;
using PlateformeLocationDisques.WebApi.Modules.Customers.Domain;

namespace PlateformeLocationDisques.Tests.Modules.Customers.Features;

public class CustomersFeaturesTests
{
    [Fact]
    public async Task Register_And_Login_Should_Work_Together()
    {
        // 1. Setup host with unique in-memory DB
        var dbName = Guid.NewGuid().ToString();
        using var host = await AlbaHost.For<Program>(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace the DB with a unique in-memory instance for this test
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CustomersDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                
                services.AddDbContext<CustomersDbContext>(options => 
                    options.UseInMemoryDatabase(dbName));
            });
        });

        // 2. Register a new customer
        var registerCommand = new RegisterCustomer("test@example.com", "P@ssword123", "Test User");
        var registerResponse = await host.Scenario(_ =>
        {
            _.Post.Json(registerCommand).ToUrl("/api/customers/register");
            _.StatusCodeShouldBeOk();
        });
        
        var registered = registerResponse.ReadAsJson<CustomerRegistered>();
        registered.Should().NotBeNull();
        registered!.Id.Should().NotBeEmpty();

        // 3. Login with the new customer
        var loginRequest = new LoginRequest("test@example.com", "P@ssword123");
        var loginResponse = await host.Scenario(_ =>
        {
            _.Post.Json(loginRequest).ToUrl("/api/customers/login");
            _.StatusCodeShouldBeOk();
        });
        
        var loginResult = loginResponse.ReadAsJson<LoginResponse>();
        loginResult!.Success.Should().BeTrue();
        loginResult.Token.Should().NotBeNullOrEmpty();
        loginResult.Message.Should().Be("Connexion réussie");
    }

    [Fact]
    public async Task Login_Should_Fail_With_Wrong_Credentials()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using var host = await AlbaHost.For<Program>(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<CustomersDbContext>(options => 
                    options.UseInMemoryDatabase(dbName));
            });
        });

        // Act
        var loginRequest = new LoginRequest("nonexistent@example.com", "wrongpass");
        await host.Scenario(_ =>
        {
            _.Post.Json(loginRequest).ToUrl("/api/customers/login");
            _.StatusCodeShouldBe(401);
        });
    }
}
