using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using System.Collections.Generic;
using System.Threading;
using System;
using OsEngine.Logging;
using RestSharp;
using Newtonsoft.Json;
using System.IO;
using OsEngine.Market.Servers.Deribit;

namespace OsEngine.Robots.DeribitPI
{
    [Bot("DeribitPI")]
    public class DeribitPI : BotPanel
    {
        private BotTabSimple _tabPerp;
        private BotTabScreener _tabOption;
        public DeribitPIUi.NameRegime Regime = DeribitPIUi.NameRegime.Off;
        public decimal UnderlyingPrice;
        public string CurrentStrike;
        public decimal MarkPriceOption;
        public decimal Deposit;
        public int PercentOfDeposit = 0;
        public decimal SettlementSizeOption; // расчетное кол-во опционов для покупки
        public bool CheckTestServer;
        public decimal PositionOptionSize; // позиция купленных опционов
        public int CountIteration = 0;
        public int TimeToCloseOption = 0;
        public int TimeFuturesLimit = 0;
        public bool CheckBoxMarketOrder = true;
        public int TimeOptionLimit = 0;
        public int PauseBuyOption = 0;
        public int CountWorkParts = 0;
        public int RatioWorkParts = 0;
        public int OneIncreaseX = 0;
        public int OneIncreaseY = 0;
        public int TwoIncreaseX = 0;
        public int TwoIncreaseY = 0;
        public int ThreeIncreaseX = 0;
        public int ThreeIncreaseY = 0;
        private int _currentTab;
        private FlagConstruction _flagConstruction = FlagConstruction.FirstBuyOption;
        public decimal PositionFutureSize;
        public List<string> LogList = new List<string>();
        private DateTime _timeLifeOrderOption;
        private int _stepOrder = 0;
        private decimal _optionBuyVolume;
        public bool OnTradeRegime = false;
        private decimal _lastOrderPriceFuture;
        private decimal _bestBidFuture;
        private decimal _bestAskFuture;
        private decimal _futureSellOrderVolume;
        private decimal _futureSellOrderVolumeExecute;
        private decimal MarkPriceFuture;
        private string _baseCurrency;
        private string _futureOrderNumber;
        private bool _flagFutureOrder;



        public DeribitPI(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tabPerp = TabsSimple[0];
            TabCreate(BotTabType.Screener);
            _tabOption = TabsScreener[0];

            _tabOption.PositionOpeningSuccesEvent += _tabOption_PositionOpeningSuccesEvent;
            _tabPerp.PositionOpeningSuccesEvent += _tabPerp_PositionOpeningSuccesEvent;
            _tabPerp.CandleUpdateEvent += _tabPerp_CandleUpdateEvent;
            _tabPerp.BestBidAskChangeEvent += _tabPerp_BestBidAskChangeEvent;

            LoadParameters();
            StartThread();

        }

        private void MyServer_NewOrderIncomeEvent(Order obj)
        {
            if (obj.SecurityNameCode == _tabPerp.Securiti.Name)
            {
                if (obj.State == OrderStateType.Activ)
                {
                    _lastOrderPriceFuture = obj.Price;
                    _flagFutureOrder = true;
                }
                if (obj.State == OrderStateType.Done)
                {
                    _futureSellOrderVolume -= obj.VolumeExecute;
                }
                if (obj.State == OrderStateType.Cancel)
                {
                    _futureSellOrderVolume -= obj.VolumeExecute;
                    _flagConstruction = FlagConstruction.FirstSellFuture;
                }

                AddLogList($"{DateTime.Now} Sec: {obj.SecurityNameCode}, Num: {obj.NumberMarket}, State: {obj.State}, " +
                    $"Side: {obj.Side}, Price: {obj.Price}, Volume: {obj.Volume}, VolEx: {obj.VolumeExecute}");
                AddLogList($"{DateTime.Now} _OrderPrice = {_lastOrderPriceFuture}, _SellOrderVolume = {_futureSellOrderVolume}, _VolEx = {_futureSellOrderVolumeExecute}");
            }
            else
            {
                AddLogList($"{DateTime.Now} Sec: {obj.SecurityNameCode}, Num: {obj.NumberMarket}, State: {obj.State}, " +
                    $"Side: {obj.Side}, Price: {obj.Price}, Volume: {obj.Volume}, VolEx: {obj.VolumeExecute}");
            }
        }

