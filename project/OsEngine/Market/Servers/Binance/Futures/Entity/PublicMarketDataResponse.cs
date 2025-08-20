using System.Collections.Generic;

namespace OsEngine.Market.Servers.Binance.Futures.Entity
{
    public class PublicMarketDataResponse<T>
    {
        public string stream { get; set; }
        public T data { get; set; }

    }

    public class PublicMarketDataFunding
    {
        public string s; // Security

        public string r; // Funding rate

        public string T; // Next Funding Time

        public string E; // event time
    }

    public class PublicMarketDataVolume24h
    {
        public string s; // Security

        public string v; // Total traded base asset volume

        public string q; // Total traded quote asset volume

        public string E; // event time
    }

    public class PublicMarketDataOptions
    {
        public string s; // Security

        public string b; // BuyImplied volatility   

        public string a; // SellImplied volatility

        public string E; // event time

        public string d; // delta

        public string t; // theta   

        public string g; // gamma

        public string v; // vega

        public string vo; // IV

        public string mp; // mark price
    }

    public class PublicMarketDataOptionsOI
    {
        public string s; // Security

        public string o; // Open interest in contracts 

        public string h; // Open interest in USDT      
    }

    public class PublicMarketDataOptionsUnderlyingPrice
    {
        public string s; // underlying symbol

        public string p; // index price
    }

    public class FundingInfo
    {
        public string symbol;
        public string adjustedFundingRateCap;
        public string adjustedFundingRateFloor;
        public string fundingIntervalHours;
    }

    public class FundingHistory
    {
        public string symbol;
        public string fundingTime;
    }

    public class OpenInterestInfo
    {
        public string openInterest;
        public string symbol;
        public string time;
    }
}