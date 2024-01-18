using MarketMaker.Services;
using MarketMaker.Models;

namespace MarketMaker.UnitTests

{
    public class LocalExchangeTests : LocalExchangeService, IDisposable 
    {
        private int _newPrice;

        private int NewPrice
        {
            get => ++_newPrice;
        }
        public LocalExchangeTests()
        {
           NewMarket(); 
           NewMarket(); 
           NewMarket(); 
        }

        [Fact]
        public void InitializeExchangeTest()
        {
            //Arrange

            //Act
                                    
            //Assert
            // Assert.Empty(Market);
        }

        [Fact, ]
        public void AddSingleBid()
        {
            var price = NewPrice;
            //Arrange
            var bidOrder = new Order("userA", "A", price, 1);

            //Act

            ProcessNewOrder(bidOrder);

            //Assert
            Assert.Equal(1, Market["A"].Bid[_newPrice].Count);
        }

        [Fact]
        public void AddSingleAsk()
        {
            var price = NewPrice;
            //Arrange
            var askOrder = new Order("userA", "A", price, -1);

            //Act

            ProcessNewOrder(askOrder);


            //Assert
            Assert.Equal(1, Market["A"].Ask[price].Count);
        }

        [Fact]
        public void HitSingleBid()
        {
            var price = NewPrice;
            //Arrange
            var askOrder = new Order("userA", "A", price, -1);
            var bidOrder = new Order("userB", "A", price, 1);

            //Act
            ProcessNewOrder(bidOrder);
            ProcessNewOrder(askOrder);


            //Assert
            Assert.Equal(0, Market["A"].Ask[price].Count);
            Assert.Equal(0, Market["A"].Bid[price].Count);
        }

        [Fact]
        public void HitSingleAsk()
        {
            var price = NewPrice;
            //Arrange
            var bidOrder = new Order("userB", "A", price, 1);
            var askOrder = new Order("userA", "A", price, -1);

            //Act
            ProcessNewOrder(askOrder);
            ProcessNewOrder(bidOrder);

            //Assert
            Assert.Equal(0, Market["A"].Ask[price].Count);
            Assert.Equal(0, Market["A"].Bid[price].Count);
        }

        [Fact]
        public void OneToMany()
        {
            var price = NewPrice;
            //Arrange
            var bidOrder = new Order("userA", "A", price, 10);
            var askOrder1 = new Order("userB", "A", price, -3);
            var askOrder2 = new Order("userC", "A", price, -4);
            var askOrder3 = new Order("userD", "A", price, -1);
            Guid bidOrderId = bidOrder.Id;

            //Act
            ProcessNewOrder(askOrder1);
            ProcessNewOrder(askOrder2);
            ProcessNewOrder(askOrder3);
            ProcessNewOrder(bidOrder);


            //Assert
            //_testOutputHelper.WriteLine(market._bid[price].ToString());

            Assert.Equal(0, Market["A"].Ask[price].Count);
            Assert.Equal(1, Market["A"].Bid[price].Count);

            Assert.Equal(2, Market["A"].GetOrder(bidOrderId).Quantity);

        }
        [Fact]
        public void ManyToOne()
        {
            var price = NewPrice;
            //Arrange
            var bidOrder = new Order("userA", "A", price, 10);
            var askOrder1 = new Order("userB", "A", price, -3);
            var askOrder2 = new Order("userC", "A", price, -4);
            var askOrder3 = new Order("userD", "A", price, -1);
            Guid bidOrderId = bidOrder.Id;

            //Act
            ProcessNewOrder(bidOrder);
            ProcessNewOrder(askOrder1);
            ProcessNewOrder(askOrder2);
            ProcessNewOrder(askOrder3);


            //Assert
            //_testOutputHelper.WriteLine(market._bid[price].ToString());
            Assert.Equal(0, Market["A"].Ask[price].Count); 
            Assert.Equal(1, Market["A"].Bid[price].Count); 

            Assert.Equal(2, Market["A"].GetOrder(bidOrderId).Quantity);

        }

