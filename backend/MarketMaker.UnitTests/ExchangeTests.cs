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
        Exchange exchange;
        private readonly ITestOutputHelper _testOutputHelper;

        public ExchangeTests(ITestOutputHelper testOutputHelper) {
            exchange = new Exchange();
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void InitializeMarketTest()
        {
            //Arange

            //Act
                                    
            //Assert
            Assert.Empty(exchange.GetOrders());
            Assert.Empty(exchange.bid);
            Assert.Empty(exchange.ask);
        }

        [Fact, ]
        public void AddSingleBid()
        {
            //Arange
            Order bidOrder = Order.MakeOrder("userA", "ABC", 100, 1);

            //Act

            exchange.NewOrder(bidOrder);


            //Assert
            Assert.Equal(1, exchange.bid[100].Count);
        }

        [Fact]
        public void AddSingleAsk()
        {
            //Arange
            Order askOrder = Order.MakeOrder("userA", "ABC", 99, -1);

            //Act

            exchange.NewOrder(askOrder);


            //Assert
            Assert.Equal(1, exchange.ask[99].Count);
        }

        [Fact]
        public void HitSingleBid()
        {
            //Arange
            Order askOrder = Order.MakeOrder("userA", "ABC", 50, -1);
            Order bidOrder = Order.MakeOrder("userB", "ABC", 50, 1);

            //Act
            exchange.NewOrder(bidOrder);
            exchange.NewOrder(askOrder);


            //Assert
            Assert.Equal(0, exchange.ask[50].Count);
            Assert.Equal(0, exchange.bid[50].Count);
        }

        [Fact]
        public void HitSingleAsk()
        {
            //Arange
            Order bidOrder = Order.MakeOrder("userB", "ABC", 51, 1);
            Order askOrder = Order.MakeOrder("userA", "ABC", 51, -1);

            //Act
            exchange.NewOrder(askOrder);
            exchange.NewOrder(bidOrder);


            //Assert
            Assert.Equal(0, exchange.ask[51].Count);
            Assert.Equal(0, exchange.bid[51].Count);
        }

        [Fact]
        public void OneToMany()
        {
            int price = 60;
            //Arange
            Order bidOrder = Order.MakeOrder("userA", "ABC", price, 10);
            Order askOrder1 = Order.MakeOrder("userB", "ABC", price, -3);
            Order askOrder2 = Order.MakeOrder("userC", "ABC", price, -4);
            Order askOrder3 = Order.MakeOrder("userD", "ABC", price, -1);
            Guid bidOrderId = bidOrder.Id;

            //Act
            exchange.NewOrder(askOrder1);
            exchange.NewOrder(askOrder2);
            exchange.NewOrder(askOrder3);
            exchange.NewOrder(bidOrder);


            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());

            Assert.Equal(0, exchange.ask[price].Count);
            Assert.Equal(1, exchange.bid[price].Count);

            Assert.Equal(2, exchange.GetOrder(bidOrderId).Quantity);

        }
        [Fact]
        public void ManyToOne()
        {
            int price = 61;
            //Arange
            Order bidOrder = Order.MakeOrder("userA", "ABC", price, 10);
            Order askOrder1 = Order.MakeOrder("userB", "ABC", price, -3);
            Order askOrder2 = Order.MakeOrder("userC", "ABC", price, -4);
            Order askOrder3 = Order.MakeOrder("userD", "ABC", price, -1);
            Guid bidOrderId = bidOrder.Id;

            //Act
            exchange.NewOrder(bidOrder);
            exchange.NewOrder(askOrder1);
            exchange.NewOrder(askOrder2);
            exchange.NewOrder(askOrder3);


            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());
            Assert.Equal(0, exchange.ask[price].Count); 
            Assert.Equal(1, exchange.bid[price].Count); 

            Assert.Equal(2, exchange.GetOrder(bidOrderId).Quantity); 


        }

        [Fact]
        public void OldOrders1()
        {
            int price = 71;
            //Arange
            Order bidOrder = Order.MakeOrder("userA", "ABC", price, 10);
            Order askOrder1 = Order.MakeOrder("userB", "ABC", price, -3);
            Order askOrder2 = Order.MakeOrder("userC", "ABC", price, -4);
            Order askOrder3 = Order.MakeOrder("userD", "ABC", price, -1);
            Guid bidOrderId = bidOrder.Id;

            //Act
            exchange.NewOrder(askOrder1);
            exchange.NewOrder(askOrder2);
            exchange.DeleteOrder(askOrder2.Id);
            exchange.NewOrder(askOrder3);
            exchange.NewOrder(bidOrder);



            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());
            Assert.Equal(0, exchange.ask[price].Count);
            Assert.Equal(1, exchange.bid[price].Count);

            Assert.Equal(6, exchange.GetOrder(bidOrderId).Quantity);


        }
        [Fact]
        public void OldOrders2()
        {
            int price = 71;
            //Arange
            Order bidOrder = Order.MakeOrder("userA", "ABC", price, 10);
            Order askOrder1 = Order.MakeOrder("userB", "ABC", price, -3);
            Order askOrder2 = Order.MakeOrder("userC", "ABC", price, -4);
            Order askOrder3 = Order.MakeOrder("userD", "ABC", price, -1);
            Guid bidOrderId = bidOrder.Id;

            //Act
            exchange.NewOrder(bidOrder);
            exchange.DeleteOrder(bidOrderId);
            exchange.NewOrder(askOrder1);
            exchange.NewOrder(askOrder2);
            exchange.NewOrder(askOrder3);



            //Assert
            //_testOutputHelper.WriteLine(exchange._bid[price].ToString());
            Assert.Equal(3, exchange.ask[price].Count);
            Assert.Equal(0, exchange.bid[price].Count); 

            //Assert.Equal(6, exchange.GetOrder(bidOrderId).Quantity);


        }
        public void Dispose()
        {
            
        }
    }
}