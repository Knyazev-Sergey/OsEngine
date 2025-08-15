using System.Collections.Generic;

namespace OsEngine.Market.Servers.Binance.Futures.Entity
{
    public class ResponceOptions
    {
        public string timezone { get; set; }
        public string serverTime { get; set; }
        public List<OptionContracts> optionContracts { get; set; }
        public List<object> exchangeFilters { get; set; }
        public List<OptionSymbols> optionSymbols { get; set; }        
    }

    public class OptionContracts
    {
    }

    public class OptionSymbols
    {
        public string symbol { get; set; }
        
        public List<Filters> filters { get; set; }
        public string side { get; set; }
        public string strikePrice { get; set; }
        public string underlying { get; set; }
        public string expiryDate { get; set; }
        public string minQty { get; set; }
        public string maxQty { get; set; }
        public string quoteAsset { get; set; }

    }

    public class Filters
    {
        public string filterType { get; set; }
        public string minPrice { get; set; }
        public string maxPrice { get; set; }
        public string tickSize { get; set; }
        public string minQty { get; set; }
        public string maxQty { get; set; }
        public string stepSize { get; set; }
    }
}
