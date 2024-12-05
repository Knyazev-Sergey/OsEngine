using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using System.Threading;
using System;
using System.Windows.Documents;

namespace OsEngine.Robots.MarketRobots
{
    [Bot("CollectorLiquidity")]
    public class CollectorLiquidity : BotPanel
    {
        private BotTabSimple _tab;
        private StrategyParameterString _regime;
        private StrategyParameterInt _minBalance;
        private StrategyParameterTimeOfDay _timeStartTrade;
        private StrategyParameterTimeOfDay _timeEndTrade;
        private StrategyParameterInt _timeUpdateAccount;
        private decimal _bestBidLqdt;
        private decimal _bestAskLqdt;
        
        public CollectorLiquidity(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _regime = CreateParameter("Regime", "Off", new string[] { "Off", "On" });
            _minBalance = CreateParameter("Minimum balance", 100, 0, 10000, 1);
            _timeStartTrade = CreateParameterTimeOfDay("Start trade time", 0, 0, 0, 0);
            _timeEndTrade = CreateParameterTimeOfDay("End trade time", 24, 0, 0, 0);
            _timeUpdateAccount = CreateParameter("Time update account", 10, 1, 100, 1);

            _tab.BestBidAskChangeEvent += _tab_BestBidAskChangeEvent;

            Thread worker = new Thread(StartThread) { IsBackground = true };
            worker.Start();
        }

        private void StartThread()
        {
            bool isUpdate = false;

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

                if (_timeStartTrade.Value > _tab.TimeServerCurrent ||
                    _timeEndTrade.Value < _tab.TimeServerCurrent)
                {
                    Thread.Sleep(10000);
                    continue;
                }

                /*if (!isUpdate)
                {
                    continue;
                }*/

                CheckAccount();
                SendNewLogMessage($"Bid: {_bestBidLqdt}, Ask: {_bestAskLqdt}", Logging.LogMessageType.Error);

                Thread.Sleep(3000);
            }
        }

        private void CheckAccount()
        {
            try
            {
                decimal value = GetPortfolioValue();

                SendNewLogMessage(value.ToString(), Logging.LogMessageType.Error);

                if (value > _minBalance.ValueInt &&
                    value - _minBalance.ValueInt > 1) // проверяем чтобы баланс был выше минимального значения и допускаем погрешность в 1 рубль
                {
                    decimal volume = Math.Floor((value - _minBalance.ValueInt) / _bestAskLqdt);
                    SendNewLogMessage($"Buy: {volume}", Logging.LogMessageType.Error);
                    //BuyLqdt(volume);
                }
                else if (value < _minBalance.ValueInt &&
                    value - _minBalance.ValueInt < 1)
                {
                    decimal volume = Math.Floor(Math.Abs(value - _minBalance.ValueInt) / _bestBidLqdt);
                    SendNewLogMessage($"Sell: {volume}", Logging.LogMessageType.Error);
                    //SellLqdt(volume);
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage($"CheckAccount: {ex.Message}", Logging.LogMessageType.Error);
            }
        }

        private decimal GetPortfolioValue()
        {
            var positions = _tab.Portfolio.GetPositionOnBoard();

            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i].SecurityNameCode == "RUB")
                {
                    return positions[i].ValueCurrent;
                }
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

                else if (_tab.PositionOpenShort.Count > 0)
                {
                    decimal volumeClose = _tab.PositionOpenShort[0].OpenVolume;
                    _tab.CloseAtMarket(_tab.PositionOpenShort[0], volumeClose);

                    if (volume > volumeClose)
                    {
                        decimal volumeOpen = volume - volumeClose;
                        _tab.BuyAtMarket(volumeOpen);
                    }
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage($"BuyLqdt: {ex.Message}", Logging.LogMessageType.Error);
            }  
        }

        private void SellLqdt(decimal volume)
        {
            try
            {
                if (_tab.PositionOpenShort.Count > 0)
                {
                    _tab.SellAtMarketToPosition(_tab.PositionOpenShort[0], volume);
                }

                else if (_tab.PositionOpenLong.Count > 0)
                {
                    decimal volumeClose = _tab.PositionOpenLong[0].OpenVolume;
                    _tab.CloseAtMarket(_tab.PositionOpenLong[0], volumeClose);

                    if (volume > volumeClose)
                    {
                        decimal volumeOpen = volume - volumeClose;
                        _tab.SellAtMarket(volumeOpen);
                    }
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage($"SellLqdt: {ex.Message}", Logging.LogMessageType.Error);
            }
        }

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
