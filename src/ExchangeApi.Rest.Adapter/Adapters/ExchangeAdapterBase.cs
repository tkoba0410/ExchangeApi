using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExchangeApi.AbstractExchange;
using ExchangeApi.Rest.Core;

namespace ExchangeApi.Rest.Adapter;

public abstract class ExchangeAdapterBase : IExchange
{
    public abstract string Name { get; }
    protected readonly IHttpClient Http;
    protected ExchangeAdapterBase(IHttpClient http) { Http = http; }

    // --- Market Data ---
    public abstract Task<Result<Ticker>> GetTickerAsync(Symbol symbol, CancellationToken ct = default);
    public abstract Task<Result<OrderBook>> GetOrderBookAsync(Symbol symbol, int levels = 25, CancellationToken ct = default);
    public abstract Task<Result<IReadOnlyList<Ohlcv>>> GetOhlcvAsync(Symbol symbol, OhlcvInterval interval, int limit = 500, System.DateTimeOffset? sinceUtc = null, CancellationToken ct = default);

    // --- Trading ---
    public abstract Task<Result<OrderSnapshot>> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default);
    public abstract Task<Result<OrderSnapshot>> CancelOrderAsync(CancelOrderRequest request, CancellationToken ct = default);
    public abstract Task<Result<OrderSnapshot>> GetOrderAsync(GetOrderRequest request, CancellationToken ct = default);
    public abstract Task<Result<Page<OrderSnapshot>>> ListOpenOrdersAsync(Symbol symbol, string? cursor = null, int limit = 50, CancellationToken ct = default);

    // --- Account ---
    public abstract Task<Result<IReadOnlyList<Balance>>> GetBalancesAsync(CancellationToken ct = default);
    public abstract Task<Result<AccountInfo>> GetAccountInfoAsync(CancellationToken ct = default);
    public abstract Task<Result<Precision>> GetPrecisionAsync(Symbol symbol, CancellationToken ct = default);
}
