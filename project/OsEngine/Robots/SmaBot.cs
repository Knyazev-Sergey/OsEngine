using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Market.Servers;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using OsEngine.Logging;

namespace OsEngine.Robots
{
    [Bot("SmaBot")]
    public class SmaBot : BotPanel
    {
        private BotTabSimple _tab;
        private Aindicator _smaFast;
        private string _regime;
        private decimal _step;
        private decimal _slippage;

        // GetVolume settings
        private decimal _volume;
        private string _volumeType;
        private string _tradeAssetInPortfolio;

        private int _periodSma;

        public SmaBot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" });
            _slippage = CreateParameter("Slippage", 0, 0, 20, 1);

            // GetVolume settings
            _volumeType = CreateParameter("Volume type", "Deposit percent", new[] { "Contracts", "Contract currency", "Deposit percent" });
            _volume = CreateParameter("Volume", 20, 1.0m, 50, 4);
            _tradeAssetInPortfolio = CreateParameter("Asset in portfolio", "Prime");
            _step = CreateParameter("Step", 20, 1.0m, 50, 4);

            _periodSma = CreateParameter("Period SMA", 20, 0, 0, 0, "SMA");

            _smaFast = IndicatorsFactory.CreateIndicatorByName("Sma", name + "SmaFast", false);
            _smaFast = (Aindicator)_tab.CreateCandleIndicator(_smaFast, "Prime");
            ((IndicatorParameterInt)_smaFast.Parameters[0]).ValueInt = _periodSma;
            _smaFast.Save();

            _tab.CandleFinishedEvent += Strateg_CandleFinishedEvent;
        }

        private decimal _lastClose;
        private decimal _lastSma;
        private bool _tradeLong = false;
        private bool _tradeShort = false;

        private void Strateg_CandleFinishedEvent(List<Candle> candles)
        {
            if (_regime == "Off")
            {
                return;
            }

            if (_smaFast.DataSeries[0].Values == null)
            {
                return;
            }

            _lastClose = candles[candles.Count - 1].Close;
            _lastSma = _smaFast.DataSeries[0].Last;

            if (candles.Count < _periodSma + 1)
            {
                return;
            }

            List<Position> openPositions = _tab.PositionsOpenAll;

            if (openPositions != null && openPositions.Count != 0)
            {
                for (int i = 0; i < openPositions.Count; i++)
                {
                    LogicClosePosition(candles, openPositions[i], openPositions);
                }
            }

            if (_regime == "OnlyClosePosition")
            {
                return;
            }

            if (openPositions == null
                || openPositions.Count == 0)
            {
                LogicOpenPosition(candles);
            }
        }

        // Logic open position
        private void LogicOpenPosition(List<Candle> candles)
        {
            if (_lastClose > _lastSma && _regime != "OnlyShort") // If the mode is not only short, then we enter long
            {
                _tab.BuyAtLimit(GetVolume(_tab), _lastClose + _slippage * _tab.Security.PriceStep);
            }

            if (_lastClose < _lastSma && _regime != "OnlyLong") // If the mode is not only long, then we enter short
            {
                _tab.SellAtLimit(GetVolume(_tab), _lastClose - _slippage * _tab.Security.PriceStep);
            }
        }

        // Logic close position
        private void LogicClosePosition(List<Candle> candles, Position position, List<Position> positionsAll)
        {
            if (position.State != PositionStateType.Open)
            {
                return;
            }

            if (position.Direction == Side.Buy) // If the direction of the position is long
            {
                if (_lastClose < _lastSma)
                {
                    _tab.CloseAtLimit(position, _lastClose - _slippage * _tab.Security.PriceStep, position.OpenVolume);

                    if (_regime != "OnlyLong"
                        && _regime != "OnlyClosePosition"
                        && positionsAll.Count == 1)
                    {
                        _tab.SellAtLimit(GetVolume(_tab), _lastClose - _slippage * _tab.Security.PriceStep);
                    }
                }
            }

            if (position.Direction == Side.Sell) // If the direction of the position is short
            {
                if (_lastClose > _lastSma)
                {
                    _tab.CloseAtLimit(position, _lastClose + _slippage * _tab.Security.PriceStep, position.OpenVolume);

                    if (_regime != "OnlyShort"
                        && _regime != "OnlyClosePosition"
                        && positionsAll.Count == 1)
                    {
                        _tab.BuyAtLimit(GetVolume(_tab), _lastClose + _slippage * _tab.Security.PriceStep);
                    }
                }
            }
        }

        // Method for calculating the volume of entry into a position
        private decimal GetVolume(BotTabSimple tab)
        {
            decimal volume = 0;

            if (_volumeType == "Contracts")
            {
                volume = _volume;
            }
            else if (_volumeType == "Contract currency")
            {
                decimal contractPrice = tab.PriceBestAsk;
                volume = _volume / contractPrice;

                if (StartProgram == StartProgram.IsOsTrader)
                {
                    IServerPermission serverPermission = ServerMaster.GetServerPermission(tab.Connector.ServerType);

                    if (serverPermission != null &&
                        serverPermission.IsUseLotToCalculateProfit &&
                    tab.Security.Lot != 0 &&
                        tab.Security.Lot > 1)
                    {
                        volume = _volume / (contractPrice * tab.Security.Lot);
                    }

                    volume = Math.Round(volume, tab.Security.DecimalsVolume);
                }
                else // Tester or Optimizer
                {
                    volume = Math.Round(volume, 6);
                }
            }
            else if (_volumeType == "Deposit percent")
            {
                Portfolio myPortfolio = tab.Portfolio;

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
                    SendNewLogMessage("Can`t found portfolio " + _tradeAssetInPortfolio, LogMessageType.Error);
                    return 0;
                }
                decimal moneyOnPosition = portfolioPrimeAsset * (_volume / 100);

                decimal qty = moneyOnPosition / tab.PriceBestAsk / tab.Security.Lot;

                if (tab.StartProgram == StartProgram.IsOsTrader)
                {
                    if (tab.Security.UsePriceStepCostToCalculateVolume == true
                    && tab.Security.PriceStep != tab.Security.PriceStepCost
                    && tab.PriceBestAsk != 0
                    && tab.Security.PriceStep != 0
                    && tab.Security.PriceStepCost != 0)
                    {// расчёт количества контрактов для фьючерсов и опционов на Мосбирже
                        qty = moneyOnPosition / (tab.PriceBestAsk / tab.Security.PriceStep * tab.Security.PriceStepCost);
                    }
                    qty = Math.Round(qty, tab.Security.DecimalsVolume);
                }
                else
                {
                    qty = Math.Round(qty, 7);
                }

                return qty;
            }

            return volume;
        }
    }
}
