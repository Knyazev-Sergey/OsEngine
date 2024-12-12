using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System.Collections.Generic;
using System;

[Bot("BreakZZChannelReverse")]
public class BreakZZChannelReverse : BotPanel
{
    private BotTabSimple _tab;
    private StrategyParameterString Regime;
    private StrategyParameterBool ReverseLogic;
    private StrategyParameterDecimal VolumeOnPosition;
    private StrategyParameterString VolumeRegime;
    private StrategyParameterDecimal Slippage;

    private StrategyParameterTimeOfDay TimeStart;
    private StrategyParameterTimeOfDay TimeEnd;

    private Aindicator _smaFilter;
    private StrategyParameterInt SmaLengthFilter;
    private StrategyParameterBool SmaPositionFilterIsOn;
    private StrategyParameterBool SmaSlopeFilterIsOn;

    private Aindicator _zz;
    private StrategyParameterInt _lengthZZ;

    public BreakZZChannelReverse(string name, StartProgram startProgram) : base(name, startProgram)
    {
        TabCreate(BotTabType.Simple);
        _tab = TabsSimple[0];

        Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
        ReverseLogic = CreateParameter("Reverse logic", true, "Base");
        VolumeRegime = CreateParameter("Volume type", "Number of contracts", new[] { "Number of contracts", "Contract currency" }, "Base");
        VolumeOnPosition = CreateParameter("Volume", 10, 1.0m, 50, 4, "Base");
        Slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");

        TimeStart = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
        TimeEnd = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

        _lengthZZ = CreateParameter("Length ZZ", 50, 50, 200, 20, "Robot parameters");

        SmaLengthFilter = CreateParameter("Sma Length", 100, 10, 500, 1, "Filters");

        SmaPositionFilterIsOn = CreateParameter("Is SMA Filter On", true, "Filters");
        SmaSlopeFilterIsOn = CreateParameter("Is Sma Slope Filter On", false, "Filters");

        _smaFilter = IndicatorsFactory.CreateIndicatorByName(nameClass: "Sma", name: name + "Sma_Filter", canDelete: false);
        _smaFilter = (Aindicator)_tab.CreateCandleIndicator(_smaFilter, nameArea: "Prime");
        _smaFilter.DataSeries[0].Color = System.Drawing.Color.Azure;
        _smaFilter.ParametersDigit[0].Value = SmaLengthFilter.ValueInt;
        _smaFilter.Save();

        _zz = IndicatorsFactory.CreateIndicatorByName(nameClass: "ZigZagChannel_indicator", name: name + "ZigZagChannel", canDelete: false);
        _zz = (Aindicator)_tab.CreateCandleIndicator(_zz, nameArea: "Prime");
        _zz.ParametersDigit[0].Value = _lengthZZ.ValueInt;
        _zz.Save();
               
        StopOrActivateIndicators();
        ParametrsChangeByUser += LRegBot_ParametrsChangeByUser;
        _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
        LRegBot_ParametrsChangeByUser();
    }

    private void LRegBot_ParametrsChangeByUser()
    {
        StopOrActivateIndicators();

        if (_zz.ParametersDigit[0].Value != _lengthZZ.ValueInt)
        {
            _zz.ParametersDigit[0].Value = _lengthZZ.ValueInt;
            _zz.Reload();
            _zz.Save();
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
        return "BreakZZChannelReverse";
    }

    public override void ShowIndividualSettingsDialog()
    {

    }

    // Logic
    private void _tab_CandleFinishedEvent(List<Candle> candles)
    {
        if (Regime.ValueString == "Off")
        {
            return;
        }

        if (TimeStart.Value > _tab.TimeServerCurrent ||
            TimeEnd.Value < _tab.TimeServerCurrent)
        {
            CancelStopsAndProfits();
            return;
        }

        if (_tab.CandlesAll == null)
        {
            return;
        }
        if (_lengthZZ.ValueInt >= candles.Count)
        {
            return;
        }

        if (SmaLengthFilter.ValueInt >= candles.Count)
        {
            return;
        }

        List<Position> positions = _tab.PositionsOpenAll;

        decimal bb_up = _zz.DataSeries[4].Last;
        decimal bb_down = _zz.DataSeries[5].Last;

        decimal lastMaFilter = _smaFilter.DataSeries[0].Last;

        if (bb_down <= 0) return;
        if (bb_up <= 0) return;
                
        decimal _slippage = 0;

        _tab.BuyAtStopCancel();
        _tab.SellAtStopCancel();

        if (positions.Count == 0)
        {// enter logic

            if (bb_up <= bb_down)
            {
                return;
            }

            if (!BuySignalIsFiltered(candles))
            {                
                if (ReverseLogic.ValueBool)
                {
                    if (lastMaFilter < bb_down)
                    {
                        return;
                    }

                    _slippage = Slippage.ValueDecimal * bb_down / 100;
                    _tab.BuyAtStop(GetVolume(), bb_down + _slippage, bb_down, StopActivateType.LowerOrEqual, 1);
                }
                else
                {
                    if (lastMaFilter > bb_up)
                    {
                        return;
                    }

                    _slippage = Slippage.ValueDecimal * bb_up / 100;
                    _tab.BuyAtStop(GetVolume(), bb_up + _slippage, bb_up, StopActivateType.HigherOrEqual, 1);
                }                
            }

            if (!SellSignalIsFiltered(candles))
            {
                if (ReverseLogic.ValueBool)
                {
                    if (lastMaFilter > bb_up)
                    {
                        return;
                    }

                    _slippage = Slippage.ValueDecimal * bb_up / 100;
                    _tab.SellAtStop(GetVolume(), bb_up - _slippage, bb_up, StopActivateType.HigherOrEqual, 1);
                }
                else
                {
                    if (lastMaFilter < bb_down)
                    {
                        return;
                    }

                    _slippage = Slippage.ValueDecimal * bb_down / 100;
                    _tab.SellAtStop(GetVolume(), bb_down - _slippage, bb_down, StopActivateType.LowerOrEqual, 1);
                }                
            }
        }
        else
        {//exit logic
            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i].State != PositionStateType.Open)
                {
                    continue;
                }
                
                if (positions[i].Direction == Side.Buy)
                {// logic to close long position
   
                    if (ReverseLogic.ValueBool)
                    {
                        _slippage = Slippage.ValueDecimal * bb_up / 100;
                        _tab.CloseAtProfit(positions[i], bb_up, bb_up - _slippage);
                    }
                    else
                    {
                        _slippage = Slippage.ValueDecimal * bb_down / 100;
                        _tab.CloseAtStop(positions[i], bb_down, bb_down - _slippage);
                    }
                }
                else if (positions[i].Direction == Side.Sell)
                {//logic to close short position

                    if (ReverseLogic.ValueBool)
                    {
                        _slippage = Slippage.ValueDecimal * bb_down / 100;
                        _tab.CloseAtProfit(positions[i], bb_down, bb_down + _slippage);
                    }
                    else
                    {
                        _slippage = Slippage.ValueDecimal * bb_up / 100;
                        _tab.CloseAtStop(positions[i], bb_up, bb_up + _slippage);
                    }
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
            if (lastSma > lastPrice)
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
        
        // If the robot is running in the tester
        if (StartProgram == StartProgram.IsTester)
        {
            volume = Math.Round(volume, 6);
        }
        else
        {
            volume = Math.Round(volume, _tab.Security.DecimalsVolume);
        }

        return volume;
    }
}


