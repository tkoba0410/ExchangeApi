using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExchangeApi.AbstractExchange;
using ExchangeApi.Rest.Core;

namespace ExchangeApi.Applications;

public class ExchangeService
{
    private readonly IExchange _exchange;
    public ExchangeService(IExchange exchange) => _exchange = exchange;

    // --- Market Data ---
    public Task<Result<Ticker>> GetTickerAsync(Symbol symbol, CancellationToken ct = default)
        => _exchange.GetTickerAsync(symbol, ct);

    public Task<Result<OrderBook>> GetOrderBookAsync(Symbol symbol, int levels = 25, CancellationToken ct = default)
        => _exchange.GetOrderBookAsync(symbol, levels, ct);

    public Task<Result<IReadOnlyList<Ohlcv>>> GetOhlcvAsync(Symbol symbol, OhlcvInterval interval, int limit = 500, System.DateTimeOffset? sinceUtc = null, CancellationToken ct = default)
        => _exchange.GetOhlcvAsync(symbol, interval, limit, sinceUtc, ct);

    // --- Trading ---
    public Task<Result<OrderSnapshot>> PlaceOrderAsync(PlaceOrderRequest req, CancellationToken ct = default)
        => _exchange.PlaceOrderAsync(req, ct);

    public Task<Result<OrderSnapshot>> CancelOrderAsync(CancelOrderRequest req, CancellationToken ct = default)
        => _exchange.CancelOrderAsync(req, ct);

    public Task<Result<OrderSnapshot>> GetOrderAsync(GetOrderRequest req, CancellationToken ct = default)
        => _exchange.GetOrderAsync(req, ct);

    public Task<Result<Page<OrderSnapshot>>> ListOpenOrdersAsync(Symbol symbol, string? cursor = null, int limit = 50, CancellationToken ct = default)
        => _exchange.ListOpenOrdersAsync(symbol, cursor, limit, ct);

    // --- Account ---
    public Task<Result<IReadOnlyList<Balance>>> GetBalancesAsync(CancellationToken ct = default)
        => _exchange.GetBalancesAsync(ct);

    public Task<Result<AccountInfo>> GetAccountInfoAsync(CancellationToken ct = default)
        => _exchange.GetAccountInfoAsync(ct);

    public Task<Result<Precision>> GetPrecisionAsync(Symbol symbol, CancellationToken ct = default)
        => _exchange.GetPrecisionAsync(symbol, ct);
}
