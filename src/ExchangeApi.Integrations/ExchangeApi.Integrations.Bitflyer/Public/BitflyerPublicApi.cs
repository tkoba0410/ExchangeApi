using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ExchangeApi.Integrations.Bitflyer.Internal;

namespace ExchangeApi.Integrations.Bitflyer.Public;

public sealed class BitflyerPublicApi
{
    private readonly HttpClient _http;

    public BitflyerPublicApi(HttpClient http, BitflyerOptions? options = null)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        var baseUrl = (options ?? new BitflyerOptions()).BaseUrl;
        if (_http.BaseAddress is null) _http.BaseAddress = new Uri(baseUrl);
    }

    public async Task<string> GetTickerRawAsync(
        string productCode = "BTC_JPY",
        CancellationToken ct = default)
    {
        using var res = await _http.GetAsync($"/v1/getticker?product_code={Uri.EscapeDataString(productCode)}", ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(ct);
    }
}
