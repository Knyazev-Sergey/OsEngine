/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.Market.Servers.Tester;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace OsEngine.Robots
{
    [Bot("FuturesKeltner")]
    public class FuturesKeltner : BotPanel
    {
        private BotTabSimple _tab;

        private decimal _volumeTrade;

        private DateTime _dateTimeClosePosition;

        // Basic settings
        private StrategyParameterString _regime;

        // GetVolume settings
        private StrategyParameterString _volumeType;
        private StrategyParameterDecimal _volume;
        private StrategyParameterString _tradeAssetInPortfolio;

        // Indicator settings
        private StrategyParameterInt _keltnerAtrLength;
        private StrategyParameterInt _keltnerEmaLength;
        private StrategyParameterDecimal _keltnerDeviation;

        private StrategyParameterString _dateClosePosition;

        private StrategyParameterCheckBox _checkBoxOuterStops;

        private Aindicator _keltner;
               
        // Trade periods
        private NonTradePeriods _tradePeriodsSettings;
        private StrategyParameterButton _tradePeriodsShowDialogButton;
        private void _tradePeriodsShowDialogButton_UserClickOnButtonEvent()
        {
            _tradePeriodsSettings.ShowDialog();
        }

        public FuturesKeltner(string name, StartProgram startProgram) : base(name, startProgram)
        {
            // non trade periods
            _tradePeriodsSettings = new NonTradePeriods(name);

            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod1Start = new TimeOfDay() { Hour = 0, Minute = 0 };
            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod1End = new TimeOfDay() { Hour = 10, Minute = 05 };
            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod1OnOff = false;

            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod2Start = new TimeOfDay() { Hour = 13, Minute = 54 };
            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod2End = new TimeOfDay() { Hour = 14, Minute = 6 };
            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod2OnOff = false;

            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod3Start = new TimeOfDay() { Hour = 18, Minute = 30 };
            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod3End = new TimeOfDay() { Hour = 24, Minute = 00 };
            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod3OnOff = false;

            _tradePeriodsSettings.TradeInSunday = false;
            _tradePeriodsSettings.TradeInSaturday = false;

            _tradePeriodsSettings.Load();

            // Basic settings

            _regime = CreateParameter("Regime base", "Off", new[] { "Off", "On", "Close Positions"}, "Base");

            _tradePeriodsShowDialogButton = CreateParameterButton("Non trade periods", "Base");
            _tradePeriodsShowDialogButton.UserClickOnButtonEvent += _tradePeriodsShowDialogButton_UserClickOnButtonEvent;

            _keltnerEmaLength = CreateParameter("Keltner ema Length", 150, 20, 300, 10, "Base");
            _keltnerAtrLength = CreateParameter("Keltner atr Length", 24, 20, 300, 10, "Base");
            _keltnerDeviation = CreateParameter("Keltner deviation", 3.9m, 1, 4, 0.1m, "Base");
                
             // GetVolume settings
            _volumeType = CreateParameter("Volume type", "Deposit percent", new[] { "Contracts", "Contract currency", "Deposit percent" }, "Base");
            _volume = CreateParameter("Volume", 100, 1.0m, 50, 4, "Base");
            _tradeAssetInPortfolio = CreateParameter("Asset in portfolio", "Prime", "Base");

            _dateClosePosition = CreateParameter("Time to close positions", "01.01.2027 12:00", "Base");

            _checkBoxOuterStops = CreateParameterCheckBox("Use outer stop orders", true, "Base");

            // Tab creation

            _tab = TabCreate<BotTabSimple>();
            _tab.ManualPositionSupport.DisableManualSupport();
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tab.DeleteBotEvent += Tab_DeleteBotEvent;
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
            

            // Keltner creation

            _keltner = IndicatorsFactory.CreateIndicatorByName("KeltnerChannel", NameStrategyUniq + "KeltnerChannel", false);
            _keltner = (Aindicator)_tab.CreateCandleIndicator(_keltner, "Prime");
            ((IndicatorParameterInt)_keltner.Parameters[0]).ValueInt = _keltnerEmaLength.ValueInt;
            ((IndicatorParameterInt)_keltner.Parameters[1]).ValueInt = _keltnerAtrLength.ValueInt;
            ((IndicatorParameterInt)_keltner.Parameters[2]).ValueInt = _keltnerAtrLength.ValueInt;
            ((IndicatorParameterDecimal)_keltner.Parameters[3]).ValueDecimal = _keltnerDeviation.ValueDecimal;
            _keltner.Save();

            ParametrsChangeByUser += FuturesKeltner_ParametrsChangeByUser;

            if (StartProgram == StartProgram.IsOsTrader)
            {
                Thread worker = new Thread(ThreadTradeLogic) { IsBackground = true };
                worker.Start();

                _tab.PositionClosingSuccesEvent += _tab_PositionClosingSuccesEvent;
            }

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
            _positionIsOpen = false;
            _upLine = 0;
            _downLine = 0;
            _middleLine = 0;

            CancelAllOrders();
        }

        private void _tab_PositionClosingSuccesEvent(Position obj)
        {
            if (_tab.PositionsOpenAll.Count == 0)
            {
                GetKelterValues();
            }
        }

        private bool _positionIsOpen = false;

        private void _tab_PositionOpeningSuccesEvent(Position obj)
        {
            _positionIsOpen = true;
        }

        private bool _deleteBot = false;

        private void Tab_DeleteBotEvent(int obj)
        {
            _tradePeriodsShowDialogButton.UserClickOnButtonEvent -= _tradePeriodsShowDialogButton_UserClickOnButtonEvent;
            _deleteBot = true;
        }

        private void FuturesKeltner_ParametrsChangeByUser()
        {
            _keltner.ParametersDigit[0].Value = _keltnerEmaLength.ValueInt;
            _keltner.ParametersDigit[1].Value = _keltnerAtrLength.ValueInt;
            _keltner.ParametersDigit[2].Value = _keltnerAtrLength.ValueInt;
            _keltner.ParametersDigit[3].Value = _keltnerDeviation.ValueDecimal;
            _keltner.Reload();
            _keltner.Save();            
        }

        #region Logic

        private decimal _upLine;
        private decimal _downLine;
        private decimal _middleLine;
        private bool _changeKeltnerValues = false;

        private void _tab_CandleFinishedEvent(List<Candle> obj)
        {
            try
            {
                if (CheckOff()) return;
                _isOff = false;

                GetKelterValues(); 
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void GetKelterValues()
        {
            if (_keltner == null) return;
            if (_keltner.DataSeries.Count == 0) return;

            _upLine = Math.Round(_keltner.DataSeries[1].Last, _tab.Security.Decimals); // upline
            _downLine = Math.Round(_keltner.DataSeries[2].Last, _tab.Security.Decimals); // downline
            _middleLine = Math.Round(_keltner.DataSeries[3].Last, _tab.Security.Decimals); // middleline

            if (_upLine == 0 || _downLine == 0 || _middleLine == 0) return;

            SendNewLogMessage($"UpLine: {_upLine}, MiddleLine: {_middleLine}, DownLine: {_downLine}, LastPrice: {_tab.CandlesAll[^1].Close}", Logging.LogMessageType.User);

            _changeKeltnerValues = true;

            if (StartProgram != StartProgram.IsOsTrader)
            {
                WhenChangeKeltnerValues();
                CheckConditionSecondCandle();
            }            
        }

        private void ThreadTradeLogic()
        {
            while (!_deleteBot)
            {
                try
                {
                    Thread.Sleep(1000);

                    if (_tab == null || _tab.Portfolio == null) continue;
                    if (_tab.Connector?.IsReadyToTrade == false) continue;

                    TradeLogic();                                       
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                }
            }            
        }

        private bool _isClosePositions = false;

        private bool _isOff = true;

        private string _valueTimeToClose = "";

        private bool _isNonTradePeriod = false;

        private void TradeLogic()
        {
            if (CheckClosePositions()) return;
            if (CheckOff()) return;  
            if (CheckNonTradePeriods()) return;         
            if (CheckDateTimeToClosePositions()) return;

            if (_upLine == 0 && _downLine == 0 && _middleLine == 0) // при первоначальном запуске получаем данные по индикатору
            {
                GetKelterValues();
            }

            WhenChangeKeltnerValues();
            CheckConditionSecondCandle();

            if (_positionIsOpen)
            {
                CancelAllOrders();

                if (_checkBoxOuterStops)
                {
                    for (int i = 0; i < _tab.PositionsOpenAll.Count; i++)
                    {
                        if (_tab.PositionsOpenAll[i].OpenOrders[^1].TypeOrder == OrderPriceType.StopMarket)
                        {
                            return;
                        }
                    }
                }

                ReplaceOrders();

                _positionIsOpen = false;
            }
        }

        private bool CheckClosePositions()
        {
            if (_regime == "Close Positions")
            {
                if (!_isClosePositions)
                {
                    CancelAllOrders();

                    _tab.CloseAllAtMarket();

                    _isClosePositions = true;

                    _valueTimeToClose = "";

                    SendNewLogMessage("Позиции закрыты, ордера отменены", Logging.LogMessageType.User);
                }

                return true;
            }

            _isClosePositions = false;

            return false;
        }

        private bool CheckOff()
        {
            if (_regime == "Off")
            {
                if (!_isOff)
                {
                    CancelAllOrders();

                    _isOff = true;

                    _valueTimeToClose = "";

                    _upLine = 0;
                    _downLine = 0;
                    _middleLine = 0;

                    SendNewLogMessage("Робот выключен", Logging.LogMessageType.User);
                }

                return true;
            }
            else if (_regime == "On")
            {
                if (_isOff)
                {
                    SendNewLogMessage("Робот включен", Logging.LogMessageType.User);
                    _isOff = false;
                }

                return false;
            }

            return true;
        }

        private bool CheckNonTradePeriods()
        {
            if (_tradePeriodsSettings.CanTradeThisTime(DateTime.Now) == false)
            {
                if (!_isNonTradePeriod)
                {
                    CancelAllOrders();
                    SendNewLogMessage("Неторговый период включен", Logging.LogMessageType.User);
                    _isNonTradePeriod = true;
                }

                return true;
            }
            else
            {
                if (_isNonTradePeriod)
                {
                    SendNewLogMessage("Неторговый период выключен", Logging.LogMessageType.User);
                    _isNonTradePeriod = false;
                }

                return false;
            }
        }

        private bool CheckDateTimeToClosePositions()
        {
            if (!_valueTimeToClose.Equals(_dateClosePosition))
            {
                if (!DateTime.TryParseExact(_dateClosePosition, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _dateTimeClosePosition))
                {
                    SendNewLogMessage("Не корректно указана дата и время, должно быть в формате dd.MM.yyyy HH:mm", Logging.LogMessageType.Error);
                    _regime.ValueString = "Off";
                    return true;
                }

                if (_dateTimeClosePosition < DateTime.Now)
                {
                    SendNewLogMessage("Робот выключен. Указанная дата меньше текущей даты.\nСкорректируйте дату закрытия позиций и перезапустите робота.", Logging.LogMessageType.Error);
                    _regime.ValueString = "Off";
                    return true;
                }

                _valueTimeToClose = _dateClosePosition;
            }

            if (DateTime.Now >= _dateTimeClosePosition)
            {          
                SendNewLogMessage("Закрываем позиции по времени и останавливаем робота", Logging.LogMessageType.User);
                _regime.ValueString = "Close Positions";
              
                return true;
            }

            return false;
        }

        private void WhenChangeKeltnerValues()
        {
            if (_changeKeltnerValues) // когда изменились данные по индикатору
            {
                CancelAllOrders();

                if (_checkBoxOuterStops)
                {
                    for (int i = 0; i < _tab.PositionsOpenAll.Count; i++)
                    {
                        if (_tab.PositionsOpenAll[i].OpenOrders[^1].TypeOrder == OrderPriceType.StopMarket)
                        {
                            return;
                        }
                    }
                }

                if (_tab.PositionsOpenAll.Count == 0)
                {
                    // если нет позиций, выставляем открывающие ордера
                    PlaceNewOrders();
                }
                else
                {
                    // если есть позиции, выставляем закрывающие ордера
                    ReplaceOrders();
                }

                _changeKeltnerValues = false;
            }
        }

        private void CheckConditionSecondCandle()
        {
            if (_tab.PositionsOpenAll.Count == 1)                 
            {
                if (_tab.PositionsOpenAll[0].OpenOrders[^1].TypeOrder == OrderPriceType.StopMarket) return;

                if (_tab.CandlesFinishedOnly == null || _tab.CandlesFinishedOnly.Count <= 2) return;

                int indexCandle = GetCandelIndex();

                if (_tab.CandlesFinishedOnly.Count - 1 - indexCandle == 1) // ждем когда закроется следующая свеча от открытой первой позиции
                {
                    //SendNewLogMessage($"Индекс свечи открытой позиции: {indexCandle}, индекс последней свечи: {_tab.CandlesFinishedOnly.Count - 1}, цена закрытия свечи: {_tab.CandlesFinishedOnly[^1].Close}", Logging.LogMessageType.User);

                    if (_tab.PositionOpenLong.Count == 1)
                    {
                        //SendNewLogMessage($"Индекс свечи открытой позиции: {indexCandle}, индекс последней свечи: {_tab.CandlesFinishedOnly.Count - 1}, цена закрытия свечи: {_tab.CandlesFinishedOnly[^1].Close}, UpLine: {_upLine}", Logging.LogMessageType.User);

                        if (_tab.CandlesFinishedOnly[indexCandle + 1].Close > _upLine)
                        {
                            _tab.BuyAtMarket(_volumeTrade);
                        }
                    }

                    if (_tab.PositionOpenShort.Count == 1)
                    {
                        //SendNewLogMessage($"Индекс свечи открытой позиции: {indexCandle}, индекс последней свечи: {_tab.CandlesFinishedOnly.Count - 1}, цена закрытия свечи: {_tab.CandlesFinishedOnly[^1].Close}, DownLine: {_downLine}", Logging.LogMessageType.User);
                        
                        if (_tab.CandlesFinishedOnly[indexCandle + 1].Close < _downLine)
                        {
                            _tab.SellAtMarket(_volumeTrade);
                        }
                    }
                }
            }
        }

        private int GetCandelIndex()
        {
            for (int i = _tab.CandlesFinishedOnly.Count - 1; i >= 1; i--)
            {
                if (_tab.CandlesFinishedOnly[i].TimeStart > _tab.PositionsOpenAll[0].TimeOpen &&
                    _tab.CandlesFinishedOnly[i - 1].TimeStart <= _tab.PositionsOpenAll[0].TimeOpen)
                {
                    return i - 1;
                }
            }

            return 0;
        }

        private void ReplaceOrders()
        {
            if (_tab.PositionOpenLong.Count >= 1)
            {
                if (_checkBoxOuterStops)
                {
                    _tab.CloseAtStopMarketOnServer(_tab.PositionOpenLong[0], _middleLine);
                }
                else
                {
                    _tab.CloseAtStopMarket(_tab.PositionOpenLong[0], _middleLine);
                }
            }

            if (_tab.PositionOpenLong.Count == 2)
            {
                if (_checkBoxOuterStops)
                {
                    _tab.CloseAtStopMarketOnServer(_tab.PositionOpenLong[1], _upLine);
                }
                else
                {
                    _tab.CloseAtStopMarket(_tab.PositionOpenLong[1], _upLine);
                }
            }

            if (_tab.PositionOpenShort.Count >= 1)
            {
                if (_checkBoxOuterStops)
                {
                    _tab.CloseAtStopMarketOnServer(_tab.PositionOpenShort[0], _middleLine);
                }
                else
                {
                    _tab.CloseAtStopMarket(_tab.PositionOpenShort[0], _middleLine);
                }
            }

            if (_tab.PositionOpenShort.Count == 2)
            {
                if (_checkBoxOuterStops)
                {
                    _tab.CloseAtStopMarketOnServer(_tab.PositionOpenShort[1], _downLine);
                }
                else
                {
                    _tab.CloseAtStopMarket(_tab.PositionOpenShort[1], _downLine);
                }
            }
        }

        private void CancelAllOrders()
        {           
            _tab.BuyAtStopCancel();
            _tab.SellAtStopCancel();

            if (_tab.PositionOpenLong.Count > 0)
            {
                for (int i = 0; i < _tab.PositionOpenLong.Count; i++)
                {
                    _tab.PositionOpenLong[i].StopOrderIsActive = false;
                    _tab.PositionOpenLong[i].StopOrderPrice = 0;
                    _tab.PositionOpenLong[i].StopOrderRedLine = 0;
                }
            }

            if (_tab.PositionOpenShort.Count > 0)
            {
                for (int i = 0; i < _tab.PositionOpenShort.Count; i++)
                {
                    _tab.PositionOpenShort[i].StopOrderIsActive = false;
                    _tab.PositionOpenShort[i].StopOrderPrice = 0;
                    _tab.PositionOpenShort[i].StopOrderRedLine = 0;
                }
            }

            if (_checkBoxOuterStops)
            {
                for (int i = 0; i < _tab.PositionsOpenAll.Count; i++)
                {
                    _tab.CloseAtStopOnServerCancel(_tab.PositionsOpenAll[i]);
                }
            }
        }

        private void PlaceNewOrders()
        {
            if (!CheckConditionsTrades()) return;

            _volumeTrade = Math.Round(GetVolume(_tab) / 2, _tab.Security.DecimalsVolume, MidpointRounding.ToZero);

            if (_volumeTrade <= 0)
            {
                SendNewLogMessage($"Расчетный объем для торговли меньше либо равен нулю.", Logging.LogMessageType.Error);
                return;
            }

            if (_checkBoxOuterStops)
            {
                _tab.BuyAtStopMarketOnServer(_volumeTrade, _upLine);
                _tab.SellAtStopMarketOnServer(_volumeTrade, _downLine);
            }
            else
            {
                _tab.BuyAtStopMarket(_volumeTrade, _upLine, _upLine, StopActivateType.HigherOrEqual, 0, "Buy", PositionOpenerToStopLifeTimeType.NoLifeTime);
                _tab.SellAtStopMarket(_volumeTrade, _downLine, _downLine, StopActivateType.LowerOrEqual, 0, "Sell", PositionOpenerToStopLifeTimeType.NoLifeTime);
            }

            SendNewLogMessage($"Place orders: Buy - {_upLine}, Sell - {_downLine}", Logging.LogMessageType.User);
        }

        private bool CheckConditionsTrades()
        {
            if (_tab.CandlesAll.Count == 0) return false;
            if (_upLine == 0 && _downLine == 0 && _middleLine == 0) return false;

            decimal lastPrice = _tab.CandlesAll[^1].Close;

            if (lastPrice > _upLine) return false;

            if (lastPrice < _downLine) return false;

            return true;
        }

        #endregion

        #region Helpers

        // Method for calculating the volume of entry into a position
        private decimal GetVolume(BotTabSimple tab)
        {
            decimal volume = 0;

            if (_volumeType.ValueString == "Contracts")
            {
                volume = _volume.ValueDecimal;
            }
            else if (_volumeType.ValueString == "Contract currency")
            {
                decimal contractPrice = tab.PriceBestAsk;
                volume = _volume.ValueDecimal / contractPrice;

                if (StartProgram == StartProgram.IsOsTrader)
                {
                    IServerPermission serverPermission = ServerMaster.GetServerPermission(tab.Connector.ServerType);

                    if (serverPermission != null &&
                        serverPermission.IsUseLotToCalculateProfit &&
                    tab.Security.Lot != 0 &&
                        tab.Security.Lot > 1)
                    {
                        volume = _volume.ValueDecimal / (contractPrice * tab.Security.Lot);
                    }

                    volume = Math.Round(volume, tab.Security.DecimalsVolume);
                }
                else // Tester or Optimizer
                {
                    volume = Math.Round(volume, 6);
                }
            }
            else if (_volumeType.ValueString == "Deposit percent")
            {
                Portfolio myPortfolio = tab.Portfolio;

                if (myPortfolio == null)
                {
                    return 0;
                }

                decimal portfolioPrimeAsset = 0;

                if (_tradeAssetInPortfolio.ValueString == "Prime")
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
                        if (positionOnBoard[i].SecurityNameCode == _tradeAssetInPortfolio.ValueString)
                        {
                            portfolioPrimeAsset = positionOnBoard[i].ValueCurrent;
                            break;
                        }
                    }
                }

                if (portfolioPrimeAsset == 0)
                {
                    SendNewLogMessage("Can`t found portfolio " + _tradeAssetInPortfolio.ValueString, Logging.LogMessageType.Error);
                    return 0;
                }

                decimal moneyOnPosition = portfolioPrimeAsset * (_volume.ValueDecimal / 100);

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

        #endregion     
    }
}
