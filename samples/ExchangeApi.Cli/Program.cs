using System;
using ExchangeApi.AbstractExchange;
using ExchangeApi.Applications;
using ExchangeApi.Foundation;
using ExchangeApi.Integrations.Mock;

static void PrintResult<T>(string title, ExchangeApi.Rest.Core.Result<T> res)
{
    if (res.IsSuccess)
        Console.WriteLine($"{title}: OK -> {res.Value}");
    else
        Console.WriteLine($"{title}: FAIL -> {res.Error?.Code} {res.Error?.Message}");
}

var http = new DefaultHttpClient();
var exchange = new MockExchange(http);
var service = new ExchangeService(exchange);

Console.WriteLine($"Exchange: {exchange.Name}");

// Ticker
var tRes = await service.GetTickerAsync(new Symbol("BTCJPY"));
if (tRes.IsSuccess)
{
    var t = tRes.Value!;
    Console.WriteLine($"Ticker {t.Symbol}: bid={t.Bid} ask={t.Ask} @ {t.TimestampUtc:u}");
}
else
{
    Console.WriteLine($"Ticker error: {tRes.Error?.Code} {tRes.Error?.Message}");
}

// Place Order
var oRes = await service.PlaceOrderAsync(new PlaceOrderRequest(new Symbol("BTCJPY"), OrderSide.Buy, OrderType.Market, 0.01m));
if (oRes.IsSuccess)
{
    var o = oRes.Value!;
    Console.WriteLine($"Order {o.Ref.ExchangeOrderId}: status={o.Status} avgPrice={o.AvgPrice} @ {o.UpdatedAtUtc:u}");
}
else
{
    Console.WriteLine($"Order error: {oRes.Error?.Code} {oRes.Error?.Message}");
}

// Get Order
if (oRes.IsSuccess)
{
    var refId = oRes.Value!.Ref.ExchangeOrderId;
    var qRes = await service.GetOrderAsync(new GetOrderRequest(new Symbol("BTCJPY"), ExchangeOrderId: refId));
    PrintResult("GetOrder", qRes);
}

// List Open Orders
var listRes = await service.ListOpenOrdersAsync(new Symbol("BTCJPY"));
PrintResult("ListOpenOrders", listRes);

// Balances
var balRes = await service.GetBalancesAsync();
if (balRes.IsSuccess)
{
    foreach (var b in balRes.Value!)
        Console.WriteLine($"Balance {b.Asset}: free={b.Free} locked={b.Locked}");
}
else
{
    Console.WriteLine($"Balances error: {balRes.Error?.Code} {balRes.Error?.Message}");
}
