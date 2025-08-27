using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExchangeApi.Rest.Core; // Result<T>

namespace ExchangeApi.AbstractExchange;

public interface IMarketDataApi
{
    Task<Result<Ticker>> GetTickerAsync(Symbol symbol, CancellationToken ct = default);

    /// <summary>OrderBook（板）。levels は各サイドの最大深さ。</summary>
    Task<Result<OrderBook>> GetOrderBookAsync(Symbol symbol, int levels = 25, CancellationToken ct = default);

    /// <summary>OHLCVの取得。limit は最大件数、sinceUtc は開始時刻（UTC）。</summary>
    Task<Result<IReadOnlyList<Ohlcv>>> GetOhlcvAsync(Symbol symbol, OhlcvInterval interval, int limit = 500, System.DateTimeOffset? sinceUtc = null, CancellationToken ct = default);
}

public interface ITradingApi
{
    /// <summary>新規注文。Market/Limit 対応。</summary>
    Task<Result<OrderSnapshot>> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default);

    /// <summary>注文取消。ExchangeOrderId か ClientOrderId を指定。</summary>
    Task<Result<OrderSnapshot>> CancelOrderAsync(CancelOrderRequest request, CancellationToken ct = default);

    /// <summary>注文照会。</summary>
    Task<Result<OrderSnapshot>> GetOrderAsync(GetOrderRequest request, CancellationToken ct = default);

    /// <summary>オープン注文一覧（ページング）。cursor はアダプタ依存形式。</summary>
    Task<Result<Page<OrderSnapshot>>> ListOpenOrdersAsync(Symbol symbol, string? cursor = null, int limit = 50, CancellationToken ct = default);
}

public interface IAccountApi
{
    Task<Result<IReadOnlyList<Balance>>> GetBalancesAsync(CancellationToken ct = default);

    /// <summary>アカウント基本情報（任意）。未対応アダプタは Error で Fail。</summary>
    Task<Result<AccountInfo>> GetAccountInfoAsync(CancellationToken ct = default);
}

public interface IExchange : IMarketDataApi, ITradingApi, IAccountApi
{
    string Name { get; }

    /// <summary>シンボルごとの精度ヒント（桁数・最小単位）。未対応なら Fail。</summary>
    Task<Result<Precision>> GetPrecisionAsync(Symbol symbol, CancellationToken ct = default);
}