        private void _tabPerp_BestBidAskChangeEvent(decimal bid, decimal ask)
        {
            _bestAskFuture = ask;
            _bestBidFuture = bid;

            if (!OnTradeRegime)
            {
                OnTradeRegime = true;
            }
        }

        private void _tabPerp_CandleUpdateEvent(List<Candle> candle)
        {
            _lastOrderPriceFuture = candle[candle.Count - 1].Close;
        }

        private void _tabPerp_PositionOpeningSuccesEvent(Position pos)
        {
            string str = $"{DateTime.Now}: {pos.MyTrades[0].Side} {_tabPerp.Securiti.Name}, Price: {pos.MyTrades[0].Price}, Volume: {pos.OpenOrders[0].VolumeExecute}";
            AddLogList(str);
        }

        private void _tabOption_PositionOpeningSuccesEvent(Position pos, BotTabSimple tab)
        {
            string str = $"{DateTime.Now}: {pos.MyTrades[0].Side} {tab.Securiti.Name}, Price: {pos.MyTrades[0].Price}, Volume: {pos.MyTrades[0].Volume}";
            AddLogList(str);
        }

        private void AddLogList(string str)
        {
            LogList.Add(str);
        }

        private void StartThread()
        {
            Thread worker = new Thread(StartRobot) { IsBackground = true };
            worker.Start();
        }

        private void StartRobot()
        {
            bool connectorOn = false;


            while (true)
            {
                Thread.Sleep(100);

                // получаем данные по теор цене, текущему страйку и базовой цене
                if (_tabPerp.Connector.IsReadyToTrade)
                {
                    if (CurrentStrike != null)
                    {
                        GetOptionMarkPrice();
                    }
                    else
                    {
                        CurrentStrike = _tabOption.Tabs[0].Securiti.Name;
                        GetOptionMarkPrice();
                    }

                    if (!connectorOn)
                    {
                        connectorOn = true;
                        _tabPerp.Connector.MyServer.NewOrderIncomeEvent += MyServer_NewOrderIncomeEvent;
                    }

                }

                // получаем данные по депозиту
                if (_tabPerp.Portfolio != null)
                {
                    List<PositionOnBoard> positionOnBoard = _tabPerp.Portfolio.GetPositionOnBoard();

                    if (_tabPerp.Securiti != null) // для тестов, потом убрать
                    {
                        GetFutureMarkPrice();
                    }

                    for (int i = 0; i < positionOnBoard.Count; i++)
                    {
                        if (positionOnBoard[i].SecurityNameCode == _baseCurrency)
                        {
                            Deposit = positionOnBoard[i].ValueCurrent;
                        }
                    }
                }

                // получаем данные по портфелю опционов
                PositionOptionSize = 0;
                for (int i = 0; i < _tabOption.Tabs.Count; i++)
                {
                    if (_tabOption.Tabs[i].Portfolio != null)
                    {
                        if (_tabOption.Tabs[i].PositionsOnBoard != null)
                        {
                            for (int j = 0; j < _tabOption.Tabs[i].PositionsOnBoard.Count; j++)
                            {
                                PositionOptionSize += _tabOption.Tabs[i].PositionsOnBoard[j].ValueCurrent;
                            }
                        }
                    }
                }

                // получаем данные по портфелю фьючерсов
                if (_tabPerp.PositionsOnBoard != null && _tabPerp.PositionsOnBoard.Count > 0)
                {
                    PositionFutureSize = _tabPerp.PositionsOnBoard[0].ValueCurrent;
                }

                // режим - выключено
                if (Regime == DeribitPIUi.NameRegime.Off)
                {
                    if (_flagConstruction == FlagConstruction.FirstBuyOption) // если есть выставленный ордер на покупку опциона, то отменяем его
                    {
                        CancelOptionOrder();

                        _stepOrder = 0;
                    }

                    // расчитываем кол-во опционов для покупки
                    if (PercentOfDeposit != 0 && Deposit != 0 && MarkPriceOption != 0)
                    {
                        SettlementSizeOption = Math.Floor(Deposit * ((decimal)PercentOfDeposit / 100) / MarkPriceOption);
                    }
                    else
                    {
                        SettlementSizeOption = 0;
                    }
                    _flagConstruction = FlagConstruction.FirstBuyOption;
                }

                // режим - набор конструкции
                if (Regime == DeribitPIUi.NameRegime.AssemblyConstruction)
                {
                    AssemblyConstruction();
                }
            }
        }

