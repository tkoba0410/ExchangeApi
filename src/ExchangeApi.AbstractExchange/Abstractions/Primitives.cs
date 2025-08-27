using System;

namespace ExchangeApi.AbstractExchange;

public readonly record struct Symbol(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct ClientOrderId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct ExchangeOrderId(string Value)
{
    public override string ToString() => Value;
}

public enum OrderSide { Buy, Sell }
public enum OrderType { Market, Limit }
public enum TimeInForce { GTC, IOC, FOK }

/// <summary>取引所や銘柄ごとの桁数・最小単位のヒント。</summary>
public sealed record Precision(
    int PriceDecimals,
    int QuantityDecimals,
    decimal MinNotional,
    decimal MinQuantity
);
