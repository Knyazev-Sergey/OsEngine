using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using System.Threading;
using System;

namespace OsEngine.Robots.MarketRobots
{
    [Bot("CollectorLiquidity")]
    public class CollectorLiquidity : BotPanel
    {
        private BotTabSimple _tab;
        private StrategyParameterString _regime;
        private StrategyParameterInt _minBalance;
        private StrategyParameterString _isChoise;
        private StrategyParameterTimeOfDay _timeStartTrade;
        private StrategyParameterTimeOfDay _timeEndTrade;
        private StrategyParameterInt _timeUpdateAccount;
        private StrategyParameterTimeOfDay _tradeAtTime;        
        
        public CollectorLiquidity(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _regime = CreateParameter("Regime", "Off", new string[] { "Off", "On" });
            _minBalance = CreateParameter("Minimum balance", 100, 0, 10000, 1);
            _isChoise = CreateParameter("Choise time trade", "Interval", new string[] { "Interval", "At time" });
            _tradeAtTime = CreateParameterTimeOfDay("Trade at time", 18, 0, 0, 0);
            _timeStartTrade = CreateParameterTimeOfDay("Start trade time", 0, 0, 0, 0);
            _timeEndTrade = CreateParameterTimeOfDay("End trade time", 24, 0, 0, 0);
            _timeUpdateAccount = CreateParameter("Interval update account", 10, 1, 100, 1);            

            _tab.BestBidAskChangeEvent += _tab_BestBidAskChangeEvent;

            Thread worker = new Thread(StartThread) { IsBackground = true };
            worker.Start();
        }

        private void StartThread()
        {
            while (true)
            {
                if (_regime.ValueString == "Off")
                {
                    Thread.Sleep(1000);
                    continue;
                }

                if (_tab.Security == null)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                if (_bestAskLqdt == 0 ||
                    _bestBidLqdt == 0)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                if (IsCheck())
                {
                    CheckAccount();                    
                }

                Thread.Sleep(5000);
            }
        }

        private bool _isCheckAtTime = true;

        private DateTime _timeInterval = DateTime.Now;

        private bool IsCheck()
        {
            if (_isChoise.ValueString == "At time")
            {
                if (!_isCheckAtTime &&
                DateTime.Now.Hour == new TimeSpan(6, 0, 0).Hours) // обновление флага на срабатывание по определенному времени
                {
                    _isCheckAtTime = true;
                }

                if (isTime() &&
                    _isCheckAtTime) // проверка на конкретное время 
                {
                    _isCheckAtTime = false;
                    return true;
                }
            }
            else
            {
                if (_timeStartTrade.Value < DateTime.Now &&
                    _timeEndTrade.Value > DateTime.Now)
                {
                    if (_timeInterval.AddMinutes(_timeUpdateAccount.ValueInt) <= DateTime.Now)
                    {
                        _timeInterval = DateTime.Now;
                        return true;
                    }                    
                }
                else
                {
                    return false;
                }

                _isCheckAtTime = true; // чтобы можно было активировать режим "At time" без перезагрузки 
            }

            return false;
        }

        private bool isTime()
        {
            DateTime timeNow = DateTime.Now;

            if (_tradeAtTime.Value.Hour == timeNow.Hour &&
                _tradeAtTime.Value.Minute == timeNow.Minute)
            {
                return true;
            }

            return false;
        }

        private void CheckAccount()
        {
            try
            {
                decimal balance = GetPortfolioValue();

                if (balance <= 0)
                {
                    return;
                }

                if (balance > _minBalance.ValueInt) // проверяем чтобы баланс был выше минимального значения
                {
                    decimal volume = Math.Floor((balance - _minBalance.ValueInt) / _bestAskLqdt);

                    if (volume <= 0)
                    {
                        return;
                    }

                    BuyLqdt(volume);
                }
                else if (_tab.PositionOpenLong.Count > 0 && 
                         balance < _minBalance.ValueInt)
                {
                    decimal volume = Math.Ceiling(Math.Abs(balance - _minBalance.ValueInt) / _bestBidLqdt);

                    if (volume <= 0)
                    {
                        return;
                    }

                    SellLqdt(volume);
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage("CheckAccount: " + ex.Message, Logging.LogMessageType.Error);
            }
        }

        private decimal GetPortfolioValue()
        {
            if (_tab.Connector.MyServer.ServerType == Market.ServerType.Alor)
            {
                var positions = _tab.Portfolio.GetPositionOnBoard();

                if (positions.Count > 0)
                {
                    for (int i = 0; i < positions.Count; i++)
                    {
                        if (positions[i].SecurityNameCode == "RUB")
                        {
                            return positions[i].ValueCurrent;
                        }
                    }
                }
            }

            if (_tab.Connector.MyServer.ServerType == Market.ServerType.TinkoffInvestments)
            {
                var positions = _tab.Portfolio.GetPositionOnBoard();

                if (positions.Count > 0)
                {
                    for (int i = 0; i < positions.Count; i++)
                    {
                        if (positions[i].SecurityNameCode == "rub")
                        {
                            return positions[i].ValueCurrent;
                        }
                    }
                }
            }

            if (_tab.Connector.MyServer.ServerType == Market.ServerType.Transaq)
            {
                return _tab.Portfolio.ValueCurrent - _tab.Portfolio.ValueBlocked;
            }

            return 0;
        }

        private void BuyLqdt(decimal volume)
        {
            try
            {
                if (_tab.PositionOpenLong.Count > 0)
                {                    
                    _tab.BuyAtMarketToPosition(_tab.PositionOpenLong[0], volume);
                }
                else
                {
                    _tab.BuyAtMarket(volume);
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage("BuyLqdt: " + ex.Message, Logging.LogMessageType.Error);
            }  
        }

        private void SellLqdt(decimal volume)
        {
            try
            {
                if (volume > _tab.PositionOpenLong[0].OpenVolume)
                {
                    volume = _tab.PositionOpenLong[0].OpenVolume;
                }

                _tab.CloseAtMarket(_tab.PositionOpenLong[0], volume);
            }
            catch (Exception ex)
            {
                SendNewLogMessage("SellLqdt: " + ex.Message, Logging.LogMessageType.Error);
            }
        }

        private decimal _bestBidLqdt;

        private decimal _bestAskLqdt;

        private void _tab_BestBidAskChangeEvent(decimal bid, decimal ask)
        {
            _bestBidLqdt = bid;
            _bestAskLqdt = ask;
        }

        public override string GetNameStrategyType()
        {
            return "CollectorLiquidity";
        }

        public override void ShowIndividualSettingsDialog()
        {            
        }
    }
}
