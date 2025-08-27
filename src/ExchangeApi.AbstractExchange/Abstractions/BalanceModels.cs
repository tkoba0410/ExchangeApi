namespace ExchangeApi.AbstractExchange;

public sealed record Balance(string Asset, decimal Free, decimal Locked);

public sealed record AccountInfo(
    string AccountId,
    string? SubAccountId = null
);
