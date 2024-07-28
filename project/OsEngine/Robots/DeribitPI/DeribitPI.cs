using Newtonsoft.Json;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market.Servers.Deribit;
using OsEngine.Market.Servers.GateIo.GateIoFutures.Entities.Response;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Tab.Internal;
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
            TabCreate(BotTabType.Simple);
            _tabIntraday = TabsSimple[1];
            TabCreate(BotTabType.Screener);
            _tabOption = TabsScreener[0];

            //_tabOption.PositionOpeningSuccesEvent += _tabOption_PositionOpeningSuccesEvent;
            //_tabPerp.PositionOpeningSuccesEvent += _tabPerp_PositionOpeningSuccesEvent;
            _tabPerp.CandleUpdateEvent += _tabPerp_CandleUpdateEvent;
            _tabOption.NewTabCreateEvent += _tabOption_NewTabCreateEvent;
            _tabPerp.OrderUpdateEvent += _tabPerp_OrderUpdateEvent;
            _tabOption.OrderUpdateEvent += _tabOption_OrderUpdateEvent;
            //_tabIntraday.OrderUpdateEvent += _tabIntraday_OrderUpdateEvent;
            
            DisableManualPositionSupport(_tabPerp);
            DisableManualPositionSupport(_tabIntraday);

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
        private BotTabSimple _tabIntraday;
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
        public decimal PositionFutureIntraday;
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
        private decimal _increaseWorkPartVolumeIntraday = 0;
        private int _countProfitOrders;
        private OrdersType _lastOrderType = OrdersType.None;

        #endregion

        #region Events

        private void MyServer_NewMarketDepthEvent(MarketDepth obj)
        {
            try
            {
                if (_tabPerp.Securiti != null)
                {
                    if (obj.SecurityNameCode == _tabPerp.Securiti.Name)
                    {
                        _bestAskPriceFuture = obj.Asks[0].Price;
                        _bestAskVolumeFuture = obj.Asks[0].Ask;
                        _bestBidPriceFuture = obj.Bids[0].Price;
                        _bestBidVolumeFuture = obj.Bids[0].Bid;
                    }
                    if (!OnTradeRegime)
                    {
                        OnTradeRegime = true;
                    }
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
            }   
        }
                
        private void _tabOption_OrderUpdateEvent(Order obj, BotTabSimple arg2)
        {
            try
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

                if (obj.State == OrderStateType.Done)
                {
                    _flagConstruction = FlagConstruction.FirstSellFuture;
                    _stepOrder = 0;
                    _futureTimeOrderLimit = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private void _tabPerp_OrderUpdateEvent(Order obj)
        {
            try
            {
                if (Regime == DeribitPIUi.NameRegime.AssemblyConstruction)
                {
                    AddLogList("OrderEvent: " + obj.SecurityNameCode + ", Num: " + obj.NumberMarket + ", State: " + obj.State + ", " +
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
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private void MyServer_NewOrderIncomeEvent(Order obj)
        {
            try
            {
                if (obj.SecurityNameCode == _tabPerp.Securiti.Name)
                {
                    if (Regime == DeribitPIUi.NameRegime.TradeFutures)
                    {

                        for (int i = 0; i < _ordersIntradayFuture.Count; i++) // проверка на пустые номера ордеров и присвоение номера
                        {
                            if (_ordersIntradayFuture[i].PriceOrder == obj.Price && _ordersIntradayFuture[i].NumberMarket == "")
                            {
                                if (obj.State == OrderStateType.Activ ||
                                obj.State == OrderStateType.Done)
                                {
                                    _ordersIntradayFuture[i].NumberMarket = obj.NumberMarket;
                                    string str = $"Num = {_ordersIntradayFuture[i].NumberMarket}, PriceOrder = {_ordersIntradayFuture[i].PriceOrder}, SideOrder = {_ordersIntradayFuture[i].SideOrder}, " +
                                        $"Vol = {_ordersIntradayFuture[i].VolumeOrder}, ExVol = {_ordersIntradayFuture[i].ExecuteVolume}, " +
                                        $"PriceCounterOrder = {_ordersIntradayFuture[i].PriceCounterOrder}, {_ordersIntradayFuture[i].OrderType}";
                                    AddLogList(str);
                                    SendNewLogMessage(str, LogMessageType.User);
                                }
                            }
                        }

                        if (obj.VolumeExecute > 0)
                        {
                            AddLogList("OrderEvent: " + obj.SecurityNameCode + ", Num: " + obj.NumberMarket + ", State: " + obj.State + ", " +
                                    "Side: " + obj.Side + ", Price: " + obj.Price + ", Vol: " + obj.Volume + ", VolEx: " + obj.VolumeExecute);

                            if (obj.State == OrderStateType.Activ ||
                                obj.State == OrderStateType.Done)
                            {
                                for (int indexFirstOrder = 0; indexFirstOrder < _ordersIntradayFuture.Count; indexFirstOrder++)
                                {
                                    if (_ordersIntradayFuture[indexFirstOrder].NumberMarket == obj.NumberMarket) // находим ордер по которому пришел ивент
                                    {
                                        _ordersIntradayFuture[indexFirstOrder].ExecuteVolume = obj.VolumeExecute;

                                        if (_ordersIntradayFuture[indexFirstOrder].ExecuteVolume > 0)
                                        {
                                            bool tryCounterOrder = false;

                                            for (int indexSecondOrder = 0; indexSecondOrder < _ordersIntradayFuture.Count; indexSecondOrder++) // находим противоположный ордер
                                            {
                                                if (_ordersIntradayFuture[indexSecondOrder].PriceOrder == _ordersIntradayFuture[indexFirstOrder].PriceCounterOrder) // если он есть
                                                {
                                                    tryCounterOrder = true;
                                                    if (_ordersIntradayFuture[indexSecondOrder].VolumeOrder != obj.VolumeExecute) // если объемы ордера и противоположного ордера не равны
                                                    {
                                                        for (int closeOrder = 0; closeOrder < _tabIntraday.PositionsOpenAll.Count; closeOrder++)
                                                        {
                                                            if (_tabIntraday.PositionsOpenAll[closeOrder].OpenOrders[0].NumberMarket != "" && // если номер ордера не пустой
                                                                _ordersIntradayFuture[indexSecondOrder].NumberMarket == _tabIntraday.PositionsOpenAll[closeOrder].OpenOrders[0].NumberMarket) // и если номер ордера такой же как в массиве
                                                            {
                                                                decimal volumeInteradayFuture = _workPartVolumeIntraday - _ordersIntradayFuture[indexFirstOrder].VolumeOrder + _ordersIntradayFuture[indexFirstOrder].ExecuteVolume; // определяем какой объем противоположного ордера выставить
                                                             
                                                                AddLogList($"Удаляем противоположный ордер {_tabIntraday.PositionsOpenAll[closeOrder].OpenOrders[0].NumberMarket} " +
                                                                   $"с ценой {_tabIntraday.PositionsOpenAll[closeOrder].OpenOrders[0].Price} " +
                                                                   $"и выставляем новый противоположный ордер Price = {_ordersIntradayFuture[indexFirstOrder].PriceCounterOrder}, Vol = {volumeInteradayFuture}");

                                                                // удаляем противоположный ордер
                                                                Position pos = _tabIntraday.PositionsOpenAll[closeOrder];
                                                                decimal vol = _tabIntraday.PositionsOpenAll[closeOrder].OpenOrders[0].Volume;// здесь!!!
                                                                AddLogList($"Объем удаляемой позиции = {vol}");
                                                                decimal price = _tabIntraday.PositionsOpenAll[closeOrder].OpenOrders[0].Price;
                                                                _tabIntraday.CloseAtFake(pos, vol, price, DateTime.Now);

                                                                if (_ordersIntradayFuture[indexFirstOrder].SideOrder == Side.Buy) // и выставляем новый противоположный ордер
                                                                {
                                                                    _tabIntraday.SellAtLimit(volumeInteradayFuture, _ordersIntradayFuture[indexFirstOrder].PriceCounterOrder);
                                                                }
                                                                else
                                                                {
                                                                    _tabIntraday.BuyAtLimit(volumeInteradayFuture, _ordersIntradayFuture[indexFirstOrder].PriceCounterOrder);
                                                                }

                                                                // вносим изменения в массив
                                                                _ordersIntradayFuture[indexSecondOrder].VolumeOrder = volumeInteradayFuture;
                                                                _ordersIntradayFuture[indexSecondOrder].NumberMarket = "";
                                                                _ordersIntradayFuture[indexSecondOrder].ExecuteVolume = 0;
                                                                break;
                                                            }
                                                        }
                                                    }                                                    
                                                    break;
                                                }
                                            }

                                            if (!tryCounterOrder) // если противоположного ордера нет, то выставляем этот ордер
                                            {
                                                if (_ordersIntradayFuture[indexFirstOrder].SideOrder == Side.Buy)
                                                {
                                                    _tabIntraday.SellAtLimit(_ordersIntradayFuture[indexFirstOrder].ExecuteVolume, _ordersIntradayFuture[indexFirstOrder].PriceCounterOrder);
                                                }
                                                else
                                                {
                                                    _tabIntraday.BuyAtLimit(_ordersIntradayFuture[indexFirstOrder].ExecuteVolume, _ordersIntradayFuture[indexFirstOrder].PriceCounterOrder);
                                                }
                                                AddLogList($"Противоположного ордера нет, выставляем новый ордер Price = {_ordersIntradayFuture[indexFirstOrder].PriceCounterOrder}, Vol = {_ordersIntradayFuture[indexFirstOrder].ExecuteVolume}");

                                                // заносим ордер в массив
                                                _ordersIntradayFuture.Add(new ListOrders(_ordersIntradayFuture[indexFirstOrder].PriceCounterOrder,
                                                    _ordersIntradayFuture[indexFirstOrder].SideOrder == Side.Buy ? Side.Sell : Side.Buy,
                                                    obj.VolumeExecute,
                                                    0,
                                                    _ordersIntradayFuture[indexFirstOrder].PriceOrder,
                                                    _ordersIntradayFuture[indexFirstOrder].OrderType == OrdersType.MainOrder ? OrdersType.CounterOrder : OrdersType.MainOrder,
                                                    ""));
                                            }
                                        }
                                        if (_ordersIntradayFuture[indexFirstOrder].ExecuteVolume == _ordersIntradayFuture[indexFirstOrder].VolumeOrder) // если объем выполненный равен объему ордера, то удаляем ордер из массива
                                        {
                                            CheckOrderType(_ordersIntradayFuture[indexFirstOrder].OrderType); //проверка на последовательность прохождения типов ордеров

                                            _ordersIntradayFuture.RemoveAt(indexFirstOrder);

                                            for (int indexCloseOrder = 0; indexCloseOrder < _tabIntraday.PositionsOpenAll.Count; indexCloseOrder++)
                                            {
                                                if (_tabIntraday.PositionsOpenAll[indexCloseOrder].OpenOrders[0].NumberMarket == obj.NumberMarket)
                                                {
                                                    Position pos = _tabIntraday.PositionsOpenAll[indexCloseOrder];
                                                    decimal vol = _tabIntraday.PositionsOpenAll[indexCloseOrder].OpenOrders[0].Volume;
                                                    decimal price = _tabIntraday.PositionsOpenAll[indexCloseOrder].OpenOrders[0].Price;
                                                    _tabIntraday.CloseAtFake(pos, vol, price, DateTime.Now);
                                                    break;
                                                }
                                            }
                                            AddLogList($"Объем выполненный {obj.VolumeExecute} равен объему ордера {obj.Volume}, удаляем ордер {obj.NumberMarket} из массива и делаем фейк-закрытие");

                                            CheckIncreaseWorkParts();// проверка на увеличение НРЧ
                                        }
                                        break;
                                    }
                                }
                                foreach (ListOrders item in _ordersIntradayFuture)
                                {
                                    AddLogList($"Num = {item.NumberMarket}, PriceOrder = {item.PriceOrder}, SideOrder = {item.SideOrder}, Vol = {item.VolumeOrder}, ExVol = {item.ExecuteVolume}, PriceCounterOrder = {item.PriceCounterOrder}, {item.OrderType}");
                                }
                            }
                        }
                        if (obj.State == OrderStateType.Cancel)
                        {
                            AddLogList($"Ордер отменен - {obj.NumberMarket}, Price = {obj.Price}, Vol = {obj.Volume}, ExVol = {obj.VolumeExecute}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
            }           
        }

        private void CheckOrderType(OrdersType type)
        {            
            if (_lastOrderType == OrdersType.MainOrder &&
                type == OrdersType.MainOrder) 
            {
                _countProfitOrders = 0;
            }
            if (_lastOrderType == OrdersType.MainOrder &&
                type == OrdersType.CounterOrder)
            {
                _countProfitOrders++;
            }
            if (_lastOrderType == OrdersType.CounterOrder &&
                type == OrdersType.CounterOrder)
            {
                _countProfitOrders++;
            }

            AddLogList($"_lastOrderType = {_lastOrderType} CurrentType = {type}, _countProfitOrders = {_countProfitOrders}");
            _lastOrderType = type;
        }

        private void CheckIncreaseWorkParts()
        {
            if (_countProfitOrders == 0)
            {
                if (_increaseWorkPartVolumeIntraday > _workPartVolumeIntraday)
                {
                    _increaseWorkPartVolumeIntraday = _workPartVolumeIntraday;
                    ChangeWorkPartToOrders();
                    AddLogList("Возвращаем рабочую часть к первоначальному значению");
                }
            }
            if (_countProfitOrders == OneIncreaseX && 
                OneIncreaseX != 0 &&
                OneIncreaseY != 0)
            {
                if (_increaseWorkPartVolumeIntraday != _workPartVolumeIntraday * OneIncreaseY)
                {
                    _increaseWorkPartVolumeIntraday = _workPartVolumeIntraday * OneIncreaseY;
                    ChangeWorkPartToOrders();
                    AddLogList($"Увеличиваем рабочую часть, по условию Х1, НРЧ = {_increaseWorkPartVolumeIntraday}");
                }                
            }
            if (_countProfitOrders == TwoIncreaseX &&
                TwoIncreaseX != 0 &&
                TwoIncreaseY != 0)
            {
                if (_increaseWorkPartVolumeIntraday != _workPartVolumeIntraday * TwoIncreaseY)
                {
                    _increaseWorkPartVolumeIntraday = _workPartVolumeIntraday * TwoIncreaseY;
                    ChangeWorkPartToOrders();
                    AddLogList($"Увеличиваем рабочую часть, по условию Х2, НРЧ = {_increaseWorkPartVolumeIntraday}");
                }
            }
            if (_countProfitOrders == ThreeIncreaseX &&
                ThreeIncreaseX != 0 &&
                ThreeIncreaseY != 0)
            {
                if (_increaseWorkPartVolumeIntraday != _workPartVolumeIntraday * ThreeIncreaseY)
                {
                    _increaseWorkPartVolumeIntraday = _workPartVolumeIntraday * ThreeIncreaseY;
                    ChangeWorkPartToOrders();
                    AddLogList($"Увеличиваем рабочую часть, по условию Х3, НРЧ = {_increaseWorkPartVolumeIntraday}");
                }
            }
        }

        private void ChangeWorkPartToOrders()
        {
            foreach (ListOrders item in _ordersIntradayFuture)
            {
                item.VolumeOrder = _increaseWorkPartVolumeIntraday;
                item.NumberMarket = "";
            }
            for (int i = 0;  i < _tabIntraday.PositionsOpenAll.Count; i++)
            {
                _tabIntraday.CloseAllOrderToPosition(_tabIntraday.PositionsOpenAll[i]);
            }
            for (int i = 0; i < _ordersIntradayFuture.Count; i++)
            {
                if (_ordersIntradayFuture[i].SideOrder == Side.Buy)
                {
                    _tabIntraday.BuyAtLimit(_ordersIntradayFuture[i].VolumeOrder, _ordersIntradayFuture[i].PriceOrder);
                }
                else
                {
                    _tabIntraday.SellAtLimit(_ordersIntradayFuture[i].VolumeOrder, _ordersIntradayFuture[i].PriceOrder);
                }
            }
        }

        private void _tabPerp_CandleUpdateEvent(List<Candle> candle)
        {
            try
            {
                if (candle.Count > 0)
                {
                    _lastOrderPriceFuture = candle[candle.Count - 1].Close;
                }
            }
             catch (Exception ex )
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
            }         
        }

        #endregion               

        #region Thread robot
        private void StartRobot()
        {
            try
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
                                        
                    List<Position> positionsIntraday;
                    if (_tabIntraday.PositionsOpenAll.Count > 0)
                    {
                        positionsIntraday = _tabIntraday.PositionsOpenAll;// удалить после тестирования
                        /*decimal pos = _tabIntraday.Connector.MyServer.Portfolios[0].ValueCurrent;
                        for (int i = 0; i < _tabIntraday.PositionsOpenAll.Count; i++)
                        {
                            PositionFutureIntraday += _tabIntraday.PositionsOpenAll[i].OpenVolume;
                        } */                       
                    }

                    // получаем данные по портфелю фьючерсов
                    if (_tabPerp.PositionsOnBoard != null && _tabPerp.PositionsOnBoard.Count > 0)
                    {
                        PositionFutureSize = 0;

                        for (int i = 0; i < _tabPerp.PositionsOpenAll.Count; i++)
                        {
                            if (_tabPerp.PositionsOpenAll[i].Direction == Side.Buy)
                            {
                                PositionFutureSize += _tabPerp.PositionsOpenAll[i].OpenVolume;
                            }
                            else
                            {
                                PositionFutureSize -= _tabPerp.PositionsOpenAll[i].OpenVolume;
                            }
                        }
                        PositionFutureIntraday = _tabIntraday.PositionsOnBoard[0].ValueCurrent - PositionFutureSize;
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
                            for (int i = 0; i < _tabIntraday.PositionsOpenAll.Count; i++)
                            {
                                if (_tabIntraday.PositionsOpenAll[i].OpenOrders[0].State == OrderStateType.Activ)
                                {
                                    _tabIntraday.CloseOrder(_tabIntraday.PositionsOpenAll[i].OpenOrders[0]);
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
                        TradeFutures();
                    }

                    if (Regime == DeribitPIUi.NameRegime.DisassemblyConstruction)
                    {
                        if (checkParameters == false) // проверка введены ли все необходимые настройки
                        {
                            checkParameters = CheckParameters();
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        

        #endregion

        #region TradeFutures
        private void TradeFutures()
        {
            try
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

                        AddLogList($"Средняя цена покупки фьючерса = {averagePriceFuture}");

                        decimal rightBreakEven = Math.Round(countRightBreakEven / volumeCall); 
                        decimal leftBreakEven = Math.Round(countLeftBreakEven / volumeCall);
                        AddLogList("Left Breakeven = " + leftBreakEven + ", Right Breakeven = " + rightBreakEven);

                        decimal multiplier = Math.Round((rightBreakEven - leftBreakEven) / 2 / CountWorkParts, 2, MidpointRounding.AwayFromZero); 
                        AddLogList($"Шаг цены ордера = {multiplier}");

                        decimal volumeIntradayFuture = PositionFutureSize;

                        _workPartVolumeIntraday = Math.Abs(Math.Round(volumeIntradayFuture / RatioWorkParts, MidpointRounding.AwayFromZero));
                        _increaseWorkPartVolumeIntraday = _workPartVolumeIntraday;

                        _ordersIntradayFuture.Add(new ListOrders(GetPriceFutureForIntraday(averagePriceFuture, -multiplier), Side.Buy, _workPartVolumeIntraday, 0, GetPriceFutureForIntraday(averagePriceFuture, -multiplier + multiplier), OrdersType.MainOrder, ""));
                        _ordersIntradayFuture.Add(new ListOrders(GetPriceFutureForIntraday(averagePriceFuture, -multiplier * 2), Side.Buy, _workPartVolumeIntraday, 0, GetPriceFutureForIntraday(averagePriceFuture, -multiplier * 2 + multiplier), OrdersType.MainOrder, ""));
                        _ordersIntradayFuture.Add(new ListOrders(GetPriceFutureForIntraday(averagePriceFuture, -multiplier * 4), Side.Buy, _workPartVolumeIntraday, 0, GetPriceFutureForIntraday(averagePriceFuture, -multiplier * 4 + multiplier * 2), OrdersType.MainOrder, ""));
                        _ordersIntradayFuture.Add(new ListOrders(GetPriceFutureForIntraday(averagePriceFuture, -multiplier * 12), Side.Buy, _workPartVolumeIntraday, 0, GetPriceFutureForIntraday(averagePriceFuture, -multiplier * 12 + multiplier * 3), OrdersType.MainOrder, ""));
                        _ordersIntradayFuture.Add(new ListOrders(GetPriceFutureForIntraday(averagePriceFuture, multiplier), Side.Sell, _workPartVolumeIntraday, 0, GetPriceFutureForIntraday(averagePriceFuture, multiplier - multiplier / 2), OrdersType.MainOrder, ""));
                        _ordersIntradayFuture.Add(new ListOrders(GetPriceFutureForIntraday(averagePriceFuture, multiplier * 2), Side.Sell, _workPartVolumeIntraday, 0, GetPriceFutureForIntraday(averagePriceFuture, multiplier * 2 - multiplier), OrdersType.MainOrder, ""));
                        _ordersIntradayFuture.Add(new ListOrders(GetPriceFutureForIntraday(averagePriceFuture, multiplier * 4), Side.Sell, _workPartVolumeIntraday, 0, GetPriceFutureForIntraday(averagePriceFuture, multiplier * 4 - multiplier * 2), OrdersType.MainOrder, ""));
                        _ordersIntradayFuture.Add(new ListOrders(GetPriceFutureForIntraday(averagePriceFuture, multiplier * 12), Side.Sell, _workPartVolumeIntraday, 0, GetPriceFutureForIntraday(averagePriceFuture, multiplier * 12 - multiplier * 3), OrdersType.MainOrder, ""));
                    }

                    if (_ordersIntradayFuture.Count != 0)
                    {
                        for (int i = 0; i < _ordersIntradayFuture.Count; i++)
                        {
                            if (_ordersIntradayFuture[i].SideOrder == Side.Buy)
                            {
                                _tabIntraday.BuyAtLimit(_ordersIntradayFuture[i].VolumeOrder, _ordersIntradayFuture[i].PriceOrder);
                            }
                            else
                            {
                                _tabIntraday.SellAtLimit(_ordersIntradayFuture[i].VolumeOrder, _ordersIntradayFuture[i].PriceOrder);
                            }
                        }
                        _flagTradeIntraday = true;
                    }

                    foreach (ListOrders item in _ordersIntradayFuture)
                    {
                        AddLogList($"Number = {item.NumberMarket}, PriceOrder = {item.PriceOrder}, SideOrder = {item.SideOrder}, VolumeOrder = {item.VolumeOrder}, ExecuteVolume = {item.ExecuteVolume}, PriceCounterOrder = {item.PriceCounterOrder}, {item.OrderType}");
                    }
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        #endregion               

        #region AssemblyConstruction
        private void AssemblyConstruction()
        {
            try
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
                        AddLogList("Выставляем лимитную заявку на покупку " + _tabPerp.Securiti.Name + ", цена: " + priceFuture + ", объем: " + _futureSellOrderVolume);
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
                                _lastOrderPriceFuture - _tabPerp.Securiti.PriceStep > _bestBidPriceFuture) // здесь надо подумать, если кто-то встает в нашу цену, чтобы не менялся ордер
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
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
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
            try
            {
                while (LogList.Count > 2000) // кол-во записей в логе
                {
                    LogList.RemoveAt(0);
                }
                LogList.Add(DateTime.Now + " " + str);
                //ListLog.Enqueue(str);
                SaveLog();
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private decimal GetFuturePrice()
        {
            try
            {
                decimal price = _bestAskPriceFuture;

                if (_bestAskPriceFuture - _bestBidPriceFuture > _tabPerp.Securiti.PriceStep)
                {
                    price = _bestAskPriceFuture - _tabPerp.Securiti.PriceStep;
                }

                return price;
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
                return _bestAskPriceFuture;
            }
        }

        private decimal GetOptionVolume()
        {
            try
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
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
                return 0;
            }
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
            try
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
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private void CancelFutureOrder()
        {
            try
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
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private decimal GetOptionPriceLimit()
        {
            try
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
                    if (_tabOption.Tabs[_currentTab].PositionsLast.EntryPrice != null)
                    {
                        priceOption = _tabOption.Tabs[_currentTab].PositionsLast.EntryPrice + stepPrice;
                    }

                    /*if (_tabOption.Tabs[_currentTab].PositionsOpenAll[_tabOption.Tabs[_currentTab].PositionsOpenAll.Count - 1].EntryPrice != null)
                    {
                        priceOption = _tabOption.Tabs[_currentTab].PositionsOpenAll[_tabOption.Tabs[_currentTab].PositionsOpenAll.Count - 1].EntryPrice + stepPrice;
                    }*/

                }

                return priceOption;
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
                return 0;
            }
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
            try
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
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
                return null;
            }
        }

        private void LoadParameters()
        {
            if (!File.Exists(@"Engine\" + NameStrategyUniq + @"Parameters.txt"))
            {
                return;
            }
            try
            { // надо подумать если файл пустой
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

        private void _tabOption_NewTabCreateEvent(BotTabSimple obj)
        {
            DisableManualPositionSupport(obj);
        }

        private void DisableManualPositionSupport(BotTabSimple tab)
        {
            BotManualControl bmc = tab.ManualPositionSupport;
            bmc.SecondToOpenIsOn = false;
            bmc.SecondToCloseIsOn = false;
            bmc.StopIsOn = false;
            bmc.ProfitIsOn = false;
            bmc.DoubleExitIsOn = false;
            bmc.SetbackToOpenIsOn = false;
            bmc.SetbackToCloseIsOn = false;
        }

        private decimal GetPriceFutureForIntraday(decimal averagePrice, decimal multiplier)
        {
            decimal priceStep = _tabIntraday.Securiti.PriceStep;
            decimal price = Math.Round((averagePrice + multiplier) / priceStep, MidpointRounding.AwayFromZero) * priceStep;

            return price;
        }

        private bool CheckParameters()
        {
            try
            {
                bool check = true;

                if (PercentOfDeposit == 0)
                {
                    check = false;
                }
                if (CountIteration == 0)
                {
                    check = false;
                }
                if (TimeToCloseOption == 0)
                {
                    check = false;
                }
                if (CountWorkParts == 0)
                {
                    check = false;
                }
                if (RatioWorkParts == 0)
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
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
                return false;
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
        public OrdersType OrderType { get; set; }
        public string NumberMarket {  get; set; }

        public ListOrders(decimal priceOrder, Side sideOrder, decimal volumeOrder, decimal executeVolume, decimal priceCounterOrder, OrdersType orderType, string numberMarket)
        {
            PriceOrder = priceOrder;
            SideOrder = sideOrder;
            VolumeOrder = volumeOrder;
            ExecuteVolume = executeVolume;
            PriceCounterOrder = priceCounterOrder;
            OrderType = orderType;
            NumberMarket = numberMarket;
        }
    }
    public enum OrdersType
    {
        MainOrder,
        CounterOrder,
        None        
    }
}