        private DateTime _futureTimeOrderLimit = DateTime.Now;
        private void AssemblyConstruction()
        {
            // выставляем начальный ордер для котирования опциона
            if (SettlementSizeOption != 0 &&
                SettlementSizeOption - PositionOptionSize > 0 &&
                _flagConstruction == FlagConstruction.FirstBuyOption)
            {
                for (int i = 0; i < _tabOption.Tabs.Count; i++)
                {
                    if (_tabOption.Tabs[i].Securiti.Name == CurrentStrike)
                    {
                        _currentTab = i;
                        DeribitServerRealization._postOnly = "false";
                        _tabOption.Tabs[i].BuyAtLimit(GetOptionVolume(), GetOptionPriceLimit());
                        _flagConstruction = FlagConstruction.QuoteBuyOption;
                        _timeLifeOrderOption = DateTime.Now;
                        _futureSellOrderVolume = GetFutureVolume();
                        break;
                    }
                }
            }

            if (_flagConstruction == FlagConstruction.QuoteBuyOption) // котируем опцион
            {
                // если опцион купился, меняем флаг и выходим отсюда
                if (_tabOption.Tabs[_currentTab].PositionsLast.State == PositionStateType.Open)
                {
                    _flagConstruction = FlagConstruction.FirstSellFuture;
                    _stepOrder = 0;

                    _futureTimeOrderLimit = DateTime.Now;
                    return;
                }

                // если сменился текущий страйк отменяем ордер и начинаем все заново
                if (_tabOption.Tabs[_currentTab].Securiti.Name != CurrentStrike)
                {
                    CancelOptionOrder();
                    AddLogList("Отмена ордера по смене страйка");
                    GetOptionMarkPrice();
                    return;
                }

                // проверяем время жизни опционного ордера (в секундах) и высталяем опцион дороже
                if (_timeLifeOrderOption.AddSeconds(TimeOptionLimit) <= DateTime.Now)
                {
                    // надо проверить выставлен ли ордер
                    CancelOptionOrder();
                    AddLogList("Отмена ордера по истечению времени");
                    _stepOrder += 1;
                    return;
                }

                List<Position> positionsLong = _tabOption.Tabs[_currentTab].PositionOpenLong;

                if (positionsLong != null &&
                    positionsLong.Count != 0)
                {
                    if (_tabOption.Tabs[_currentTab].PositionsLast.State == PositionStateType.Opening)
                    {
                        decimal stepPrice = 0.0005m; // взависимости от цены опциона, меняется шаг цены

                        if (MarkPriceOption < 0.005m)
                        {
                            stepPrice = 0.0001m;
                        }
                        if (_stepOrder == 0)
                        {
                            if (Math.Abs(MarkPriceOption - _tabOption.Tabs[_currentTab].PositionsLast.EntryPrice) > stepPrice / 2)
                            {
                                CancelOptionOrder();
                                AddLogList($"Отмена ордера по изменению теор цены = {MarkPriceOption}");
                            }
                        }
                        else if (_stepOrder > 0)
                        {
                            if (MarkPriceOption - _tabOption.Tabs[_currentTab].PositionsLast.EntryPrice >= stepPrice)
                            {
                                CancelOptionOrder();
                                AddLogList($"Отмена ордера по изменению теор цены = {MarkPriceOption}");
                                _stepOrder = 0;
                            }
                        }
                    }
                }
            }

            if (_flagConstruction == FlagConstruction.FirstSellFuture)
            {
                if (CheckBoxMarketOrder) // если чекбокс на маркетные заявки стоит
                {
                    _tabPerp.SellAtMarket(GetFutureVolume());
                    _flagConstruction = FlagConstruction.QuoteSellFuture;
                    return;
                }
                else if (_futureTimeOrderLimit.AddSeconds(TimeFuturesLimit) <= DateTime.Now)
                {
                    _tabPerp.SellAtMarket(_futureSellOrderVolume);
                    _flagConstruction = FlagConstruction.QuoteSellFuture;
                    return;
                }
                else
                {
                    DeribitServerRealization._postOnly = "true";
                    //_futureSellOrderVolumeExecute = 0;
                    _tabPerp.SellAtLimit(_futureSellOrderVolume, GetFuturePrice());
                    _flagConstruction = FlagConstruction.QuoteSellFuture;

                    return;
                }
            }

            if (_flagConstruction == FlagConstruction.QuoteSellFuture)
            {
                if (_tabPerp.PositionsLast != null)
                {
                    if (_futureSellOrderVolume == 0)
                    {
                        _flagConstruction = FlagConstruction.FirstBuyOption;

                        if (PositionOptionSize == SettlementSizeOption)
                        {
                            Regime = DeribitPIUi.NameRegime.TradeFutures;
                        }
                        return;
                    }
                    if (_flagFutureOrder)
                    {
                        if (_lastOrderPriceFuture != _bestAskFuture)
                        {
                            CancelFutureOrder();
                        }
                    }

                    // если лимитная заяка по фьючерсу не исполнилась, делаем заявку по маркету
                    if (_futureTimeOrderLimit.AddSeconds(TimeFuturesLimit) <= DateTime.Now)
                    {
                        CancelFutureOrder();
                        return;
                    }
                }
            }
        }

