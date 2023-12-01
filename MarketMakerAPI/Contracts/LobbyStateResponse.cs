namespace MarketMaker.Contracts;

public record LobbyStateResponse(
        List<string> Exchanges,
        List<string> Participants,
        string State,
        string MarketName,
        string MarketCode
    );