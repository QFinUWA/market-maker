using MarketMaker.Services;
using MarketMaker.Models;

using Xunit;
using System;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MarketMaker.UnitTests

{
    public class LocalMarketTests : LocalMarketService, IDisposable 
    {
        private readonly ITestOutputHelper _testOutputHelper;

        private int _newPrice = 0;

        private int NewPrice
        {
            get => ++_newPrice;
        }
        public LocalMarketTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            Exchange.Add("ExchangeA", new Exchange());
            Exchange.Add("ExchangeB", new Exchange());
            Exchange.Add("ExchangeC", new Exchange());
        }

        [Fact]
        public void InitializeMarketTest()
        {
            //Arange

            //Act
                                    
            //Assert
            Assert.Empty(Exchange["ExchangeA"].Orders);
            Assert.Empty(Exchange["ExchangeA"].Bid);
            Assert.Empty(Exchange["ExchangeA"].Ask);
        }

        [Fact, ]
        public void AddSingleBid()
        {
            int price = NewPrice;
            //Arange
            Order bidOrder = new Order("userA", "ExchangeA", price, 1);

            //Act

            NewOrder(bidOrder);

            //Assert
            Assert.Equal(1, Exchange["ExchangeA"].Bid[_newPrice].Count);
        }

        [Fact]
        public void AddSingleAsk()
        {
            int price = NewPrice;
            //Arange
            Order askOrder = new Order("userA", "ExchangeA", price, -1);

            //Act

            NewOrder(askOrder);


            //Assert
            Assert.Equal(1, Exchange["ExchangeA"].Ask[price].Count);
        }

        [Fact]
        public void HitSingleBid()
        {
            int price = NewPrice;
            //Arange
            Order askOrder = new Order("userA", "ExchangeA", price, -1);
            Order bidOrder = new Order("userB", "ExchangeA", price, 1);

            //Act
            NewOrder(bidOrder);
            NewOrder(askOrder);


            //Assert
            Assert.Equal(0, Exchange["ExchangeA"].Ask[price].Count);
            Assert.Equal(0, Exchange["ExchangeA"].Bid[price].Count);
        }

        [Fact]
        public void HitSingleAsk()
        {
            int price = NewPrice;
            //Arange
            Order bidOrder = new Order("userB", "ExchangeA", price, 1);
            Order askOrder = new Order("userA", "ExchangeA", price, -1);

            //Act
            NewOrder(askOrder);
            NewOrder(bidOrder);

            //Assert
            Assert.Equal(0, Exchange["ExchangeA"].Ask[price].Count);
            Assert.Equal(0, Exchange["ExchangeA"].Bid[price].Count);
        }

        [Fact]
        public void OneToMany()
        {
            int price = NewPrice;
            //Arange
            Order bidOrder = new Order("userA", "ExchangeA", price, 10);
            Order askOrder1 = new Order("userB", "ExchangeA", price, -3);
            Order askOrder2 = new Order("userC", "ExchangeA", price, -4);
            Order askOrder3 = new Order("userD", "ExchangeA", price, -1);
            Guid bidOrderId = bidOrder.Id;

            //Act
            NewOrder(askOrder1);
            NewOrder(askOrder2);
            NewOrder(askOrder3);
            NewOrder(bidOrder);


            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());

            Assert.Equal(0, Exchange["ExchangeA"].Ask[price].Count);
            Assert.Equal(1, Exchange["ExchangeA"].Bid[price].Count);

            Assert.Equal(2, Exchange["ExchangeA"].GetOrder(bidOrderId).Quantity);

        }
        [Fact]
        public void ManyToOne()
        {
            int price = NewPrice;
            //Arange
            Order bidOrder = new Order("userA", "ExchangeA", price, 10);
            Order askOrder1 = new Order("userB", "ExchangeA", price, -3);
            Order askOrder2 = new Order("userC", "ExchangeA", price, -4);
            Order askOrder3 = new Order("userD", "ExchangeA", price, -1);
            Guid bidOrderId = bidOrder.Id;

            //Act
            NewOrder(bidOrder);
            NewOrder(askOrder1);
            NewOrder(askOrder2);
            NewOrder(askOrder3);


            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());
            Assert.Equal(0, Exchange["ExchangeA"].Ask[price].Count); 
            Assert.Equal(1, Exchange["ExchangeA"].Bid[price].Count); 

            Assert.Equal(2, Exchange["ExchangeA"].GetOrder(bidOrderId).Quantity);

        }

        [Fact]
        public void OldOrders1()
        {
            int price = NewPrice;
            //Arange
            Order bidOrder = new Order("userA", "ExchangeA", price, 10);
            Order askOrder1 = new Order("userB", "ExchangeA", price, -3);
            Order askOrder2 = new Order("userC", "ExchangeA", price, -4);
            Order askOrder3 = new Order("userD", "ExchangeA", price, -1);
            Guid bidOrderId = bidOrder.Id;

            //Act
            NewOrder(askOrder1);
            NewOrder(askOrder2);
            DeleteOrder(askOrder2.Id, "userC".ToLower());
            NewOrder(askOrder3);
            NewOrder(bidOrder);

            Exchange["ExchangeA"].RemoveEmptyOrders();


            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());
            Assert.False(Exchange["ExchangeA"].Ask.ContainsKey(price));
            Assert.Equal(1, Exchange["ExchangeA"].Bid[price].Count);

            Assert.Equal(6, Exchange["ExchangeA"].GetOrder(bidOrderId).Quantity);

        }
        [Fact]
        public void OldOrders2()
        {
            int price = NewPrice;
            //Arange
            Order bidOrder = new Order("userA", "ExchangeA", price, 10);
            Order askOrder1 = new Order("userB", "ExchangeA", price, -3);
            Order askOrder2 = new Order("userC", "ExchangeA", price, -4);
            Order askOrder3 = new Order("userD", "ExchangeA", price, -1);
            Guid bidOrderId = bidOrder.Id;

            //Act
            NewOrder(bidOrder);
            DeleteOrder(bidOrderId, "userA".ToLower());
            NewOrder(askOrder1);
            NewOrder(askOrder2);
            NewOrder(askOrder3);

            Exchange["ExchangeA"].RemoveEmptyOrders();


            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());
            Assert.Equal(3, Exchange["ExchangeA"].Ask[price].Count);
            Assert.False(Exchange["ExchangeA"].Bid.ContainsKey(price));

            //Assert.Equal(6, exchange.GetOrder(bidOrderId).Quantity);


        }
        
        [Fact]
        public void UnorthorisedDeletion()
        {
            int priceA = NewPrice;
            int priceB = NewPrice;
            //Arange
            Order bidOrder1 = new Order("userA", "ExchangeA", priceA, 10);
            Order bidOrder2 = new Order("userA", "ExchangeA", priceB, 10);

            //Act
            NewOrder(bidOrder1);
            NewOrder(bidOrder2);
            bool result1 = DeleteOrder(bidOrder1.Id, "userA".ToLower());
            bool result2 = DeleteOrder(bidOrder2.Id, "not_userA".ToLower());
            Exchange["ExchangeA"].RemoveEmptyOrders();
            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());
            Assert.False(Exchange["ExchangeA"].Bid.ContainsKey(priceA));
            Assert.Equal(1, Exchange["ExchangeA"].Bid[priceB].Count); 

            //Assert.Equal(6, exchange.GetOrder(bidOrderId).Quantity);


        }
        // ordering on different exchanges
        [Fact]
        public void MultipleExchanges()
        {
            int price = NewPrice;
            var bidOrderA = new Order("userA", "ExchangeA", price, 10);
            var askOrderA = new Order("userB", "ExchangeB", price, -10);

            NewOrder(bidOrderA);
            NewOrder(askOrderA);
            
            Assert.Equal(1, Exchange["ExchangeA"].Bid[price].Count); 
            Assert.Equal(1, Exchange["ExchangeB"].Ask[price].Count); 
        }
        // self-buying
        [Fact]
        public void CanSelfBuy()
        {
            int price = NewPrice;
            var bidOrderA = new Order("userA", "ExchangeA", price, 10);
            var askOrderA = new Order("userA", "ExchangeA", price, -10);

            NewOrder(bidOrderA);
            NewOrder(askOrderA);
            
            Assert.Equal(0, Exchange["ExchangeA"].Bid[price].Count); 
            Assert.Equal(0, Exchange["ExchangeA"].Ask[price].Count); 
        }
        
        // add exchange (new, existing, basic ordering)
        [Fact]
        public void CanAddExchange()
        {
            const string newExchangeName = "ExchangeD";
            var firstAdd = AddExchange(newExchangeName);
            Assert.True(firstAdd);
            Assert.True(Exchange.ContainsKey(newExchangeName));
            
            var secondAdd = AddExchange(newExchangeName);
            Assert.False(secondAdd);
            Assert.True(Exchange.ContainsKey(newExchangeName));
            
        }
        // state transitions
        [Fact]
        public void StateTransitions()
        {
            Assert.Equal(MarketState.Lobby, State);

            State = MarketState.Lobby;
            
            Assert.Throws<ArgumentException>(() => State = MarketState.Paused);
            Assert.Throws<ArgumentException>(() => State = MarketState.Closed);

            State = MarketState.Open;
            
            Assert.Throws<ArgumentException>(() => State = MarketState.Lobby);

            State = MarketState.Paused;
            
            Assert.Throws<ArgumentException>(() => State = MarketState.Lobby);

            State = MarketState.Closed;
            
            Assert.Throws<ArgumentException>(() => State = MarketState.Open);
            Assert.Throws<ArgumentException>(() => State = MarketState.Paused);
            
        }
        // ordering from non-existant exchange
        [Fact]
        public void InvalidOrder()
        {
            var price = NewPrice;

            var order = new Order("user", "Exchange_ERR", price, 10);
            var transactions = NewOrder(order);
            
            Assert.Null(transactions);
        }
        // deleting non-existant order
        [Fact]
        public void InvalidDeletion()
        {
            var price = NewPrice;
            
            var order = new Order("userA", "ExchangeA", NewPrice, 10);
            var transactions = NewOrder(order);
            Assert.NotNull(transactions);
           
            Assert.False(DeleteOrder(Guid.NewGuid(), order.User)); 
            Assert.False(DeleteOrder(order.Id, "")); 
            
        }
        
        public void Dispose()
        {
            
        }
    }
}