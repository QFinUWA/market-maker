﻿using System.Text.Json.Serialization;

namespace MarketMaker.Models;

[Serializable]
public record Order(Guid Id, string User, long Price, string Market, int Quantity, DateTime TimeStamp)
{
    public int Quantity { get; set; } = Quantity;
}