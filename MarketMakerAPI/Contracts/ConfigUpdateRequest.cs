namespace MarketMaker.Contracts;

public record ConfigUpdateRequest(
        string? ExchangeName,
        Dictionary<string, string>? MarketNames
);