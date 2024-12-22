using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System.Collections.Generic;
using System.Drawing;
using System;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.Robots.Classes;


[Bot("ImpulseHma")]
public class ImpulseHma : BotPanel
{
    BotTabSimple _tab;

    public StrategyParameterString Regime;
    public StrategyParameterDecimal VolumeOnPosition;
    public StrategyParameterString VolumeRegime;
    public StrategyParameterDecimal Slippage;

    private StrategyParameterTimeOfDay TimeStart;
    private StrategyParameterTimeOfDay TimeEnd;

    public Aindicator _Sma;
    public StrategyParameterInt _periodSma;

    public Aindicator _hma;
    public StrategyParameterInt _periodHma;

    public Aindicator _hma2;
    public StrategyParameterInt _periodHma2;

    public Aindicator _atr;
    public StrategyParameterInt _periodAtr;
    public StrategyParameterDecimal _multiplerAtr;

    public Aindicator _smaFilter;
    private StrategyParameterInt SmaLengthFilter;
    public StrategyParameterBool SmaPositionFilterIsOn;
    public StrategyParameterBool SmaSlopeFilterIsOn;

    // создаем переменные для Трейлинг стопа
    //---------------------------------
    private TrailingStop _trailingStop;
    private StrategyParameterBool TrailingStopIsOn;
    private StrategyParameterString TrailingStopTypeOrder;
    private StrategyParameterDecimal ChangeStepStop;
    private StrategyParameterDecimal MinDist;
    private StrategyParameterDecimal QuantityStepsPrices;
    //---------------------------------

    public ImpulseHma(string name, StartProgram startProgram) : base(name, startProgram)
    {
        TabCreate(BotTabType.Simple);
        _tab = TabsSimple[0];

        Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
        VolumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency", "% of the total portfolio" }, "Base");
        VolumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");
        Slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");

        TimeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
        TimeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

        _periodSma = CreateParameter("SMA period", 500, 100, 1000, 100, "Robot parameters");
        _periodHma = CreateParameter("HMA period", 500, 100, 1000, 100, "Robot parameters");
        _periodHma2 = CreateParameter("HMA2 period", 150, 50, 500, 100, "Robot parameters");
        _periodAtr = CreateParameter("Atr period", 14, 5, 50, 5, "Robot parameters");
        _multiplerAtr = CreateParameter("Atr multipler", 1m, 0.1m, 5.0m, 0.5m, "Robot parameters");

        SmaLengthFilter = CreateParameter("Sma Length Filter", 100, 10, 500, 1, "Filters");

        SmaPositionFilterIsOn = CreateParameter("Is SMA Filter On", false, "Filters");
        SmaSlopeFilterIsOn = CreateParameter("Is Sma Slope Filter On", false, "Filters");

        // создаем параметры настроек и создаем объект класса TrailingStop
        //---------------------------------
        TrailingStopIsOn = CreateParameter("Is Trailing stop On", false, "Trailing Stop");
        TrailingStopTypeOrder = CreateParameter("Type order", OrderPriceType.Market.ToString(), new[] { OrderPriceType.Market.ToString(), OrderPriceType.Limit.ToString() }, "Trailing Stop");
        ChangeStepStop = CreateParameter("Stop level change step (points)", 1, 1, 100, 001m, "Trailing Stop");
        MinDist = CreateParameter("Minimum distance to price (points)", 1, 1, 100, 0.01m, "Trailing Stop");
        QuantityStepsPrices = CreateParameter("Quantity steps prices for limit order", 0m, 0, 10000, 1, "Trailing Stop");
        _trailingStop = new TrailingStop(_tab, TrailingStopTypeOrder.ValueString, ChangeStepStop.ValueDecimal, MinDist.ValueDecimal, QuantityStepsPrices.ValueDecimal);
        //---------------------------------

        _smaFilter = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma_Filter", canDelete: false);
        _smaFilter = (Aindicator)_tab.CreateCandleIndicator(_smaFilter, nameArea: "Prime");
        _smaFilter.DataSeries[0].Color = Color.Azure;
        _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
        _smaFilter.Save();

        _Sma = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma", canDelete: false);
        _Sma = (Aindicator)_tab.CreateCandleIndicator(_Sma, nameArea: "Prime");
        _Sma.ParametersDigit[0].Value = _periodSma.ValueInt;
        _Sma.DataSeries[0].Color = Color.Green;
        _Sma.Save();

        _hma = IndicatorsFactory.CreateIndicatorByName("HMA_indicator", name: name + "HMA", canDelete: false);
        _hma = (Aindicator)_tab.CreateCandleIndicator(_hma, nameArea: "Prime");
        _hma.ParametersDigit[0].Value = _periodHma.ValueInt;
        _hma.DataSeries[0].Color = Color.Red;
        _hma.Save();

        _hma2 = IndicatorsFactory.CreateIndicatorByName("HMA_indicator", name: name + "HMA2", canDelete: false);
        _hma2 = (Aindicator)_tab.CreateCandleIndicator(_hma2, nameArea: "Prime");
        _hma2.ParametersDigit[0].Value = _periodHma2.ValueInt;
        _hma2.DataSeries[0].Color = Color.Blue;
        _hma2.Save();

        _atr = IndicatorsFactory.CreateIndicatorByName(nameClass: "ATR", name: name + "ATR", canDelete: false);
        _atr = (Aindicator)_tab.CreateCandleIndicator(_atr, nameArea: "New1");
        _atr.ParametersDigit[0].Value = _periodAtr.ValueInt;
        _atr.Save();

        StopOrActivateIndicators();
        _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
        ParametrsChangeByUser += LRegBot_ParametrsChangeByUser;
        LRegBot_ParametrsChangeByUser();

        _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
    }

