/*
 *Your rights to use the code are governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using Newtonsoft.Json;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market.Servers.Entity;
using OsEngine.Market.Servers.Esunny.Entity;
using System;
using System.Collections.Concurrent;
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
            EsunnyServerRealization realization = new EsunnyServerRealization();
            ServerRealization = realization;                       

            CreateParameterString("Account ID", "");
            CreateParameterPassword("Password", "");
            CreateParameterString("AuthCode", "");
            CreateParameterString("APPID", "");
            CreateParameterString("Data server url", "");
            CreateParameterString("Trade server url", "");
            CreateParameterBoolean("Enable full log Data server", false);
            CreateParameterBoolean("Enable full log Trade server", false);        }
    }

    public class EsunnyServerRealization : IServerRealization
    {
        #region 1 Constructor, Status, Connection

        public EsunnyServerRealization()
        {
            ServerStatus = ServerConnectStatus.Disconnect;

            Thread worker = new Thread(ThreadPortfolio);
            worker.Start();

            Thread worker2 = new Thread(WorkerPlaceMarketData);
            worker2.IsBackground = true;
            worker2.Start();

            Thread worker3 = new Thread(WorkerPlaceTradeRouter);
            worker3.IsBackground = true;
            worker3.Start();

            Thread worker4 = new Thread(CheckSocketThreadsStatus);
            worker4.Start();
        }

        public DateTime ServerTime { get; set; }

        public void Connect(WebProxy proxy = null)
        {
            _accountId = ((ServerParameterString)ServerParameters[0]).Value;
            _userPassword = ((ServerParameterPassword)ServerParameters[1]).Value;            
            _authCode = ((ServerParameterString)ServerParameters[2]).Value;
            _appId = ((ServerParameterString)ServerParameters[3]).Value;
            _dataServerUrl = ((ServerParameterString)ServerParameters[4]).Value;
            _tradeServerUrl = ((ServerParameterString)ServerParameters[5]).Value;
            _fullLogMarketData = ((ServerParameterBool)ServerParameters[6]).Value;
            _fullLogTradeData = ((ServerParameterBool)ServerParameters[7]).Value;

            if (string.IsNullOrEmpty(_accountId))
            {
                SendLogMessage("No BrokerId!!! No connection!!!", LogMessageType.Error);
                return;
            }

            if (string.IsNullOrEmpty(_userPassword))
            {
                SendLogMessage("No UserPassword!!! No connection!!!", LogMessageType.Error);
                return;
            }

            if (string.IsNullOrEmpty(_authCode))
            {
                SendLogMessage("No AuthCode!!! No connection!!!", LogMessageType.Error);
                return;
            }

            if (string.IsNullOrEmpty(_appId))
            {
                SendLogMessage("No AppId!!! No connection!!!", LogMessageType.Error);
                return;
            }

            if (DataRouterIsActivate == true &&
                string.IsNullOrEmpty(_dataServerUrl))
            {
                SendLogMessage("No Data server url!!! No connection!!!", LogMessageType.Error);
                return;
            }

            if (TradeRouterIsActivate == true &&
                string.IsNullOrEmpty(_tradeServerUrl))
            {
                SendLogMessage("No Trade server url!!! No connection!!!", LogMessageType.Error);
                return;
            }

            _tradeSocketConnect = false;
            _marketSocketConnect = false;

            _subscribeSecurities = new List<Security>();

            CloseRouters();

            Thread.Sleep(5000);

            LoadRouters();

            Thread.Sleep(5000);

            _messagesToSendMarketData = new ConcurrentQueue<string>();
            _messagesToSendTrade = new ConcurrentQueue<string>();

            string connectionMarketData = "{\"cmd\":\"connect\"";
            connectionMarketData += ",\"dataServerUrl\":\"" + _dataServerUrl.Split(':')[0] + "\"";
            connectionMarketData += ",\"dataServerPort\":\"" + _dataServerUrl.Split(':')[1] + "\"}";

            string connectionTrade = "{\"cmd\":\"connect\"";
            connectionTrade += ",\"accountId\":\"" + _accountId + "\"";
            connectionTrade += ",\"password\":\"" + _userPassword + "\"";
            connectionTrade += ",\"appId\":\"" + _appId + "\"";
            connectionTrade += ",\"authCode\":\"" + _authCode + "\"";
            connectionTrade += ",\"tradeServerUrl\":\"" + _tradeServerUrl.Split(':')[0] + "\"";
            connectionTrade += ",\"tradeServerPort\":\"" + _tradeServerUrl.Split(':')[1] + "\"}";

            _messagesToSendMarketData.Enqueue(connectionMarketData);
            _messagesToSendTrade.Enqueue(connectionTrade);

            if (DataRouterIsActivate == true)
            {// Сокет для данных
                if (_socketMarketData == null)
                {
                    IPHostEntry ipHost = Dns.GetHostEntry("localhost");

                    IPAddress ipAddr = null;

                    for (int i = 0; i < ipHost.AddressList.Length; i++)
                    {
                        IPAddress ipAddrCurrent = ipHost.AddressList[i];

                        string adr = ipAddrCurrent.ToString();

                        if (adr == "127.0.0.1")
                        {
                            ipAddr = ipHost.AddressList[i];
                            break;
                        }
                    }

                    if (ipAddr == null)
                    {
                        SendLogMessage("No localhost address", LogMessageType.Error);
                        return;
                    }

                    IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 5555);

                    _socketMarketData = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    try
                    {
                        _socketMarketData.Connect(ipEndPoint);
                    }
                    catch (Exception ex)
                    {
                        SendLogMessage("Esunny market server is not responding" + ex.ToString(),

                            LogMessageType.Error);
                        return;
                    }
                }
            }                      

            if (TradeRouterIsActivate == true)
            {// Сокет для торговли
                if (_socketToTrade == null)
                {
                    IPHostEntry ipHost = Dns.GetHostEntry("localhost");

                    IPAddress ipAddr = null;

                    for (int i = 0; i < ipHost.AddressList.Length; i++)
                    {
                        IPAddress ipAddrCurrent = ipHost.AddressList[i];

                        string adr = ipAddrCurrent.ToString();

                        if (adr == "127.0.0.1")
                        {
                            ipAddr = ipHost.AddressList[i];
                            break;
                        }
                    }

                    IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 5556);

                    _socketToTrade = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    try
                    {
                        _socketToTrade.Connect(ipEndPoint);
                    }
                    catch (Exception ex)
                    {
                        SendLogMessage("Esunny trade server is not responding" + ex.ToString(),

                            LogMessageType.Error);
                        return;
                    }
                }
            }

            Thread.Sleep(5000);

            _canSendMessagesMarketData = true;
            _canSendMessagesTradeRouter = true;
        }

        public void Dispose()
        {
            _canSendMessagesMarketData = false;
            _canSendMessagesTradeRouter = false;

            try
            {
                CloseRouters();
            }
            catch (Exception exeption)
            {
                HandlerException(exeption);
            }

            try
            {
                if (_socketMarketData != null)
                {
                    try
                    {
                        //SendMessage("{\"cmd\":\"disconnect\"" + "}", _socketMarketData, "MarketServer");
                        _socketMarketData.Shutdown(SocketShutdown.Send);
                    }
                    catch
                    {
                        // ignore
                    }

                    _socketMarketData.Close();
                    _socketMarketData.Dispose();
                    _socketMarketData = null;
                }
            }
            catch (Exception exeption)
            {
                HandlerException(exeption);
            }
            
            try
            {
                if (_socketToTrade != null)
                {
                    try
                    {
                        //SendMessage("{\"cmd\":\"disconnect\"" + "}", _socketToTrade, "TradeServer");
                        _socketToTrade.Shutdown(SocketShutdown.Send);
                    }
                    catch
                    {
                        // ignore
                    }

                    _socketToTrade.Close();
                    _socketToTrade.Dispose();
                    _socketToTrade = null;
                }
            }
            catch (Exception exeption)
            {
                HandlerException(exeption);
            }

            if (ServerStatus != ServerConnectStatus.Disconnect)
            {
                ServerStatus = ServerConnectStatus.Disconnect;
                DisconnectEvent();
            }
        }

        public ServerType ServerType
        {
            get { return ServerType.Esunny; }
        }

        public ServerConnectStatus ServerStatus { get; set; }

        public event Action ConnectEvent;

        public event Action DisconnectEvent;

        public event Action ForceCheckOrdersAfterReconnectEvent { add { } remove { } }

        public bool TradeRouterIsActivate
        {
            get
            {
                return true;
            }
        }

        public bool DataRouterIsActivate
        {
            get
            {
                return true;
            }
        }

        public void CloseRouters()
        {
            Process[] ps1 = System.Diagnostics.Process.GetProcesses();

            List<Process> process = new List<Process>();

            for (int i = 0; i < ps1.Length; i++)
            {
                Process p = ps1[i];

                try
                {
                    if (p.MainModule.FileName != ""
                        && p.Modules != null)
                    {
                        process.Add(p);
                    }
                }
                catch
                {

                }
            }

            for (int i = 0; i < process.Count; i++)
            {
                Process p = process[i];

                for (int j = 0; p.Modules != null && j < p.Modules.Count; j++)
                {
                    if (p.Modules[j].FileName == null)
                    {
                        continue;
                    }

                    if (p.Modules[j].FileName.EndsWith("EsunnyMarketData.exe"))
                    {
                        p.Kill();
                        p.Dispose();
                        break;
                    }
                    else if (p.Modules[j].FileName.EndsWith("cmd.exe"))
                    {
                        p.Kill();
                        p.Dispose();
                        break;
                    }
                    else if (p.Modules[j].FileName.EndsWith("EsunnyTradeData.exe"))
                    {
                        p.Kill();
                        p.Dispose();
                        break;
                    }

                }
            }
        }

        public void LoadRouters()
        {
            string curDir = Environment.CurrentDirectory;

            string dirMarketData = curDir + "\\Esunny_Router\\MarketData\\x64\\Release\\EsunnyMarketData.exe";
            string dirTrader = curDir + "\\Esunny_Router\\TradeData\\x64\\Release\\EsunnyTradeData.exe";

            try
            {
                if (TradeRouterIsActivate)
                {
                    Process.Start(dirTrader);
                }

                if (DataRouterIsActivate)
                {
                    Process.Start(dirMarketData);
                }

                Thread.Sleep(3000);
            }
            catch (Exception e)
            {
                SendLogMessage(e.ToString(), LogMessageType.Error);
            }
        }
        
        #endregion

        #region 2 Properties

        public List<IServerParameter> ServerParameters { get; set; }

        public bool IsCompletelyDeleted { get; set; }

        private string _accountId;

        private string _userPassword;

        private string _appId;

        private string _authCode;

        private string _dataServerUrl;

        private string _tradeServerUrl;

        private bool _fullLogMarketData;

        private bool _fullLogTradeData;

        #endregion

        #region 3 Securities

        public void GetSecurities()
        {
            string str = "getSecurities";

            _messagesToSendTrade.Enqueue(str);

            while (true)
            {
                if (_securities != null && _securities.Count > 0)
                {
                    break;
                }

                Thread.Sleep(1000);
            }

            SecurityEvent(_securities);
        }

        private void GetSecurityList(string message)
        {
            try
            {
                ResponceMessageSecurity responce = JsonConvert.DeserializeAnonymousType(message, new ResponceMessageSecurity());

                List<Security> loadSecurities = new();

                for (int i = 0; i < responce.list.Count; i++)
                {
                    Security sec = new();

                    sec.Name = responce.list[i].contractNo;
                    sec.NameId = responce.list[i].contractIndex;                    
                    sec.Exchange = GetNameExchange(responce.list[i].exchangeId);
                    sec.NameClass = GetNameClass(responce.list[i].commodityType);
                    sec.Lot = 1;
                    sec.VolumeStep = responce.list[i].contractSize.ToDecimal();
                    sec.MinTradeAmount = responce.list[i].contractSize.ToDecimal();
                    sec.MinTradeAmountType = MinTradeAmountType.Contract;
                    sec.DecimalsVolume = GetDecimals(responce.list[i].contractSize);
                    sec.PriceStep = responce.list[i].contractTickSize.ToDecimal();
                    sec.PriceStepCost = responce.list[i].contractTickSize.ToDecimal();
                    sec.Decimals = GetDecimals(responce.list[i].contractTickSize);
                    sec.NameFull = GetFullNameSecurity(responce.list[i], sec);
                    
                    loadSecurities.Add(sec);
                }

                _securities = loadSecurities;
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private string GetFullNameSecurity(Data data, Security sec)
        {
            string prefix = new string(data.contractNo.TakeWhile(char.IsLetter).ToArray());
            string suffix = new string(data.contractNo.SkipWhile(char.IsLetter).ToArray());

            return sec.Exchange + "|" + data.commodityType + "|" + prefix + "|" + suffix;
        }

        private string GetNameExchange(string exchangeId)
        {
            switch (exchangeId)
            {
                case "Z":
                    return "ZCE";
                case "S":
                    return "SHFE";
                case "I":
                    return "INE";
                case "C":
                    return "CFFEX";
                case "D":
                    return "DCE";
                case "F":
                    return "GFEX";
                case "G":
                    return "SGE";
                default:
                    return "";
            }          
        }

        private string GetNameClass(string type)
        {
            switch (type)
            {
                case "F":
                    return "Futures";
                case "O":
                    return "Options";
                case "M":
                    return "Inter-commodity spread";
                case "S":
                    return "Calendar spread";
                case "D":
                    return "Straddle";
                case "G":
                    return "Strip spread";
                case "R":
                    return "Covered option";
                default:
                    return "None";
            }
        }

        private int GetDecimals(string str)
        {
            string[] s = str.Split('.');

            if (s.Length > 1)
            {
                return s[1].Length;
            }
            else
            {
                return 0;
            }
        }

        public List<Security> _securities = new List<Security>();

        public event Action<List<Security>> SecurityEvent;

        #endregion

        #region 4 Portfolios

        private Portfolio _portfolio = new Portfolio();

        private bool _portfoliosLoaded = false;
        private bool _positionsLoaded = false;

        public void GetPortfolios()
        {
        }

        private void ThreadPortfolio()
        {
            while (true)
            {
                try
                {
                    if (ServerStatus == ServerConnectStatus.Disconnect)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    _messagesToSendTrade.Enqueue("getPortfolio");
                    _messagesToSendTrade.Enqueue("getPositions");

                    Thread.Sleep(10000);
                }
                catch (Exception ex)
                {
                    SendLogMessage(ex.ToString(), LogMessageType.Error);
                }
            }
        }

        private void GetPortfolioData(string message)
        {
            try
            {
                ResponceMessageAccount responce = JsonConvert.DeserializeAnonymousType(message, new ResponceMessageAccount());

                Portfolio portfolio = new Portfolio();

                portfolio.Number = responce.accountNo;

                if (!_portfoliosLoaded)
                {
                    portfolio.ValueBegin = responce.equity.ToDecimal();
                    _portfoliosLoaded = true;
                }
                else
                {
                    if(_portfolio != null && _portfolio.ValueBegin != 0)
                    {
                        portfolio.ValueBegin = _portfolio.ValueBegin;
                    }
                }
                
                portfolio.ValueCurrent = responce.equity.ToDecimal();
                portfolio.ValueBlocked = responce.margin.ToDecimal();
                portfolio.UnrealizedPnl = responce.positionProfit.ToDecimal();

                _portfolio = portfolio;

                PortfolioEvent(new List<Portfolio> { portfolio });
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private void GetPositionsData(string message)
        {
            try
            {
                ResponceMessagePositions responce = JsonConvert.DeserializeAnonymousType(message, new ResponceMessagePositions());

                Portfolio portfolio = new Portfolio();

                portfolio.Number = _portfolio.Number;
                portfolio.ValueBegin = _portfolio.ValueBegin;
                portfolio.ValueCurrent = _portfolio.ValueCurrent;
                portfolio.ValueBlocked = _portfolio.ValueBlocked;
                portfolio.UnrealizedPnl = _portfolio.UnrealizedPnl;

                for (int i = 0; i < responce.list.Count; i++)
                {
                    ListPositions item = responce.list[i];

                    PositionOnBoard posLong = new PositionOnBoard();

                    posLong.PortfolioName = item.accountNo;
                    posLong.SecurityNameCode = item.contractNo + "_LONG";
                    posLong.ValueBlocked = 0;
                    posLong.ValueCurrent = item.preBuyQty.ToDecimal() + item.todayBuyQty.ToDecimal();

                    if (!_positionsLoaded)
                    {
                        posLong.ValueBegin = item.preBuyQty.ToDecimal() + item.todayBuyQty.ToDecimal();
                    }

                    portfolio.SetNewPosition(posLong);

                    PositionOnBoard posShort = new PositionOnBoard();

                    posShort.PortfolioName = item.accountNo;
                    posShort.SecurityNameCode = item.contractNo + "_SHORT";
                    posShort.ValueBlocked = 0;
                    posShort.ValueCurrent = item.preSellQty.ToDecimal() + item.todaySellQty.ToDecimal();

                    if (!_positionsLoaded)
                    {
                        posShort.ValueBegin = item.preSellQty.ToDecimal() + item.todaySellQty.ToDecimal();
                    }

                    portfolio.SetNewPosition(posShort);
                }

                _positionsLoaded = true;

                PortfolioEvent(new List<Portfolio> { portfolio });
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        public event Action<List<Portfolio>> PortfolioEvent;

        #endregion

        #region 5 Data

        public List<Trade> GetTickDataToSecurity(Security security, DateTime startTime, DateTime endTime, DateTime actualTime)
        {
            return null;
        }

        public List<Candle> GetCandleDataToSecurity(Security security, TimeFrameBuilder timeFrameBuilder, DateTime startTime, DateTime endTime, DateTime actualTime)
        {
            return null;
        }

        public List<Candle> GetCandleHistory(string nameSec, TimeSpan tf, bool IsOsData, int CountToLoad, DateTime timeEnd)
        {
            return null;
        }

        public List<Candle> GetLastCandleHistory(Security security, TimeFrameBuilder timeFrameBuilder, int candleCount)
        {
            return null;
        }

        #endregion

        #region 6 Tcp router

        // data socket

        private bool _canSendMessagesMarketData;

        private Socket _socketMarketData;

        private ConcurrentQueue<string> _messagesToSendMarketData = new ConcurrentQueue<string>();

        private void WorkerPlaceMarketData()
        {
            while (true)
            {
                Thread.Sleep(1);
                try
                {

                    if (_socketMarketData == null)
                    {
                        _lastTimeSendMessageInSocketData = DateTime.Now;
                        continue;
                    }

                    if (_canSendMessagesMarketData == false)
                    {
                        _lastTimeSendMessageInSocketData = DateTime.Now;
                        continue;
                    }

                    if (_messagesToSendMarketData.IsEmpty)
                    { // request any incoming data for us that are saving in server / запрос каких-либо входящих данных для нас, которые копятся в сервере
                        if (IncomeMessageFromDataRouter(SendMessage("Process", _socketMarketData, "MarketServer")))
                        {
                            Thread.Sleep(10);
                        }

                        _lastTimeSendMessageInSocketData = DateTime.Now;
                        continue;
                    }

                    string message = null;
                    _messagesToSendMarketData.TryDequeue(out message);

                    if (message == null)
                    {
                        _lastTimeSendMessageInSocketData = DateTime.Now;
                        continue;
                    }

                    _lastTimeSendMessageInSocketData = DateTime.Now;
                                        
                    IncomeMessageFromDataRouter(SendMessage(message, _socketMarketData, "MarketServer"));
                }
                catch (Exception error)
                {
                    _canSendMessagesMarketData = false;

                    if (ServerStatus != ServerConnectStatus.Disconnect)
                    {
                        ServerStatus = ServerConnectStatus.Disconnect;
                        if (DisconnectEvent != null)
                        {
                            DisconnectEvent();
                        }
                    }

                    Thread.Sleep(10000);
                    SendLogMessage(error.ToString(), LogMessageType.Error);
                }
            }
        }

        // trade socket

        private bool _canSendMessagesTradeRouter;

        private Socket _socketToTrade;

        private ConcurrentQueue<string> _messagesToSendTrade = new ConcurrentQueue<string>();

        private DateTime _lastTimeSendPing;

        private void WorkerPlaceTradeRouter()
        {
            while (true)
            {
                Thread.Sleep(100);
                try
                {

                    if (_socketToTrade == null)
                    {
                        _lastTimeSendMessageInSocketTrade = DateTime.Now;
                        continue;
                    }

                    if (_canSendMessagesTradeRouter == false)
                    {
                        _lastTimeSendMessageInSocketTrade = DateTime.Now;
                        continue;
                    }

                    if (_messagesToSendTrade.IsEmpty)
                    {
                        // request any incoming data for us that are saving in server
                        // запрос каких-либо входящих данных для нас, которые копятся в сервере

                        if (_lastTimeSendPing.AddMilliseconds(200) < DateTime.Now)
                        {
                            _lastTimeSendPing = DateTime.Now;
                            _lastTimeSendMessageInSocketTrade = DateTime.Now;
                            IncomeMessageFromTradeRouter(SendMessage("Process", _socketToTrade, "TradeServer"));
                            continue;
                        }
                    }

                    string message = null;
                    _messagesToSendTrade.TryDequeue(out message);

                    if (message == null)
                    {
                        _lastTimeSendMessageInSocketTrade = DateTime.Now;
                        continue;
                    }

                    _lastTimeSendMessageInSocketTrade = DateTime.Now;

                    IncomeMessageFromTradeRouter(SendMessage(message, _socketToTrade, "TradeServer"));
                }
                catch (Exception error)
                {
                    _canSendMessagesTradeRouter = false;

                    if (ServerStatus != ServerConnectStatus.Disconnect)
                    {
                        ServerStatus = ServerConnectStatus.Disconnect;

                        if (DisconnectEvent != null)
                        {
                            DisconnectEvent();
                        }
                    }

                    Thread.Sleep(10000);
                    SendLogMessage(error.ToString(), LogMessageType.Error);
                }
            }
        }

        private string _placeLostDataSocket = "";

        private string _placeLostTradeSocket = "";

        private string _lastMessageToDataServer = "";

        private string _lastMessageToTradeServer = "";

        private void SendFrame(Socket socket, string payload)
        {
            if (socket == null)
            {
                return;
            }

            byte[] body = Encoding.UTF8.GetBytes(payload);
            int len = body.Length;
            byte[] header = new byte[4];
            header[0] = (byte)((len >> 24) & 0xFF);
            header[1] = (byte)((len >> 16) & 0xFF);
            header[2] = (byte)((len >> 8) & 0xFF);
            header[3] = (byte)(len & 0xFF);

            socket.Send(header);

            int sent = 0;
            while (sent < body.Length)
            {
                sent += socket.Send(body, sent, body.Length - sent, SocketFlags.None);
            }
        }

        private string ReceiveFrame(Socket socket)
        {
            byte[] header = ReceiveExact(socket, 4);
            int len = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];

            if (len < 0 || len > 10 * 1024 * 1024)
            {
                throw new Exception("Invalid frame length from market server: " + len);
            }

            byte[] body = ReceiveExact(socket, len);
            return Encoding.UTF8.GetString(body);
        }

        private byte[] ReceiveExact(Socket socket, int length)
        {
            byte[] buffer = new byte[length];
            int offset = 0;

            while (offset < length)
            {
                int read = socket.Receive(buffer, offset, length - offset, SocketFlags.None);
                if (read <= 0)
                {
                    throw new Exception("Socket closed while receiving data");
                }
                offset += read;
            }

            return buffer;
        }

        private string SendMessage(string message, Socket socket, string socketName)
        {           
            if (socketName == "MarketServer")
            {               
                _placeLostDataSocket = "Sending";
                _lastMessageToDataServer = message;
            }
            else
            {
                _placeLostTradeSocket = "Sending";
                _lastMessageToTradeServer = message;
            }

            // send data through socket

            if (socketName == "MarketServer")
            {
                SendFrame(socket, message);
            }
            else
            {
                SendFrame(socket, message);
            }            

            if (message.StartsWith("Process") == false)
            {
                return message;
            }

            if (socketName == "MarketServer")
            {
                _placeLostDataSocket = "Receive";
            }
            else
            {
                _placeLostTradeSocket = "Receive";
            }

            // get response from the server

            string request;
            if (socketName == "MarketServer")
            {
                request = ReceiveFrame(socket);
            }
            else
            {
                request = ReceiveFrame(socket);
            }

            for (int i = 0; i < request.Length; i++)
            {
                if (request[i] == '%')
                {
                    request = request.Substring(0, i);
                    break;
                }
            }

            return request;
        }

        // common connect

        private bool _tradeSocketConnect = false;

        private bool _marketSocketConnect = false;

        private void CheckConnectStatus()
        {
            if (TradeRouterIsActivate == true &&
                _tradeSocketConnect == false)
            {
                return;
            }
            if (DataRouterIsActivate == true
                && _marketSocketConnect == false)
            {
                return;
            }

            ServerStatus = ServerConnectStatus.Connect;

            if (ConnectEvent != null)
            {
                ConnectEvent();
            }

        }

        private DateTime _lastTimeSendMessageInSocketTrade;

        private DateTime _lastTimeSendMessageInSocketData;

        private void CheckSocketThreadsStatus()
        {
            while (true)
            {
                Thread.Sleep(5000);

                if (_socketToTrade != null &&
                    _lastTimeSendMessageInSocketTrade.AddSeconds(10) < DateTime.Now
                    && _lastTimeSendMessageInSocketTrade.AddSeconds(30) > DateTime.Now)
                {
                    SendLogMessage("Sockets thread is lost. Trade router. Reconnect", LogMessageType.Error);
                    SendLogMessage("Place lost trade socket thread: " + _placeLostTradeSocket, LogMessageType.Error);
                    SendLogMessage("Last message to trade server: " + _lastMessageToTradeServer, LogMessageType.Error);
                    CloseRouters();
                    ServerStatus = ServerConnectStatus.Disconnect;
                    DisconnectEvent();
                }

                if (_socketMarketData != null &&
                    _lastTimeSendMessageInSocketData.AddSeconds(10) < DateTime.Now
                     && _lastTimeSendMessageInSocketData.AddSeconds(30) > DateTime.Now)
                {
                    SendLogMessage("Sockets thread is lost. Data router. Reconnect", LogMessageType.Error);
                    SendLogMessage("Place lost data socket thread: " + _placeLostDataSocket, LogMessageType.Error);
                    SendLogMessage("Last message to data server: " + _lastMessageToDataServer, LogMessageType.Error);

                    CloseRouters();
                    ServerStatus = ServerConnectStatus.Disconnect;
                    DisconnectEvent();
                }
            }
        }

        #endregion             

        #region 8 WebSocket security subscribe

        private RateGate rateGateSubscribe = new RateGate(1, TimeSpan.FromMilliseconds(300));

        private List<Security> _subscribeSecurities = new List<Security>();

        public void Subscribe(Security security)
        {
            try
            {
                rateGateSubscribe.WaitToProceed();

                for (int i = 0; i < _subscribeSecurities.Count; i++)
                {
                    if (_subscribeSecurities[i].Name == security.Name)
                    {
                        return;
                    }
                }

                _messagesToSendMarketData.Enqueue("{\"cmd\":\"subscribeQuote\",\"symbol\":\"" + security.NameFull + "\"}");
                
                _subscribeSecurities.Add(security);
            }
            catch (Exception exeption)
            {
                HandlerException(exeption);
            }
        }

        public bool SubscribeNews()
        {
            return false;
        }

        public event Action<News> NewsEvent { add { } remove { } }

        #endregion

        #region 9 WebSocket parsing the messages

        private void IncomeMessageFromTradeRouter(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (message.StartsWith("Process"))
            {
                return;
            }

            if (message.StartsWith("{\"type\":\"connect\"") &&
                ServerStatus == ServerConnectStatus.Disconnect)
            {
                SendLogMessage("Trade router is connected", LogMessageType.System);
                _tradeSocketConnect = true;
                CheckConnectStatus();
            }
            else if (message.StartsWith("{\"type\":\"disconnect\""))
            {
                SendLogMessage("Trade router is disconnected", LogMessageType.System);
                ServerStatus = ServerConnectStatus.Disconnect;
                DisconnectEvent?.Invoke();
            }
            else if (message.Contains("{\"type\":\"account\""))
            {
                GetPortfolioData(message);
            }
            else if (message.Contains("{\"type\":\"positions\""))
            {
                GetPositionsData(message);
            }
            else if (message.Contains("\"type\":\"security\""))
            {
                GetSecurityList(message);
            }
            else if (message.Contains("\"type\":\"rtnOrder\""))
            {
                SendLogMessage("Trade router message: " + message, LogMessageType.System);
                GetMyOrder(message);                
            }
            else if (message.Contains("\"type\":\"rtnMatch\""))
            {
                SendLogMessage("Trade router message: " + message, LogMessageType.System);
                GetMyTrade(message);
            }

            if (_fullLogTradeData)
            {
                SendLogMessage("Trade router message: " + message, LogMessageType.System);
            }
        }

        private void GetMyOrder(string message)
        {
            try
            {
                ResponceMessageMyOrder responce = JsonConvert.DeserializeAnonymousType(message, new ResponceMessageMyOrder());

                Order order = new();

                order.SecurityNameCode = responce.contractNo1;                
                order.NumberUser = int.Parse(responce.reference);
                order.NumberMarket = responce.orderId;
                order.TypeOrder = GetOrderPriceType(responce.orderType);
                order.State = GetOrderState(responce.orderState);
                order.Price = responce.orderPrice.ToDecimal();
                order.Volume = responce.orderQty.ToDecimal();
                order.Side = GetDirectionOrder(responce.direct);
                order.PositionConditionType = GetPositionConditionType(responce.offset);
                order.PortfolioNumber = responce.accountNo;
                order.VolumeExecute = responce.matchQty.ToDecimal();
                order.ServerType = ServerType.Esunny;
                order.SecurityClassCode = GetClassSecurity(responce.contractNo1);

                if (responce.orderState == "1")
                {
                    order.TimeCreate = ParseDateTimePending(responce.updateTime);
                }
                else
                {
                    order.TimeCreate = ParseDateTimeTrade(responce.updateTime);
                }

                order.TimeCallBack = order.TimeCreate;

                MyOrderEvent?.Invoke(order);
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private DateTime ParseDateTimePending(string updateTime)
        {
            string time = updateTime.Split(' ')[0];
            string dateNow = DateTime.Now.ToString("yyyy-MM-dd");

            return ParseDateTimeTrade(dateNow + " " + time);
        }

        private DateTime ParseDateTimeTrade(string dateTime)
        {
            return DateTime.ParseExact(dateTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private string GetClassSecurity(string name)
        {            
            for(int i = 0; i < _securities.Count; i++)
            {
                if (name == _securities[i].Name)
                {
                    return _securities[i].NameClass;
                }
            }

            return "";
        }

        private OrderPositionConditionType GetPositionConditionType(string offset)
        {
            // 'O' Open
            // 'C' Close
            // 'T' Close today

            switch (offset)
            {
                case "O": return OrderPositionConditionType.Open;
                case "C": return OrderPositionConditionType.Close;
                default: return OrderPositionConditionType.None;
            }            
        }

        private Side GetDirectionOrder(string direct)
        {
            // 'B' Buy
            // 'S' Sell
            // 'N' All

            switch (direct)
            {
                case "B": return Side.Buy;
                case "S": return Side.Sell;
                default: return Side.None;
            }
        }

        private OrderStateType GetOrderState(string orderState)
        {
            // '1' Accepted
            // '2' Queued
            // '3' Applied (exercise/abandon/spread application succeeded)
            // '4' Suspended
            // '5' Triggered
            // '6' Partially filled
            // '7' Fully filled
            // '8' Command failed
            // 'B' Canceled
            // 'C' Remaining quantity canceled
            // 'D' Deleted
            // 'E' Strategy pending trigger*/

            switch (orderState)
            {
                case "1": return OrderStateType.Pending;
                case "2": return OrderStateType.Active;
                case "6": return OrderStateType.Partial;
                case "7": return OrderStateType.Done;
                case "8": return OrderStateType.Fail;
                case "B": return OrderStateType.Cancel;
                case "C": return OrderStateType.Cancel;
                case "D": return OrderStateType.Cancel;
                default: return OrderStateType.Fail;
            }
        }

        private OrderPriceType GetOrderPriceType(string orderType)
        {
            // '0' None
            // '1' Market order
            // '2' Limit order
            // '3' Exercise
            // '4' Abandon
            // '5' Inquiry
            // '6' Quote
            // '7' Swap
            // '8' EFP

            switch (orderType)
            {
                case "1": return OrderPriceType.Market;
                case "2": return OrderPriceType.Limit;

                default: return OrderPriceType.Market;
            }
        }

        private void GetMyTrade(string message)
        {
            try
            {
                ResponceMessageMyTrade responce = JsonConvert.DeserializeAnonymousType(message, new ResponceMessageMyTrade());

                MyTrade trade = new();

                trade.SecurityNameCode = responce.contractNo;
                trade.NumberOrderParent = responce.orderId;
                trade.Price = responce.matchPrice.ToDecimal();
                trade.Volume = responce.matchQty.ToDecimal();
                trade.Side = GetDirectionOrder(responce.direct);
                trade.Time = ParseDateTimeTrade(responce.updateTime);
                trade.NumberTrade = responce.matchId;
                
                MyTradeEvent?.Invoke(trade);
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private bool IncomeMessageFromDataRouter(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return true;
            }

            if (message.Contains("Process"))
            {
                return true;
            }

            if (message.Contains("{\"type\":\"connect\"") &&
                ServerStatus != ServerConnectStatus.Connect)
            {
                SendLogMessage("Data router is connected", LogMessageType.System);
                _marketSocketConnect = true;
                CheckConnectStatus();
                SendLogMessage(message, LogMessageType.System);
            }
            else if (message.Contains("{\"type\":\"disconnect\""))
            {
                ServerStatus = ServerConnectStatus.Disconnect;

                if (DisconnectEvent != null)
                {
                    DisconnectEvent();
                }

                ResponceMessageMarketDataError responce = JsonConvert.DeserializeAnonymousType(message, new ResponceMessageMarketDataError());

                SendLogMessage("Data router is disconnected", LogMessageType.System);
                SendLogMessage("MarketData. Code: " + responce.code + ", Message: " + responce.message, LogMessageType.Error);

                //string connectionMarketData = "{\"cmd\":\"disconnect\"";
                //_messagesToSendMarketData.Enqueue(connectionMarketData);
            }
            else if (message.Contains("\"type\":\"quote\""))
            {
                ParseQuote(message);
            }            
            else
            {
                SendLogMessage(message, LogMessageType.System);
            }

            if (_fullLogMarketData)
            {
                SendLogMessage("MarketDateRouter: " + message, LogMessageType.System);
            }

            return false;
        }

        private void ParseQuote(string message)
        {
            try
            {
                ResponceMessageQuote responce = JsonConvert.DeserializeAnonymousType(message, new ResponceMessageQuote());

                GetLastPrice(responce);
                GetMarketDepth(responce);
            }
            catch(Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private Trade _trade = new Trade();

        private void GetLastPrice(ResponceMessageQuote responce)
        {
            if (_trade.Price == responce.lastPrice.ToDecimal() &&
                _trade.Volume == responce.lastQty.ToDecimal())
            {
                return;
            }

            _trade.SecurityNameCode = GetSecurityNameForFullName(responce.contractNo);
            _trade.Price = responce.lastPrice.ToDecimal();
            _trade.Time = ParseDateTimeQuote(responce.dateTimeStamp);
            _trade.Volume = responce.lastQty.ToDecimal();
            _trade.Side = Side.Buy;

            NewTradesEvent?.Invoke(_trade);
        }

        private DateTime ParseDateTimeQuote(string dateTime)
        {
            return DateTime.ParseExact(dateTime, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        private void GetMarketDepth(ResponceMessageQuote responce)
        {
            MarketDepth marketDepth = new MarketDepth();

            List<MarketDepthLevel> ascs = new List<MarketDepthLevel>();
            List<MarketDepthLevel> bids = new List<MarketDepthLevel>();

            marketDepth.SecurityNameCode = GetSecurityNameForFullName(responce.contractNo);

            bids.Add(new MarketDepthLevel { Price = responce.bidPrice1.ToDouble(), Bid = responce.bidQty1.ToDouble() });
            bids.Add(new MarketDepthLevel { Price = responce.bidPrice2.ToDouble(), Bid = responce.bidQty2.ToDouble() });
            bids.Add(new MarketDepthLevel { Price = responce.bidPrice3.ToDouble(), Bid = responce.bidQty3.ToDouble() });
            bids.Add(new MarketDepthLevel { Price = responce.bidPrice4.ToDouble(), Bid = responce.bidQty4.ToDouble() });
            bids.Add(new MarketDepthLevel { Price = responce.bidPrice5.ToDouble(), Bid = responce.bidQty5.ToDouble() });
            bids.Add(new MarketDepthLevel { Price = responce.bidPrice6.ToDouble(), Bid = responce.bidQty6.ToDouble() });
            bids.Add(new MarketDepthLevel { Price = responce.bidPrice7.ToDouble(), Bid = responce.bidQty7.ToDouble() });
            bids.Add(new MarketDepthLevel { Price = responce.bidPrice8.ToDouble(), Bid = responce.bidQty8.ToDouble() });
            bids.Add(new MarketDepthLevel { Price = responce.bidPrice9.ToDouble(), Bid = responce.bidQty9.ToDouble() });
            bids.Add(new MarketDepthLevel { Price = responce.bidPrice10.ToDouble(), Bid = responce.bidQty10.ToDouble() });

            ascs.Add(new MarketDepthLevel { Price = responce.askPrice1.ToDouble(), Ask = responce.askQty1.ToDouble() });
            ascs.Add(new MarketDepthLevel { Price = responce.askPrice2.ToDouble(), Ask = responce.askQty2.ToDouble() });
            ascs.Add(new MarketDepthLevel { Price = responce.askPrice3.ToDouble(), Ask = responce.askQty3.ToDouble() });
            ascs.Add(new MarketDepthLevel { Price = responce.askPrice4.ToDouble(), Ask = responce.askQty4.ToDouble() });
            ascs.Add(new MarketDepthLevel { Price = responce.askPrice5.ToDouble(), Ask = responce.askQty5.ToDouble() });
            ascs.Add(new MarketDepthLevel { Price = responce.askPrice6.ToDouble(), Ask = responce.askQty6.ToDouble() });
            ascs.Add(new MarketDepthLevel { Price = responce.askPrice7.ToDouble(), Ask = responce.askQty7.ToDouble() });
            ascs.Add(new MarketDepthLevel { Price = responce.askPrice8.ToDouble(), Ask = responce.askQty8.ToDouble() });
            ascs.Add(new MarketDepthLevel { Price = responce.askPrice9.ToDouble(), Ask = responce.askQty9.ToDouble() });
            ascs.Add(new MarketDepthLevel { Price = responce.askPrice10.ToDouble(), Ask = responce.askQty10.ToDouble() });

            marketDepth.Asks = ascs;
            marketDepth.Bids = bids;
            marketDepth.Time = ParseDateTimeQuote(responce.dateTimeStamp);

            MarketDepthEvent?.Invoke(marketDepth);
        }

        private string GetSecurityNameForFullName(string contractNo)
        {
            for (int i = 0; i < _securities.Count; i++)
            {
                if (_securities[i].NameFull == contractNo)
                {
                    return _securities[i].Name;
                }
            }

            return contractNo;
        }

        public event Action<Order> MyOrderEvent;

        public event Action<MyTrade> MyTradeEvent;

        public event Action<MarketDepth> MarketDepthEvent;

        public event Action<Trade> NewTradesEvent;

        public event Action<OptionMarketDataForConnector> AdditionalMarketDataEvent { add { } remove { } }

        #endregion

        #region 10 Trade

        private RateGate rateGateSendOrder = new RateGate(1, TimeSpan.FromMilliseconds(200));

        private RateGate rateGateCancelOrder = new RateGate(1, TimeSpan.FromMilliseconds(200));

        public void SendOrder(Order order)
        {
            rateGateSendOrder.WaitToProceed();

            string msg = "";
            msg += ",\"symbol\":\"" + order.SecurityNameCode + "\"";
            msg += ",\"symbolIndex\":\"" + GetSymbolIndex(order.SecurityNameCode) + "\"";
            msg += ",\"side\":\"" + order.Side + "\"";
            msg += ",\"price\":\"" + order.Price + "\"";
            msg += ",\"volume\":\"" + order.Volume + "\"";
            msg += ",\"numberUser\":\"" + order.NumberUser + "\"";
            msg += ",\"offset\":\"" + order.PositionConditionType + "\"";
            msg += ",\"orderType\":\"" + order.TypeOrder + "\"";
            msg += ",\"hedge\":\"" + "Speculate" + "\"";

            string orderToTcp = "{\"cmd\":\"placeOrder\"" + msg + "}";
                        
            _messagesToSendTrade.Enqueue(orderToTcp);
        }

        private string GetSymbolIndex(string securityNameCode)
        {
            for (int i = 0; i < _securities.Count; i++)
            {
                if (_securities[i].Name == securityNameCode)
                {
                    return _securities[i].NameId;
                }
            }

            return "0";
        }

        public bool CancelOrder(Order order)
        {
            rateGateCancelOrder.WaitToProceed();

            string orderToTcp = "{\"cmd\":\"cancelOrder\",\"orderId\":\"" + order.NumberMarket + "\"}";

            _messagesToSendTrade.Enqueue(orderToTcp);
            return true;
        }

        public void CancelAllOrders()
        {

        }

        public void CancelAllOrdersToSecurity(Security security)
        {

        }

        public void ChangeOrderPrice(Order order, decimal newPrice)
        {

        }

        public void GetAllActivOrders()
        {

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

        public void SetLeverage(Security security, decimal leverage) { }

        #endregion

        #region 11 Log

        public event Action<string, LogMessageType> LogMessageEvent;

        public event Action<Funding> FundingUpdateEvent { add { } remove { } }

        public event Action<SecurityVolumes> Volume24hUpdateEvent { add { } remove { } }

        private void SendLogMessage(string message, LogMessageType messageType)
        {
            LogMessageEvent(message, messageType);
        }

        private void HandlerException(Exception exception)
        {
            if (exception is AggregateException)
            {
                AggregateException httpError = (AggregateException)exception;

                foreach (var item in httpError.InnerExceptions)

                {
                    if (item is NullReferenceException == false)
                    {
                        SendLogMessage(item.InnerException.Message + $" {exception.StackTrace}", LogMessageType.Error);
                    }

                }
            }
            else
            {
                if (exception is NullReferenceException == false)
                {
                    SendLogMessage(exception.Message + $" {exception.StackTrace}", LogMessageType.Error);
                }
            }
        }

        #endregion
    }
}
