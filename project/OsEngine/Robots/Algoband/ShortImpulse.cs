using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace OsEngine.Robots.AlgoBand.ShortImpulse
{
    [Bot("ShortImpulse")]
    public class ShortImpulse : BotPanel
    {
        private readonly BotTabSimple _tab;
        private StrategyParameterString _regime;
        private StrategyParameterString _volumeType;
        private StrategyParameterDecimal _volume;
        private StrategyParameterInt _limitSlipage;
        private StrategyParameterString _tradeAssetInPortfolio;
        private StrategyParameterInt _lengthPercentile;
        private StrategyParameterInt _minPercentile;
        private StrategyParameterInt _maxPercentile;
        private StrategyParameterInt _skewLength;
        private StrategyParameterInt _stepCount;
        private StrategyParameterInt _dayInYears;
        private StrategyParameterInt _recalcMobilityBars;
        private StrategyParameterInt _lengthAverageMobility;
        private StrategyParameterDecimal _buyRatio;
        private StrategyParameterDecimal _buyRatioRev;
        private StrategyParameterDecimal _sellRatio;
        private StrategyParameterDecimal _sellRatioRev;
        private StrategyParameterDecimal _exitBuyRatio;
        private StrategyParameterDecimal _exitSellRatio;
        private StrategyParameterDecimal _buyFilterRatio;
        private StrategyParameterDecimal _buyFilterRatioRev;
        private StrategyParameterDecimal _sellFilterRatio;
        private StrategyParameterDecimal _sellFilterRatioRev;
        private StrategyParameterDecimal _stopRatio;
        private Aindicator _bband;
        private Aindicator _nrtr;
        private Aindicator _kalman;
        private Aindicator _stochastic;
        private StrategyParameterInt _bbLength;
        private StrategyParameterDecimal _bbStdDev;
        private StrategyParameterInt _nrtrLength;
        private StrategyParameterDecimal _nrtrMultiplier;
        private StrategyParameterDecimal _kalmanSharpness;
        private StrategyParameterDecimal _kalmanK;
        private StrategyParameterInt _stochPeriod1;
        private StrategyParameterInt _stochPeriod2;
        private StrategyParameterInt _stochPeriod3;
        private List<Signal> _signals = [];

        public ShortImpulse(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            //Базовые параметры
            _regime = CreateParameter("Regime", "Off", ["Off", "On", "OnlyLong","OnlyShort", "OnlyClose"], "Настройки");
            _volumeType = CreateParameter("Volume Type", "Contracts", ["Contracts", "Percent"], "Настройки");
            _volume = CreateParameter("Volume", 1m, 1m, 100m, 1m, "Настройки");
            _limitSlipage = CreateParameter("Sleepage limit, price step", 10, 2, 20, 2, "Настройки");
            _tradeAssetInPortfolio = CreateParameter("Trade Asset In Portfolio", "Prime", ["Prime"], "Настройки");

            _dayInYears = CreateParameter("Trading Days in Year", 365, 365, 365, 1, "Настройки");
            _stepCount = CreateParameter("Step Count Mobility, bars", 2, 1, 5, 1, "Настройки");
            _recalcMobilityBars = CreateParameter("Recalc Mobility, bars", 14, 10, 20, 2, "Настройки");
            _lengthAverageMobility = CreateParameter("Length Average Mobility", 30, 15, 60, 5, "Настройки");
            _lengthPercentile = CreateParameter("Length Percentile, weeks", 3, 1, 10, 1, "Настройки");
            _minPercentile = CreateParameter("Min Percentile", 30, 20, 30, 5, "Настройки");
            _maxPercentile = CreateParameter("Max Percentile", 70, 70, 95, 5, "Настройки");
            _skewLength = CreateParameter("Skew Length", 1440, 1440, 14400, 60, "Настройки");

            _buyRatio = CreateParameter("Buy Ratio, %", 75.0m, 50.0m, 95m, 1.0m, "Настройки");
            _buyRatioRev = CreateParameter("Buy Ratio Rev, %", 30.0m, 25.0m, 70.0m, 5.0m, "Настройки");
            _sellRatio = CreateParameter("Sell Ratio, %", -75.0m, -95.0m, -50.0m, 1.0m, "Настройки");
            _sellRatioRev = CreateParameter("Sell Ratio Rev, %", -50.0m, -75.0m, -30.0m, 5.0m, "Настройки");

            _exitBuyRatio = CreateParameter("Exit Buy Ratio, %", -60.0m, -75.0m, -30.0m, 1.0m, "Настройки");
            _exitSellRatio = CreateParameter("Exit Sell Ratio, %", 60.0m, 30.0m, 75.0m, 1.0m, "Настройки");

            _buyFilterRatio = CreateParameter("Buy Filter Ratio, %", 80.0m, 50.0m, 95.0m, 1.0m, "Настройки");
            _buyFilterRatioRev = CreateParameter("Buy Filter Ratio Rev, %", 50m, 30m, 70m, 5m, "Настройки");
            _sellFilterRatio = CreateParameter("Sell Filter Ratio, %", -80.0m, -95.0m, 50.0m, 1.0m, "Настройки");
            _sellFilterRatioRev = CreateParameter("Sell Filter Ratio Rev, %", -50m, -75m, -30m, 5m, "Настройки");

            _stopRatio = CreateParameter("Stop Ratio", 1.0m, 0.5m, 2.0m, 0.1m, "Настройки");

            //Параметры индикаторов
            _bbLength = CreateParameter("BB Length", 720, 1, 100, 1, "Параметры индикаторов");
            _bbStdDev = CreateParameter("BB StdDev", 2.0m, 1m, 3.0m, 0.5m, "Параметры индикаторов");

            _nrtrLength = CreateParameter("NRTR Length", 20, 1, 100, 1, "Параметры индикаторов");
            _nrtrMultiplier = CreateParameter("NRTR Multiplier", 2.0m, 1.0m, 3.0m, 0.5m, "Параметры индикаторов");
            
            _kalmanSharpness = CreateParameter("Kalman Sharpness", 1.0m, 0.1m, 5.0m, 0.1m, "Параметры индикаторов");
            _kalmanK = CreateParameter("Kalman K", 1.0m, 0.2m, 5.0m, 0.1m, "Параметры индикаторов");
            
            _stochPeriod1 = CreateParameter("Stochastic Period 1", 5, 1, 50, 1, "Параметры индикаторов");
            _stochPeriod2 = CreateParameter("Stochastic Period 2", 3, 1, 50, 1, "Параметры индикаторов");
            _stochPeriod3 = CreateParameter("Stochastic Period 3", 3, 1, 50, 1, "Параметры индикаторов");

            //Индикаторы
            _bband = IndicatorsFactory.CreateIndicatorByName("Bollinger", name + "Bollinger", false);
            _bband = (Aindicator)_tab.CreateCandleIndicator(_bband, "Prime");
            ((IndicatorParameterInt)_bband.Parameters[0]).ValueInt = _bbLength.ValueInt;
            ((IndicatorParameterDecimal)_bband.Parameters[1]).ValueDecimal= _bbStdDev.ValueDecimal;
            _bband.DataSeries[0].Color = Color.DarkRed;
            _bband.DataSeries[1].Color = Color.DarkGreen;
            _bband.DataSeries[2].Color = Color.DarkBlue;
            _bband.Save();

            _nrtr = IndicatorsFactory.CreateIndicatorByName("NRTR_WATR", name + "NRTR_WATR", false);
            _nrtr = (Aindicator)_tab.CreateCandleIndicator(_nrtr, "Prime");
            ((IndicatorParameterInt)_nrtr.Parameters[0]).ValueInt = _nrtrLength.ValueInt;
            ((IndicatorParameterDecimal)_nrtr.Parameters[1]).ValueDecimal = _nrtrMultiplier.ValueDecimal;
            _nrtr.DataSeries[0].Color = Color.Orange;
            _nrtr.Save();

            _kalman = IndicatorsFactory.CreateIndicatorByName("KalmanFilter", name + "KalmanFilter", false);
            _kalman = (Aindicator)_tab.CreateCandleIndicator(_kalman, "Prime");
            ((IndicatorParameterDecimal)_kalman.Parameters[0]).ValueDecimal = _kalmanSharpness.ValueDecimal;
            ((IndicatorParameterDecimal)_kalman.Parameters[1]).ValueDecimal = _kalmanK.ValueDecimal;
            _kalman.DataSeries[0].Color = Color.Magenta;
            _kalman.Save();

            _stochastic = IndicatorsFactory.CreateIndicatorByName("Stochastic", name + "Stochastic", false);
            _stochastic = (Aindicator)_tab.CreateCandleIndicator(_stochastic, "Stochastic");
            ((IndicatorParameterInt)_stochastic.Parameters[0]).ValueInt = 5;
            ((IndicatorParameterInt)_stochastic.Parameters[1]).ValueInt = 3;
            ((IndicatorParameterInt)_stochastic.Parameters[2]).ValueInt = 3;
            _stochastic.DataSeries[0].Color = Color.DodgerBlue;
            _stochastic.DataSeries[1].Color = Color.DarkRed;
            _stochastic.Save();

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            ParametrsChangeByUser += Event_ParametersChangeByUser;
            DeleteEvent += _tab_DeleteEvent;
        }

        private void Event_ParametersChangeByUser()
        {
            if (_bband.ParametersDigit[0].Value != _bbLength.ValueInt)
            {
                _bband.ParametersDigit[0].Value = _bbLength.ValueInt;
                _bband.Reload();
            }

            if (_bband.ParametersDigit[1].Value != _bbStdDev.ValueDecimal)
            {
                _bband.ParametersDigit[1].Value = _bbStdDev.ValueDecimal;
                _bband.Reload();
            }

            if (_nrtr.ParametersDigit[0].Value != _nrtrLength.ValueInt)
            {
                _nrtr.ParametersDigit[0].Value = _nrtrLength.ValueInt;
                _nrtr.Reload();
            }
            if (_nrtr.ParametersDigit[1].Value != _nrtrMultiplier.ValueDecimal)
            {
                _nrtr.ParametersDigit[1].Value = _nrtrMultiplier.ValueDecimal;
                _nrtr.Reload();
            }
            if (_kalman.ParametersDigit[0].Value != _kalmanSharpness.ValueDecimal)
            {
                _kalman.ParametersDigit[0].Value = _kalmanSharpness.ValueDecimal;
                _kalman.Reload();
            }
            if (_kalman.ParametersDigit[1].Value != _kalmanK.ValueDecimal)
            {
                _kalman.ParametersDigit[1].Value = _kalmanK.ValueDecimal;
                _kalman.Reload();
            }
            if (_stochastic.ParametersDigit[0].Value != _stochPeriod1.ValueInt)
            {
                _stochastic.ParametersDigit[0].Value = _stochPeriod1.ValueInt;
                _stochastic.Reload();
            }
            if (_stochastic.ParametersDigit[1].Value != _stochPeriod2.ValueInt)
            {
                _stochastic.ParametersDigit[1].Value = _stochPeriod2.ValueInt;
                _stochastic.Reload();
            }
            if (_stochastic.ParametersDigit[2].Value != _stochPeriod3.ValueInt)
            {
                _stochastic.ParametersDigit[2].Value = _stochPeriod3.ValueInt;
                _stochastic.Reload();
            }
        }

        private void _tab_DeleteEvent()
        {
            _signals.Clear();
            if (File.Exists(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
            {
                File.Delete(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt");
            }
        }

        private decimal _lastFractalUp = decimal.MaxValue;
        private decimal _lastFractalDown = decimal.MinValue;
        private double _skew = 0;
        private (double Min, double Median, double Max) _mobilityPercentile;
        private (double Mobility, double avgMobility) _currentMobility;
        private decimal _lastPrice = 0;
        private (int Volatility, int Skew, int Trend) _marketRegime;

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (_regime.ValueString == "Off") return;
            
            if(candles.Count < _stepCount.ValueInt + 1) return;
            _currentMobility = CalculateMobility(candles, _stepCount.ValueInt, _lengthAverageMobility.ValueInt);
            
            _lastPrice = candles[^1].Close;

            var _logSignal = new Signal();
            
            var dayOfWeek = (int)candles[^1].TimeStart.DayOfWeek;
            var numberHour = candles[^1].TimeStart.Hour;
            _mobilityPercentile = CalculateVolaPercentile(_currentMobility.Mobility, dayOfWeek, numberHour, _lengthPercentile.ValueInt, _maxPercentile.ValueInt);
            
            _skew = CalculateSkew(candles, _skewLength.ValueInt);
            
            var signal = CalculateSignals(candles);
            var filter = CalculateFilters(candles);

            SendNewLogMessage($"Signal: {signal}, filter: {filter}", Logging.LogMessageType.User);

            _marketRegime = GetMarketRegime(candles[^1]);
            
            CalculateFractals(candles,30);

            var posLong = _tab.PositionOpenLong;
            var posShort = _tab.PositionOpenShort;

            if (signal > (double)_buyRatio.ValueDecimal) //есть сигнал на покупку
            {
                //Если есть позиция на продажу, то закрываем её
                if (posShort.Count > 0)
                {
                    ExitShort(posShort); //закрываем позицию на продажу
                }

                if (filter > (double)_buyFilterRatio.ValueDecimal)//сигнал прошел фильтр на покупку
                {
                    //открываем позицию на покупку
                    BuyLogic();
                }
            }
            else if (signal < (double)_sellRatio.ValueDecimal) //есть сигнал на продажу
            {
                //Если есть позиция на покупку, то закрываем её
                if (posLong.Count > 0)
                {
                    ExitLong(posLong); //закрываем позицию на покупку
                }
                
                if (filter < (double)_sellFilterRatio.ValueDecimal)//сигнал прошел фильтр на продажу
                {
                    //открываем позицию на ппродажу
                    SellLogic();
                }
            }
        }

        private void SellLogic()
        {
            if (_regime.ValueString.Equals("OnlyLong") || _regime.ValueString.Equals("OnlyClose")) return;
            //TODO: реализовать логику продажи
        }

        private decimal _takeProfitPrice = 0;
        private decimal _takeProfit2Price = 0;
        private decimal _stopLossPrice = 0;
        private decimal _volume1 = 0;
        private decimal _volume2 = 0;
        private void BuyLogic()
        {
            //TODO: реализовать логику покупки
            //*** Нужны разные пороги сигналов и фильтров для разных режимов рынка? Учитывать Skew при определении тренда?
            /*1. Если vola(режим) = 0 -> выходим.
             *2. Если trend(режим) = 2 (рост bb и цена > bb) и фильтр > buyFilterRatio и сигнал > buyRatio и есть fractalDown ->
             *   считаем TakeProfit 1) vola = 1 -> take = avgMobility   2) vola = 2 -> take1(на 0,5 объема) = avgMobility,
             *   take2(остальной объем) = mobPercentile.Max. stopLossPrice = fractalDown - 2 stepPrice.
             *   При этом Close-stopLossPrice < stopRatio * (TakeProfit-Close)) и до стопа > 0.5? * avgMobility -> покупаем лимиткой по лучшей цене (в середину спреда?)
             *   полный объем. Двигаясь на 1 шаг цены каждые N сек, но не более порога. Если спред очень узкий (меньше limitSlipage и в
             *   стакане есть нужный объем и комиссия = 0?) - берем все по рынку. Если лимиткой не налили за 1 минуту (30 сек?)
             *   - снимаем ордер или берем по маркету (доп. условия). После открытия позы ставим стоп-лосс и тейк(и).
             *3. Если trend(режим) = 1 (неопределенность), сигнал и фильтр прошли (со своими коэфф.?), есть фрактал... аналогично п.2.
             *   Полный объем с тейком = Мин(mobilityPercentile.Median, avgMobility) если стоп не сильно близко-далеко. Покупка как и в п.2.
             *   ..............
             */

            /*Записать в лог-файл информацию от сигналов без фильтрации (фильтр >< 0):
                1. Время сигнала
                2. Цена сигнала
                3. Название сигнала (список сигналов, если несколько совпало)
                4. Режим рынка
                5. Значение фильтра
                5. День            
                6. Час
            Получим данные для последующего анализа эффективности сигналов в разных режимах рынка, времени, фильтров.
             */
            if (_regime.ValueString.Equals("OnlyShort") || _regime.ValueString.Equals("OnlyClose")) return;
            if (_marketRegime.Volatility == 0) return; //низкая вола
            //Если еще не посчитались фракталы - выходим
            if (_lastFractalDown == decimal.MinValue || _mobilityPercentile.Median == 0 || _lastPrice == 0)
            {
                _stopLossPrice = 0;
                return;
            }
            else
            {
                _stopLossPrice = _lastFractalDown - _tab.Security.PriceStep * 2;
                
                //I'm here now
                _takeProfitPrice = _lastPrice + (decimal)_mobilityPercentile.Median;
            }
            //Если текущая и средняя мобильность выше максимального перцентиля -> берем 2 равные позы:
            //тейк 1-й по средней мобильности, тейк 2-й по макс. перцентилю.
            if (_currentMobility.Mobility > _mobilityPercentile.Max &&
                _currentMobility.avgMobility > _mobilityPercentile.Max)
            {
                _takeProfitPrice = _lastPrice + (decimal)_currentMobility.avgMobility;
                _takeProfit2Price = _lastPrice + (decimal)_mobilityPercentile.Max;
                _volume1 = Math.Round(GetVolume() / 2, _tab.Security.DecimalsVolume);
                _volume2 = _volume1;
            }
        }

        private decimal GetVolume()
        {
            //TODO: Получить обеъем торговой позиции
            return 0;
        }

        private void ExitLong(List<Position> posLong)
        {
            //TODO: реализовать логику выхода из лонга

        }

        private void ExitShort(List<Position> posShort)
        {
            //TODO: реализовать логику выхода из шорта
        }

        private readonly Dictionary<(int DayOfWeek, int NumberHour), List<double>> _mapMobility =
            new Dictionary<(int DayOfWeek, int NumberHour), List<double>>();
        private (double Min, double Median, double Max) CalculateVolaPercentile(double currentMobility, int dayOfWeek, int numberHour, int lengthPercentile, int minPercentile=30, int maxPercentile = 70)
        {
            if (currentMobility == 0) return (0.0,0.0,0.0);
            
            List<double> data = [];
            if (_mapMobility.ContainsKey((dayOfWeek, numberHour)))
            {
                _mapMobility[(dayOfWeek, numberHour)].Add(currentMobility);
            }
            else
            {
                data.Add(currentMobility);
                _mapMobility.Add((dayOfWeek, numberHour), data);
            }
            //Обрезаем лишние данные по заданной длине
            if (_mapMobility[(dayOfWeek, numberHour)].Count > lengthPercentile * 60)
            {
                _mapMobility[(dayOfWeek, numberHour)].RemoveAt(0);
            }

            if (_mapMobility[(dayOfWeek, numberHour)].Count < 5) return (0, 0, 0);

            var mob = new List<double>(_mapMobility[(dayOfWeek, numberHour)]);
            mob.Sort();
            
            if(minPercentile <  1) minPercentile = 5;
            var indexMin = (int)Math.Ceiling(minPercentile / 100.0 * mob.Count) - 1;
            
            var indexMedian = (int)Math.Ceiling(0.5 * mob.Count) - 1;
            
            if(maxPercentile <= 50) maxPercentile = 70;
            var indexMax = (int)Math.Ceiling(maxPercentile / 100.0 * mob.Count) - 1;

            return (mob[indexMin], mob[indexMedian], mob[indexMax]);
        }

        private readonly List<double> _mobility = [];
        private (double Mobility, double avgMobility) CalculateMobility(List<Candle> candles, int stepCount=2, int lengthAvg=30)
        {
            if (candles.Count < stepCount + 1) return (0.0, 0.0);

            double sumMob = 0;

            for (int i = candles.Count - 1; i > candles.Count - stepCount - 1; i--)
            {
                sumMob += Math.Pow((double)(candles[i].Close - candles[i - 1].Close), 2);
            }
            var mob = Math.Sqrt(sumMob * _recalcMobilityBars.ValueInt / stepCount);
            
            _mobility.Add(mob);
            var avgMob = 0.0;

            if (_mobility.Count > lengthAvg)
            {
                _mobility.RemoveAt(0);
            }
            
            for (int i = 0; i < _mobility.Count; i++)
            {
                avgMob += _mobility[i];
            }

            avgMob /= _mobility.Count;

            return (mob, avgMob);
        }

        private void CalculateFractals(List<Candle> candles, int length = 30)
        {
            if(candles.Count < length + 1) return;

            if (candles[^1].Low < _lastFractalDown) _lastFractalDown = decimal.MinValue;
            if (candles[^1].High > _lastFractalUp) _lastFractalUp = decimal.MaxValue;
            
            for (int i = candles.Count - 1; i >= candles.Count - length - 2; i--)
            {
                //верхний фрактал
                if (candles[i].High < candles[i - 1].High && candles[i - 1].High > candles[i - 2].High)
                {
                    if (candles[i - 1].High < _lastFractalUp && candles[^1].High < candles[i - 1].High)
                    {
                        _lastFractalUp = candles[i - 1].High;
                    }
                }
                //нижний фрактал
                if (candles[i].Low > candles[i - 1].Low && candles[i - 1].Low < candles[i - 2].Low)
                {
                    if (candles[i - 1].Low > _lastFractalDown && candles[^1].Low > candles[i - 1].Low)
                    {
                        _lastFractalDown = candles[i - 1].Low;
                    }
                }
            }
        }
        
        private (int Volatility, int Skew, int Trend) GetMarketRegime(Candle candle)
        {
            //3 диапазона волатильности: низкая, средняя, высокая (0.1.2)
            //3 диапазона Skew: положительный, нейтральный, отрицательный (0.1.2)
            //3 диапазона направления тренда: восходящий, боковой, нисходящий (0.1.2)
            //Итог: 27 режимов рынка (снизить размерность?)
            //0 - неопределенность
            //используем кодирование для определения режима рынка
            //старший разряд - волатильность (0 - низкая, 1 - средняя, 2 - высокая)
            //средний разряд - skew (0 - отрицательный, 1 - нейтральный, 2 - положительный)
            //младший разряд - направление тренда (0 - нисходящий, 1 - боковой, 2 - восходящий)
            
            if (_currentMobility.Mobility == 0 || _mobilityPercentile.Max == 0 || _skew == 0) return (9,9,9);
            if(_bband.DataSeries[2].Values.Count < 2) return (9,9,9);
            
            int vola;
            if (_currentMobility.avgMobility < _mobilityPercentile.Min)
            {
                vola = 0; //низкая
            }
            else if (_currentMobility.avgMobility < _mobilityPercentile.Max)
            {
                vola = 1; //норм
            }
            else vola = 2; //высокая

            int skew;
            if (_skew < -0.2)
            {
                skew = 0; //хвосты вниз
            }
            else if (_skew > 0.2)
            {
                skew = 2; //хвосты вверх
            }
            else skew = 1; //околонейтрально

            int trend;
            if (_bband.DataSeries[2].Values[^1] >= _bband.DataSeries[2].Values[^2] &&
                candle.Close > _bband.DataSeries[2].Values[^1])
            {
                trend = 2; //вверх
            }
            else if (_bband.DataSeries[2].Values[^1] < _bband.DataSeries[2].Values[^2] &&
                     candle.Close < _bband.DataSeries[2].Values[^1])
            {
                trend = 0; //вниз
            }
            else trend = 1; //боковик

            return (vola, skew, trend);
        }

        private double CalculateFilters(List<Candle> candles)
        {
            if (candles.Count < _nrtrLength.ValueInt) return 0;
            if (candles.Count < _bbLength.ValueInt + 2) return 0;

            var sumFilter = 0.0;
            var n = 0;
            //1. цена выше/ниже NRTR
            if (_nrtr.DataSeries[0].Values.Count > 1)
            {
                if (candles[^1].Close >= _nrtr.DataSeries[0].Values[^1])
                {
                    sumFilter += 1;
                }
                else
                {
                    sumFilter -= 1;
                }
                n++;
            }
            
            //2. цена выше/ниже Калмана
            if (_kalman.DataSeries[0].Values.Count > 2)
            {
                if (candles[^1].Close >= _kalman.DataSeries[0].Values[^1])
                {
                    sumFilter += 1;
                }
                else
                {
                    sumFilter -= 1;
                }
                n++;
                
                //3. Калман растет/падает
                if (_kalman.DataSeries[0].Values[^1] >= _kalman.DataSeries[0].Values[^2])
                {
                    sumFilter += 1;
                }
                else
                {
                    sumFilter -= 1;
                }
                n++;
            }

            //4. Цена выше/ниже средней Боллинджера
            if (_bband.DataSeries[2].Values.Count > 2)
            {
                if (candles[^1].Close >= _bband.DataSeries[2].Values[^1])
                {
                    sumFilter += 1;
                }
                else
                {
                    sumFilter -= 1;
                }
                n++;

                //5. Средняя Боллинджера растет/падает
                if (_bband.DataSeries[2].Values[^1] >= _bband.DataSeries[2].Values[^2])
                {
                    sumFilter += 1;
                }
                else
                {
                    sumFilter -= 1;
                }
                n++;
            }

            //6. Skew
            if (_skew != 0)
            {
                if (_skew > 0.1)
                {
                    sumFilter += 1;
                }
                else if (_skew < -0.1)
                {
                    sumFilter -= 1;
                }
                n++;
            }

            return sumFilter / n * 100;
        }

        private double CalculateSignals(List<Candle> candles)
        {
            if(candles.Count < 5) return 0.0;

            var sumSignal = 0.0;
            var n = 0;
            
            //1. Цена пересекла вверх/вниз NRTR
            if (candles[^1].Close > _nrtr.DataSeries[0].Values[^1] &&
                candles[^2].Close <= _nrtr.DataSeries[0].Values[^2])
            {
                sumSignal += 1;
            }
            else if (candles[^1].Close < _nrtr.DataSeries[0].Values[^1] &&
                     candles[^2].Close >= _nrtr.DataSeries[0].Values[^2])
            {
                sumSignal -= 1;
            }
            //2. Цена пересекла NRTR 2 бара назад
            else if (candles[^1].Close > _nrtr.DataSeries[0].Values[^1] &&
                     candles[^2].Close > _nrtr.DataSeries[0].Values[^2] &&
                     candles[^3].Close <= _nrtr.DataSeries[0].Values[^3])
            {
                sumSignal += 0.5;
            }
            else if (candles[^1].Close < _nrtr.DataSeries[0].Values[^1] &&
                     candles[^2].Close < _nrtr.DataSeries[0].Values[^2] &&
                     candles[^3].Close >= _nrtr.DataSeries[0].Values[^3])
            {
                sumSignal -= 0.5;
            }
            n++;
            
            //3.Цена пересекла Калмана
            if (_kalman.DataSeries[0].Values.Count > 3)
            {
                if (candles[^1].Close > _kalman.DataSeries[0].Values[^1] &&
                    candles[^2].Close <= _kalman.DataSeries[0].Values[^2])
                {
                    sumSignal += 1;
                }
                else if (candles[^1].Close < _kalman.DataSeries[0].Values[^1] &&
                         candles[^2].Close >= _kalman.DataSeries[0].Values[^2])
                {
                    sumSignal -= 1;
                }
                //4. Цена пересекла Калмана 2 бара назад
                else if (candles[^1].Close > _kalman.DataSeries[0].Values[^1] &&
                         candles[^2].Close > _kalman.DataSeries[0].Values[^2] &&
                         candles[^3].Close <= _kalman.DataSeries[0].Values[^3])
                {
                    sumSignal += 0.5;
                }
                else if (candles[^1].Close < _kalman.DataSeries[0].Values[^1] &&
                         candles[^2].Close < _kalman.DataSeries[0].Values[^2] &&
                         candles[^3].Close >= _kalman.DataSeries[0].Values[^3])
                {
                    sumSignal -= 0.5;
                }
                n++;
            }
            
            //5. Стохастик пересек свою среднюю линию вверх/вниз и его значение < 70/>30
            if (_stochastic.DataSeries[0].Values.Count > 3)
            {
                if (_stochastic.DataSeries[0].Values[^1] > _stochastic.DataSeries[1].Values[^1] &&
                    _stochastic.DataSeries[0].Values[^2] <= _stochastic.DataSeries[1].Values[^2] &&
                    _stochastic.DataSeries[0].Values[^1] < 70)
                {
                    sumSignal += 1;
                }
                else if (_stochastic.DataSeries[0].Values[^1] < _stochastic.DataSeries[1].Values[^1] &&
                         _stochastic.DataSeries[0].Values[^2] >= _stochastic.DataSeries[1].Values[^2] &&
                         _stochastic.DataSeries[0].Values[^1] > 30)
                {
                    sumSignal -= 1;
                }
                //6. Стохастик пересек свою среднюю линию вверх/вниз 2 бара назад и его значение < 70/>30
                else if (_stochastic.DataSeries[0].Values[^1] > _stochastic.DataSeries[1].Values[^1] &&
                         _stochastic.DataSeries[0].Values[^2] > _stochastic.DataSeries[1].Values[^2] &&
                         _stochastic.DataSeries[0].Values[^3] <= _stochastic.DataSeries[1].Values[^3] &&
                         _stochastic.DataSeries[0].Values[^1] < 70)
                {
                    sumSignal += 0.5;
                }
                else if (_stochastic.DataSeries[0].Values[^1] < _stochastic.DataSeries[1].Values[^1] &&
                         _stochastic.DataSeries[0].Values[^2] < _stochastic.DataSeries[1].Values[^2] &&
                         _stochastic.DataSeries[0].Values[^3] >= _stochastic.DataSeries[1].Values[^3] &&
                         _stochastic.DataSeries[0].Values[^1] > 30)
                {
                    sumSignal -= 0.5;
                }
                n++;
            }
            
            //7. Добавляем 1 балл за повышенный объем
            if (candles[^1].Volume > candles[^2].Volume &&
                candles[^1].Volume > candles[^3].Volume &&
                candles[^1].Volume > candles[^4].Volume)
            {
                if (candles[^1].IsUp) 
                    sumSignal += 1;
                else 
                    sumSignal -= 1;
            }
            n++;

            //8. Баллы за свечной паттерн
            //TODO: переписать на итог по сумме паттернов
            
            return sumSignal / n * 100;
        }

        private List<CandleType> GetCandlePattern(List<Candle> candles) //TODO: дописать свечные паттерны
        {
            if (candles[^1].ShadowBody == 0) return [];
            var bodyPercent = candles[^1].Body / candles[^1].ShadowBody;
            var lastCandle = candles[^1];
            var prevCandle = candles[^2];

            List<CandleType> patterns = [];

            //Strong Candle - большое тело, волатильная, растущий объем
            if (bodyPercent >= 0.75m && lastCandle.Volume > prevCandle.Volume 
                                     && lastCandle.ShadowBody > (decimal)_mobilityPercentile.Min)
            {
                patterns.Add(CandleType.Strong);
            }
            //Doji
            else if(bodyPercent < 0.0002m)
            {
                patterns.Add(CandleType.Doji);
            }
            //Inner Candle
            if (lastCandle.High <= prevCandle.High && lastCandle.Low >= prevCandle.Low)
            {
                patterns.Add(CandleType.Inner);
            }
            //Outer Candle
            else if (lastCandle.High >= prevCandle.High && lastCandle.Low <= prevCandle.Low)
            {
                patterns.Add(CandleType.Outer);
            }
            
            // Patterns with small body
            if (bodyPercent <= 0.3m)
            {
                decimal body = lastCandle.Body == 0 ? 0.00000001m : lastCandle.Body;

                var topShadowRatio = lastCandle.ShadowTop / body;
                var bottomShadowRatio = lastCandle.ShadowBottom / body;

                // Hummer (Hammer)
                if (lastCandle.IsUp && bottomShadowRatio >= 2.0m && topShadowRatio <= 0.5m)
                {
                    patterns.Add(CandleType.Hammer);
                }
                // RevHummer (Inverted Hammer)
                else if (lastCandle.IsUp && topShadowRatio >= 2.0m && bottomShadowRatio <= 0.5m)
                {
                    patterns.Add(CandleType.RevHammer);
                }
                // Hanging Man
                else if (lastCandle.IsDown && bottomShadowRatio >= 2.0m && topShadowRatio <= 0.5m)
                {
                    patterns.Add(CandleType.HangingMan);
                }
                // Shooting Star
                else if (lastCandle.IsDown && topShadowRatio >= 2.0m && bottomShadowRatio <= 0.5m)
                {
                    patterns.Add(CandleType.ShootingStar);
                }
                
                // PinBar
                if ((topShadowRatio >= 3.0m && bottomShadowRatio < 1.0m) || 
                    (bottomShadowRatio >= 3.0m && topShadowRatio < 1.0m))
                {
                    patterns.Add(CandleType.PinBar);
                }
            }

            return patterns;
        }

        public override string GetNameStrategyType()
        {
            return "ShortImpulse";
        }

        public override void ShowIndividualSettingsDialog()
        {
        }

        private double CalculateSkew(List<Candle> candles, int windowLength, bool useLogReturns = true)
        {
            var m = candles.Count;
            var take = Math.Min(windowLength, m);

            var start = m - take;
            var n = take - 1; // number of returns
            if (n < 1) return 0;

            var rets = new double[n];
            var sumRets = 0.0;
            for (int i = 0; i < n; i++)
            {
                var prev = candles[start + i].Close;
                var curr = candles[start + i + 1].Close;

                if (prev <= 0.0m || curr <= 0.0m)
                {
                    rets[i] = 0.0;
                    continue;
                }

                rets[i] = useLogReturns ? Math.Log((double)(curr / prev)) : (double)((curr - prev) / prev);
                sumRets += rets[i];
            }

            var mean = sumRets / n;
            double sum2 = 0.0, sum3 = 0.0;

            for (int i = 0; i < n; i++)
            {
                var d = rets[i] - mean;
                var d2 = d * d;
                sum2 += d2;
                sum3 += d2 * d;
            }

            if (n < 2) return 0.0;

            var s2 = sum2 / (n - 1); // sample variance
            if (s2 <= double.Epsilon) return 0.0;

            var skew = 0.0;
            if (n >= 3)
            {
                skew = (n * sum3) / ((n - 1) * (n - 2) * Math.Pow(s2, 1.5));
            }

            return skew;
        }

        public enum CandleType
        {
            Strong,
            Inner,
            Outer,
            Doji,
            Hammer,
            RevHammer,
            ShootingStar,
            HangingMan,
            PinBar,
        }

        public enum SignalType
        {
            Stochastic,
            Stochastic2,
            Nrtr,
            Nrtr2,
            Kalman,
            Kalman2,
            VolumeUp,
            CandleStrong,
            CandleInner,
            CandleOuter,
            CandleHammer,
            CandleRevHammer,
            CandleShootingStar,
            CandleHangingMan,
            CandlePinBar
        }

        public class Signal
        {
            public DateTime SignalTime = new DateTime();
            public string SignalSide = "BUY";
            public decimal SignalPrice = 0;
            public decimal SignalTake = 0;
            public decimal SignalStop = 0;
            public List<SignalType> SignalName = [];
            public string SignalRegime = "666";
            public decimal SignalFilter = 0;
            public int SignalDayNumber = 0;
            public int SignalHourNumber = 0;
            public bool IsProfit = false;

            public override string ToString()
            {
                return SignalTime.ToString("dd-MM-yyyy HH:mm") + "," +
                       SignalSide + "," +
                       SignalPrice.ToString(CultureInfo.InvariantCulture) + "," +
                       SignalTake.ToString(CultureInfo.InvariantCulture) + "," +
                       SignalStop.ToString(CultureInfo.InvariantCulture) + "," +
                       string.Join("|", SignalName) + "," +
                       SignalRegime + "," +
                       SignalFilter.ToString(CultureInfo.InvariantCulture) + "," +
                       SignalDayNumber + "," +
                       SignalHourNumber + "," +
                       IsProfit.ToString();
            }
        }
    }
}
