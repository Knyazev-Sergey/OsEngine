using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using System.Threading;
using System;
using OsEngine.Charts.CandleChart.Indicators;

namespace OsEngine.Robots.MarketRobots
{
    [Bot("CollectorLiquidity")]
    public class CollectorLiquidity : BotPanel
    {
        private BotTabSimple _tab;
        private StrategyParameterString _regime;
        private StrategyParameterInt _minBalance;
        private decimal _bestBidLqdt;
        private decimal _bestAskLqdt;
        
        public CollectorLiquidity(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _regime = CreateParameter("Regime", "Off", new string[] { "Off", "On" });
            _minBalance = CreateParameter("Minimum balance", 100, 0, 10000, 1);

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

                CheckAccount();
                SendNewLogMessage($"Bid: {_bestBidLqdt}, Ask: {_bestAskLqdt}", Logging.LogMessageType.Error);

                Thread.Sleep(3000);
            }
        }

        private void CheckAccount()
        {
            try
            {
                decimal value = _tab.Portfolio.ValueCurrent;

                SendNewLogMessage(value.ToString(), Logging.LogMessageType.Error);

                if (value > _minBalance.ValueInt &&
                    value - _minBalance.ValueInt > 1) // проверяем чтобы баланс был выше минимального значения и допускаем погрешность в 1 рубль
                {
                    decimal volume = Math.Floor((value - _minBalance.ValueInt) / _bestAskLqdt);
                    BuyLqdt(volume);
                }
                else if (value < _minBalance.ValueInt &&
                    value - _minBalance.ValueInt < 1)
                {
                    decimal volume = Math.Floor(Math.Abs(value - _minBalance.ValueInt) / _bestBidLqdt);
                    SellLqdt(volume);
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage($"CheckAccount: {ex.Message}", Logging.LogMessageType.Error);
            }
        }

        private void BuyLqdt(decimal volume)
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

        private void SellLqdt(decimal volume)
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
