using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Threading;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Charts.CandleChart.Elements;
using System.Drawing;
using OsEngine.Indicators;
using OsEngine.Market.Servers.Tester;
using OsEngine.Market.Servers;
using OsEngine.Market;

namespace OsEngine.Robots
{
    [Bot("FractalBreakthrough")]
    public class FractalBreakthrough : BotPanel
    {
        private Logging.LogMessageType _logType = Logging.LogMessageType.User;
        private StartProgram _startProgram;
        private BotTabSimple _tab;
        private StrategyParameterString _regime;
        private StrategyParameterDecimal _volume;
        private StrategyParameterDecimal _coefTP;
        private StrategyParameterDecimal _comission;
        private StrategyParameterDecimal _coefficientComission;
        private StrategyParameterInt _lengthATR;
        private StrategyParameterDecimal _kATR;
        private StrategyParameterDecimal _k2ATR;
        private StrategyParameterDecimal _kalman1Par1;
        private StrategyParameterDecimal _kalman1Par2;
        private StrategyParameterDecimal _kalman2Par1;
        private StrategyParameterDecimal _kalman2Par2;

        private Fractal _fractal;
        private decimal _lastUpFractal;
        private decimal _lastDownFractal;

        private Aindicator _atr;
        private Aindicator _kalman1;
        private Aindicator _kalman2;

        private decimal _priceTPBuy;
        private decimal _priceTPSell;
        private decimal _priceSLBuy;
        private decimal _priceSLSell;

        private string _timeCandle;

        public FractalBreakthrough(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _startProgram = startProgram;

            this.ParamGuiSettings.Title = "Fractal Breakthrough";
            this.ParamGuiSettings.Height = 600;
            this.ParamGuiSettings.Width = 700;

            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            string tabNameParameters = " Параметры ";

            _regime = CreateParameter("Режим", "Off", new string[] { "Off", "On" }, tabNameParameters);
            _volume = CreateParameter("Объем позиции", 1m, 0m, 0m, 0m, tabNameParameters);
            _coefTP = CreateParameter("Коэффициент для тейк-профита к стоп-лоссу", 1.5m, 0m, 0m, 0m, tabNameParameters);
            _comission = CreateParameter("Комиссия, %", 0m, 0m, 0m, 0m, tabNameParameters);
            _coefficientComission = CreateParameter("Коэффициент комиссии", 0m, 0m, 0m, 0m, tabNameParameters);
            _lengthATR = CreateParameter("ATR Length", 14, 0, 0, 0, tabNameParameters);
            _kATR = CreateParameter("ATR K1", 0m, 0m, 0m, 0m, tabNameParameters);
            _k2ATR = CreateParameter("ATR K2", 0m, 0m, 0m, 0m, tabNameParameters);
            _kalman1Par1 = CreateParameter("Kalman1 Par1", 0m, 0m, 0m, 0m, tabNameParameters);
            _kalman1Par2 = CreateParameter("Kalman1 Par2", 0m, 0m, 0m, 0m, tabNameParameters);
            _kalman2Par1 = CreateParameter("Kalman2 Par1", 0m, 0m, 0m, 0m, tabNameParameters);
            _kalman2Par2 = CreateParameter("Kalman2 Par2", 0m, 0m, 0m, 0m, tabNameParameters);

            _fractal = new Fractal(name + "Fractal", false);

            _atr = IndicatorsFactory.CreateIndicatorByName("ATR", name + "Atr", false);
            _atr = (Aindicator)_tab.CreateCandleIndicator(_atr, "AtrArea");
            ((IndicatorParameterInt)_atr.Parameters[0]).ValueInt = _lengthATR;
            _atr.Save();

            _kalman1 = IndicatorsFactory.CreateIndicatorByName("KalmanFilter", name + "Kalman1", false);
            _kalman1 = (Aindicator)_tab.CreateCandleIndicator(_kalman1, "Prime");
            ((IndicatorParameterDecimal)_kalman1.Parameters[0]).ValueDecimal = _kalman1Par1;
            ((IndicatorParameterDecimal)_kalman1.Parameters[1]).ValueDecimal = _kalman1Par2;
            _kalman1.Save();

            _kalman2 = IndicatorsFactory.CreateIndicatorByName("KalmanFilter", name + "Kalman2", false);
            _kalman2 = (Aindicator)_tab.CreateCandleIndicator(_kalman2, "Prime");
            ((IndicatorParameterDecimal)_kalman2.Parameters[0]).ValueDecimal = _kalman2Par1;
            ((IndicatorParameterDecimal)_kalman2.Parameters[1]).ValueDecimal = _kalman2Par2;
            _kalman2.Save();

            ParametrsChangeByUser += FractalBreakthrough_ParametrsChangeByUser;

            _tab.ManualPositionSupport.DisableManualSupport();
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;

            if (StartProgram == StartProgram.IsTester
                && ServerMaster.GetServers() != null)
            {
                List<IServer> servers = ServerMaster.GetServers();

                if (servers != null
                    && servers.Count > 0
                    && servers[0].ServerType == ServerType.Tester)
                {
                    TesterServer server = (TesterServer)servers[0];
                    server.TestingStartEvent += Server_TestingStartEvent;
                }
            }
        }

