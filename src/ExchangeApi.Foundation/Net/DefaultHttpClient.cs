using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ExchangeApi.Rest.Core;

namespace ExchangeApi.Foundation;

public class DefaultHttpClient : IHttpClient
{
    private readonly HttpClient _inner;
    public DefaultHttpClient(HttpClient? inner = null) => _inner = inner ?? new HttpClient();
    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default)
        => _inner.SendAsync(request, ct);
}
