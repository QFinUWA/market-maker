using System;

namespace MarketMaker.Models
{
    public class Order : ICloneable
    {
        public Guid Id { get; }
        public string User { get; }

        public int Price { get; }

        public string Exchange {  get; }

        public int Quantity { get; set; }

        public DateTime TimeStamp { get; }

        public Order(
            string user,
            string market,
            int price,
            int quantity
            )
        {
            Id = Guid.NewGuid();
            User = user.ToLower();
            Exchange = market;
            Price = price;
            Quantity = quantity;
            TimeStamp = DateTime.Now;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
