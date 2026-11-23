using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace web.test.TestSupport;

/// <summary>
/// Serves canned responses by request path (e.g. "/data/missions.json"); anything else is a 404.
/// Counts requests per path so tests can check caching / repeat-fetch behaviour.
/// </summary>
internal sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (string Body, string MediaType)> responses = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> requestCounts = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Serve <paramref name="body"/> at <paramref name="path"/> as markdown.</summary>
    public string this[string path] { set => responses[Normalise(path)] = (value, "text/markdown"); }

    public StubHttpHandler Serve(string path, string body, string mediaType = "text/plain")
    {
        responses[Normalise(path)] = (body, mediaType);
        return this;
    }

    public StubHttpHandler ServeJson(string path, object value)
        => Serve(path, JsonSerializer.Serialize(value), "application/json");

    public int RequestCount { get; private set; }

    public int RequestsFor(string path) => requestCounts.GetValueOrDefault(Normalise(path));

    public HttpClient CreateClient() => new(this) { BaseAddress = new Uri("http://localhost/") };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        RequestCount++;
        requestCounts[path] = requestCounts.GetValueOrDefault(path) + 1;

        var response = responses.TryGetValue(path, out var r)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(r.Body, Encoding.UTF8, r.MediaType) }
            : new HttpResponseMessage(HttpStatusCode.NotFound);
        return Task.FromResult(response);
    }

    private static string Normalise(string path) => path.StartsWith('/') ? path : "/" + path;
}
