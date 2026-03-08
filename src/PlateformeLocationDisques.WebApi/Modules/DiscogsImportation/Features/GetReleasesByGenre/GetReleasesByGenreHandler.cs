using Microsoft.EntityFrameworkCore;
using PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Infrastructure;

namespace PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.GetReleasesByGenre;

/// <summary>
/// Handler for GetReleasesByGenre query.
/// </summary>
public static class GetReleasesByGenreHandler
{
    public static async Task<GetReleasesByGenreResult> Handle(
        GetReleasesByGenre query,
        DiscogsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Filter releases by genre (case-insensitive)
        var genreLower = query.Genre.ToLower();
        var releasesQuery = dbContext.Releases
            .AsNoTracking()
            .Where(r => r.Genres.Any(g => g.ToLower() == genreLower));

        // Get total count for pagination
        var totalCount = await releasesQuery.CountAsync(cancellationToken);

        // Apply pagination
        var items = await releasesQuery
            .OrderByDescending(r => r.Year)
            .ThenBy(r => r.Title)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new ReleaseByGenreItemDto(
                Id: r.Id.ToString(),
                DiscogsId: r.DiscogsId,
                Title: r.Title,
                Year: r.Year,
                Country: r.Country,
                Genres: r.Genres.ToArray(),
                Styles: r.Styles.ToArray(),
                Artists: r.Artists.Select(a => a.GetDisplayName()).ToArray(),
                Thumb: r.Thumb,
                Format: r.Formats.FirstOrDefault() != null
                    ? r.Formats.First().GetFormattedDescription()
                    : null
            ))
            .ToArrayAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return new GetReleasesByGenreResult(
            Items: items,
            Genre: query.Genre,
            TotalCount: totalCount,
            Page: query.Page,
            PageSize: query.PageSize,
            TotalPages: totalPages
        );
    }
}
