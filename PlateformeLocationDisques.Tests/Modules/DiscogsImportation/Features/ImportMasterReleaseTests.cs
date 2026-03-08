using Alba;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Adapters;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.ImportMasterRelease;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Infrastructure;

namespace PlateformeLocationDisques.Tests.Modules.DiscogsImportation.Features;

public class ImportMasterReleaseTests
{
    [Fact]
    public async Task ImportMasterRelease_Should_Import_From_FakeClient_Successfully()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using var host = await AlbaHost.For<Program>(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace DiscogsDbContext with unique in-memory instance
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DiscogsDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<DiscogsDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));

                // Ensure FakeDiscogsClient is used (should already be the case in test environment)
                var clientDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDiscogsClient));
                if (clientDescriptor != null) services.Remove(clientDescriptor);
                services.AddSingleton<IDiscogsClient, FakeDiscogsClient>();
            });
        });

        // Act - Import master release with ID 1 (Pink Floyd from FakeDiscogsClient)
        var response = await host.Scenario(_ =>
        {
            _.Post.Url("/api/discogs/import/master/1");
            _.StatusCodeShouldBeOk();
        });

        // Assert - Verify BFF contract
        var result = response.ReadAsJson<MasterReleaseImported>();
        result.Should().NotBeNull();
        result!.DiscogsId.Should().Be(1);
        result.Title.Should().Be("The Dark Side Of The Moon");
        result.AlreadyExisted.Should().BeFalse();
        result.Id.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ImportMasterRelease_Should_Return_Existing_If_Already_Imported()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using var host = await AlbaHost.For<Program>(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DiscogsDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<DiscogsDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));

                var clientDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDiscogsClient));
                if (clientDescriptor != null) services.Remove(clientDescriptor);
                services.AddSingleton<IDiscogsClient, FakeDiscogsClient>();
            });
        });

        // Act - Import twice
        var firstResponse = await host.Scenario(_ =>
        {
            _.Post.Url("/api/discogs/import/master/1");
            _.StatusCodeShouldBeOk();
        });

        var firstResult = firstResponse.ReadAsJson<MasterReleaseImported>();

        var secondResponse = await host.Scenario(_ =>
        {
            _.Post.Url("/api/discogs/import/master/1");
            _.StatusCodeShouldBeOk();
        });

        // Assert - Second import should return the same ID and AlreadyExisted = true
        var secondResult = secondResponse.ReadAsJson<MasterReleaseImported>();
        secondResult!.Id.Should().Be(firstResult!.Id);
        secondResult.DiscogsId.Should().Be(1);
        secondResult.AlreadyExisted.Should().BeTrue();
    }

    [Fact]
    public async Task ImportMasterRelease_Should_Persist_Complete_Data()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using var host = await AlbaHost.For<Program>(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DiscogsDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<DiscogsDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));

                var clientDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDiscogsClient));
                if (clientDescriptor != null) services.Remove(clientDescriptor);
                services.AddSingleton<IDiscogsClient, FakeDiscogsClient>();
            });
        });

        // Act - Import master release
        var response = await host.Scenario(_ =>
        {
            _.Post.Url("/api/discogs/import/master/1");
            _.StatusCodeShouldBeOk();
        });

        var result = response.ReadAsJson<MasterReleaseImported>();

        // Assert - Verify data was persisted correctly
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DiscogsDbContext>();
        var masterRelease = await dbContext.MasterReleases
            .FirstOrDefaultAsync(m => m.DiscogsId == 1);

        masterRelease.Should().NotBeNull();
        masterRelease!.Title.Should().Be("The Dark Side Of The Moon");
        masterRelease.Year.Should().Be(1973);
        masterRelease.Genres.Should().Contain("Rock");
        masterRelease.Artists.Should().NotBeEmpty();
        masterRelease.Artists.First().Name.Should().Be("Pink Floyd");
        masterRelease.Tracklist.Should().HaveCount(10);
    }
}
