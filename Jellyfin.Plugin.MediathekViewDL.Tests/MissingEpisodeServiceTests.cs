using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MediathekViewDL.Configuration;
using Jellyfin.Plugin.MediathekViewDL.Configuration.Groups;
using Jellyfin.Plugin.MediathekViewDL.Services;
using Jellyfin.Plugin.MediathekViewDL.Services.Downloading.Models;
using Jellyfin.Plugin.MediathekViewDL.Services.Downloading.Queue;
using Jellyfin.Plugin.MediathekViewDL.Services.Library;
using Jellyfin.Plugin.MediathekViewDL.Services.Media;
using MediathekView.Api.External;
using MediathekView.Api.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.MediathekViewDL.Tests;

public class MissingEpisodeServiceTests : IDisposable
{
    private readonly string _testDir;

    public MissingEpisodeServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "MediathekMissingEpTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public async Task SyncMissingEpisodes_WhenDisabled_ReturnsZero()
    {
        var config = new PluginConfiguration();
        config.MissingEpisodes.EnableMissingEpisodesSync = false;

        var configProviderMock = new Mock<IConfigurationProvider>();
        configProviderMock.Setup(c => c.ConfigurationOrNull).Returns(config);

        var service = new MissingEpisodeService(
            NullLogger<MissingEpisodeService>.Instance,
            configProviderMock.Object,
            new Mock<ILocalMediaScanner>().Object,
            new Mock<IMediathekViewApiClient>().Object,
            new Mock<IVideoParser>().Object,
            new Mock<IFileNameBuilderService>().Object,
            new Mock<IDownloadQueueManager>().Object,
            new Mock<IVideoQualitySelector>().Object);

        var result = await service.SyncMissingEpisodesAsync(CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task SyncSeriesMissingEpisodes_StreamMode_CreatesStrmFile()
    {
        var seriesDir = Path.Combine(_testDir, "The Rookie");
        Directory.CreateDirectory(seriesDir);

        var config = new PluginConfiguration();
        config.MissingEpisodes.EnableMissingEpisodesSync = true;
        config.MissingEpisodes.SyncMode = MissingEpisodeSyncMode.StreamLink;

        var configProviderMock = new Mock<IConfigurationProvider>();
        configProviderMock.Setup(c => c.ConfigurationOrNull).Returns(config);

        var scannerMock = new Mock<ILocalMediaScanner>();
        var localResult = new LocalScanResult();
        scannerMock.Setup(s => s.ScanSubscriptionDirectory(seriesDir, "The Rookie")).Returns(localResult);

        var apiClientMock = new Mock<IMediathekViewApiClient>();
        apiClientMock.Setup(a => a.GetItemsAsync(It.IsAny<ApiQueryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResultDto
            {
                Results = new List<ResultItemDto>
                {
                    new ResultItemDto
                    {
                        Id = "zdf_rookie_s01e01",
                        Topic = "The Rookie",
                        Title = "S01/E01: Neubeginn",
                        UrlVideo = "https://rodlzdf-a.akamaihd.net/rookie_s01e01.mp4"
                    }
                }
            });

        var videoParserMock = new Mock<IVideoParser>();
        videoParserMock.Setup(v => v.ParseVideoInfo("The Rookie", "S01/E01: Neubeginn"))
            .Returns(new VideoInfo
            {
                Title = "Neubeginn",
                SeasonNumber = 1,
                EpisodeNumber = 1,
                IsShow = true
            });

        var service = new MissingEpisodeService(
            NullLogger<MissingEpisodeService>.Instance,
            configProviderMock.Object,
            scannerMock.Object,
            apiClientMock.Object,
            videoParserMock.Object,
            new Mock<IFileNameBuilderService>().Object,
            new Mock<IDownloadQueueManager>().Object,
            new Mock<IVideoQualitySelector>().Object);

        var synced = await service.SyncSeriesMissingEpisodesAsync("The Rookie", seriesDir, CancellationToken.None);
        Assert.Equal(1, synced);

        var season1Dir = Path.Combine(seriesDir, "Season 01");
        Assert.True(Directory.Exists(season1Dir));
        var strmFiles = Directory.GetFiles(season1Dir, "*.strm");
        Assert.Single(strmFiles);
        var content = await File.ReadAllTextAsync(strmFiles[0]);
        Assert.Equal("https://rodlzdf-a.akamaihd.net/rookie_s01e01.mp4", content);
    }
}
