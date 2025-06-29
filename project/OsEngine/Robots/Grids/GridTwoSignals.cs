﻿/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Grids;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;

/*Description
Ejection of two grids in one direction
First buy signal: Breakdown of Price-Channel down
Second buy signal: There is the first grid + price returned to the center of the channel.
Output: By the number of closed lines
Output 2: By time in seconds
TrailingUp / Trailing Down. The permutation step is 20 minimum price steps.
*/

namespace OsEngine.Robots.Grids
{
    [Bot("GridTwoSignals")]
    public class GridTwoSignals : BotPanel
    {
        private StrategyParameterString _regime;
        private StrategyParameterTimeOfDay _startTradeTime;
        private StrategyParameterTimeOfDay _endTradeTime;

        private StrategyParameterInt _lifeTimeSeconds;
        private StrategyParameterInt _closePositionsCountToCloseGrid;

        private StrategyParameterString _volumeType;
        private StrategyParameterDecimal _volume;
        private StrategyParameterString _tradeAssetInPortfolio;

        private StrategyParameterInt _priceChannelLength;

        private StrategyParameterInt _linesCount;
        private StrategyParameterDecimal _linesStep;
        private StrategyParameterDecimal _profitValue;

        private Aindicator _priceChannel;

        private BotTabSimple _tab;

        public GridTwoSignals(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tab.Connector.TestStartEvent += Connector_TestStartEvent;

            _regime = CreateParameter("Regime", "Off", new[] { "Off", "On" }, "Base");

            _startTradeTime = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            _endTradeTime = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            _lifeTimeSeconds = CreateParameter("Grid life time seconds", 1200, 10, 300, 10, "Base");
            _closePositionsCountToCloseGrid = CreateParameter("Grid close positions max", 50, 10, 300, 10, "Base");

            _linesCount = CreateParameter("Grid lines count", 10, 10, 300, 10, "Grid");
            _linesStep = CreateParameter("Grid lines step", 0.1m, 10m, 300, 10, "Grid");
            _profitValue = CreateParameter("Profit percent", 0.05m, 1, 5, 0.1m, "Grid");

            _volumeType = CreateParameter("Volume type", "Contracts", new[] { "Contracts", "Contract currency", "Deposit percent" }, "Grid");
            _volume = CreateParameter("Volume on one line", 1, 1.0m, 50, 4, "Grid");
            _tradeAssetInPortfolio = CreateParameter("Asset in portfolio", "Prime", "Grid");

            // Indicator settings
            _priceChannelLength = CreateParameter("Price channel length", 21, 7, 48, 7, "Indicator");

            // Create indicator Bollinger
            _priceChannel = IndicatorsFactory.CreateIndicatorByName("PriceChannel", name + "PriceChannel", false);
            _priceChannel = (Aindicator)_tab.CreateCandleIndicator(_priceChannel, "Prime");
            ((IndicatorParameterInt)_priceChannel.Parameters[0]).ValueInt = _priceChannelLength.ValueInt;
            ((IndicatorParameterInt)_priceChannel.Parameters[1]).ValueInt = _priceChannelLength.ValueInt;

            _priceChannel.Save();

            ParametrsChangeByUser += ParametersChangeByUser;

            Description =
                "Ejection of two grids in one direction " +
                "First buy signal: Breakdown of Price-Channel down " +
                "Second buy signal: There is the first grid + price returned to the center of the channel. " +
                "Output: By the number of closed lines. " +
                "Output 2: By time in seconds. " +
                "TrailingUp / Trailing Down. The permutation step is 20 minimum price steps.";
        }

        private void ParametersChangeByUser()
        {
            ((IndicatorParameterInt)_priceChannel.Parameters[0]).ValueInt = _priceChannelLength.ValueInt;
            ((IndicatorParameterInt)_priceChannel.Parameters[1]).ValueInt = _priceChannelLength.ValueInt;
            _priceChannel.Save();
            _priceChannel.Reload();
        }

        public override string GetNameStrategyType()
        {
            return "GridTwoSignals";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        private void Connector_TestStartEvent()
        {
            if(_tab.GridsMaster == null)
            {
                return;
            }
            for (int i = 0; i < _tab.GridsMaster.TradeGrids.Count; i++)
            {
                TradeGrid grid = _tab.GridsMaster.TradeGrids[i];
                _tab.GridsMaster.DeleteAtNum(grid.Number);
                i--;
            }
        }

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (_regime.ValueString == "Off")
            {
                return;
            }

            if (candles.Count < _priceChannelLength.ValueInt)
            {
                return;
            }

            if (_startTradeTime.Value > _tab.TimeServerCurrent ||
                _endTradeTime.Value < _tab.TimeServerCurrent)
            {
                return;
            }

            if (_tab.GridsMaster.TradeGrids.Count == 0
                || _tab.GridsMaster.TradeGrids.Count == 1)
            {
                LogicCreateGrid(candles);
            }

            LogicDeleteGrid(candles);

        }

