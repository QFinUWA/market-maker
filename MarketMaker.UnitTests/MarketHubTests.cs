using MarketMaker.Services;
using MarketMaker.Models;

using Xunit;
using System;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MarketMaker.UnitTests
{
        
    public class MarketHubTests : IDisposable
    {
        private MarketService _market;
        private ITestOutputHelper _testOutputHelper; 
        
        public MarketHubTests(ITestOutputHelper testOutputHelper)
        {
            _market = new LocalMarketService();
            _testOutputHelper = testOutputHelper;
        }
    
        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}
