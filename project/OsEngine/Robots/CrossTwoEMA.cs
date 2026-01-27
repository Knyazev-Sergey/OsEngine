using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Threading;

namespace OsEngine.Robots
{
    [Bot("CrossTwoEMA")]
    public class CrossTwoEMA : BotPanel
    {
        private Logging.LogMessageType _logType = Logging.LogMessageType.NoName;
        private Logging.LogMessageType _logTelegram = Logging.LogMessageType.User;
        private StartProgram _startProgram;
        private BotTabSimple _tab;
        private StrategyParameterString _regime;
        private StrategyParameterString _sideOrder;
        private StrategyParameterInt _fastEMA;
        private StrategyParameterInt _slowEMA;
        private StrategyParameterInt _countCandles;
        private StrategyParameterDecimal _distanceToStopLoss;
        private StrategyParameterDecimal _riskDeal;
        private StrategyParameterString _tradeAssetInPortfolio;
        private StrategyParameterString _typeTP;
        private StrategyParameterDecimal _coefTP;
        private StrategyParameterDecimal _lostDealOfSL;
        private StrategyParameterBool _boolLogCrossEMA;

        private Aindicator _emaFast;
        private Aindicator _emaSlow;

        private decimal _valueSL;

        public CrossTwoEMA(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _startProgram = startProgram;

            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            this.ParamGuiSettings.Title = "Cross Two EMA";
            this.ParamGuiSettings.Height = 400;
            this.ParamGuiSettings.Width = 300;

            string tabNameParameters = " Параметры ";

            _regime = CreateParameter("Режим", "Off", new string[] { "Off", "On" }, tabNameParameters);
            _sideOrder = CreateParameter("Направление сделки", Side.Buy.ToString(), new string[] { Side.Buy.ToString(), Side.Sell.ToString() }, tabNameParameters);
            _fastEMA = CreateParameter("Быстрая EMA", 14, 0, 0, 0, tabNameParameters);
            _slowEMA = CreateParameter("Медленная EMA", 50, 0, 0, 0, tabNameParameters);
            _countCandles = CreateParameter("Количество свечей (SL)", 10, 0, 0, 0, tabNameParameters);
            _distanceToStopLoss = CreateParameter("Расстояние от локального уровня до SL (в %)", 2m, 0m, 0m, 0m, tabNameParameters);
            _riskDeal = CreateParameter("Риск на сделку (в %)", 10m, 0m, 0m, 0m, tabNameParameters);
            _tradeAssetInPortfolio = CreateParameter("Asset in portfolio", "Prime", tabNameParameters);
            _typeTP = CreateParameter("Тип TP", TypeTP.Coeficient.ToString(), new string[] { TypeTP.Coeficient.ToString(), TypeTP.CrossEMA.ToString() }, tabNameParameters);
            _coefTP = CreateParameter("Коэффициент TP", 1m, 0m, 0m, 0m, tabNameParameters);
            _lostDealOfSL = CreateParameter("Пропускаем сделку, если SL > (в %)", 1m, 0m, 0m, 0m, tabNameParameters);
            _boolLogCrossEMA = CreateParameter("Присылать в телеграмм сообщение о пересечении EMA", false, tabNameParameters);

            _emaFast = IndicatorsFactory.CreateIndicatorByName("Ema", name + "EmaFast", false);
            _emaFast = (Aindicator)_tab.CreateCandleIndicator(_emaFast, "Prime");
            ((IndicatorParameterInt)_emaFast.Parameters[0]).ValueInt = _fastEMA;
            _emaFast.Save();

            _emaSlow = IndicatorsFactory.CreateIndicatorByName("Ema", name + "EmaSlow", false);
            _emaSlow = (Aindicator)_tab.CreateCandleIndicator(_emaSlow, "Prime");
            ((IndicatorParameterInt)_emaSlow.Parameters[0]).ValueInt = _slowEMA;
            _emaSlow.Save();

            _tab.ManualPositionSupport.DisableManualSupport();
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;

            ParametrsChangeByUser += CrossTwoEMA_ParametrsChangeByUser;

            Thread mainThread = new Thread(MainThread);
            mainThread.Start();
        }

