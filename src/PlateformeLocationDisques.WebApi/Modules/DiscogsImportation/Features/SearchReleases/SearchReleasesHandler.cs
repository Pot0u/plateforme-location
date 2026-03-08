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
        var releasesQuery = dbContext.Releases.AsNoTracking();

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            releasesQuery = releasesQuery.Where(r =>
                r.Title.ToLower().Contains(searchTerm) ||
                r.Artists.Any(a => a.Name.ToLower().Contains(searchTerm)) ||
                r.Labels.Any(l => l.CatalogNumber.ToLower().Contains(searchTerm))
            );
        }

        // Get total count for pagination
        var totalCount = await releasesQuery.CountAsync(cancellationToken);

        // Apply pagination
        var items = await releasesQuery
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
                Artists: r.Artists.Select(a => a.GetDisplayName()).ToArray(),
                Thumb: r.Thumb,
                Format: r.Formats.FirstOrDefault() != null
                    ? r.Formats.First().GetFormattedDescription()
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
