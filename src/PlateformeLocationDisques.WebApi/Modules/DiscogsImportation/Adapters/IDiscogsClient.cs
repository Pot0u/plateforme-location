namespace PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Adapters;

/// <summary>
/// Adapter interface for Discogs API client.
/// This abstraction allows for easy testing with fake implementations.
/// </summary>
public interface IDiscogsClient
{
    /// <summary>
    /// Retrieves a master release from Discogs by its master ID.
    /// </summary>
    /// <param name="masterId">The Discogs master release ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The master release data as a DTO</returns>
    Task<DiscogsMasterReleaseDto?> GetMasterReleaseAsync(int masterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific release from Discogs by its release ID.
    /// </summary>
    /// <param name="releaseId">The Discogs release ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The release data as a DTO</returns>
    Task<DiscogsReleaseDto?> GetReleaseAsync(int releaseId, CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO representing a Discogs Master Release response.
/// Matches the structure from the Discogs API.
/// </summary>
public record DiscogsMasterReleaseDto(
    int Id,
    string Title,
    int Year,
    string Status,
    string Uri,
    string ResourceUrl,
    string DataQuality,
    int? MainRelease,
    string? MainReleaseUrl,
    int? MostRecentRelease,
    string? MostRecentReleaseUrl,
    string? VersionsUrl,
    int? NumForSale,
    decimal? LowestPrice,
    string? Thumb,
    string? Notes,
    string[] Genres,
    string[] Styles,
    DiscogsArtistDto[] Artists,
    DiscogsTrackDto[] Tracklist,
    DiscogsImageDto[] Images,
    DiscogsVideoDto[] Videos,
    DiscogsCommunityDto? Community,
    DateTime DateAdded,
    DateTime DateChanged
);

/// <summary>
/// DTO representing a Discogs Release response.
/// </summary>
public record DiscogsReleaseDto(
    int Id,
    string Title,
    int Year,
    string Status,
    string Uri,
    string ResourceUrl,
    string DataQuality,
    string? Released,
    string? ReleasedFormatted,
    string? Country,
    string? Notes,
    string? Thumb,
    int? MasterId,
    string? MasterUrl,
    string[] Genres,
    string[] Styles,
    DiscogsArtistDto[] Artists,
    DiscogsLabelDto[] Labels,
    DiscogsFormatDto[] Formats,
    DiscogsTrackDto[] Tracklist,
    DiscogsIdentifierDto[] Identifiers,
    DiscogsImageDto[] Images,
    DiscogsVideoDto[] Videos,
    DiscogsCommunityDto? Community,
    DateTime DateAdded,
    DateTime DateChanged
);

public record DiscogsArtistDto(
    int Id,
    string Name,
    string? Anv,
    string? Join,
    string? Role,
    string? Tracks,
    string ResourceUrl
);

public record DiscogsLabelDto(
    int Id,
    string Name,
    string Catno,
    string EntityType,
    string EntityTypeName,
    string ResourceUrl
);

public record DiscogsFormatDto(
    string Name,
    string Qty,
    string? Text,
    string[]? Descriptions
);

public record DiscogsTrackDto(
    string Position,
    string Type_,
    string Title,
    string? Duration,
    DiscogsArtistDto[]? Artists,
    DiscogsArtistDto[]? ExtraArtists,
    DiscogsSubTrackDto[]? SubTracks
);

public record DiscogsSubTrackDto(
    string Position,
    string Type_,
    string Title,
    string? Duration
);

public record DiscogsIdentifierDto(
    string Type,
    string Value,
    string? Description
);

public record DiscogsImageDto(
    string Type,
    string Uri,
    string Uri150,
    int Width,
    int Height,
    string ResourceUrl
);

public record DiscogsVideoDto(
    string Uri,
    string Title,
    string? Description,
    int? Duration,
    bool Embed
);

public record DiscogsCommunityDto(
    string Status,
    DiscogsRatingDto? Rating,
    int Want,
    int Have,
    DiscogsContributorDto[]? Contributors,
    DiscogsSubmitterDto? Submitter,
    string DataQuality
);

public record DiscogsRatingDto(
    int Count,
    decimal Average
);

public record DiscogsContributorDto(
    string Username,
    string ResourceUrl
);

public record DiscogsSubmitterDto(
    string Username,
    string ResourceUrl
);
