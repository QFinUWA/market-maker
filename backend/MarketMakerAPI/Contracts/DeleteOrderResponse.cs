﻿namespace MarketMakerAPI.Contracts
{
    public record DeleteOrderResponse(
           string market,
           Guid Id
        );

}