        private void Server_TestingStartEvent()
        {
            WithdrawOrders();
        }

        private void FractalBreakthrough_ParametrsChangeByUser()
        {
            ((IndicatorParameterInt)_atr.Parameters[0]).ValueInt = _lengthATR;
            _atr.Save();
            _atr.Reload();

            ((IndicatorParameterDecimal)_kalman1.Parameters[0]).ValueDecimal = _kalman1Par1;
            ((IndicatorParameterDecimal)_kalman1.Parameters[1]).ValueDecimal = _kalman1Par2;
            _kalman1.Reload();
            _kalman1.Save();

            ((IndicatorParameterDecimal)_kalman2.Parameters[0]).ValueDecimal = _kalman2Par1;
            ((IndicatorParameterDecimal)_kalman2.Parameters[1]).ValueDecimal = _kalman2Par2;
            _kalman2.Reload();
            _kalman2.Save();
        }

        private void _tab_CandleFinishedEvent(List<Candle> candels)
        {
            try
            {
                _timeCandle = candels[^1].TimeStart.ToString("dd.MM.yyyy HH:mm:ss");

                AddFractalsToChart(candels);
                TradeLogic();
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                Thread.Sleep(5000);
            }
        }

        private void TradeLogic()
        {
            if (_regime == "Off") return;

            // open long
            if (_downFractalIsChange || _upFractalIsChange)                
            {
                if (!_upFractalIsDelete && !_downFractalIsDelete)
                {       
                    _downFractalIsChange = false;
                    _upFractalIsChange = false;

                    if (_tab.PositionOpenLong.Count == 0 && _tab.PositionOpenShort.Count == 0)
                    {                        
                        if (FilterBuyOrder()) //проверка фильтров
                        {
                            _tab.BuyAtStopCancel();
                            _tab.BuyAtStopMarket(_volume, _lastUpFractal, _lastUpFractal, StopActivateType.HigherOrEqual, 0, null, PositionOpenerToStopLifeTimeType.NoLifeTime);
                        }

                        if (FilterSellOrder()) //проверка фильтров
                        {                            
                            _tab.SellAtStopCancel();
                            _tab.SellAtStopMarket(_volume, _lastDownFractal, _lastDownFractal, StopActivateType.LowerOrEqual, 0, null, PositionOpenerToStopLifeTimeType.NoLifeTime);
                        }
                    }
                }
            }

            if (_upFractalIsDelete)
            {               
                _tab.SellAtStopCancel();
            }

            if (_downFractalIsDelete)
            {
                _tab.BuyAtStopCancel();
            }
        }
                
