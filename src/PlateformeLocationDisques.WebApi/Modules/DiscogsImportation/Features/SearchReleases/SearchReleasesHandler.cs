using Microsoft.EntityFrameworkCore;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Infrastructure;

namespace PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.SearchReleases;

/// <summary>
/// Handler for SearchReleases query.
/// </summary>
public static class SearchReleasesHandler
{
    public static async Task<SearchReleasesResult> Handle(
        SearchReleases query,
        DiscogsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Apply search filter at the database level (optimized for EF Core)
        var filteredQuery = dbContext.Releases
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm;
            // Note: In a real PostgreSQL environment with jsonb, 
            // the artist and label search might need specific EF Core function mappings
            // if they are not automatically translated from the LINQ expressions.
            filteredQuery = filteredQuery.Where(r =>
                r.Title.Contains(searchTerm) ||
                r.Artists.Any(a => a.Name.Contains(searchTerm)) ||
                r.Labels.Any(l => l.CatalogNumber.Contains(searchTerm))
            );
        }

        var totalCount = await filteredQuery.CountAsync(cancellationToken);

        // Apply pagination and projection (BFF pattern)
        var items = await filteredQuery
            .OrderByDescending(r => r.ImportedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new ReleaseSearchItemDto(
                Id: r.Id.ToString(),
                DiscogsId: r.DiscogsId,
                Title: r.Title,
                Year: r.Year,
                Country: r.Country,
                Genres: r.Genres.ToArray(),
                Artists: r.Artists.Select(a => a.Name).ToArray(), // Projection
                Thumb: r.Thumb,
                Format: r.Formats.FirstOrDefault() != null
                    ? r.Formats.OrderBy(f => f.Id).First().Name // Simplified for demonstration
                    : null
            ))
            .ToArrayAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return new SearchReleasesResult(
            Items: items,
            TotalCount: totalCount,
            Page: query.Page,
            PageSize: query.PageSize,
            TotalPages: totalPages
        );
    }
}
