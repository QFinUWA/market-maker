using System.Text.Json.Serialization;

namespace MarketMaker.Models
{
    [Serializable]
    public class Order : ICloneable
    {
        public Guid Id { get; }
        public string User { get; }

        public int Price { get; }

        public string Market {  get; }

        public int Quantity { get; set; }

        public DateTime TimeStamp { get; }

        [JsonConstructor]
        public Order(Guid id, string user, int price, string market, int quantity, DateTime timeStamp)
        {
            Id = Guid.NewGuid();
            User = user.ToLower();
            Price = price;
            Market = market;
            Quantity = quantity;
            TimeStamp = DateTime.Now;
        }

        public Order(
            string user,
            string market,
            int price,
            int quantity
            )
        {
            Id = Guid.NewGuid();
            User = user.ToLower();
            Market = market;
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