        private void CrossTwoEMA_ParametrsChangeByUser()
        {
            ((IndicatorParameterInt)_emaFast.Parameters[0]).ValueInt = _fastEMA;
            _emaFast.Save();
            _emaFast.Reload();

            ((IndicatorParameterInt)_emaSlow.Parameters[0]).ValueInt = _slowEMA;
            _emaSlow.Save();
            _emaSlow.Reload();
        }

        private bool _isOpenOrderLong = false;
        private bool _isCloseOrderLong = true;
        private bool _isOpenOrderShort = false;
        private bool _isCloseOrderShort = true;

        private void MainThread()
        {
            while (true)
            {
                try
                {
                    if (_startProgram == StartProgram.IsOsTrader)
                    {
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }

                    if (_tab == null)
                    {
                        continue;
                    }

                    if (_tab.Security == null)
                    {
                        continue;
                    }

                    if (_regime == "Off")
                    {
                        WithdrawOrders();
                        continue;
                    }

                    LogicLongPosition();
                    LogicShortPosition();
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                    Thread.Sleep(5000);
                }
            }
        }

        private void WithdrawOrders()
        {
            if (_tab.PositionOpenLong.Count > 0)
            {
                if (_tab.PositionOpenLong[0].StopOrderIsActive)
                {
                    _tab.PositionOpenLong[0].StopOrderIsActive = false;
                    _tab.PositionOpenLong[0].StopOrderPrice = 0;
                    _tab.PositionOpenLong[0].StopOrderRedLine = 0;

                    _tab.PositionOpenLong[0].ProfitOrderIsActive = false;
                    _tab.PositionOpenLong[0].ProfitOrderPrice = 0;
                    _tab.PositionOpenLong[0].ProfitOrderRedLine = 0;
                }
            }

            if (_tab.PositionOpenShort.Count > 0)
            {
                _tab.PositionOpenShort[0].StopOrderIsActive = false;
                _tab.PositionOpenShort[0].StopOrderPrice = 0;
                _tab.PositionOpenShort[0].StopOrderRedLine = 0;

                _tab.PositionOpenShort[0].ProfitOrderIsActive = false;
                _tab.PositionOpenShort[0].ProfitOrderPrice = 0;
                _tab.PositionOpenShort[0].ProfitOrderRedLine = 0;
            }
        }

        private void LogicLongPosition()
        {
            if (_tab.PositionOpenLong.Count > 0)
            {
                if (_typeTP == TypeTP.CrossEMA.ToString())
                {
                    if (_tab.PositionOpenLong[0].ProfitOrderIsActive)
                    {
                        _tab.PositionOpenLong[0].ProfitOrderIsActive = false;
                        _tab.PositionOpenLong[0].ProfitOrderPrice = 0;
                        _tab.PositionOpenLong[0].ProfitOrderRedLine = 0;

                        SendNewLogMessage("Включен режим ТР = CrossEMA", _logType);
                    }
                }

                SetLongSL();
                SetLongTP();

                if (!_isOpenOrderLong && _tab.PositionOpenLong[0].State == PositionStateType.Open)
                {
                    SendNewLogMessage($"Позиция Long открылась, по цене {_tab.PositionOpenLong[0].EntryPrice}, объем {_tab.PositionOpenLong[0].OpenVolume}", _logTelegram);
                    _isOpenOrderLong = true;
                    _isCloseOrderLong = false;
                }
            }
            else
            {
                if (_tab.PositionsCloseAll.Count > 0)
                {
                    if (!_isCloseOrderLong && _tab.PositionsCloseAll[^1].State == PositionStateType.Done)
                    {
                        SendNewLogMessage($"Позиция Long закрылась, по цене {_tab.PositionsCloseAll[^1].ClosePrice}", _logTelegram);
                        _isOpenOrderLong = false;
                        _isCloseOrderLong = true;
                    }
                }
            }
        }

        private void SetLongSL()
        {
            if (!_tab.PositionOpenLong[0].StopOrderIsActive)
            {
                if (_valueSL == 0) return;
                Position pos = _tab.PositionOpenLong[0];
                _tab.CloseAtStopMarket(pos, _valueSL);

                SendNewLogMessage($"Выставлен StopLoss по Long, цена: {_valueSL}", _logType);
            }
        }

