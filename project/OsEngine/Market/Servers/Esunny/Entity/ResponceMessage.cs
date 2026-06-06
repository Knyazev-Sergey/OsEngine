using System.Collections.Generic;


namespace OsEngine.Market.Servers.Esunny.Entity
{
    public class ResponceMessageSecurity
    {
        public List<Data> list;
    }

    public class Data
    {
        public string exchangeId;
        public string commodityType;
        public string contractIndex;
        public string contractSize;
        public string contractNo;
        public string contractTickSize;
        public string preSettlePrice;
        public string limitUpPrice;
        public string limitDownPrice;
        public string expDate;
    }

    public class ResponceMessageAccount
    {
        public string accountNo;
        public string equity;
        public string avail;
        public string margin;
        public string positionProfit;
    }

    public class ResponceMessagePositions
    {
        public List<ListPositions> list;        
    }

    public class ListPositions
    {
        public string accountNo;
        public string contractNo;
        public string preBuyQty;
        public string todayBuyQty;
        public string buyAvgPrice;
        public string preSellQty;
        public string todaySellQty;
        public string sellAvgPrice;
    }
}
