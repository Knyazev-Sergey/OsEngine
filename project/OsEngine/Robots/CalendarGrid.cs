using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Drawing;
using System.Windows.Forms.Integration;
using System.Windows.Forms;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace OsEngine.Robots
{
    [Bot("CalendarGrid")]
    public class CalendarGrid : BotPanel
    {
        #region Constructor

        private BotTabSimple _tab;
        private StrategyParameterDecimal _setDiffValueUsd;
        private StrategyParameterDecimal _setRatioForStop;
        private StrategyParameterDecimal _setRatioForCounterOrder;
        private StrategyParameterString _setCounterOrder;
        private StrategyParameterInt _setDelayCycle;
        private StrategyParameterDecimal _setOrderSizeForBestPrice;
        private StrategyParameterBool _setTradeOneSecurity;
        private Logging.LogMessageType _logging = Logging.LogMessageType.User;

        public CalendarGrid(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];
            _tab.ManualPositionSupport.DisableManualSupport();

            this.ParamGuiSettings.Title = "CalendarGrid";
            this.ParamGuiSettings.Height = 400;
            this.ParamGuiSettings.Width = 400;

            string tabName = " Параметры ";

            _setDiffValueUsd = CreateParameter("Разница объемов, $", 0m, 0m, 0m, 0m, tabName);
            _setRatioForStop = CreateParameter("Соотношение стоимости активов для остановки бота", 0m, 0m, 0m, 0m, tabName);
            _setRatioForCounterOrder = CreateParameter("Соотношение стоимости активов для удовлетворения встречных заявок", 0m, 0m, 0m, 0m, tabName);
            _setCounterOrder = CreateParameter("Брать заявку по встречной цене", "Off", new string[] { "Off", "On" }, tabName);
            _setDelayCycle = CreateParameter("Задержка перезапуска цикла, сек.", 30, 0, 0, 0, tabName);
            _setOrderSizeForBestPrice = CreateParameter("Размер ордера, меньше которого не встаем лучшей ценой", 0m, 0m, 0m, 0m, tabName);
            _setTradeOneSecurity = CreateParameter("Торговать только один инструмент", false, tabName);

            CustomTabToParametersUi customTabMonitoring = ParamGuiSettings.CreateCustomTab(" Мониторинг ");

            CreateTableMonitoring();
            customTabMonitoring.AddChildren(_hostMonitoring);

            Thread threadMonitoring = new Thread(ThreadRefreshMonitoring) { IsBackground = true };
            threadMonitoring.Start();

            _tab.CandleUpdateEvent += _tab_CandleUpdateEvent;

            _tab.Connector.AdditionalDataEvent += Connector_AdditionalDataEvent;
        }

        private decimal _lastMarkPrice = 0;

        private void Connector_AdditionalDataEvent(OptionMarketData obj)
        {
            _lastMarkPrice = obj.MarkPrice.ToDecimal();
        }

        private decimal _lastPrice;

        private void _tab_CandleUpdateEvent(List<Candle> obj)
        {
            if (obj == null || obj.Count == 0) return;

            _lastPrice = obj[^1].Close;
        }

        #endregion

        #region Monitoring

        private WindowsFormsHost _hostMonitoring;
        private DataGridView _dgvMonitoring;

        private void CreateTableMonitoring()
        {
            _hostMonitoring = new WindowsFormsHost();

            _dgvMonitoring = GridMonitoring();

            _hostMonitoring.Child = _dgvMonitoring;
        }

        private DataGridView GridMonitoring()
        {
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

            dgv.ColumnCount = 3;
            dgv.RowCount = 6;

            foreach (DataGridViewColumn column in dgv.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                column.DefaultCellStyle.SelectionBackColor = dgv.DefaultCellStyle.BackColor;
                column.DefaultCellStyle.SelectionForeColor = dgv.DefaultCellStyle.ForeColor;
                column.ReadOnly = true;
            }

            dgv.Columns[0].Width = 250;

            dgv.Columns[1].HeaderText = " ";
            dgv.Columns[2].HeaderText = " ";

            dgv[0, 0].Value = "Mark Price";
            dgv[0, 1].Value = "Объем позиции";
            dgv[0, 2].Value = "Капитал";
            dgv[0, 3].Value = "Разница";
            dgv[0, 4].Value = "Объем позиции в USDT";
            dgv[0, 5].Value = "Кросс-курс последней сделки";

            return dgv;
        }

        private void ThreadRefreshMonitoring()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(1000);
                    RefreshMonitoring();
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                }
            }
        }

        private decimal _diffValueUSD;

        private void RefreshMonitoring()
        {
            DataGridView dgv = _dgvMonitoring;

            if (_tab == null)
            {
                return;
            }

            if (_tab.Security != null)
            {
                dgv.Columns[1].HeaderText = _tab.Security.Name;
            }

            if (_tab.PositionsOpenAll == null || _tab.PositionsOpenAll.Count == 0) return;

            if (_lastMarkPrice == 0) return;

            dgv[1, 0].Value = _lastMarkPrice;

            /*decimal valueOpenPosition = Math.Round(_tab.PositionsOpenAll[0].OpenVolume / _lastMarkPrice, 6);
            dgv[1, 1].Value = valueOpenPosition;*/

            if (_tab.Portfolio == null) return;

            var posOnBoard = _tab.Portfolio.GetPositionOnBoard();

            decimal valueOpenPosition = 0;
            decimal valueCurrentDeposit = 0;

            for (int i = 0; i < posOnBoard.Count; i++)
            {
                if (posOnBoard[i].SecurityNameCode == "ETH")
                {
                    valueCurrentDeposit = posOnBoard[i].ValueCurrent;
                    dgv[1, 2].Value = valueCurrentDeposit;
                }

                if (posOnBoard[i].SecurityNameCode == _tab.Security.Name)
                {
                    valueOpenPosition = Math.Abs(Math.Round(posOnBoard[i].ValueCurrent / _lastMarkPrice, 6));

                    dgv[1, 1].Value = valueOpenPosition;
                }
            }        
            
            if (valueCurrentDeposit != 0 && valueOpenPosition != 0)
            {
                decimal diffValue = valueOpenPosition - valueCurrentDeposit;
                _diffValueUSD = Math.Floor(diffValue * _lastMarkPrice);

                dgv[1, 3].Value = diffValue;
                dgv[2, 3].Value = _diffValueUSD;
            }           
        }

        #endregion
    }
}
