using System.Net;
using System.Text;
using System.Text.Json;

namespace ScanWorker.Tests.Helpers;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _content;

    public MockHttpMessageHandler(HttpStatusCode statusCode, string content)
    {
        _statusCode = statusCode;
        _content = content;
    }

    public static MockHttpMessageHandler WithJsonResponse<T>(T response, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(response);
        return new MockHttpMessageHandler(statusCode, json);
    }

    public static MockHttpMessageHandler WithStatusCode(HttpStatusCode statusCode)
    {
        return new MockHttpMessageHandler(statusCode, string.Empty);
    }

    public static MockHttpMessageHandler WithInvalidJson()
    {
        return new MockHttpMessageHandler(HttpStatusCode.OK, "{ invalid json }}}");
    }

    public static MockHttpMessageHandler WithRawJson(string json)
    {
        return new MockHttpMessageHandler(HttpStatusCode.OK, json);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content, Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
}