        private bool FilterBuyOrder()
        {
            // filter kalman
            if (_kalman1.DataSeries[0].Values[^1] <= _kalman1.DataSeries[0].Values[^2] &&
                _kalman2.DataSeries[0].Values[^1] <= _kalman2.DataSeries[0].Values[^2])
            {
                SendNewLogMessage($"{_timeCandle} Фильтр по Kalman Long: не проходим по фильтру", _logType);
                return false; 
            }

            SendNewLogMessage($"{_timeCandle} Фильтр по Kalman Long: проходим по фильтру", _logType);

            // filter TP
            _priceTPBuy = (_tab.PriceBestAsk - _lastDownFractal) * _coefTP + _tab.PriceBestAsk;

            decimal costTP = (_priceTPBuy - _tab.PriceBestAsk) * _volume;
            decimal needTP = _tab.PriceBestAsk * _comission / 100 * _coefficientComission;

            if (costTP < needTP)
            {
                SendNewLogMessage($"{_timeCandle} Фильтр по ТП Long: минимальная прибыль ТП = {needTP}, прибыль ТП = {costTP}, не проходим по фильтру", _logType);
                return false;
            }

            SendNewLogMessage($"{_timeCandle} Фильтр по ТП Long: минимальная прибыль ТП = {needTP}, прибыль ТП = {costTP}, проходим по фильтру", _logType);

            //filter SL
            _priceSLBuy = _lastDownFractal - _tab.Security.PriceStep * 2;
            decimal lastATR = _atr.DataSeries[0].Last;

            if (_priceSLBuy < lastATR * _kATR)
            {
                SendNewLogMessage($"{_timeCandle} Фильтр по kATR Long: цена SL = {_priceSLBuy}, значение фильтра = {lastATR * _kATR}, не проходим по фильтру", _logType);
                return false;
            }

            if (_priceSLBuy > lastATR * _k2ATR)
            {
                SendNewLogMessage($"{_timeCandle} Фильтр по kATR Long: цена SL = {_priceSLBuy}, значение фильтра = {lastATR * _k2ATR}, не проходим по фильтру", _logType);
                return false;
            }

            SendNewLogMessage($"{_timeCandle} Фильтр по kATR Long: проходим по фильтру", _logType);

            return true;
        }

        private bool FilterSellOrder()
        {
            // filter kalman
            if (_kalman1.DataSeries[0].Values[^1] >= _kalman1.DataSeries[0].Values[^2] &&
                _kalman2.DataSeries[0].Values[^1] >= _kalman2.DataSeries[0].Values[^2])
            {
                SendNewLogMessage($"{_timeCandle} Фильтр по Kalman Short: не проходим по фильтру", _logType);
                return false;
            }

            SendNewLogMessage($"{_timeCandle} Фильтр по Kalman Short: проходим по фильтру", _logType);

            // filter TP
            _priceTPSell = _tab.PriceBestBid - (_lastUpFractal - _tab.PriceBestBid) * _coefTP;

            decimal costTP = (_tab.PriceBestBid - _priceTPSell) * _volume;
            decimal needTP = _tab.PriceBestBid * _comission / 100 * _coefficientComission;

            if (costTP < needTP)
            {
                SendNewLogMessage($"{_timeCandle} Фильтр по ТП Short: минимальная прибыль ТП = {needTP}, прибыль ТП = {costTP}, не проходим по фильтру", _logType);
                return false;
            }

            SendNewLogMessage($"{_timeCandle} Фильтр по ТП Short: минимальная прибыль ТП = {needTP}, прибыль ТП = {costTP}, проходим по фильтру", _logType);

            //filter SL
            _priceSLSell = _lastUpFractal + _tab.Security.PriceStep * 2;

            decimal lastATR = _atr.DataSeries[0].Last;

            if (_priceSLSell < lastATR * _kATR)
            {
                SendNewLogMessage($"{_timeCandle} Фильтр по kATR Short: цена SL = {_priceSLSell}, значение фильтра = {lastATR * _kATR}, не проходим по фильтру", _logType);
                return false;
            }

            if (_priceSLSell > lastATR * _k2ATR)
            {
                SendNewLogMessage($"{_timeCandle} Фильтр по kATR Short: цена SL = {_priceSLSell}, значение фильтра = {lastATR * _k2ATR}, не проходим по фильтру", _logType);
                return false;
            }

            SendNewLogMessage($"{_timeCandle} Фильтр по kATR Short: проходим по фильтру", _logType);

            return true;
        }

