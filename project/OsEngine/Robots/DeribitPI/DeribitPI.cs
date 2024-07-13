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
using System.IO;
using System.Threading;

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
        private List<dynamic> _ordersIntradayFuture = new List<dynamic>();
        private bool _flagTradeIntraday = false;
        private List<dynamic> _ordersStopProfit = new List<dynamic>();

        public DeribitPI(string name, StartProgram startProgram) : base(name, startProgram)
        {
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
                    if (obj.VolumeExecute > 0 && obj.State == OrderStateType.Activ)
                    {
                        for (int i = 0; i < _ordersIntradayFuture.Count; i++)
                        {
                            if (_ordersIntradayFuture[i]["PriceOrder"] == obj.Price)
                            {
                                _ordersIntradayFuture[i]["VolumeExecute"] = obj.VolumeExecute;
                                _ordersIntradayFuture[i]["VolumeCounterOrder"] = obj.VolumeExecute;

                                if (_ordersStopProfit.Count > 0)
                                {
                                    for (int j = 0; j < _ordersStopProfit.Count; j++)
                                    {
                                        if (_ordersStopProfit[j]["PriceStopProfit"] == obj.Price)
                                        {
                                            _tabPerp.CloseOrder(obj);

                                            if (obj.Side == Side.Buy)
                                            {
                                                _tabPerp.SellAtLimit(obj.VolumeExecute, _ordersIntradayFuture[i]["PriceCounterOrder"]);
                                            }
                                            else
                                            {
                                                _tabPerp.BuyAtLimit(obj.VolumeExecute, _ordersIntradayFuture[i]["PriceCounterOrder"]);
                                            }
                                            _ordersStopProfit[j]["Volume"] = obj.VolumeExecute;
                                        }
                                        break;
                                    }
                                }
                                if (obj.Side == Side.Buy)
                                {
                                    _tabPerp.SellAtLimit(obj.VolumeExecute, _ordersIntradayFuture[i]["PriceCounterOrder"]);
                                }
                                else
                                {
                                    _tabPerp.BuyAtLimit(obj.VolumeExecute, _ordersIntradayFuture[i]["PriceCounterOrder"]);
                                }
                            }
                        }
                    }
                    
                }
                

                AddLogList(DateTime.Now + " OrderEvent: " + obj.SecurityNameCode + ", Num: " + obj.NumberMarket + ", State: " + obj.State + ", " +
                    "Side: " + obj.Side+ ", Price: " + obj.Price + ", Volume: " + obj.Volume + ", VolEx: " + obj.VolumeExecute);
                //AddLogList($"{DateTime.Now} _OrderPrice = {_lastOrderPriceFuture}, _SellOrderVolume = {_futureSellOrderVolume}, _VolEx = {_futureSellOrderVolumeExecute}");
            }
            else
            {
                AddLogList(DateTime.Now + " OrderEvent: " + obj.SecurityNameCode + ", Num: " + obj.NumberMarket + ", State: " + obj.State + ", " +
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
            _lastOrderPriceFuture = candle[candle.Count - 1].Close;
        }

        private void _tabPerp_PositionOpeningSuccesEvent(Position pos)
        {
            string str = DateTime.Now + ": " + pos.MyTrades[0].Side + " " + _tabPerp.Securiti.Name + ", Price: " + pos.MyTrades[0].Price+ ", Volume: "+ pos.MyTrades[0].Volume;
            AddLogList(str);
        }

        private void _tabOption_PositionOpeningSuccesEvent(Position pos, BotTabSimple tab)
        {
            string str = DateTime.Now + ": " + pos.MyTrades[0].Side + " " + tab.Securiti.Name + ", Price: " + pos.MyTrades[0].Price + ", Volume: " + pos.MyTrades[0].Volume;
            AddLogList(str);

            _flagConstruction = FlagConstruction.FirstSellFuture;
            _stepOrder = 0;
            _futureTimeOrderLimit = DateTime.Now;
        }

        private void AddLogList(string str)
        {
            while (LogList.Count > 1000) // кол-во записей в логе
            {
                LogList.RemoveAt(0);
            }
            LogList.Add(str);
            //ListLog.Enqueue(str);
            SaveLog();
        }

        private void StartThread()
        {
            Thread worker = new Thread(StartRobot) { IsBackground = true };
            worker.Start();
        }

        #region StartRobot
        private void StartRobot()
        {
            bool connectorOn = false;

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
                        AddLogList(DateTime.Now + " Робот запущен");
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

                    if (_flagConstruction == FlagConstruction.QuoteBuyOption) // если есть выставленный ордер на покупку опциона, то отменяем его
                    {
                        CancelOptionOrder();
                        AddLogList(DateTime.Now + " Отмена ордера - выбран режим Выключено");
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
                    if (!_flagTimerAssemblyConstruction)
                    {
                        _timeLifeAssemblyConsruction = DateTime.Now;
                        _flagTimerAssemblyConstruction = true;
                        AddLogList(DateTime.Now + " Время начала набора конструкции");
                    }
                    AssemblyConstruction();
                }

                if (Regime == DeribitPIUi.NameRegime.TradeFutures)
                {
                    //TradeFutures();
                }
            }
        }

        #endregion

        #region TradeFutures
        private void TradeFutures()
        {
            if (!_flagTradeIntraday)
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

                AddLogList(DateTime.Now + " Left Breakeven = " + leftBreakEven + ", Right Breakeven = " + rightBreakEven);

                decimal multiplier = Math.Round((rightBreakEven - leftBreakEven) / 2 / CountWorkParts, MidpointRounding.AwayFromZero);

                //decimal pointPriceFuture = GetFuturePrice();

                decimal volumeIntradayFuture = PositionFutureSize;

                decimal workPartVolumeIntraday = Math.Abs(Math.Round(volumeIntradayFuture / RatioWorkParts, MidpointRounding.AwayFromZero)); // деление на ноль исключить

                _ordersIntradayFuture.Add(new Dictionary<string, dynamic>()
                {
                    { "PriceOrder", averagePriceFuture - multiplier },
                    { "Side", Side.Buy },
                    { "WorkPartVolume", workPartVolumeIntraday },
                    { "ExecuteVolume", 0 },
                    { "PriceCounterOrder", Math.Round(averagePriceFuture - multiplier + multiplier / 2, 1, MidpointRounding.AwayFromZero)},
                    { "VolumeCounterOrder",  0},
                    { "OrderType", OrdersType.Order }

                });

                _ordersIntradayFuture.Add(new Dictionary<string, dynamic>()
                {
                    { "PriceOrder", averagePriceFuture - multiplier * 2 },
                    { "Side", Side.Buy },
                    { "WorkPartVolume", workPartVolumeIntraday },
                    { "ExecuteVolume", 0 },
                    { "PriceCounterOrder", Math.Round(averagePriceFuture - multiplier + multiplier, 1, MidpointRounding.AwayFromZero)},
                    { "VolumeCounterOrder",  0},
                    { "OrderType", OrdersType.Order }

                });

                _ordersIntradayFuture.Add(new Dictionary<string, dynamic>()
                {
                    { "PriceOrder", averagePriceFuture - multiplier * 4},
                    { "Side", Side.Buy },
                    { "WorkPartVolume", workPartVolumeIntraday },
                    { "ExecuteVolume", 0 },
                    { "PriceCounterOrder", Math.Round(averagePriceFuture - multiplier + multiplier * 2, 1, MidpointRounding.AwayFromZero)},
                    { "VolumeCounterOrder",  0},
                    { "OrderType", OrdersType.Order }

                });

                _ordersIntradayFuture.Add(new Dictionary<string, dynamic>()
                {
                    { "PriceOrder", averagePriceFuture - multiplier * 12},
                    { "Side", Side.Buy },
                    { "WorkPartVolume", workPartVolumeIntraday },
                    { "ExecuteVolume", 0 },
                    { "PriceCounterOrder", Math.Round(averagePriceFuture - multiplier + multiplier * 3, 1, MidpointRounding.AwayFromZero)},
                    { "VolumeCounterOrder",  0},
                    { "OrderType", OrdersType.Order }

                });

                _ordersIntradayFuture.Add(new Dictionary<string, dynamic>()
                {
                    { "PriceOrder", averagePriceFuture + multiplier },
                    { "Side", Side.Sell },
                    { "WorkPartVolume", workPartVolumeIntraday },
                    { "ExecuteVolume", 0 },
                    { "PriceCounterOrder", Math.Round(averagePriceFuture + multiplier - multiplier / 2, 1, MidpointRounding.AwayFromZero)},
                    { "VolumeCounterOrder",  0},
                    { "OrderType", OrdersType.Order }

                });

                _ordersIntradayFuture.Add(new Dictionary<string, dynamic>()
                {
                    { "PriceOrder", averagePriceFuture + multiplier * 2},
                    { "Side", Side.Sell },
                    { "WorkPartVolume", workPartVolumeIntraday },
                    { "ExecuteVolume", 0 },
                    { "PriceCounterOrder", Math.Round(averagePriceFuture + multiplier - multiplier, 1, MidpointRounding.AwayFromZero)},
                    { "VolumeCounterOrder",  0},
                    { "OrderType", OrdersType.Order }

                });

                _ordersIntradayFuture.Add(new Dictionary<string, dynamic>()
                {
                    { "PriceOrder", averagePriceFuture + multiplier * 4},
                    { "Side", Side.Sell },
                    { "WorkPartVolume", workPartVolumeIntraday },
                    { "ExecuteVolume", 0 },
                    { "PriceCounterOrder", Math.Round(averagePriceFuture + multiplier - multiplier * 2, 1, MidpointRounding.AwayFromZero)},
                    { "VolumeCounterOrder",  0},
                    { "OrderType", OrdersType.Order }

                });

                _ordersIntradayFuture.Add(new Dictionary<string, dynamic>()
                {
                    { "PriceOrder", averagePriceFuture + multiplier * 12},
                    { "Side", Side.Sell },
                    { "WorkPartVolume", workPartVolumeIntraday },
                    { "ExecuteVolume", 0 },
                    { "PriceCounterOrder", Math.Round(averagePriceFuture + multiplier - multiplier * 3, 1, MidpointRounding.AwayFromZero)},
                    { "VolumeCounterOrder",  0},
                    { "OrderType", OrdersType.Order }

                });


                /* _ordersIntradayFuture.Add(new List<dynamic> { averagePriceFuture - multiplier, Side.Buy, workPartVolumeIntraday, 0, Math.Round(averagePriceFuture - multiplier + multiplier / 2, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order});
                 _ordersIntradayFuture.Add(new List<dynamic> { averagePriceFuture - multiplier * 2, Side.Buy, workPartVolumeIntraday, 0, Math.Round(averagePriceFuture - multiplier * 2 + multiplier, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order });
                 _ordersIntradayFuture.Add(new List<dynamic> { averagePriceFuture - multiplier * 4, Side.Buy, workPartVolumeIntraday, 0, Math.Round(averagePriceFuture - multiplier * 4 + multiplier * 2, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order });
                 _ordersIntradayFuture.Add(new List<dynamic> { averagePriceFuture - multiplier * 12, Side.Buy, workPartVolumeIntraday, 0, Math.Round(averagePriceFuture - multiplier * 12 + multiplier * 3, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order });
                 _ordersIntradayFuture.Add(new List<dynamic> { averagePriceFuture + multiplier, Side.Sell, workPartVolumeIntraday, 0, Math.Round(averagePriceFuture + multiplier - multiplier / 2, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order });
                 _ordersIntradayFuture.Add(new List<dynamic> { averagePriceFuture + multiplier * 2, Side.Sell, workPartVolumeIntraday, 0, Math.Round(averagePriceFuture + multiplier * 2 - multiplier, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order });
                 _ordersIntradayFuture.Add(new List<dynamic> { averagePriceFuture + multiplier * 4, Side.Sell, workPartVolumeIntraday, 0, Math.Round(averagePriceFuture + multiplier * 4 - multiplier * 2, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order });
                 _ordersIntradayFuture.Add(new List<dynamic> { averagePriceFuture + multiplier * 12, Side.Sell, workPartVolumeIntraday, 0, Math.Round(averagePriceFuture + multiplier * 12 - multiplier * 3, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order });*/

                //_ordersIntradayFuture.Add(new ListOrders(averagePriceFuture - multiplier, Side.Buy, workPartVolumeIntraday, 0, Math.Round(averagePriceFuture - multiplier + multiplier / 2, 1, MidpointRounding.AwayFromZero), 0, OrdersType.Order));

                for (int i = 0; i < _ordersIntradayFuture.Count; i++)
                {
                    if (_ordersIntradayFuture[i]["Side"] == Side.Buy)
                    {
                        _tabPerp.BuyAtLimit(_ordersIntradayFuture[i]["WorkPartVolume"], _ordersIntradayFuture[i]["PriceOrder"]);
                    }
                    else
                    {
                        _tabPerp.SellAtLimit(_ordersIntradayFuture[i]["WorkPartVolume"], _ordersIntradayFuture[i]["PriceOrder"]);
                    }
                }

                _flagTradeIntraday = true;
            }
            else
            {
                Regime = DeribitPIUi.NameRegime.Off;
            }

        }

        #endregion

        private void AssemblyConstruction()
        {           
            if (_timeLifeAssemblyConsruction.AddMinutes(TimeLifeAssemblyConstruction) <= DateTime.Now)
            {                
                if (_flagConstruction == FlagConstruction.QuoteBuyOption || 
                    _flagConstruction == FlagConstruction.FirstBuyOption)
                {
                    CancelOptionOrder();
                    AddLogList(DateTime.Now + " Отмена ордера " + _tabOption.Tabs[_currentTab].Securiti.Name + " вышло время набора конструкции");

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
            
            if (_flagOptionChangeStrike && _flagConstruction != FlagConstruction.None)
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
                                        AddLogList(DateTime.Now + " Отмена ордера " + _tabOption.Tabs[_currentTab].Securiti.Name + " по смене страйка, когда частично позиция набрана");
                                    }
                                    
                                }
                            }
                        }                        
                    }
                    return;
                }
            }
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

                AddLogList(DateTime.Now + " Выставляем заявку на покупку " + _tabOption.Tabs[_currentTab].Securiti.Name + ", цена: " + priceOption + ", объем: " + volumeOption);                
            }

            if (_flagConstruction == FlagConstruction.QuoteBuyOption) // котируем опцион
            {               
                // если сменился текущий страйк отменяем ордер и начинаем все заново
                if (_tabOption.Tabs[_currentTab].Securiti.Name != CurrentStrike && !_flagOptionChangeStrike)
                {                    
                    if (PositionOptionSize > 0) // если страйк поменялся и если уже есть набранная позиция
                    {
                        _flagOptionChangeStrike = true;
                        _firstFixedStrike = _tabOption.Tabs[_currentTab].Securiti.Name;                                               
                    }
                    
                    CancelOptionOrder();
                    AddLogList(DateTime.Now + " Отмена ордера " + _tabOption.Tabs[_currentTab].Securiti.Name + " по смене страйка, когда позиций нет");
                    GetOptionMarkPrice();
                   
                    return;
                }

                // проверяем время жизни опционного ордера (в секундах) и высталяем опцион дороже
                if (_timeLifeOrderOption.AddSeconds(TimeOptionLimit) <= DateTime.Now && TimeOptionLimit != 0)
                {
                    if (_stepOrder > 2) // 2 - кол-во шагов изменения цены опциона, если превысили, начинаем котировать опцион от начальных условий
                    {
                        _stepOrder = 0;
                        AddLogList(DateTime.Now + " Отмена ордера " + _tabOption.Tabs[_currentTab].Securiti.Name + " по истечению времени ордера и достижению предела шагов изменений ордера");
                    }
                    else
                    {
                        _stepOrder += 1;
                        AddLogList(DateTime.Now + " Отмена ордера " + _tabOption.Tabs[_currentTab].Securiti.Name + " по истечению времени ордера");                        
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
                                AddLogList(DateTime.Now + " Отмена ордера " + _tabOption.Tabs[_currentTab].Securiti.Name + " по изменению теор цены = " + MarkPriceOption);
                            }
                            /*if (MarkPriceOption < _tabOption.Tabs[_currentTab].PositionsLast.EntryPrice || // нужно для тестов
                                MarkPriceOption >= _tabOption.Tabs[_currentTab].PositionsLast.EntryPrice + stepPrice)
                            {
                                CancelOptionOrder();
                                AddLogList(DateTime.Now + " Отмена ордера " + _tabOption.Tabs[_currentTab].Securiti.Name + " по изменению теор цены = " + MarkPriceOption);
                            }*/
                        }
                        else if (_stepOrder > 0)
                        {
                            if (MarkPriceOption - _tabOption.Tabs[_currentTab].PositionsLast.EntryPrice >= stepPrice)
                            {
                                CancelOptionOrder();
                                AddLogList(DateTime.Now + " Отмена ордера " + _tabOption.Tabs[_currentTab].Securiti.Name + " по изменению теор цены = " + MarkPriceOption);
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
                    AddLogList(DateTime.Now + " Выставляем рыночную заявку на покупку " + _tabPerp.Securiti.Name + ", объем: " + _futureSellOrderVolume);
                    return;
                }
                else if (_futureTimeOrderLimit.AddSeconds(TimeFuturesLimit) <= DateTime.Now)
                {
                    _tabPerp.SellAtMarket(_futureSellOrderVolume);
                    _flagConstruction = FlagConstruction.QuoteSellFuture;
                    AddLogList(DateTime.Now + " Выставляем рыночную заявку на покупку " + _tabPerp.Securiti.Name + ", объем: " + _futureSellOrderVolume);
                    return;
                }
                else
                {
                    DeribitServerRealization._postOnly = "true";
                    decimal priceFuture = GetFuturePrice();
                    _tabPerp.SellAtLimit(_futureSellOrderVolume, priceFuture);
                    _flagConstruction = FlagConstruction.QuoteSellFuture;
                    AddLogList(DateTime.Now + " Выставляем лимитную заявку на покупку " + _tabPerp.Securiti.Name + ", цена: " + priceFuture+ ", объем: " + _futureSellOrderVolume);
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
                            AddLogList(DateTime.Now + " Отмена ордера " + _tabPerp.Securiti.Name + ", цена ордера не лучшая в стакане. Цена ордера: " + _lastOrderPriceFuture + ", Best Ask: " + _bestAskPriceFuture);
                        }
                        if (_lastOrderPriceFuture == _bestAskPriceFuture && 
                            _futureSellOrderVolume != _bestAskVolumeFuture && 
                            _lastOrderPriceFuture - _tabPerp.Securiti.PriceStep >_bestBidPriceFuture) // здесь надо подумать, если кто-то встает в нашу цену, чтобы не менялся ордер
                        {
                            CancelFutureOrder();
                            AddLogList(DateTime.Now + " Отмена ордера " + _tabPerp.Securiti.Name + ", цена ордера такая же как и у других ордеров. Цена ордера: " + _lastOrderPriceFuture + ", Best Ask: " + _bestAskPriceFuture);
                        }
                            
                    }

                    // если лимитная заяка по фьючерсу не исполнилась, делаем заявку по маркету
                    if (_futureTimeOrderLimit.AddSeconds(TimeFuturesLimit) <= DateTime.Now)
                    {
                        CancelFutureOrder();
                        AddLogList(DateTime.Now + " Отмена ордера " + _tabPerp.Securiti.Name + " по истечению времени");
                        return;
                    }
                }
            }
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
    public class ListOrders
    {
        public decimal PriceOrder;
        public Side SideOrder;
        public decimal VolumeOrder;
        public decimal ExecuteVolume;
        public decimal PriceCounterOrder;
        public decimal VolumeCounterOrder;
        public OrdersType OrderType;
        public ListOrders(decimal priceOrder, Side sideOrder, decimal volumeOrder, decimal executeVolume, decimal priceCounterOrder, decimal volumeCounterOrder, OrdersType orderType)
        {
            PriceOrder = priceOrder;
            SideOrder = sideOrder;
            VolumeOrder = volumeOrder;
            ExecuteVolume = executeVolume;
            PriceCounterOrder = priceCounterOrder;
            VolumeCounterOrder = volumeCounterOrder;
            OrderType = orderType;
        }
    }
    public enum OrdersType
    {
        Order,
        ProfitOrder
    }
}
