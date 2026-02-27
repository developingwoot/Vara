using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Vara.Api.Services.YouTube;
using Vara.Tests.Helpers;

namespace Vara.Tests.Services.YouTube;

public class TranscriptFetcherTests
{
    private static IHttpClientFactory MakeFactory(HttpResponseMessage response)
    {
        var handler = new FakeHttpMessageHandler(_ => response);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.youtube.com") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("YouTube").Returns(client);
        return factory;
    }

    private static TranscriptFetcher BuildSut(HttpResponseMessage response) =>
        new(MakeFactory(response), NullLogger<TranscriptFetcher>.Instance);

    // -------------------------------------------------------------------------
    // Non-success responses
    // -------------------------------------------------------------------------

    [Fact]
    public async Task FetchAsync_NotFoundStatus_ReturnsNull()
    {
        var sut = BuildSut(new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await sut.FetchAsync("vid1");

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchAsync_ForbiddenStatus_ReturnsNull()
    {
        var sut = BuildSut(new HttpResponseMessage(HttpStatusCode.Forbidden));

        var result = await sut.FetchAsync("vid1");

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // Empty / missing content
    // -------------------------------------------------------------------------

    [Fact]
    public async Task FetchAsync_EmptyEventsList_ReturnsNull()
    {
        var sut = BuildSut(OkJson("""{"events":[]}"""));

        var result = await sut.FetchAsync("vid1");

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchAsync_NullEvents_ReturnsNull()
    {
        var sut = BuildSut(OkJson("{}"));

        var result = await sut.FetchAsync("vid1");

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // Valid transcripts
    // -------------------------------------------------------------------------

    [Fact]
    public async Task FetchAsync_SingleSegment_ReturnsText()
    {
        var json = """{"events":[{"segs":[{"utf8":"Hello world"}]}]}""";
        var sut = BuildSut(OkJson(json));

        var result = await sut.FetchAsync("vid1");

        Assert.Equal("Hello world", result);
    }

    [Fact]
    public async Task FetchAsync_MultipleEvents_ConcatenatesWithSpaces()
    {
        var json = """
        {
          "events": [
            {"segs": [{"utf8": "Hello"}]},
            {"segs": [{"utf8": "world"}]},
            {"segs": [{"utf8": "foo"}]}
          ]
        }
        """;
        var sut = BuildSut(OkJson(json));

        var result = await sut.FetchAsync("vid1");

        Assert.Equal("Hello world foo", result);
    }

    [Fact]
    public async Task FetchAsync_EventsWithNullSegs_SkipsNullSegments()
    {
        var json = """
        {
          "events": [
            {"segs": null},
            {"segs": [{"utf8": "valid"}]}
          ]
        }
        """;
        var sut = BuildSut(OkJson(json));

        var result = await sut.FetchAsync("vid1");

        Assert.Equal("valid", result);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static HttpResponseMessage OkJson(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json) };
}
