using System.Collections.Generic;


namespace OsEngine.Market.Servers.Esunny.Entity
{
    public class ResponceMessageSecurity
    {
        public List<Data> symbols;
    }

    public class Data
    {
        public string symbol;
        public string contractSize;
        public string contractTickSize;
        public string exchange;
        public string contractType;
    }

    public class ResponceMessageAccount
    {
        public string accountNo;
        public string equity;
        public string avail;
        public string frozen;
    }
}
