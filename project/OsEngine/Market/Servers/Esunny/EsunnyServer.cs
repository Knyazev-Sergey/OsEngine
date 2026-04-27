using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market.Servers.Entity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace OsEngine.Market.Servers.Esunny
{
    public class EsunnyServer : AServer
    {
        public EsunnyServer()
        {
            ServerRealization = new EsunnyServerRealization();

            CreateParameterString("Router host", "127.0.0.1"); // 0
            CreateParameterInt("Router port", 19090); // 1
            CreateParameterBoolean("Auto start router", false); // 2
            CreateParameterString("Router exe path", @"Engine\Routers\Esunny\EsunnyRouter.exe"); // 3
            CreateParameterString("Front IP", "101.132.153.16"); // 4
            CreateParameterInt("Front port", 6668); // 5
            CreateParameterString("Account", ""); // 6
            CreateParameterPassword("Password", ""); // 7
            CreateParameterString("AppId", "esunny_epolestar_9.0"); // 8
            CreateParameterString("LicenseNo", "esunny_epolestar"); // 9
            CreateParameterBoolean("Use UDP", false); // 10
            CreateParameterInt("Seat index", 1); // 11
        }
    }

    public class EsunnyServerRealization : IServerRealization
    {
        public ServerType ServerType => ServerType.Esunny;

        public ServerConnectStatus ServerStatus { get; set; }

        public DateTime ServerTime { get; set; }

        public List<IServerParameter> ServerParameters { get; set; }

        public bool IsCompletelyDeleted { get; set; }

        public event Action ConnectEvent;
        public event Action DisconnectEvent;
        public event Action ForceCheckOrdersAfterReconnectEvent { add { } remove { } }
        public event Action<List<Security>> SecurityEvent;
        public event Action<List<Portfolio>> PortfolioEvent;
        public event Action<News> NewsEvent { add { } remove { } }
        public event Action<MarketDepth> MarketDepthEvent;
        public event Action<Trade> NewTradesEvent;
        public event Action<Funding> FundingUpdateEvent { add { } remove { } }
        public event Action<SecurityVolumes> Volume24hUpdateEvent { add { } remove { } }
        public event Action<Order> MyOrderEvent;
        public event Action<MyTrade> MyTradeEvent;
        public event Action<string, LogMessageType> LogMessageEvent;
        public event Action<OptionMarketDataForConnector> AdditionalMarketDataEvent { add { } remove { } }

        private readonly object _sendLocker = new object();
        private readonly object _portfolioLocker = new object();
        private readonly List<Security> _securities = new List<Security>();
        private readonly Dictionary<string, Portfolio> _portfolios = new Dictionary<string, Portfolio>();

        private TcpClient _client;
        private StreamWriter _writer;
        private StreamReader _reader;
        private Thread _readerThread;
        private bool _needStop;
        private Process _routerProcess;

        private int _clientOrderId;
        private string _routerHost;
        private int _routerPort;
        private int _seatIndex;

        public EsunnyServerRealization()
        {
            ServerStatus = ServerConnectStatus.Disconnect;
        }

        public void Connect(WebProxy proxy)
        {
            try
            {
                if (ServerParameters == null || ServerParameters.Count < 12)
                {
                    SendLogMessage("Esunny: invalid server parameters", LogMessageType.Error);
                    return;
                }

                _routerHost = ((ServerParameterString)ServerParameters[0]).Value;
                _routerPort = ((ServerParameterInt)ServerParameters[1]).Value;
                bool autoStartRouter = ((ServerParameterBool)ServerParameters[2]).Value;
                string routerPath = ((ServerParameterString)ServerParameters[3]).Value;
                string frontIp = ((ServerParameterString)ServerParameters[4]).Value;
                int frontPort = ((ServerParameterInt)ServerParameters[5]).Value;
                string account = ((ServerParameterString)ServerParameters[6]).Value;
                string password = ((ServerParameterPassword)ServerParameters[7]).Value;
                string appId = ((ServerParameterString)ServerParameters[8]).Value;
                string licenseNo = ((ServerParameterString)ServerParameters[9]).Value;
                bool useUdp = ((ServerParameterBool)ServerParameters[10]).Value;
                _seatIndex = ((ServerParameterInt)ServerParameters[11]).Value;

                if (autoStartRouter)
                {
                    TryStartRouter(routerPath);
                }

                if (_client != null && _client.Connected)
                {
                    SendCommand(new
                    {
                        type = "connect",
                        front_ip = frontIp,
                        front_port = frontPort,
                        account,
                        password,
                        app_id = appId,
                        license_no = licenseNo,
                        use_udp = useUdp
                    });
                    return;
                }

                _client = new TcpClient();
                _client.Connect(_routerHost, _routerPort);
                NetworkStream stream = _client.GetStream();
                _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                _reader = new StreamReader(stream, Encoding.UTF8);

                _needStop = false;
                _readerThread = new Thread(ReadLoop);
                _readerThread.IsBackground = true;
                _readerThread.Name = "EsunnyRouterReader";
                _readerThread.Start();

                SendCommand(new
                {
                    type = "connect",
                    front_ip = frontIp,
                    front_port = frontPort,
                    account,
                    password,
                    app_id = appId,
                    license_no = licenseNo,
                    use_udp = useUdp
                });

                SendLogMessage($"Esunny: connected to router {_routerHost}:{_routerPort}", LogMessageType.System);
            }
            catch (Exception ex)
            {
                SendLogMessage($"Esunny connect error: {ex.Message}", LogMessageType.Error);
                SetDisconnected();
            }
        }

        public void Dispose()
        {
            try
            {
                _needStop = true;
                SendCommand(new { type = "disconnect" });
            }
            catch
            {
                // ignore
            }

            try
            {
                _reader?.Close();
                _writer?.Close();
                _client?.Close();
                _reader = null;
                _writer = null;
                _client = null;
            }
            catch
            {
                // ignore
            }

            SetDisconnected();
        }

        public void GetSecurities()
        {
            SendCommand(new { type = "get_securities" });
        }

        public void SetLeverage(Security security, decimal leverage)
        {
            // not supported
        }

        public void GetPortfolios()
        {
            SendCommand(new { type = "get_portfolios" });
        }

        public void Subscribe(Security security)
        {
            if (security == null || string.IsNullOrEmpty(security.Name))
            {
                return;
            }

            SendCommand(new { type = "subscribe", security = security.Name });
        }

        public void Unsubscribe(Security security)
        {
            if (security == null || string.IsNullOrEmpty(security.Name))
            {
                return;
            }

            SendCommand(new { type = "unsubscribe", security = security.Name });
        }

        public bool SubscribeNews()
        {
            return false;
        }

        public List<Candle> GetLastCandleHistory(Security security, TimeFrameBuilder timeFrameBuilder, int candleCount)
        {
            return null;
        }

        public List<Candle> GetCandleDataToSecurity(Security security, TimeFrameBuilder timeFrameBuilder, DateTime startTime, DateTime endTime, DateTime actualTime)
        {
            return null;
        }

        public List<Trade> GetTickDataToSecurity(Security security, DateTime startTime, DateTime endTime, DateTime actualTime)
        {
            return null;
        }

        public void SendOrder(Order order)
        {
            if (order == null)
            {
                return;
            }

            if (order.NumberUser == 0)
            {
                _clientOrderId++;
                order.NumberUser = _clientOrderId;
            }

            string offset = "Open";
            if (order.PositionConditionType == OrderPositionConditionType.Close)
            {
                offset = "Close";
            }

            SendCommand(new
            {
                type = "send_order",
                security = order.SecurityNameCode,
                side = order.Side.ToString(),
                offset,
                price = order.Price,
                volume = order.Volume,
                order_type = order.TypeOrder == OrderPriceType.Market ? "Market" : "Limit",
                client_req_id = order.NumberUser,
                portfolio = order.PortfolioNumber,
                seat_index = _seatIndex
            });
        }

        public void ChangeOrderPrice(Order order, decimal newPrice)
        {
            SendLogMessage("Esunny: ChangeOrderPrice is not supported", LogMessageType.Error);
        }

        public bool CancelOrder(Order order)
        {
            if (order == null)
            {
                return false;
            }

            SendCommand(new
            {
                type = "cancel_order",
                number_market = order.NumberMarket,
                client_req_id = order.NumberUser,
                seat_index = _seatIndex
            });

            return true;
        }

        public void CancelAllOrders()
        {
            SendCommand(new { type = "cancel_all" });
        }

        public void CancelAllOrdersToSecurity(Security security)
        {
            if (security == null)
            {
                return;
            }
            SendCommand(new { type = "cancel_all", security = security.Name });
        }

        public void GetAllActivOrders()
        {
            SendCommand(new { type = "get_active_orders" });
        }

        public OrderStateType GetOrderStatus(Order order)
        {
            return OrderStateType.None;
        }

        public List<Order> GetActiveOrders(int startIndex, int count)
        {
            return null;
        }

        public List<Order> GetHistoricalOrders(int startIndex, int count)
        {
            return null;
        }

        private void ReadLoop()
        {
            while (!_needStop)
            {
                try
                {
                    string line = _reader.ReadLine();
                    if (line == null)
                    {
                        SetDisconnected();
                        return;
                    }

                    ProcessIncoming(line);
                }
                catch (Exception ex)
                {
                    if (_needStop)
                    {
                        return;
                    }

                    SendLogMessage($"Esunny read error: {ex.Message}", LogMessageType.Error);
                    SetDisconnected();
                    return;
                }
            }
        }

        private void ProcessIncoming(string line)
        {
            JObject message;
            try
            {
                message = JObject.Parse(line);
            }
            catch
            {
                SendLogMessage($"Esunny invalid message: {line}", LogMessageType.Error);
                return;
            }

            string type = message.Value<string>("type");
            if (string.IsNullOrEmpty(type))
            {
                return;
            }

            if (type == "connected")
            {
                ServerStatus = ServerConnectStatus.Connect;
                ConnectEvent?.Invoke();
                return;
            }

            if (type == "disconnected")
            {
                SetDisconnected();
                return;
            }

            if (type == "log")
            {
                string msg = message.Value<string>("message");
                string level = message.Value<string>("level");
                LogMessageType logType = LogMessageType.System;
                if (string.Equals(level, "error", StringComparison.OrdinalIgnoreCase))
                {
                    logType = LogMessageType.Error;
                }
                SendLogMessage(msg, logType);
                return;
            }

            if (type == "securities")
            {
                JArray arr = message.Value<JArray>("data");
                List<Security> result = new List<Security>();
                if (arr != null)
                {
                    for (int i = 0; i < arr.Count; i++)
                    {
                        Security sec = ParseSecurity(arr[i] as JObject);
                        if (sec != null)
                        {
                            result.Add(sec);
                        }
                    }
                }

                if (result.Count > 0)
                {
                    _securities.Clear();
                    _securities.AddRange(result);
                    SecurityEvent?.Invoke(result);
                }
                return;
            }

            if (type == "portfolio")
            {
                ParsePortfolio(message.Value<JObject>("data"));
                return;
            }

            if (type == "portfolios")
            {
                JArray arr = message.Value<JArray>("data");
                if (arr != null)
                {
                    for (int i = 0; i < arr.Count; i++)
                    {
                        ParsePortfolio(arr[i] as JObject);
                    }
                }
                return;
            }

            if (type == "order")
            {
                Order order = ParseOrder(message.Value<JObject>("data"));
                if (order != null)
                {
                    MyOrderEvent?.Invoke(order);
                }
                return;
            }

            if (type == "my_trade")
            {
                MyTrade trade = ParseMyTrade(message.Value<JObject>("data"));
                if (trade != null)
                {
                    MyTradeEvent?.Invoke(trade);
                }
                return;
            }

            if (type == "trade")
            {
                Trade trade = ParseTrade(message.Value<JObject>("data"));
                if (trade != null)
                {
                    NewTradesEvent?.Invoke(trade);
                }
                return;
            }

            if (type == "depth")
            {
                MarketDepth depth = ParseDepth(message.Value<JObject>("data"));
                if (depth != null)
                {
                    MarketDepthEvent?.Invoke(depth);
                }
                return;
            }

            if (type == "heartbeat")
            {
                DateTime serverTime;
                if (TryParseDateTime(message.Value<string>("server_time"), out serverTime))
                {
                    ServerTime = serverTime;
                }
            }
        }

        private void ParsePortfolio(JObject data)
        {
            if (data == null)
            {
                return;
            }

            string number = data.Value<string>("number");
            if (string.IsNullOrEmpty(number))
            {
                return;
            }

            lock (_portfolioLocker)
            {
                if (!_portfolios.TryGetValue(number, out Portfolio portfolio))
                {
                    portfolio = new Portfolio { Number = number };
                    _portfolios[number] = portfolio;
                }

                portfolio.ValueBegin = GetDecimal(data["value_begin"]);
                portfolio.ValueCurrent = GetDecimal(data["value_current"]);
                portfolio.ValueBlocked = GetDecimal(data["value_blocked"]);

                JArray positions = data.Value<JArray>("positions");
                if (positions != null)
                {
                    for (int i = 0; i < positions.Count; i++)
                    {
                        JObject pos = positions[i] as JObject;
                        if (pos == null)
                        {
                            continue;
                        }

                        PositionOnBoard position = new PositionOnBoard();
                        position.PortfolioName = number;
                        position.SecurityNameCode = pos.Value<string>("security");
                        position.ValueCurrent = GetDecimal(pos["value_current"]);
                        position.ValueBlocked = GetDecimal(pos["value_blocked"]);
                        portfolio.SetNewPosition(position);
                    }
                }

                PortfolioEvent?.Invoke(_portfolios.Values.ToList());
            }
        }

        private Security ParseSecurity(JObject data)
        {
            if (data == null)
            {
                return null;
            }

            Security sec = new Security();
            sec.Name = data.Value<string>("name");
            sec.NameFull = data.Value<string>("name_full");
            sec.NameClass = data.Value<string>("class");
            sec.NameId = data.Value<string>("id");
            sec.SecurityType = SecurityType.Futures;
            sec.State = SecurityStateType.Activ;
            sec.Exchange = ServerType.ToString();
            sec.PriceStep = GetDecimal(data["price_step"]);
            sec.PriceStepCost = GetDecimal(data["price_step_cost"]);
            if (sec.PriceStepCost == 0 && sec.PriceStep != 0)
            {
                sec.PriceStepCost = sec.PriceStep;
            }
            sec.Lot = GetDecimal(data["lot"]);
            if (sec.Lot == 0)
            {
                sec.Lot = 1;
            }

            return sec;
        }

        private Order ParseOrder(JObject data)
        {
            if (data == null)
            {
                return null;
            }

            Order order = new Order();
            order.ServerType = ServerType.Esunny;
            order.SecurityNameCode = data.Value<string>("security");
            order.PortfolioNumber = data.Value<string>("portfolio");
            order.NumberMarket = data.Value<string>("number_market");
            order.NumberUser = data.Value<int?>("number_user") ?? 0;
            order.Price = GetDecimal(data["price"]);
            order.Volume = GetDecimal(data["volume"]);
            order.VolumeExecute = GetDecimal(data["volume_execute"]);
            order.Side = ParseSide(data.Value<string>("side"));
            order.State = ParseOrderState(data.Value<string>("state"));

            if (TryParseDateTime(data.Value<string>("time"), out DateTime time))
            {
                order.TimeCallBack = time;
            }

            string orderType = data.Value<string>("order_type");
            order.TypeOrder = string.Equals(orderType, "Market", StringComparison.OrdinalIgnoreCase)
                ? OrderPriceType.Market
                : OrderPriceType.Limit;

            return order;
        }

        private MyTrade ParseMyTrade(JObject data)
        {
            if (data == null)
            {
                return null;
            }

            MyTrade trade = new MyTrade();
            trade.SecurityNameCode = data.Value<string>("security");
            trade.NumberOrderParent = data.Value<string>("order_id");
            trade.NumberTrade = data.Value<string>("trade_id");
            trade.Price = GetDecimal(data["price"]);
            trade.Volume = GetDecimal(data["volume"]);
            trade.Side = ParseSide(data.Value<string>("side"));

            if (TryParseDateTime(data.Value<string>("time"), out DateTime time))
            {
                trade.Time = time;
            }

            return trade;
        }

        private Trade ParseTrade(JObject data)
        {
            if (data == null)
            {
                return null;
            }

            Trade trade = new Trade();
            trade.SecurityNameCode = data.Value<string>("security");
            trade.Id = data.Value<string>("trade_id");
            trade.Price = GetDecimal(data["price"]);
            trade.Volume = GetDecimal(data["volume"]);
            trade.Side = ParseSide(data.Value<string>("side"));

            if (TryParseDateTime(data.Value<string>("time"), out DateTime time))
            {
                trade.Time = time;
            }

            return trade;
        }

        private MarketDepth ParseDepth(JObject data)
        {
            if (data == null)
            {
                return null;
            }

            MarketDepth depth = new MarketDepth();
            depth.SecurityNameCode = data.Value<string>("security");

            if (TryParseDateTime(data.Value<string>("time"), out DateTime time))
            {
                depth.Time = time;
            }
            else
            {
                depth.Time = DateTime.Now;
            }

            depth.Bids = new List<MarketDepthLevel>();
            depth.Asks = new List<MarketDepthLevel>();

            JArray bids = data.Value<JArray>("bids");
            if (bids != null)
            {
                for (int i = 0; i < bids.Count; i++)
                {
                    JObject level = bids[i] as JObject;
                    if (level == null)
                    {
                        continue;
                    }

                    depth.Bids.Add(new MarketDepthLevel
                    {
                        Price = GetDecimal(level["price"]),
                        Bid = GetDecimal(level["volume"])
                    });
                }
            }

            JArray asks = data.Value<JArray>("asks");
            if (asks != null)
            {
                for (int i = 0; i < asks.Count; i++)
                {
                    JObject level = asks[i] as JObject;
                    if (level == null)
                    {
                        continue;
                    }

                    depth.Asks.Add(new MarketDepthLevel
                    {
                        Price = GetDecimal(level["price"]),
                        Ask = GetDecimal(level["volume"])
                    });
                }
            }

            depth.Bids = depth.Bids.OrderByDescending(l => l.Price).ToList();
            depth.Asks = depth.Asks.OrderBy(l => l.Price).ToList();

            return depth;
        }

        private void SendCommand(object command)
        {
            try
            {
                if (_writer == null)
                {
                    return;
                }

                string payload = JsonConvert.SerializeObject(command);
                lock (_sendLocker)
                {
                    _writer.WriteLine(payload);
                }
            }
            catch (Exception ex)
            {
                SendLogMessage($"Esunny send command error: {ex.Message}", LogMessageType.Error);
            }
        }

        private void TryStartRouter(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }

                if (_routerProcess != null && !_routerProcess.HasExited)
                {
                    return;
                }

                string fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath))
                {
                    SendLogMessage($"Esunny router file not found: {fullPath}", LogMessageType.Error);
                    return;
                }

                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = fullPath,
                    Arguments = $"--port {_routerPort}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(fullPath)
                };

                _routerProcess = Process.Start(info);
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                SendLogMessage($"Esunny router start error: {ex.Message}", LogMessageType.Error);
            }
        }

        private void SetDisconnected()
        {
            if (ServerStatus == ServerConnectStatus.Disconnect)
            {
                return;
            }

            ServerStatus = ServerConnectStatus.Disconnect;
            DisconnectEvent?.Invoke();
        }

        private Side ParseSide(string value)
        {
            if (string.Equals(value, "Sell", StringComparison.OrdinalIgnoreCase))
            {
                return Side.Sell;
            }

            if (string.Equals(value, "S", StringComparison.OrdinalIgnoreCase))
            {
                return Side.Sell;
            }

            return Side.Buy;
        }

        private OrderStateType ParseOrderState(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return OrderStateType.None;
            }

            switch (value)
            {
                case "Accept":
                case "Queue":
                case "1":
                case "2":
                    return OrderStateType.Active;
                case "PartFill":
                case "6":
                    return OrderStateType.Partial;
                case "Fill":
                case "7":
                    return OrderStateType.Done;
                case "Delete":
                case "LeftDelete":
                case "B":
                case "C":
                    return OrderStateType.Cancel;
                case "Fail":
                case "8":
                    return OrderStateType.Fail;
                default:
                    return OrderStateType.None;
            }
        }

        private bool TryParseDateTime(string value, out DateTime result)
        {
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out result))
            {
                return true;
            }

            return DateTime.TryParse(value, out result);
        }

        private decimal GetDecimal(JToken token)
        {
            if (token == null)
            {
                return 0;
            }

            if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
            {
                return token.Value<decimal>();
            }

            if (decimal.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
            {
                return value;
            }

            if (decimal.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out value))
            {
                return value;
            }

            return 0;
        }

        private void SendLogMessage(string message, LogMessageType type)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            LogMessageEvent?.Invoke(message, type);
        }
    }
}