    private void LRegBot_ParametrsChangeByUser()
    {
        StopOrActivateIndicators();

        if (_Sma.ParametersDigit[0].Value != _periodSma.ValueInt)
        {
            _Sma.ParametersDigit[0].Value = _periodSma.ValueInt;
            _Sma.Reload();
            _Sma.Save();
        }

        if (_hma.ParametersDigit[0].Value != _periodHma.ValueInt)
        {
            _hma.ParametersDigit[0].Value = _periodHma.ValueInt;
            _hma.Reload();
            _hma.Save();
        }

        if (_hma2.ParametersDigit[0].Value != _periodHma2.ValueInt)
        {
            _hma2.ParametersDigit[0].Value = _periodHma2.ValueInt;
            _hma2.Reload();
            _hma2.Save();
        }

        if (_atr.ParametersDigit[0].Value != _periodAtr.ValueInt)
        {
            _atr.ParametersDigit[0].Value = _periodAtr.ValueInt;
            _atr.Reload();
            _atr.Save();
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

        // если мы меняли параметры настроек, то пересоздаем объект класса TrailingStop
        //---------------
        if (TrailingStopIsOn.ValueBool)
        {
            _trailingStop = null;
            _trailingStop = new TrailingStop(_tab, TrailingStopTypeOrder.ValueString, ChangeStepStop.ValueDecimal, MinDist.ValueDecimal, QuantityStepsPrices.ValueDecimal);
        }
        else
        {
            _trailingStop = null;
        }
        //-------------------
    }

    private void StopOrActivateIndicators()
    {
        if (SmaPositionFilterIsOn.ValueBool == false
                  && SmaSlopeFilterIsOn.ValueBool == false
                  && _smaFilter.IsOn == true)
        {
            _smaFilter.IsOn = false;
            _smaFilter.Reload();
        }
        else if ((SmaPositionFilterIsOn.ValueBool == true
            || SmaSlopeFilterIsOn.ValueBool == true)
            && _smaFilter.IsOn == false)
        {
            _smaFilter.IsOn = true;
            _smaFilter.Reload();
        }
    }

    private void _tab_PositionOpeningSuccesEvent(Position obj)
    {
        // этот код для того, чтобы стоп открывался в тот же момент, когда окрывается ордер
        // если включен режим трейлинг стопа, то обращаемся к методу SetTrailingStop и передаем в него цену закрытия последней свечи
        //-----------------------------------------
        if (TrailingStopIsOn.ValueBool)
        {
            _trailingStop.SetTrailingStop(obj.EntryPrice);
            return;
        }
        //--------------------------------------
    }

    public override string GetNameStrategyType()
    {
        return "ImpulseHma";
    }

    public override void ShowIndividualSettingsDialog()
    {

    }

    // Logic
    private void _tab_CandleFinishedEvent(List<Candle> candles)
    {
        if (Regime.ValueString == "Off") { return; }

        if (TimeStart.Value > _tab.TimeServerCurrent ||
            TimeEnd.Value < _tab.TimeServerCurrent)
        {
            CancelStopsAndProfits();
            return;
        }

        if (_tab.CandlesAll == null) { return; }
        if (_periodSma.ValueInt +10 > candles.Count || _periodAtr.ValueInt > candles.Count) { return; }
        if (_periodHma.ValueInt +30 > candles.Count || _periodHma2.ValueInt +10 > candles.Count) { return; }

        if (SmaLengthFilter.ValueInt +10 >= candles.Count)
        {
            return;
        }

        List<Position> positions = _tab.PositionsOpenAll;

        decimal lastPrice = candles[candles.Count - 1].Close;

        decimal lastSma = _Sma.DataSeries[0].Last;
        decimal prewSma = _Sma.DataSeries[0].Values[candles.Count - 2];
        decimal prew2Sma = _Sma.DataSeries[0].Values[candles.Count - 3];
        decimal lastHma = _hma.DataSeries[0].Last;
        decimal prewHma = _hma.DataSeries[0].Values[candles.Count - 2];
        decimal prew2Hma = _hma.DataSeries[0].Values[candles.Count - 3];
        decimal lastFHma = _hma.DataSeries[1].Last;
        decimal lastHma2 = _hma2.DataSeries[0].Last;
        decimal prewHma2 = _hma2.DataSeries[0].Values[candles.Count - 2];
        decimal prew2Hma2 = _hma2.DataSeries[0].Values[candles.Count - 3];
        decimal lastFHma2 = _hma2.DataSeries[1].Last;
        decimal lastAtr = _atr.DataSeries[0].Last;
        decimal _slippage = 0;

        if (positions.Count == 0 && Regime.ValueString != "OnlyClosePosition")
        {// enter logic
            if (!BuySignalIsFiltered(candles))
            {
                _slippage = Slippage.ValueDecimal * (lastHma + lastAtr * _multiplerAtr.ValueDecimal) / 100;
                _tab.BuyAtStop(GetVolume(), (lastHma + lastAtr * _multiplerAtr.ValueDecimal) + _slippage, lastHma + lastAtr * _multiplerAtr.ValueDecimal, StopActivateType.HigherOrEqual, 1);
            }
            if (!SellSignalIsFiltered(candles))
            {
                _slippage = Slippage.ValueDecimal * (lastHma - lastAtr * _multiplerAtr.ValueDecimal) / 100;
                _tab.SellAtStop(GetVolume(), (lastHma - lastAtr * _multiplerAtr.ValueDecimal) - _slippage, lastHma - lastAtr * _multiplerAtr.ValueDecimal, StopActivateType.LowerOrEqyal, 1);
            }

            if (BuySignalIsFiltered(candles))
            {
                _tab.BuyAtStopCancel();
            }
            if (SellSignalIsFiltered(candles))
            {
                _tab.SellAtStopCancel();
            }

            // the younger HMA grows more slowly than the older HMA
            if (lastPrice < lastSma && Math.Abs(lastHma - prewHma) < Math.Abs(lastHma2 - prewHma2))
            {
                _tab.BuyAtStopCancel();
            }
            // the younger HMA decreases more slowly than the older HMA
            if (lastPrice > lastSma && Math.Abs(lastHma - prewHma) < Math.Abs(lastHma2 - prewHma2))
            {
                _tab.SellAtStopCancel();
            }

            // the younger HMA grows more slowly than the older HMA
            if (lastPrice < lastSma && Math.Abs(prewHma - prew2Hma) < Math.Abs(prewHma2 - prew2Hma2))
            {
                _tab.BuyAtStopCancel();
            }
            // the younger HMA decreases more slowly than the older HMA
            if (lastPrice > lastSma && Math.Abs(prewHma - prew2Hma) < Math.Abs(prewHma2 - prew2Hma2))
            {
                _tab.SellAtStopCancel();
            }

            // SMA decreases and picks up speed
            if (prewSma > lastSma && Math.Abs(prewSma - lastSma) > Math.Abs(prew2Sma - prewSma))
            {
                _tab.BuyAtStopCancel();
            }
            // SMA is growing and picking up speed
            if (prewSma < lastSma && Math.Abs(lastSma - prewSma) > Math.Abs(prewSma - prew2Sma))
            {
                _tab.SellAtStopCancel();
            }

            // closing below the fast HMA, which 'grows' worse than the slow HMA
            if (lastPrice < lastHma && Math.Abs(lastHma - prewHma) < Math.Abs(lastHma2 - prewHma2))
            {
                _tab.BuyAtStopCancel();
            }
            // closing above the fast HMA, which 'grows' worse than the slow HMA
            if (lastPrice > lastHma && Math.Abs(lastHma - prewHma) < Math.Abs(lastHma2 - prewHma2))
            {
                _tab.SellAtStopCancel();
            }

            // Junior HMA is below SMA and slowing down
            if (lastHma < lastSma && Math.Abs(lastHma - prewHma) < Math.Abs(lastHma2 - prewHma2))
            {
                _tab.BuyAtStopCancel();
            }

            // Junior HMA is above SMA and slowing down
            if (lastHma > lastSma && Math.Abs(lastHma - prewHma) < Math.Abs(lastHma2 - prewHma2))
            {
                _tab.SellAtStopCancel();
            }

            //
            if (lastHma < prewHma)
            {
                _tab.BuyAtStopCancel();
            }
            if (lastHma > prewHma)
            {
                _tab.SellAtStopCancel();
            }

            if (lastFHma < lastHma)
            {
                _tab.BuyAtStopCancel();
            }
            if (lastFHma > lastHma)
            {
                _tab.SellAtStopCancel();
            }

            if (lastHma2 < lastSma)
            {
                _tab.BuyAtStopCancel();
            }
            if (lastHma2 > lastSma)
            {
                _tab.SellAtStopCancel();
            }

            if (lastHma2 < prewHma2)
            {
                _tab.BuyAtStopCancel();
            }
            if (lastHma2 > prewHma2)
            {
                _tab.SellAtStopCancel();
            }

            if (lastFHma2 < lastHma2)
            {
                _tab.BuyAtStopCancel();
            }
            if (lastFHma2 > lastHma2)
            {
                _tab.SellAtStopCancel();
            }

            // The junior HMA is lower than the senior HMA and the junior HMA is slower than the SMA
            if (lastHma < lastHma2 && Math.Abs(lastHma - prewHma) < Math.Abs(lastSma - prewSma))
            {
                _tab.BuyAtStopCancel();
            }
            // The junior HMA is higher than the senior HMA and the junior HMA is slower than the SMA
            if (lastHma > lastHma2 && Math.Abs(lastHma - prewHma) < Math.Abs(lastSma - prewSma))
            {
                _tab.SellAtStopCancel();
            }

            // The junior HMA is lower than the senior HMA and the senior HMA is slower than the SMA
            if (lastHma < lastHma2 && Math.Abs(lastHma2 - prewHma2) < Math.Abs(lastSma - prewSma))
            {
                _tab.BuyAtStopCancel();
            }
            // The junior HMA is higher than the senior HMA and the senior HMA is slower than the SMA
            if (lastHma > lastHma2 && Math.Abs(lastHma2 - prewHma2) < Math.Abs(lastSma - prewSma))
            {
                _tab.SellAtStopCancel();
            }

            // The junior HMA is lower than the senior HMA and the junior HMA is slower than the senior HMA
            if (lastHma < lastHma2 && Math.Abs(lastHma - prewHma) < Math.Abs(lastHma2 - prewHma2))
            {
                _tab.BuyAtStopCancel();
            }
            // The younger HMA is higher than the older HMA and the younger HMA is slower than the older HMA
            if (lastHma > lastHma2 && Math.Abs(lastHma - prewHma) < Math.Abs(lastHma2 - prewHma2))
            {
                _tab.SellAtStopCancel();
            }
        }
        else
        {//exit logic

            // если включен режим трейлинг стопа, то обращаемся к методу SetTrailingStop и передаем в него цену закрытия последней свечи
            //-----------------------------------------
            if (TrailingStopIsOn.ValueBool)
            {
                _trailingStop.SetTrailingStop(candles[candles.Count - 1].Close);
                return;
            }
            //--------------------------------------

            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i].State == PositionStateType.ClosingFail)
                {
                    _tab.CloseAtMarket(positions[i], positions[i].OpenVolume);
                    continue;
                }
                if (positions[i].State != PositionStateType.Open)
                {
                    continue;
                }

                decimal stop_level = 0;
                               
                if (positions[i].Direction == Side.Buy)
                {// logic to close long position

                    stop_level = lastHma < lastHma2 ? lastHma - lastAtr * _multiplerAtr.ValueDecimal : lastHma2 > lastSma ? lastHma2 - lastAtr * _multiplerAtr.ValueDecimal : lastSma;
                    _slippage = Slippage.ValueDecimal * stop_level / 100;
                    _tab.CloseAtTrailingStop(positions[i], stop_level, stop_level - _slippage);
                }
                else if (positions[i].Direction == Side.Sell)
                {//logic to close short position

                    stop_level = lastHma > lastHma2 ? lastHma + lastAtr * _multiplerAtr.ValueDecimal : lastHma2 < lastSma ? lastHma2 + lastAtr * _multiplerAtr.ValueDecimal : lastSma;
                    _slippage = Slippage.ValueDecimal * stop_level / 100;
                    _tab.CloseAtTrailingStop(positions[i], stop_level, stop_level + _slippage);
                    }
                                
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

    private bool BuySignalIsFiltered(List<Candle> candles)
    {
        decimal lastPrice = candles[candles.Count - 1].Close;
        decimal lastSma = _smaFilter.DataSeries[0].Last;
        // filter for buy
        if (Regime.ValueString == "Off" ||
            Regime.ValueString == "OnlyShort" ||
            Regime.ValueString == "OnlyClosePosition")
        {
            return true;
            //if the robot's operating mode does not correspond to the direction of the position
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

        return false;
    }

    private bool SellSignalIsFiltered(List<Candle> candles)
    {
        decimal lastPrice = candles[candles.Count - 1].Close;
        decimal lastSma = _smaFilter.DataSeries[0].Last;
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

        return false;
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
}

