using System;
using System.Collections.Generic;

namespace ExchangeApi.AbstractExchange;

public sealed record PlaceOrderRequest(
    Symbol Symbol,
    OrderSide Side,
    OrderType Type,
    decimal Quantity,
    decimal? Price = null,
    TimeInForce? Tif = null,
    ClientOrderId? ClientOrderId = null,
    bool? ReduceOnly = null,
    bool? PostOnly = null,
    string? Tag = null
);

public sealed record OrderRef(
    ExchangeOrderId ExchangeOrderId,
    ClientOrderId? ClientOrderId,
    Symbol Symbol
);

public enum OrderStatus
{
    New,
    PartiallyFilled,
    Filled,
    Canceled,
    Rejected
}

public sealed record OrderExecution(
    Symbol Symbol,
    OrderSide Side,
    decimal Price,
    decimal Quantity,
    DateTimeOffset ExecutedAtUtc
);

public sealed record OrderSnapshot(
    OrderRef Ref,
    OrderStatus Status,
    decimal ExecutedQty,
    decimal AvgPrice,
    DateTimeOffset UpdatedAtUtc
);

public sealed record CancelOrderRequest(
    Symbol Symbol,
    ExchangeOrderId? ExchangeOrderId = null,
    ClientOrderId? ClientOrderId = null
);

public sealed record GetOrderRequest(
    Symbol Symbol,
    ExchangeOrderId? ExchangeOrderId = null,
    ClientOrderId? ClientOrderId = null
);

/// <summary>ページング付き応答（シンプルなカーソル型）。</summary>
public sealed record Page<T>(
    IReadOnlyList<T> Items,
    string? NextCursor
);
