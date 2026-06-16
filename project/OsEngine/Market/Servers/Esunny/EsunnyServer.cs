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
        }
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

        private DateTime _lastConnectTime;

        public void Connect(WebProxy proxy = null)
        {
            AccountId = ((ServerParameterString)ServerParameters[0]).Value;
            UserPassword = ((ServerParameterPassword)ServerParameters[1]).Value;            
            AuthCode = ((ServerParameterString)ServerParameters[2]).Value;
            AppId = ((ServerParameterString)ServerParameters[3]).Value;
            DataServerUrl = ((ServerParameterString)ServerParameters[4]).Value;
            TradeServerUrl = ((ServerParameterString)ServerParameters[5]).Value;

            if (string.IsNullOrEmpty(AccountId))
            {
                SendLogMessage("No BrokerId!!! No connection!!!", LogMessageType.Error);
                return;
            }

            if (string.IsNullOrEmpty(UserPassword))
            {
                SendLogMessage("No UserPassword!!! No connection!!!", LogMessageType.Error);
                return;
            }

            if (string.IsNullOrEmpty(AuthCode))
            {
                SendLogMessage("No AuthCode!!! No connection!!!", LogMessageType.Error);
                return;
            }

            if (string.IsNullOrEmpty(AppId))
            {
                SendLogMessage("No AppId!!! No connection!!!", LogMessageType.Error);
                return;
            }

            if (DataRouterIsActivate == true &&
                string.IsNullOrEmpty(DataServerUrl))
            {
                SendLogMessage("No Data server url!!! No connection!!!", LogMessageType.Error);
                return;
            }

            if (TradeRouterIsActivate == true &&
                string.IsNullOrEmpty(TradeServerUrl))
            {
                SendLogMessage("No Trade server url!!! No connection!!!", LogMessageType.Error);
                return;
            }

            _tradeSocketConnect = false;
            _marketSocketConnect = false;

            _subscribeSecurities = new List<Security>();

            CloseRouters();

            Thread.Sleep(5000);

            _lastConnectTime = DateTime.Now;

            LoadRouters();

            Thread.Sleep(5000);

            _messagesToSendMarketData = new ConcurrentQueue<string>();
            _messagesToSendTrade = new ConcurrentQueue<string>();

            string connectionMarketData = "{\"cmd\":\"connect\"";
            connectionMarketData += ",\"dataServerUrl\":\"" + DataServerUrl.Split(':')[0] + "\"";
            connectionMarketData += ",\"dataServerPort\":\"" + DataServerUrl.Split(':')[1] + "\"}";

            string connectionTrade = "{\"cmd\":\"connect\"";
            connectionTrade += ",\"accountId\":\"" + AccountId + "\"";
            connectionTrade += ",\"password\":\"" + UserPassword + "\"";
            connectionTrade += ",\"appId\":\"" + AppId + "\"";
            connectionTrade += ",\"authCode\":\"" + AuthCode + "\"";
            connectionTrade += ",\"tradeServerUrl\":\"" + TradeServerUrl.Split(':')[0] + "\"";
            connectionTrade += ",\"tradeServerPort\":\"" + TradeServerUrl.Split(':')[1] + "\"}";

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

            //ClearFileSystem();

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
                        SendMessage("{\"cmd\":\"disconnect\"", _socketMarketData, "MarketServer");
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
                /*if (File.Exists("Atp_Router\\Files\\ConnectTrade.txt"))
                {
                    File.Delete("Atp_Router\\Files\\ConnectTrade.txt");
                }

                if (File.Exists("Atp_Router\\Files\\ConnectData.txt"))
                {
                    File.Delete("Atp_Router\\Files\\ConnectData.txt");
                }*/
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
                        SendMessage("{\"cmd\":\"disconnect\"", _socketToTrade, "TradeServer");
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
                /*ServerParameterEnum parameter = ((ServerParameterEnum)ServerParameters[7]);

                if (parameter.Value.Contains("trade"))
                {
                    return true;
                }*/

                return true;
            }
        }

        public bool DataRouterIsActivate
        {
            get
            {
                /*ServerParameterEnum parameter = ((ServerParameterEnum)ServerParameters[7]);

                if (parameter.Value.Contains("data"))
                {
                    return true;
                }*/

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
                    else if (p.Modules[j].FileName.EndsWith("apidemo.exe"))
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

            string dirMarketData = curDir + "\\Esunny_Router\\MarketData\\x64\\Debug\\EsunnyMarketData.exe";
            string dirTrader = curDir + "\\Esunny_Router\\TradeData\\x64\\Debug\\apidemo.exe";

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

        private void ClearFileSystem()
        {
            try
            {
                CheckFolders();

                ClearFolder("Atp_Router\\Files\\");

                string[] folders = Directory.GetDirectories("Atp_Router\\Files\\");

                for (int i = 0; i < folders.Length; i++)
                {
                    ClearFolder(folders[i]);
                }
            }
            catch (Exception e)
            {
                SendLogMessage(e.ToString(), LogMessageType.Error);
            }
        }

        private void CheckFolders()
        {
            string path = "Atp_Router\\Files\\";

            string[] folders = new string[]
            {
                path,
                path + "MyTrades\\",
                path + "MyTrades2\\",
                path + "OrderAction2\\",
                path + "OrderActiv\\",
                path + "OrderFail1\\",
                path + "OrderFail2\\",
                path + "OrderFail3\\",
                path + "OrderFail4\\",
                path + "OrderFail5\\",
            };

            for (int i = 0; i < folders.Length; i++)
            {
                if (!Directory.Exists(folders[i]))
                {
                    Directory.CreateDirectory(folders[i]);
                }
            }
        }

        private void ClearFolder(string folderPath)
        {
            string[] files = Directory.GetFiles(folderPath);

            for (int i = 0; i < files.Length; i++)
            {
                File.Delete(files[i]);
            }
        }

        public bool IsCompletelyDeleted { get; set; }

        #endregion

        #region 2 Properties

        public List<IServerParameter> ServerParameters { get; set; }

        private string AccountId;

        private string UserPassword;

        private string AppId;

        private string AuthCode;

        private string DataServerUrl;

        private string TradeServerUrl;

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

        private void GetPortfolioDate(string message)
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

        private void GetPositionsDate(string message)
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

                    if (item.preBuyQty.ToDecimal() + item.todayBuyQty.ToDecimal() > 0)
                    {
                        PositionOnBoard pos = new PositionOnBoard();

                        pos.PortfolioName = item.accountNo;
                        pos.SecurityNameCode = item.contractNo + "_LONG";
                        pos.ValueBlocked = 0;
                        pos.ValueCurrent = item.preBuyQty.ToDecimal() + item.todayBuyQty.ToDecimal();

                        if (!_positionsLoaded)
                        {
                            pos.ValueBegin = item.preBuyQty.ToDecimal() + item.todayBuyQty.ToDecimal();
                        }

                        portfolio.SetNewPosition(pos);
                    }

                    if (item.preSellQty.ToDecimal() + item.todaySellQty.ToDecimal() > 0)
                    {
                        PositionOnBoard pos = new PositionOnBoard();

                        pos.PortfolioName = item.accountNo;
                        pos.SecurityNameCode = item.contractNo + "_SHORT";
                        pos.ValueBlocked = 0;
                        pos.ValueCurrent = item.preSellQty.ToDecimal() + item.todaySellQty.ToDecimal();

                        if (!_positionsLoaded)
                        {
                            pos.ValueBegin = item.preSellQty.ToDecimal() + item.todaySellQty.ToDecimal();
                        }

                        portfolio.SetNewPosition(pos);
                    }
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

                    /*if (ServerStatus == ServerConnectStatus.Disconnect &&
                        _marketSocketConnect == false)
                    {
                        if (File.Exists("Esunny_Router\\Files\\ConnectData.txt"))
                        {
                            SendLogMessage("data router is connected", LogMessageType.System);
                            _marketSocketConnect = true;
                            CheckConnectStatus();
                        }
                    }*/

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

                    /*if (ServerStatus == ServerConnectStatus.Disconnect &&
                       _tradeSocketConnect == false)
                    {
                        if (File.Exists("Atp_Router\\Files\\ConnectTrade.txt"))
                        {
                            SendLogMessage("trade router is connected", LogMessageType.System);
                            _tradeSocketConnect = true;
                            CheckConnectStatus();

                        }
                    }*/

                    //TryGetTradeDataFromFileSys();

                    if (_messagesToSendTrade.IsEmpty)
                    {
                        // request any incoming data for us that are saving in server
                        // запрос каких-либо входящих данных для нас, которые копятся в сервере

                        if (_lastTimeSendPing.AddSeconds(1) < DateTime.Now)
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
                if (message.StartsWith("Process"))
                {
                    // message += iteratorPDataServer;
                    //iteratorPDataServer++;
                }

                _placeLostDataSocket = "Sending";
                _lastMessageToDataServer = message;
            }
            else
            {
                if (message.StartsWith("Process"))
                {
                    // message += iteratorPTradeServer;
                    // iteratorPTradeServer++;
                }

                _placeLostTradeSocket = "Sending";
                _lastMessageToTradeServer = message;
            }

            // send data through socket

            if (socketName == "MarketServer")
            {
                //string jsonCommand = ConvertLegacyToJsonCommandForMarketServer(message);
                SendFrame(socket, message);
            }
            else
            {
                SendFrame(socket, message);
                /*byte[] msg = Encoding.UTF8.GetBytes(message);
                socket.Send(msg);*/
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
                //request = ConvertJsonResponseToLegacyForMarketServer(jsonResponse);
            }
            else
            {
                request = ReceiveFrame(socket);
                /*byte[] bytes = new byte[1024];
                int bytesRec = socket.Receive(bytes);
                request = Encoding.UTF8.GetString(bytes, 0, bytesRec);*/
            }

            //clear socket / Освобождаем сокет
            //sender.Shutdown(SocketShutdown.Send);
            //sender.Close();

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

        #region 7 Check file system 

        private void TryGetTradeDataFromFileSys()
        {
            TryLoadMyTrade();
            TryLoadMyTrades2();
            TryLoadOrderAction2();
            TryLoadOrderFail1();
            TryLoadOrderFail3();
        }

        private string[] GetSortedFileNames(string[] files)
        {
            List<string> result = new List<string>();

            result.AddRange(files);

            for (int i = 0; i < result.Count; i++)
            {
                for (int i2 = 1; i2 < result.Count; i2++)
                {
                    string previousNum = (result[i2 - 1].Split('\\')[result[i2 - 1].Split('\\').Length - 1]).Replace(".txt", "");
                    string curNum = (result[i2].Split('\\')[result[i2].Split('\\').Length - 1]).Replace(".txt", "");

                    if (Convert.ToInt32(previousNum) > Convert.ToInt32(curNum))
                    {
                        string prevAdress = result[i2];
                        result[i2] = result[i2 - 1];
                        result[i2 - 1] = prevAdress;
                    }
                }
            }

            return result.ToArray();
        }

        private int _counterMyTrades = 0;
        private void TryLoadMyTrade()
        {
            if (Directory.Exists("Atp_Router\\Files\\MyTrades\\") == false)
            {
                return;
            }

            string[] files = Directory.GetFiles("Atp_Router\\Files\\MyTrades\\");

            if (_counterMyTrades >= files.Length)
            {
                return;
            }

            files = GetSortedFileNames(files);

            for (int i = _counterMyTrades; i < files.Length; i++)
            {
                try
                {
                    /*  DateTime timeCreate = File.GetCreationTime(files[i]);
                      if (timeCreate.AddSeconds(1) > DateTime.Now)
                      {
                          return;
                      }*/

                    using (StreamReader reader = new StreamReader(files[i]))
                    {
                        string myTrade = reader.ReadLine();

                        if (myTrade.EndsWith("%"))
                        {
                            myTrade = myTrade.Substring(0, myTrade.Length - 1);
                        }

                        LoadMyTrade(myTrade);
                    }
                }
                catch (Exception e)
                {
                    SendLogMessage(e.ToString(), LogMessageType.Error);
                }
            }

            _counterMyTrades = files.Length;
        }

        private int _counterMyTrades2 = 0;
        private void TryLoadMyTrades2()
        {
            if (Directory.Exists("Atp_Router\\Files\\MyTrades2\\") == false)
            {
                return;
            }

            string[] files = Directory.GetFiles("Atp_Router\\Files\\MyTrades2\\");

            if (_counterMyTrades2 >= files.Length)
            {
                return;
            }

            files = GetSortedFileNames(files);

            for (int i = _counterMyTrades2; i < files.Length; i++)
            {
                try
                {
                    /* DateTime timeCreate = File.GetCreationTime(files[i]);
                     if (timeCreate.AddSeconds(1) > DateTime.Now)
                     {
                         return;
                     }*/
                    using (StreamReader reader = new StreamReader(files[i]))
                    {
                        string myTrade = reader.ReadLine();

                        if (myTrade.EndsWith("%"))
                        {
                            myTrade = myTrade.Substring(0, myTrade.Length - 1);
                        }

                        LoadMyTrade(myTrade);
                    }
                }
                catch (Exception e)
                {
                    SendLogMessage(e.ToString(), LogMessageType.Error);
                }
            }

            _counterMyTrades2 = files.Length;
        }

        private int _counterOrderAction2 = 0;
        private void TryLoadOrderAction2()
        {
            if (Directory.Exists("Atp_Router\\Files\\OrderAction2\\") == false)
            {
                return;
            }

            string[] files = Directory.GetFiles("Atp_Router\\Files\\OrderAction2\\");

            if (_counterOrderAction2 >= files.Length)
            {
                return;
            }

            files = GetSortedFileNames(files);

            for (int i = _counterOrderAction2; i < files.Length; i++)
            {
                try
                {
                    /* DateTime timeCreate =  File.GetCreationTime(files[i]);
                     if(timeCreate.AddSeconds(1) > DateTime.Now)
                     {
                         return;
                     }*/

                    using (StreamReader reader = new StreamReader(files[i]))
                    {
                        string myTrade = reader.ReadLine();

                        if (myTrade.EndsWith("%"))
                        {
                            myTrade = myTrade.Substring(0, myTrade.Length - 1);
                        }

                        LoadMyOrder(myTrade);
                    }
                }
                catch (Exception e)
                {
                    SendLogMessage(e.ToString(), LogMessageType.Error);
                }
            }

            _counterOrderAction2 = files.Length;
        }

        private int _counterOrderFail1 = 0;
        private void TryLoadOrderFail1()
        {
            if (Directory.Exists("Atp_Router\\Files\\OrderFail1\\") == false)
            {
                return;
            }

            string[] files = Directory.GetFiles("Atp_Router\\Files\\OrderFail1\\");

            if (_counterOrderFail1 >= files.Length)
            {
                return;
            }

            files = GetSortedFileNames(files);

            for (int i = _counterOrderFail1; i < files.Length; i++)
            {
                try
                {
                    /* DateTime timeCreate = File.GetCreationTime(files[i]);
                     if (timeCreate.AddSeconds(1) > DateTime.Now)
                     {
                         return;
                     }*/
                    using (StreamReader reader = new StreamReader(files[i]))
                    {
                        string myTrade = reader.ReadLine();

                        if (myTrade.EndsWith("%"))
                        {
                            myTrade = myTrade.Substring(0, myTrade.Length - 1);
                        }

                        LoadMyFailOrder(myTrade);
                    }
                }
                catch (Exception e)
                {
                    SendLogMessage(e.ToString(), LogMessageType.Error);
                }
            }

            _counterOrderFail1 = files.Length;
        }

        private int _counterOrderFail3 = 0;
        private void TryLoadOrderFail3()
        {
            if (Directory.Exists("Atp_Router\\Files\\OrderFail3\\") == false)
            {
                return;
            }

            string[] files = Directory.GetFiles("Atp_Router\\Files\\OrderFail3\\");

            if (_counterOrderFail3 >= files.Length)
            {
                return;
            }

            files = GetSortedFileNames(files);

            for (int i = _counterOrderFail3; i < files.Length; i++)
            {
                try
                {
                    /*DateTime timeCreate = File.GetCreationTime(files[i]);
                    if (timeCreate.AddSeconds(1) > DateTime.Now)
                    {
                        return;
                    }*/
                    using (StreamReader reader = new StreamReader(files[i]))
                    {
                        string myTrade = reader.ReadLine();

                        if (myTrade.EndsWith("%"))
                        {
                            myTrade = myTrade.Substring(0, myTrade.Length - 1);
                        }

                        LoadMyFailOrder(myTrade);
                    }
                }
                catch (Exception e)
                {
                    SendLogMessage(e.ToString(), LogMessageType.Error);
                }
            }

            _counterOrderFail3 = files.Length;
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

            SendLogMessage("Trade router message: " + message, LogMessageType.System);

            if (message.StartsWith("{\"type\":\"connect\"") &&
                ServerStatus == ServerConnectStatus.Disconnect)
            {
                SendLogMessage("Trade router is connected", LogMessageType.System);
                _tradeSocketConnect = true;
                CheckConnectStatus();
            }
            else if (message.StartsWith("{\"type\":\"disconnect\""))
            {
                ServerStatus = ServerConnectStatus.Disconnect;
                DisconnectEvent?.Invoke();
            }
            else if (message.Contains("{\"type\":\"account\""))
            {
                GetPortfolioDate(message);
            }
            else if (message.Contains("{\"type\":\"positions\""))
            {
                GetPositionsDate(message);
            }
            else if (message.Contains("\"type\":\"security\""))
            {
                GetSecurityList(message);
            }
            else if (message.Contains("\"type\":\"rtnOrder\""))
            {
                GetMyOrder(message);
                //SendLogMessage(message, LogMessageType.Error);
            }
            else if (message.Contains("\"type\":\"rtnMatch\""))
            {
                GetMyTrade(message);
                //SendLogMessage(message, LogMessageType.Error);
            }
            else if (message.Contains("\"type\":\"rspOrderInsert\""))
            {
                SaveOrderNumberUser(message);
                //SendLogMessage(message, LogMessageType.Error);
            }
            
        }

        private Dictionary<string, int> _listNumber = new();

        private void SaveOrderNumberUser(string message)
        {
            try
            {
                ResponceMessageOrderNumber responce = JsonConvert.DeserializeAnonymousType(message, new ResponceMessageOrderNumber());

                int numberMarket = 0;

                if (!int.TryParse(responce.clientReqId, out numberMarket))
                {
                    return;
                }

                if (numberMarket == 0)
                {
                    return;
                }

                _listNumber.Add(responce.orderId, numberMarket);
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }
                
        private void GetMyOrder(string message)
        {
            try
            {
                ResponceMessageMyOrder responce = JsonConvert.DeserializeAnonymousType(message, new ResponceMessageMyOrder());

                if (!_listNumber.ContainsKey(responce.orderId))
                {
                    return;
                }

                Order order = new();

                order.SecurityNameCode = responce.contractNo1;
                order.NumberMarket = responce.orderId;
                order.NumberUser = _listNumber[responce.orderId];
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
                order.TimeCreate = DateTime.UtcNow;
                order.TimeCallBack = DateTime.UtcNow;

                //MyOrderEvent?.Invoke(order);

                SendLogMessage($"NumberUser: {order.NumberUser}, NumberMarket: {order.NumberMarket}", LogMessageType.Error);

                if (MyOrderEvent != null)
                {
                    MyOrderEvent(order);
                }

                if (order.State == OrderStateType.Done ||
                    order.State == OrderStateType.Cancel ||
                    order.State == OrderStateType.Fail ||
                    order.State == OrderStateType.None)
                {
                    _listNumber.Remove(responce.orderId);
                }
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
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
            switch (offset)
            {
                case "O": return OrderPositionConditionType.Open;
                case "C": return OrderPositionConditionType.Close;
                default: return OrderPositionConditionType.None;
            }

            /*'O' Open
            'C' Close
            'T' Close today*/
        }

        private Side GetDirectionOrder(string direct)
        {
            switch (direct)
            {
                case "B": return Side.Buy;
                case "S": return Side.Sell;
                default: return Side.None;
            }

            /*const DstarApiDirectType DSTAR_API_DIRECT_BUY = 'B';          // Buy
            const DstarApiDirectType DSTAR_API_DIRECT_SELL = 'S';          // Sell
            const DstarApiDirectType DSTAR_API_DIRECT_ALL = 'N';          // All*/
        }

        private OrderStateType GetOrderState(string orderState)
        {
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

            /*const DstarApiOrderStateType DSTAR_API_STATUS_ACCEPT = '1';          // Accepted
            const DstarApiOrderStateType DSTAR_API_STATUS_QUEUE = '2';          // Queued
            const DstarApiOrderStateType DSTAR_API_STATUS_APPLY = '3';          // Applied (exercise/abandon/spread application succeeded)
            const DstarApiOrderStateType DSTAR_API_STATUS_SUSPENDED = '4';          // Suspended
            const DstarApiOrderStateType DSTAR_API_STATUS_TRIGGERED = '5';          // Triggered
            const DstarApiOrderStateType DSTAR_API_STATUS_PARTFILL = '6';          // Partially filled
            const DstarApiOrderStateType DSTAR_API_STATUS_FILL = '7';          // Fully filled
            const DstarApiOrderStateType DSTAR_API_STATUS_FAIL = '8';          // Command failed
            const DstarApiOrderStateType DSTAR_API_STATUS_DELETE = 'B';          // Canceled
            const DstarApiOrderStateType DSTAR_API_STATUS_LEFTDELETE = 'C';          // Remaining quantity canceled
            const DstarApiOrderStateType DSTAR_API_STATUS_SYSDELETE = 'D';          // Deleted
            const DstarApiOrderStateType DSTAR_API_STATUS_TRIGGERING = 'E';          // Strategy pending trigger*/
        }

        private OrderPriceType GetOrderPriceType(string orderType)
        {
            switch (orderType)
            {
                case "1": return OrderPriceType.Market;
                case "2": return OrderPriceType.Limit;

                default: return OrderPriceType.Market;
            }

            /*const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_NONE = '0';          // None
            const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_MARKET = '1';          // Market order
            const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_LIMIT = '2';          // Limit order
            const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_EXECUTE = '3';          // Exercise
            const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_ABANDON = '4';          // Abandon
            const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_ENQUIRY = '5';          // Inquiry
            const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_OFFER = '6';          // Quote
            const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_SWAP = '7';          // Swap
            const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_EFP = '8';          // EFP*/
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
                trade.Time = DateTime.UtcNow;
                trade.NumberTrade = responce.matchId;
                
                //MyTradeEvent?.Invoke(trade);

                if (MyTradeEvent != null)
                {
                    MyTradeEvent(trade);
                }
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

            //SendLogMessage("DateRouter: " + message, LogMessageType.System);

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

                SendLogMessage("MarketData. Code: " + responce.code + ", Message: " + responce.message, LogMessageType.Error);
            }
            else if (message.Contains("\"type\":\"quote\""))
            {
                ParseQuote(message);
            }            
            else
            {
                SendLogMessage(message, LogMessageType.System);
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
            _trade.Time = DateTime.UtcNow;
            _trade.Volume = responce.lastQty.ToDecimal();
            _trade.Side = Side.Buy;

            NewTradesEvent?.Invoke(_trade);
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
            marketDepth.Time = DateTime.UtcNow;

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

        private void LoadMyOrder(string strMyOrder)
        {
            //OrderAction2      0
            //@177              1 UserOrderId OrderRef
            //@NI2312 - SH      2 InstrumentID
            //@0                3 Direction
            //@3                4 OrderStatus
            //@20231127         5 InsertDate
            //@06:17:17         6 InsertTime
            //@12344            7 LimitPrice
            //@1                8 VolumeTotalOriginal

            string[] ordArr = strMyOrder.Split('@');

            Order order = new Order();

            order.NumberMarket = ordArr[1];

            try
            {
                order.NumberUser = Convert.ToInt32(ordArr[0]);
            }
            catch
            {
                try
                {
                    order.NumberUser = Convert.ToInt32(ordArr[1]);
                }
                catch
                {
                    // ignore
                }
            }

            if (string.IsNullOrEmpty(order.NumberMarket) == true)
            {
                order.NumberMarket = ordArr[9];
            }

            order.SecurityNameCode = ordArr[2];

            order.Price = ordArr[7].ToDecimal();
            order.Volume = ordArr[8].ToDecimal();


            string date = ordArr[5];
            string time = ordArr[6];

            DateTime timeData = GetTimeFromStrings(date, time);
            order.TimeCallBack = timeData;

            if (ordArr[3] == "0")
            {
                order.Side = Side.Buy;
            }
            else
            {
                order.Side = Side.Sell;
            }

            string status = ordArr[4];

            if (status == "0")
            { // Done
                order.State = OrderStateType.Done;
            }
            else if (status == "5")
            { // Cancel
                order.State = OrderStateType.Cancel;
            }
            else if (status == "3")
            { // Cancel
                order.State = OrderStateType.Active;
            }
            else
            {
                return;
            }

            if (order.State == OrderStateType.Active
                && _lastConnectTime.AddMinutes(1) > DateTime.Now)
            {
                return;
            }

            if (MyOrderEvent != null)
            {
                MyOrderEvent(order);
            }
        }

        private void LoadMyFailOrder(string strMyOrder)
        {
            //OrderFail3@922@55@ @@@15@The order has been all traded or canceled.%

            string[] ordArr = strMyOrder.Split('@');

            Order order = new Order();

            order.NumberMarket = ordArr[1];

            try
            {
                order.NumberUser = Convert.ToInt32(ordArr[1]);
            }
            catch
            {
                // ignore
            }

            order.State = OrderStateType.Cancel;

            if (MyOrderEvent != null)
            {
                MyOrderEvent(order);
            }

            SendLogMessage(strMyOrder, LogMessageType.Error);
        }

        private void LoadMyTrade(string strMyTrade)
        {//MyTrade1      0
         //@177          1 UserOrderId
         //@MTwmwtbx     2
         //@20231127     3 Date
         //@06:17:17     4 Time
         //@NI2312 - SH  5 SecCode
         //@1            6 Volume
         //@0 %          7 Price
         //@0 %          8 Direction
         //@asdfag213    9 TradeId

            string[] mtArr = strMyTrade.Split('@');

            MyTrade trade = new MyTrade();
            trade.NumberOrderParent = mtArr[1];

            if (string.IsNullOrEmpty(trade.NumberOrderParent) == true)
            {
                trade.NumberOrderParent = mtArr[2];
            }

            trade.SecurityNameCode = mtArr[5].Replace(" ", "");

            string date = mtArr[3];
            string time = mtArr[4];

            DateTime timeData = GetTimeFromStrings(date, time);
            trade.Time = timeData;

            decimal volume = mtArr[6].ToDecimal();
            decimal price = mtArr[7].ToDecimal();

            trade.Volume = volume;
            trade.Price = price;

            if (mtArr[8] == "0")
            {
                trade.Side = Side.Buy;
            }
            else
            {
                trade.Side = Side.Sell;
            }

            trade.NumberTrade = mtArr[9];

            if (MyTradeEvent != null)
            {
                MyTradeEvent(trade);
            }

            /*
            /////////////////////////////////////////////////////////////////////////
            ///TFtdcDirectionType是一个买卖方向类型
            /////////////////////////////////////////////////////////////////////////
            ///买
            #define THOST_FTDC_D_Buy '0'
            ///卖
            #define THOST_FTDC_D_Sell '1'

            typedef char TThostFtdcDirectionType;*/
        }

        private void LoadMd(string md)
        {
            /*
             * 
            InstrumentID);
            TradingDay);
            UpdateTime);

            LastPrice);
            Volume);

            BidPrice1);
            BidVolume1);

            AskPrice1);
            AskVolume1);
            */

            string[] str = md.Split('@');

            MarketDepth newMd = new MarketDepth();

            string date = str[2];
            string time = str[3];

            DateTime timeData = GetTimeFromStrings(date, time);

            // формируем трейд

            Trade newTrade = new Trade();
            newTrade.SecurityNameCode = str[1];
            newTrade.Time = timeData;
            newTrade.Price = str[4].ToDecimal();
            newTrade.Volume = str[5].ToDecimal();

            if (NewTradesEvent != null)
            {
                bool isSameTimeInArray = false;
                bool isInArray = false;

                for (int i = 0; i < _lastTrades.Count; i++)
                {
                    if (_lastTrades[i].SecurityNameCode == newTrade.SecurityNameCode)
                    {
                        isInArray = true;
                        if (_lastTrades[i].Time == newTrade.Time)
                        {
                            isSameTimeInArray = true;
                            break;
                        }

                        _lastTrades[i] = newTrade;
                        break;
                    }
                }

                if (isInArray == false)
                {
                    _lastTrades.Add(newTrade);
                }

                if (isSameTimeInArray == false)
                {
                    NewTradesEvent(newTrade);
                }
            }

            // формируем стакан

            newMd.SecurityNameCode = str[1];

            MarketDepthLevel b1 = GetBid(str[6], str[7]);

            MarketDepthLevel a1 = GetAsk(str[8], str[9]);

            if (b1.Price != 0
                && b1.Bid != 0)
            {
                newMd.Bids.Add(b1);
            }

            if (a1.Price != 0
                && a1.Ask != 0)
            {
                newMd.Asks.Add(a1);
            }

            if (newMd.Asks.Count != 0 ||
                newMd.Bids.Count != 0)
            {
                if (MarketDepthEvent != null)
                {
                    MarketDepthEvent(newMd);
                }
            }
        }

        private DateTime GetTimeFromStrings(string date, string time)
        {
            int year = 0;
            int month = 0;
            int day = 0;

            try
            {
                year = Convert.ToInt32(date.Substring(0, 4));
                month = Convert.ToInt32(date.Substring(4, 2));
                day = Convert.ToInt32(date.Substring(6, 2));
            }
            catch
            {
                DateTime now = DateTime.Now;
                year = now.Year;
                month = now.Month;
                day = now.Day;
            }

            // Time 3 "15:00:00"

            int hour = 0;
            int minute = 0;
            int second = 0;

            try
            {
                hour = Convert.ToInt32(time.Substring(0, 2));
                minute = Convert.ToInt32(time.Substring(3, 2));
                second = Convert.ToInt32(time.Substring(6, 2));
            }
            catch
            {
                DateTime now = DateTime.Now;
                hour = now.Hour;
                minute = now.Minute;
                second = now.Second;
            }

            DateTime timeData = new DateTime(year, month, day, hour, minute, second);

            return timeData;
        }

        private MarketDepthLevel GetBid(string price, string vol)
        {
            MarketDepthLevel level = new MarketDepthLevel();

            level.Bid = vol.ToDouble();
            level.Price = price.ToDouble();

            return level;
        }

        private MarketDepthLevel GetAsk(string price, string vol)
        {
            MarketDepthLevel level = new MarketDepthLevel();

            level.Ask = vol.ToDouble();
            level.Price = price.ToDouble();

            return level;
        }

        private List<Trade> _lastTrades = new List<Trade>();

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

            order.NumberMarket = order.NumberUser.ToString();

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
            /*if (order.NumberUser == 0)
            {
                SendLogMessage("NumberUser is 0. Can`t cancel order", LogMessageType.Error);
            }*/

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
