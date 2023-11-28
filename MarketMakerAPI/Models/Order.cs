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

        private Order(
            Guid id,
            string user,
            string market,
            int price,
            int quantity,
            DateTime timeStamp
            )
        {
            Id = id;
            User = user.ToLower();
            Exchange = market;
            Price = price;
            Quantity = quantity;
            TimeStamp = timeStamp;
        }

        public static Order MakeOrder(
            string user,
            string market, 
            int price,
            int quantity,
            Guid? id = null
            )
        {
            return new Order(
                id ?? Guid.NewGuid(),
                user,
                market,
                price,
                quantity,
                DateTime.Now
                );
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