        private void _tab_PositionOpeningSuccesEvent(Position obj)
        {
            try
            {
                if (obj.Direction == Side.Buy)
                {
                    SetLongSL();
                    SetLongTP();
                }

                if (obj.Direction == Side.Sell)
                {
                    SetShortSL();
                    SetShortTP();
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                Thread.Sleep(5000);
            }
        }

        private void WithdrawOrders()
        {
            _tab.BuyAtStopCancel();
            _tab.SellAtStopCancel();

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

        private void SetLongSL()
        {
            if (_tab.PositionOpenLong[0].StopOrderPrice == 0)
            {
                Position pos = _tab.PositionOpenLong[0];
                decimal price = _lastDownFractal - _tab.Security.PriceStep * 2;
                
                _tab.CloseAtStopMarket(pos, _priceSLBuy);

                SendNewLogMessage($"{_timeCandle} Выставлен StopLoss по Long, цена: {_priceSLBuy}", _logType);
            }
        }

        private void SetLongTP()
        {
            if (_tab.PositionOpenLong[0].ProfitOrderPrice == 0)
            {                
                Position pos = _tab.PositionOpenLong[0];
                decimal price = (_tab.PriceBestAsk - _lastDownFractal) * _coefTP + _tab.PriceBestAsk;
                _tab.CloseAtProfitMarket(pos, _priceTPBuy);

                SendNewLogMessage($"{_timeCandle} Выставлен TakeProfit по Long, цена: {_priceTPBuy}", _logType);
            }
        }

        private void SetShortSL()
        {
            if (_tab.PositionOpenShort[0].StopOrderPrice == 0)
            {
                Position pos = _tab.PositionOpenShort[0];
                decimal price = _lastDownFractal - _tab.Security.PriceStep * 2;
                _tab.CloseAtStopMarket(pos, _priceSLSell);

                SendNewLogMessage($"{_timeCandle} Выставлен StopLoss по Long, цена: {_priceSLSell}", _logType);
            }
        }

        private void SetShortTP()
        {
            if (_tab.PositionOpenShort[0].ProfitOrderPrice == 0)
            {
                Position pos = _tab.PositionOpenShort[0];
                decimal price = _tab.PriceBestBid - (_lastUpFractal - _tab.PriceBestBid) * _coefTP;
                _tab.CloseAtProfitMarket(pos, _priceTPSell);

                SendNewLogMessage($"{_timeCandle} Выставлен TakeProfit по Long, цена: {_priceTPSell}", _logType);
            }
        }

        #region Fractal

        private bool _upFractalIsChange;
        private bool _downFractalIsChange;
        private bool _upFractalIsDelete;
        private bool _downFractalIsDelete;

        private void AddFractalsToChart(List<Candle> obj)
        {
            if (obj.Count < 5)
            {
                _lastUpFractal = 0;
                _lastDownFractal = 0;
                _fractal.ValuesUp?.Clear();
                _fractal.ValuesDown?.Clear();
                _tab.BuyAtStopCancel();
                _tab.SellAtStopCancel();

                return;
            }

            _fractal.Process(obj);

            for (int i = _fractal.ValuesUp.Count - 1; i >= 0; i--)
            {
                if (_fractal.ValuesUp[i] != 0)
                {
                    PointElement point = new PointElement("UpFractal", "Prime");

                    if (_fractal.ValuesUp[i] < obj[^1].Close)
                    {
                        _lastUpFractal = _fractal.ValuesUp[i];
                        _tab.DeleteChartElement(point);
                        _upFractalIsChange = true;
                        _upFractalIsDelete = true;

                        break;
                    }

                    if (_lastUpFractal != _fractal.ValuesUp[i])
                    {
                        _lastUpFractal = _fractal.ValuesUp[i];
                        _upFractalIsChange = true;
                        _upFractalIsDelete = false;

                        point.Y = obj[i].High + _tab.Security.PriceStep * 10;
                        point.TimePoint = obj[i].TimeStart;
                        point.Color = Color.Green;
                        point.Style = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Star4;
                        point.Size = 12;

                        _tab.SetChartElement(point);
                        SendNewLogMessage($"{_timeCandle} Изменился верхний фрактал: " + _lastUpFractal, _logType);
                    }

                    break;
                }
            }

            for (int i = _fractal.ValuesDown.Count - 1; i >= 0; i--)
            {
                if (_fractal.ValuesDown[i] != 0)
                {
                    PointElement point = new PointElement("DownFractal", "Prime");

                    if (_fractal.ValuesDown[i] > obj[^1].Close)
                    {
                        _lastDownFractal = _fractal.ValuesDown[i];
                        _tab.DeleteChartElement(point);
                        _downFractalIsChange = true;
                        _downFractalIsDelete = true;

                        break;
                    }

                    if (_lastDownFractal != _fractal.ValuesDown[i])
                    {
                        _downFractalIsChange = true;
                        _downFractalIsDelete = false;

                        _lastDownFractal = _fractal.ValuesDown[i];

                        point.Y = obj[i].Low - _tab.Security.PriceStep * 10;
                        point.TimePoint = obj[i].TimeStart;
                        point.Color = Color.Red;
                        point.Style = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Star4;
                        point.Size = 12;

                        _tab.SetChartElement(point);
                        SendNewLogMessage($"{_timeCandle} Изменился нижний фрактал: " + _lastDownFractal, _logType);
                    }

                    break;
                }
            }
        }

        #endregion

    }
}

