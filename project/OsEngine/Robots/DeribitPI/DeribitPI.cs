using Newtonsoft.Json;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market.Servers.Deribit;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;

namespace OsEngine.Robots.DeribitPI
{
    [Bot("DeribitPI")]
    public class DeribitPI : BotPanel
    {       
        public DeribitPI(string name, StartProgram startProgram) : base(name, startProgram)
        {
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
            TabCreate(BotTabType.Simple);
            _tabPerp = TabsSimple[0];
            TabCreate(BotTabType.Screener);
            _tabOption = TabsScreener[0];

            _tabOption.PositionOpeningSuccesEvent += _tabOption_PositionOpeningSuccesEvent;
            _tabPerp.PositionOpeningSuccesEvent += _tabPerp_PositionOpeningSuccesEvent;
            _tabPerp.CandleUpdateEvent += _tabPerp_CandleUpdateEvent;
                        
            LoadParameters();
            LoadLog();
            StartThread();                        
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

        #region Variables

        private BotTabSimple _tabPerp;
        private BotTabScreener _tabOption;
        public DeribitPIUi.NameRegime Regime = DeribitPIUi.NameRegime.Off;
        public decimal UnderlyingPrice;
        public string CurrentStrike;
        public decimal MarkPriceOption;
        public decimal Deposit;
        public decimal PercentOfDeposit = 0;
        public decimal SettlementSizeOption; // расчетное кол-во опционов для покупки
        public decimal PositionOptionSize; // позиция купленных опционов
        public int CountIteration = 0;
        public int TimeToCloseOption = 0;
        public int TimeFuturesLimit = 0;
        public bool CheckBoxMarketOrder = true;
        public int TimeOptionLimit = 0;
        public int TimeLifeAssemblyConstruction = 0;
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
        private decimal _bestBidPriceFuture;
        private decimal _bestAskPriceFuture;
        private decimal _futureSellOrderVolume;
        private decimal _futureSellOrderVolumeExecute;
        private decimal MarkPriceFuture;
        private string _baseCurrency;
        private string _futureOrderNumber;
        private bool _flagFutureOrder;
        private decimal _bestAskVolumeFuture;
        private decimal _bestBidVolumeFuture;
        private string _typeServer;
        private string _firstFixedStrike;
        private bool _flagOptionChangeStrike = false;
        private DateTime _timeLifeAssemblyConsruction;
        public ConcurrentQueue<string> ListLog = new ConcurrentQueue<string>();
        private DateTime _futureTimeOrderLimit = DateTime.Now;
        private bool _flagTimerAssemblyConstruction = false;
        private List<ListOrders> _ordersIntradayFuture = new List<ListOrders>();
        private bool _flagTradeIntraday = false;
        private List<ListOrders> _ordersStopProfit = new List<ListOrders>();
        private decimal _workPartVolumeIntraday = 0;

        #endregion

        #region Events

        private void MyServer_NewMarketDepthEvent(MarketDepth obj)
        {
            if (obj.SecurityNameCode == _tabPerp.Securiti.Name)
            {
                _bestAskPriceFuture = obj.Asks[0].Price;
                _bestAskVolumeFuture = obj.Asks[0].Ask;
                _bestBidPriceFuture = obj.Bids[0].Price;
                _bestBidVolumeFuture = obj.Bids[0].Bid;

                if (!OnTradeRegime)
                {
                    OnTradeRegime = true;
                }
            }
        }

        private void MyServer_NewOrderIncomeEvent(Order obj)
        {
            if (obj.SecurityNameCode == _tabPerp.Securiti.Name)
            {
                if (Regime == DeribitPIUi.NameRegime.AssemblyConstruction)
                {
                    AddLogList(" OrderEvent: " + obj.SecurityNameCode + ", Num: " + obj.NumberMarket + ", State: " + obj.State + ", " +
                    "Side: " + obj.Side + ", Price: " + obj.Price + ", Volume: " + obj.Volume + ", VolEx: " + obj.VolumeExecute);

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
                }

                if (Regime == DeribitPIUi.NameRegime.TradeFutures)
                {
                    for (int i = 0; i < _ordersIntradayFuture.Count; i++)
                    {
                        if (_ordersIntradayFuture[i].PriceOrder == obj.Price && _ordersIntradayFuture[i].NumberMarket == "")
                        {
                            if (obj.State == OrderStateType.Activ ||
                            obj.State == OrderStateType.Done)
                            {
                                _ordersIntradayFuture[i].NumberMarket = obj.NumberMarket;
                                AddLogList($"Number = {_ordersIntradayFuture[i].NumberMarket}, PriceOrder = {_ordersIntradayFuture[i].PriceOrder}, SideOrder = {_ordersIntradayFuture[i].SideOrder}, " +
                                    $"VolumeOrder = {_ordersIntradayFuture[i].VolumeOrder}, ExecuteVolume = {_ordersIntradayFuture[i].ExecuteVolume}, " +
                                    $"PriceCounterOrder = {_ordersIntradayFuture[i].PriceCounterOrder}, {_ordersIntradayFuture[i].OrderType}");
                            }
                                
                        }
                    }

                    if (obj.VolumeExecute > 0)
                    {
                        AddLogList("OrderEvent: " + obj.SecurityNameCode + ", Num: " + obj.NumberMarket + ", State: " + obj.State + ", " +
                                "Side: " + obj.Side + ", Price: " + obj.Price + ", Volume: " + obj.Volume + ", VolEx: " + obj.VolumeExecute);
                        

                        if (obj.State == OrderStateType.Activ ||
                            obj.State == OrderStateType.Done)
                        {
                            for (int i = 0; i < _ordersIntradayFuture.Count; i++)
                            {
                                if (_ordersIntradayFuture[i].NumberMarket == obj.NumberMarket) // находим ордер по которому пришел ивент
                                {
                                    _ordersIntradayFuture[i].ExecuteVolume = obj.VolumeExecute;                               
                                    
                                    if (_ordersIntradayFuture[i].ExecuteVolume > 0)
                                    {
                                        bool tryCounterOrder = false;
                                        
                                        for (int j = 0; j < _ordersIntradayFuture.Count; j++) // находим противоположный ордер
                                        {
                                            if (_ordersIntradayFuture[j].PriceOrder == _ordersIntradayFuture[i].PriceCounterOrder) // если он есть
                                            {
                                                tryCounterOrder = true;
                                                if (_ordersIntradayFuture[j].VolumeOrder != obj.VolumeExecute) // если объемы ордера и противоположного ордера не равны
                                                {
                                                    for (int closeOrder = 0; closeOrder < _tabPerp.PositionsOpenAll.Count; closeOrder++)
                                                    {
                                                        if (_ordersIntradayFuture[j].NumberMarket == _tabPerp.PositionsOpenAll[closeOrder].OpenOrders[0].NumberMarket)
                                                        {
                                                            _tabPerp.CloseOrder(_tabPerp.PositionsOpenAll[closeOrder].OpenOrders[0]); // удаляем противоположный ордер

                                                            if (_ordersIntradayFuture[i].SideOrder == Side.Buy) // и выставляем новый противоположный ордер
                                                            {
                                                                _tabPerp.SellAtLimit(_ordersIntradayFuture[i].VolumeOrder - _ordersIntradayFuture[i].ExecuteVolume + _ordersIntradayFuture[j].ExecuteVolume, _ordersIntradayFuture[i].PriceCounterOrder);
                                                            }
                                                            else
                                                            {
                                                                _tabPerp.BuyAtLimit(_ordersIntradayFuture[i].VolumeOrder - _ordersIntradayFuture[i].ExecuteVolume + _ordersIntradayFuture[j].ExecuteVolume, _ordersIntradayFuture[i].PriceCounterOrder);
                                                            }
                                                            _ordersIntradayFuture[i].VolumeOrder = _ordersIntradayFuture[i].VolumeOrder - _ordersIntradayFuture[i].ExecuteVolume + _ordersIntradayFuture[j].ExecuteVolume;
                                                            _ordersIntradayFuture[i].NumberMarket = "";
                                                            AddLogList($"Удаляем противоположный ордер {_tabPerp.PositionsOpenAll[closeOrder].OpenOrders[0].NumberMarket} " +
                                                                $"с ценой {_tabPerp.PositionsOpenAll[closeOrder].OpenOrders[0].PriceReal} " +
                                                                $"и выставляем новый противоположный ордер Price = {_ordersIntradayFuture[i].PriceCounterOrder}, Vol = {_ordersIntradayFuture[i].VolumeOrder - _ordersIntradayFuture[i].ExecuteVolume + _ordersIntradayFuture[j].ExecuteVolume}");
                                                        }
                                                    }

                                                }
                                                //_ordersIntradayFuture[j].VolumeOrder = obj.VolumeExecute;
                                                break;
                                            }
                                        }
                                                                                
                                        if (!tryCounterOrder) // если противоположного ордера нет, то выставляем этот ордер
                                        {
                                            if (_ordersIntradayFuture[i].SideOrder == Side.Buy)
                                            {
                                                _tabPerp.SellAtLimit(_ordersIntradayFuture[i].ExecuteVolume, _ordersIntradayFuture[i].PriceCounterOrder);
                                            }
                                            else
                                            {
                                                _tabPerp.BuyAtLimit(_ordersIntradayFuture[i].ExecuteVolume, _ordersIntradayFuture[i].PriceCounterOrder);
                                            }
                                            AddLogList($"Противоположного ордера нет, выставляем этот ордер Price = {_ordersIntradayFuture[i].PriceCounterOrder}, Vol = {_ordersIntradayFuture[i].ExecuteVolume}");

                                            // заносим ордер в массив
                                            _ordersIntradayFuture.Add(new ListOrders(_ordersIntradayFuture[i].PriceCounterOrder,
                                                _ordersIntradayFuture[i].SideOrder == Side.Buy ? Side.Sell : Side.Buy,
                                                obj.VolumeExecute,
                                                0,
                                                _ordersIntradayFuture[i].PriceOrder,
                                                0,
                                                _ordersIntradayFuture[i].OrderType == OrdersType.MainOrder ? OrdersType.CounterOrder : OrdersType.MainOrder, 
                                                ""));
                                        }
                                    }
                                    if (_ordersIntradayFuture[i].ExecuteVolume == _ordersIntradayFuture[i].VolumeOrder) // если объем выполненный равен объему ордера, то удаляем ордер из массива
                                    {
                                        _ordersIntradayFuture.RemoveAt(i);
                                        AddLogList("Объем выполненный равен объему ордера, удаляем ордер из массива");
                                    }
                                    break;
                                }
                            }
                        }  
                        foreach (ListOrders item in _ordersIntradayFuture)
                        { 
                            AddLogList($"Number = {item.NumberMarket}, PriceOrder = {item.PriceOrder}, SideOrder = {item.SideOrder}, VolumeOrder = {item.VolumeOrder}, ExecuteVolume = {item.ExecuteVolume}, PriceCounterOrder = {item.PriceCounterOrder}, {item.OrderType}");
                        }
                    }
                    if (obj.State == OrderStateType.Cancel)
                    {
                        AddLogList($"Ордер отменен - {obj.NumberMarket}, Price = {obj.PriceReal}, Volume = {obj.Volume}, ExVol = {obj.VolumeExecute}");
                    }
                }   
            }
            else
            {
                AddLogList("OrderEvent: " + obj.SecurityNameCode + ", Num: " + obj.NumberMarket + ", State: " + obj.State + ", " +
                    "Side: " + obj.Side + ", Price: " + obj.Price + ", Volume: " + obj.Volume + ", VolEx: " + obj.VolumeExecute);

                if (obj.SecurityNameCode == _tabOption.Tabs[_currentTab].Securiti.Name)
                {
                    if (obj.State == OrderStateType.Activ)
                    {
                        if (obj.VolumeExecute != obj.Volume && obj.VolumeExecute > 0) // если не полностью исполнился ордер по опциону
                        {
                            CancelOptionOrder(); 
                            _futureSellOrderVolume = Math.Round(_futureSellOrderVolume / obj.Volume * obj.VolumeExecute);
                            _flagConstruction = FlagConstruction.FirstSellFuture;
                            _stepOrder = 0;
                            _futureTimeOrderLimit = DateTime.Now;
                        }                      
                    }
                }

                if (obj.State == OrderStateType.Cancel)
                {
                    _flagConstruction = FlagConstruction.FirstBuyOption;                   
                }
            }
        }

        private void _tabPerp_CandleUpdateEvent(List<Candle> candle)
        {
            if (candle.Count > 0)
            {
                _lastOrderPriceFuture = candle[candle.Count - 1].Close;
            }            
        }

        private void _tabPerp_PositionOpeningSuccesEvent(Position pos)
        {
            string str = pos.MyTrades[0].Side + " " + _tabPerp.Securiti.Name + ", Price: " + pos.MyTrades[0].Price+ ", Volume: "+ pos.MyTrades[0].Volume;
            AddLogList(str);
        }

        private void _tabOption_PositionOpeningSuccesEvent(Position pos, BotTabSimple tab)
        {
            string str = pos.MyTrades[0].Side + " " + tab.Securiti.Name + ", Price: " + pos.MyTrades[0].Price + ", Volume: " + pos.MyTrades[0].Volume;
            AddLogList(str);

            _flagConstruction = FlagConstruction.FirstSellFuture;
            _stepOrder = 0;
            _futureTimeOrderLimit = DateTime.Now;
        }

        #endregion               

        #region Thread robot
        private void StartRobot()
        {
            bool connectorOn = false;
            bool checkParameters = false;

            while (true)
            {
                Thread.Sleep(100);

                if (_tabOption.Tabs == null || _tabOption.Tabs.Count == 0)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                if (_tabOption.Tabs[0].Securiti == null)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                if (_tabPerp.Connector.MyServer.ServerStatus == Market.Servers.ServerConnectStatus.Disconnect)
                {
                    CurrentStrike = null;
                    Thread.Sleep(1000);
                    continue;
                }     
                
                if (_tabPerp.Connector.MyServer.ServerStatus == Market.Servers.ServerConnectStatus.Connect)
                {
                    if (DeribitServerRealization.testServer)
                    {
                        _typeServer = "https://test.deribit.com";
                    }
                    else
                    {
                        _typeServer = "https://www.deribit.com";
                    }
                }

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
                        _tabPerp.Connector.MyServer.NewMarketDepthEvent += MyServer_NewMarketDepthEvent;
                        AddLogList("Робот запущен");
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
                                _firstFixedStrike = _tabOption.Tabs[i].Securiti.Name;
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
                    _flagTimerAssemblyConstruction = false;
                    _flagOptionChangeStrike = false;
                    checkParameters = false;

                    if (_flagConstruction == FlagConstruction.QuoteBuyOption) // если есть выставленный ордер на покупку опциона, то отменяем его
                    {
                        CancelOptionOrder();
                        AddLogList("Отмена ордера - выбран режим Выключено");
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

                    if (_flagTradeIntraday == true)
                    {
                        for (int i = 0; i < _tabPerp.PositionsOpenAll.Count; i++)
                        {
                            if (_tabPerp.PositionsOpenAll[i].OpenOrders[0].State == OrderStateType.Activ)
                            {
                                _tabPerp.CloseOrder(_tabPerp.PositionsOpenAll[i].OpenOrders[0]);
                            }
                        }
                        _flagTradeIntraday = false;
                        _ordersIntradayFuture.Clear();
                        AddLogList("Заявки по фьючерсу сняты, массив очищен");
                    }
                    
                }

                // режим - набор конструкции
                if (Regime == DeribitPIUi.NameRegime.AssemblyConstruction)
                {
                    if (checkParameters == false) // проверка введены ли все необходимые настройки
                    {
                        checkParameters = CheckParameters();
                        continue;
                    }

                    if (!_flagTimerAssemblyConstruction)
                    {
                        _timeLifeAssemblyConsruction = DateTime.Now;
                        _flagTimerAssemblyConstruction = true;
                        AddLogList("Время начала набора конструкции");
                    }
                    AssemblyConstruction();
                }

                if (Regime == DeribitPIUi.NameRegime.TradeFutures)
                {
                    if (checkParameters == false) // проверка введены ли все необходимые настройки и набрана ли конструкция
                    {
                        checkParameters = CheckParameters();
                        if (checkParameters == false)
                        {
                            continue;
                        }

                        if (PositionFutureSize == 0 || PositionOptionSize == 0)
                        {
                            checkParameters = false;
                            Regime = DeribitPIUi.NameRegime.Off;
                            MessageBox.Show("Невозможно запустить торговлю фьючерсами, нет набранной конструкции", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            continue;
                        }                        
                    }

                    //TradeFutures();
                }

                if(Regime == DeribitPIUi.NameRegime.DisassemblyConstruction)
                {
                    if (checkParameters == false) // проверка введены ли все необходимые настройки
                    {
                        checkParameters = CheckParameters();                        
                        continue;
                    }
                }
            }
        }

        private bool CheckParameters()
        {
            bool check = true;

            if (PercentOfDeposit == null || PercentOfDeposit == 0)
            {
                check = false;
            }
            if (CountIteration == null || CountIteration == 0)
            {
                check = false;
            }
            if (TimeToCloseOption == null || TimeToCloseOption == 0)
            {
                check = false;
            }
            if (CountWorkParts == null || CountWorkParts == 0)
            {
                check = false;
            }
            if (RatioWorkParts == null || RatioWorkParts == 0)
            {
                check = false;
            }
            if (check == false)
            {
                Regime = DeribitPIUi.NameRegime.Off;
                MessageBox.Show("Для начала работы необходимо заполнить параметры. Необходимые к заполнению параметры помечены звездочкой (*)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);                
            }

            return check;
        }

        #endregion



        #region TradeFutures
        private void TradeFutures()
        {
            if (!_flagTradeIntraday)
            {
                if (_ordersIntradayFuture.Count == 0)
                {
                    decimal countRightBreakEven = 0;
                    decimal countLeftBreakEven = 0;
                    decimal volumeCall = 0;
                    decimal sum1 = 0;
                    decimal sum2 = 0;
                    decimal averagePriceFuture = 0;

                    for (int i = 0; i < _tabPerp.PositionsOpenAll.Count; i++)
                    {
                        decimal priceFuture = _tabPerp.PositionsOpenAll[i].EntryPrice;
                        decimal volumeFuture = _tabPerp.PositionsOpenAll[i].OpenVolume;

                        sum1 += priceFuture * volumeFuture;
                        sum2 += volumeFuture;
                    }
                    if (sum2 > 0 && sum1 > 0)
                    {
                        averagePriceFuture = Math.Round(sum1 / sum2, 1, MidpointRounding.AwayFromZero);
                    }

                    for (int i = 0; i < _tabOption.Tabs.Count; i++)
                    {
                        if (_tabOption.Tabs[i].PositionsOpenAll.Count > 0)
                        {
                            int strike = Convert.ToInt32(_tabOption.Tabs[i].PositionsOpenAll[0].SecurityName.Split('-')[2]);

                            for (int j = 0; j < _tabOption.Tabs[i].PositionsOpenAll.Count; j++)
                            {
                                decimal priceCall = _tabOption.Tabs[i].PositionsOpenAll[j].EntryPrice;
                                volumeCall += _tabOption.Tabs[i].PositionsOpenAll[j].OpenVolume;
                                decimal pricePut = Math.Round(priceCall - ((averagePriceFuture - strike) / averagePriceFuture), 4);
                                countRightBreakEven += Math.Round(strike / (1 - priceCall - pricePut) * _tabOption.Tabs[i].PositionsOpenAll[j].OpenVolume);
                                countLeftBreakEven += Math.Round(strike / (1 + priceCall + pricePut) * _tabOption.Tabs[i].PositionsOpenAll[j].OpenVolume);
                            }
                        }
                    }

                    decimal rightBreakEven = Math.Round(countRightBreakEven / volumeCall);
                    decimal leftBreakEven = Math.Round(countLeftBreakEven / volumeCall);

                    AddLogList("Left Breakeven = " + leftBreakEven + ", Right Breakeven = " + rightBreakEven);

                    //decimal multiplier = Math.Round((rightBreakEven - leftBreakEven) / 2 / CountWorkParts, MidpointRounding.AwayFromZero);

                    decimal multiplier = Math.Round((rightBreakEven - leftBreakEven) / 2 / CountWorkParts, 2, MidpointRounding.AwayFromZero); // попытка деления на ноль
                    AddLogList($"multiplier = {multiplier}");

                    decimal volumeIntradayFuture = PositionFutureSize;

                    _workPartVolumeIntraday = Math.Abs(Math.Round(volumeIntradayFuture / RatioWorkParts, MidpointRounding.AwayFromZero)); // деление на ноль исключить

                    /*_ordersIntradayFuture.Add(new ListOrders(averagePriceFuture - multiplier, Side.Buy, _workPartVolumeIntraday, 0, Math.Round(averagePriceFuture - multiplier + multiplier / 2, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order));
                    _ordersIntradayFuture.Add(new ListOrders(averagePriceFuture - multiplier * 2, Side.Buy, _workPartVolumeIntraday, 0, Math.Round(averagePriceFuture - multiplier * 2 + multiplier, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order));
                    _ordersIntradayFuture.Add(new ListOrders(averagePriceFuture - multiplier * 4, Side.Buy, _workPartVolumeIntraday, 0, Math.Round(averagePriceFuture - multiplier * 4 + multiplier * 2, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order));
                    _ordersIntradayFuture.Add(new ListOrders(averagePriceFuture - multiplier * 12, Side.Buy, _workPartVolumeIntraday, 0, Math.Round(averagePriceFuture - multiplier * 12 + multiplier * 3, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order));
                    _ordersIntradayFuture.Add(new ListOrders(averagePriceFuture + multiplier, Side.Sell, _workPartVolumeIntraday, 0, Math.Round(averagePriceFuture + multiplier - multiplier / 2, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order));
                    _ordersIntradayFuture.Add(new ListOrders(averagePriceFuture + multiplier * 2, Side.Sell, _workPartVolumeIntraday, 0, Math.Round(averagePriceFuture + multiplier * 2 - multiplier, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order));
                    _ordersIntradayFuture.Add(new ListOrders(averagePriceFuture + multiplier * 4, Side.Sell, _workPartVolumeIntraday, 0, Math.Round(averagePriceFuture + multiplier * 4 - multiplier * 2, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order));
                    _ordersIntradayFuture.Add(new ListOrders(averagePriceFuture + multiplier * 12, Side.Sell, _workPartVolumeIntraday, 0, Math.Round(averagePriceFuture + multiplier * 12 - multiplier * 3, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order));*/

                    _ordersIntradayFuture.Add(new ListOrders(GetPriceFutureForIntraday(averagePriceFuture, - multiplier), Side.Buy, _workPartVolumeIntraday, 0, GetPriceFutureForIntraday(averagePriceFuture, - multiplier + multiplier), 0, OrdersType.MainOrder, ""));
                    _ordersIntradayFuture.Add(new ListOrders(GetPriceFutureForIntraday(averagePriceFuture, - multiplier * 2), Side.Buy, _workPartVolumeIntraday, 0, GetPriceFutureForIntraday(averagePriceFuture, - multiplier * 2 + multiplier), 0, OrdersType.MainOrder, ""));
                    _ordersIntradayFuture.Add(new ListOrders(GetPriceFutureForIntraday(averagePriceFuture, - multiplier * 4), Side.Buy, _workPartVolumeIntraday, 0, GetPriceFutureForIntraday(averagePriceFuture, - multiplier * 4 + multiplier * 2), 0, OrdersType.MainOrder, ""));
                    _ordersIntradayFuture.Add(new ListOrders(GetPriceFutureForIntraday(averagePriceFuture, - multiplier * 12), Side.Buy, _workPartVolumeIntraday, 0, GetPriceFutureForIntraday(averagePriceFuture, -multiplier * 12 + multiplier * 3), 0, OrdersType.MainOrder, ""));
                    _ordersIntradayFuture.Add(new ListOrders(GetPriceFutureForIntraday(averagePriceFuture, multiplier), Side.Sell, _workPartVolumeIntraday, 0, GetPriceFutureForIntraday(averagePriceFuture, multiplier - multiplier / 2), 0, OrdersType.MainOrder, ""));
                    _ordersIntradayFuture.Add(new ListOrders(GetPriceFutureForIntraday(averagePriceFuture, multiplier * 2), Side.Sell, _workPartVolumeIntraday, 0, GetPriceFutureForIntraday(averagePriceFuture, multiplier * 2 - multiplier), 0, OrdersType.MainOrder, ""));
                    _ordersIntradayFuture.Add(new ListOrders(GetPriceFutureForIntraday(averagePriceFuture, multiplier * 4), Side.Sell, _workPartVolumeIntraday, 0, GetPriceFutureForIntraday(averagePriceFuture, multiplier * 4 - multiplier * 2), 0, OrdersType.MainOrder, ""));
                    _ordersIntradayFuture.Add(new ListOrders(GetPriceFutureForIntraday(averagePriceFuture, multiplier * 12), Side.Sell, _workPartVolumeIntraday, 0, GetPriceFutureForIntraday(averagePriceFuture, multiplier * 12 - multiplier * 3), 0, OrdersType.MainOrder, ""));

                }

                if (_ordersIntradayFuture.Count != 0)
                {
                    for (int i = 0; i < _ordersIntradayFuture.Count; i++)
                    {
                        if (_ordersIntradayFuture[i].SideOrder == Side.Buy)
                        {
                            _tabPerp.BuyAtLimit(_ordersIntradayFuture[i].VolumeOrder, _ordersIntradayFuture[i].PriceOrder);
                        }
                        else
                        {
                            _tabPerp.SellAtLimit(_ordersIntradayFuture[i].VolumeOrder, _ordersIntradayFuture[i].PriceOrder);
                        }
                    }
                    _flagTradeIntraday = true;
                }

                foreach (ListOrders item in _ordersIntradayFuture)
                {
                    AddLogList($"Number = {item.NumberMarket}, PriceOrder = {item.PriceOrder}, SideOrder = {item.SideOrder}, VolumeOrder = {item.VolumeOrder}, ExecuteVolume = {item.ExecuteVolume}, PriceCounterOrder = {item.PriceCounterOrder}, {item.OrderType}");
                }
            }
            else
            {
               
            }

        }

        #endregion

        private decimal GetPriceFutureForIntraday(decimal averagePrice, decimal multiplier)
        {
            decimal priceStep = _tabPerp.Securiti.PriceStep;
            decimal price = Math.Round((averagePrice + multiplier) / priceStep, MidpointRounding.AwayFromZero) * priceStep;

            return price;
        }

        #region AssemblyConstruction
        private void AssemblyConstruction()
        {
            if (TimeLifeAssemblyConstruction != 0)
            {
                if (_timeLifeAssemblyConsruction.AddMinutes(TimeLifeAssemblyConstruction) <= DateTime.Now) //
                {
                    if (_flagConstruction == FlagConstruction.QuoteBuyOption ||
                        _flagConstruction == FlagConstruction.FirstBuyOption)
                    {
                        CancelOptionOrder();
                        AddLogList("Отмена ордера " + _tabOption.Tabs[_currentTab].Securiti.Name + " вышло время набора конструкции");

                        if (PositionOptionSize == 0)
                        {
                            Regime = DeribitPIUi.NameRegime.Off;
                        }
                        else
                        {
                            Regime = DeribitPIUi.NameRegime.TradeFutures;
                        }
                        return;
                    }
                }
            }
            
            /*if (_flagOptionChangeStrike && _flagConstruction != FlagConstruction.None)
            {
                if (_firstFixedStrike != CurrentStrike)
                {
                    for (int i = 0; i < _tabOption.Tabs.Count; i++)
                    {
                        if (_tabOption.Tabs[i].PositionsLast != null)
                        {
                            if (_tabOption.Tabs[i].PositionsLast.OpenOrders.Count > 0)
                            {
                                for (int j = 0; j < _tabOption.Tabs[i].PositionsLast.OpenOrders.Count; j++)
                                {
                                    if (_tabOption.Tabs[i].PositionsLast.OpenOrders[j].State == OrderStateType.Activ)
                                    {
                                        _tabOption.Tabs[i].CloseOrder(_tabOption.Tabs[i].PositionsLast.OpenOrders[j]);
                                        AddLogList("Отмена ордера " + _tabOption.Tabs[_currentTab].Securiti.Name + " по смене страйка, когда частично позиция набрана");
                                    }
                                    
                                }
                            }
                        }                        
                    }
                    return;
                }
            }*/
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
                        break;
                    }
                }

                DeribitServerRealization._postOnly = "false";
                decimal priceOption = GetOptionPriceLimit();
                decimal volumeOption = GetOptionVolume();
                _tabOption.Tabs[_currentTab].BuyAtLimit(volumeOption, priceOption);
                _flagConstruction = FlagConstruction.QuoteBuyOption;                
                _futureSellOrderVolume = GetFutureVolume();
                _timeLifeOrderOption = DateTime.Now;               

                AddLogList("Выставляем заявку на покупку " + _tabOption.Tabs[_currentTab].Securiti.Name + ", цена: " + priceOption + ", объем: " + volumeOption);                
            }

            if (_flagConstruction == FlagConstruction.QuoteBuyOption) // котируем опцион
            {               
                // если сменился текущий страйк отменяем ордер и начинаем все заново
                //if (_tabOption.Tabs[_currentTab].Securiti.Name != CurrentStrike && !_flagOptionChangeStrike)
                if (_tabOption.Tabs[_currentTab].Securiti.Name != CurrentStrike)
                    {                    
                    /*if (PositionOptionSize > 0) // если страйк поменялся и если уже есть набранная позиция
                    {
                        _flagOptionChangeStrike = true;
                        _firstFixedStrike = _tabOption.Tabs[_currentTab].Securiti.Name;                                               
                    }*/
                    
                    CancelOptionOrder();
                    AddLogList("Отмена ордера " + _tabOption.Tabs[_currentTab].Securiti.Name + " по смене страйка");
                    GetOptionMarkPrice();
                   
                    return;
                }

                // проверяем время жизни опционного ордера (в секундах) и высталяем опцион дороже
                if (_timeLifeOrderOption.AddSeconds(TimeOptionLimit) <= DateTime.Now && TimeOptionLimit != 0)
                {
                    if (_stepOrder > 2) // 2 - кол-во шагов изменения цены опциона, если превысили, начинаем котировать опцион от начальных условий
                    {
                        _stepOrder = 0;
                        AddLogList("Отмена ордера " + _tabOption.Tabs[_currentTab].Securiti.Name + " по истечению времени ордера и достижению предела шагов изменений ордера");
                    }
                    else
                    {
                        _stepOrder += 1;
                        AddLogList("Отмена ордера " + _tabOption.Tabs[_currentTab].Securiti.Name + " по истечению времени ордера");                        
                    }

                    CancelOptionOrder();

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
                            if (Math.Abs(MarkPriceOption - _tabOption.Tabs[_currentTab].PositionsLast.EntryPrice) > stepPrice / 2) // как надо заказчику
                            {
                                CancelOptionOrder();
                                AddLogList("Отмена ордера " + _tabOption.Tabs[_currentTab].Securiti.Name + " по изменению теор цены = " + MarkPriceOption);
                            }
                           /* if (MarkPriceOption < _tabOption.Tabs[_currentTab].PositionsLast.EntryPrice || // нужно для тестов
                                MarkPriceOption >= _tabOption.Tabs[_currentTab].PositionsLast.EntryPrice + stepPrice)
                            {
                                CancelOptionOrder();
                                AddLogList("Отмена ордера " + _tabOption.Tabs[_currentTab].Securiti.Name + " по изменению теор цены = " + MarkPriceOption);
                            }*/
                        }
                        else if (_stepOrder > 0)
                        {
                            if (MarkPriceOption - _tabOption.Tabs[_currentTab].PositionsLast.EntryPrice >= stepPrice)
                            {
                                CancelOptionOrder();
                                AddLogList("Отмена ордера " + _tabOption.Tabs[_currentTab].Securiti.Name + " по изменению теор цены = " + MarkPriceOption);
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
                    _tabPerp.SellAtMarket(_futureSellOrderVolume);
                    _flagConstruction = FlagConstruction.QuoteSellFuture;
                    AddLogList("Выставляем рыночную заявку на покупку " + _tabPerp.Securiti.Name + ", объем: " + _futureSellOrderVolume);
                    return;
                }
                else if (_futureTimeOrderLimit.AddSeconds(TimeFuturesLimit) <= DateTime.Now)
                {
                    _tabPerp.SellAtMarket(_futureSellOrderVolume);
                    _flagConstruction = FlagConstruction.QuoteSellFuture;
                    AddLogList("Выставляем рыночную заявку на покупку " + _tabPerp.Securiti.Name + ", объем: " + _futureSellOrderVolume);
                    return;
                }
                else
                {
                    DeribitServerRealization._postOnly = "true";
                    decimal priceFuture = GetFuturePrice();
                    _tabPerp.SellAtLimit(_futureSellOrderVolume, priceFuture);
                    _flagConstruction = FlagConstruction.QuoteSellFuture;
                    AddLogList("Выставляем лимитную заявку на покупку " + _tabPerp.Securiti.Name + ", цена: " + priceFuture+ ", объем: " + _futureSellOrderVolume);
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
                            _flagOptionChangeStrike = false;
                        }
                        return;
                    }
                    if (_flagFutureOrder && !CheckBoxMarketOrder)
                    {
                        if (_lastOrderPriceFuture > _bestAskPriceFuture)
                        {
                            CancelFutureOrder();
                            AddLogList("Отмена ордера " + _tabPerp.Securiti.Name + ", цена ордера не лучшая в стакане. Цена ордера: " + _lastOrderPriceFuture + ", Best Ask: " + _bestAskPriceFuture);
                        }
                        if (_lastOrderPriceFuture == _bestAskPriceFuture && 
                            _futureSellOrderVolume != _bestAskVolumeFuture && 
                            _lastOrderPriceFuture - _tabPerp.Securiti.PriceStep >_bestBidPriceFuture) // здесь надо подумать, если кто-то встает в нашу цену, чтобы не менялся ордер
                        {
                            CancelFutureOrder();
                            AddLogList("Отмена ордера " + _tabPerp.Securiti.Name + ", цена ордера такая же как и у других ордеров. Цена ордера: " + _lastOrderPriceFuture + ", Best Ask: " + _bestAskPriceFuture);
                        }
                            
                    }

                    // если лимитная заяка по фьючерсу не исполнилась, делаем заявку по маркету
                    if (_futureTimeOrderLimit.AddSeconds(TimeFuturesLimit) <= DateTime.Now)
                    {
                        CancelFutureOrder();
                        AddLogList("Отмена ордера " + _tabPerp.Securiti.Name + " по истечению времени");
                        return;
                    }
                }
            }
        }
        #endregion

