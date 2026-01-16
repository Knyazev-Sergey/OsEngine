using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Globalization;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Charts.CandleChart.Elements;
using System.Drawing;
using System.IO;

namespace OsEngine.Robots
{
    [Bot("FractalBreakthrough")]
    public class FractalBreakthrough : BotPanel
    {
        private Logging.LogMessageType _logType = Logging.LogMessageType.User;
        private StartProgram _startProgram;
        private BotTabSimple _tab;
        private StrategyParameterString _typeVolume;
        private StrategyParameterDecimal _volume;
        private StrategyParameterDecimal _comission;
        private StrategyParameterDecimal _coefficientComission;
        private StrategyParameterDecimal _lengthATR;
        private StrategyParameterDecimal _coefficientATR;
        private StrategyParameterDecimal _2coefficientATR;
        private StrategyParameterDecimal _kalman1Par1;
        private StrategyParameterDecimal _kalman1Par2;
        private StrategyParameterDecimal _kalman2Par1;
        private StrategyParameterDecimal _kalman2Par2;

        private Fractal _fractal;
        private decimal _upFractal;
        private decimal _downFractal;

        private decimal _lastPrice;

        public FractalBreakthrough(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _startProgram = startProgram;

            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _fractal = new Fractal(name + "Fractal", false);

            string tabNameParameters = " Параметры ";

            _typeVolume = CreateParameter("Режим", "Off", new string[] { "Off", "On" }, tabNameParameters);
            _volume = CreateParameter("Объем позиции", 0m, 0m, 0m, 0m, tabNameParameters);
            _comission = CreateParameter("Комиссия", 0m, 0m, 0m, 0m, tabNameParameters);
            _coefficientComission = CreateParameter("Коэффициент комиссии", 0m, 0m, 0m, 0m, tabNameParameters);
            _lengthATR = CreateParameter("ATR Length", 0m, 0m, 0m, 0m, tabNameParameters);
            _coefficientATR = CreateParameter("ATR K1", 0m, 0m, 0m, 0m, tabNameParameters);
            _2coefficientATR = CreateParameter("ATR K2", 0m, 0m, 0m, 0m, tabNameParameters);
            _kalman1Par1 = CreateParameter("Kalman1 Par1", 0m, 0m, 0m, 0m, tabNameParameters);
            _kalman1Par2 = CreateParameter("Kalman1 Par2", 0m, 0m, 0m, 0m, tabNameParameters);
            _kalman2Par1 = CreateParameter("Kalman2 Par1", 0m, 0m, 0m, 0m, tabNameParameters);
            _kalman2Par2 = CreateParameter("Kalman2 Par2", 0m, 0m, 0m, 0m, tabNameParameters);

            this.ParamGuiSettings.Title = "Fractal Breakthrough";
            this.ParamGuiSettings.Height = 600;
            this.ParamGuiSettings.Width = 700;

           
            _tab.ManualPositionSupport.DisableManualSupport();
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tab.CandleUpdateEvent += _tab_CandleUpdateEvent;

            //_tab.PositionOpenerToStop?.Clear();

            if (_startProgram == StartProgram.IsOsTrader)
            {
                Thread mainThread = new Thread(MainThread) { IsBackground = true };
                mainThread.Start();
            }
        }

        private void _tab_CandleUpdateEvent(List<Candle> candels)
        {
            if (_startProgram == StartProgram.IsOsTrader)
            {
                AddFractalsToChart(candels);
            }
        }

        private void _tab_CandleFinishedEvent(List<Candle> candels)
        {
            if (_startProgram == StartProgram.IsTester)
            {
                _lastPrice = candels[^1].Close;

                AddFractalsToChart(candels);
                TradeLogic();
            }
        }

        private void MainThread(object obj)
        {

        }

        private void TradeLogic()
        {
            // open long
            if (_downFractalIsChange || _upFractalIsChange)                
            {
                if (_upFractal != 0 && _downFractal != 0)
                {
                    //проверка фильтров

                    _downFractalIsChange = false;
                    _upFractalIsChange = false;

                    if (_tab.PositionOpenLong.Count == 0)
                    {
                        _tab.BuyAtStopCancel();
                        _tab.BuyAtStopMarket(_volume, _upFractal, _upFractal, StopActivateType.HigherOrEqual, 0, null, PositionOpenerToStopLifeTimeType.NoLifeTime);
                    }

                    if (_tab.PositionOpenShort.Count == 0)
                    {
                        _tab.SellAtStopCancel();
                        _tab.SellAtStopMarket(_volume, _downFractal, _downFractal, StopActivateType.LowerOrEqual, 0, null, PositionOpenerToStopLifeTimeType.NoLifeTime);
                    }

                }
            }

            //open short
            if (_upFractalIsChange && _downFractal != 0)
            {
                _upFractalIsChange = false;
                //проверка фильтров

                
            }

            /*if (_upFractalIsChange && _upFractal == 0)
            {
                // проверка на выставленный ордер
                

                _tab.SellAtStopCancel();
            }

            if (_downFractalIsChange && _downFractal == 0)
            {
                // проверка на выставленный ордер

                _tab.BuyAtStopCancel();
            }*/
        }

        #region Fractal

        private bool _upFractalIsChange;
        private bool _downFractalIsChange;

        private void AddFractalsToChart(List<Candle> obj)
        {
            if (obj.Count < 5)
            {
                _upFractal = 0;
                _downFractal = 0;
                _fractal.ValuesUp?.Clear();
                _fractal.ValuesDown?.Clear();
                //_tab.BuyAtStopCancel();
                //_tab.SellAtStopCancel();

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
                        _upFractal = 0;
                        _tab.DeleteChartElement(point);
                        _upFractalIsChange = true;

                        break;
                    }

                    if (_upFractal != _fractal.ValuesUp[i])
                    {
                        _upFractal = _fractal.ValuesUp[i];
                        _upFractalIsChange = true;
                        
                        point.Y = obj[i].High + _tab.Security.PriceStep * 10;
                        point.TimePoint = obj[i].TimeStart;
                        point.Color = Color.Green;
                        point.Style = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Star4;
                        point.Size = 12;

                        _tab.SetChartElement(point);
                        SendNewLogMessage("Изменился верхний фрактал: " + _upFractal, _logType);
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
                        _downFractal = 0;
                        _tab.DeleteChartElement(point);
                        _downFractalIsChange = true;

                        break;
                    }

                    if (_downFractal != _fractal.ValuesDown[i])
                    {
                        _downFractalIsChange = true;

                        _downFractal = _fractal.ValuesDown[i];

                        point.Y = obj[i].Low - _tab.Security.PriceStep * 10;
                        point.TimePoint = obj[i].TimeStart;
                        point.Color = Color.Red;
                        point.Style = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Star4;
                        point.Size = 12;

                        _tab.SetChartElement(point);
                        SendNewLogMessage("Изменился нижний фрактал: " + _downFractal, _logType);
                    }

                    break;
                }
            }
        }

        #endregion

    }
}

