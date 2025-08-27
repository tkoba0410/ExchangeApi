using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeApi.Rest.Core;

public interface IHttpClient
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default);
}