        private decimal GetFuturePrice()
        {
            decimal price = _bestAskFuture;

            if (_bestAskFuture - _bestBidFuture > _tabPerp.Securiti.PriceStep)
            {
                price = _bestAskFuture - _tabPerp.Securiti.PriceStep;
            }

            return price;
        }

        private decimal GetOptionVolume()
        {

            _optionBuyVolume = Math.Round(SettlementSizeOption / CountIteration, MidpointRounding.AwayFromZero);
            if (_optionBuyVolume < 1)
            {
                _optionBuyVolume = 1;
            }

            if (_optionBuyVolume > SettlementSizeOption - PositionOptionSize)
            {
                _optionBuyVolume = SettlementSizeOption - PositionOptionSize;
            }

            return _optionBuyVolume;
        }

        private decimal GetFutureVolume()
        {
            decimal amount = Math.Round(_optionBuyVolume / 2m * GetFutureMarkPrice(), MidpointRounding.AwayFromZero);

            return amount;
        }

        private enum FlagConstruction
        {
            None,
            FirstBuyOption,
            QuoteBuyOption,
            FirstSellFuture,
            QuoteSellFuture
        }

        private void CancelOptionOrder()
        {
            _flagConstruction = FlagConstruction.FirstBuyOption;

            if (_currentTab == 0)
            {
                return;
            }
            List<Position> positionsLong = _tabOption.Tabs[_currentTab].PositionOpenLong;

            if (positionsLong != null &&
                positionsLong.Count != 0)
            {
                if (_tabOption.Tabs[_currentTab].PositionsLast.State == PositionStateType.Opening ||
                    _tabOption.Tabs[_currentTab].PositionsLast.State == PositionStateType.Open)
                {
                    _tabOption.Tabs[_currentTab].CloseAllOrderToPosition(_tabOption.Tabs[_currentTab].PositionsLast);
                }
            }
        }

