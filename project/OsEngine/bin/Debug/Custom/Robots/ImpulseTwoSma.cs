using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.Classes;
using System;
using System.Collections.Generic;
using System.Drawing;

[Bot("ImpulseTwoSma")]
class ImpulseTwoSma : BotPanel
{
    BotTabSimple _tab;

    StrategyParameterString Regime;
    public StrategyParameterDecimal VolumeOnPosition;
    public StrategyParameterString VolumeRegime;

    private StrategyParameterTimeOfDay TimeStart;
    private StrategyParameterTimeOfDay TimeEnd;

    public Aindicator _sma1;
    public StrategyParameterInt _periodSma1;

    public Aindicator _sma2;
    public StrategyParameterInt _periodSma2;

    public StrategyParameterInt LookBack;

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

    public ImpulseTwoSma(string name, StartProgram startProgram) : base(name, startProgram)
    {
        TabCreate(BotTabType.Simple);
        _tab = TabsSimple[0];

        Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
        VolumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency", "% of the total portfolio" }, "Base");
        VolumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");

        TimeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
        TimeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

        LookBack = CreateParameter("Candles Look Back", 4, 1, 10, 1, "Robot parameters");

        _periodSma1 = CreateParameter("fast SMA period", 250, 50, 500, 50, "Robot parameters");
        _periodSma2 = CreateParameter("slow SMA2 period", 1000, 500, 1500, 100, "Robot parameters");

        SmaLengthFilter = CreateParameter("Sma Length", 100, 10, 500, 1, "Filters");

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

        _sma1 = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma", canDelete: false);
        _sma1 = (Aindicator)_tab.CreateCandleIndicator(_sma1, nameArea: "Prime");
        _sma1.ParametersDigit[0].Value = _periodSma1.ValueInt;
        _sma1.DataSeries[0].Color = Color.Red;
        _sma1.Save();

        _sma2 = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma2", canDelete: false);
        _sma2 = (Aindicator)_tab.CreateCandleIndicator(_sma2, nameArea: "Prime");
        _sma2.ParametersDigit[0].Value = _periodSma2.ValueInt;
        _sma2.DataSeries[0].Color = Color.Green;
        _sma2.Save();

        StopOrActivateIndicators();
        _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
        ParametrsChangeByUser += LRegBot_ParametrsChangeByUser;
        LRegBot_ParametrsChangeByUser();

        _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
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

    private void LRegBot_ParametrsChangeByUser()
    {
        StopOrActivateIndicators();

        if (_sma1.ParametersDigit[0].Value != _periodSma1.ValueInt)
        {
            _sma1.ParametersDigit[0].Value = _periodSma1.ValueInt;
            _sma1.Reload();
            _sma1.Save();
        }

        if (_sma2.ParametersDigit[0].Value != _periodSma2.ValueInt)
        {
            _sma2.ParametersDigit[0].Value = _periodSma2.ValueInt;
            _sma2.Reload();
            _sma2.Save();
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

    public override string GetNameStrategyType()
    {
        return "ImpulseTwoSma";
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

        if (SmaLengthFilter.ValueInt >= candles.Count)
        {
            return;
        }

        if (LookBack.ValueInt + 2 > candles.Count)
        {
            return;
        }

        if (candles.Count < _periodSma1.ValueInt || candles.Count < _periodSma2.ValueInt) { return; }

        decimal lastMa2 = _sma2.DataSeries[0].Last;
        decimal prewMa2 = _sma2.DataSeries[0].Values[candles.Count - 2];

        bool sigUp = false;
        bool sigDown = false;
        bool sigUpClose = false;
        bool sigDownClose = false;

        // signal long
        for (int i = candles.Count - 1; i > candles.Count - 1 - LookBack.ValueInt; i--)
        {
            if (_sma1.DataSeries[0].Values[i] < _sma2.DataSeries[0].Values[i])
            {
                sigUp = false;
                sigDownClose = false;
                break;
            }

            sigUp = true;
            sigDownClose = true;
        }

        if (sigUp == true && _sma1.DataSeries[0].Values[candles.Count - LookBack.ValueInt - 2] > _sma2.DataSeries[0].Values[candles.Count - LookBack.ValueInt - 2])
        { // repeat signal
            sigUp = false;
        }

        if (lastMa2 < prewMa2) { sigUp = false; }

        // signal short
        for (int i = candles.Count - 1; i > candles.Count - 1 - LookBack.ValueInt; i--)
        {
            if (_sma1.DataSeries[0].Values[i] > _sma2.DataSeries[0].Values[i])
            {
                sigDown = false;
                sigUpClose = false;
                break;
            }
            sigDown = true;
            sigUpClose = true;
        }

        if (sigDown == true && _sma1.DataSeries[0].Values[candles.Count - LookBack.ValueInt - 2] < _sma2.DataSeries[0].Values[candles.Count - LookBack.ValueInt - 2])
        { // repeat signal
            sigDown = false;
        }

        if (lastMa2 > prewMa2)
        {
            sigDown = false;
        }

        List<Position> positions = _tab.PositionsOpenAll;

        if (positions.Count == 0)
        {
            // enter logic
            if (!BuySignalIsFiltered(candles) && sigUp)
            {
                _tab.BuyAtMarket(GetVolume());               
            }

            if (!SellSignalIsFiltered(candles) && sigDown)
            {
                _tab.SellAtMarket(GetVolume());               
            }
        }
        else
        {
            //exit logic

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

                if (positions[i].State != PositionStateType.Open) { continue; }

                // logic to reverse long position
                if (positions[i].Direction == Side.Buy && (sigDown || sigUpClose))
                {
                    _tab.CloseAtMarket(positions[i], positions[i].OpenVolume);

                    if (!SellSignalIsFiltered(candles))
                    { _tab.SellAtMarket(GetVolume()); }
                    continue;
                }

                // logic to reverse short position
                if (positions[i].Direction == Side.Sell && (sigUp || sigDownClose))
                {
                    _tab.CloseAtMarket(positions[i], positions[i].OpenVolume);

                    if (!BuySignalIsFiltered(candles))
                    { _tab.BuyAtMarket(GetVolume()); }
                    continue;
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

