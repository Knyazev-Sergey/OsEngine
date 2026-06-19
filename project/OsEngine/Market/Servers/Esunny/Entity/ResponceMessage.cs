/*
 *Your rights to use the code are governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

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

    public class ResponceMessageQuote
    {
        public string contractNo;
        public string dateTimeStamp;
        public string lastPrice;
        public string lastQty;

        public string bidPrice1;
        public string bidPrice2;
        public string bidPrice3;
        public string bidPrice4;
        public string bidPrice5;
        public string bidPrice6;
        public string bidPrice7;
        public string bidPrice8;
        public string bidPrice9;
        public string bidPrice10;

        public string bidQty1;
        public string bidQty2;
        public string bidQty3;
        public string bidQty4;
        public string bidQty5;
        public string bidQty6;
        public string bidQty7;
        public string bidQty8;
        public string bidQty9;
        public string bidQty10;

        public string askPrice1;
        public string askPrice2;
        public string askPrice3;
        public string askPrice4;
        public string askPrice5;
        public string askPrice6;
        public string askPrice7;
        public string askPrice8;
        public string askPrice9;
        public string askPrice10;

        public string askQty1;
        public string askQty2;
        public string askQty3;
        public string askQty4;
        public string askQty5;
        public string askQty6;
        public string askQty7;
        public string askQty8;
        public string askQty9;
        public string askQty10;
    }

    public class ResponceMessageMarketDataError
    {
        public string code;
        public string message;
    }

    public class ResponceMessageMyOrder
    {
        public string accountNo;
        public string contractNo1;
        public string contractNo2;
        public string direct;
        public string matchQty;
        public string offset;
        public string orderId;
        public string orderLocalNo;
        public string orderPrice;
        public string orderQty;
        public string orderState;
        public string orderType;
        public string updateTime;
        public string errCode;
        public string reference;
    }

    public class ResponceMessageMyTrade
    {
        public string accountNo;
        public string contractNo;
        public string direct;
        public string matchId;
        public string matchPrice;
        public string matchQty;
        public string matchTime;
        public string offset;
        public string orderId;
        public string orderType;
        public string updateTime;
        public string serialId;
    }

    public class ResponceMessageOrderNumber
    {
        public string accountNo;
        public string clientReqId;
        public string orderId;        
    }
}