        private void CancelFutureOrder()
        {
            if (_tabPerp.PositionsLast != null)
            {
                if (_tabPerp.PositionsLast.State == PositionStateType.Opening ||
                    _tabPerp.PositionsLast.State == PositionStateType.Open)
                {
                    _tabPerp.CloseOrder(_tabPerp.PositionsLast.OpenOrders[0]);
                    _flagFutureOrder = false;
                }
            }
        }

        private decimal GetOptionPriceLimit()
        {
            decimal stepPrice = 0.0005m; // взависимости от цены опциона, меняется шаг цены

            if (MarkPriceOption < 0.005m)
            {
                stepPrice = 0.0001m;
            }

            decimal priceOption = MarkPriceOption;

            if (MarkPriceOption % stepPrice != 0)
            {
                priceOption = (Math.Round(MarkPriceOption / stepPrice, MidpointRounding.AwayFromZero) * stepPrice);
            }

            if (_stepOrder > 0)
            {
                priceOption = _tabOption.Tabs[_currentTab].PositionsLast.EntryPrice + stepPrice;
            }

            return priceOption;
        }


        private void GetOptionMarkPrice()
        {
            try
            {
                IRestResponse responseMessage = RequestRest($"/api/v2/public/get_book_summary_by_instrument?instrument_name={CurrentStrike}");

                if (responseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    ResponseMessageOptionMarkPrice response = JsonConvert.DeserializeObject<ResponseMessageOptionMarkPrice>(responseMessage.Content);
                    MarkPriceOption = Math.Round(response.result[0].mark_price, 4);
                    UnderlyingPrice = response.result[0].underlying_price;
                    CurrentStrike = GetCurrentStrike();
                }
                else
                {
                    SendNewLogMessage($"GetOptionMarkPrice - Http State Code: {responseMessage.StatusCode}, {responseMessage.Content}", LogMessageType.Error);
                }
            }
            catch (Exception exception)
            {
                SendNewLogMessage(exception.ToString(), LogMessageType.Error);
            }
        }

        private decimal GetFutureMarkPrice()
        {
            decimal markPrice = 0;
            try
            {
                IRestResponse responseMessage = RequestRest($"/api/v2/public/get_book_summary_by_instrument?instrument_name={_tabPerp.Securiti.Name}");

                if (responseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    ResponseMessageOptionMarkPrice response = JsonConvert.DeserializeObject<ResponseMessageOptionMarkPrice>(responseMessage.Content);
                    markPrice = Math.Round(response.result[0].mark_price, 4);
                    _baseCurrency = response.result[0].base_currency; // для тестов, потом убрать
                }
                else
                {
                    SendNewLogMessage($"GetOptionMarkPrice - Http State Code: {responseMessage.StatusCode}, {responseMessage.Content}", LogMessageType.Error);
                }
            }
            catch (Exception exception)
            {
                SendNewLogMessage(exception.ToString(), LogMessageType.Error);
            }

            return markPrice;
        }

        private IRestResponse RequestRest(string stringRequest)
        {
            try
            {
                string typeServer = "https://www.deribit.com";

                if (CheckTestServer)
                {
                    typeServer = "https://test.deribit.com";
                }

                string url = $"{typeServer}{stringRequest}";
                RestClient client = new RestClient(url);
                RestRequest request = new RestRequest(Method.GET);
                IRestResponse responseMessage = client.Execute(request);

                return responseMessage;
            }
            catch (Exception exception)
            {
                SendNewLogMessage(exception.ToString(), LogMessageType.Error);
                return null;
            }
        }