        #region Methods

        private void StartThread()
        {
            Thread worker = new Thread(StartRobot) { IsBackground = true };
            worker.Start();
        }

        private void AddLogList(string str)
        {
            while (LogList.Count > 1000) // кол-во записей в логе
            {
                LogList.RemoveAt(0);
            }
            LogList.Add(DateTime.Now + " " + str);
            //ListLog.Enqueue(str);
            SaveLog();
        }

        private decimal GetFuturePrice()
        {
            decimal price = _bestAskPriceFuture;

            if (_bestAskPriceFuture - _bestBidPriceFuture > _tabPerp.Securiti.PriceStep)
            {
                price = _bestAskPriceFuture - _tabPerp.Securiti.PriceStep;
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
            _flagConstruction = FlagConstruction.None;

            if (_currentTab == 0 || _currentTab == _tabOption.Tabs.Count - 1)
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
                    _tabOption.Tabs[_currentTab].CloseOrder(_tabOption.Tabs[_currentTab].PositionsLast.OpenOrders[0]);
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
                priceOption = (Math.Round(MarkPriceOption / stepPrice, MidpointRounding.AwayFromZero) * stepPrice); // как надо заказчику
                //priceOption = Math.Floor(MarkPriceOption / stepPrice) * stepPrice; // как надо для тестов
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
                IRestResponse responseMessage = RequestRest("/api/v2/public/get_book_summary_by_instrument?instrument_name=" + CurrentStrike);

                if (responseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    ResponseMessageOptionMarkPrice response = JsonConvert.DeserializeObject<ResponseMessageOptionMarkPrice>(responseMessage.Content);
                    MarkPriceOption = Math.Round(response.result[0].mark_price, 4);
                    UnderlyingPrice = Math.Round(response.result[0].underlying_price, 2, MidpointRounding.AwayFromZero);                    
                    CurrentStrike = GetCurrentStrike();
                }
                else
                {
                    SendNewLogMessage(CurrentStrike + " - GetOptionMarkPrice - Http State Code: " + responseMessage.StatusCode + ", " + responseMessage.Content, LogMessageType.Error);
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
                IRestResponse responseMessage = RequestRest("/api/v2/public/get_book_summary_by_instrument?instrument_name=" + _tabPerp.Securiti.Name);

                if (responseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    ResponseMessageOptionMarkPrice response = JsonConvert.DeserializeObject<ResponseMessageOptionMarkPrice>(responseMessage.Content);
                    markPrice = Math.Round(response.result[0].mark_price, 4);
                    _baseCurrency = response.result[0].base_currency; // для тестов, потом убрать
                }
                else
                {
                    SendNewLogMessage("GetFutureMarkPrice - Http State Code: " + responseMessage.StatusCode + ", " + responseMessage.Content, LogMessageType.Error);
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
                string url = _typeServer + stringRequest;
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

            if (_tabOption.Tabs.Count == 0) 
            {
                return null;
            }

            for (int i = 0; i < _tabOption.Tabs.Count - 1; i++)
            {
                if (_tabOption.Tabs[i].Securiti != null && _tabOption.Tabs[i + 1].Securiti != null)
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
            }
            return currStrike;
        }

        /*private void LoadParameters()
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
                            else if (decimal.TryParse(split[1], out decimal numDec))
                            {
                                SetField(split[0], numDec);
                            }
                            else if (split[1] == "True")
                            {
                                SetField(split[0], true);
                            }
                            else if (split[1] == "False")
                            {
                                SetField(split[0], false);
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
        }*/

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
                    PercentOfDeposit = Convert.ToDecimal(reader.ReadLine().Split('^')[1]);
                    CountIteration = Convert.ToInt32(reader.ReadLine().Split('^')[1]);
                    TimeToCloseOption = Convert.ToInt32(reader.ReadLine().Split('^')[1]);
                    TimeFuturesLimit = Convert.ToInt32(reader.ReadLine().Split('^')[1]);
                    CheckBoxMarketOrder = Convert.ToBoolean(reader.ReadLine().Split('^')[1]);
                    TimeOptionLimit = Convert.ToInt32(reader.ReadLine().Split('^')[1]);
                    TimeLifeAssemblyConstruction = Convert.ToInt32(reader.ReadLine().Split('^')[1]);
                    CountWorkParts = Convert.ToInt32(reader.ReadLine().Split('^')[1]);
                    RatioWorkParts = Convert.ToInt32(reader.ReadLine().Split('^')[1]);
                    OneIncreaseX = Convert.ToInt32(reader.ReadLine().Split('^')[1]);
                    OneIncreaseY = Convert.ToInt32(reader.ReadLine().Split('^')[1]);
                    TwoIncreaseX = Convert.ToInt32(reader.ReadLine().Split('^')[1]);
                    TwoIncreaseY = Convert.ToInt32(reader.ReadLine().Split('^')[1]);
                    ThreeIncreaseX = Convert.ToInt32(reader.ReadLine().Split('^')[1]);
                    ThreeIncreaseY = Convert.ToInt32(reader.ReadLine().Split('^')[1]);

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
                    writer.WriteLine("PercentOfDeposit^" + PercentOfDeposit);
                    writer.WriteLine("CountIteration^" + CountIteration);
                    writer.WriteLine("TimeToCloseOption^" + TimeToCloseOption);
                    writer.WriteLine("TimeFuturesLimit^" + TimeFuturesLimit);
                    writer.WriteLine("CheckBoxMarketOrder^" + CheckBoxMarketOrder);
                    writer.WriteLine("TimeOptionLimit^" + TimeOptionLimit);
                    writer.WriteLine("TimeLifeAssemblyConstruction^" + TimeLifeAssemblyConstruction);
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



        private void LoadLog()
        {
            if (!File.Exists(@"Engine\" + NameStrategyUniq + @"Log.txt"))
            {
                return;
            }
            try
            {
                using (StreamReader reader = new StreamReader(@"Engine\" + NameStrategyUniq + @"Log.txt"))
                {
                    string line;
                    LogList = new List<string>();

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {                     
                            if (LogList.Count > 500)
                            {
                                LogList.RemoveAt(0);
                            }
                            LogList.Add(line);
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

        private void SaveLog()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Engine\" + NameStrategyUniq + @"Log.txt", false)
                    )
                {
                    for (int i = 0; i < LogList.Count; i++)
                    {
                        writer.WriteLine(LogList[i]);
                    }                    
                    writer.Close();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }

        #endregion
                
    }
    public class ListOrders
    {
        public decimal PriceOrder { get; set; }
        public Side SideOrder { get; set; }
        public decimal VolumeOrder { get; set; }
        public decimal ExecuteVolume { get; set; }
        public decimal PriceCounterOrder { get; set; }
        public decimal VolumeCounterOrder { get; set; }
        public OrdersType OrderType { get; set; }
        public string NumberMarket {  get; set; }

        public ListOrders(decimal priceOrder, Side sideOrder, decimal volumeOrder, decimal executeVolume, decimal priceCounterOrder, decimal volumeCounterOrder, OrdersType orderType, string numberMarket)
        {
            PriceOrder = priceOrder;
            SideOrder = sideOrder;
            VolumeOrder = volumeOrder;
            ExecuteVolume = executeVolume;
            PriceCounterOrder = priceCounterOrder;
            VolumeCounterOrder = volumeCounterOrder;
            OrderType = orderType;
            NumberMarket = numberMarket;
        }
    }
    public enum OrdersType
    {
        MainOrder,
        CounterOrder
    }
}
