using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace OsEngine.Robots
{
    [Bot("OptionsDH")]
    public class OptionsDH : BotPanel
    {
        #region Constructor

        private BotTabSimple _tabOption;
        private BotTabSimple _tabFutures;
        private StrategyParameterString _regime;
        private StrategyParameterDecimal _deviationDelta;
        private StrategyParameterDecimal _priceActivation;
        private StrategyParameterString _directionPriceActivation;
        private StrategyParameterDecimal _priceDeactivationHysteresis;

        private Logging.LogMessageType _logType = Logging.LogMessageType.User;

        public OptionsDH(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _tabOption = (BotTabSimple)TabCreate(BotTabType.Simple);
            _tabFutures = (BotTabSimple)TabCreate(BotTabType.Simple);

            this.ParamGuiSettings.Title = "Options Delta Hedge";
            this.ParamGuiSettings.Height = 400;
            this.ParamGuiSettings.Width = 400;

            string tabName = " Parameters ";

            _regime = CreateParameter("Regime", "Off", new string[] { "Off", "On" }, tabName);
            _deviationDelta = CreateParameter("Deviation Delta", 0.01m, 0m, 0m, 0m, tabName);
            _priceActivation = CreateParameter("Price Activation", 0m, 0m, 0m, 0m, tabName);
            _directionPriceActivation = CreateParameter("Direction price activation", DirectionPriceActivation.Higher.ToString(), 
                new string[] { DirectionPriceActivation.Higher.ToString(), DirectionPriceActivation.Lower.ToString() }, tabName);
            _priceDeactivationHysteresis = CreateParameter("Price deactivation hysteresis", 0m, 0m, 0m, 0m, tabName);
                       
            CustomTabToParametersUi customTabMonitoring = ParamGuiSettings.CreateCustomTab(" Мониторинг ");

            CreateTableTable();
            customTabMonitoring.AddChildren(_hostMonitoring);

            Thread threadMonitoring = new Thread(ThreadRefreshTable) { IsBackground = true };
            threadMonitoring.Start();

            Thread threadTradeLogic = new Thread(ThreadTradeLogic) { IsBackground = true };
            threadTradeLogic.Start();
        }

        #endregion

        #region Parameters

        private WindowsFormsHost _hostMonitoring;
        private DataGridView _dgvMonitoring;

        private void CreateTableTable()
        {
            _hostMonitoring = new WindowsFormsHost();

            DataGridView dgv =
                DataGridFactory.GetDataGridView(DataGridViewSelectionMode.CellSelect,
                DataGridViewAutoSizeRowsMode.AllCells);

            dgv.Dock = DockStyle.Fill;
            dgv.ScrollBars = ScrollBars.Both;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.GridColor = Color.Gray;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Bold | FontStyle.Italic);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgv.ColumnCount = 2;
            dgv.RowCount = 5;

            foreach (DataGridViewColumn column in dgv.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                column.ReadOnly = false;
            }

            dgv.Columns[0].HeaderText = "Показатель";
            dgv.Columns[1].HeaderText = "Значение";

            dgv[0, 0].Value = "Цена фьючерса";
            dgv[0, 1].Value = "Дельта Опциона в позиции";
            dgv[0, 2].Value = "Дельта фьючерса в позиции";
            dgv[0, 3].Value = "Общая дельта";
            //dgv[0, 4].Value = "5";

            _hostMonitoring.Child = dgv;
            _dgvMonitoring = dgv;
        }

        private void ThreadRefreshTable()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(1000);
                    RefreshTable();
                }
                catch (Exception error)
                {
                    SendNewLogMessage(error.ToString(), Logging.LogMessageType.Error);
                }
            }
        }

        private void RefreshTable()
        {
            if (_tabFutures == null) return;
            if (_tabFutures.Security == null) return;
            if (_tabFutures.Security.Name == null) return;
            if (_tabFutures.CandlesAll == null) return;
            if (_tabFutures.CandlesAll.Count == 0) return;

            _dgvMonitoring[1, 0].Value = _tabFutures.CandlesAll[^1].Close;
            _dgvMonitoring[1, 1].Value = _deltaOption;
            _dgvMonitoring[1, 2].Value = _deltaFutures;
            _dgvMonitoring[1, 3].Value = _deltaOption + _deltaFutures;

            _dgvMonitoring[0, 4].Value = "_priceActivationState";
            _dgvMonitoring[1, 4].Value = _priceActivationState;
        }

        #endregion

        #region Trade Logic

        private decimal _deltaOption;
        private decimal _deltaFutures;

        private void ThreadTradeLogic()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(100);

                    if (_tabOption == null || _tabFutures == null) continue;
                    if (_tabOption.Security == null || _tabFutures.Security == null) continue;
                    if (_tabOption.Security.Name == null || _tabFutures.Security.Name == null) continue;
                    if (_tabFutures.CandlesAll == null) continue;
                    if (_tabFutures.CandlesAll.Count == 0) continue;

                    TradeLogic();
                }
                catch (Exception error)
                {
                    SendNewLogMessage(error.ToString(), Logging.LogMessageType.Error);
                }
            }
        }

        private bool _checkOpenPosition = false;

        private bool _checkClosePosition = false;

        private void TradeLogic()
        {
            _deltaOption = GetDeltaOption();
            _deltaFutures = GetDeltaFutures();

            if (_regime.ValueString == "Off")
            {
                _priceActivationState = false;
                return;
            }

            if (!CheckPriceActivation())
            {                
                return;
            }

            if (_checkOpenPosition)
            {
                CheckOpenPositionFutures();
                return;
            }

            if (_checkClosePosition)
            {
                CheckClosePositionFutures();
                return;
            }

            if (Math.Abs(_deltaOption + _deltaFutures) >= _deviationDelta)
            {
                decimal volume = Math.Round(Math.Abs(_deltaOption + _deltaFutures), _tabFutures.Security.DecimalsVolume, MidpointRounding.ToNegativeInfinity);

                if (_deltaOption + _deltaFutures > 0)
                {
                    SellFutures(volume);                    
                }
                else if (_deltaOption + _deltaFutures < 0)
                {
                    BuyFutures(volume);
                }
            }
        }

        private void CheckClosePositionFutures()
        {
            if (_tabFutures.PositionsOpenAll.Count == 0)
            {
                _checkClosePosition = false;
                return;
            }

            if (!_tabFutures.PositionsOpenAll[0].CloseActive)
            {
                SendNewLogMessage("Все закрывающие ордера исполнены", _logType);

                _checkClosePosition = false;
                return;
            }
        }

        private void CheckOpenPositionFutures()
        {
            if (_tabFutures.PositionsOpenAll.Count == 0) return;
            if (_tabFutures.PositionsOpenAll[0].OpenOrders.Count == 0) return;

            if (_tabFutures.PositionsOpenAll[0].OpenOrders[^1].State == OrderStateType.Done)
            {
                SendNewLogMessage("Все открывающие ордера исполнены", _logType);

                _checkOpenPosition = false;
            }
        }

        private bool _priceActivationState = false;

        private bool CheckPriceActivation()
        {
            if (_directionPriceActivation == DirectionPriceActivation.Higher.ToString())
            {
                if (_priceActivationState && _tabFutures.CandlesAll[^1].Close > _priceActivation - _priceDeactivationHysteresis)
                {
                    return true;
                }

                if (_tabFutures.CandlesAll[^1].Close > _priceActivation)
                {
                    if (!_priceActivationState)
                    {
                        SendNewLogMessage("Цена БА выше установленной. Активируем ДХ", _logType);
                    }

                    _priceActivationState = true;
                    return true;
                }
                
                if (_priceActivationState && _tabFutures.CandlesAll[^1].Close < _priceActivation - _priceDeactivationHysteresis)
                {
                    CloseHedgeFutures();
                    SendNewLogMessage($"Цена БА актива вышла за пределы интервала. Last price: {_tabFutures.CandlesAll[^1].Close}, price deactivation: {_priceActivation - _priceDeactivationHysteresis}, ", _logType);
                    return false;
                }
            }

            if (_directionPriceActivation == DirectionPriceActivation.Lower.ToString())
            {
                if (_priceActivationState && _tabFutures.CandlesAll[^1].Close < _priceActivation + _priceDeactivationHysteresis)
                {
                    return true;
                }

                if (_tabFutures.CandlesAll[^1].Close < _priceActivation)
                {
                    if (!_priceActivationState)
                    {
                        SendNewLogMessage("Цена БА ниже установленной. Активируем ДХ", _logType);
                    }

                    _priceActivationState = true;
                    return true;
                }
                
                if (_priceActivationState && _tabFutures.CandlesAll[^1].Close > _priceActivation + _priceDeactivationHysteresis)
                {
                    CloseHedgeFutures();
                    SendNewLogMessage($"Цена БА актива вышла за пределы интервала. Last price: {_tabFutures.CandlesAll[^1].Close}, price deactivation: {_priceActivation + _priceDeactivationHysteresis}, ", _logType);
                    return false;
                }
            }

            return false;
        }

        private void CloseHedgeFutures()
        {
            if (!_priceActivationState) return;

            if (_tabFutures.PositionsOpenAll.Count > 0)
            {
                Position position = _tabFutures.PositionsOpenAll[0];
                decimal volume = position.OpenVolume;

                _tabFutures.CloseAtMarket(position, volume);

                SendNewLogMessage("Деактивания хеджа. Закрываем позицию по фьючерсу", _logType);

                _priceActivationState = false;
            }
        }

        private void BuyFutures(decimal volume)
        {
            if (_tabFutures.PositionOpenShort.Count > 0)
            {
                if (_tabFutures.PositionOpenShort[0].OpenVolume >= volume)
                {
                    _tabFutures.CloseAtMarket(_tabFutures.PositionOpenShort[0], volume);                    
                }
                else
                {
                    _tabFutures.CloseAtMarket(_tabFutures.PositionOpenShort[0], _tabFutures.PositionOpenShort[0].OpenVolume);
                }

                _checkClosePosition = true;
                SendNewLogMessage("Увеличиваем позицию по фьючерсу (закрытие ордера)", _logType);
            }
            else
            {
                if (_tabFutures.PositionOpenLong.Count == 0)
                {
                    _tabFutures.BuyAtMarket(volume);
                }
                else
                {
                    Position position = _tabFutures.PositionOpenLong[0];
                    _tabFutures.BuyAtMarketToPosition(position, volume);
                }

                _checkOpenPosition = true;
                SendNewLogMessage("Увеличиваем позицию по фьючерсу (открытие ордера)", _logType);
            }
        }

        private void SellFutures(decimal volume)
        {
            if (_tabFutures.PositionOpenLong.Count > 0)
            {
                if (_tabFutures.PositionOpenLong[0].OpenVolume >= volume)
                {
                    _tabFutures.CloseAtMarket(_tabFutures.PositionOpenLong[0], volume);                    
                }
                else
                {
                    _tabFutures.CloseAtMarket(_tabFutures.PositionOpenLong[0], _tabFutures.PositionOpenLong[0].OpenVolume);
                }

                _checkClosePosition = true;
                SendNewLogMessage("Уменьшаем позицию по фьючерсу (закрытие ордера)", _logType);
            }
            else
            {
                if (_tabFutures.PositionOpenShort.Count == 0)
                {
                    _tabFutures.SellAtMarket(volume);
                }
                else
                {
                    Position position = _tabFutures.PositionOpenShort[0];
                    _tabFutures.SellAtMarketToPosition(position, volume);
                }

                _checkOpenPosition = true;
                SendNewLogMessage("Уменьшаем позицию по фьючерсу (открытие ордера)", _logType);
            }
        }

        private decimal GetDeltaOption()
        {
            if (_tabOption.PositionsOpenAll.Count == 0)
            {
                return 0;
            }

            decimal volume = _tabOption.PositionsOpenAll[0].OpenVolume;

            if (_tabOption.PositionsOpenAll[0].Direction == Side.Buy)
            {
                return volume * (decimal)_tabOption.Connector.OptionMarketData.Delta;
            }
            else
            {
                return -volume * (decimal)_tabOption.Connector.OptionMarketData.Delta;
            }
        }

        private decimal GetDeltaFutures()
        {
            if (_tabFutures.PositionsOpenAll.Count == 0)
            {
                return 0;
            }

            decimal volume = _tabFutures.PositionsOpenAll[0].OpenVolume;

            if (_tabFutures.PositionsOpenAll[0].Direction == Side.Buy)
            {
                return volume;
            }
            else
            {
                return -volume;
            }
        }

        #endregion

        private enum DirectionPriceActivation
        {
            Higher,
            Lower
        }
    }

    
}
