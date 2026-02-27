using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Vara.Api.Services.YouTube;
using Vara.Tests.Helpers;

namespace Vara.Tests.Services.YouTube;

public class YouTubeClientChannelTests
{
    // -------------------------------------------------------------------------
    // Helpers (mirrors YouTubeClientTests)
    // -------------------------------------------------------------------------

    private static IConfiguration MakeConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["YouTube:ApiKey"] = "test-key" })
            .Build();

    private static IHttpClientFactory MakeFactory(params HttpResponseMessage[] responses)
    {
        var queue = new Queue<HttpResponseMessage>(responses);
        var handler = new FakeHttpMessageHandler(_ => queue.Dequeue());
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("YouTube").Returns(client);
        return factory;
    }

    private static YouTubeClient BuildSut(IHttpClientFactory factory) =>
        new(factory, Substitute.For<ITranscriptFetcher>(), MakeConfig(), NullLogger<YouTubeClient>.Instance);

    private static HttpResponseMessage OkJson(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json) };

    // ---- JSON builders ----

    private static string ChannelListJson(
        string id = "UCxxxxxxxxxxxxxxxxxxxxxx",
        string title = "Test Channel",
        string customUrl = "@testchannel",
        string thumbnail = "https://img.jpg",
        string subscribers = "100000",
        string videoCount = "200",
        string viewCount = "5000000") => $$"""
        {
          "items": [{
            "id": "{{id}}",
            "snippet": {
              "title": "{{title}}",
              "customUrl": "{{customUrl}}",
              "thumbnails": { "high": { "url": "{{thumbnail}}" } }
            },
            "statistics": {
              "subscriberCount": "{{subscribers}}",
              "videoCount": "{{videoCount}}",
              "viewCount": "{{viewCount}}"
            }
          }]
        }
        """;

    private static string EmptyChannelListJson() => """{"items":[]}""";

    private static string ContentDetailsJson(string uploadsPlaylistId) => $$"""
        {
          "items": [{
            "id": "UCxxxxxxxxxxxxxxxxxxxxxx",
            "contentDetails": {
              "relatedPlaylists": { "uploads": "{{uploadsPlaylistId}}" }
            }
          }]
        }
        """;

    private static string EmptyContentDetailsJson() => """{"items":[]}""";

    private static string PlaylistItemsJson(string? nextPageToken, params string[] videoIds)
    {
        var items = string.Join(",", videoIds.Select(id =>
            "{\"snippet\":{\"resourceId\":{\"videoId\":\"" + id + "\"}}}"));
        var json = "{\"items\":[" + items + "]";
        if (nextPageToken is not null)
            json += ",\"nextPageToken\":\"" + nextPageToken + "\"";
        return json + "}";
    }

    // -------------------------------------------------------------------------
    // GetChannelAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetChannelAsync_ByHandle_ReturnsMappedMetadata()
    {
        var factory = MakeFactory(OkJson(ChannelListJson(
            id: "UCxxxxxxxxxxxxxxxxxxxxxx",
            title: "Test Channel",
            subscribers: "100000",
            videoCount: "200",
            viewCount: "5000000")));
        var sut = BuildSut(factory);

        var result = await sut.GetChannelAsync("@testchannel");

        Assert.NotNull(result);
        Assert.Equal("UCxxxxxxxxxxxxxxxxxxxxxx", result!.YoutubeChannelId);
        Assert.Equal("Test Channel", result.DisplayName);
        Assert.Equal(100000L, result.SubscriberCount);
        Assert.Equal(200, result.VideoCount);
        Assert.Equal(5000000L, result.TotalViewCount);
        Assert.Equal("https://img.jpg", result.ThumbnailUrl);
    }

    [Fact]
    public async Task GetChannelAsync_ById_ReturnsMappedMetadata()
    {
        var factory = MakeFactory(OkJson(ChannelListJson(id: "UCxxxxxxxxxxxxxxxxxxxxxx")));
        var sut = BuildSut(factory);

        // 24-char "UC..." ID — should resolve as channel ID, not handle
        var result = await sut.GetChannelAsync("UCxxxxxxxxxxxxxxxxxxxxxx");

        Assert.NotNull(result);
        Assert.Equal("UCxxxxxxxxxxxxxxxxxxxxxx", result!.YoutubeChannelId);
    }

    [Fact]
    public async Task GetChannelAsync_EmptyItems_ReturnsNull()
    {
        var factory = MakeFactory(OkJson(EmptyChannelListJson()));
        var sut = BuildSut(factory);

        var result = await sut.GetChannelAsync("@nobody");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetChannelAsync_YouTubeUrl_ResolvesHandle()
    {
        var factory = MakeFactory(OkJson(ChannelListJson(id: "UCxxxxxxxxxxxxxxxxxxxxxx")));
        var sut = BuildSut(factory);

        // Should strip the URL prefix and resolve via forHandle
        var result = await sut.GetChannelAsync("https://www.youtube.com/@testchannel");

        Assert.NotNull(result);
    }

    // -------------------------------------------------------------------------
    // GetChannelVideoIdsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetChannelVideoIdsAsync_SinglePage_ReturnsAllIds()
    {
        var factory = MakeFactory(
            OkJson(ContentDetailsJson("UUxxxxxxxxxxxxxxxxxxxxxx")),
            OkJson(PlaylistItemsJson(null, "vid1", "vid2", "vid3")));
        var sut = BuildSut(factory);

        var ids = new List<string>();
        await foreach (var id in sut.GetChannelVideoIdsAsync("UCxxxxxxxxxxxxxxxxxxxxxx"))
            ids.Add(id);

        Assert.Equal(["vid1", "vid2", "vid3"], ids);
    }

    [Fact]
    public async Task GetChannelVideoIdsAsync_MultiPage_PaginatesCorrectly()
    {
        var factory = MakeFactory(
            OkJson(ContentDetailsJson("UUxxxxxxxxxxxxxxxxxxxxxx")),
            OkJson(PlaylistItemsJson("page2token", "vid1", "vid2")),
            OkJson(PlaylistItemsJson(null, "vid3", "vid4")));
        var sut = BuildSut(factory);

        var ids = new List<string>();
        await foreach (var id in sut.GetChannelVideoIdsAsync("UCxxxxxxxxxxxxxxxxxxxxxx"))
            ids.Add(id);

        Assert.Equal(["vid1", "vid2", "vid3", "vid4"], ids);
    }

    [Fact]
    public async Task GetChannelVideoIdsAsync_NoUploadsPlaylist_ReturnsEmpty()
    {
        var factory = MakeFactory(OkJson(EmptyContentDetailsJson()));
        var sut = BuildSut(factory);

        var ids = new List<string>();
        await foreach (var id in sut.GetChannelVideoIdsAsync("UCxxxxxxxxxxxxxxxxxxxxxx"))
            ids.Add(id);

        Assert.Empty(ids);
    }
}
