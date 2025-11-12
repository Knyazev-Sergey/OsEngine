using OsEngine.Entity;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;

namespace OsEngine.Indicators.Samples
{
    [Indicator("MobilityIndicator")]
    public class MobilityIndicator : Aindicator
    {
        public IndicatorDataSeries _series;
        private List<double> _price = new();
        private double _priceCandleClose;
        private double _priceBid;
        private double _priceAsk;
        private DateTime _timeStart;
        private BotTabSimple _tab;
        private decimal _lastValue;
        private int _timeCalculatePrice;
        private int _timeTradeSeccion;
        private int _dayTrade;

        public override void OnStateChange(IndicatorState state)
        {
            if(state == IndicatorState.Configure)
            {
                _series = CreateSeries("Mobility", System.Drawing.Color.AliceBlue, IndicatorChartPaintType.Line, true);

                _timeCalculatePrice = CreateParameterInt("Время расчета цены в минутах", 2);
                _timeTradeSeccion = CreateParameterInt("Длительность торговой сессии в минутах", 1440);
                _dayTrade = CreateParameterInt("Количество торговых дней", 365);
            }
        }

        public override void OnProcess(List<Candle> source, int index)
        {
            if (_timeStart.AddMinutes(_timeCalculatePrice) <= DateTime.UtcNow)
            {
                _priceCandleClose = (double)source[^1].Close;
                _timeStart = DateTime.UtcNow;

                decimal value = (decimal)CalculateMobility();
                _series.Values[index] = value;
                _lastValue = value;
                _price = new();
            }
            else
            {
                if (_lastValue != 0)
                {
                    _series.Values[index] = _lastValue;
                }                
            }
        }

        internal void SendTab(BotTabSimple tab)
        {
            tab.MarketDepthUpdateEvent += _tab_MarketDepthUpdateEvent;
            tab.SecuritySubscribeEvent += _tab_SecuritySubscribeEvent;

            _tab = tab;
        }

        private void _tab_SecuritySubscribeEvent(Security obj)
        {
            _price = new();
            _timeStart = new();
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

        private double CalculateMobility()
        {
            double totalPrice = 0;

            for (int i = 1; i < _price.Count; i++)
            {
                totalPrice += Math.Pow(_price[i] - _price[i - 1], 2);
            }

            double dt = totalPrice / (_price.Count - 2);
            double countPeriods = _timeTradeSeccion / _timeCalculatePrice;
            double mt = Math.Sqrt(dt * countPeriods);

            double m = mt * Math.Sqrt(_dayTrade) / _priceCandleClose * 100;

            return m;
        }
    }
}