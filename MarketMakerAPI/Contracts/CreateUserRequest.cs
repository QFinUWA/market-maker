namespace MarketMaker.Contracts;

public record CreateUserRequest(
    string Email,
    string Password
);