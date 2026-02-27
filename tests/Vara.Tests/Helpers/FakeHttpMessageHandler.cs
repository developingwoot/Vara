namespace Vara.Tests.Helpers;

/// <summary>
/// Minimal HttpMessageHandler for unit tests. Intercepts outgoing requests and
/// returns a pre-built response supplied by the caller via a delegate.
/// </summary>
public sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => Task.FromResult(handler(request));
}
