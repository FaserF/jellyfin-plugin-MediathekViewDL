using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MediathekViewDL.Configuration;
using Jellyfin.Plugin.MediathekViewDL.Services;
using Jellyfin.Plugin.MediathekViewDL.Services.Library;
using Jellyfin.Plugin.MediathekViewDL.Services.Media;
using MediathekView.Api.External;
using MediathekView.Api.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.MediathekViewDL.Tests;

public class VirtualLibrarySyncServiceTests : IDisposable
{
    private readonly string _testDir;

    public VirtualLibrarySyncServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "MediathekVirtualLibTest_" + Guid.NewGuid().ToString("N"));
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
    public async Task SyncVirtualLibrary_WhenDisabled_ReturnsZero()
    {
        var config = new PluginConfiguration();
        config.VirtualLibrary.EnableVirtualLibrary = false;

        var configProviderMock = new Mock<IConfigurationProvider>();
        configProviderMock.Setup(c => c.ConfigurationOrNull).Returns(config);

        var service = new VirtualLibrarySyncService(
            NullLogger<VirtualLibrarySyncService>.Instance,
            configProviderMock.Object,
            new Mock<IMediathekViewApiClient>().Object,
            new Mock<IVideoParser>().Object);

        var count = await service.SyncVirtualLibraryAsync(CancellationToken.None);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task SyncTopicAsync_CreatesStrmAndNfoSidecars()
    {
        var config = new PluginConfiguration();
        config.VirtualLibrary.EnableVirtualLibrary = true;
        config.VirtualLibrary.VirtualLibraryPath = _testDir;
        config.VirtualLibrary.MinDurationMinutes = 5;

        var configProviderMock = new Mock<IConfigurationProvider>();
        configProviderMock.Setup(c => c.ConfigurationOrNull).Returns(config);

        var apiClientMock = new Mock<IMediathekViewApiClient>();
        apiClientMock.Setup(a => a.GetItemsAsync(It.IsAny<ApiQueryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResultDto
            {
                Results = new List<ResultItemDto>
                {
                    new ResultItemDto
                    {
                        Id = "rookie_ep1",
                        Topic = "The Rookie",
                        Title = "S01/E01: Frischfleisch",
                        Duration = 2400, // 40 minutes
                        UrlVideo = "https://example.com/stream.mp4",
                        Description = "John Nolan starts his career in LAPD."
                    }
                }
            });

        var videoParserMock = new Mock<IVideoParser>();
        videoParserMock.Setup(v => v.ParseVideoInfo("The Rookie", "S01/E01: Frischfleisch"))
            .Returns(new VideoInfo
            {
                Title = "Frischfleisch",
                SeasonNumber = 1,
                EpisodeNumber = 1,
                IsShow = true
            });

        var service = new VirtualLibrarySyncService(
            NullLogger<VirtualLibrarySyncService>.Instance,
            configProviderMock.Object,
            apiClientMock.Object,
            videoParserMock.Object);

        var synced = await service.SyncTopicAsync("The Rookie", _testDir, CancellationToken.None);
        Assert.Equal(1, synced);

        var season1Dir = Path.Combine(_testDir, "series", "The Rookie", "Season 01");
        Assert.True(Directory.Exists(season1Dir));

        var strmFiles = Directory.GetFiles(season1Dir, "*.strm");
        var nfoFiles = Directory.GetFiles(season1Dir, "*.nfo");

        Assert.Single(strmFiles);
        Assert.Single(nfoFiles);

        var strmContent = await File.ReadAllTextAsync(strmFiles[0]);
        Assert.Equal("https://example.com/stream.mp4", strmContent);

        var nfoContent = await File.ReadAllTextAsync(nfoFiles[0]);
        Assert.Contains("Frischfleisch", nfoContent);
        Assert.Contains("<season>1</season>", nfoContent);
        Assert.Contains("<episode>1</episode>", nfoContent);
    }
}