        private void SetLongTP()
        {
            if (!_tab.PositionOpenLong[0].ProfitOrderIsActive)
            {
                if (_typeTP == TypeTP.Coeficient.ToString())
                {
                    if (_coefTP == 0)
                    {
                        _coefTP.ValueDecimal = 1;
                    }

                    if (_valueSL == 0) return;

                    decimal priceTP = (_tab.PositionOpenLong[0].EntryPrice - _valueSL) * _coefTP + _tab.PositionOpenLong[0].EntryPrice;
                    Position pos = _tab.PositionOpenLong[0];
                    _tab.CloseAtProfitMarket(pos, priceTP);

                    SendNewLogMessage($"Выставлен TakeProfit по Long, цена: {priceTP}", _logType);
                }
            }
        }

        private void LogicShortPosition()
        {
            if (_tab.PositionOpenShort.Count > 0)
            {
                if (_typeTP == TypeTP.CrossEMA.ToString())
                {
                    if (_tab.PositionOpenShort[0].ProfitOrderIsActive)
                    {
                        _tab.PositionOpenShort[0].ProfitOrderIsActive = false;
                        _tab.PositionOpenShort[0].ProfitOrderPrice = 0;
                        _tab.PositionOpenShort[0].ProfitOrderRedLine = 0;

                        SendNewLogMessage("Включен режим ТР = CrossEMA", _logType);
                    }
                }

                SetShortSL();
                SetShortTP();

                if (!_isOpenOrderShort && _tab.PositionOpenShort[0].State == PositionStateType.Open)
                {
                    SendNewLogMessage($"Позиция Short открылась, по цене {_tab.PositionOpenShort[0].EntryPrice}, объем {_tab.PositionOpenShort[0].OpenVolume}", _logTelegram);
                    _isOpenOrderShort = true;
                    _isCloseOrderShort = false;
                }
            }
            else
            {
                if (_tab.PositionsCloseAll.Count > 0)
                {
                    if (!_isCloseOrderShort && _tab.PositionsCloseAll[^1].State == PositionStateType.Done)
                    {
                        SendNewLogMessage($"Позиция Short закрылась, по цене {_tab.PositionsCloseAll[^1].ClosePrice}", _logTelegram);
                        _isOpenOrderShort = false;
                        _isCloseOrderShort = true;
                    }
                }
            }            
        }

        private void SetShortSL()
        {
            if (!_tab.PositionOpenShort[0].StopOrderIsActive)
            {
                if (_valueSL == 0) return;

                Position pos = _tab.PositionOpenShort[0];
                _tab.CloseAtStopMarket(pos, _valueSL);

                SendNewLogMessage($"Выставлен StopLoss по Long, цена: {_valueSL}", _logType);
            }
        }

