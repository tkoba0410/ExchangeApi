using System.Threading.Tasks;
using ExchangeApi.AbstractExchange;
using ExchangeApi.Applications;
using ExchangeApi.Foundation;
using ExchangeApi.Integrations.Mock;
using Xunit;

public class MockExchangeTests
{
    private static ExchangeService CreateService()
    {
        var http = new DefaultHttpClient();
        var ex = new MockExchange(http);
        return new ExchangeService(ex);
    }

    [Fact]
    public async Task Ticker_Should_Succeed()
    {
        var svc = CreateService();
        var res = await svc.GetTickerAsync(new Symbol("BTCJPY"));
        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Value);
        Assert.Equal("BTCJPY", res.Value!.Symbol.Value);
    }

    [Fact]
    public async Task Place_Get_Cancel_Order_Should_Work()
    {
        var svc = CreateService();

        // Place
        var place = await svc.PlaceOrderAsync(new PlaceOrderRequest(new Symbol("BTCJPY"), OrderSide.Buy, OrderType.Market, 0.01m));
        Assert.True(place.IsSuccess);
        var exId = place.Value!.Ref.ExchangeOrderId;

        // Get
        var got = await svc.GetOrderAsync(new GetOrderRequest(new Symbol("BTCJPY"), ExchangeOrderId: exId));
        Assert.True(got.IsSuccess);
        Assert.Equal(exId, got.Value!.Ref.ExchangeOrderId);

        // Cancel
        var canceled = await svc.CancelOrderAsync(new CancelOrderRequest(new Symbol("BTCJPY"), ExchangeOrderId: exId));
        Assert.True(canceled.IsSuccess);
        Assert.Equal(OrderStatus.Canceled, canceled.Value!.Status);
    }

    [Fact]
    public async Task ListOpenOrders_Should_Return_Empty()
    {
        var svc = CreateService();
        var list = await svc.ListOpenOrdersAsync(new Symbol("BTCJPY"));
        Assert.True(list.IsSuccess);
        Assert.Empty(list.Value!.Items);
    }

    [Fact]
    public async Task Balances_Should_Succeed()
    {
        var svc = CreateService();
        var bal = await svc.GetBalancesAsync();
        Assert.True(bal.IsSuccess);
        Assert.NotEmpty(bal.Value!);
    }
}
