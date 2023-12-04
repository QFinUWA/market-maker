namespace MarketMaker.Contracts;

public record LobbyStateResponse(
        List<List<string?>> Markets,
        List<string> Participants,
        string State,
        string ExchangeName,
        string ExchangeCode
    );