        public class ResponseMessageOptionMarkPrice
        {
            public List<Result> result { get; set; }

            public class Result
            {
                public decimal mark_price { get; set; }
                public decimal underlying_price { get; set; }
                public string base_currency { get; set; }
            }
        }

        private string GetCurrentStrike() // вычисляем ближайший страйк
        {
            string currStrike = null;

            for (int i = 0; i < _tabOption.Tabs.Count - 1; i++)
            {
                int strike1 = int.Parse(_tabOption.Tabs[i].Securiti.Name.Split('-')[2]);
                int strike2 = int.Parse(_tabOption.Tabs[i + 1].Securiti.Name.Split('-')[2]);

                if (UnderlyingPrice > strike1 && UnderlyingPrice < strike2)
                {
                    if (UnderlyingPrice - strike1 >= strike2 - UnderlyingPrice)
                    {
                        currStrike = _tabOption.Tabs[i + 1].Securiti.Name;
                    }
                    else
                    {
                        currStrike = _tabOption.Tabs[i].Securiti.Name;
                    }
                    break;
                }
            }
            return currStrike;
        }

        private void LoadParameters()
        {
            if (!File.Exists(@"Engine\" + NameStrategyUniq + @"Parameters.txt"))
            {
                return;
            }
            try
            {
                using (StreamReader reader = new StreamReader(@"Engine\" + NameStrategyUniq + @"Parameters.txt"))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            string[] split = line.Split('^');
                            if (int.TryParse(split[1], out int num))
                            {
                                SetField(split[0], num);
                            }
                            else if (split[1] == "True")
                            {
                                SetField(split[0], true);
                            }
                            else if (split[1] == "False")
                            {
                                SetField(split[0], false);
                            }
                            else
                            {
                                //SetField(split[0], split[1]); // заготовка (нерабочая) под загрузку режима работы
                            }
                        }
                    }
                    reader.Close();
                }
            }
            catch (Exception e)
            {
                SendNewLogMessage(e.ToString(), Logging.LogMessageType.Error);
            }
        }

        public void SetField(string name, object value)
        {
            var field = typeof(DeribitPI).GetField(name);
            field.SetValue(this, value);
        }

        public void SaveParameters()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Engine\" + NameStrategyUniq + @"Parameters.txt", false)
                    )
                {
                    writer.WriteLine("Regime^" + Regime);
                    writer.WriteLine("PercentOfDeposit^" + PercentOfDeposit);
                    writer.WriteLine("CountIteration^" + CountIteration);
                    writer.WriteLine("TimeToCloseOption^" + TimeToCloseOption);
                    writer.WriteLine("TimeFuturesLimit^" + TimeFuturesLimit);
                    writer.WriteLine("CheckBoxMarketOrder^" + CheckBoxMarketOrder);
                    writer.WriteLine("TimeOptionLimit^" + TimeOptionLimit);
                    writer.WriteLine("PauseBuyOption^" + PauseBuyOption);
                    writer.WriteLine("CountWorkParts^" + CountWorkParts);
                    writer.WriteLine("RatioWorkParts^" + RatioWorkParts);
                    writer.WriteLine("OneIncreaseX^" + OneIncreaseX);
                    writer.WriteLine("OneIncreaseY^" + OneIncreaseY);
                    writer.WriteLine("TwoIncreaseX^" + TwoIncreaseX);
                    writer.WriteLine("TwoIncreaseY^" + TwoIncreaseY);
                    writer.WriteLine("ThreeIncreaseX^" + ThreeIncreaseX);
                    writer.WriteLine("ThreeIncreaseY^" + ThreeIncreaseY);
                    writer.Close();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }

        public override string GetNameStrategyType()
        {
            return "DeribitPI";
        }

        public override void ShowIndividualSettingsDialog()
        {
            DeribitPIUi ui = new DeribitPIUi(this);
            ui.ShowDialog();
        }
    }
}
