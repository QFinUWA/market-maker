﻿using MarketMaker.Models;

namespace MarketMaker.Contracts
{
    public record MarketStateResponse(
        List<string> Users,
        List<Order> Orders,
        List<TransactionEvent> Transactions
        );
    
    // TODO: add # of users spectating 

}