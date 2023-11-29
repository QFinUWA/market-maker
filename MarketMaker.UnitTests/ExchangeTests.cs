using MarketMaker.Services;
using MarketMaker.Models;

using Xunit;
using System;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MarketMaker.UnitTests

{
    public class ExchangeTests : IDisposable
    {
        private readonly Exchange _exchange;
        private readonly ITestOutputHelper _testOutputHelper;

        public ExchangeTests(ITestOutputHelper testOutputHelper)
        {
            _exchange = new Exchange();
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void InitializeMarketTest()
        {
            //Arange

            //Act
                                    
            //Assert
            Assert.Empty(_exchange.Orders);
            Assert.Empty(_exchange.Bid);
            Assert.Empty(_exchange.Ask);
        }

        [Fact, ]
        public void AddSingleBid()
        {
            //Arange
            Order bidOrder = new Order("userA", "ABC", 100, 1);

            //Act

            _exchange.NewOrder(bidOrder);


            //Assert
            Assert.Equal(1, _exchange.Bid[100].Count);
        }

        [Fact]
        public void AddSingleAsk()
        {
            //Arange
            Order askOrder = new Order("userA", "ABC", 99, -1);

            //Act

            _exchange.NewOrder(askOrder);


            //Assert
            Assert.Equal(1, _exchange.Ask[99].Count);
        }

        [Fact]
        public void HitSingleBid()
        {
            //Arange
            Order askOrder = new Order("userA", "ABC", 50, -1);
            Order bidOrder = new Order("userB", "ABC", 50, 1);

            //Act
            _exchange.NewOrder(bidOrder);
            _exchange.NewOrder(askOrder);


            //Assert
            Assert.Equal(0, _exchange.Ask[50].Count);
            Assert.Equal(0, _exchange.Bid[50].Count);
        }

        [Fact]
        public void HitSingleAsk()
        {
            //Arange
            Order bidOrder = new Order("userB", "ABC", 51, 1);
            Order askOrder = new Order("userA", "ABC", 51, -1);

            //Act
            _exchange.NewOrder(askOrder);
            _exchange.NewOrder(bidOrder);


            //Assert
            Assert.Equal(0, _exchange.Ask[51].Count);
            Assert.Equal(0, _exchange.Bid[51].Count);
        }

        [Fact]
        public void OneToMany()
        {
            int price = 60;
            //Arange
            Order bidOrder = new Order("userA", "ABC", price, 10);
            Order askOrder1 = new Order("userB", "ABC", price, -3);
            Order askOrder2 = new Order("userC", "ABC", price, -4);
            Order askOrder3 = new Order("userD", "ABC", price, -1);
            Guid bidOrderId = bidOrder.Id;

            //Act
            _exchange.NewOrder(askOrder1);
            _exchange.NewOrder(askOrder2);
            _exchange.NewOrder(askOrder3);
            _exchange.NewOrder(bidOrder);


            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());

            Assert.Equal(0, _exchange.Ask[price].Count);
            Assert.Equal(1, _exchange.Bid[price].Count);

            Assert.Equal(2, _exchange.GetOrder(bidOrderId).Quantity);

        }
        [Fact]
        public void ManyToOne()
        {
            int price = 61;
            //Arange
            Order bidOrder = new Order("userA", "ABC", price, 10);
            Order askOrder1 = new Order("userB", "ABC", price, -3);
            Order askOrder2 = new Order("userC", "ABC", price, -4);
            Order askOrder3 = new Order("userD", "ABC", price, -1);
            Guid bidOrderId = bidOrder.Id;

            //Act
            _exchange.NewOrder(bidOrder);
            _exchange.NewOrder(askOrder1);
            _exchange.NewOrder(askOrder2);
            _exchange.NewOrder(askOrder3);


            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());
            Assert.Equal(0, _exchange.Ask[price].Count); 
            Assert.Equal(1, _exchange.Bid[price].Count); 

            Assert.Equal(2, _exchange.GetOrder(bidOrderId).Quantity); 


        }

        [Fact]
        public void OldOrders1()
        {
            int price = 71;
            //Arange
            Order bidOrder = new Order("userA", "ABC", price, 10);
            Order askOrder1 = new Order("userB", "ABC", price, -3);
            Order askOrder2 = new Order("userC", "ABC", price, -4);
            Order askOrder3 = new Order("userD", "ABC", price, -1);
            Guid bidOrderId = bidOrder.Id;

            //Act
            _exchange.NewOrder(askOrder1);
            _exchange.NewOrder(askOrder2);
            _exchange.DeleteOrder(askOrder2.Id, "userC".ToLower());
            _exchange.NewOrder(askOrder3);
            _exchange.NewOrder(bidOrder);

            _exchange.RemoveEmptyOrders();


            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());
            Assert.False(_exchange.Ask.ContainsKey(price));
            Assert.Equal(1, _exchange.Bid[price].Count);

            Assert.Equal(6, _exchange.GetOrder(bidOrderId).Quantity);


        }
        [Fact]
        public void OldOrders2()
        {
            int price = 71;
            //Arange
            Order bidOrder = new Order("userA", "ABC", price, 10);
            Order askOrder1 = new Order("userB", "ABC", price, -3);
            Order askOrder2 = new Order("userC", "ABC", price, -4);
            Order askOrder3 = new Order("userD", "ABC", price, -1);
            Guid bidOrderId = bidOrder.Id;

            //Act
            _exchange.NewOrder(bidOrder);
            _exchange.DeleteOrder(bidOrderId, "userA".ToLower());
            _exchange.NewOrder(askOrder1);
            _exchange.NewOrder(askOrder2);
            _exchange.NewOrder(askOrder3);

            _exchange.RemoveEmptyOrders();


            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());
            Assert.Equal(3, _exchange.Ask[price].Count);
            Assert.False(_exchange.Bid.ContainsKey(price));

            //Assert.Equal(6, exchange.GetOrder(bidOrderId).Quantity);


        }
        
        [Fact]
        public void UnorthorisedDeletion()
        {
            int price = 71;
            //Arange
            Order bidOrder1 = new Order("userA", "ABC", price, 10);
            Order bidOrder2 = new Order("userA", "ABC", price+1, 10);

            //Act
            _exchange.NewOrder(bidOrder1);
            _exchange.NewOrder(bidOrder2);
            bool result1 = _exchange.DeleteOrder(bidOrder1.Id, "userA".ToLower());
            bool result2 = _exchange.DeleteOrder(bidOrder2.Id, "not_userA".ToLower());
            _exchange.RemoveEmptyOrders();

            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());
            Assert.False(_exchange.Bid.ContainsKey(price));
            Assert.Equal(1, _exchange.Bid[price+1].Count); 

            //Assert.Equal(6, exchange.GetOrder(bidOrderId).Quantity);


        }
        public void Dispose()
        {
            
        }
    }
}