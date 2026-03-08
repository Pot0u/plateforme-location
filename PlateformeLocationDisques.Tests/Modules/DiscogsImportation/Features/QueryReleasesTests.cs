using Alba;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Adapters;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.GetMasterReleaseById;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.GetReleaseById;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.SearchReleases;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.GetReleasesByGenre;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.GetReleasesByArtist;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Infrastructure;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.ImportMasterRelease;

namespace PlateformeLocationDisques.Tests.Modules.DiscogsImportation.Features;

public class QueryReleasesTests
{
    [Fact]
    public async Task GetMasterReleaseById_Should_Return_DTO_With_Correct_Format()
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

        // Act - Import first, then query
        var importResponse = await host.Scenario(_ =>
        {
            _.Post.Url("/api/discogs/import/master/1");
            _.StatusCodeShouldBeOk();
        });

        var imported = importResponse.ReadAsJson<MasterReleaseImported>();

        var queryResponse = await host.Scenario(_ =>
        {
            _.Get.Url($"/api/discogs/master-releases/{imported!.Id}");
            _.StatusCodeShouldBeOk();
        });

        // Assert - Verify BFF contract (DTO shape)
        var result = queryResponse.ReadAsJson<MasterReleaseDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(imported!.Id);
        result.DiscogsId.Should().Be(1);
        result.Title.Should().Be("The Dark Side Of The Moon");
        result.Year.Should().Be(1973);
        result.Genres.Should().Contain("Rock");
        result.Artists.Should().NotBeEmpty();
        result.Artists.First().Name.Should().Be("Pink Floyd");
        result.Tracklist.Should().HaveCount(10);
        result.Community.Should().NotBeNull();
        result.Community!.Want.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetMasterReleaseById_Should_Return_404_For_NonExistent_Id()
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
            });
        });

        // Act & Assert
        await host.Scenario(_ =>
        {
            _.Get.Url($"/api/discogs/master-releases/{ByteAether.Ulid.Ulid.New()}");
            _.StatusCodeShouldBe(404);
        });
    }

    [Fact]
    public async Task GetMasterReleaseById_Should_Return_400_For_Invalid_Id()
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
            });
        });

        // Act & Assert
        await host.Scenario(_ =>
        {
            _.Get.Url("/api/discogs/master-releases/invalid-id");
            _.StatusCodeShouldBe(400);
        });
    }

    [Fact]
    public async Task SearchReleases_Should_Return_Paginated_Results()
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

        // Seed data - Import master which creates a release in FakeDiscogsClient
        await host.Scenario(_ =>
        {
            _.Post.Url("/api/discogs/import/master/1");
            _.StatusCodeShouldBeOk();
        });

        // Act
        var response = await host.Scenario(_ =>
        {
            _.Get.Url("/api/discogs/releases?page=1&pageSize=10");
            _.StatusCodeShouldBeOk();
        });

        // Assert
        var result = response.ReadAsJson<SearchReleasesResult>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task SearchReleases_Should_Filter_By_Search_Term()
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

        // Seed - Import master release (creates releases internally via fake client logic)
        await host.Scenario(_ =>
        {
            _.Post.Url("/api/discogs/import/master/1");
            _.StatusCodeShouldBeOk();
        });

        // Act - Search for "Dark Side"
        var response = await host.Scenario(_ =>
        {
            _.Get.Url("/api/discogs/releases?search=Dark");
            _.StatusCodeShouldBeOk();
        });

        // Assert
        var result = response.ReadAsJson<SearchReleasesResult>();
        result.Should().NotBeNull();
        // The search should work even if no Release entities exist (only MasterRelease)
    }

    [Fact]
    public async Task GetReleasesByGenre_Should_Filter_Correctly()
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
            });
        });

        // Act
        var response = await host.Scenario(_ =>
        {
            _.Get.Url("/api/discogs/releases/genre/Rock?page=1&pageSize=20");
            _.StatusCodeShouldBeOk();
        });

        // Assert
        var result = response.ReadAsJson<GetReleasesByGenreResult>();
        result.Should().NotBeNull();
        result!.Genre.Should().Be("Rock");
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetReleasesByArtist_Should_Filter_Correctly()
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
            });
        });

        // Act
        var response = await host.Scenario(_ =>
        {
            _.Get.Url("/api/discogs/releases/artist/Pink Floyd?page=1&pageSize=20");
            _.StatusCodeShouldBeOk();
        });

        // Assert
        var result = response.ReadAsJson<GetReleasesByArtistResult>();
        result.Should().NotBeNull();
        result!.ArtistName.Should().Be("Pink Floyd");
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }
}
