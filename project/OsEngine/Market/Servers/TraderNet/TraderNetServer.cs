using Newtonsoft.Json;
using OsEngine.Entity;
using OsEngine.Language;
using OsEngine.Logging;
using OsEngine.Market.Servers.TraderNet.Entity;
using OsEngine.Market.Servers.Entity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using WebSocket4Net;
using System.IO;
using System.Collections;

namespace OsEngine.Market.Servers.TraderNet
{
    public class TraderNetServer : AServer
    {
        public TraderNetServer()
        {

            TraderNetServerRealization realization = new TraderNetServerRealization();
            ServerRealization = realization;

            CreateParameterString(OsLocalization.Market.ServerParamPublicKey, "");
            CreateParameterPassword(OsLocalization.Market.ServerParamSecretKey, "");            
        }
    }

    public class TraderNetServerRealization : IServerRealization
    {
        #region 1 Constructor, Status, Connection

        public TraderNetServerRealization()
        {
            ServerStatus = ServerConnectStatus.Disconnect;

            /*Thread threadMessageReaderPublic = new Thread(MessageReaderPublic);
            threadMessageReaderPublic.IsBackground = true;
            threadMessageReaderPublic.Name = "MessageReaderPublic";
            threadMessageReaderPublic.Start();

            Thread threadMessageReaderPrivate = new Thread(MessageReaderPrivate);
            threadMessageReaderPrivate.IsBackground = true;
            threadMessageReaderPrivate.Name = "MessageReaderPrivate";
            threadMessageReaderPrivate.Start();

            Thread thread = new Thread(CheckAliveWebSocket);
            thread.Name = "CheckAliveWebSocket";
            thread.Start();*/
        }

        public void Connect()
        {
            _publicKey = ((ServerParameterString)ServerParameters[0]).Value;
            _secretKey = ((ServerParameterPassword)ServerParameters[1]).Value;

            if (string.IsNullOrEmpty(_publicKey) ||
                string.IsNullOrEmpty(_secretKey))
            {
                SendLogMessage("Can`t run TraderNet connector. No API keys",
                    LogMessageType.Error);
                return;
            }

           /* ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Ssl3
                | SecurityProtocolType.Tls11
                | SecurityProtocolType.Tls;*/

            try
            {
                Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
                data.Add("apiKey", _publicKey+"1");
                data.Add("cmd", "getSidInfo");

                HttpResponseMessage responseMessage = CreateAuthQuery($"/api/v2/cmd/getSidInfo", "POST", null, data);
                string JsonResponse = responseMessage.Content.ReadAsStringAsync().Result;

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    TimeToSendPingPublic = DateTime.Now;
                    TimeToSendPingPrivate = DateTime.Now;
                    FIFOListWebSocketPublicMessage = new ConcurrentQueue<string>();
                    FIFOListWebSocketPrivateMessage = new ConcurrentQueue<string>();
                    CreateWebSocketConnection();
                    _lastConnectionStartTime = DateTime.Now;
                }
                else
                {
                    SendLogMessage("Connection can be open. TraderNet. Error request", LogMessageType.Error);

                    if (ServerStatus != ServerConnectStatus.Disconnect)
                    {
                        ServerStatus = ServerConnectStatus.Disconnect;
                        DisconnectEvent();
                    }
                }
            }
            catch (Exception exeption)
            {
                SendLogMessage(exeption.ToString(), LogMessageType.Error);
                SendLogMessage("Connection can be open. TraderNet. Error request", LogMessageType.Error);

                if (ServerStatus != ServerConnectStatus.Disconnect)
                {
                    ServerStatus = ServerConnectStatus.Disconnect;
                    DisconnectEvent();
                }
            }
        }

        public void Dispose()
        {
            try
            {
                //UnsubscribeFromAllWebSockets();
                _subscribledSecutiries.Clear();
                //DeleteWebSocketConnection();
            }
            catch (Exception exeption)
            {
                SendLogMessage(exeption.ToString(), LogMessageType.Error);
            }

            FIFOListWebSocketPublicMessage = null;
            FIFOListWebSocketPrivateMessage = null;

            if (ServerStatus != ServerConnectStatus.Disconnect)
            {
                ServerStatus = ServerConnectStatus.Disconnect;
                DisconnectEvent();
            }
        }

        public ServerType ServerType
        {
            get { return ServerType.TraderNet; }
        }

        public ServerConnectStatus ServerStatus { get; set; }

        public DateTime ServerTime { get; set; }

        private DateTime _lastConnectionStartTime = DateTime.MinValue;

        public event Action ConnectEvent;

        public event Action DisconnectEvent;

        #endregion

        #region 2 Properties

        public List<IServerParameter> ServerParameters { get; set; }

        private string _baseUrl = "https://tradernet.ru";

        private string _publicKey;

        private string _secretKey;

        private int _limitCandlesData = 200;

        private int _limitCandlesTrader = 1000;

        private List<string> _listCoin;

        private bool _hedgeMode;

        private string _marginMode = "crossed";

        private Dictionary<string, List<string>> _allPositions = new Dictionary<string, List<string>>();

        #endregion

        #region 3 Securities

        public void GetSecurities()
        {
          
            try
            {
                /* string requestStr = $"/api/v2/mix/market/contracts?productType={_listCoin[indCoin]}";
                    RestRequest requestRest = new RestRequest(requestStr, Method.GET);
                    IRestResponse response = new RestClient(_baseUrl).Execute(requestRest);

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        SendLogMessage($"Http State Code: {response.StatusCode} - {response.Content}", LogMessageType.Error);
                        continue;
                    }

                    ResponseRestMessage<List<RestMessageSymbol>> symbols = JsonConvert.DeserializeAnonymousType(response.Content, new ResponseRestMessage<List<RestMessageSymbol>>());*/

                /*using (StreamWriter sw = new StreamWriter(@"C:\1.csv", false, Encoding.Default))
                {
                    for (int i = 0; i < _listSecurities.Count; i++)
                    {
                        sw.WriteLine($"{_listSecurities[i].ticker};" +
                                    $"{_listSecurities[i].instr_id};" +
                                    $"{_listSecurities[i].instr_type_c};" +
                                    $"{_listSecurities[i].code_sec};" +
                                    $"{_listSecurities[i].mkt_short_code};" +
                                    $"{_listSecurities[i].step_price};" +
                                    $"{_listSecurities[i].min_step};" +
                                    $"{_listSecurities[i].lot_size_q}");
                    }
                }*/

                ResponceMessageSecurities symbols = new ResponceMessageSecurities();

                symbols = GetSecuritiesFromFile();

                if (symbols.securities.Count == 0)
                {
                    return;
                }

                List<Security> securities = new List<Security>();

                for (int i = 0; i < symbols.securities.Count; i++)
                {
                    ListSecurities item = symbols.securities[i];

                    Security newSecurity = new Security();

                    newSecurity.Exchange = ServerType.TraderNet.ToString();
                    newSecurity.DecimalsVolume = item.lot_size_q.DecimalsCount();
                    newSecurity.Lot = item.lot_size_q.ToDecimal();
                    newSecurity.Name = item.ticker;
                    newSecurity.NameFull = item.ticker;
                    newSecurity.NameClass = item.mkt_short_code;
                    newSecurity.NameId = item.instr_id;
                    newSecurity.SecurityType = GetSecurityType(Convert.ToInt32(item.instr_type_c));
                    newSecurity.Decimals = item.min_step.DecimalsCount();
                    newSecurity.PriceStep = item.step_price.ToDecimal();
                    newSecurity.PriceStepCost = newSecurity.PriceStep;
                    newSecurity.State = SecurityStateType.Activ;

                    securities.Add(newSecurity);                    
                }
                SecurityEvent(securities);
            }

            catch (Exception exception)
            {
                SendLogMessage(exception.ToString(), LogMessageType.Error);
            }            
        }

