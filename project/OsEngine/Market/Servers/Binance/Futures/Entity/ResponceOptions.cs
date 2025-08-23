using System.Collections.Generic;
using System.IO.Packaging;

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

    public class ResponseOptionsCandles
    {
        public string open { get; set; }
        public string high { get; set; }
        public string low { get; set; }
        public string close { get; set; }
        public string volume { get; set; }
        public string openTime { get; set; }
    }

    public class ResponceAccount
    {
        public List<Asset> asset { get; set; }
    }

    public class Asset
    {
        public string asset { get; set; }
        public string marginBalance { get; set; }
        public string equity { get; set; }
        public string available { get; set; }
        public string locked { get; set; }
        public string unrealizedPNL { get; set; }
    }

    public class OrderOptionsResponce
    {
        public string orderId { get; set; }
        public string symbol { get; set; }
        public string price { get; set; }
        public string quantity { get; set; }
        public string side { get; set; }
        public string type { get; set; }
        public string createDate { get; set; }
        public string status { get; set; }
        public string clientOrderId { get; set; }
    }

    public class MyTradeOptionsResponce
    {
        public string id { get; set; }
        public string symbol { get; set; }
        public string price { get; set; }
        public string quantity { get; set; }
        public string time { get; set; }
        public string quoteAsset { get; set; }
        public string tradeId { get; set; }
        public string orderId { get; set; }
        public string side { get; set; }
        public string type { get; set; }
    }

    public class PositionsResponce
    {
        public string entryPrice { get; set; }
        public string symbol { get; set; }
        public string side { get; set; }
        public string quantity { get; set; }
        public string unrealizedPNL { get; set; }
    }
    public class OptionsOrderUpdResponse
    {
        public string e; // ":"ORDER_TRADE_UPDATE",     // Event Type
        public string E; //":1568879465651,            // Event Time
        public string T; //": 1568879465650,           //  Transaction Time

        public List<OptionsOrderResp> o; // order
    }

    public class OptionsOrderResp
    {
        public string T;              //Order Create Time
        public string t;              //Order Update Time
        public string s;              //Symbol
        public string c;              //clientOrderId
        public string oid;            //order id
        public string p;              //order price
        public string q;              //order quantity (positive for BUY, negative for SELL)
        public string S;              //status
        public string e;              //completed trade volume(in contracts)       
        public string ec;             //completed trade amount(in quote asset)       
        public string oty;            //order type
        public List<OptionsTrades> fi;       
    }

    public class OptionsTrades
    {
        public string t;                   //tradeId
        public string p;                 //trade price
        public string q;                 //trade quantity
        public string T;          //trade time       
    }

}
