using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;

namespace OsEngine.Robots
{
    [Bot("Mobility")]

    public class Mobility : BotPanel
    {
        private BotTabSimple _tab;
        private List<double> _price = new();
        private bool _finishCandle = false;
        private double _priceCandleClose;
        private double _priceBid;
        private double _priceAsk;
        private DateTime _timeStart;

        public Mobility(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _tab.MarketDepthUpdateEvent += _tab_MarketDepthUpdateEvent;
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tab.SecuritySubscribeEvent += _tab_SecuritySubscribeEvent;
        }

        private void _tab_SecuritySubscribeEvent(Security obj)
        {
            _price = new();
            _timeStart = new();
        }

        private void _tab_CandleFinishedEvent(List<Candle> obj)
        {            
            if (_timeStart.AddMinutes(2) <= DateTime.UtcNow)
            {
                _finishCandle = true;
                _priceCandleClose = (double)obj[^1].Close;
                _timeStart = DateTime.UtcNow;
                
                CalculateDT();
                _price = new();
            }            
        }

        private void _tab_MarketDepthUpdateEvent(MarketDepth obj)
        {
            if (_priceAsk == obj.Asks[0].Price && _priceBid == obj.Bids[0].Price) return;

            _priceBid = obj.Bids[0].Price;
            _priceAsk = obj.Asks[0].Price;

            CalculatePrice();
        }

        private void CalculatePrice()
        {
            if (_priceAsk == 0 || _priceBid == 0) return;

            double lastPrice = 0;
            
            if (_tab.CandlesAll == null || _tab.CandlesAll.Count == 0)
            {
                lastPrice = (_priceAsk - _priceBid) / 2;
            }
            else
            {
                lastPrice = (double)_tab.CandlesAll[^1].Close;
            }

            double price = lastPrice;

            if (lastPrice > _priceAsk)
            {
                price = _priceAsk;
            }
            else if (lastPrice < _priceBid)
            {
                price = _priceBid;
            }

            _price.Add(price);
        }

        private void CalculateDT()
        {
            double totalPrice = 0;

            for (int i = 1; i < _price.Count; i++)
            {
                totalPrice += Math.Pow(_price[i] - _price[i - 1], 2);
            }

            double dt = totalPrice / (_price.Count - 2);
            double countPeriods = 1440 / 2;
            double mt = Math.Sqrt(dt * countPeriods);

            double m = mt * Math.Sqrt(365) / _priceCandleClose * 100;

            SendNewLogMessage($"dT = {dt}, mT = {mt}, m = {m}", Logging.LogMessageType.Error);
        }
    }
}
