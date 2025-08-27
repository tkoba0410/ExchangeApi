using System;
using System.Collections.Generic;

namespace ExchangeApi.AbstractExchange;

public sealed record Ticker(
    Symbol Symbol,
    decimal Bid,
    decimal Ask,
    DateTimeOffset TimestampUtc
);

public sealed record BookLevel(decimal Price, decimal Quantity);

public sealed record OrderBook(
    Symbol Symbol,
    IReadOnlyList<BookLevel> Bids,
    IReadOnlyList<BookLevel> Asks,
    DateTimeOffset TimestampUtc
);

public sealed record Ohlcv(
    DateTimeOffset OpenTimeUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume
);

public enum OhlcvInterval
{
    M1, M3, M5, M15, M30, H1, H4, D1
}