        private void LogicCreateGrid(List<Candle> candles)
        {
            decimal lastUpLine = _priceChannel.DataSeries[0].Values[^2];
            decimal lastDownLine = _priceChannel.DataSeries[1].Values[^2];

            if (lastUpLine == 0
                || lastDownLine == 0)
            {
                return;
            }

            decimal lastPrice = candles[^1].Close;

            if (_tab.GridsMaster.TradeGrids.Count == 0
                && lastPrice < lastDownLine)
            {
                ThrowGrid(lastPrice);
            }
            else if(_tab.GridsMaster.TradeGrids.Count == 1
                && _tab.GridsMaster.TradeGrids[0].Number == 1
                && lastPrice > (lastDownLine + lastUpLine) / 2)
            {
                ThrowGrid(lastPrice);
            }
        }

        private void ThrowGrid(decimal lastPrice)
        {
            // 1 создаём сетку
            TradeGrid grid = _tab.GridsMaster.CreateNewTradeGrid();

            // 2 устанавливаем её тип
            grid.GridType = TradeGridPrimeType.MarketMaking;

            // 3 устанавливаем объёмы
            grid.GridCreator.StartVolume = _volume.ValueDecimal;
            grid.GridCreator.TradeAssetInPortfolio = _tradeAssetInPortfolio.ValueString;
            if (_volumeType.ValueString == "Contracts")
            {
                grid.GridCreator.TypeVolume = TradeGridVolumeType.Contracts;
            }
            else if (_volumeType.ValueString == "Contract currency")
            {
                grid.GridCreator.TypeVolume = TradeGridVolumeType.ContractCurrency;
            }
            else if (_volumeType.ValueString == "Deposit percent")
            {
                grid.GridCreator.TypeVolume = TradeGridVolumeType.DepositPercent;
            }

            // 4 генерируем линии

            grid.GridCreator.FirstPrice = lastPrice;
            grid.GridCreator.LineCountStart = _linesCount.ValueInt;
            grid.GridCreator.LineStep = _linesStep.ValueDecimal;
            grid.GridCreator.TypeStep = TradeGridValueType.Percent;
            grid.GridCreator.TypeProfit = TradeGridValueType.Percent;
            grid.GridCreator.ProfitStep = _profitValue.ValueDecimal;
            grid.GridCreator.GridSide = Side.Buy;
            grid.GridCreator.CreateNewGrid(_tab, TradeGridPrimeType.MarketMaking);

            // 5 устанавливаем Trailing Up

            grid.TrailingUp.TrailingUpStep = _tab.Security.PriceStep * 20;
            grid.TrailingUp.TrailingUpLimit = lastPrice + lastPrice * 0.1m;
            grid.TrailingUp.TrailingUpIsOn = true;

            // 6 устанавливаем Trailing Down

            grid.TrailingUp.TrailingDownStep = _tab.Security.PriceStep * 20;
            grid.TrailingUp.TrailingDownLimit = lastPrice - lastPrice * 0.1m;
            grid.TrailingUp.TrailingDownIsOn = true;

            // 7 устанавливаем закрытие сетки по времени
 
            grid.StopBy.StopGridByLifeTimeReaction = TradeGridRegime.CloseForced;
            grid.StopBy.StopGridByLifeTimeSecondsToLife = _lifeTimeSeconds.ValueInt;
            grid.StopBy.StopGridByLifeTimeIsOn = true;

            // 8 устанавливаем закрытие сетки по количеству сделок

            grid.StopBy.StopGridByPositionsCountReaction = TradeGridRegime.CloseForced;
            grid.StopBy.StopGridByPositionsCountValue = _closePositionsCountToCloseGrid.ValueInt;
            grid.StopBy.StopGridByPositionsCountIsOn = true;

            // сохраняем
            grid.Save();

            // включаем
            grid.Regime = TradeGridRegime.On;
        }

        private void LogicDeleteGrid(List<Candle> candles)
        {
            for(int i = 0;i < _tab.GridsMaster.TradeGrids.Count;i++)
            {
                TradeGrid grid = _tab.GridsMaster.TradeGrids[i];

                // проверяем сетку на то что она уже прекратила работать и её надо удалить

                if (grid.HaveOpenPositionsByGrid == false
                    && grid.Regime == TradeGridRegime.Off)
                { // Grid is stop work
                    _tab.GridsMaster.DeleteAtNum(grid.Number);
                    i--;
                }
            }
        }
    }
}
