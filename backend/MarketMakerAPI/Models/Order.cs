using System;

namespace MarketMakerAPI.Models
{
    public class Order
    {
        public Guid Id { get; }
        public string User { get; }

        public int Price { get; }

        public string Market {  get; }

        public int Quantity { get; set; }

        public DateTime CreatedAt { get; }

        private Order(
            Guid id,
            string user,
            string market,
            int price,
            int quantity,
            DateTime CreatedAt
            )
        {
            Id = id;
            User = user;
            Market = market;
            Price = price;
            Quantity = quantity;
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
    }
}