        [Fact]
        public void OldOrders1()
        {
            var price = NewPrice;
            //Arrange
            var bidOrder = new Order("userA", "A", price, 10);
            var askOrder1 = new Order("userB", "A", price, -3);
            var askOrder2 = new Order("userC", "A", price, -4);
            var askOrder3 = new Order("userD", "A", price, -1);
            Guid bidOrderId = bidOrder.Id;

            //Act
            ProcessNewOrder(askOrder1);
            ProcessNewOrder(askOrder2);
            ProcessDeleteOrder(askOrder2);
            ProcessNewOrder(askOrder3);
            ProcessNewOrder(bidOrder);

            Market["A"].RemoveEmptyOrders();


            //Assert
            //_testOutputHelper.WriteLine(market._bid[price].ToString());
            Assert.False(Market["A"].Ask.ContainsKey(price));
            Assert.Equal(1, Market["A"].Bid[price].Count);

            Assert.Equal(6, Market["A"].GetOrder(bidOrderId).Quantity);

        }
        [Fact]
        public void OldOrders2()
        {
            var price = NewPrice;
            //Arrange
            var bidOrder = new Order("userA", "A", price, 10);
            var askOrder1 = new Order("userB", "A", price, -3);
            var askOrder2 = new Order("userC", "A", price, -4);
            var askOrder3 = new Order("userD", "A", price, -1);
            Guid bidOrderId = bidOrder.Id;

            //Act
            ProcessNewOrder(bidOrder);
            ProcessDeleteOrder(bidOrder);
            ProcessNewOrder(askOrder1);
            ProcessNewOrder(askOrder2);
            ProcessNewOrder(askOrder3);

            Market["A"].RemoveEmptyOrders();


            //Assert
            //_testOutputHelper.WriteLine(market._bid[price].ToString());
            Assert.Equal(3, Market["A"].Ask[price].Count);
            Assert.False(Market["A"].Bid.ContainsKey(price));

            //Assert.Equal(6, market.GetOrder(bidOrderId).Quantity);
        }
        
        [Fact]
        public void UnauthorizedDeletion()
        {
            var priceA = NewPrice;
            var priceB = NewPrice;
            
            //Arrange
            var bidOrder1 = new Order("userA", "A", priceA, 10);
            var bidOrder2 = new Order("userA", "A", priceB, 10);

            //Act
            ProcessNewOrder(bidOrder1);
            ProcessNewOrder(bidOrder2);
            Assert.True(ProcessDeleteOrder(bidOrder1));
            
            Market["A"].RemoveEmptyOrders();
            
            //Assert
            //_testOutputHelper.WriteLine(market._bid[price].ToString());
            Assert.False(Market["A"].Bid.ContainsKey(priceA));
            Assert.Equal(1, Market["A"].Bid[priceB].Count); 

            //Assert.Equal(6, market.GetOrder(bidOrderId).Quantity);
        }
        
        // ordering on different markets
        [Fact]
        public void MultipleMarkets()
        {
            var price = NewPrice;
            var bidOrderA = new Order("userA", "A", price, 10);
            var askOrderA = new Order("userB", "B", price, -10);

            ProcessNewOrder(bidOrderA);
            ProcessNewOrder(askOrderA);
            
            Assert.Equal(1, Market["A"].Bid[price].Count); 
            Assert.Equal(1, Market["B"].Ask[price].Count); 
        }
        // self-buying
        [Fact]
        public void CanSelfBuy()
        {
            var price = NewPrice;
            var bidOrderA = new Order("userA", "A", price, 10);
            var askOrderA = new Order("userA", "A", price, -10);

            ProcessNewOrder(bidOrderA);
            ProcessNewOrder(askOrderA);
            
            Assert.Equal(0, Market["A"].Bid[price].Count); 
            Assert.Equal(0, Market["A"].Ask[price].Count); 
        }
        
        // add market (new, existing, basic ordering)
        [Fact]
        public void CanAddMarket()
        {
            var code = NewMarket();
            // Assert.False(Market.ContainsKey(code));
            Assert.Contains(code, Markets);
        }
        // state transitions
        [Fact]
        public void StateTransitions()
        {
            // Assert.Equal(ExchangeState.Lobby, State);
            //
            // State = ExchangeState.Lobby;
            //
            // Assert.Throws<ArgumentException>(() => State = ExchangeState.Paused);
            // Assert.Throws<ArgumentException>(() => State = ExchangeState.Closed);
            //
            // State = ExchangeState.Open;
            //
            // Assert.Throws<ArgumentException>(() => State = ExchangeState.Lobby);
            //
            // State = ExchangeState.Paused;
            //
            // Assert.Throws<ArgumentException>(() => State = ExchangeState.Lobby);
            //
            // State = ExchangeState.Closed;
            //
            // Assert.Throws<ArgumentException>(() => State = ExchangeState.Open);
            // Assert.Throws<ArgumentException>(() => State = ExchangeState.Paused);
            
        }
        // ordering from non-existent market
        [Fact]
        public void InvalidOrder()
        {
            var price = NewPrice;

            var order = new Order("user", "Market_ERR", price, 10);
            var transactions = ProcessNewOrder(order);
            
            Assert.Null(transactions);
        }
        // deleting non-existent order
        [Fact]
        public void InvalidDeletion()
        {
            var price = NewPrice;
            
            var order = new Order("userA", "A", price, 10);
            var transactions = ProcessNewOrder(order);
            Assert.NotNull(transactions);
            
            var falseOrder1 = new Order("userA", "A", price, 100000);
            var falseOrder2 = new Order("smuserA", "A", price, 10);
            // Assert.False(ProcessDeleteOrder(falseOrder1)); 
            // Assert.False(ProcessDeleteOrder(falseOrder2)); 
            
        }
        
        public void Dispose()
        {
            
        }
    }
}