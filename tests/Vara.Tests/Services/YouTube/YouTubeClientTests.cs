using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Vara.Api.Services.YouTube;
using Vara.Tests.Helpers;

namespace Vara.Tests.Services.YouTube;

public class YouTubeClientTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static IConfiguration MakeConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["YouTube:ApiKey"] = "test-key" })
            .Build();

    /// <summary>
    /// Returns a factory whose single HttpClient dequeues responses in order.
    /// Search + GetVideo calls each consume one response from the queue.
    /// </summary>
    private static IHttpClientFactory MakeFactory(params HttpResponseMessage[] responses)
    {
        var queue = new Queue<HttpResponseMessage>(responses);
        var handler = new FakeHttpMessageHandler(_ => queue.Dequeue());
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("YouTube").Returns(client);
        return factory;
    }

    private static YouTubeClient BuildSut(IHttpClientFactory factory, ITranscriptFetcher? transcript = null) =>
        new(factory, transcript ?? Substitute.For<ITranscriptFetcher>(), MakeConfig(), NullLogger<YouTubeClient>.Instance);

    private static HttpResponseMessage OkJson(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json) };

    // ---- JSON builders ----

    private static string SearchJson(params string[] videoIds)
    {
        var items = string.Join(",", videoIds.Select(id => $$$"""{"id":{"videoId":"{{{id}}}"}}"""));
        return $$$"""{"items":[{{{items}}}]}""";
    }

    private static string VideoListJson(
        string id,
        string title = "Test Video",
        string duration = "PT1M30S",
        string views = "1000",
        string likes = "50",
        string comments = "5",
        string thumbnail = "https://img.jpg")
    {
        return $$$"""
        {
          "items": [{
            "id": "{{{id}}}",
            "snippet": {
              "title": "{{{title}}}",
              "description": "A description",
              "channelTitle": "Test Channel",
              "channelId": "UC1234",
              "publishedAt": "2024-06-01T00:00:00Z",
              "thumbnails": { "high": { "url": "{{{thumbnail}}}" } }
            },
            "statistics": {
              "viewCount": "{{{views}}}",
              "likeCount": "{{{likes}}}",
              "commentCount": "{{{comments}}}"
            },
            "contentDetails": { "duration": "{{{duration}}}" }
          }]
        }
        """;
    }

    // -------------------------------------------------------------------------
    // SearchAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SearchAsync_MapsMetadataCorrectly()
    {
        var factory = MakeFactory(
            OkJson(SearchJson("abc123")),
            OkJson(VideoListJson("abc123", title: "Cats 101", views: "5000", likes: "100", comments: "10")));
        var sut = BuildSut(factory);

        var results = await sut.SearchAsync("cats");

        Assert.Single(results);
        var v = results[0];
        Assert.Equal("abc123", v.YoutubeId);
        Assert.Equal("Cats 101", v.Title);
        Assert.Equal(90, v.DurationSeconds);       // PT1M30S
        Assert.Equal(5000L, v.ViewCount);
        Assert.Equal(100, v.LikeCount);
        Assert.Equal(10, v.CommentCount);
        Assert.Equal("https://img.jpg", v.ThumbnailUrl);
        Assert.Equal("Test Channel", v.ChannelName);
        Assert.Equal("UC1234", v.ChannelId);
    }

    [Fact]
    public async Task SearchAsync_NullItems_ReturnsEmptyList()
    {
        var factory = MakeFactory(OkJson("""{"items":null}"""));
        var sut = BuildSut(factory);

        var results = await sut.SearchAsync("nothing");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_MultipleVideos_ReturnsAll()
    {
        var searchJson = SearchJson("vid1", "vid2");
        var videosJson = $$"""
        {
          "items": [
            {
              "id": "vid1",
              "snippet": { "title": "Video 1", "thumbnails": { "high": { "url": "" } } },
              "statistics": { "viewCount": "10", "likeCount": "1", "commentCount": "0" },
              "contentDetails": { "duration": "PT30S" }
            },
            {
              "id": "vid2",
              "snippet": { "title": "Video 2", "thumbnails": { "high": { "url": "" } } },
              "statistics": { "viewCount": "20", "likeCount": "2", "commentCount": "0" },
              "contentDetails": { "duration": "PT1M" }
            }
          ]
        }
        """;
        var factory = MakeFactory(OkJson(searchJson), OkJson(videosJson));
        var sut = BuildSut(factory);

        var results = await sut.SearchAsync("test");

        Assert.Equal(2, results.Count);
        Assert.Equal("vid1", results[0].YoutubeId);
        Assert.Equal("vid2", results[1].YoutubeId);
    }

    // -------------------------------------------------------------------------
    // GetVideoAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetVideoAsync_Found_ReturnsMappedMetadata()
    {
        var factory = MakeFactory(OkJson(VideoListJson("vid1")));
        var sut = BuildSut(factory);

        var result = await sut.GetVideoAsync("vid1");

        Assert.NotNull(result);
        Assert.Equal("vid1", result!.YoutubeId);
    }

    [Fact]
    public async Task GetVideoAsync_EmptyItems_ReturnsNull()
    {
        var factory = MakeFactory(OkJson("""{"items":[]}"""));
        var sut = BuildSut(factory);

        var result = await sut.GetVideoAsync("missing");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("PT30S", 30)]
    [InlineData("PT1M30S", 90)]
    [InlineData("PT2H3M4S", 7384)]
    [InlineData("PT1H", 3600)]
    public async Task GetVideoAsync_ParsesIsoDuration(string iso, int expectedSeconds)
    {
        var factory = MakeFactory(OkJson(VideoListJson("vid1", duration: iso)));
        var sut = BuildSut(factory);

        var result = await sut.GetVideoAsync("vid1");

        Assert.NotNull(result);
        Assert.Equal(expectedSeconds, result!.DurationSeconds);
    }

    [Fact]
    public async Task GetVideoAsync_InvalidDuration_LeavesDurationNull()
    {
        var factory = MakeFactory(OkJson(VideoListJson("vid1", duration: "INVALID")));
        var sut = BuildSut(factory);

        var result = await sut.GetVideoAsync("vid1");

        Assert.NotNull(result);
        Assert.Null(result!.DurationSeconds);
    }

    [Fact]
    public async Task GetVideoAsync_NonNumericStats_DefaultsToZero()
    {
        var json = """
        {
          "items": [{
            "id": "vid1",
            "snippet": { "title": "T", "thumbnails": {} },
            "statistics": { "viewCount": "n/a", "likeCount": "", "commentCount": null },
            "contentDetails": { "duration": "PT1S" }
          }]
        }
        """;
        var factory = MakeFactory(OkJson(json));
        var sut = BuildSut(factory);

        var result = await sut.GetVideoAsync("vid1");

        Assert.NotNull(result);
        Assert.Equal(0L, result!.ViewCount);
        Assert.Equal(0, result.LikeCount);
        Assert.Equal(0, result.CommentCount);
    }

    // -------------------------------------------------------------------------
    // GetTranscriptAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetTranscriptAsync_DelegatesToTranscriptFetcher()
    {
        var fetcher = Substitute.For<ITranscriptFetcher>();
        fetcher.FetchAsync("vid1", Arg.Any<CancellationToken>()).Returns("transcript text");
        var factory = Substitute.For<IHttpClientFactory>();
        var sut = BuildSut(factory, fetcher);

        var result = await sut.GetTranscriptAsync("vid1");

        Assert.Equal("transcript text", result);
        await fetcher.Received(1).FetchAsync("vid1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTranscriptAsync_FetcherReturnsNull_ReturnsNull()
    {
        var fetcher = Substitute.For<ITranscriptFetcher>();
        fetcher.FetchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var factory = Substitute.For<IHttpClientFactory>();
        var sut = BuildSut(factory, fetcher);

        var result = await sut.GetTranscriptAsync("vid1");

        Assert.Null(result);
    }
}
