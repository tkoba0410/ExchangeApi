namespace ExchangeApi.Rest.Core;

public record Error(string Code, string Message, string? Detail = null);
