namespace ExchangeApi.Integrations.Bitflyer.Models;

/// <summary>
/// 最小のティッカーモデル（必要に応じて拡張）
/// </summary>
public sealed record Ticker(
    decimal Ltp,
    decimal BestBid,
    decimal BestAsk
);
