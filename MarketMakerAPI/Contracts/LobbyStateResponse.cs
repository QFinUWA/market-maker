namespace MarketMaker.Contracts;

public record LobbyStateResponse(
        List<List<string?>> Exchanges,
        List<string> Participants,
        string State,
        string MarketName,
        string MarketCode
    );