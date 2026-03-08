namespace PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Features.SearchReleases;

/// <summary>
/// Query to search releases by title, artist name, or catalog number.
/// </summary>
public record SearchReleases(
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 20
);

/// <summary>
/// Paginated search results.
/// </summary>
public record SearchReleasesResult(
    ReleaseSearchItemDto[] Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

/// <summary>
/// Lightweight DTO for search results list.
/// </summary>
public record ReleaseSearchItemDto(
    string Id,
    int DiscogsId,
    string Title,
    int Year,
    string? Country,
    string[] Genres,
    string[] Artists,
    string? Thumb,
    string? Format
);
