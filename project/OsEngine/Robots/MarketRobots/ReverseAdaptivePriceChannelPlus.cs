using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Market.CustomConnectors.Coinglass;
using OsEngine.Market.CustomConnectors.Coinglass.Entity;
using OsEngine.Market.Servers.Optimizer;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.Robots.MyBots
{
    [Bot("ReverseAdaptivePriceChannelPlus")]
    public class ReverseAdaptivePriceChannelPlus : BotPanel
    {
        private readonly BotTabSimple _tab;

        public StrategyParameterString Regime;
        public StrategyParameterDecimal VolumeOnPosition;
        public StrategyParameterString VolumeRegime;
        public StrategyParameterDecimal Slippage;

        private StrategyParameterTimeOfDay TimeStart;
        private StrategyParameterTimeOfDay TimeEnd;

        public Aindicator _APC;
        private StrategyParameterInt AdxPeriod;
        private StrategyParameterInt Ratio;

        private Aindicator _smaFilter;
        private StrategyParameterInt SmaLengthFilter;
        public StrategyParameterBool SmaPositionFilterIsOn;
        public StrategyParameterBool SmaSlopeFilterIsOn;
        //-------------------------------------------------------
        public StrategyParameterBool LongShortRatioFilterIsOn;
        public StrategyParameterString ApiKey;
        public StrategyParameterString MinIntervalTimeFrame;
        public StrategyParameterString ExchageForLongShort;
        private StrategyParameterDecimal LongShortRatioBuy;
        private StrategyParameterDecimal LongShortRatioSell;
        private StrategyParameterBool PrintLSR;
        private List<LongShortRatio> _longShortRatio = new List<LongShortRatio>();
        private CoinglassConnector _conn;
        private readonly RequestContent _requestContent = new RequestContent();
        //-------------------------------------------------------

        public ReverseAdaptivePriceChannelPlus(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
            VolumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency", "% of the total portfolio" }, "Base");
            VolumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");

            Slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");

            TimeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            TimeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            //-------------------------------------------------------
            ApiKey = CreateParameter("API Key", "", "Base");
            MinIntervalTimeFrame = CreateParameter("Min History Interval API", "1d", new[] { "1m", "5m", "15m", "30m", "1h", "4h", "6h", "8h", "12h", "1d" }, "Base");
            ExchageForLongShort = CreateParameter("Exchange for Long Short", "Binance", new[] { "Binance", "Bybit" }, "Base");
            LongShortRatioBuy = CreateParameter("Long Short Ratio Buy", 0.96078m, 0.94m, 0.98m, 0.0001m, "Base");
            LongShortRatioSell = CreateParameter("Long Short Ratio Sell", 1.04m, 0.94m, 0.98m, 0.0001m, "Base");
            LongShortRatioFilterIsOn = CreateParameter("Is Long Short Ratio Filter On", false, "Filters");
            PrintLSR = CreateParameter("Print LSR Data on Chart", false, "Filters");
            //-------------------------------------------------------

            AdxPeriod = CreateParameter("Ronco Period", 14, 2, 300, 12, "Robot parameters");
            Ratio = CreateParameter("Ratio", 100, 50, 300, 10, "Robot parameters");

            SmaLengthFilter = CreateParameter("Sma Length Filter", 100, 10, 500, 1, "Filters");

            SmaPositionFilterIsOn = CreateParameter("Is SMA Filter On", false, "Filters");
            SmaSlopeFilterIsOn = CreateParameter("Is Sma Slope Filter On", false, "Filters");

            _smaFilter = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma_Filter", canDelete: false);
            _smaFilter = (Aindicator)_tab.CreateCandleIndicator(_smaFilter, nameArea: "Prime");
            _smaFilter.DataSeries[0].Color = System.Drawing.Color.Azure;
            _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
            _smaFilter.Save();

            _APC = IndicatorsFactory.CreateIndicatorByName("AdaptivePriceChannel_Indicator", name + "APC", false);
            _APC = (Aindicator)_tab.CreateCandleIndicator(_APC, "Prime");
            _APC.ParametersDigit[0].Value = AdxPeriod.ValueInt;
            _APC.ParametersDigit[1].Value = Ratio.ValueInt;
            _APC.Save();

            StopOrActivateIndicators();
            ParametrsChangeByUser += RoncoParam_ParametrsChangeByUser;
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            RoncoParam_ParametrsChangeByUser();
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;

            _requestContent.StartProgram = startProgram; //Запоминаем в запрос вызывающую программу
        }

        //Обработка обновления из коннектора
        private void CoinglassUpdateEvent(string botName, ResponseType type, List<LongShortRatio> lsrList)
        {
            if (botName != _tab.TabName) return; //если имя вкладки не совпало - выходим

            if (type == ResponseType.LongShortRatio)
            {
                _longShortRatio = lsrList; //обновляем значение показателя
            }
        }

        private void _tab_PositionOpeningSuccesEvent(Position obj)
        {
            _tab.SellAtStopCancel();
            _tab.BuyAtStopCancel();
        }

        private void RoncoParam_ParametrsChangeByUser()
        {
            StopOrActivateIndicators();

            if (_APC.ParametersDigit[0].Value != AdxPeriod.ValueInt ||
                _APC.ParametersDigit[1].Value != Ratio.ValueInt)
            {
                _APC.ParametersDigit[0].Value = AdxPeriod.ValueInt;
                _APC.ParametersDigit[1].Value = Ratio.ValueInt;
                _APC.Save();
                _APC.Reload();
            }

            if (_smaFilter.DataSeries.Count == 0)
            {
                return;
            }
            if (_smaFilter.ParametersDigit[0].Value != SmaLengthFilter.ValueInt)
            {
                _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
                _smaFilter.Reload();
                _smaFilter.Save();
            }

            if (_smaFilter.DataSeries != null && _smaFilter.DataSeries.Count > 0)
            {
                if (!SmaPositionFilterIsOn.ValueBool && !SmaSlopeFilterIsOn.ValueBool)
                {
                    _smaFilter.DataSeries[0].IsPaint = false;
                }
                else if (SmaPositionFilterIsOn.ValueBool || SmaSlopeFilterIsOn.ValueBool)
                {
                    _smaFilter.DataSeries[0].IsPaint = true;
                }
            }
        }

        private void StopOrActivateIndicators()
        {
            if (SmaPositionFilterIsOn.ValueBool == false
                && SmaSlopeFilterIsOn.ValueBool == false
                && _smaFilter.IsOn)
            {
                _smaFilter.IsOn = false;
                _smaFilter.Reload();
            }
            else if ((SmaPositionFilterIsOn.ValueBool
                      || SmaSlopeFilterIsOn.ValueBool)
                     && _smaFilter.IsOn == false)
            {
                _smaFilter.IsOn = true;
                _smaFilter.Reload();
            }
        }

        public override string GetNameStrategyType()
        {
            return "ReverseAdaptivePriceChannelPlus";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        private void Server_TestingEndEvent(int obj)
        {
            _conn.CoinglassUpdateEvent -= CoinglassUpdateEvent;
            _conn = null;
        }

        // Logic
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (_conn == null)
            {
                _conn = CoinglassConnector.GetServer(ApiKey.ValueString); //Получаем или создаем коннектор
                _conn.CoinglassUpdateEvent += CoinglassUpdateEvent; //Подписываемся на обновления из коннектора

                if (StartProgram == StartProgram.IsOsOptimizer)
                {
                    OptimizerServer server = (OptimizerServer)_tab.Connector.MyServer;
                    server.TestingEndEvent += Server_TestingEndEvent;
                }
            }

            //----------------------------------------------------------------
            DateTime dt = candles[candles.Count - 1].TimeStart; //запоминаем время последней свечи

            if (LongShortRatioFilterIsOn.ValueBool) //если фильтр по LSR включен
            {
                //заполняем поля класса запроса
                _requestContent.Exchange = ExchageForLongShort.ValueString; //биржа, на которой смотрим показатель
                _requestContent.Interval = MinIntervalTimeFrame.ValueString; //мин. доступный интервал для ключа API
                _requestContent.BotName = _tab.TabName; //имя вкладки бота
                _requestContent.ResponseType = ResponseType.LongShortRatio; //запрашиваемый показатель
                _requestContent.Symbol = _tab.Securiti.Name; //название инструмента

                if (_requestContent.StartProgram != StartProgram.IsOsTrader) //вызывающая программа не OsTrader
                {
                    if (_longShortRatio == null || _longShortRatio.Count < 2) //если данные еще не получены
                    {
                        _conn.SendRequest(_requestContent); //отправляем запрос серверу
                        while (_longShortRatio == null || _longShortRatio.Count < 2) //ждем поступления данных
                        {
                            Thread.Sleep(500);
                        }
                    }
                }
                else
                {
                    _conn.SendRequest(_requestContent); //отправляем запрос серверу
                }
            }
            //----------------------------------------------------------------

            if (TimeStart.Value > _tab.TimeServerCurrent ||
                TimeEnd.Value < _tab.TimeServerCurrent)
            {
                CancelStopsAndProfits();
                return;
            }

            if (candles.Count < AdxPeriod.ValueInt + 10 ||
                candles.Count < 50)
            {
                return;
            }

            decimal upChannel = _APC.DataSeries[0].Last;
            decimal downChannel = _APC.DataSeries[1].Last;

            if (upChannel == 0 || downChannel == 0)
            {
                return;
            }

            List<Position> positions = _tab.PositionsOpenAll;

            //если активирован PrintLSR - на последней свече пишем значение показателя и ставим цветную метку
            if (PrintLSR.ValueBool)
            {
                DrawLabelOnChart(candles, candles[candles.Count - 1].IsUp ? Side.Buy : Side.Sell);
            }

            if (positions.Count == 0)
            {
                if (BuySignalIsFiltered(candles) == false)
                {
                    decimal slippage = Slippage.ValueDecimal * upChannel / 100;
                    _tab.BuyAtStopCancel();
                    _tab.BuyAtStop(GetVolume(), upChannel + _tab.Securiti.PriceStep + slippage, upChannel + _tab.Securiti.PriceStep,
                        StopActivateType.HigherOrEqual);
                }

                if (SellSignalIsFiltered(candles) == false)
                {
                    decimal slippage = Slippage.ValueDecimal * downChannel / 100;
                    _tab.SellAtStopCancel();
                    _tab.SellAtStop(GetVolume(), downChannel - _tab.Securiti.PriceStep - slippage, downChannel - _tab.Securiti.PriceStep,
                        StopActivateType.LowerOrEqyal);
                }
            }
            else
            {
                _tab.SellAtStopCancel();
                _tab.BuyAtStopCancel();

                Position pos = positions[0];

                if (positions.Count > 1)
                {

                }

                if (pos.CloseActiv)
                {
                    return;
                }

                if (pos.Direction == Side.Buy)
                {
                    decimal priceLine = downChannel - _tab.Securiti.PriceStep;
                    decimal priceOrder = downChannel - _tab.Securiti.PriceStep;
                    decimal slippage = Slippage.ValueDecimal * priceOrder / 100;

                    if (SellSignalIsFiltered(candles) == false)
                    {
                        _tab.SellAtStopCancel();
                        _tab.SellAtStop(GetVolume(), priceOrder - slippage, priceLine, StopActivateType.LowerOrEqyal);
                    }

                    //если коэф. longshort > LongShortRatioSell и фильтр включен
                    if (LongShortRatioFilterIsOn.ValueBool)
                    {
                        decimal lsr = GetLsr(dt, _longShortRatio);

                        if (lsr > LongShortRatioSell.ValueDecimal)
                            _tab.CloseAtMarket(pos, pos.OpenVolume); //закрываем позицию по рынку
                    }

                    _tab.CloseAtStop(pos, priceLine, priceOrder - slippage);
                }
                else if (pos.Direction == Side.Sell)
                {
                    decimal priceLine = upChannel + _tab.Securiti.PriceStep;
                    decimal priceOrder = upChannel + _tab.Securiti.PriceStep;
                    decimal slippage = Slippage.ValueDecimal * priceOrder / 100;

                    if (BuySignalIsFiltered(candles) == false)
                    {
                        _tab.BuyAtStopCancel();
                        _tab.BuyAtStop(GetVolume(), priceOrder + slippage, priceLine, StopActivateType.HigherOrEqual);
                    }

                    if (LongShortRatioFilterIsOn.ValueBool)
                    {
                        decimal lsr = GetLsr(dt, _longShortRatio);

                        if (lsr < LongShortRatioBuy.ValueDecimal)
                            _tab.CloseAtMarket(pos, pos.OpenVolume);
                    }

                    _tab.CloseAtStop(pos, priceLine, priceOrder + slippage);
                }
            }
        }

        private bool BuySignalIsFiltered(List<Candle> candles)
        {
            decimal lastPrice = candles[candles.Count - 1].Close;
            decimal lastSma = _smaFilter.DataSeries[0].Last;
            DateTime dt = candles[candles.Count - 1].TimeStart;

            // filter for buy
            if (Regime.ValueString == "Off" ||
                Regime.ValueString == "OnlyShort" ||
                Regime.ValueString == "OnlyClosePosition")
            {
                return true;
                //if the robot's operating mode does not correspond to the direction of the positio
            }

            if (SmaPositionFilterIsOn.ValueBool)
            {
                if (_smaFilter.DataSeries[0].Last > lastPrice)
                {
                    return true;
                }
                // if the price is lower than the last Sma - return true to the top
            }
            if (SmaSlopeFilterIsOn.ValueBool)
            {
                decimal prevSma = _smaFilter.DataSeries[0].Values[_smaFilter.DataSeries[0].Values.Count - 2];

                if (lastSma < prevSma)
                {
                    return true;
                }
                // if the last Sma is lower than the previous Sma - return true to the top
            }
            //----------------------------------------------------------------
            if (LongShortRatioFilterIsOn.ValueBool) //если включен фильтр LongShortRatioFilter
            {
                decimal lsr = GetLsr(dt, _longShortRatio); //получаем актуальное значение LongShortRatio

                if (lsr == 0) return true; //если значение показателя не обновлено - возвращаем true
                // если LongShortRatio больше порогового значения - возвращаем true
                if (lsr > LongShortRatioBuy.ValueDecimal) return true;
            }
            //----------------------------------------------------------------

            return false;
        }

        private bool SellSignalIsFiltered(List<Candle> candles)
        {
            decimal lastPrice = candles[candles.Count - 1].Close;
            decimal lastSma = _smaFilter.DataSeries[0].Last;
            DateTime dt = candles[candles.Count - 1].TimeStart;

            // filter for sell
            if (Regime.ValueString == "Off" ||
                Regime.ValueString == "OnlyLong" ||
                Regime.ValueString == "OnlyClosePosition")
            {
                return true;
                //if the robot's operating mode does not correspond to the direction of the position
            }
            if (SmaPositionFilterIsOn.ValueBool)
            {
                if (lastSma < lastPrice)
                {
                    return true;
                }
                // if the price is higher than the last Sma - return true to the top
            }
            if (SmaSlopeFilterIsOn.ValueBool)
            {
                decimal prevSma = _smaFilter.DataSeries[0].Values[_smaFilter.DataSeries[0].Values.Count - 2];

                if (lastSma > prevSma)
                {
                    return true;
                }
                // if the last Sma is higher than the previous Sma - return true to the top
            }
            //----------------------------------------------------------------
            if (LongShortRatioFilterIsOn.ValueBool) //если включен фильтр LongShortRatioFilter
            {
                decimal lsr = GetLsr(dt, _longShortRatio); //получаем актуальный LongShortRatio

                if (lsr == 0) return true; //если значение показателя не обновлено - возвращаем true
                // если LongShortRatio меньше порогового значения - возвращаем true
                if (lsr < LongShortRatioSell.ValueDecimal) return true;
            }
            //-----------------------------------------------------------------
            return false;
        }

        private void CancelStopsAndProfits()
        {
            List<Position> positions = _tab.PositionsOpenAll;

            for (int i = 0; i < positions.Count; i++)
            {
                Position pos = positions[i];

                pos.StopOrderIsActiv = false;
                pos.ProfitOrderIsActiv = false;
            }

            _tab.BuyAtStopCancel();
            _tab.SellAtStopCancel();
        }

        private decimal GetVolume()
        {
            decimal volume = 0;

            if (VolumeRegime.ValueString == "Contract currency")
            {
                decimal contractPrice = TabsSimple[0].PriceBestAsk;
                volume = VolumeOnPosition.ValueDecimal / contractPrice;

            }
            else if (VolumeRegime.ValueString == "Number of contracts")
            {
                volume = VolumeOnPosition.ValueDecimal;
            }
            else //if (VolumeRegime.ValueString == "% of the total portfolio")
            {
                volume = _tab.Portfolio.ValueCurrent * (VolumeOnPosition.ValueDecimal / 100) / _tab.PriceBestAsk / _tab.Securiti.Lot;
            }

            // If the robot is running in the tester
            if (StartProgram == StartProgram.IsTester)
            {
                volume = Math.Round(volume, 6);
            }
            else
            {
                volume = Math.Round(volume, _tab.Securiti.DecimalsVolume);
            }
            return volume;
        }

        /// <summary>
        /// Получает актуальное значение показателя LongShortRatio
        /// </summary>
        /// <param name="dt">Время актуальной свечи</param>
        /// <param name="lsr">Список всех доступных объектов LongShortRatio</param>
        /// <returns>Значение LongShortRatio</returns>
        private decimal GetLsr(DateTime dt, List<LongShortRatio> lsr)
        {
            if (lsr == null || lsr.Count == 0) return 0;

            if (_requestContent.StartProgram == StartProgram.IsOsTrader)
            {
                return lsr[0].LSR;
            }

            for (int i = 1; i < lsr.Count; i++)
            {
                if (lsr[i].Time > dt && lsr[i - 1].Time <= dt)
                    return lsr[i - 1].LSR;
            }

            return 0;
        }

        private PointElement _point;

        /// <summary>
        /// Вывод на график значения показателя LSR
        /// </summary>
        /// <param name="candles">Список свечей</param>
        /// <param name="side">Сторона сделки</param>
        private void DrawLabelOnChart(List<Candle> candles, Side side)
        {
            if (StartProgram == StartProgram.IsOsOptimizer)
            {
                return;
            }

            DateTime dt = candles[candles.Count - 1].TimeStart;
            decimal lsr = GetLsr(dt, _longShortRatio);

            if (lsr == 0) { return; }

            PointElement point = new PointElement("LSR", "Prime");

            if (side == Side.Buy)
            {
                point.Y = candles[candles.Count - 1].Low;
                point.Color = Color.Green;
            }
            else
            {
                point.Y = candles[candles.Count - 1].High;
                point.Color = Color.Red;
            }

            point.TimePoint = candles[candles.Count - 1].TimeStart;
            point.Label = "LSR: " + lsr;
            point.Font = new Font("Arial", 8);
            point.LabelTextColor = Color.White;
            point.Size = 6;

            _point = point;

            _tab.SetChartElement(_point);
        }
    }
}
