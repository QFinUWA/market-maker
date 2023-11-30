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
            _exchange.Add("ExchangeA", new Exchange());
            _exchange.Add("ExchangeB", new Exchange());
            _exchange.Add("ExchangeC", new Exchange());
            _exchange.Add("ExchangeD", new Exchange());
        }

        [Fact]
        public void InitializeMarketTest()
        {
            //Arange

            //Act
                                    
            //Assert
            Assert.Empty(_exchange["ExchangeA"].Orders);
            Assert.Empty(_exchange["ExchangeA"].Bid);
            Assert.Empty(_exchange["ExchangeA"].Ask);
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
            Assert.Equal(1, _exchange["ExchangeA"].Bid[_newPrice].Count);
        }

        [Fact]
        public void AddSingleAsk()
        {
            int price = NewPrice;
            //Arange
            Order askOrder = new Order("userA", "ExchangeA", price, -1);

            //Act

            _exchange["ExchangeA"].NewOrder(askOrder);


            //Assert
            Assert.Equal(1, _exchange["ExchangeA"].Ask[price].Count);
        }

        [Fact]
        public void HitSingleBid()
        {
            int price = NewPrice;
            //Arange
            Order askOrder = new Order("userA", "ExchangeA", price, -1);
            Order bidOrder = new Order("userB", "ExchangeA", price, 1);

            //Act
            _exchange["ExchangeA"].NewOrder(bidOrder);
            _exchange["ExchangeA"].NewOrder(askOrder);


            //Assert
            Assert.Equal(0, _exchange["ExchangeA"].Ask[price].Count);
            Assert.Equal(0, _exchange["ExchangeA"].Bid[price].Count);
        }

        [Fact]
        public void HitSingleAsk()
        {
            int price = NewPrice;
            //Arange
            Order bidOrder = new Order("userB", "ExchangeA", price, 1);
            Order askOrder = new Order("userA", "ExchangeA", price, -1);

            //Act
            _exchange["ExchangeA"].NewOrder(askOrder);
            _exchange["ExchangeA"].NewOrder(bidOrder);


            //Assert
            Assert.Equal(0, _exchange["ExchangeA"].Ask[price].Count);
            Assert.Equal(0, _exchange["ExchangeA"].Bid[price].Count);
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
            _exchange["ExchangeA"].NewOrder(askOrder1);
            _exchange["ExchangeA"].NewOrder(askOrder2);
            _exchange["ExchangeA"].NewOrder(askOrder3);
            _exchange["ExchangeA"].NewOrder(bidOrder);


            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());

            Assert.Equal(0, _exchange["ExchangeA"].Ask[price].Count);
            Assert.Equal(1, _exchange["ExchangeA"].Bid[price].Count);

            Assert.Equal(2, _exchange["ExchangeA"].GetOrder(bidOrderId).Quantity);

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
            _exchange["ExchangeA"].NewOrder(bidOrder);
            _exchange["ExchangeA"].NewOrder(askOrder1);
            _exchange["ExchangeA"].NewOrder(askOrder2);
            _exchange["ExchangeA"].NewOrder(askOrder3);


            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());
            Assert.Equal(0, _exchange["ExchangeA"].Ask[price].Count); 
            Assert.Equal(1, _exchange["ExchangeA"].Bid[price].Count); 

            Assert.Equal(2, _exchange["ExchangeA"].GetOrder(bidOrderId).Quantity);

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
            _exchange["ExchangeA"].NewOrder(askOrder1);
            _exchange["ExchangeA"].NewOrder(askOrder2);
            _exchange["ExchangeA"].DeleteOrder(askOrder2.Id, "userC".ToLower());
            _exchange["ExchangeA"].NewOrder(askOrder3);
            _exchange["ExchangeA"].NewOrder(bidOrder);

            _exchange["ExchangeA"].RemoveEmptyOrders();


            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());
            Assert.False(_exchange["ExchangeA"].Ask.ContainsKey(price));
            Assert.Equal(1, _exchange["ExchangeA"].Bid[price].Count);

            Assert.Equal(6, _exchange["ExchangeA"].GetOrder(bidOrderId).Quantity);

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
            _exchange["ExchangeA"].NewOrder(bidOrder);
            _exchange["ExchangeA"].DeleteOrder(bidOrderId, "userA".ToLower());
            _exchange["ExchangeA"].NewOrder(askOrder1);
            _exchange["ExchangeA"].NewOrder(askOrder2);
            _exchange["ExchangeA"].NewOrder(askOrder3);

            _exchange["ExchangeA"].RemoveEmptyOrders();


            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());
            Assert.Equal(3, _exchange["ExchangeA"].Ask[price].Count);
            Assert.False(_exchange["ExchangeA"].Bid.ContainsKey(price));

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
            _exchange["ExchangeA"].NewOrder(bidOrder1);
            _exchange["ExchangeA"].NewOrder(bidOrder2);
            bool result1 = _exchange["ExchangeA"].DeleteOrder(bidOrder1.Id, "userA".ToLower());
            bool result2 = _exchange["ExchangeA"].DeleteOrder(bidOrder2.Id, "not_userA".ToLower());
            _exchange["ExchangeA"].RemoveEmptyOrders();
            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());
            Assert.False(_exchange["ExchangeA"].Bid.ContainsKey(priceA));
            Assert.Equal(1, _exchange["ExchangeA"].Bid[priceB].Count); 

            //Assert.Equal(6, exchange.GetOrder(bidOrderId).Quantity);


        }
        public void Dispose()
        {
            
        }
    }
}