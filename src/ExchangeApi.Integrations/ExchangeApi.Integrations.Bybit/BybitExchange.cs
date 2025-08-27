using ExchangeApi.AbstractExchange;
using ExchangeApi.Rest.Adapter;
using ExchangeApi.Rest.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace ExchangeApi.Integrations.Bybit;

/// <summary>
/// Bybit REST (V5) – 公開APIのみ最小実装（GetTicker）
/// ドキュメント: GET /v5/market/tickers （category=spot 推奨）
/// </summary>
public sealed class BybitExchange : ExchangeAdapterBase
{
    private readonly string _baseUrl;

    public BybitExchange(IHttpClient http, string baseUrl = "https://api.bybit.com") : base(http)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public override string Name => "Bybit";

    // ===== Market Data =====
    public override async Task<Result<Ticker>> GetTickerAsync(Symbol symbol, CancellationToken ct = default)
    {
        // Bybitは大文字シンボル前提（例: BTCUSDT）
        var sym = symbol.Value.ToUpperInvariant();
        var url = $"{_baseUrl}/v5/market/tickers?category=spot&symbol={Uri.EscapeDataString(sym)}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var res = await Http.SendAsync(req, ct);

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            return Result<Ticker>.Fail(new Error("HttpError", $"Status {(int)res.StatusCode}", body));
        }

        var json = await res.Content.ReadAsStringAsync(ct);
        try
        {
            var data = JsonSerializer.Deserialize<BybitTickersResponse>(json, _jsonOptions);
            if (data is null || data.RetCode != 0 || data.Result?.List is null || data.Result.List.Count == 0)
                return Result<Ticker>.Fail(new Error("ApiError", data?.RetMsg ?? "Empty response", json));

            var t = data.Result.List[0];

            var bid = ParseDecimal(t.Bid1Price);
            var ask = ParseDecimal(t.Ask1Price);

            // Timestamp はAPIレスポンスにミリ秒timeがあるが（ドキュメント上は全体レスポンスに time あり）、
            // ここでは簡便的に受信時刻をUTCで使用
            var ticker = new Ticker(new Symbol(sym), bid, ask, DateTimeOffset.UtcNow);
            return Result<Ticker>.Ok(ticker);
        }
        catch (Exception ex)
        {
            return Result<Ticker>.Fail(new Error("DeserializeError", ex.Message, json));
        }
    }

    // 以降は未実装（Failで返す）
    public override Task<Result<OrderBook>> GetOrderBookAsync(Symbol symbol, int levels = 25, CancellationToken ct = default)
        => Task.FromResult(Result<OrderBook>.Fail(new Error("NotImplemented", "GetOrderBookAsync")));

    public override Task<Result<IReadOnlyList<Ohlcv>>> GetOhlcvAsync(Symbol symbol, OhlcvInterval interval, int limit = 500, DateTimeOffset? sinceUtc = null, CancellationToken ct = default)
        => Task.FromResult(Result<IReadOnlyList<Ohlcv>>.Fail(new Error("NotImplemented", "GetOhlcvAsync")));

    public override Task<Result<OrderSnapshot>> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default)
        => Task.FromResult(Result<OrderSnapshot>.Fail(new Error("NotImplemented", "PlaceOrderAsync")));

    public override Task<Result<OrderSnapshot>> CancelOrderAsync(CancelOrderRequest request, CancellationToken ct = default)
        => Task.FromResult(Result<OrderSnapshot>.Fail(new Error("NotImplemented", "CancelOrderAsync")));

    public override Task<Result<OrderSnapshot>> GetOrderAsync(GetOrderRequest request, CancellationToken ct = default)
        => Task.FromResult(Result<OrderSnapshot>.Fail(new Error("NotImplemented", "GetOrderAsync")));

    public override Task<Result<Page<OrderSnapshot>>> ListOpenOrdersAsync(Symbol symbol, string? cursor = null, int limit = 50, CancellationToken ct = default)
        => Task.FromResult(Result<Page<OrderSnapshot>>.Fail(new Error("NotImplemented", "ListOpenOrdersAsync")));

    public override Task<Result<IReadOnlyList<Balance>>> GetBalancesAsync(CancellationToken ct = default)
        => Task.FromResult(Result<IReadOnlyList<Balance>>.Fail(new Error("NotImplemented", "GetBalancesAsync")));

    public override Task<Result<AccountInfo>> GetAccountInfoAsync(CancellationToken ct = default)
        => Task.FromResult(Result<AccountInfo>.Fail(new Error("NotImplemented", "GetAccountInfoAsync")));

    public override Task<Result<Precision>> GetPrecisionAsync(Symbol symbol, CancellationToken ct = default)
        => Task.FromResult(Result<Precision>.Fail(new Error("NotImplemented", "GetPrecisionAsync")));

    // ===== helpers =====
    private static decimal ParseDecimal(string s)
        => decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0m;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class BybitTickersResponse
    {
        [JsonPropertyName("retCode")] public int RetCode { get; set; }
        [JsonPropertyName("retMsg")] public string? RetMsg { get; set; }
        [JsonPropertyName("result")] public BybitTickersResult? Result { get; set; }
        [JsonPropertyName("time")] public long Time { get; set; }
    }

    private sealed class BybitTickersResult
    {
        [JsonPropertyName("category")] public string? Category { get; set; }
        [JsonPropertyName("list")] public List<BybitTickerItem>? List { get; set; }
    }

    private sealed class BybitTickerItem
    {
        [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
        [JsonPropertyName("bid1Price")] public string Bid1Price { get; set; } = "0";
        [JsonPropertyName("ask1Price")] public string Ask1Price { get; set; } = "0";
        [JsonPropertyName("lastPrice")] public string LastPrice { get; set; } = "0";
        [JsonPropertyName("prevPrice24h")] public string PrevPrice24h { get; set; } = "0";
        [JsonPropertyName("highPrice24h")] public string HighPrice24h { get; set; } = "0";
        [JsonPropertyName("lowPrice24h")] public string LowPrice24h { get; set; } = "0";
    }
}
