using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Windows.Media.Effects;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.Robots.MarketRobots
{
    [Bot("BreakLinearRegressionChannelReverse")]
    public class BreakLinearRegressionChannelReverse : BotPanel
    {
        private BotTabSimple _tab;

        private StrategyParameterString Regime;
        private StrategyParameterBool ReverseLogic;
        private StrategyParameterDecimal VolumeOnPosition;
        private StrategyParameterString VolumeRegime;
        private StrategyParameterInt VolumeDecimals;
        private StrategyParameterDecimal Slippage;

        private StrategyParameterTimeOfDay TimeStart;
        private StrategyParameterTimeOfDay TimeEnd;

        private Aindicator _LinearRegression;
        private StrategyParameterDecimal UpDeviation;
        private StrategyParameterInt PeriodLR;

        private Aindicator _smaFilter;
        private StrategyParameterInt SmaLengthFilter;

        public BreakLinearRegressionChannelReverse(string name, StartProgram startProgram)
            : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
            ReverseLogic = CreateParameter("Reverse logic", false, "Base");
            VolumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency" }, "Base");
            VolumeDecimals = CreateParameter("Number of Digits after the decimal point in the volume", 2, 1, 50, 4, "Base");
            VolumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");

            Slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");

            TimeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            TimeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            PeriodLR = CreateParameter("Period Linear Regression", 50, 50, 300, 1, "Robot parameters");
            UpDeviation = CreateParameter("Deviation LR", 1, 0.1m, 3, 0.1m, "Robot parameters");

            SmaLengthFilter = CreateParameter("Sma Length Filter", 100, 10, 500, 1, "Filters");

            _smaFilter = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma_Filter", canDelete: false);
            _smaFilter = (Aindicator)_tab.CreateCandleIndicator(_smaFilter, nameArea: "Prime");
            _smaFilter.DataSeries[0].Color = System.Drawing.Color.Azure;
            _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
            _smaFilter.IsOn = true;
            _smaFilter.Save();

            _LinearRegression = IndicatorsFactory.CreateIndicatorByName("LinearRegressionChannelFast_Indicator", name + "LinearRegressionChannel", false);
            _LinearRegression = (Aindicator)_tab.CreateCandleIndicator(_LinearRegression, "Prime");
            _LinearRegression.ParametersDigit[0].Value = PeriodLR.ValueInt;
            _LinearRegression.ParametersDigit[1].Value = UpDeviation.ValueDecimal;
            _LinearRegression.ParametersDigit[2].Value = UpDeviation.ValueDecimal;
            _LinearRegression.Save();

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            ParametrsChangeByUser += LinearRegressionTraderParam_ParametrsChangeByUser;
            LinearRegressionTraderParam_ParametrsChangeByUser();
        }

        private void LinearRegressionTraderParam_ParametrsChangeByUser()
        {
            if (_LinearRegression.ParametersDigit[0].Value != PeriodLR.ValueInt ||
            _LinearRegression.ParametersDigit[1].Value != UpDeviation.ValueDecimal ||
            _LinearRegression.ParametersDigit[2].Value != UpDeviation.ValueDecimal)
            {
                _LinearRegression.ParametersDigit[0].Value = PeriodLR.ValueInt;
                _LinearRegression.ParametersDigit[1].Value = UpDeviation.ValueDecimal;
                _LinearRegression.ParametersDigit[2].Value = UpDeviation.ValueDecimal;
                _LinearRegression.Save();
                _LinearRegression.Reload();
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
        }

        public override string GetNameStrategyType()
        {
            return "BreakLinearRegressionChannelReverse";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        // логика

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            // использование
            if (TimeStart.Value > _tab.TimeServerCurrent ||
                TimeEnd.Value < _tab.TimeServerCurrent)
            {
                CancelStopsAndProfits();
                return;
            }

            if (SmaLengthFilter.ValueInt >= candles.Count)
            {
                return;
            }

            if (candles.Count < 20)
            {
                return;
            }

            List<Position> positions = _tab.PositionsOpenAll;

            if (positions.Count == 0)
            {
                TryOpenPosition(candles);
            }
            else
            {
                TryClosePosition(positions[0]);
            }
        }

        private bool BuySignalIsFiltered(List<Candle> candles)
        {
            decimal lastPrice = candles[candles.Count - 1].Close;
            decimal lastSma = _smaFilter.DataSeries[0].Last;
            // фильтр для покупок
            if (Regime.ValueString == "Off" ||
                Regime.ValueString == "OnlyShort" ||
                Regime.ValueString == "OnlyClosePosition")
            {
                return true;
                //если режим работы робота не соответсвует направлению позиции
            }

            if (_smaFilter.DataSeries[0].Last > lastPrice)
            {
                return true;
            }
            // если цена ниже последней сма - возвращаем на верх true

            decimal prevSma = _smaFilter.DataSeries[0].Values[_smaFilter.DataSeries[0].Values.Count - 2];

            if (lastSma < prevSma)
            {
                return true;
            }
            // если последняя сма ниже предыдущей сма - возвращаем на верх true

            return false;
        }

        private bool SellSignalIsFiltered(List<Candle> candles)
        {
            decimal lastPrice = candles[candles.Count - 1].Close;
            decimal lastSma = _smaFilter.DataSeries[0].Last;
            // фильтр для продаж
            if (Regime.ValueString == "Off" ||
                Regime.ValueString == "OnlyLong" ||
                Regime.ValueString == "OnlyClosePosition")
            {
                return true;
                //если режим работы робота не соответсвует направлению позиции
            }

            if (lastSma < lastPrice)
            {
                return true;
            }
            // если цена выше последней сма - возвращаем на верх true

            decimal prevSma = _smaFilter.DataSeries[0].Values[_smaFilter.DataSeries[0].Values.Count - 2];

            if (lastSma > prevSma)
            {
                return true;
            }
            // если последняя сма выше предыдущей сма - возвращаем на верх true

            return false;
        }

        private void TryOpenPosition(List<Candle> candles)
        {
            decimal upChannel = _LinearRegression.DataSeries[0].Last;
            decimal downChannel = _LinearRegression.DataSeries[2].Last;

            if (upChannel == 0 ||
                downChannel == 0)
            {
                return;
            }

            bool signalUpLine = candles[candles.Count - 1].Close > upChannel;
            bool signalDownLine = candles[candles.Count - 1].Close < downChannel;

            if (signalUpLine) // При пересечении верхней линии канала
            {
                if (ReverseLogic.ValueBool)
                {
                    if (!SellSignalIsFiltered(candles))//если метод возвращает false можно входить в сделку
                        _tab.SellAtLimit(GetVolume(), upChannel - GetSlippage(upChannel));
                }
                else
                {
                    if (!BuySignalIsFiltered(candles))//если метод возвращает false можно входить в сделку
                        _tab.BuyAtLimit(GetVolume(), upChannel + GetSlippage(upChannel));
                }
            }
            else if (signalDownLine) // При пересечении нижней линии канала
            {
                if (ReverseLogic.ValueBool)
                {
                    if (!BuySignalIsFiltered(candles))//если метод возвращает false можно входить в сделку
                        _tab.BuyAtLimit(GetVolume(), upChannel + GetSlippage(upChannel));
                }
                else
                {
                    if (!SellSignalIsFiltered(candles))//если метод возвращает false можно входить в сделку
                        _tab.SellAtLimit(GetVolume(), upChannel - GetSlippage(upChannel));
                }
            }
        }

        private void TryClosePosition(Position position)
        {
            decimal upChannel = _LinearRegression.DataSeries[0].Last;
            decimal downChannel = _LinearRegression.DataSeries[2].Last;

            if (upChannel == 0 ||
                downChannel == 0)
            {
                return;
            }

            if (position.Direction == Side.Buy)
            {
                if (ReverseLogic.ValueBool)
                {
                    _tab.CloseAtProfit(position, upChannel, upChannel - GetSlippage(upChannel));
                }
                else
                {
                    _tab.CloseAtStop(position, downChannel, downChannel - GetSlippage(downChannel));
                }
            }
            else if (position.Direction == Side.Sell)
            {
                if (ReverseLogic.ValueBool)
                {
                    _tab.CloseAtProfit(position, downChannel, downChannel + GetSlippage(downChannel));
                }
                else
                {
                    _tab.CloseAtStop(position, upChannel, upChannel + GetSlippage(upChannel));
                }
            }
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
            decimal volume = VolumeOnPosition.ValueDecimal;

            if (VolumeRegime.ValueString == "Contract currency") // "Валюта контракта"
            {
                decimal contractPrice = TabsSimple[0].PriceBestAsk;
                volume = Math.Round(VolumeOnPosition.ValueDecimal / contractPrice, VolumeDecimals.ValueInt);
                return volume;
            }
            else //if (VolumeRegime.ValueString == "Number of contracts")
            {
                return volume;
            }
        }

        private decimal GetSlippage(decimal price)
        {
            return price * Slippage.ValueDecimal / 100;
        }
    }
}


