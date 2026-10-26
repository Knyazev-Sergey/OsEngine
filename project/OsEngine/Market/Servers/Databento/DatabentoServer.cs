/*
 *Your rights to use the code are governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using Newtonsoft.Json.Linq;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market.Servers.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace OsEngine.Market.Servers.Databento
{
    public class DatabentoServer : AServer
    {
        public DatabentoServer()
        {
            ServerRealization = new DatabentoServerRealization();
            CreateParameterPassword("API key", "");
            CreateParameterString("Dataset", "GLBX.MDP3");
            CreateParameterString("Symbol", "ES.FUT");
            CreateParameterEnum("Symbology", "parent", new List<string> { "parent", "raw_symbol" });
            CreateParameterEnum("Book depth", "mbp-1", new List<string> { "mbp-1", "mbp-10", "mbo" });
        }
    }

    public class DatabentoServerRealization : IServerRealization
    {
        private TcpClient _client;
        private CancellationTokenSource _cancel;
        private string _apiKey;
        private string _dataset;
        private string _symbol;
        private string _symbology;
        private string _depth;
        private Security _security;

        public DatabentoServerRealization() { ServerStatus = ServerConnectStatus.Disconnect; }
        public ServerType ServerType { get { return ServerType.Databento; } }
        public ServerConnectStatus ServerStatus { get; set; }
        public DateTime ServerTime { get; set; }
        public List<IServerParameter> ServerParameters { get; set; }
        public bool IsCompletelyDeleted { get; set; }
        public event Action ConnectEvent;
        public event Action DisconnectEvent;
        public event Action ForceCheckOrdersAfterReconnectEvent { add { } remove { } }
        public event Action<List<Security>> SecurityEvent;
        public event Action<List<Portfolio>> PortfolioEvent;
        public event Action<MarketDepth> MarketDepthEvent;
        public event Action<Trade> NewTradesEvent;
        public event Action<News> NewsEvent;
        public event Action<Funding> FundingUpdateEvent;
        public event Action<SecurityVolumes> Volume24hUpdateEvent;
        public event Action<OptionMarketDataForConnector> AdditionalMarketDataEvent { add { } remove { } }
        public event Action<Order> MyOrderEvent;
        public event Action<MyTrade> MyTradeEvent;
        public event Action<string, LogMessageType> LogMessageEvent;

        public void Connect(WebProxy proxy)
        {
            try
            {
                _apiKey = ((ServerParameterPassword)ServerParameters[0]).Value;
                _dataset = ((ServerParameterString)ServerParameters[1]).Value;
                _symbol = ((ServerParameterString)ServerParameters[2]).Value;
                _symbology = ((ServerParameterEnum)ServerParameters[3]).Value;
                _depth = ((ServerParameterEnum)ServerParameters[4]).Value;
                string host = _dataset.ToLowerInvariant().Replace('.', '-') + ".lsg.databento.com";
                _client = new TcpClient();
                _client.Connect(host, 13000);
                _cancel = new CancellationTokenSource();
                Thread reader = new Thread(ReadStream) { IsBackground = true, Name = "DatabentoRawReader" };
                reader.Start();
            }
            catch (Exception error) { SendLogMessage(error.ToString(), LogMessageType.Error); Disconnect(); }
        }

        private void ReadStream()
        {
            try
            {
                using NetworkStream stream = _client.GetStream();
                using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                using StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\n", AutoFlush = true };
                string greeting = reader.ReadLine();
                string challenge = reader.ReadLine();
                if (challenge == null || !challenge.StartsWith("cram=")) throw new InvalidOperationException("Databento challenge was not received");
                string digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(challenge.Substring(5) + "|" + _apiKey))).ToLowerInvariant();
                writer.WriteLine($"auth={digest}-{_apiKey.Substring(Math.Max(0, _apiKey.Length - 5))}|dataset={_dataset}|encoding=json|ts_out=0|heartbeat_interval_s=30");
                string auth = reader.ReadLine();
                if (auth == null || !auth.Contains("success=1")) throw new InvalidOperationException("Databento authentication failed: " + auth);
                ServerStatus = ServerConnectStatus.Connect;
                ConnectEvent?.Invoke();
                writer.WriteLine("schema=trades|stype_in=" + _symbology + "|symbols=" + _symbol);
                writer.WriteLine("schema=" + _depth + "|stype_in=" + _symbology + "|symbols=" + _symbol);
                while (!_cancel.IsCancellationRequested)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    ProcessRecord(JObject.Parse(line));
                }
            }
            catch (Exception error) { if (!_cancel.IsCancellationRequested) SendLogMessage(error.ToString(), LogMessageType.Error); }
            finally { Disconnect(); }
        }

        private void ProcessRecord(JObject record)
        {
            JObject data = record["data"] as JObject ?? record;
            string type = (string)(record["schema"] ?? record["type"] ?? data["type"]);
            DateTime time = DateTime.UtcNow;
            if (data["ts_event"] != null && long.TryParse(data["ts_event"].ToString(), out long nanos)) time = DateTime.UnixEpoch.AddTicks(nanos / 100);
            if (type != null && type.IndexOf("trade", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Trade trade = new Trade { SecurityNameCode = _security?.Name ?? _symbol, Price = DecimalValue(data, "price"), Volume = DecimalValue(data, "size"), Time = time, Id = (string)data["sequence"] };
                NewTradesEvent?.Invoke(trade);
            }
            else if (type != null && (type.IndexOf("mbp", StringComparison.OrdinalIgnoreCase) >= 0 || type.IndexOf("mbo", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                MarketDepth depth = new MarketDepth { SecurityNameCode = _security?.Name ?? _symbol, Time = time };
                AddLevel(depth.Bids, data["bid_px"], data["bid_sz"]);
                AddLevel(depth.Asks, data["ask_px"], data["ask_sz"]);
                MarketDepthEvent?.Invoke(depth);
            }
        }

        private static decimal DecimalValue(JObject data, string name) { return decimal.TryParse(data[name]?.ToString(), out decimal value) ? value : 0; }
        private static void AddLevel(List<MarketDepthLevel> levels, JToken price, JToken size) { if (price != null && size != null) levels.Add(new MarketDepthLevel { Price = (double)DecimalValue(new JObject { ["v"] = price }, "v"), Ask = 0, Bid = (double)DecimalValue(new JObject { ["v"] = size }, "v") }); }
        private void SendLogMessage(string message, LogMessageType type) { LogMessageEvent?.Invoke(message, type); if (LogMessageEvent == null) ServerMaster.SendNewLogMessage(message, type); }
        public void Dispose() { try { _cancel?.Cancel(); _client?.Close(); } catch (Exception error) { SendLogMessage(error.ToString(), LogMessageType.Error); } Disconnect(); }
        private void Disconnect() { if (ServerStatus != ServerConnectStatus.Disconnect) { ServerStatus = ServerConnectStatus.Disconnect; DisconnectEvent?.Invoke(); } }
        public void GetSecurities() { _security = new Security { Name = _symbol, NameFull = _symbol, NameId = _symbol, Exchange = ServerType.Databento.ToString(), SecurityType = SecurityType.Futures, State = SecurityStateType.Activ, Lot = 1 }; SecurityEvent?.Invoke(new List<Security> { _security }); }
        public void GetPortfolios() { PortfolioEvent?.Invoke(new List<Portfolio> { new Portfolio { Number = "Databento Market Data", ValueCurrent = 1 } }); }
        public void Subscribe(Security security) { _security = security; }
        public void SetLeverage(Security security, decimal leverage) { }
        public bool SubscribeNews() { return false; }
        public List<Candle> GetLastCandleHistory(Security security, TimeFrameBuilder timeFrameBuilder, int candleCount) { return new List<Candle>(); }
        public List<Candle> GetCandleDataToSecurity(Security security, TimeFrameBuilder timeFrameBuilder, DateTime startTime, DateTime endTime, DateTime actualTime) { return new List<Candle>(); }
        public List<Trade> GetTickDataToSecurity(Security security, DateTime startTime, DateTime endTime, DateTime actualTime) { return new List<Trade>(); }
        public void SendOrder(Order order) { }
        public void ChangeOrderPrice(Order order, decimal newPrice) { }
        public bool CancelOrder(Order order) { return false; }
        public void CancelAllOrders() { }
        public void CancelAllOrdersToSecurity(Security security) { }
        public void GetAllActivOrders() { }
        public OrderStateType GetOrderStatus(Order order) { return OrderStateType.None; }
        public List<Order> GetActiveOrders(int startIndex, int count) { return new List<Order>(); }
        public List<Order> GetHistoricalOrders(int startIndex, int count) { return new List<Order>(); }
    }
}
