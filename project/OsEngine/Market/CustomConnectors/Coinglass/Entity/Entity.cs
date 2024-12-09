using System;
using OsEngine.Entity;

namespace OsEngine.Market.CustomConnectors.Coinglass.Entity
{
    public class GlobalAccountRatio
    {
        public string time { get; set; }              //time in UTC, seconds
        public string longAccount { get; set; }       //: 56.779999999999994,
        public string shortAccount { get; set; }      //: 43.22,
        public string longShortRatio { get; set; }    //longshort ratio, 1.314
    }

    public class RestResponse<T>
    {
        public string code { get; set; }
        public string msg { get; set; }
        public T data { get; set; } 
    }

    public class  RequestContent
    {
        public string BotName = "";
        public ResponseType ResponseType = ResponseType.LongShortRatio;
        public StartProgram StartProgram = StartProgram.IsOsTrader;
        public string Exchange = "Binance";
        public string Symbol = "BTCUSDT";
        public string Interval = "1d";
        public int Limit = 4500;
        public DateTime StartTime;
        public DateTime EndTime;
    }

    public class LongShortRatio
    {
        public DateTime Time { get; set; }
        public decimal Long { get; set; }
        public decimal Short { get; set; }
        public decimal LSR { get; set; }
    }

    public enum ResponseType
    {
        LongShortRatio,
        //OpenInterest,
        //FundingRate,
        //Liquidation
    }
}
