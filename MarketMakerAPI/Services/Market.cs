using MarketMaker.Models;
namespace MarketMaker.Services;

public class Market
{
    private readonly Dictionary<Guid, Order> _orders = new();
    public readonly Dictionary<long, PriorityQueue<Guid, DateTime>> Ask = new();
    public readonly Dictionary<long, PriorityQueue<Guid, DateTime>> Bid = new();
    private long? _bestAsk;
    private int _nAsks = 0;
    private long? _bestBid;
    private int _nBids = 0;
    
    // public readonly Dictionary<string, float> UserProfits = new();

    public IEnumerable<Order> Orders => _orders.Values;

    public Order? GetOrder(Guid id)
    {
        return _orders.GetValueOrDefault(id);
    }


    public (Order, List<Transaction>) NewOrder(NewOrderRequest orderRequest)
    {
        var (user, market, requestedPrice, quantity) = orderRequest;
        var sideIsBid = quantity > 0;

        // Gets the best ask by iterating through orders
        _bestAsk = _orders.Values
            .Where(o => o.Quantity < 0)
            .Select(o => o.Price)
            .DefaultIfEmpty()
            .Min();
            
        _bestAsk = _bestAsk == 0 ? null : _bestAsk;

        // Gets the best bid by iterating through orders
        _bestBid = _orders.Values
            .Where(o => o.Quantity > 0)
            .Select(o => o.Price)
            .DefaultIfEmpty()
            .Max();
    
        _bestBid = _bestBid == 0 ? null : _bestBid;

        var side = sideIsBid ? Bid : Ask;
        var otherSide = !sideIsBid ? Bid : Ask;
        var sign = sideIsBid ? 1 : -1;
        long? otherBestPrice = sideIsBid ? _bestAsk : _bestBid;

        // keep removing from queue until first order exists
        var price = otherBestPrice is not null
            ? Math.Abs(Math.Min(sign * requestedPrice, sign * otherBestPrice.Value))
            : requestedPrice;

        // while we've made all trades possible
        Guid newId;
        
        var transactions = new List<Transaction>();
        
        var order = new Order(Guid.NewGuid(), user, price, market, quantity, DateTime.Now);
        while ( order.Quantity != 0)
        {
            // if we can make trades at this price
            if (otherSide.TryGetValue(price, out var otherQueue))
            {

                if ((side!.GetValueOrDefault(price, null)?.Count ?? 0) > 0 || otherQueue.Count > 0)
                {
                    // match each trade
                    newId = Guid.NewGuid();
                    // transactions.Add(price, new());
                    while (otherQueue.Count > 0 && order.Quantity != 0)
                    {
                        Guid otherId = otherQueue.Peek();
                        
                        // dormant deleted orders
                        if (!_orders.ContainsKey(otherId))
                        {
                            otherQueue.Dequeue();
                            continue;
                        }

                        var otherOrder = _orders[otherId];
                        int quantityTraded;

                        if (Math.Sign(order.Quantity + otherOrder.Quantity) != Math.Sign(order.Quantity))
                        {
                            quantityTraded = order.Quantity;

                            otherOrder.Quantity += order.Quantity;
                            // UserProfits[otherOrder.User] += order.Quantity * price;
                            order.Quantity = 0;
                            // UserProfits[order.User] += -1 * order.Quantity * price;
                        }
                        else
                        {
                            quantityTraded = otherOrder.Quantity;

                            order.Quantity += otherOrder.Quantity;
                            // UserProfits[order.User] += otherOrder.Quantity * price;
                            otherOrder.Quantity = 0;
                            // UserProfits[otherOrder.User] += -1 * otherOrder.Quantity * price;
                        }

                        if (otherOrder.Quantity == 0)
                        {
                            otherQueue.Dequeue();
                            _orders.Remove(otherId);
                            if (sideIsBid) {
                                _nAsks--;
                            }
                            else {
                                _nBids--;
                            }
                        }

                        var transaction = CreateTransaction(order, otherOrder, sideIsBid, quantityTraded);
                        transactions.Add(transaction);
                        
                    }
                }
            }
            // move up to next best price in otherSide
            if (sign == 1) {
                price = otherSide.Keys.Where(p => p > price).DefaultIfEmpty().Min();
            } else {
                price = otherSide.Keys.Where(p => p < price).DefaultIfEmpty().Max();
            }
            if (price == 0 || price * sign > sign * requestedPrice) {
                break;
            } else {
                continue;
            }

        }
        
        if (order.Quantity != 0)
        {
            _orders.Add(order.Id, order);
            if (sideIsBid)
            {
                _nAsks++;
            }
            else
            {
                _nBids++;
            }
            if (!side.ContainsKey(order.Price)) side.TryAdd(order.Price, new PriorityQueue<Guid, DateTime>());
            side[order.Price].Enqueue(order.Id, order.TimeStamp);
        }
        
        // NOTE: the opposite side's best price will be outdated but 
        //       can never be less competitive so we don't need to update it

        return (order, transactions);
    }

    public bool DeleteOrder(Order deleteOrder)
    {
        _orders.Remove(deleteOrder.Id);
        if (deleteOrder.Quantity > 0)
        {
            _nBids--;
        }
        else
        {
            _nAsks--;
        }
        return true;
    }

    public void Close(long price)
    {
        // foreach (var order in _orders.Values) UserProfits[order.User] += (price - order.Price) * order.Quantity;

        _orders.Clear();
        Bid.Clear();
        Ask.Clear();
    }

    public void RemoveEmptyOrders()
    {
        Bid.Clear();
        Ask.Clear();

        foreach (var order in _orders.Values)
        {
            var side = order.Quantity > 0 ? Bid : Ask;
            side.TryAdd(order.Price, new PriorityQueue<Guid, DateTime>());
            side[order.Price].Enqueue(order.Id, order.TimeStamp);
        }
    }

    public void InsertOrder(Order order)
    {
        _orders.Add(order.Id, order);
        if (order.Quantity > 0)
        {
            _nBids++;
        }
        else
        {
            _nAsks++;
        }
    }

    private Transaction CreateTransaction(Order aggressive, Order passive, bool buy, int quantity)
    {
        var (buyer, seller) = buy ? (aggressive, passive) : (passive, aggressive);
        return new Transaction(
            buyer.User,
            buyer.Id,
            seller.User,
            seller.Id,
            passive.Market,
            passive.Price,
            Math.Abs(quantity),
            passive.Id,
            aggressive.TimeStamp
        );
    }
}