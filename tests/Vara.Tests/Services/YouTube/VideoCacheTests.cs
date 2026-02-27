using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Vara.Api.Services.YouTube;

namespace Vara.Tests.Services.YouTube;

public class VideoCacheTests
{
    private readonly IYouTubeClient _inner = Substitute.For<IYouTubeClient>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private VideoCache BuildSut() =>
        new(_inner, _cache, NullLogger<VideoCache>.Instance);

    // -------------------------------------------------------------------------
    // SearchAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SearchAsync_CacheMiss_CallsInnerAndStoresResult()
    {
        var expected = new List<VideoMetadata>
        {
            new("abc123", "Title", null, null, null, null, null, 0, 0, 0, null)
        };
        _inner.SearchAsync("cats", 10, Arg.Any<CancellationToken>()).Returns(expected);
        var sut = BuildSut();

        var result = await sut.SearchAsync("cats");

        Assert.Equal(expected, result);
        await _inner.Received(1).SearchAsync("cats", 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_CacheHit_DoesNotCallInnerAgain()
    {
        var expected = new List<VideoMetadata>
        {
            new("abc123", "Title", null, null, null, null, null, 0, 0, 0, null)
        };
        _inner.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expected);
        var sut = BuildSut();

        await sut.SearchAsync("dogs");
        var result = await sut.SearchAsync("dogs"); // should come from cache

        Assert.Equal(expected, result);
        await _inner.Received(1)
            .SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_DifferentKeywords_EachCallsInner()
    {
        _inner.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var sut = BuildSut();

        await sut.SearchAsync("cats");
        await sut.SearchAsync("dogs");

        await _inner.Received(1).SearchAsync("cats", 10, Arg.Any<CancellationToken>());
        await _inner.Received(1).SearchAsync("dogs", 10, Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // GetVideoAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetVideoAsync_CacheMiss_CallsInner()
    {
        var meta = new VideoMetadata("vid1", "Title", null, null, null, null, null, 0, 0, 0, null);
        _inner.GetVideoAsync("vid1", Arg.Any<CancellationToken>()).Returns(meta);
        var sut = BuildSut();

        var result = await sut.GetVideoAsync("vid1");

        Assert.Equal(meta, result);
        await _inner.Received(1).GetVideoAsync("vid1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetVideoAsync_CacheHit_DoesNotCallInnerAgain()
    {
        var meta = new VideoMetadata("vid1", "Title", null, null, null, null, null, 0, 0, 0, null);
        _inner.GetVideoAsync("vid1", Arg.Any<CancellationToken>()).Returns(meta);
        var sut = BuildSut();

        await sut.GetVideoAsync("vid1");
        await sut.GetVideoAsync("vid1"); // second call from cache

        await _inner.Received(1).GetVideoAsync("vid1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetVideoAsync_NullResult_NotCached_CallsInnerEachTime()
    {
        _inner.GetVideoAsync("missing", Arg.Any<CancellationToken>()).Returns((VideoMetadata?)null);
        var sut = BuildSut();

        await sut.GetVideoAsync("missing");
        await sut.GetVideoAsync("missing");

        await _inner.Received(2).GetVideoAsync("missing", Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // GetTranscriptAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetTranscriptAsync_AlwaysDelegatesToInner_NotCached()
    {
        _inner.GetTranscriptAsync("vid1", Arg.Any<CancellationToken>()).Returns("transcript");
        var sut = BuildSut();

        var result1 = await sut.GetTranscriptAsync("vid1");
        var result2 = await sut.GetTranscriptAsync("vid1");

        Assert.Equal("transcript", result1);
        Assert.Equal("transcript", result2);
        await _inner.Received(2).GetTranscriptAsync("vid1", Arg.Any<CancellationToken>());
    }
}