        private SecurityType GetSecurityType(int code)
        {
            SecurityType _securityType = SecurityType.None;

            switch (code)
            {
                case (1):
                    _securityType = SecurityType.Stock;
                    break;
                case (2):
                    _securityType = SecurityType.Bond;
                    break;
                case (3):
                    _securityType = SecurityType.Futures;
                    break;
                case (4):
                    _securityType = SecurityType.Option;
                    break;
                case (5):
                    _securityType = SecurityType.Index;
                    break;
                case (6):
                    _securityType = SecurityType.CurrencyPair;
                    break;
                case (7):
                    _securityType = SecurityType.CurrencyPair;
                    break;
            }
            return _securityType;
        }

        private ResponceMessageSecurities GetSecuritiesFromFile()
        {
            if (!File.Exists(@"Engine\TraderNetSecurities.csv"))
            {
                return null;
            }

            ResponceMessageSecurities symbols = new ResponceMessageSecurities();

            using (StreamReader reader = new StreamReader(@"Engine\TraderNetSecurities.csv"))
            {
                string line;

                symbols.securities = new List<ListSecurities>();

                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        string[] split = line.Split(';');

                        symbols.securities.Add(new ListSecurities());
                        symbols.securities[symbols.securities.Count - 1].ticker = split[0];
                        symbols.securities[symbols.securities.Count - 1].instr_id = split[1];
                        symbols.securities[symbols.securities.Count - 1].instr_type_c = split[2];
                        symbols.securities[symbols.securities.Count - 1].code_sec = split[3];
                        symbols.securities[symbols.securities.Count - 1].mkt_short_code = split[4];
                        symbols.securities[symbols.securities.Count - 1].step_price = split[5];
                        symbols.securities[symbols.securities.Count - 1].min_step = split[6];
                        symbols.securities[symbols.securities.Count - 1].lot_size_q = split[7];
                    }
                }
            }
            return symbols;
        }

        public event Action<List<Security>> SecurityEvent;

        #endregion

        #region 4 Portfolios

        public void GetPortfolios()
        {
        }

        private bool _portfolioIsStarted = false;

        public event Action<List<Portfolio>> PortfolioEvent;

        #endregion

        #region 5 Data

        public List<Candle> GetLastCandleHistory(Security security, TimeFrameBuilder timeFrameBuilder, int candleCount)
        {
            int tfTotalMinutes = (int)timeFrameBuilder.TimeFrameTimeSpan.TotalMinutes;
            DateTime endTime = DateTime.UtcNow;
            DateTime startTime = endTime.AddMinutes(-tfTotalMinutes * candleCount);

            return GetCandleData(security, timeFrameBuilder, startTime, endTime, endTime);
        }

        public List<Candle> GetCandleDataToSecurity(Security security, TimeFrameBuilder timeFrameBuilder, DateTime startTime, DateTime endTime, DateTime actualTime)
        {
            return GetCandleData(security, timeFrameBuilder, startTime, endTime, actualTime);
        }

        private List<Candle> GetCandleData(Security security, TimeFrameBuilder timeFrameBuilder, DateTime startTime, DateTime endTime, DateTime actualTime)
        {
            if (!CheckTime(startTime, endTime, actualTime))
            {
                return null;
            }

            int tfTotalMinutes = (int)timeFrameBuilder.TimeFrameTimeSpan.TotalMinutes;

            List<Candle> allCandles = RequestCandleHistory(security, tfTotalMinutes, startTime, endTime);

            if (allCandles[allCandles.Count - 1].TimeStart <= endTime)
            {
                startTime = allCandles[allCandles.Count - 1].TimeStart.AddHours(5);
                List<Candle> candles = RequestCandleHistory(security, tfTotalMinutes, startTime, endTime);

                while (true)
                {
                    if (candles[0].TimeStart <= allCandles[allCandles.Count - 1].TimeStart)
                    {
                        candles.RemoveAt(0);
                    }
                    else
                    {
                        allCandles.AddRange(candles);
                        break;
                    }
                }
            }

        return allCandles;
        }

        private bool CheckTime(DateTime startTime, DateTime endTime, DateTime actualTime)
        {
            if (startTime >= endTime ||
                startTime >= DateTime.Now ||
                actualTime > endTime ||
                actualTime > DateTime.Now ||
                endTime < DateTime.UtcNow.AddYears(-20))
            {
                return false;
            }
            return true;
        }

        private bool CheckTf(int timeFrameMinutes)
        {
            if (timeFrameMinutes == 1 ||
                timeFrameMinutes == 5 ||
                timeFrameMinutes == 15 ||
                timeFrameMinutes == 60 ||
                timeFrameMinutes == 1440)
            {
                return true;
            }
            return false;
        }

        private readonly RateGate _rgCandleData = new RateGate(1, TimeSpan.FromMilliseconds(3000));

        private List<Candle> RequestCandleHistory(Security security, int interval, DateTime startTime, DateTime endTime)
        {
            _rgCandleData.WaitToProceed();

            try
            {
                RequestCandle reqData = new RequestCandle();
                reqData.q = new RequestCandle.Q();
                reqData.q.cmd = "getHloc";
                reqData.q.@params = new RequestCandle.Params();
                reqData.q.@params.id = security.Name;
                reqData.q.@params.timeframe = interval;
                reqData.q.@params.count = -1;
                reqData.q.@params.date_from = startTime.ToString("dd.MM.yyyy HH:mm");
                reqData.q.@params.date_to = endTime.ToString("dd.MM.yyyy HH:mm");

                HttpResponseMessage responseMessage = CreateQuery($"/api/", "POST", null, reqData);
                string JsonResponse = responseMessage.Content.ReadAsStringAsync().Result;

                return ConvertCandles(JsonResponse);
            }            
            catch (Exception exception)
            {
                SendLogMessage(exception.ToString(), LogMessageType.Error);
                return null;
            }             
        }

        private List<Candle> ConvertCandles(string JsonResponse)
        {
            ResponseCandle result = JsonConvert.DeserializeObject<ResponseCandle>(JsonResponse);

            if (result == null)
            {
                return null;
            }

            List<List<string>> listHloc = new List<List<string>>();
            List<string> listVl = new List<string>();
            List<string> listSeries = new List<string>();

            IDictionaryEnumerator enumerator = result.hloc.GetEnumerator();
            while (enumerator.MoveNext())
            {
                listHloc = (List<List<string>>)enumerator.Value;
            }
            enumerator.Reset();

            enumerator = result.vl.GetEnumerator();
            while (enumerator.MoveNext())
            {
                listVl = (List<string>)enumerator.Value;
            }
            enumerator.Reset();

            enumerator = result.xSeries.GetEnumerator();
            while (enumerator.MoveNext())
            {
                listSeries = (List<string>)enumerator.Value;
            }
            enumerator.Reset();

            List<Candle> candles = new List<Candle>();

            for (int i = 0; i < listHloc.Count; i++)
            {
                if (CheckCandlesToZeroData(listHloc[i]))
                {
                    continue;
                }

                Candle candle = new Candle();

                candle.State = CandleState.Finished;
                candle.High = listHloc[i][0].ToDecimal();
                candle.Low = listHloc[i][1].ToDecimal();
                candle.Open = listHloc[i][2].ToDecimal();
                candle.Close = listHloc[i][3].ToDecimal();
                candle.Volume = listVl[i].ToDecimal();
                candle.TimeStart = TimeManager.GetDateTimeFromTimeStampSeconds(long.Parse(listSeries[i]));

                candles.Add(candle);
            }
            return candles;
        }

        private bool CheckCandlesToZeroData(List<string> item)
        {
            if (item[0].ToDecimal() == 0 ||
                item[1].ToDecimal() == 0 ||
                item[2].ToDecimal() == 0 ||
                item[3].ToDecimal() == 0)
            {
                return true;
            }
            return false;
        }

        public List<Trade> GetTickDataToSecurity(Security security, DateTime startTime, DateTime endTime, DateTime actualTime)
        {
            return null;
        }

        #endregion

        #region 6 WebSocket creation

        private string _webSocketUrl = "wss://wss.tradernet.ru";

        private WebSocket _webSocket;
        
        private void CreateWebSocketConnection()
        {
            try
            {

                if (_webSocket != null)
                {
                    return;
                }

                _webSocket = new WebSocket(_webSocketUrl);
                _webSocket.EnableAutoSendPing = true;
                _webSocket.AutoSendPingInterval = 10;
                _webSocket.Opened += WebSocket_Opened;
                _webSocket.Closed += WebSocket_Closed;
                _webSocket.MessageReceived += WebSocket_MessageReceived;
                _webSocket.Error += WebSocket_Error;
                _webSocket.Open();               
            }
            catch (Exception exeption)
            {
                SendLogMessage(exeption.ToString(), LogMessageType.Error);
            }
        }

        private void DeleteWebSocketConnection()
        {
            if (_webSocket != null)
            {
                try
                {
                    _webSocket.Opened -= WebSocket_Opened;
                    _webSocket.Closed -= WebSocket_Closed;
                    _webSocket.MessageReceived -= WebSocket_MessageReceived;
                    _webSocket.Error -= WebSocket_Error;
                    _webSocket.Close();
                }
                catch
                {
                    // ignore
                }

                _webSocket = null;
            }           
        }

        private void CreateAuthMessageWebSocekt()
        {
           /* try
            {
                string TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                string Sign = GenerateSignature(TimeStamp, "GET", "/user/verify", null, null, _secretKey);

                RequestWebsocketAuth requestWebsocketAuth = new RequestWebsocketAuth();

                requestWebsocketAuth.op = "login";
                requestWebsocketAuth.args = new List<AuthItem>();
                requestWebsocketAuth.args.Add(new AuthItem());
                requestWebsocketAuth.args[0].apiKey = _publicKey;
                requestWebsocketAuth.args[0].passphrase = Passphrase;
                requestWebsocketAuth.args[0].timestamp = TimeStamp;
                requestWebsocketAuth.args[0].sign = Sign;

                string AuthJson = JsonConvert.SerializeObject(requestWebsocketAuth);

                _webSocketPrivate.Send(AuthJson);
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.Message, LogMessageType.Error);
            }*/
        }

        #endregion

        #region 7 WebSocket events

        private void WebSocket_Opened(object sender, EventArgs e)
        {
            try
            {
                if (ServerStatus == ServerConnectStatus.Disconnect)
                {
                    SendLogMessage("TraderNet WebSocket connection open", LogMessageType.System);
                    
                    ServerStatus = ServerConnectStatus.Connect;

                    if (ConnectEvent != null)
                    {
                        ConnectEvent();
                    }                    
                }

                //CreateAuthMessageWebSocekt();

            }
            catch (Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private void WebSocket_Closed(object sender, EventArgs e)
        {
            try
            {
                if (DisconnectEvent != null
                 & ServerStatus != ServerConnectStatus.Disconnect)
                {
                    SendLogMessage("Connection Closed by TraderNet. WebSocket Closed Event", LogMessageType.System);
                    ServerStatus = ServerConnectStatus.Disconnect;
                    DisconnectEvent();
                }
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private void WebSocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                if (e == null)
                {
                    return;
                }
                if (string.IsNullOrEmpty(e.Message))
                {
                    return;
                }
                if (e.Message.Length == 4)
                { // pong message
                    return;
                }

                if (FIFOListWebSocketPublicMessage == null)
                {
                    return;
                }

                FIFOListWebSocketPublicMessage.Enqueue(e.Message);
            }
            catch (Exception error)
            {
                SendLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

        private void WebSocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs error)
        {
            if (error.Exception != null)
            {
                SendLogMessage(error.Exception.ToString(), LogMessageType.Error);
            }
        }

        #endregion

        #region 8 WebSocket check alive

        private DateTime TimeToSendPingPublic = DateTime.Now;
        private DateTime TimeToSendPingPrivate = DateTime.Now;

        private void CheckAliveWebSocket()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(1000);

                    if (ServerStatus == ServerConnectStatus.Disconnect)
                    {
                        continue;
                    }

                    if (_webSocket != null &&
                        (_webSocket.State == WebSocketState.Open ||
                        _webSocket.State == WebSocketState.Connecting)
                        )
                    {
                        if (TimeToSendPingPublic.AddSeconds(25) < DateTime.Now)
                        {
                            _webSocket.Send("ping");
                            TimeToSendPingPublic = DateTime.Now;
                        }
                    }
                    else
                    {
                        if (ServerStatus != ServerConnectStatus.Disconnect)
                        {
                            ServerStatus = ServerConnectStatus.Disconnect;
                            DisconnectEvent();
                        }
                    }       
                }
                catch (Exception ex)
                {
                    SendLogMessage(ex.ToString(), LogMessageType.Error);
                    Thread.Sleep(3000);
                }
            }
        }

        #endregion

        #region 9 Security subscrible

        private RateGate _rateGateSubscrible = new RateGate(1, TimeSpan.FromMilliseconds(350));

        private List<Security> _subscribledSecutiries = new List<Security>();

        public void Subscrible(Security security)
        {
            try
            {
                _rateGateSubscrible.WaitToProceed();
                //CreateSubscribleSecurityMessageWebSocket(security);

            }
            catch (Exception exeption)
            {
                SendLogMessage(exeption.ToString(), LogMessageType.Error);
            }
        }

        private void CreateSubscribleSecurityMessageWebSocket(Security security)
        {
            try
            {
                if (ServerStatus == ServerConnectStatus.Disconnect)
                {
                    return;
                }

                if (_subscribledSecutiries != null)
                {
                    for (int i = 0; i < _subscribledSecutiries.Count; i++)
                    {
                        if (_subscribledSecutiries[i].Name.Equals(security.Name))
                        {
                            return;
                        }
                    }
                }

                _subscribledSecutiries.Add(security);

                _webSocket.Send($"{{\"op\": \"subscribe\",\"args\": [{{\"instType\": \"{security.NameClass}\",\"channel\": \"books15\",\"instId\": \"{security.Name}\"}}]}}");
                _webSocket.Send($"{{\"op\": \"subscribe\",\"args\": [{{ \"instType\": \"{security.NameClass}\",\"channel\": \"trade\",\"instId\": \"{security.Name}\"}}]}}");
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.Message, LogMessageType.Error);
            }
        }

        private void UnsubscribeFromAllWebSockets()
        {
            if (_webSocket != null)
            {
                try
                {
                    if (_subscribledSecutiries != null)
                    {
                        for (int i = 0; i < _subscribledSecutiries.Count; i++)
                        {
                            _webSocket.Send($"{{\"op\": \"unsubscribe\",\"args\": [{{\"instType\": \"{_subscribledSecutiries[i].NameClass}\",\"channel\": \"books15\",\"instId\": \"{_subscribledSecutiries[i].Name}\"}}]}}");
                            _webSocket.Send($"{{\"op\": \"unsubscribe\",\"args\": [{{\"instType\": \"{_subscribledSecutiries[i].NameClass}\",\"channel\": \"trade\",\"instId\": \"{_subscribledSecutiries[i].Name}\"}}]}}");
                        }
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }

        #endregion

        #region 10 WebSocket parsing the messages

        private ConcurrentQueue<string> FIFOListWebSocketPublicMessage = new ConcurrentQueue<string>();

        private ConcurrentQueue<string> FIFOListWebSocketPrivateMessage = new ConcurrentQueue<string>();

        private void MessageReaderPublic()
        {
            Thread.Sleep(5000);

            while (true)
            {
                try
                {
                    if (ServerStatus == ServerConnectStatus.Disconnect)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    if (FIFOListWebSocketPublicMessage.IsEmpty)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    string message = null;

                    FIFOListWebSocketPublicMessage.TryDequeue(out message);

                    if (message == null)
                    {
                        continue;
                    }

                    ResponseWebSocketMessageSubscrible SubscribleState = null;

                    try
                    {
                        SubscribleState = JsonConvert.DeserializeAnonymousType(message, new ResponseWebSocketMessageSubscrible());
                    }
                    catch (Exception error)
                    {
                        SendLogMessage("Error in message reader: " + error.ToString(), LogMessageType.Error);
                        SendLogMessage("message str: \n" + message, LogMessageType.Error);
                        continue;
                    }

                    if (SubscribleState.code != null)
                    {
                        if (SubscribleState.code.Equals("0") == false)
                        {
                            SendLogMessage("WebSocket listener error", LogMessageType.Error);
                            SendLogMessage(SubscribleState.code + "\n" +
                                SubscribleState.msg, LogMessageType.Error);

                            if (_lastConnectionStartTime.AddMinutes(5) > DateTime.Now)
                            { // если на старте вёб-сокета проблемы, то надо его перезапускать
                                ServerStatus = ServerConnectStatus.Disconnect;
                                DisconnectEvent();
                            }
                        }

                        continue;
                    }
                    else
                    {
                        ResponseWebSocketMessageAction<object> action = JsonConvert.DeserializeAnonymousType(message, new ResponseWebSocketMessageAction<object>());

                        if (action.arg != null)
                        {
                            if (action.arg.channel.Equals("books15"))
                            {
                                UpdateDepth(message);
                                continue;
                            }
                            if (action.arg.channel.Equals("trade"))
                            {
                                UpdateTrade(message);
                                continue;
                            }
                        }
                    }
                }
                catch (Exception exeption)
                {
                    SendLogMessage(exeption.ToString(), LogMessageType.Error);
                    Thread.Sleep(3000);
                }
            }
        }

        private void MessageReaderPrivate()
        {
            Thread.Sleep(5000);

            while (true)
            {
                try
                {
                    if (ServerStatus == ServerConnectStatus.Disconnect)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    if (FIFOListWebSocketPrivateMessage.IsEmpty)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    string message = null;

                    FIFOListWebSocketPrivateMessage.TryDequeue(out message);

                    if (message == null)
                    {
                        continue;
                    }

                    ResponseWebSocketMessageSubscrible SubscribleState = null;

                    try
                    {
                        SubscribleState = JsonConvert.DeserializeAnonymousType(message, new ResponseWebSocketMessageSubscrible());
                    }
                    catch (Exception error)
                    {
                        SendLogMessage("Error in message reader: " + error.ToString(), LogMessageType.Error);
                        SendLogMessage("message str: \n" + message, LogMessageType.Error);
                        continue;
                    }

                    if (SubscribleState.code != null)
                    {
                        if (SubscribleState.code.Equals("0") == false)
                        {
                            SendLogMessage("WebSocket listener error", LogMessageType.Error);
                            SendLogMessage(SubscribleState.code + "\n" +
                                SubscribleState.msg, LogMessageType.Error);

                            if (_lastConnectionStartTime.AddMinutes(5) > DateTime.Now)
                            { // если на старте вёб-сокета проблемы, то надо его перезапускать
                                ServerStatus = ServerConnectStatus.Disconnect;
                                DisconnectEvent();
                            }
                        }

                        continue;
                    }
                    else
                    {
                        ResponseWebSocketMessageAction<object> action = JsonConvert.DeserializeAnonymousType(message, new ResponseWebSocketMessageAction<object>());

                        if (action.arg != null)
                        {
                            if (action.arg.channel.Equals("account"))
                            {
                                UpdateAccount(message);
                                continue;
                            }
                            if (action.arg.channel.Equals("positions"))
                            {
                                UpdatePositions(message);
                                continue;
                            }
                            if (action.arg.channel.Equals("orders"))
                            {
                                UpdateOrder(message);
                                continue;
                            }
                        }
                    }
                }
                catch (Exception exeption)
                {
                    SendLogMessage(exeption.ToString(), LogMessageType.Error);
                    Thread.Sleep(3000);
                }
            }
        }

        private void UpdatePositions(string message)
        {
            ResponseWebSocketMessageAction<List<ResponseMessagePositions>> positions = JsonConvert.DeserializeAnonymousType(message, new ResponseWebSocketMessageAction<List<ResponseMessagePositions>>());

            if (positions.data == null)
            {
                return;
            }

            Portfolio portfolio = new Portfolio();
            portfolio.Number = "BitGetFutures";
            portfolio.ValueBegin = 1;
            portfolio.ValueCurrent = 1;

            if (positions != null)
            {
                if (positions.data.Count > 0)
                {
                    for (int i = 0; i < positions.data.Count; i++)
                    {
                        PositionOnBoard pos = new PositionOnBoard();
                        pos.PortfolioName = "BitGetFutures";
                        pos.SecurityNameCode = positions.data[i].instId;

                        if (positions.data[i].posMode == "hedge_mode")
                        {
                            pos.SecurityNameCode = positions.data[i].instId + "_" + positions.data[i].holdSide;
                        }

                        if (positions.data[i].holdSide == "long")
                        {
                            pos.ValueCurrent = positions.data[i].available.ToDecimal();
                            pos.ValueBlocked = positions.data[i].frozen.ToDecimal();
                        }
                        else if (positions.data[i].holdSide == "short")
                        {
                            pos.ValueCurrent = positions.data[i].available.ToDecimal() * -1;
                            pos.ValueBlocked = positions.data[i].frozen.ToDecimal();
                        }

                        if (_portfolioIsStarted == false)
                        {
                            pos.ValueBegin = pos.ValueCurrent;
                        }

                        portfolio.SetNewPosition(pos);

                        if (!_allPositions.ContainsKey(positions.arg.instType))
                        {
                            _allPositions.Add(positions.arg.instType, new List<string>());
                        }

                        if (!_allPositions[positions.arg.instType].Contains(pos.SecurityNameCode))
                        {
                            _allPositions[positions.arg.instType].Add(pos.SecurityNameCode);
                        }
                    }
                }

                if (_allPositions.ContainsKey(positions.arg.instType))
                {
                    if (_allPositions[positions.arg.instType].Count > 0)
                    {
                        for (int indAllPos = 0; indAllPos < _allPositions[positions.arg.instType].Count; indAllPos++)
                        {
                            bool isInData = false;

                            if (positions.data.Count > 0)
                            {
                                for (int indData = 0; indData < positions.data.Count; indData++)
                                {
                                    if (positions.data[indData].posMode == "hedge_mode")
                                    {
                                        if (_allPositions[positions.arg.instType][indAllPos] == positions.data[indData].instId + "_" + positions.data[indData].holdSide)
                                        {
                                            isInData = true;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (_allPositions[positions.arg.instType][indAllPos] == positions.data[indData].instId)
                                        {
                                            isInData = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (!isInData)
                            {
                                PositionOnBoard pos = new PositionOnBoard();
                                pos.PortfolioName = "BitGetFutures";
                                pos.SecurityNameCode = _allPositions[positions.arg.instType][indAllPos];
                                pos.ValueCurrent = 0;
                                pos.ValueBlocked = 0;

                                portfolio.SetNewPosition(pos);

                                _allPositions[positions.arg.instType].RemoveAt(indAllPos);
                                indAllPos--;
                            }
                        }
                    }
                }
            }
            else
            {
                SendLogMessage("BITGET ERROR. NO POSITIONS IN REQUEST.", LogMessageType.Error);
            }
            _portfolioIsStarted = true;

            PortfolioEvent(new List<Portfolio> { portfolio });
        }

        private void UpdateAccount(string message)
        {
            try
            {
                ResponseWebSocketMessageAction<List<ResponseWebSocketAccount>> assets = JsonConvert.DeserializeAnonymousType(message, new ResponseWebSocketMessageAction<List<ResponseWebSocketAccount>>());

                if (assets.data == null ||
                    assets.data.Count == 0)
                {
                    return;
                }

                Portfolio portfolio = new Portfolio();
                portfolio.Number = "BitGetFutures";
                portfolio.ValueBegin = 1;
                portfolio.ValueCurrent = 1;


                for (int i = 0; i < assets.data.Count; i++)
                {
                    PositionOnBoard pos = new PositionOnBoard();

                    pos.PortfolioName = "BitGetFutures";
                    pos.SecurityNameCode = assets.data[i].marginCoin;
                    pos.ValueBlocked = assets.data[i].frozen.ToDecimal();
                    pos.ValueCurrent = assets.data[i].available.ToDecimal();

                    portfolio.SetNewPosition(pos);
                }

                PortfolioEvent(new List<Portfolio> { portfolio });
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.Message, LogMessageType.Error);
            }
        }

        private void UpdateOrder(string message)
        {
            try
            {
                ResponseWebSocketMessageAction<List<ResponseWebSocketOrder>> order = JsonConvert.DeserializeAnonymousType(message, new ResponseWebSocketMessageAction<List<ResponseWebSocketOrder>>());

                if (order.data == null ||
                    order.data.Count == 0)
                {
                    return;
                }

                for (int i = 0; i < order.data.Count; i++)
                {
                    ResponseWebSocketOrder item = order.data[i];

                    if (string.IsNullOrEmpty(item.orderId))
                    {
                        continue;
                    }

                    OrderStateType stateType = GetOrderState(item.status);

                    if (item.orderType.Equals("market") &&
                        stateType != OrderStateType.Done &&
                        stateType != OrderStateType.Partial)
                    {
                        continue;
                    }



                    Order newOrder = new Order();
                    newOrder.SecurityNameCode = item.instId;
                    newOrder.TimeCallBack = TimeManager.GetDateTimeFromTimeStamp(Convert.ToInt64(item.cTime));
                    int.TryParse(item.clientOId, out newOrder.NumberUser);
                    newOrder.NumberMarket = item.orderId.ToString();
                    newOrder.Side = GetSide(item.tradeSide, item.side);
                    newOrder.State = stateType;
                    newOrder.Volume = item.size.ToDecimal();
                    newOrder.Price = item.price.ToDecimal();
                    newOrder.ServerType = ServerType.BitGetFutures;
                    newOrder.PortfolioNumber = "BitGetFutures";
                    newOrder.SecurityClassCode = order.arg.instType.ToString();



                    if (item.orderType.Equals("market"))
                    {
                        newOrder.TypeOrder = OrderPriceType.Market;
                    }
                    else
                    {
                        newOrder.TypeOrder = OrderPriceType.Limit;
                    }

                    if (stateType == OrderStateType.Partial)
                    {
                        MyOrderEvent(newOrder);

                        MyTrade myTrade = new MyTrade();
                        myTrade.Time = TimeManager.GetDateTimeFromTimeStamp(Convert.ToInt64(item.fillTime));
                        myTrade.NumberOrderParent = item.orderId.ToString();
                        myTrade.NumberTrade = item.tradeId;
                        myTrade.Volume = item.baseVolume.ToDecimal();
                        myTrade.Price = item.fillPrice.ToDecimal();
                        myTrade.SecurityNameCode = item.instId;
                        myTrade.Side = GetSide(item.tradeSide, item.side);

                        MyTradeEvent(myTrade);

                        return;
                    }
                    else if (stateType == OrderStateType.Done)
                    {
                        MyOrderEvent(newOrder);

                        MyTrade myTrade = new MyTrade();
                        myTrade.Time = TimeManager.GetDateTimeFromTimeStamp(Convert.ToInt64(item.fillTime));
                        myTrade.NumberOrderParent = item.orderId.ToString();
                        myTrade.NumberTrade = item.tradeId;
                        myTrade.Volume = item.baseVolume.ToDecimal();

                        if (myTrade.Volume > 0)
                        {
                            myTrade.Price = item.fillPrice.ToDecimal();
                            myTrade.SecurityNameCode = item.instId;
                            myTrade.Side = GetSide(item.tradeSide, item.side);

                            MyTradeEvent(myTrade);
                        }

                        return;
                    }
                    else
                    {
                        MyOrderEvent(newOrder);
                    }
                }
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.Message, LogMessageType.Error);
            }
        }

        private Side GetSide(string tradeSide, string side)
        {
            if (tradeSide == "close")
            {
                return side == "buy" ? Side.Sell : Side.Buy;
            }
            return side == "buy" ? Side.Buy : Side.Sell;
        }

        private void UpdateTrade(string message)
        {
            try
            {
                ResponseWebSocketMessageAction<List<ResponseWebsocketTrade>> responseTrade = JsonConvert.DeserializeAnonymousType(message, new ResponseWebSocketMessageAction<List<ResponseWebsocketTrade>>());

                if (responseTrade == null)
                {
                    return;
                }

                if (responseTrade.data == null)
                {
                    return;
                }

                if (responseTrade.data[0] == null)
                {
                    return;
                }

                Trade trade = new Trade();
                trade.SecurityNameCode = responseTrade.arg.instId;
                trade.Price = responseTrade.data[0].price.ToDecimal();
                trade.Id = responseTrade.data[0].tradeId;

                if (trade.Id == null)
                {
                    return;
                }

                trade.Time = TimeManager.GetDateTimeFromTimeStamp(Convert.ToInt64(responseTrade.data[0].ts));
                trade.Volume = responseTrade.data[0].size.ToDecimal();
                trade.Side = responseTrade.data[0].side.Equals("buy") ? Side.Buy : Side.Sell;

                NewTradesEvent(trade);
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.Message, LogMessageType.Error);
            }
        }

        private void UpdateDepth(string message)
        {
            try
            {
                ResponseWebSocketMessageAction<List<ResponseWebSocketDepthItem>> responseDepth = JsonConvert.DeserializeAnonymousType(message, new ResponseWebSocketMessageAction<List<ResponseWebSocketDepthItem>>());

                if (responseDepth.data == null)
                {
                    return;
                }

                if (responseDepth.data[0].asks.Count == 0 && responseDepth.data[0].bids.Count == 0)
                {
                    return;
                }

                MarketDepth marketDepth = new MarketDepth();

                List<MarketDepthLevel> ascs = new List<MarketDepthLevel>();
                List<MarketDepthLevel> bids = new List<MarketDepthLevel>();

                marketDepth.SecurityNameCode = responseDepth.arg.instId;

                for (int i = 0; i < responseDepth.data[0].asks.Count; i++)
                {
                    decimal ask = responseDepth.data[0].asks[i][1].ToString().ToDecimal();
                    decimal price = responseDepth.data[0].asks[i][0].ToString().ToDecimal();

                    if (ask == 0 ||
                        price == 0)
                    {
                        continue;
                    }

                    MarketDepthLevel level = new MarketDepthLevel();
                    level.Ask = ask;
                    level.Price = price;
                    ascs.Add(level);
                }

                for (int i = 0; i < responseDepth.data[0].bids.Count; i++)
                {
                    decimal bid = responseDepth.data[0].bids[i][1].ToString().ToDecimal();
                    decimal price = responseDepth.data[0].bids[i][0].ToString().ToDecimal();

                    if (bid == 0 ||
                        price == 0)
                    {
                        continue;
                    }

                    MarketDepthLevel level = new MarketDepthLevel();
                    level.Bid = bid;
                    level.Price = price;
                    bids.Add(level);
                }

                marketDepth.Asks = ascs;
                marketDepth.Bids = bids;

                marketDepth.Time = TimeManager.GetDateTimeFromTimeStamp(Convert.ToInt64(responseDepth.data[0].ts));

                if (marketDepth.Time < _lastTimeMd)
                {
                    marketDepth.Time = _lastTimeMd;
                }
                else if (marketDepth.Time == _lastTimeMd)
                {
                    _lastTimeMd = DateTime.FromBinary(_lastTimeMd.Ticks + 1);
                    marketDepth.Time = _lastTimeMd;
                }

                _lastTimeMd = marketDepth.Time;

                MarketDepthEvent(marketDepth);
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.Message, LogMessageType.Error);
            }
        }

        private DateTime _lastTimeMd;

        public event Action<Order> MyOrderEvent;

        public event Action<MyTrade> MyTradeEvent;

        public event Action<MarketDepth> MarketDepthEvent;

        public event Action<Trade> NewTradesEvent;

        #endregion

        #region 11 Trade

        private RateGate _rateGateSendOrder = new RateGate(1, TimeSpan.FromMilliseconds(200));

        private RateGate _rateGateCancelOrder = new RateGate(1, TimeSpan.FromMilliseconds(200));

        public void SendOrder(Order order)
        {
            try
            {
                string trSide = "open";
                string posSide;

                if (_hedgeMode)
                {
                    if (order.PositionConditionType == OrderPositionConditionType.Close)
                    {
                        trSide = "close";
                        posSide = order.Side == Side.Buy ? "sell" : "buy";
                    }
                    else
                    {
                        trSide = "open";
                        posSide = order.Side == Side.Buy ? "buy" : "sell";
                    }
                }
                else
                {
                    posSide = order.Side == Side.Buy ? "buy" : "sell";
                }

                _rateGateSendOrder.WaitToProceed();

                Dictionary<string, dynamic> jsonContent = new Dictionary<string, dynamic>();

                jsonContent.Add("symbol", order.SecurityNameCode);
                jsonContent.Add("productType", order.SecurityClassCode.ToLower());
                jsonContent.Add("marginMode", _marginMode);
                jsonContent.Add("marginCoin", order.SecurityClassCode.Split('-')[0]);
                jsonContent.Add("side", posSide);
                jsonContent.Add("orderType", order.TypeOrder.ToString().ToLower());
                jsonContent.Add("price", order.Price.ToString().Replace(",", "."));
                jsonContent.Add("size", order.Volume.ToString().Replace(",", "."));
                jsonContent.Add("clientOid", order.NumberUser);

                if (_hedgeMode)
                {
                    jsonContent.Add("tradeSide", trSide);
                }

                string jsonRequest = JsonConvert.SerializeObject(jsonContent);

               /* HttpResponseMessage responseMessage = CreatePrivateQueryOrders("/api/v2/mix/order/place-order", Method.POST.ToString(), null, jsonRequest);
                string JsonResponse = responseMessage.Content.ReadAsStringAsync().Result;

                ResponseRestMessage<object> stateResponse = JsonConvert.DeserializeAnonymousType(JsonResponse, new ResponseRestMessage<object>());

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    if (stateResponse.code.Equals("00000") == true)
                    {
                        // ignore
                    }
                    else
                    {
                        CreateOrderFail(order);
                        SendLogMessage($"Code: {stateResponse.code}\n"
                            + $"Message: {stateResponse.msg}", LogMessageType.Error);
                    }
                }
                else
                {
                    CreateOrderFail(order);
                    SendLogMessage($"Http State Code: {responseMessage.StatusCode}", LogMessageType.Error);

                    if (stateResponse != null && stateResponse.code != null)
                    {
                        SendLogMessage($"Code: {stateResponse.code}\n"
                            + $"Message: {stateResponse.msg}", LogMessageType.Error);
                    }
                }*/
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.Message, LogMessageType.Error);
            }
        }

        public void CancelOrder(Order order)
        {
            try
            {
                _rateGateCancelOrder.WaitToProceed();

                Dictionary<string, string> jsonContent = new Dictionary<string, string>();

                jsonContent.Add("symbol", order.SecurityNameCode);
                jsonContent.Add("productType", order.SecurityClassCode.ToLower());
                jsonContent.Add("orderId", order.NumberMarket);

                string jsonRequest = JsonConvert.SerializeObject(jsonContent);

                /*HttpResponseMessage response = CreatePrivateQueryOrders("/api/v2/mix/order/cancel-order", Method.POST.ToString(), null, jsonRequest);
                string JsonResponse = response.Content.ReadAsStringAsync().Result;

                ResponseRestMessage<object> stateResponse = JsonConvert.DeserializeAnonymousType(JsonResponse, new ResponseRestMessage<object>());

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    if (stateResponse.code.Equals("00000") == true)
                    {
                        // ignore
                    }
                    else
                    {
                        CreateOrderFail(order);
                        SendLogMessage($"Code: {stateResponse.code}\n"
                            + $"Message: {stateResponse.msg}", LogMessageType.Error);
                    }
                }
                else
                {
                    CreateOrderFail(order);
                    SendLogMessage($"Http State Code: {response.StatusCode}", LogMessageType.Error);

                    if (stateResponse != null && stateResponse.code != null)
                    {
                        SendLogMessage($"Code: {stateResponse.code}\n"
                            + $"Message: {stateResponse.msg}", LogMessageType.Error);
                    }
                }*/
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.Message, LogMessageType.Error);
            }
        }

        public void GetAllActivOrders()
        {
            List<Order> orders = GetAllOpenOrders();

            if (orders == null)
            {
                return;
            }

            for (int i = 0; i < orders.Count; i++)
            {
                if (MyOrderEvent != null)
                {
                    MyOrderEvent(orders[i]);
                }
            }
        }

        public void GetOrderStatus(Order order)
        {
            try
            {
                /*string path = "/api/v2/mix/order/detail?symbol=" + order.SecurityNameCode + "&productType=" + order.SecurityClassCode + "&clientOid=" + order.NumberUser;

                IRestResponse responseMessage = CreatePrivateQuery(path, Method.GET, null, null);
                string json = responseMessage.Content;

                ResponseRestMessage<DataOrderStatus> stateResponse = JsonConvert.DeserializeAnonymousType(json, new ResponseRestMessage<DataOrderStatus>());

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    if (stateResponse.code.Equals("00000") == true)
                    {
                        Order newOrder = new Order();

                        OrderStateType stateType = GetOrderState(stateResponse.data.state);

                        newOrder.SecurityNameCode = stateResponse.data.symbol;
                        newOrder.SecurityClassCode = stateResponse.data.marginCoin + "-FUTURES";
                        newOrder.TimeCallBack = TimeManager.GetDateTimeFromTimeStamp(Convert.ToInt64(stateResponse.data.cTime));
                        int.TryParse(stateResponse.data.clientOid, out newOrder.NumberUser);
                        newOrder.NumberMarket = stateResponse.data.orderId.ToString();
                        newOrder.Side = stateResponse.data.side == "buy" ? Side.Buy : Side.Sell;
                        newOrder.State = stateType;
                        newOrder.Volume = stateResponse.data.size.ToDecimal();
                        newOrder.Price = stateResponse.data.price.ToDecimal();
                        newOrder.ServerType = ServerType.BitGetFutures;
                        newOrder.PortfolioNumber = "BitGetFutures";
                        newOrder.TypeOrder = stateResponse.data.orderType == "limit" ? OrderPriceType.Limit : OrderPriceType.Market;

                        if (newOrder != null
                            && MyOrderEvent != null)
                        {
                            MyOrderEvent(newOrder);
                        }

                        if (newOrder.State == OrderStateType.Done ||
                            newOrder.State == OrderStateType.Partial)
                        {
                            FindMyTradesToOrder(newOrder);
                        }
                    }
                    else
                    {
                        SendLogMessage($"Code: {stateResponse.code}\n"
                            , LogMessageType.Error);
                    }
                }*/
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.Message, LogMessageType.Error);
            }
        }

        private void FindMyTradesToOrder(Order order)
        {
            try
            {
                /*string path = $"/api/v2/mix/order/fills?symbol={order.SecurityNameCode}&productType={order.SecurityClassCode}";

                IRestResponse responseMessage = CreatePrivateQuery(path, Method.GET, null, null);
                string json = responseMessage.Content;

                RestMyTradesResponce stateResponse = JsonConvert.DeserializeAnonymousType(json, new RestMyTradesResponce());

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    if (stateResponse.code.Equals("00000") == true)
                    {
                        for (int i = 0; i < stateResponse.data.fillList.Count; i++)
                        {
                            FillList item = stateResponse.data.fillList[i];

                            MyTrade myTrade = new MyTrade();
                            myTrade.Time = TimeManager.GetDateTimeFromTimeStamp(Convert.ToInt64(item.cTime));
                            myTrade.NumberOrderParent = item.orderId.ToString();
                            myTrade.NumberTrade = item.tradeId;
                            myTrade.Volume = item.baseVolume.ToDecimal();
                            myTrade.Price = item.price.ToDecimal();
                            myTrade.SecurityNameCode = item.symbol.ToUpper();
                            myTrade.Side = item.side == "buy" ? Side.Buy : Side.Sell;

                            MyTradeEvent(myTrade);
                        }
                    }
                    else
                    {
                        SendLogMessage($"Code: {stateResponse.code}\n"
                            + $"Message: {stateResponse.msg}", LogMessageType.Error);
                    }
                }*/
            }
            catch (Exception e)
            {
                SendLogMessage(e.Message, LogMessageType.Error);
            }
        }

        public void ChangeOrderPrice(Order order, decimal newPrice)
        {

        }

        public void CancelAllOrdersToSecurity(Security security)
        {
            try
            {
                /*_rateGateCancelOrder.WaitToProceed();

                Dictionary<string, string> jsonContent = new Dictionary<string, string>();

                jsonContent.Add("symbol", security.Name);
                jsonContent.Add("productType", security.NameClass);

                string jsonRequest = JsonConvert.SerializeObject(jsonContent);

                CreatePrivateQueryOrders("/api/v2/mix/order/cancel-all-orders", Method.POST.ToString(), null, jsonRequest);*/
            }
            catch (Exception e)
            {
                SendLogMessage(e.Message, LogMessageType.Error);
            }
        }

        public void CancelAllOrders()
        {
            try
            {
                /*_rateGateCancelOrder.WaitToProceed();

                for (int i = 0; i < _listCoin.Count; i++)
                {
                    Dictionary<string, string> jsonContent = new Dictionary<string, string>();
                    jsonContent.Add("productType", _listCoin[i]);

                    string jsonRequest = JsonConvert.SerializeObject(jsonContent);

                    CreatePrivateQueryOrders("/api/v2/mix/order/cancel-all-orders", Method.POST.ToString(), null, jsonRequest);
                }*/
            }
            catch (Exception e)
            {
                SendLogMessage(e.Message, LogMessageType.Error);
            }
        }

        public List<Order> GetAllOpenOrders()
        {
            try
            {
                /*for (int i = 0; i < _listCoin.Count; i++)
                {
                    IRestResponse responseMessage = CreatePrivateQuery($"/api/v2/mix/order/orders-pending?productType={_listCoin[i]}", Method.GET, null, null);
                    string json = responseMessage.Content;

                    ResponseRestMessage<RestMessageOrders> stateResponse = JsonConvert.DeserializeAnonymousType(json, new ResponseRestMessage<RestMessageOrders>());

                    if (responseMessage.StatusCode == HttpStatusCode.OK)
                    {
                        if (stateResponse.code.Equals("00000") == true)
                        {
                            if (stateResponse.data.entrustedList == null)
                            {
                                return null;
                            }

                            List<Order> orders = new List<Order>();

                            for (int ind = 0; ind < stateResponse.data.entrustedList.Count; ind++)
                            {
                                Order curOder = ConvertRestToOrder(stateResponse.data.entrustedList[ind]);
                                orders.Add(curOder);
                            }

                            return orders;
                        }
                        else
                        {
                            SendLogMessage($"Code: {stateResponse.code}\n"
                                + $"Message: {stateResponse.msg}", LogMessageType.Error);
                        }
                    }
                }*/
                return null;
            }
            catch (Exception e)
            {
                SendLogMessage(e.Message, LogMessageType.Error);
                return null;
            }
        }

        private Order ConvertRestToOrder(EntrustedList item)
        {
            Order newOrder = new Order();

            OrderStateType stateType = GetOrderState(item.status);

            newOrder.SecurityNameCode = item.symbol;
            newOrder.SecurityClassCode = item.marginCoin + "-FUTURES";
            newOrder.TimeCallBack = TimeManager.GetDateTimeFromTimeStamp(Convert.ToInt64(item.cTime));
            int.TryParse(item.clientOid, out newOrder.NumberUser);
            newOrder.NumberMarket = item.orderId.ToString();
            newOrder.Side = item.side == "buy" ? Side.Buy : Side.Sell;
            newOrder.State = stateType;
            newOrder.Volume = item.size.ToDecimal();
            newOrder.Price = item.price.ToDecimal();
            newOrder.ServerType = ServerType.BitGetFutures;
            newOrder.PortfolioNumber = "BitGetFutures";
            newOrder.TypeOrder = item.orderType == "limit" ? OrderPriceType.Limit : OrderPriceType.Market;

            return newOrder;
        }

        private OrderStateType GetOrderState(string orderStateResponse)
        {
            OrderStateType stateType;

            switch (orderStateResponse)
            {
                case ("live"):
                    stateType = OrderStateType.Active;
                    break;
                case ("partially_filled"):
                    stateType = OrderStateType.Partial;
                    break;
                case ("filled"):
                    stateType = OrderStateType.Done;
                    break;
                case ("canceled"):
                    stateType = OrderStateType.Cancel;
                    break;
                default:
                    stateType = OrderStateType.None;
                    break;
            }

            return stateType;
        }

        private void CreateOrderFail(Order order)
        {
            order.State = OrderStateType.Fail;

            if (MyOrderEvent != null)
            {
                MyOrderEvent(order);
            }
        }

        #endregion

        #region 12 Queries

        private HttpClient _httpClient = new HttpClient();

        private HttpResponseMessage CreateAuthQuery(string path, string method, string queryString, dynamic reqData)
        {
            try
            {
                string str = QueryData(reqData);

                string url = $"{_baseUrl}{path}";
                string strFromDict = StrFromDict(reqData);
                string signature = GenerateSignature(_secretKey, strFromDict);

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-NtApi-Sig", signature);

                return _httpClient.PostAsync(url, new StringContent(str, Encoding.UTF8, "application/x-www-form-urlencoded")).Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        private string QueryData(Dictionary<string, object> reqData)
        {
            string str = "";
            foreach (var item in reqData)
            {
                if (item.Value is Dictionary<string, dynamic>)
                {
                    foreach (var item2 in (Dictionary<string, dynamic>)item.Value)
                    {
                        if (item2.Value as string == null)
                        {
                            continue;
                        }
                        if (str != "")
                        {
                            str += "&";
                        }

                        string s = item2.Value as string;
                        s = s.Replace(" ", "%20");
                        s = s.Replace(":", "%3A");
                        str += $"{item.Key}[{item2.Key}]={s}";
                    }
                    continue;
                }
                if (str != "")
                {
                    str += "&";
                }

                str += $"{item.Key}={item.Value}";
            }

            //"apiKey=0e54f1028e8ca22dbca53908dbe6efe2&cmd=getHloc&params[count]=-1&params[date_from]=15.08.2024%2000%3A00&params[date_to]=16.08.2024%2000%3A00&params[id]=TATN&params[intervalMode]=ClosedRay&params[timeframe]=1440";

            return str;
        }

        public string StrFromDict(Dictionary<string, object> dictionary)
        {
            var strings = new List<string>();

            foreach (var kvp in dictionary.OrderBy(x => x.Key))
            {
                object value = kvp.Value;

                if (value is Dictionary<string, object>)
                    value = StrFromDict((Dictionary<string, object>)value);
                else if (value is List<object>)
                    value = SimpleList((List<object>)value);
                else
                    value = value.ToString();

                strings.Add($"{kvp.Key}={value}");
            }

            return string.Join("&", strings);
        }

        private string SimpleList(List<object> rawList)
        {
            var stringValues = rawList.Select(x => $"'{x}'");
            var stringList = string.Join(", ", stringValues);
            return $"[{stringList}]";
        }

        private HttpResponseMessage CreateQuery(string path, string method, string stringData, dynamic jsonData)
        {
            try
            {
                string json = stringData;
                string contentType = "application/x-www-form-urlencoded";

                if (jsonData != null)
                {
                    json = JsonConvert.SerializeObject(jsonData);
                    contentType = "application/json";
                }

                string url = $"{_baseUrl}{path}";

                _httpClient.DefaultRequestHeaders.Clear();

                if (method.Equals("POST"))
                {
                    return _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, contentType)).Result;
                }
                else
                {
                    return _httpClient.GetAsync(url).Result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public string GenerateSignature(string key, string message, string algorithmName = "sha256")
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));

            byte[] hash;
            if (string.IsNullOrEmpty(message))
            {
                hash = hmac.ComputeHash(new byte[0]);
            }
            else
            {
                hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            }

            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private string GetSID()
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();
            data.Add("apiKey", _publicKey);
            data.Add("cmd", "getSidInfo");

            HttpResponseMessage responseMessage = CreateAuthQuery($"/api/v2/cmd/getSidInfo", "POST", null, data);
            string JsonResponse = responseMessage.Content.ReadAsStringAsync().Result;

            GetSID result = JsonConvert.DeserializeObject<GetSID>(JsonResponse);
            
            return result.SID;
        }

        #endregion

        #region 13 Log

        private void SendLogMessage(string message, LogMessageType messageType)
        {
            LogMessageEvent(message, messageType);
        }

        public event Action<string, LogMessageType> LogMessageEvent;

        #endregion
    }
}