using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.MediathekViewDL.Providers;

/// <summary>
/// Registers the Mediathek ID provider for movies.
/// </summary>
public class MediathekMovieExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "Mediathek";

    /// <inheritdoc />
    public string Key => "MediathekId";

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Movie;

    /// <inheritdoc />
    public string? UrlFormatString => null;

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Movie;
}

/// <summary>
/// Registers the Mediathek ID provider for series.
/// </summary>
public class MediathekSeriesExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "Mediathek";

    /// <inheritdoc />
    public string Key => "MediathekId";

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Series;

    /// <inheritdoc />
    public string? UrlFormatString => null;

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Series;
}

/// <summary>
/// Registers the Mediathek ID provider for episodes.
/// </summary>
public class MediathekEpisodeExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "Mediathek";

    /// <inheritdoc />
    public string Key => "MediathekId";

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Episode;

    /// <inheritdoc />
    public string? UrlFormatString => null;

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Episode;
}
