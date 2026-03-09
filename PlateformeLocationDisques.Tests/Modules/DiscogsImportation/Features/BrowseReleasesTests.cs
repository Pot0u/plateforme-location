using Alba;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Adapters;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.GetAllGenres;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.GetAllArtists;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.SearchReleases;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Infrastructure;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.ImportMasterRelease;

namespace PlateformeLocationDisques.Tests.Modules.DiscogsImportation.Features;

public class BrowseReleasesTests
{
    [Fact]
    public async Task GetAllGenres_Should_Return_Genres_With_Counts_And_HATEOAS_Links()
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

        // Seed data - Import master which creates a release
        await host.Scenario(_ =>
        {
            _.Post.Url("/api/discogs/import/master/1");
            _.StatusCodeShouldBeOk();
        });

        // Act
        var response = await host.Scenario(_ =>
        {
            _.Get.Url("/api/discogs/genres");
            _.StatusCodeShouldBeOk();
        });

        // Assert
        var result = response.ReadAsJson<GetAllGenresResult>();
        result.Should().NotBeNull();
        result!.Genres.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThan(0);

        // Verify HATEOAS links at collection level
        result.Links.Should().NotBeNull();
        result.Links.Should().ContainKey("self");
        result.Links.Should().ContainKey("releases");
        result.Links.Should().ContainKey("artists");
        result.Links["self"]!.Href.Should().Be("/api/discogs/genres");

        // Verify each genre has HATEOAS links
        var firstGenre = result.Genres.First();
        firstGenre.Links.Should().NotBeNull();
        firstGenre.Links.Should().ContainKey("self");
        firstGenre.Links.Should().ContainKey("byGenre");
        firstGenre.Links.Should().ContainKey("releases");
        firstGenre.ReleaseCount.Should().BeGreaterThan(0);
    }


    [Fact]
    public async Task GetAllArtists_Should_Return_Artists_With_Counts_And_HATEOAS_Links()
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

        // Seed data - Import master which creates a release
        await host.Scenario(_ =>
        {
            _.Post.Url("/api/discogs/import/master/1");
            _.StatusCodeShouldBeOk();
        });

        // Act
        var response = await host.Scenario(_ =>
        {
            _.Get.Url("/api/discogs/artists");
            _.StatusCodeShouldBeOk();
        });

        // Assert
        var result = response.ReadAsJson<GetAllArtistsResult>();
        result.Should().NotBeNull();
        result!.Artists.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThan(0);

        // Verify HATEOAS links at collection level
        result.Links.Should().NotBeNull();
        result.Links.Should().ContainKey("self");
        result.Links.Should().ContainKey("releases");
        result.Links.Should().ContainKey("genres");
        result.Links["self"]!.Href.Should().Be("/api/discogs/artists");

        // Verify each artist has HATEOAS links
        var firstArtist = result.Artists.First();
        firstArtist.Links.Should().NotBeNull();
        firstArtist.Links.Should().ContainKey("self");
        firstArtist.Links.Should().ContainKey("byArtist");
        firstArtist.Links.Should().ContainKey("releases");
        firstArtist.ReleaseCount.Should().BeGreaterThan(0);
    }


    [Fact]
    public async Task SearchReleases_Should_Return_HATEOAS_Links_With_Pagination()
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

        // Seed data
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
        result!.Links.Should().NotBeNull();

        // Verify pagination links
        result.Links.Should().ContainKey("self");
        result.Links.Should().ContainKey("first");
        result.Links.Should().ContainKey("last");
        result.Links["self"]!.Href.Should().Contain("page=1");
        result.Links["self"]!.Href.Should().Contain("pageSize=10");

        // Verify browse links
        result.Links.Should().ContainKey("genres");
        result.Links.Should().ContainKey("artists");
        result.Links.Should().ContainKey("releases");

        // Verify each item has HATEOAS links
        if (result.Items.Any())
        {
            var firstItem = result.Items.First();
            firstItem.Links.Should().NotBeNull();
            firstItem.Links.Should().ContainKey("self");
            firstItem.Links["self"]!.Href.Should().Contain("/api/discogs/releases/");
        }
    }

    [Fact]
    public async Task SearchReleases_With_SearchTerm_Should_Include_Search_In_Pagination_Links()
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

        // Seed data
        await host.Scenario(_ =>
        {
            _.Post.Url("/api/discogs/import/master/1");
            _.StatusCodeShouldBeOk();
        });

        // Act - Search with search term
        var response = await host.Scenario(_ =>
        {
            _.Get.Url("/api/discogs/releases?search=Dark&page=1&pageSize=10");
            _.StatusCodeShouldBeOk();
        });

        // Assert
        var result = response.ReadAsJson<SearchReleasesResult>();
        result.Should().NotBeNull();
        result!.Links.Should().NotBeNull();

        // Verify pagination links include search parameter
        result.Links["self"]!.Href.Should().Contain("search=Dark");
        result.Links["first"]!.Href.Should().Contain("search=Dark");
        result.Links["last"]!.Href.Should().Contain("search=Dark");
    }

    [Fact]
    public async Task Release_Items_Should_Include_Links_To_Genre_And_Artist_Filters()
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

        // Seed data - Import master which creates a release
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
        result!.Items.Should().NotBeEmpty();

        var firstItem = result.Items.First();
        firstItem.Links.Should().NotBeNull();
        firstItem.Links.Should().ContainKey("self");

        // Should have genre filter link if release has genres
        if (firstItem.Genres.Any())
        {
            firstItem.Links.Should().ContainKey("byGenre");
            firstItem.Links["byGenre"]!.Href.Should().Contain("/api/discogs/releases/genre/");
        }

        // Should have artist filter link if release has artists
        if (firstItem.Artists.Any())
        {
            firstItem.Links.Should().ContainKey("byArtist");
            firstItem.Links["byArtist"]!.Href.Should().Contain("/api/discogs/releases/artist/");
        }
    }
}
