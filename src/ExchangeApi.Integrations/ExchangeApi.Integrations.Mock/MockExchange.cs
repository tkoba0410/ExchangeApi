using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExchangeApi.AbstractExchange;
using ExchangeApi.Rest.Adapter;
using ExchangeApi.Rest.Core;
using System.Linq; // 先頭の using 群に追加

namespace ExchangeApi.Integrations.Mock;

public sealed class MockExchange : ExchangeAdapterBase
{
    private readonly Dictionary<string, OrderSnapshot> _orders = new();

    public MockExchange(IHttpClient http) : base(http) { }

    public override string Name => "MockExchange";

    // ===== Market Data =====
    public override Task<Result<Ticker>> GetTickerAsync(Symbol symbol, CancellationToken ct = default)
    {
        var t = new Ticker(symbol, 100m, 101m, DateTimeOffset.UtcNow);
        return Task.FromResult(Result<Ticker>.Ok(t));
    }

    public override Task<Result<OrderBook>> GetOrderBookAsync(Symbol symbol, int levels = 25, CancellationToken ct = default)
    {
        var bids = new List<BookLevel>();
        var asks = new List<BookLevel>();
        for (int i = 0; i < levels; i++)
        {
            bids.Add(new BookLevel(100m - i, 0.1m + i * 0.01m));
            asks.Add(new BookLevel(101m + i, 0.1m + i * 0.01m));
        }
        var ob = new OrderBook(symbol, bids, asks, DateTimeOffset.UtcNow);
        return Task.FromResult(Result<OrderBook>.Ok(ob));
    }

    public override Task<Result<IReadOnlyList<Ohlcv>>> GetOhlcvAsync(Symbol symbol, OhlcvInterval interval, int limit = 500, DateTimeOffset? sinceUtc = null, CancellationToken ct = default)
    {
        var list = new List<Ohlcv>();
        var now = DateTimeOffset.UtcNow;
        var step = interval switch
        {
            OhlcvInterval.M1 => TimeSpan.FromMinutes(1),
            OhlcvInterval.M3 => TimeSpan.FromMinutes(3),
            OhlcvInterval.M5 => TimeSpan.FromMinutes(5),
            OhlcvInterval.M15 => TimeSpan.FromMinutes(15),
            OhlcvInterval.M30 => TimeSpan.FromMinutes(30),
            OhlcvInterval.H1 => TimeSpan.FromHours(1),
            OhlcvInterval.H4 => TimeSpan.FromHours(4),
            OhlcvInterval.D1 => TimeSpan.FromDays(1),
            _ => TimeSpan.FromMinutes(1),
        };
        var start = sinceUtc ?? now - step * limit;
        var t = start;
        var price = 100m;
        var rand = new Random(42);
        for (int i = 0; i < limit; i++)
        {
            var open = price;
            var high = open + (decimal)rand.NextDouble();
            var low = open - (decimal)rand.NextDouble();
            var close = low + (decimal)rand.NextDouble() * (high - low);
            var vol = (decimal)rand.NextDouble() * 5m;
            list.Add(new Ohlcv(t, open, high, low, close, vol));
            price = close;
            t = t + step;
        }
        return Task.FromResult(Result<IReadOnlyList<Ohlcv>>.Ok(list));
    }

    // ===== Trading =====
    public override Task<Result<OrderSnapshot>> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var exId = new ExchangeOrderId(Guid.NewGuid().ToString("N"));
        var price = request.Price ?? (request.Side == OrderSide.Buy ? 101m : 100m);
        var snap = new OrderSnapshot(
            new OrderRef(exId, request.ClientOrderId, request.Symbol),
            OrderStatus.Filled,
            request.Quantity,
            price,
            now
        );
        _orders[exId.Value] = snap;
        return Task.FromResult(Result<OrderSnapshot>.Ok(snap));
    }


public override Task<Result<OrderSnapshot>> CancelOrderAsync(CancelOrderRequest request, CancellationToken ct = default)
{
    if (request.ExchangeOrderId is null && request.ClientOrderId is null)
        return Task.FromResult(Result<OrderSnapshot>.Fail(new Error("BadRequest", "Require ExchangeOrderId or ClientOrderId")));

    ExchangeOrderId exId;

    if (request.ExchangeOrderId is { } ex)
    {
        exId = ex;
        if (!_orders.TryGetValue(exId.Value, out _))
            return Task.FromResult(Result<OrderSnapshot>.Fail(new Error("NotFound", "Order not found")));
    }
    else
    {
        var cid = request.ClientOrderId!.Value;
        var hit = _orders.FirstOrDefault(kv => kv.Value.Ref.ClientOrderId is { } c && c.Value == cid.Value);
        if (hit.Equals(default(KeyValuePair<string, OrderSnapshot>)))
            return Task.FromResult(Result<OrderSnapshot>.Fail(new Error("NotFound", "Order not found for ClientOrderId")));
        exId = new ExchangeOrderId(hit.Key);
    }

    var canceled = new OrderSnapshot(
        new OrderRef(exId, request.ClientOrderId, request.Symbol),
        OrderStatus.Canceled,
        0m,
        0m,
        DateTimeOffset.UtcNow
    );
    _orders[exId.Value] = canceled;
    return Task.FromResult(Result<OrderSnapshot>.Ok(canceled));
}

public override Task<Result<OrderSnapshot>> GetOrderAsync(GetOrderRequest request, CancellationToken ct = default)
    {
        if (request.ExchangeOrderId is { } ex)
        {
            if (_orders.TryGetValue(ex.Value, out var snap))
                return Task.FromResult(Result<OrderSnapshot>.Ok(snap));
            return Task.FromResult(Result<OrderSnapshot>.Fail(new Error("NotFound", "Order not found")));
        }
        // 簡易：ClientOrderId では見つからない動作（アダプタ依存）
        return Task.FromResult(Result<OrderSnapshot>.Fail(new Error("NotImplemented", "Lookup by ClientOrderId is not implemented in mock")));
    }

    public override Task<Result<Page<OrderSnapshot>>> ListOpenOrdersAsync(Symbol symbol, string? cursor = null, int limit = 50, CancellationToken ct = default)
    {
        // モック：Openなし（Filled/Cancelledのみ）として空ページを返す
        var page = new Page<OrderSnapshot>(Array.Empty<OrderSnapshot>(), null);
        return Task.FromResult(Result<Page<OrderSnapshot>>.Ok(page));
    }

    // ===== Account =====
    public override Task<Result<IReadOnlyList<Balance>>> GetBalancesAsync(CancellationToken ct = default)
    {
        var list = (IReadOnlyList<Balance>)new[] { new Balance("JPY", 1_000_000m, 0m) };
        return Task.FromResult(Result<IReadOnlyList<Balance>>.Ok(list));
    }

    public override Task<Result<AccountInfo>> GetAccountInfoAsync(CancellationToken ct = default)
        => Task.FromResult(Result<AccountInfo>.Ok(new AccountInfo("mock-account")));

    public override Task<Result<Precision>> GetPrecisionAsync(Symbol symbol, CancellationToken ct = default)
        => Task.FromResult(Result<Precision>.Ok(new Precision(0, 5, 1m, 0.00001m)));
}