        private void SetShortTP()
        {
            if (!_tab.PositionOpenShort[0].ProfitOrderIsActive)
            {
                if (_typeTP == TypeTP.Coeficient.ToString())
                {
                    if (_coefTP == 0)
                    {
                        _coefTP.ValueDecimal = 1;
                    }

                    if (_valueSL == 0) return;

                    decimal priceTP = _tab.PositionOpenShort[0].EntryPrice - (_valueSL - _tab.PositionOpenShort[0].EntryPrice) * _coefTP;
                    Position pos = _tab.PositionOpenShort[0];
                    _tab.CloseAtProfitMarket(pos, priceTP);

                    SendNewLogMessage($"Выставлен TakeProfit по Long, цена: {priceTP}", _logType);
                }
            }
        }

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            try
            {
                decimal fastEmaLast = _emaFast.DataSeries[0].Values[^1];
                decimal fastEmaPrev = _emaFast.DataSeries[0].Values[^2];
                decimal slowEmaLast = _emaSlow.DataSeries[0].Values[^1];
                decimal slowEmaPrev = _emaSlow.DataSeries[0].Values[^2];

                if (_boolLogCrossEMA)
                {
                    if (fastEmaLast > fastEmaPrev &&
                        fastEmaLast > slowEmaLast &&
                        fastEmaPrev < slowEmaPrev)
                    {
                        SendNewLogMessage($"Быстрая EMA пересекла медленную EMA снизу вверх", _logTelegram);
                    }

                    if (fastEmaLast < fastEmaPrev &&
                        fastEmaLast < slowEmaLast &&
                        fastEmaPrev > slowEmaPrev)
                    {
                        SendNewLogMessage($"Быстрая EMA пересекла медленную EMA сверху вниз", _logTelegram);
                    }
                }

                if (_regime == "Off") return;

                if (_emaFast.DataSeries[0].Values[^1] == 0 || _emaSlow.DataSeries[0].Values[^1] == 0) return;

                if (_sideOrder == Side.Buy.ToString())
                {
                    TrySendOrderBuy(candles);
                }

                if (_sideOrder == Side.Sell.ToString())
                {
                    TrySendOrderSell(candles);
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void TrySendOrderBuy(List<Candle> candles)
        {
            decimal fastEmaLast = _emaFast.DataSeries[0].Values[^1];
            decimal fastEmaPrev = _emaFast.DataSeries[0].Values[^2];
            decimal slowEmaLast = _emaSlow.DataSeries[0].Values[^1];
            decimal slowEmaPrev = _emaSlow.DataSeries[0].Values[^2];

            if (_tab.PositionOpenLong.Count == 0)
            {
                if (fastEmaLast > fastEmaPrev &&
                    fastEmaLast > slowEmaLast &&
                    fastEmaPrev < slowEmaPrev)
                {
                    _valueSL = GetValueLongSL(candles);

                    if (_valueSL == 0) return;

                    decimal propusk = candles[^1].Close - candles[^1].Close * _lostDealOfSL / 100;

                    SendNewLogMessage($"Last Price: {candles[^1].Close}, StopLoss: {_valueSL}, Уровень пропуска сделки: {propusk}", _logType);

                    if (_valueSL < propusk)
                    {
                        SendNewLogMessage($"Пропускаем сделку Long. StopLoss ({_valueSL}) меньше чем уровень пропуска ({propusk}).", _logTelegram);
                        SendNewLogMessage($"{candles[^1].TimeStart}: Пропускаем сделку Long. StopLoss ({_valueSL}) меньше чем уровень пропуска ({propusk}).", _logType);
                        return;
                    }

                    decimal volume = GetVolume();

                    if (volume == 0)
                    {
                        return;
                    }

                    _tab.BuyAtMarket(volume);
                }
            }
            else
            {
                if (_typeTP == TypeTP.CrossEMA.ToString())
                {
                    if (fastEmaLast < fastEmaPrev &&
                        fastEmaLast < slowEmaLast &&
                        fastEmaPrev > slowEmaLast)
                    {
                        Position pos = _tab.PositionOpenLong[0];
                        decimal volume = pos.OpenVolume;
                        _tab.CloseAtMarket(pos, volume);
                    }
                }
            }
        }

        private decimal GetValueLongSL(List<Candle> candles)
        {
            decimal valueMin = decimal.MaxValue;

            if (candles.Count < _countCandles) return 0;

            for (int i = candles.Count - 1; i >= candles.Count - 1 - _countCandles; i--)
            {
                if (candles[i].Low < valueMin)
                {
                    valueMin = candles[i].Low;
                }
            }

            SendNewLogMessage($"Время последней свечи: {candles[^1].TimeStart}, цена закрытия последней свечи: {candles[^1].Close} макс. значение {valueMin}, за {_countCandles.ValueInt} свечей.", _logType);

            decimal delta = candles[^1].Close * _distanceToStopLoss / 100;

            return Math.Round(valueMin - delta, _tab.Security.Decimals);
        }

        private void TrySendOrderSell(List<Candle> candles)
        {
            decimal fastEmaLast = _emaFast.DataSeries[0].Values[^1];
            decimal fastEmaPrev = _emaFast.DataSeries[0].Values[^2];
            decimal slowEmaLast = _emaSlow.DataSeries[0].Values[^1];
            decimal slowEmaPrev = _emaSlow.DataSeries[0].Values[^2];

            if (_tab.PositionOpenShort.Count == 0)
            {
                if (fastEmaLast < fastEmaPrev &&
                    fastEmaLast < slowEmaLast &&
                    fastEmaPrev > slowEmaPrev)
                {
                    _valueSL = GetValueShortSL(candles);

                    if (_valueSL == 0) return;

                    decimal propusk = candles[^1].Close + candles[^1].Close * _lostDealOfSL / 100;

                    SendNewLogMessage($"Last Price: {candles[^1].Close}, StopLoss: {_valueSL}, Уровень пропуска сделки: {propusk}", _logType);

                    if (_valueSL > propusk)
                    {
                        SendNewLogMessage($"Пропускаем сделку Short. StopLoss ({_valueSL}) больше чем уровень пропуска ({propusk}).", _logTelegram);
                        SendNewLogMessage($"{candles[^1].TimeStart}: Пропускаем сделку Short. StopLoss ({_valueSL}) больше чем уровень пропуска ({propusk}).", _logType);
                        return;
                    }

                    decimal volume = GetVolume();

                    if (volume == 0)
                    {
                        return;
                    }

                    _tab.SellAtMarket(volume);
                }
            }
            else
            {
                if (_typeTP == TypeTP.CrossEMA.ToString())
                {
                    if (fastEmaLast > fastEmaPrev &&
                        fastEmaLast > slowEmaLast &&
                        fastEmaPrev < slowEmaLast)
                    {
                        Position pos = _tab.PositionOpenShort[0];
                        decimal volume = pos.OpenVolume;
                        _tab.CloseAtMarket(pos, volume);
                    }
                }
            }
        }

        private decimal GetValueShortSL(List<Candle> candles)
        {
            decimal valueMax = 0;

            if (candles.Count < _countCandles) return 0;

            for (int i = candles.Count - 1; i >= candles.Count - 1 - _countCandles; i--)
            {
                if (candles[i].High > valueMax)
                {
                    valueMax = candles[i].High;
                }
            }

            SendNewLogMessage($"Время последней свечи: {candles[^1].TimeStart}, цена закрытия последней свечи: {candles[^1].Close}, макс. значение {valueMax} за {_countCandles.ValueInt} свечей.", _logType);

            decimal delta = candles[^1].Close * _distanceToStopLoss / 100;

            return Math.Round(valueMax + delta, _tab.Security.Decimals);
        }

        private decimal GetVolume()
        {
            Portfolio myPortfolio = _tab.Portfolio;

            if (myPortfolio == null)
            {
                return 0;
            }

            decimal portfolioPrimeAsset = 0;

            if (_tradeAssetInPortfolio == "Prime")
            {
                portfolioPrimeAsset = myPortfolio.ValueCurrent;
            }
            else
            {
                List<PositionOnBoard> positionOnBoard = myPortfolio.GetPositionOnBoard();

                if (positionOnBoard == null)
                {
                    return 0;
                }

                for (int i = 0; i < positionOnBoard.Count; i++)
                {
                    if (positionOnBoard[i].SecurityNameCode == _tradeAssetInPortfolio)
                    {
                        portfolioPrimeAsset = positionOnBoard[i].ValueCurrent;
                        break;
                    }
                }
            }

            if (portfolioPrimeAsset == 0)
            {
                SendNewLogMessage("Can`t found portfolio " + _tradeAssetInPortfolio, Logging.LogMessageType.Error);
                return 0;
            }
            decimal moneyOnPosition = portfolioPrimeAsset * (_riskDeal.ValueDecimal / 100);

            decimal qty = moneyOnPosition / Math.Abs(_tab.PriceBestAsk - _valueSL) / _tab.Security.Lot;
                        
            if (_tab.Security.UsePriceStepCostToCalculateVolume == true
                && _tab.Security.PriceStep != _tab.Security.PriceStepCost
                && _tab.PriceBestAsk != 0
                && _tab.Security.PriceStep != 0
                && _tab.Security.PriceStepCost != 0)
            {// расчёт количества контрактов для фьючерсов и опционов на Мосбирже
                qty = moneyOnPosition / (Math.Abs(_tab.PriceBestAsk - _valueSL) / _tab.Security.PriceStep * _tab.Security.PriceStepCost);
            }

            qty = Math.Round(qty, _tab.Security.DecimalsVolume);

            SendNewLogMessage($"Volume: {qty}, риск на сделку: {moneyOnPosition}, расстояние до SL: {Math.Abs(_tab.PriceBestAsk - _valueSL)}" + _tradeAssetInPortfolio, _logType);

            return qty;
        }

        private enum TypeTP
        {
            Coeficient,
            CrossEMA
        }
    }
}

