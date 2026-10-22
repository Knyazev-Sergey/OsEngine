/*
 * 4.10.26 исправлено восстановление ордеров после их отмены и после перезапуска
 *          добавил зону активации хеджа по unrealizPnl
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace OsEngine.Robots
{
    [Bot("GridBot")]
    public class GridBot : BotPanel
    {
        private StartProgram _startProgram;
        private BotTabSimple _tab;
        private BotTabSimple _tabRSI;
        private BotTabSimple _tabHedge;
        private StrategyParameterString _regime;
        private StrategyParameterString _typeVolume;
        private StrategyParameterDecimal _volume;
        private StrategyParameterDecimal _leverage;
        private StrategyParameterBool _loadGrig;
        private StrategyParameterString _stringFileSample;
        private StrategyParameterDecimal _сoveringPriceChanges;
        private StrategyParameterInt _countOrdersGrid;
        private StrategyParameterDecimal _percentMartingale;
        private StrategyParameterDecimal _firstOrderIndent;
        private StrategyParameterDecimal _profit;
        private StrategyParameterDecimal _rearrangeGrid;
        private StrategyParameterInt _countOrdersOnExchange;
        private StrategyParameterDecimal _delayRearrange;
        private StrategyParameterDecimal _delayAfterCycle;
        private StrategyParameterDecimal _logPriceLevels;  
        private List<string> _tableListSide = new List<string> { "Buy", "Sell" };
        private Status _status = Status.NoWork;
        private StrategyParameterDecimal _minVolumeForTicker;
        private StrategyParameterBool _autoCalcMartingaleAmountOrders;
        private StrategyParameterDecimal _setTimeTwapOrders;
        private StrategyParameterBool _boolTimerTwapOrders;
        private StrategyParameterBool _setAutoStart;

        Aindicator _rsi;
        Aindicator _rsiBTC;
        private StrategyParameterString _rsiOnOff;
        private StrategyParameterString _choiseRSI;
        private StrategyParameterString _operationRSI;
        private StrategyParameterInt _valueRsi;
        private StrategyParameterInt _periodRsi;
        private decimal _lastRsi;
        private decimal _lastRsiSecond;

        private StrategyParameterString _setRegimeAutoHedge;
        private StrategyParameterString _setTypeLotHedge;
        private StrategyParameterDecimal _setLotHedge;
        private StrategyParameterString _setHedgeTypeLoss;
        private StrategyParameterDecimal _setHedgeLoss;
        private StrategyParameterBool _setZoneActivMainPriceBool;
        private StrategyParameterDecimal _setZoneActivMainPriceValue;
        private StrategyParameterBool _setZoneActivDepositLossBool;
        private StrategyParameterDecimal _setZoneActivDepositLossValue;
        private StrategyParameterBool _setZoneActivRangeLiquidationBool;
        private StrategyParameterDecimal _setRangeLiquidationValue;
        private StrategyParameterDecimal _setOffsetLastOrder;
        private StrategyParameterInt _countOrdersGridHedge;
        private StrategyParameterDecimal _percentMartingaleHedge;
        private StrategyParameterDecimal _logPriceLevelsHedge;
        private StrategyParameterDecimal _profitHedge;

        private StrategyParameterInt _setValueUnrealizePnLToClose;

        private StrategyParameterBool _setZoneActivUnrealPnlBool;
        private StrategyParameterDecimal _setUnrealPnlValue;

        private StrategyParameterBool _setZoneDeactivRSIBool;

        private bool _versionDiff = true;

        public GridBot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");

            _startProgram = startProgram;

            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];
            TabCreate(BotTabType.Simple);
            _tabRSI = TabsSimple[1];
            TabCreate(BotTabType.Simple);
            _tabHedge = TabsSimple[2];

            string tabNameParameters = " Параметры ";
            string tabNameRsi = " Фильтр RSI ";
            string tabNameAutoHedge = " Автохедж ";

            _setAutoStart = CreateParameter("Автоподключение", false, tabNameParameters);
            _typeVolume = CreateParameter("Тип объема", GetDescription(TypeVolume.Percent), new string[] { GetDescription(TypeVolume.Fix), GetDescription(TypeVolume.Percent) }, tabNameParameters);
            _volume = CreateParameter("Объем", 0m, 0m, 0m, 0m, tabNameParameters);
            _leverage = CreateParameter("Плечо", 5m, 0m, 0m, 0m, tabNameParameters);
            _regime = CreateParameter("Режим работы", GetDescription(Regime.Long),
                new string[] { GetDescription(Regime.Long), GetDescription(Regime.Short) }, tabNameParameters);

            _loadGrig = CreateParameter("Загрузить сетку из шаблона", false, tabNameParameters);
            _stringFileSample = CreateParameter("Путь к файлу шаблона", "", tabNameParameters);
            _сoveringPriceChanges = CreateParameter("Перекрытие изменения цены, %", 5m, 0m, 0m, 0m, tabNameParameters);
            _countOrdersGrid = CreateParameter("Сетка ордеров", 20, 0, 0, 0, tabNameParameters);
            _percentMartingale = CreateParameter("Процент мартингейла, %", 0.01m, 0m, 0m, 0m, tabNameParameters);
            _autoCalcMartingaleAmountOrders = CreateParameter("Автоматический расчет ордеров по Мартингейлу", false, tabNameParameters);
            _firstOrderIndent = CreateParameter("Отступ первого ордера, %", 0.15m, 0m, 0m, 0m, tabNameParameters);
            _profit = CreateParameter("Профит, %", 1m, 0m, 0m, 0m, tabNameParameters);
            _rearrangeGrid = CreateParameter("Подтяжка сетки ордеров, %", 0.3m, 0m, 0m, 0m, tabNameParameters);
            _countOrdersOnExchange = CreateParameter("Частичное выставление", 3, 0, 0, 0, tabNameParameters);
            _delayRearrange = CreateParameter("Задержка перед отменой сетки ордеров для подтяжки, мин", 0m, 0m, 0m, 0m, tabNameParameters);
            _delayAfterCycle = CreateParameter("Задержка после завершения цикла, мин", 0m, 0m, 0m, 0m, tabNameParameters);
            _logPriceLevels = CreateParameter("Логарифмическое распределение уровней цен", 0.01m, 0m, 0m, 0m, tabNameParameters);
            _minVolumeForTicker = CreateParameter("Минимальный объем по инструменту", 0m, 0m, 0m, 0m, tabNameParameters);
            _boolTimerTwapOrders = CreateParameter("Включить TWAP ордера", false, tabNameParameters);
            _setTimeTwapOrders = CreateParameter("Задержка выставления TWAP ордеров", 0m, 0m, 0m, 0m, tabNameParameters);

            _rsiOnOff = CreateParameter("Фильтр RSI", RegimeOnOff.Off.ToString(), new string[] { RegimeOnOff.Off.ToString(), RegimeOnOff.On.ToString() }, tabNameRsi);
            _periodRsi = CreateParameter("Период RSI", 14, 0, 0, 0, tabNameRsi);
            _choiseRSI = CreateParameter("Выбор по", GetDescription(ChoiseRSI.Current), new string[] { GetDescription(ChoiseRSI.Current), GetDescription(ChoiseRSI.Second) }, tabNameRsi);
            _operationRSI = CreateParameter("Условие фильтрации", GetDescription(RsiOperation.Less), new string[] { GetDescription(RsiOperation.Less), GetDescription(RsiOperation.More) }, tabNameRsi);
            _valueRsi = CreateParameter("Значение RSI", 40, 0, 0, 0, tabNameRsi); 

            _rsi = IndicatorsFactory.CreateIndicatorByName("RSI", name + "RSI", true);
            _rsi = (Aindicator)_tab.CreateCandleIndicator(_rsi, name + "NewArea");
            ((IndicatorParameterInt)_rsi.Parameters[0]).ValueInt = _periodRsi.ValueInt;
            _rsi.Save();            

            _rsiBTC = IndicatorsFactory.CreateIndicatorByName("RSI", name + "RSIBTC", true);
            _rsiBTC = (Aindicator)_tabRSI.CreateCandleIndicator(_rsiBTC, name + "NewArea");
            ((IndicatorParameterInt)_rsiBTC.Parameters[0]).ValueInt = _periodRsi.ValueInt;
            _rsiBTC.Save();            

            _setRegimeAutoHedge = CreateParameter("Вкл/выкл Автохедж", RegimeOnOff.Off.ToString(), new string[] { RegimeOnOff.Off.ToString(), RegimeOnOff.On.ToString() }, tabNameAutoHedge);
            _setTypeLotHedge = CreateParameter("Тип лота хеджирования ", GetDescription(TypeVolume.Fix), new string[] { GetDescription(TypeVolume.Fix), GetDescription(TypeVolume.Percent) }, tabNameAutoHedge);
            _setLotHedge = CreateParameter("Лот хеджирования", 10m, 0m, 0m, 0m, tabNameAutoHedge);
            _setHedgeTypeLoss = CreateParameter("Тип допустимого убытка", GetDescription(TypeVolume.Fix), new string[] { GetDescription(TypeVolume.Fix), GetDescription(TypeVolume.Percent) }, tabNameAutoHedge);
            _setHedgeLoss = CreateParameter("Допустимый убыток", 5m, 0m, 0m, 0m, tabNameAutoHedge);
            _setZoneActivMainPriceBool = CreateParameter("Зона активации: % от цены основной позиции", false, tabNameAutoHedge);
            _setZoneActivMainPriceValue = CreateParameter("% от цены основной позиции", 10m, 0m, 0m, 0m, tabNameAutoHedge);
            _setZoneActivDepositLossBool = CreateParameter("Зона активации: % от просадки депозита", false, tabNameAutoHedge);
            _setZoneActivDepositLossValue = CreateParameter("% от просадки депозита", 10m, 0m, 0m, 0m, tabNameAutoHedge);
            _setZoneActivRangeLiquidationBool = CreateParameter("Зона активации: диапазон ликвидации", false, tabNameAutoHedge);
            _setRangeLiquidationValue = CreateParameter("Диапазон ликвидации, % от рыночной цены", 10m, 0m, 0m, 0m, tabNameAutoHedge);
            _setOffsetLastOrder = CreateParameter("Смещение последнего ордера, %", 5m, 0m, 0m, 0m, tabNameAutoHedge);
            _countOrdersGridHedge = CreateParameter("Сетка ордеров хедж сетки", 20, 0, 0, 0, tabNameAutoHedge);
            _percentMartingaleHedge = CreateParameter("Процент мартингейла хедж сетки, %", 0.01m, 0m, 0m, 0m, tabNameAutoHedge);
            _logPriceLevelsHedge = CreateParameter("Логарифмическое распределение уровней цен хедж сетки", 0.01m, 0m, 0m, 0m, tabNameAutoHedge);
            _profitHedge = CreateParameter("Профит хедж сетки, %", 1m, 0m, 0m, 0m, tabNameAutoHedge);

            if (_versionDiff)
            {
                _setZoneActivUnrealPnlBool = CreateParameter("Зона активации: значение UnrealizePnL", false, tabNameAutoHedge);
                _setUnrealPnlValue = CreateParameter("Значение UnrealizePnL для активации хеджа (с минусом)", -50m, 0m, 0m, 0m, tabNameAutoHedge);
                _setValueUnrealizePnLToClose = CreateParameter("Значение UnrealizePnL для закрытия позиции (с минусом)", -200, 0, 0, 0, tabNameAutoHedge);
                _setZoneDeactivRSIBool = CreateParameter("Зона активации: при отмене ордеров по RSI", true, tabNameAutoHedge);
            }

            ParametrsChangeByUser += GridBot_ParametrsChangeByUser;

            this.ParamGuiSettings.Title = "Grid Bot";
            this.ParamGuiSettings.Height = 600;
            this.ParamGuiSettings.Width = 700;

            CustomTabToParametersUi customTabGrid = ParamGuiSettings.CreateCustomTab(" Ручная настройка сетки ");
            CustomTabToParametersUi customTabMonitoring = ParamGuiSettings.CreateCustomTab(" Мониторинг и управление ");
            CustomTabToParametersUi customTabLogs = ParamGuiSettings.CreateCustomTab(" Логи ");

            CreateTableGrid();
            customTabGrid.AddChildren(_hostTableGrid);

            CreateTableMonitoring();
            customTabMonitoring.AddChildren(_hostMonitoring);

            CreateTableLogs();
            customTabLogs.AddChildren(_hostLogs);

            _tab.ManualPositionSupport.DisableManualSupport();
            _tab.ManualPositionSupport.OrderTypeTime = OrderTypeTime.GTC;
            _tabHedge.ManualPositionSupport.DisableManualSupport();
            _tabHedge.ManualPositionSupport.OrderTypeTime = OrderTypeTime.GTC;

            if (startProgram == StartProgram.IsOsTrader)
            {
                _tab.CandleUpdateEvent += _tab_CandleUpdateEvent;

                Thread pollingThread = new Thread(PollingThread) { IsBackground = true };
                pollingThread.Start();                
            }

            Thread worker = new Thread(ThreadRefreshTable) { IsBackground = true };
            worker.Start();

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tabRSI.CandleFinishedEvent += _tabRSI_CandleFinishedEvent;

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

            _tab.DeleteBotEvent += Tab_DeleteBotEvent;
        }

        private void _tabRSI_CandleFinishedEvent(List<Candle> obj)
        {
            if (_choiseRSI == GetDescription(ChoiseRSI.Second))
            {
                _timeCheckRsi = true;
            }
        }

        private bool _deleteBot = false;

        private void Tab_DeleteBotEvent(int obj)
        {
            _deleteBot = true;
        }

        private void Server_TestingStartEvent()
        {
            _autoHedgeState = AutoHedgeState.Monitoring;

            CreateNewGrid(TypeGrid.Main);
            CreateNewGrid(TypeGrid.Hedge);

            _main.State_PlaceNewGrid = true;

            _status = Status.Work;
            _timeStatus = DateTime.Now;

            _checkAutoStart = false;

            _boolRsiBlocked = false;
            _boolRsiUnblocked = false;
            _rsiBlocked = true;
            _timeCheckRsi = false;
            _lastRsi = 0;
            _lastRsiSecond = 0;
        }

        private void GridBot_ParametrsChangeByUser()
        {
            if (((IndicatorParameterInt)_rsi.Parameters[0]).ValueInt != _periodRsi.ValueInt)
            {
                ((IndicatorParameterInt)_rsi.Parameters[0]).ValueInt = _periodRsi.ValueInt;
                _rsi.Save();
                _rsi.Reload();
            }

            if (((IndicatorParameterInt)_rsiBTC.Parameters[0]).ValueInt != _periodRsi.ValueInt)
            {
                ((IndicatorParameterInt)_rsiBTC.Parameters[0]).ValueInt = _periodRsi.ValueInt;
                _rsiBTC.Save();
                _rsiBTC.Reload();
            }
        }

        #region Logs grid tab

        private WindowsFormsHost _hostLogs;
        private DataGridView _gridLogs;

        private void CreateTableLogs()
        {
            _hostLogs = new WindowsFormsHost();            

            DataGridView dgv =
                DataGridFactory.GetDataGridView(DataGridViewSelectionMode.CellSelect,
                DataGridViewAutoSizeRowsMode.AllCells);

            dgv.Dock = DockStyle.Fill;
            dgv.ScrollBars = ScrollBars.Both;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            dgv.ColumnCount = 1;
            dgv.RowCount = 1;

            foreach (DataGridViewColumn column in dgv.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
                column.ReadOnly = true;
            }

            _hostLogs.Child = dgv;
            _gridLogs = dgv;

            _gridLogs.CellEndEdit += _gridLogs_CellEndEdit;
        }

        private void _gridLogs_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            
        }

        private void AddDataToLogsGrid()
        {
            if (MainWindow.GetDispatcher.CheckAccess() == false)
            {
                MainWindow.GetDispatcher.Invoke(new Action(AddDataToLogsGrid));
                return;
            }

            List<string> str = new();

            str.Add($"_status: {_status}");
            str.Add($"_rsiBlocked: {_rsiBlocked}");
            str.Add($"_startDepositValue: {_startDepositValue}");
            str.Add($"_autoHedgeState = " + _autoHedgeState);

            if (_main != null)
            {
                str.Add("\nMain:");
                str.Add($"State_PlaceNewGrid = " + _main.State_PlaceNewGrid);
                str.Add($"State_PlaceNewOrderGrid = " + _main.State_PlaceNewOrderGrid);
                str.Add($"State_CheckDoneOpenOrder = " + _main.State_CheckDoneOpenOrder);
                str.Add($"State_CheckDoneCloseOrder = " + _main.State_CheckDoneCloseOrder);
                str.Add($"State_CheckCancelActiveCloseOrder = " + _main.State_CheckCancelActiveCloseOrder);
                str.Add($"State_PlaceCloseOrder = " + _main.State_PlaceCloseOrder);
                str.Add($"State_CheckPlaceCloseOrder = " + _main.State_CheckPlaceCloseOrder);
                str.Add($"State_CancelOpenOrders = " + _main.State_CancelOpenOrders);
                str.Add($"State_CheckCancelOpenOrders = " + _main.State_CheckCancelOpenOrders);
                str.Add($"State_CheckRearrangeGrid = " + _main.State_CheckRearrangeGrid);
                str.Add($"State_NeedRearrangeGrid = " + _main.State_NeedRearrangeGrid);
                str.Add($"State_HasHedgeGrid = " + _main.State_HasHedgeGrid);
                str.Add($"State_NeedReCalcGrid = " + _main.State_NeedReCalcGrid);
                str.Add($"State_CancelActiveOrders = " + _main.State_CancelActiveOrders);
                str.Add($"State_CheckCancelActiveOrders = " + _main.State_CheckCancelActiveOrders);
                str.Add($"State_ClosePositionAtMarket = " + _main.State_ClosePositionAtMarket);
                str.Add($"State_CheckClosePositionAtMarket = " + _main.State_CheckClosePositionAtMarket);
                str.Add($"State_CheckUnrealizedPnL = " + _main.State_CheckUnrealizedPnL);

                str.Add($"\nListOrders: {_main.ListOrders.Count}");

                if (_main.ListOrders.Count > 0)
                {
                    List<ListOrders> list = _main.ListOrders;

                    for (int i = 0; i < list.Count; i++)
                    {
                        str.Add($"Price: {list[i].Price}, Vol: {list[i].Volume}");
                    }
                }
            }

            if (_hedge != null)
            {
                str.Add("\nHedge:");                
                str.Add($"State_PlaceNewGrid = " + _hedge.State_PlaceNewGrid);
                str.Add($"State_PlaceNewOrderGrid = " + _hedge.State_PlaceNewOrderGrid);
                str.Add($"State_CheckDoneOpenOrder = " + _hedge.State_CheckDoneOpenOrder);
                str.Add($"State_CheckDoneCloseOrder = " + _hedge.State_CheckDoneCloseOrder);
                str.Add($"State_CheckCancelActiveCloseOrder = " + _hedge.State_CheckCancelActiveCloseOrder);
                str.Add($"State_PlaceCloseOrder = " + _hedge.State_PlaceCloseOrder);
                str.Add($"State_CheckPlaceCloseOrder = " + _hedge.State_CheckPlaceCloseOrder);
                str.Add($"State_CancelOpenOrders = " + _hedge.State_CancelOpenOrders);
                str.Add($"State_CheckCancelOpenOrders = " + _hedge.State_CheckCancelOpenOrders);
                str.Add($"State_CheckRearrangeGrid = " + _hedge.State_CheckRearrangeGrid);
                str.Add($"State_NeedRearrangeGrid = " + _hedge.State_NeedRearrangeGrid);
                str.Add($"State_HasHedgeGrid = " + _hedge.State_HasHedgeGrid);
                str.Add($"State_NeedReCalcGrid = " + _hedge.State_NeedReCalcGrid);
                str.Add($"State_CancelActiveOrders = " + _hedge.State_CancelActiveOrders);
                str.Add($"State_CheckCancelActiveOrders = " + _hedge.State_CheckCancelActiveOrders);
                str.Add($"State_ClosePositionAtMarket = " + _hedge.State_ClosePositionAtMarket);
                str.Add($"State_CheckClosePositionAtMarket = " + _hedge.State_CheckClosePositionAtMarket);
                str.Add($"State_CheckUnrealizedPnL = " + _hedge.State_CheckUnrealizedPnL);

                str.Add($"\nListOrders: {_hedge.ListOrders.Count}");

                if (_hedge.ListOrders.Count > 0)
                {
                    List<ListOrders> list = _hedge.ListOrders;

                    for (int i = 0; i < list.Count; i++)
                    {
                        str.Add($"Price: {list[i].Price}, Vol: {list[i].Volume}");
                    }
                }
            }

            while (_gridLogs.Rows.Count < str.Count)
            {
                _gridLogs.Rows.Add();
            }

            while (_gridLogs.Rows.Count > str.Count)
            {
                _gridLogs.Rows.RemoveAt(0);
            }

            for (int i = 0; i < str.Count; i++)
            {
                _gridLogs.Rows[i].Cells[0].Value = str[i];
            }
        }

        #endregion

        #region Manual grid tab

        private WindowsFormsHost _hostTableGrid;
        private DataGridView _gridTableGrid;
        private DataGridView _gridButtonGrid;                

        private void CreateTableGrid()
        {           
            _hostTableGrid = new WindowsFormsHost();

            _gridButtonGrid = AddManualGridButton();     
            _gridTableGrid = AddManualGridTable();

            TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.ColumnCount = 1;
            tableLayoutPanel.RowCount = 2;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));

            tableLayoutPanel.Controls.Add(_gridButtonGrid, 0, 0);
            tableLayoutPanel.Controls.Add(_gridTableGrid, 0, 1);

            _gridTableGrid.CellClick += GridTableGrid_CellClick;
            _gridTableGrid.CellValueChanged += GridTableGrid_CellValueChanged;
            _gridTableGrid.DataError += _gridTableGrid_DataError;

            _hostTableGrid.Child = tableLayoutPanel;            
        }

        private void _gridTableGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            
        }

        private DataGridView AddManualGridButton()
        {
            DataGridView newButtonGrid =
               DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect,
               DataGridViewAutoSizeRowsMode.AllCells);

            newButtonGrid.Dock = DockStyle.Fill;
            newButtonGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            newButtonGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            newButtonGrid.CellClick += NewButtonGrid_CellClick; ;

            newButtonGrid.Columns.Add("Column1", "");
            newButtonGrid.Columns.Add("Column2", "");
            newButtonGrid.Columns.Add("Column3", "");
            newButtonGrid.Columns.Add("Column4", "");
            newButtonGrid.Columns.Add("Column5", "");
            newButtonGrid.Columns.Add("Column6", "");
            newButtonGrid.Columns.Add("Column7", "");

            foreach (DataGridViewColumn column in newButtonGrid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            DataGridViewRow row = new DataGridViewRow();

            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[0].Value = "Сохранить шаблон";
            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells[1].ReadOnly = true;
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[2].Value = "Загрузить шаблон";
            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells[3].ReadOnly = true;
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[4].Value = "Рассчитать сетку";
            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells[5].ReadOnly = true;
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[6].Value = "Добавить новую строку";

            newButtonGrid.Rows.Add(row);

            return newButtonGrid;
        }

        private DataGridView AddManualGridTable()
        {
            DataGridView newTableGrid =
                DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect,
                DataGridViewAutoSizeRowsMode.AllCells);

            newTableGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            newTableGrid.Dock = DockStyle.Fill;
            newTableGrid.ScrollBars = ScrollBars.Vertical;

            DataGridViewColumn colum0 = new DataGridViewColumn();
            colum0.HeaderText = "Номер";
            colum0.ReadOnly = true;
            colum0.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colum0.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colum0.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            newTableGrid.Columns.Add(colum0);

            DataGridViewColumn colum05 = new DataGridViewColumn();
            colum05.HeaderText = "% от текущей цены";
            colum05.ReadOnly = false;
            colum05.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colum05.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colum05.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            newTableGrid.Columns.Add(colum05);

            DataGridViewColumn colum01 = new DataGridViewColumn();
            colum01.HeaderText = "Рассчетная цена входа";
            colum01.ReadOnly = true;
            colum01.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colum01.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colum01.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            newTableGrid.Columns.Add(colum01);

            DataGridViewColumn colum02 = new DataGridViewColumn();
            colum02.HeaderText = "Объем";
            colum02.ReadOnly = false;
            colum02.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colum02.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colum02.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            newTableGrid.Columns.Add(colum02);

            DataGridViewColumn columAvg = new DataGridViewColumn();
            columAvg.HeaderText = "Средняя цена позиции";
            columAvg.ReadOnly = true;
            columAvg.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            columAvg.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            columAvg.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            newTableGrid.Columns.Add(columAvg);

            DataGridViewComboBoxColumn colum03 = new DataGridViewComboBoxColumn();
            colum03.HeaderText = "Направление";
            colum03.ReadOnly = false;
            colum03.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colum03.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colum03.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            newTableGrid.Columns.Add(colum03);

            DataGridViewButtonColumn colum04 = new DataGridViewButtonColumn();
            colum04.ReadOnly = true;
            colum04.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colum04.UseColumnTextForButtonValue = true;
            colum04.Text = "Удалить строку";
            newTableGrid.Columns.Add(colum04);

            return newTableGrid;
        }

        private void NewButtonGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == 0) // сохранить шаблон
                {
                    SaveGridTable();
                }

                if (e.ColumnIndex == 2) // загрузить шаблон
                {
                    LoadGridTable();
                }

                if (e.ColumnIndex == 4) //рассчитать сетку
                {
                    if (_lastPrice == 0)
                    {
                        SendNewLogMessage("Нет данных по цене инструмента", Logging.LogMessageType.Error);
                        return;
                    }

                    if (_сoveringPriceChanges.ValueDecimal == 0 ||
                    _firstOrderIndent.ValueDecimal == 0 ||
                    _volume.ValueDecimal == 0 ||
                    _leverage.ValueDecimal == 0 ||
                    _countOrdersGrid.ValueInt == 0 ||
                    _logPriceLevels.ValueDecimal == 0)
                    {
                        SendNewLogMessage("Не введены необходимые данные для работы робота", Logging.LogMessageType.Error);
                        return;
                    }

                    for (int i = 0; i < _gridTableGrid.Rows.Count; i++)
                    {
                        _gridTableGrid.Rows.RemoveAt(i);
                        i--;
                    }

                    List<ListOrders> listBuy = new List<ListOrders>();
                    List<ListOrders> listSell = new List<ListOrders>();

                    if (_regime.ValueString == GetDescription(Regime.Long))
                    {
                        listBuy = CalculateGrid(Side.Buy);
                    }
                    else if (_regime.ValueString == GetDescription(Regime.Short))
                    {
                        listSell = CalculateGrid(Side.Sell);
                    }
                    else
                    {
                        listBuy = CalculateGrid(Side.Buy);
                        listSell = CalculateGrid(Side.Sell);
                    }

                    for (int i = listSell.Count - 1; i >= 0; i--)
                    {
                        DataGridViewRow newRow = new DataGridViewRow();

                        newRow.Cells.Add(new DataGridViewTextBoxCell());
                        newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = Math.Round((listSell[i].Price / _lastPrice - 1) * 100, 4) });
                        newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = Math.Round(listSell[i].Price, _tab.Security.Decimals) });
                        newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = listSell[i].Volume });
                        newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = 0 });
                        newRow.Cells.Add(new DataGridViewComboBoxCell()

                        {
                            DataSource = _tableListSide,
                            Value = _tableListSide[1],
                        }
                        );

                        _gridTableGrid.Rows.Add(newRow);

                        _gridTableGrid.Rows[_gridTableGrid.Rows.Count - 1].Cells[0].Value = _gridTableGrid.Rows.Count;
                    }
                    for (int i = 0; i < listBuy.Count; i++)
                    {
                        DataGridViewRow newRow = new DataGridViewRow();

                        newRow.Cells.Add(new DataGridViewTextBoxCell());
                        newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = Math.Round((listBuy[i].Price / _lastPrice - 1) * 100, 4) });
                        newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = Math.Round(listBuy[i].Price, _tab.Security.Decimals) });
                        newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = listBuy[i].Volume });
                        newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = 0 });
                        newRow.Cells.Add(new DataGridViewComboBoxCell()

                        {
                            DataSource = _tableListSide,
                            Value = _tableListSide[0],
                        }
                        );

                        _gridTableGrid.Rows.Add(newRow);

                        _gridTableGrid.Rows[_gridTableGrid.Rows.Count - 1].Cells[0].Value = _gridTableGrid.Rows.Count;
                    }

                    RecalculateAveragePriceColumn();
                }

                if (e.ColumnIndex == 6) // добавить строку
                {
                    DataGridViewRow newRow = new DataGridViewRow();

                    newRow.Cells.Add(new DataGridViewTextBoxCell());
                    newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = 0 });
                    newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = 0 });
                    newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = 0 });
                    newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = 0 });
                    newRow.Cells.Add(new DataGridViewComboBoxCell()
                    {
                        DataSource = _tableListSide,
                        Value = _tableListSide[0],
                    }
                    );

                    _gridTableGrid.Rows.Add(newRow);

                    _gridTableGrid.Rows[_gridTableGrid.Rows.Count - 1].Cells[0].Value = _gridTableGrid.Rows.Count;

                    RecalculateAveragePriceColumn();
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void GridTableGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == 1)
                {
                    decimal value;
                    if (!decimal.TryParse(_gridTableGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString(), out value))
                    {
                        _gridTableGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = 0;
                    }

                    _gridTableGrid.Rows[e.RowIndex].Cells[2].Value = Math.Round(value / 100 * _lastPrice + _lastPrice, 4);
                }

                if (e.ColumnIndex == 3)
                {
                    decimal value;
                    if (!decimal.TryParse(_gridTableGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString(), out value))
                    {
                        _gridTableGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = 0;
                    }
                }

                if (e.ColumnIndex == 1 || e.ColumnIndex == 3 || e.ColumnIndex == 5)
                {
                    RecalculateAveragePriceColumn();
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void GridTableGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == 6)
                {
                    _gridTableGrid.Rows.RemoveAt(e.RowIndex);

                    for (int i = 0; i < _gridTableGrid.Rows.Count; i++)
                    {
                        _gridTableGrid.Rows[i].Cells[0].Value = i + 1;
                    }

                    RecalculateAveragePriceColumn();
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void SaveGridTable()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
            saveFileDialog.Title = "Сохранить файл";
            saveFileDialog.FileName = "sample"; 

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                    {
                        if (_gridTableGrid.Rows.Count == 0)
                        {
                            string saveString = "";
                            writer.WriteLine(saveString);
                        }
                        else
                        {
                            for (int i = 0; i < _gridTableGrid.Rows.Count; i++)
                            {
                                string saveString = "";

                                saveString += _gridTableGrid.Rows[i].Cells[1].FormattedValue.ToString() + " ";
                                saveString += _gridTableGrid.Rows[i].Cells[3].FormattedValue.ToString() + " ";
                                saveString += _gridTableGrid.Rows[i].Cells[5].FormattedValue.ToString();

                                writer.WriteLine(saveString);
                            }
                        }
                        writer.Close();
                    }
                    SendNewLogMessage("Шаблон сохранен: " + saveFileDialog.FileName, Logging.LogMessageType.User);
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                }
            }            
        }

        private void LoadGridTable()
        {
            if (MainWindow.GetDispatcher.CheckAccess() == false)
            {
                MainWindow.GetDispatcher.Invoke(new Action(LoadGridTable));
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
          
            openFileDialog.Filter = "Все файлы (*.*)|*.*|Текстовые файлы (*.txt)|*.txt";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < _gridTableGrid.Rows.Count; i++)
                {
                    _gridTableGrid.Rows.RemoveAt(i);
                    i--;
                }

                try
                {
                    string filePath = openFileDialog.FileName;

                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                string[] split = line.Split(' ');

                                decimal percent = 0;

                                decimal.TryParse(split[0], out percent);

                                DataGridViewRow newRow = new DataGridViewRow();

                                newRow.Cells.Add(new DataGridViewTextBoxCell());
                                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = split[0] });
                                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = Math.Round(percent / 100 * _lastPrice + _lastPrice, 4) });
                                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = split[1] });
                                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = 0 });
                                newRow.Cells.Add(new DataGridViewComboBoxCell()

                                {
                                    DataSource = _tableListSide,
                                    Value = split[2],
                                }
                                );

                                _gridTableGrid.Rows.Add(newRow);

                                _gridTableGrid.Rows[_gridTableGrid.Rows.Count - 1].Cells[0].Value = _gridTableGrid.Rows.Count;
                            }
                        }
                        reader.Close();
                    }

                    RecalculateAveragePriceColumn();
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                }
            }         
        }

        private void RecalculateAveragePriceColumn()
        {
            try
            {
                int count = _gridTableGrid.Rows.Count;
                decimal[] averages = new decimal[count];

                decimal sumCostBuy = 0m;
                decimal sumVolumeBuy = 0m;

                for (int i = 0; i < count; i++)
                {
                    DataGridViewRow row = _gridTableGrid.Rows[i];

                    decimal price = GetCellDecimalValue(row, 2);
                    decimal volume = GetCellDecimalValue(row, 3);
                    string side = row.Cells[5].Value?.ToString();

                    if (price > 0 && volume > 0 && side == _tableListSide[0])
                    {
                        sumCostBuy += price * volume;
                        sumVolumeBuy += volume;
                        averages[i] = sumVolumeBuy > 0 ? sumCostBuy / sumVolumeBuy : 0m;
                    }
                }

                decimal sumCostSell = 0m;
                decimal sumVolumeSell = 0m;

                for (int i = count - 1; i >= 0; i--)
                {
                    DataGridViewRow row = _gridTableGrid.Rows[i];

                    decimal price = GetCellDecimalValue(row, 2);
                    decimal volume = GetCellDecimalValue(row, 3);
                    string side = row.Cells[5].Value?.ToString();

                    if (price > 0 && volume > 0 && side == _tableListSide[1])
                    {
                        sumCostSell += price * volume;
                        sumVolumeSell += volume;
                        averages[i] = sumVolumeSell > 0 ? sumCostSell / sumVolumeSell : 0m;
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    _gridTableGrid.Rows[i].Cells[4].Value = Math.Round(averages[i], _tab.Security.Decimals);
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private decimal GetCellDecimalValue(DataGridViewRow row, int columnIndex)
        {
            if (row == null ||
                row.Cells.Count <= columnIndex ||
                row.Cells[columnIndex] == null ||
                row.Cells[columnIndex].Value == null)
            {
                return 0;
            }

            object valueObj = row.Cells[columnIndex].Value;

            if (valueObj is decimal decimalValue)
            {
                return decimalValue;
            }

            if (valueObj is double doubleValue)
            {
                return Convert.ToDecimal(doubleValue);
            }

            if (valueObj is int intValue)
            {
                return intValue;
            }

            decimal result;

            if (decimal.TryParse(valueObj.ToString(), out result))
            {
                return result;
            }

            return 0;
        }

        #endregion

        #region Monitoring tab
                
        private WindowsFormsHost _hostMonitoring;
        private DataGridView _gridMonitoring;
        private DataGridView _gridMonitoringButton;

        Status _statusValue = Status.NoWork;
        private DateTime _timeStatus;
        private int _postedOrdersMain;
        private int _postedOrdersHedge;
        private int _completedOrdersMain;
        private int _completedOrdersHedge;

        private void ThreadRefreshTable()
        {
            while (!_deleteBot)
            {
                try
                {
                    Thread.Sleep(1000);

                    if (!ParamGuiIsOpen) continue;

                    OrdersMonitoring();
                    AddDataToGrid();
                    AddDataToLogsGrid();
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                }
            }           
        }

        private void CreateTableMonitoring()
        {
            _hostMonitoring = new WindowsFormsHost();

            _gridMonitoringButton = AddMonitoringButton();  
            _gridMonitoring = AddMonitoringTable();

            TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.ColumnCount = 1;
            tableLayoutPanel.RowCount = 2;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));

            tableLayoutPanel.Controls.Add(_gridMonitoringButton, 0, 0);
            tableLayoutPanel.Controls.Add(_gridMonitoring, 0, 1);

            _gridMonitoringButton.CellClick += _gridMonitoringButton_CellClick;
            _gridMonitoringButton.DataError += _gridMonitoringButton_DataError;
            _gridMonitoring.DataError += _gridMonitoringButton_DataError;

            _hostMonitoring.Child = tableLayoutPanel;
        }

        private void _gridMonitoringButton_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            
        }

        private DataGridView AddMonitoringButton()
        {
            DataGridView newButtonGrid =
               DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect,
               DataGridViewAutoSizeRowsMode.AllCells);

            newButtonGrid.Dock = DockStyle.Fill;
            newButtonGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            newButtonGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            newButtonGrid.Columns.Add("Col0", "");
            newButtonGrid.Columns.Add("Col1", "");
            newButtonGrid.Columns.Add("Col2", "");
            newButtonGrid.Columns.Add("Col3", "");
            newButtonGrid.Columns.Add("Col4", "");
            //newButtonGrid.Columns.Add("Col5", "");

            foreach (DataGridViewColumn column in newButtonGrid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            DataGridViewRow row = new DataGridViewRow();

            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[0].Value = GetDescription(Status.Start);
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[1].Value = GetDescription(Status.Stop);
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[2].Value = GetDescription(Status.Pause);
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[3].Value = GetDescription(Status.CancelAllOrders);
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[4].Value = GetDescription(Status.CloseOrdersOnMarket);

            newButtonGrid.Rows.Add(row);

            return newButtonGrid;
        }

        private DataGridView AddMonitoringTable()
        {
            DataGridView newTableGrid =
                DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect,
                DataGridViewAutoSizeRowsMode.AllCells);

            newTableGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            newTableGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            newTableGrid.Dock = DockStyle.Fill;

            DataGridViewTextBoxCell cell0 = new DataGridViewTextBoxCell();
            cell0.Style = newTableGrid.DefaultCellStyle;

            newTableGrid.Columns.Add("Col1", "");
            newTableGrid.Columns.Add("Col2", "");

            foreach (DataGridViewColumn column in newTableGrid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                column.ReadOnly = true;
            }

            List<string> listMonitoring = new List<string>()
            {
                "Текущий режим",
                "Торговая пара",
                "Текущая цена (время последней свечи)",
                "Кошелек",
                "Всего средств",
                "Свободно средств",
                "Сумма депозита в работе",
                "Состояние ордеров (выставлено/выполнено)",
                "Нереализованный PNL",                
                "Расчетная цена тейк-профита",
                "Средняя цена позиции",
                "RSI Current",
                "RSI Second",
                "Цена отклонения",
                "Цена ликвидации"
            };

            for (int i = 0; i < listMonitoring.Count; i++)
            {
                DataGridViewRow row = new DataGridViewRow();

                row.Cells.Add(new DataGridViewTextBoxCell());
                row.Cells[0].Value = listMonitoring[i];
                row.Cells.Add(new DataGridViewTextBoxCell());
                newTableGrid.Rows.Add(row);
            }

            return newTableGrid;
        }

        private void AddDataToGrid()
        {
            if (MainWindow.GetDispatcher.CheckAccess() == false)
            {
                MainWindow.GetDispatcher.Invoke(new Action(AddDataToGrid));
                return;
            }

            if (_tab.Portfolio == null)
            {
                return;
            }

            decimal freeMoney = Math.Round(_tab.Portfolio.ValueCurrent - _tab.Portfolio.ValueBlocked, 4);
            decimal wallet = Math.Round(_volume.ValueDecimal * _leverage.ValueDecimal, 4);

            double averagePricePositionMain = 0;
            decimal unrealizPnLMain = 0;
            double priceTPMain = 0;

            if (_main != null)
            {
                averagePricePositionMain = GetAveragePricePositions(_main.SideGrid);
                priceTPMain = GetTpPricePosition(_main, averagePricePositionMain);
                unrealizPnLMain = _main.UnrealizPnL;
            }

            double averagePricePositionHedge = 0;
            decimal unrealizPnLHedge = 0;
            double priceTPHedge = 0;

            if (_hedge != null)
            {
                averagePricePositionHedge = GetAveragePricePositions(_hedge.SideGrid);
                priceTPHedge = GetTpPricePosition(_hedge, averagePricePositionHedge);
                unrealizPnLHedge = _hedge.UnrealizPnL;
            }
            
            if (_typeVolume.ValueString == GetDescription(TypeVolume.Percent))
            {
                wallet = Math.Round(_tab.Portfolio.ValueCurrent * _volume.ValueDecimal / 100 * _leverage.ValueDecimal, 4);
            }

            if (_status != _statusValue)
            {
                _timeStatus = DateTime.Now;
                _statusValue = _status;
            }

            string timeStatus = _timeStatus == DateTime.MinValue ? "" : _timeStatus.ToString();

            _gridMonitoring.Rows[0].Cells[1].Value = GetDescription(_statusValue) + " " + timeStatus; //статус + время
            if (_tab.Security != null)
            {
                _gridMonitoring.Rows[1].Cells[1].Value = _tab.Security.Name; //Торговая пара
            }
            _gridMonitoring.Rows[2].Cells[1].Value = $"{_lastPrice} ({_timeLastPrice})"; //Текущая цена
            _gridMonitoring.Rows[3].Cells[1].Value = wallet; //Кошелек
            _gridMonitoring.Rows[4].Cells[1].Value = Math.Round(_tab.Portfolio.ValueCurrent, 4);//Всего средств
            _gridMonitoring.Rows[5].Cells[1].Value = freeMoney;//Свободно средств
            _gridMonitoring.Rows[6].Cells[1].Value = Math.Round(_tab.Portfolio.ValueBlocked, 4);//Сумма депозита в работе
            _gridMonitoring.Rows[7].Cells[1].Value = "Main: " + _postedOrdersMain + "/" + _completedOrdersMain + ", Hedge: " + _postedOrdersHedge + "/" + _completedOrdersHedge;//Состояние ордеров
            _gridMonitoring.Rows[8].Cells[1].Value = "Main: " + unrealizPnLMain + ", Hedge: " + unrealizPnLHedge;//Нереализованный PNL
            _gridMonitoring.Rows[9].Cells[1].Value = "Main: " + priceTPMain + ", Hedge: " + priceTPHedge; //Цена выставленного тейк-профита
            _gridMonitoring.Rows[10].Cells[1].Value = "Main: " + averagePricePositionMain + ", Hedge: " + averagePricePositionHedge;//Средняя цена позиции
            _gridMonitoring.Rows[11].Cells[1].Value = _lastRsi;//RSI Current
            _gridMonitoring.Rows[12].Cells[1].Value = _lastRsiSecond;//RSI Second
            _gridMonitoring.Rows[13].Cells[1].Value = _deviationPriceMain;
            _gridMonitoring.Rows[14].Cells[1].Value = _liquidationPriceMain;
        }

        private void _gridMonitoringButton_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == 0 && e.RowIndex == 0)
                {
                    PushStartButton();
                }
                if (e.ColumnIndex == 1 && e.RowIndex == 0)
                {
                    PushStopButton();
                }
                if (e.ColumnIndex == 2 && e.RowIndex == 0)
                {
                    PushPauseButton();
                }
                if (e.ColumnIndex == 3 && e.RowIndex == 0)
                {                    
                    CancelAllOrders();
                }
                /*if (e.ColumnIndex == 4 && e.RowIndex == 0)
                {
                    //PushForcedStop();
                }*/
                if (e.ColumnIndex == 4 && e.RowIndex == 0)
                {                    
                    CloseOrdersOnMarket();
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }
        
        private void OrdersMonitoring()
        {
            try
            {
                _postedOrdersMain = 0;
                _completedOrdersMain = 0;
                _postedOrdersHedge = 0;
                _completedOrdersHedge = 0;

                if (_main == null || _hedge == null) return;

                List<Position> listPos = _main.GetListPosition();

                if (listPos.Count > 0)
                {
                    if (listPos[0].OpenOrders.Count > 0)
                    {
                        for (int i = 0; i < listPos[0].OpenOrders.Count; i++)
                        {
                            if (listPos[0].OpenOrders[i].State == OrderStateType.Active ||
                                listPos[0].OpenOrders[i].State == OrderStateType.Done)
                            {
                                _postedOrdersMain++;
                            }
                            if (listPos[0].OpenOrders[i].State == OrderStateType.Done)
                            {
                                _completedOrdersMain++;
                            }
                        }
                    }
                }

                listPos = _hedge.GetListPosition();

                if (listPos.Count > 0)
                {
                    if (listPos[0].OpenOrders.Count > 0)
                    {
                        for (int i = 0; i < listPos[0].OpenOrders.Count; i++)
                        {
                            if (listPos[0].OpenOrders[i].State == OrderStateType.Active ||
                                listPos[0].OpenOrders[i].State == OrderStateType.Done)
                            {
                                _postedOrdersHedge++;
                            }
                            if (listPos[0].OpenOrders[i].State == OrderStateType.Done)
                            {
                                _completedOrdersHedge++;
                            }
                        }
                    }
                }              
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        #endregion

        #region Trade Logic

        private Grid _main;
        private Grid _hedge;
        private decimal _lastPrice;
        private DateTime _timeLastPrice;
        private bool _checkAutoStart = true;
        private bool _rsiBlocked = false;

        private void _tab_CandleUpdateEvent(List<Candle> obj)
        {
            if (obj == null || obj.Count == 0)
            {
                _lastPrice = 0;
            }
            else
            {
                _lastPrice = obj[^1].Close;
                _timeLastPrice = obj[^1].TimeStart;
            }    
        }

        private void _tab_CandleFinishedEvent(List<Candle> obj)
        {
            if (obj == null || obj.Count == 0)
            {
                _lastPrice = 0;
            }
            else
            {
                _lastPrice = obj[^1].Close;
                _timeLastPrice = obj[^1].TimeStart;

                _timeCheckRsi = true;
            }            

            if (_startProgram == StartProgram.IsTester)
            {
                PollingThreadRun();
            }            
        }

        private void PollingThread()
        {
            while (!_deleteBot)
            {
                try
                {
                    PollingThreadRun();
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                    Thread.Sleep(1000);
                }
            }    
        }

        private void PollingThreadRun()
        {
            if (_tab == null || _tab.Portfolio == null) return;
            if (_tab.Connector?.IsReadyToTrade == false) return;
            if (_lastPrice == 0) return;

            if (_rsi.DataSeries != null || _rsiBTC.DataSeries != null)
            {
                _lastRsi = _rsi.DataSeries[0].Last;
                _lastRsiSecond = _rsiBTC.DataSeries[0].Last;
            }

            if (_lastRsi == 0 && _lastRsiSecond == 0) return;

            AutoStart();

            if (_status == Status.NoWork)
            {
                //if (_main != null) _main = null;
                //if (_hedge != null) _hedge = null;

                return;
            }

            if (_main == null) CreateNewGrid(TypeGrid.Main);
            if (_hedge == null) CreateNewGrid(TypeGrid.Hedge);

            GetValueWallet();
            RsiFilter();
            CalcUnrealizePnL();
            LogicMainGrid();
            AutoHedge();
        }

        private void CalcUnrealizePnL()
        {
            if (_main != null)
            {
                List<Position> listPos = _main.GetListPosition();

                if (listPos.Count > 0)
                {
                    var averagePricePositionMain = GetAveragePricePositions(_main.SideGrid);
        
                    decimal volume = listPos[0].OpenVolume;

                    if (_main.SideGrid == Side.Buy)
                    {
                        _main.UnrealizPnL = Math.Round(_lastPrice * volume - (decimal)averagePricePositionMain * volume, 2);
                    }
                    else
                    {
                        _main.UnrealizPnL = Math.Round((decimal)averagePricePositionMain * volume - _lastPrice * volume, 2);
                    }
                }
                else
                {
                    _main.UnrealizPnL = 0;
                }
            }

            if (_hedge != null)
            {
                List<Position> listPos = _hedge.GetListPosition();

                if (listPos.Count > 0)
                {
                    var averagePricePositionHedge = GetAveragePricePositions(_hedge.SideGrid);

                    decimal volume = listPos[0].OpenVolume;
                    decimal unrealizPnLOrderHedge = 0;
                    decimal unrealizPnLHedge = 0;

                    if (_hedge.SideGrid == Side.Buy)
                    {
                        unrealizPnLHedge = Math.Round(_lastPrice * volume - (decimal)averagePricePositionHedge * volume, 2);

                        if (_tabHedge.PositionsOpenAll.Count > 0)
                        {
                            unrealizPnLOrderHedge = Math.Round(_tabHedge.PositionsOpenAll[0].OpenVolume * _lastPrice - _tabHedge.PositionsOpenAll[0].OpenVolume * _tabHedge.PositionsOpenAll[0].EntryPrice, 2);
                        }
                    }
                    else
                    {
                        unrealizPnLHedge = Math.Round((decimal)averagePricePositionHedge * volume - _lastPrice * volume, 2);

                        if (_tabHedge.PositionsOpenAll.Count > 0)
                        {
                            unrealizPnLOrderHedge = Math.Round(_tabHedge.PositionsOpenAll[0].OpenVolume * _tabHedge.PositionsOpenAll[0].EntryPrice - _tabHedge.PositionsOpenAll[0].OpenVolume * _lastPrice, 2);
                        }
                    }

                    unrealizPnLHedge += unrealizPnLOrderHedge;

                    _hedge.UnrealizPnL = unrealizPnLHedge;
                }
                else
                {
                    _hedge.UnrealizPnL = 0;
                }
            }
        }

        private void LogicMainGrid()
        {  
            if (_main.State_PlaceNewGrid) PlaceNewGrid(_main);
            if (_main.State_PlaceNewOrderGrid) PlaceNewOrderFromMainGrid(_main);
            if (_main.State_CheckDoneOpenOrder) CheckDoneOpenOrder(_main);
            if (_main.State_CheckCancelActiveCloseOrder) CheckCancelActiveCloseOrder(_main);
            if (_main.State_PlaceCloseOrder) PlaceCloseOrder(_main);
            if (_main.State_CheckPlaceCloseOrder) CheckPlaceCloseOrder(_main);
            if (_main.State_CheckDoneCloseOrder) CheckDoneCloseOrder(_main);
            if (_main.State_CancelOpenOrders) CancelOpenOrders(_main);
            if (_main.State_CheckCancelOpenOrders) CheckCancelOpenOrders(_main);
            if (_main.State_CheckRearrangeGrid) CheckRearrangeGrid(_main);
            if (_main.State_NeedRearrangeGrid) NeedRearrangeGrid(_main);

            if (_main.State_HasHedgeGrid) CheckHedgeLoss(_main);

            if (_versionDiff)
            {
                if (_main.State_CheckUnrealizedPnL) CheckUnrealizedPnL(_main);
            }

            if (_main.State_CancelActiveOrders) CancelActiveOrders(_main);
            if (_main.State_CheckCancelActiveOrders) CheckCancelActiveOrders(_main);
            if (_main.State_ClosePositionAtMarket) ClosePositionAtMarket(_main);
            if (_main.State_CheckClosePositionAtMarket) CheckClosePositionAtMarket(_main);
        }

        private void CheckUnrealizedPnL(Grid grid)
        {
            decimal checkValue = _setValueUnrealizePnLToClose.ValueInt;

            if (_main.UnrealizPnL > checkValue) return;

            SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Unrealized PnL больше {checkValue}, Отменяем все ордера и закрываем позицию.", Logging.LogMessageType.User);

            SetZeroFlagsGrid(_hedge);
            _autoHedgeState = AutoHedgeState.Disable;
            SendLogFlagsGrid(_hedge);

            SetZeroFlagsGrid(grid);
            grid.State_CancelActiveOrders = true;
            grid.State_CheckUnrealizedPnL = false;
            SendLogFlagsGrid(grid);

            SaveRecovery();
        }

        private void CheckHedgeLoss(Grid grid)
        {
            if (_autoHedgeState != AutoHedgeState.ActiveHedgeGrid) return;

            decimal hedgeLoss = _setHedgeLoss.ValueDecimal;

            if (_setHedgeTypeLoss.ValueString == GetDescription(TypeVolume.Percent))
            {
                hedgeLoss = (decimal)grid.AveragePricePosition * _setHedgeLoss.ValueDecimal / 100;
            }

            if ((_main.UnrealizPnL + _hedge.UnrealizPnL) < hedgeLoss) return;

            SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Доход от Main сетки: {_main.UnrealizPnL}, доход от Хедж сетки: {_hedge.UnrealizPnL}, HedgeLoss: {hedgeLoss}, Отменяем все ордера и закрываем позицию.", Logging.LogMessageType.User);

            SetZeroFlagsGrid(_hedge);
            _autoHedgeState = AutoHedgeState.Disable;
            SendLogFlagsGrid(_hedge);

            SetZeroFlagsGrid(grid);            
            grid.State_CancelActiveOrders = true;
            grid.State_HasHedgeGrid = true;
            SendLogFlagsGrid(grid);

            SaveRecovery();
        }

        private void CreateNewGrid(TypeGrid type)
        {
            if (type == TypeGrid.Main)
            {
                Side side = _regime == GetDescription(Regime.Long) ? Side.Buy : Side.Sell;
                _main = new(TypeGrid.Main, side, _tab);

                SendNewLogMessage($"[{_main.TypeGrid}, {_main.SideGrid}] Создаем новый объект сетки", Logging.LogMessageType.User);                
            }

            if (type == TypeGrid.Hedge)
            {
                Side side = _regime == GetDescription(Regime.Long) ? Side.Sell : Side.Buy;
                _hedge = new(TypeGrid.Hedge, side, _tab);

                SendNewLogMessage($"[{_hedge.TypeGrid}, {_hedge.SideGrid}] Создаем новый объект хеджирующей сетки", Logging.LogMessageType.User);
            }
        }

        private void PlaceNewGrid(Grid grid)
        {
            if (_status != Status.Work) return;

            if (grid.TimeCycle > DateTime.Now) return;
                                    
            if (_loadGrig.ValueBool)
            {
                grid.ListOrders = LoadSampleToGrid(grid.SideGrid);
            }
            else
            {
                if (grid.TypeGrid == TypeGrid.Main)
                {
                    grid.ListOrders = CalculateGrid(grid.SideGrid);
                    SendSettingsInLog();
                }
                else
                {
                    grid.ListOrders = CalculateHedgeGrid(grid.SideGrid);

                    if (grid.ListOrders == null || grid.ListOrders.Count == 0)
                    {
                        //_setRegimeAutoHedge.ValueString = RegimeOnOff.Off.ToString();
                        _main.State_HasHedgeGrid = false;
                        _autoHedgeState = AutoHedgeState.Disable;
                        SendLogFlagsGrid(grid);
                        SaveRecovery();

                        SendNewLogMessage("Автохедж выключен. Для запуска измените настройки хедж сетки и нажмите кнопку \"Обновить\"", Logging.LogMessageType.Error);
                    }
                }
            }

            WriteLogOrders(grid);
            SendOrder(grid);             

            grid.State_PlaceNewGrid = false;
            grid.State_PlaceNewOrderGrid = true;
            grid.State_CheckDoneOpenOrder = true;
            grid.State_CheckRearrangeGrid = true;

            grid.State_CheckUnrealizedPnL = true;

            grid.PriceTP = 0;
            grid.AveragePricePosition = 0;
            grid.UnrealizPnL = 0;

            if (grid.TypeGrid == TypeGrid.Main) _autoHedgeState = AutoHedgeState.Monitoring;

            SendLogFlagsGrid(grid);
            SaveRecovery();
        }

        private void SendOrder(Grid grid)
        {           
            int count = 1;

            for (int i = 0; i < grid.ListOrders.Count; i++)
            {
                if (count > _countOrdersOnExchange.ValueInt)
                {
                    break;
                }

                SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Ставим новый ордер из сетки: " + grid.ListOrders[i].Price + " - " + grid.ListOrders[i].Volume, Logging.LogMessageType.User);

                if (grid.GetListPosition().Count == 0)
                {
                    if (grid.SideGrid == Side.Buy)
                    {
                        _tab.BuyAtLimit(grid.ListOrders[i].Volume, grid.ListOrders[i].Price);
                    }
                    else
                    {
                        _tab.SellAtLimit(grid.ListOrders[i].Volume, grid.ListOrders[i].Price);
                    }
                }
                else
                {
                    if (grid.SideGrid == Side.Buy)
                    {
                        _tab.BuyAtLimitToPositionUnsafe(grid.GetPosition(), grid.ListOrders[i].Price, grid.ListOrders[i].Volume);
                    }
                    else
                    {
                        _tab.SellAtLimitToPositionUnsafe(grid.GetPosition(), grid.ListOrders[i].Price, grid.ListOrders[i].Volume);
                    }
                }

                grid.ListOrders.RemoveAt(i);
                i--;
                count++;

                if (_boolTimerTwapOrders)
                {
                    grid.TimerTwapOrders = DateTime.UtcNow;
                    SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Установлено время TWAP ордера: {grid.TimerTwapOrders}, следующий ордер будет выставлен после {grid.TimerTwapOrders.AddMinutes((double)_setTimeTwapOrders.ValueDecimal)}", Logging.LogMessageType.User);
                    break;
                }
            }
        }

        private void PlaceNewOrderFromMainGrid(Grid grid)
        {
            if (_status != Status.Work && _status != Status.Stop) return;

            if (_boolTimerTwapOrders)
            {
                if (grid.TimerTwapOrders.AddMinutes((double)_setTimeTwapOrders.ValueDecimal) > DateTime.UtcNow) return;
            }

            if (grid.ListOrders.Count == 0) return;

            if (grid.GetListPosition().Count == 0) return;

            Position pos = grid.GetPosition();

            int count = 0;

            for (int i = 0; i < pos.OpenOrders.Count; i++)
            {
                if (pos.OpenOrders[i].State == OrderStateType.Active ||
                    pos.OpenOrders[i].State == OrderStateType.None ||
                    pos.OpenOrders[i].State == OrderStateType.Pending ||
                    pos.OpenOrders[i].State == OrderStateType.Partial
                    )
                {
                    count++;
                }
            }

            if (count < _countOrdersOnExchange.ValueInt)
            {
                SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Добавляем ордер на биржу из сетки: " + grid.ListOrders[0].Price + " - " + grid.ListOrders[0].Volume, Logging.LogMessageType.User);

                if (grid.SideGrid == Side.Buy)
                {
                    _tab.BuyAtLimitToPositionUnsafe(pos, grid.ListOrders[0].Price, grid.ListOrders[0].Volume);
                }
                else
                {
                    _tab.SellAtLimitToPositionUnsafe(pos, grid.ListOrders[0].Price, grid.ListOrders[0].Volume);
                }

                grid.ListOrders.RemoveAt(0);

                if (_boolTimerTwapOrders)
                {
                    grid.TimerTwapOrders = DateTime.UtcNow;
                    SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Выставлен TWAP ордер: {grid.TimerTwapOrders}, следующий ордер будет выставлен после {grid.TimerTwapOrders.AddMinutes((double)_setTimeTwapOrders.ValueDecimal)}", Logging.LogMessageType.User);
                }
            }

            SaveRecovery();
        }

        private void CheckDoneOpenOrder(Grid grid)
        {
            if (_status != Status.Work && _status != Status.Stop) return;

            if (grid.GetListPosition().Count == 0) return;

            Position pos = grid.GetPosition();

            decimal volume = Math.Round(pos.OpenVolume, _tab.Security.DecimalsVolume, MidpointRounding.ToNegativeInfinity);

            if (volume <= 0) return;

            if (grid.AveragePricePosition == GetAveragePricePositions(grid.SideGrid))
            {
                return;
            }
            else
            {
                grid.AveragePricePosition = GetAveragePricePositions(grid.SideGrid);
            }

            if (pos.CloseOrders != null &&
                pos.CloseOrders.Count > 0 &&
                pos.CloseOrders[^1].State != OrderStateType.Done &&
                pos.CloseOrders[^1].State != OrderStateType.Cancel)
            {                
                if (pos.OpenVolume > pos.CloseOrders[^1].Volume)
                {
                    SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Исполнен открывающий ордер", Logging.LogMessageType.User);                    
                    
                    CancelActiveCloseOrders(pos);

                    SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Отменяем все закрывающие ордера", Logging.LogMessageType.User);

                    grid.State_CheckCancelActiveCloseOrder = true;
                    grid.State_CheckDoneOpenOrder = false;

                    if (grid.State_HasHedgeGrid) // разрешение на подтяжку хедж сетки
                    {
                        if (_hedge.GetListPosition().Count > 0 && _hedge.GetListPosition()[0].OpenVolume > 0)
                        {
                        }
                        else
                        {
                            _hedge.State_NeedReCalcGrid = true;
                            grid.State_CheckCancelActiveCloseOrder = false;
                            grid.State_CheckDoneOpenOrder = true;
                        }
                    }

                    SendLogFlagsGrid(grid);
                    SaveRecovery();
                }
            }
            else
            {
                SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Исполнен открывающий ордер", Logging.LogMessageType.User);
                grid.State_PlaceCloseOrder = true;

                SendLogFlagsGrid(grid);
                SaveRecovery();
            }

            GetPriceCloseOrder(grid);

            if (grid.TypeGrid == TypeGrid.Hedge && grid.GetListPosition().Count > 0 && grid.ListOrders.Count == 0 && !grid.GetPosition().OpenActive)
            {
                SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Все ордера из хедж сетки исполнены. Отменяем все ордера и закрываем позицию.", Logging.LogMessageType.User);
                
                SetZeroFlagsGrid(grid);
                SetZeroFlagsGrid(_main);
                _autoHedgeState = AutoHedgeState.Disable;

                SendLogFlagsGrid(grid);
                _main.State_CancelActiveOrders = true;

                SendLogFlagsGrid(_main);
                SaveRecovery();
            }
        }

        private void GetPriceCloseOrder(Grid grid)
        {
            grid.AveragePricePosition = GetAveragePricePositions(grid.SideGrid);

            double profit = (double)_profit.ValueDecimal;

            if (grid.TypeGrid == TypeGrid.Hedge)
            {
                profit = (double)_profitHedge.ValueDecimal;
            }

            if (grid.SideGrid == Side.Buy)
            {
                grid.PriceTP = Math.Round((grid.AveragePricePosition + grid.AveragePricePosition * profit / 100) / (double)_tab.Security.PriceStep * (double)_tab.Security.PriceStep, _tab.Security.Decimals, MidpointRounding.AwayFromZero);
            }
            else
            {
                grid.PriceTP = Math.Round((grid.AveragePricePosition - grid.AveragePricePosition * profit / 100) / (double)_tab.Security.PriceStep * (double)_tab.Security.PriceStep, _tab.Security.Decimals, MidpointRounding.AwayFromZero);
            }

            SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Цена TP ордера должен быть: " + grid.PriceTP, Logging.LogMessageType.User);
        }

        private void CheckCancelActiveCloseOrder(Grid grid)
        {
            if (_status != Status.Work && _status != Status.Stop) return;

            if (!grid.GetPosition().CloseActive)
            {
                grid.State_CheckCancelActiveCloseOrder = false;
                grid.State_PlaceCloseOrder = true;

                if (grid.TypeGrid == TypeGrid.Main && grid.State_HasHedgeGrid)
                {
                    grid.State_PlaceCloseOrder = false;
                    grid.State_CheckDoneOpenOrder = true;
                }

                SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Все закрывающие ордера отменены", Logging.LogMessageType.User);

                SendLogFlagsGrid(grid);
                SaveRecovery();
            }            
        }

        private void PlaceCloseOrder(Grid grid)
        {
            if (_status != Status.Work && _status != Status.Stop) return;

            //grid.AveragePricePosition = GetAveragePricePositions(grid.SideGrid);

            if (grid.TypeGrid == TypeGrid.Main && grid.State_HasHedgeGrid) return;

            if (grid.AveragePricePosition == 0) return;

            decimal volume = grid.GetPosition().OpenVolume;
                        
            SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Ставим новый закрывающий ордер: " + grid.PriceTP + " - " + volume, Logging.LogMessageType.User);

            _tab.CloseAtLimitUnsafe(grid.GetPosition(), (decimal)grid.PriceTP, volume);

            grid.State_PlaceCloseOrder = false;
            grid.State_CheckDoneOpenOrder = false;
            grid.State_CheckPlaceCloseOrder = true;

            SendLogFlagsGrid(grid);
            SaveRecovery();
        }

        private void CheckPlaceCloseOrder(Grid grid)
        {
            if (_status != Status.Work && _status != Status.Stop) return;

            List<Position> listPos = grid.GetListPosition();

            if (listPos.Count == 0)
            {
                grid.State_CheckPlaceCloseOrder = false;
                return;
            }

            Position pos = grid.GetPosition();

            if (pos.CloseActive && pos.CloseOrders[^1].State == OrderStateType.Active)
            {
                grid.State_CheckDoneOpenOrder = true;
                grid.State_CheckPlaceCloseOrder = false;
                grid.State_CheckDoneCloseOrder = true;

                SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Закрывающий ордер выставлен", Logging.LogMessageType.User);

                SendLogFlagsGrid(grid);
                SaveRecovery();
            }
        }

        private void CheckDoneCloseOrder(Grid grid)
        {
            if (_status != Status.Work && _status != Status.Stop) return;

            if (grid.GetListPosition().Count == 0)
            {
                CloseOrderIsDone(grid);    
                return;
            }

            Position pos = grid.GetPosition();

            if (pos.CloseOrders != null &&
                pos.CloseOrders.Count > 0 &&
                pos.CloseOrders[^1].State == OrderStateType.Done)
            {                
                CloseOrderIsDone(grid);
            }
        }

        private void CloseOrderIsDone(Grid grid)
        {
            grid.State_CheckDoneCloseOrder = false;
            grid.State_CancelOpenOrders = true;
            grid.State_PlaceNewOrderGrid = false;
            grid.State_NeedRearrangeGrid = false;
            grid.State_CheckRearrangeGrid = false;
            grid.State_NeedReCalcGrid = false;

            if (_rsiBlocked && grid.TypeGrid == TypeGrid.Main)
            {
                SetZeroFlagsGrid(grid);
            }

            SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Закрывающий ордер исполнен.", Logging.LogMessageType.User);

            SendLogFlagsGrid(grid);
            SaveRecovery();
        }

        private void CancelOpenOrders(Grid grid)
        {
            if (_status != Status.Work && _status != Status.Stop) return;

            if (grid.GetListPosition().Count == 0)
            {
                CancelOrderIsDone(grid);
                return;
            }

            Position pos = grid.GetPosition();

            if (pos.OpenActive)
            {
                for (int i = 0; i < pos.OpenOrders.Count; i++)
                {
                    if (pos.OpenOrders[i].State == OrderStateType.Active ||
                        pos.OpenOrders[i].State == OrderStateType.Pending ||
                        pos.OpenOrders[i].State == OrderStateType.None ||
                        pos.OpenOrders[i].State == OrderStateType.Partial)
                    {
                        _tab.CloseOrder(pos.OpenOrders[i]);
                    }
                }
            }

            CancelOrderIsDone(grid);
        }

        private void CancelOrderIsDone(Grid grid)
        {
            SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Отменяем открывающие ордера", Logging.LogMessageType.User);

            grid.State_CancelOpenOrders = false;
            grid.State_CheckCancelOpenOrders = true;
            grid.State_PlaceNewOrderGrid = false;
            grid.State_NeedRearrangeGrid = false;
            grid.State_CheckRearrangeGrid = false;

            SendLogFlagsGrid(grid);
            SaveRecovery();
        }

        private void CheckCancelOpenOrders(Grid grid)
        {
            if (_status != Status.Work && 
                _status != Status.Stop && 
                _status != Status.CloseOrdersOnMarket)
            {
                return;
            }

            bool needDone = false;

            List<Position> listPos = grid.GetListPosition();

            if (listPos.Count == 0)
            {
                needDone = true;
            }
            else
            {
                if (grid.GetPosition().OpenActive)
                {
                    return;
                }
                else
                {
                    needDone = true;
                }
            }

            if (needDone)
            {
                SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Нужно пересоздать сетку", Logging.LogMessageType.User);

                SetZeroFlagsGrid(grid);

                if (_status == Status.CloseOrdersOnMarket)
                {
                    SendNewLogMessage($"Статус: CloseOrdersOnMarket. Все ордера сняты, позиция закрыта. Запускаем новую сетку", Logging.LogMessageType.User);
                    _status = Status.Work;
                }

                if (_status == Status.Work)
                {
                    grid.State_PlaceNewGrid = true;
                    grid.PriceTP = 0;
                    grid.AveragePricePosition = 0;
                    grid.UnrealizPnL = 0;
                    _timeCheckRsi = true;
                    _boolRsiBlocked = false;
                    _boolRsiUnblocked = false;

                    if (grid.TypeGrid == TypeGrid.Hedge && _tabHedge.PositionsOpenAll.Count > 0)
                    {
                        if (_hedge.SideGrid == Side.Buy)
                        {
                            grid.UnrealizPnL = Math.Round(_tabHedge.PositionsOpenAll[0].OpenVolume * _lastPrice - _tabHedge.PositionsOpenAll[0].OpenVolume * _tabHedge.PositionsOpenAll[0].EntryPrice, 2);                            
                        }
                        else
                        {
                            grid.UnrealizPnL = Math.Round(_tabHedge.PositionsOpenAll[0].OpenVolume * _tabHedge.PositionsOpenAll[0].EntryPrice - _tabHedge.PositionsOpenAll[0].OpenVolume * _lastPrice, 2);                            
                        }
                    }

                    grid.TimeCycle = DateTime.Now.AddMinutes((double)_delayAfterCycle.ValueDecimal);

                    SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Новая сетка запустится после {grid.TimeCycle}", Logging.LogMessageType.User);
                }

                if (_status == Status.Stop)
                {
                    SendNewLogMessage($"Была включена остановка. Цикл закончен и больше перезапускаться не будет", Logging.LogMessageType.User);
                    _status = Status.NoWork;
                }

                SendLogFlagsGrid(grid);
                SaveRecovery();
            }
        }

        private void CheckRearrangeGrid(Grid grid)
        {
            if (_status != Status.Work && _status != Status.Stop) return;
            //if (grid.TypeGrid == TypeGrid.Hedge) return;

            if (grid.GetListPosition().Count == 0) return;

            Position pos = grid.GetPosition();

            if (pos.CloseActive) return;
            if (grid.State_HasHedgeGrid) return;

            decimal priceOrder = 0;

            for (int i = 0; i < pos.OpenOrders.Count; i++)
            {
                if (pos.OpenOrders[i].State == OrderStateType.None)
                {
                    return;
                }

                if (i == 0)
                {
                    priceOrder = pos.OpenOrders[i].Price;
                }
                else
                {
                    if (grid.SideGrid == Side.Buy)
                    {
                        if (priceOrder < pos.OpenOrders[i].Price)
                        {
                            priceOrder = pos.OpenOrders[i].Price;
                        }
                    }
                    else
                    {
                        if (priceOrder > pos.OpenOrders[i].Price)
                        {
                            priceOrder = pos.OpenOrders[i].Price;
                        }
                    }
                }
            }

            if (priceOrder == 0) return;

            bool needRearrange = false;            

            if (grid.SideGrid == Side.Buy)
            {
                if (_lastPrice > priceOrder + priceOrder * _rearrangeGrid.ValueDecimal / 100) // проверяем ушла ли цена для подтяжки сетки
                {
                    if (grid.State_NeedRearrangeGrid) return;
                    needRearrange = true;
                }
                else
                {
                    if (!grid.State_NeedRearrangeGrid) return;
                    needRearrange = false;                    
                }
            }
            else
            {
                if (_lastPrice < priceOrder - priceOrder * _rearrangeGrid.ValueDecimal / 100)
                {
                    if (grid.State_NeedRearrangeGrid) return;
                    needRearrange = true;
                }
                else
                {
                    if (!grid.State_NeedRearrangeGrid) return;
                    needRearrange = false;
                }
            }

            if (needRearrange)
            {
                SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Требуется подтяжка сетки", Logging.LogMessageType.User);

                grid.State_NeedRearrangeGrid = true;          
                grid.TimeRearrangeGrid = DateTime.Now.AddMinutes((double)_delayRearrange.ValueDecimal);

                if (grid.TypeGrid == TypeGrid.Hedge)
                {
                    grid.State_NeedReCalcGrid = true;
                }

                SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Следующая подтяжка сетки будет после {grid.TimeRearrangeGrid}", Logging.LogMessageType.User);

                SendLogFlagsGrid(grid);
                SaveRecovery();
            }
            else
            {
                SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Подтяжка сетки не требуется", Logging.LogMessageType.User);
                grid.State_NeedRearrangeGrid = false;

                if (grid.TypeGrid == TypeGrid.Hedge) grid.State_NeedReCalcGrid = false;

                SendLogFlagsGrid(grid);
                SaveRecovery();
            }
        }

        private void NeedRearrangeGrid(Grid grid)
        {
            if (_status != Status.Work && _status != Status.Stop) return;
            if (grid.TypeGrid == TypeGrid.Hedge) return;

            if (grid.TimeRearrangeGrid > DateTime.Now) return;

            if (grid.GetPosition().OpenOrders.Count > 0)
            {
                grid.State_NeedRearrangeGrid = false;
            }

            SendNewLogMessage($"[{grid.TypeGrid}, {grid.SideGrid}] Подтягиваем сетку", Logging.LogMessageType.User);
            
            grid.State_CancelOpenOrders = true;
            grid.State_CheckRearrangeGrid = false;            

            SendLogFlagsGrid(grid);

            SaveRecovery();
        }

        private void CancelActiveOrders(Grid grid)
        {
            for (int i = 0; i < _tab.PositionsOpenAll.Count; i++)
            {
                _tab.CloseAllOrderToPosition(_tab.PositionsOpenAll[i]);
            }

            if (_tabHedge != null && _tabHedge.PositionsOpenAll.Count > 0)
            {
                for (int i = 0; i < _tabHedge.PositionsOpenAll.Count; i++)
                {
                    _tabHedge.CloseAllOrderToPosition(_tabHedge.PositionsOpenAll[i]);
                }
            }

            grid.State_CancelActiveOrders = false;
            grid.State_CheckCancelActiveOrders = true;

            _main.ListOrders.Clear();
            _hedge.ListOrders.Clear();

            SendNewLogMessage("grid.State_CheckCancelActiveOrders = true;", Logging.LogMessageType.User);
        }

        private void CheckCancelActiveOrders(Grid grid)
        {
            for (int i = 0; i < _tab.PositionsOpenAll.Count; i++)
            {
                if (_tab.PositionsOpenAll[i].OpenActive) return;
                if (_tab.PositionsOpenAll[i].CloseActive) return;
            }

            if (_tabHedge != null && _tabHedge.PositionsOpenAll.Count > 0)
            {
                for (int i = 0; i < _tabHedge.PositionsOpenAll.Count; i++)
                {
                    if (_tabHedge.PositionsOpenAll[i].OpenActive) return;
                    if (_tabHedge.PositionsOpenAll[i].CloseActive) return;
                }
            }

            grid.State_CheckCancelActiveOrders = false;
            grid.State_ClosePositionAtMarket = true;

            SendNewLogMessage("grid.State_ClosePositionAtMarket = true;", Logging.LogMessageType.User);
        }

        private void ClosePositionAtMarket(Grid grid)
        {
            for (int i = 0; i < _tab.PositionsOpenAll.Count; i++)
            {
                /*decimal price = _lastPrice + _tab.Security.PriceStep * 100;

                if (_tab.PositionsOpenAll[i].Direction == Side.Buy)
                {
                    price = _lastPrice - _tab.Security.PriceStep * 100;
                }

                _tab.CloseAtLimit(_tab.PositionsOpenAll[i], price, _tab.PositionsOpenAll[i].OpenVolume);
                SendNewLogMessage($"Закрываем ордер {price} - {_tab.PositionsOpenAll[i].OpenVolume}", Logging.LogMessageType.User);*/

                _tab.CloseAtMarket(_tab.PositionsOpenAll[i], _tab.PositionsOpenAll[i].OpenVolume);
                SendNewLogMessage($"Закрываем ордер {grid.SideGrid} - {_tab.PositionsOpenAll[i].OpenVolume}", Logging.LogMessageType.User);
            }

            grid.State_ClosePositionAtMarket = false;
            grid.State_CheckClosePositionAtMarket = true;

            for (int i = 0; i < _tabHedge.PositionsOpenAll.Count; i++)
            {              
                _tabHedge.CloseAtMarket(_tabHedge.PositionsOpenAll[i], _tabHedge.PositionsOpenAll[i].OpenVolume);
                SendNewLogMessage($"Закрываем ордер на хедж вкладке - {_tabHedge.PositionsOpenAll[i].OpenVolume}", Logging.LogMessageType.User);
            }

            SendNewLogMessage("grid.State_CheckClosePositionAtMarket = true;", Logging.LogMessageType.User);
        }

        private void CheckClosePositionAtMarket(Grid grid)
        {
            for (int i = 0; i < _tab.PositionsOpenAll.Count; i++)
            {
                if (_tab.PositionsOpenAll[i].OpenVolume > 0) return;
            }

            for (int i = 0; i < _tabHedge.PositionsOpenAll.Count; i++)
            {
                if (_tabHedge.PositionsOpenAll[i].OpenVolume > 0) return;
            }

            grid.State_CheckClosePositionAtMarket = false;

            SetZeroFlagsGrid(grid);

            grid.State_CheckCancelOpenOrders = true;     
            
            if (_status == Status.CloseOrdersOnMarket)
            {
                SetZeroFlagsGrid(grid);
                _status = Status.NoWork;
            }
        }

        private void SendLogFlagsGrid(Grid grid)
        {
            if (grid == null) return;

            string str = $"[{grid.TypeGrid}, {grid.SideGrid}]: ";
            str += $"State_PlaceNewGrid = " + grid.State_PlaceNewGrid + ", ";
            str += $"State_PlaceNewOrderGrid = " + grid.State_PlaceNewOrderGrid + ", ";
            str += $"State_CheckDoneOpenOrder = " + grid.State_CheckDoneOpenOrder + ", ";
            str += $"State_CheckDoneCloseOrder = " + grid.State_CheckDoneCloseOrder + ", ";
            str += $"State_CheckCancelActiveCloseOrder = " + grid.State_CheckCancelActiveCloseOrder + ", ";
            str += $"State_PlaceCloseOrder = " + grid.State_PlaceCloseOrder + ", ";
            str += $"State_CheckPlaceCloseOrder = " + grid.State_CheckPlaceCloseOrder + ", ";
            str += $"State_CancelOpenOrders = " + grid.State_CancelOpenOrders + ", ";
            str += $"State_CheckCancelOpenOrders = " + grid.State_CheckCancelOpenOrders + ", ";
            str += $"State_CheckRearrangeGrid = " + grid.State_CheckRearrangeGrid + ", ";
            str += $"State_NeedRearrangeGrid = " + grid.State_NeedRearrangeGrid + ", ";
            str += $"State_HasHedgeGrid = " + grid.State_HasHedgeGrid + ", ";
            str += $"State_NeedReCalcGrid = " + grid.State_NeedReCalcGrid + ", ";
            str += $"State_ClosePositionAtMarket = " + grid.State_ClosePositionAtMarket + ", ";
            str += $"State_CheckClosePositionAtMarket = " + grid.State_CheckClosePositionAtMarket + ", ";
            str += $"State_CancelActiveOrders = " + grid.State_CancelActiveOrders + ", ";
            str += $"State_CheckCancelActiveOrders = " + grid.State_CheckCancelActiveOrders;

            SendNewLogMessage(str, Logging.LogMessageType.User);
        }

        private void SetZeroFlagsGrid(Grid grid)
        {
            if (grid == null) return;

            grid.State_PlaceNewGrid = false;
            grid.State_PlaceNewOrderGrid = false;
            grid.State_CheckDoneOpenOrder = false;
            grid.State_CheckDoneCloseOrder = false;
            grid.State_CheckCancelActiveCloseOrder = false;
            grid.State_PlaceCloseOrder = false;
            grid.State_CancelOpenOrders = false;
            grid.State_CheckCancelOpenOrders = false;
            grid.State_CheckRearrangeGrid = false;
            grid.State_HasHedgeGrid = false;
            grid.State_NeedReCalcGrid = false;
            grid.State_ClosePositionAtMarket = false;
            grid.State_CheckClosePositionAtMarket = false;
            grid.State_CancelActiveOrders = false;
            grid.State_CheckCancelActiveOrders = false;            
        }

        private void SendSettingsInLog()
        {
            string str = "Settings: ";

            str += $"Перекрытие изменения цены, %  - {_сoveringPriceChanges.ValueDecimal}, ";
            str += $"Сетка ордеров - {_countOrdersGrid.ValueInt}, ";
            str += $"Процент мартингейла, % - {_percentMartingale.ValueDecimal}, ";
            str += $"Отступ первого ордера, % - {_firstOrderIndent.ValueDecimal}, ";
            str += $"Профит, % - {_profit.ValueDecimal}, ";
            str += $"Подтяжка сетки ордеров, % - {_rearrangeGrid.ValueDecimal}, ";
            str += $"Частичное выставление - {_countOrdersOnExchange.ValueInt}, ";
            str += $"Задержка перед отменой сетки ордеров для подтяжки, мин - {_delayRearrange.ValueDecimal}, ";
            str += $"Задержка после завершения цикла, мин - {_delayAfterCycle.ValueDecimal}, ";
            str += $"Логарифмическое распределение уровней цен - {_logPriceLevels.ValueDecimal}, ";
            str += $"Минимальный объем по инструменту - {_minVolumeForTicker.ValueDecimal}, ";
            str += $"Включить TWAP ордера - {_boolTimerTwapOrders.ValueBool}, ";
            str += $"Задержка выставления TWAP ордеров - {_setTimeTwapOrders.ValueDecimal}, ";

            str += $"Фильтр RSI - {_rsiOnOff.ValueString}, ";
            str += $"Период RSI - {_periodRsi.ValueInt}, ";
            str += $"Выбор по - {_choiseRSI.ValueString}, ";
            str += $"Условие фильтрации - {_operationRSI.ValueString}, ";
            str += $"Значение RSI - {_valueRsi.ValueInt}, ";

            str += $"Вкл/выкл Автохедж - {_setRegimeAutoHedge.ValueString}, ";
            str += $"Тип лота хеджирования - {_setTypeLotHedge.ValueString}, ";
            str += $"Лот хеджирования - {_setLotHedge.ValueDecimal}, ";
            str += $"Тип допустимого убытка - {_setHedgeTypeLoss.ValueString}, ";
            str += $"Допустимый убыток - {_setHedgeLoss.ValueDecimal}, ";
            str += $"Зона активации: % от цены основной позиции - {_setZoneActivMainPriceBool.ValueBool}, ";
            str += $"% от цены основной позиции - {_setZoneActivMainPriceValue.ValueDecimal}, ";
            str += $"Зона активации: % от просадки депозита - {_setZoneActivDepositLossBool.ValueBool}, ";
            str += $"% от просадки депозита - {_setZoneActivDepositLossValue.ValueDecimal}, ";
            str += $"Зона активации: диапазон ликвидации - {_setZoneActivRangeLiquidationBool.ValueBool}, ";
            str += $"Диапазон ликвидации, % от рыночной цены - {_setRangeLiquidationValue.ValueDecimal}, ";
            str += $"Смещение последнего ордера, % - {_setOffsetLastOrder.ValueDecimal}, ";
            str += $"Сетка ордеров хедж сетки - {_countOrdersGridHedge.ValueInt}, ";
            str += $"Процент мартингейла хедж сетки, % - {_percentMartingaleHedge.ValueDecimal}, ";
            str += $"Логарифмическое распределение уровней цен хедж сетки - {_logPriceLevelsHedge.ValueDecimal}, ";
            str += $"Профит хедж сетки, % - {_profitHedge.ValueDecimal}, ";

            SendNewLogMessage(str, Logging.LogMessageType.User);
        }

        private void SaveRecovery()
        {
            try
            {
                if (_startProgram == StartProgram.IsTester) return;

                string fileName = @"Engine\" + NameStrategyUniq + @"Recovery.json";

                Dictionary<string, dynamic> recoveryData = new();

                recoveryData.Add("Status", _status.ToString());

                if (_main != null)
                {
                    recoveryData.Add("Side", _main.SideGrid.ToString());
                    recoveryData.Add("StartDepositValue", _startDepositValue.ToString());
                    recoveryData.Add("AutoHedgeState", _autoHedgeState.ToString());

                    Dictionary<string, dynamic> data = new();

                    List<ListOrders> list = _main.ListOrders;

                    List<object> ordersArray = new();

                    if (list.Count > 0)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            ordersArray.Add(new { list[i].Price, list[i].Volume });
                        }                        
                    }

                    data.Add("ListOrders", ordersArray);

                    data.Add("State_PlaceNewGrid", _main.State_PlaceNewGrid);
                    data.Add("State_PlaceNewOrderGrid", _main.State_PlaceNewOrderGrid);
                    data.Add("State_CheckDoneOpenOrder", _main.State_CheckDoneOpenOrder);
                    data.Add("State_CheckDoneCloseOrder", _main.State_CheckDoneCloseOrder);
                    data.Add("State_CheckCancelActiveCloseOrder", _main.State_CheckCancelActiveCloseOrder);
                    data.Add("State_PlaceCloseOrder", _main.State_PlaceCloseOrder);
                    data.Add("State_CheckPlaceCloseOrder", _main.State_CheckPlaceCloseOrder);
                    data.Add("State_CancelOpenOrders", _main.State_CancelOpenOrders);
                    data.Add("State_CheckCancelOpenOrders", _main.State_CheckCancelOpenOrders);
                    data.Add("State_CheckRearrangeGrid", _main.State_CheckRearrangeGrid);
                    data.Add("State_NeedRearrangeGrid", _main.State_NeedRearrangeGrid);
                    data.Add("State_HasHedgeGrid", _main.State_HasHedgeGrid);
                    data.Add("State_ClosePositionAtMarket", _main.State_ClosePositionAtMarket);
                    data.Add("State_CheckClosePositionAtMarket", _main.State_CheckClosePositionAtMarket);
                    data.Add("State_CancelActiveOrders", _main.State_CancelActiveOrders);
                    data.Add("State_CheckCancelActiveOrders", _main.State_CheckCancelActiveOrders);
                    data.Add("State_CheckUnrealizedPnL", _main.State_CheckUnrealizedPnL);

                    recoveryData.Add("Main", data);                    
                }

                if (_hedge != null)
                {
                    Dictionary<string, dynamic> data = new();

                    List<ListOrders> list = _hedge.ListOrders;

                    List<object> ordersArray = new();

                    if (list.Count > 0)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            ordersArray.Add(new { list[i].Price, list[i].Volume });
                        }
                    }

                    data.Add("ListOrders", ordersArray);

                    data.Add("State_PlaceNewGrid", _hedge.State_PlaceNewGrid);
                    data.Add("State_PlaceNewOrderGrid", _hedge.State_PlaceNewOrderGrid);
                    data.Add("State_CheckDoneOpenOrder", _hedge.State_CheckDoneOpenOrder);
                    data.Add("State_CheckDoneCloseOrder", _hedge.State_CheckDoneCloseOrder);
                    data.Add("State_CheckCancelActiveCloseOrder", _hedge.State_CheckCancelActiveCloseOrder);
                    data.Add("State_PlaceCloseOrder", _hedge.State_PlaceCloseOrder);
                    data.Add("State_CheckPlaceCloseOrder", _hedge.State_CheckPlaceCloseOrder);
                    data.Add("State_CancelOpenOrders", _hedge.State_CancelOpenOrders);
                    data.Add("State_CheckCancelOpenOrders", _hedge.State_CheckCancelOpenOrders);
                    data.Add("State_CheckRearrangeGrid", _hedge.State_CheckRearrangeGrid);
                    data.Add("State_NeedRearrangeGrid", _hedge.State_NeedRearrangeGrid);
                    data.Add("State_NeedReCalcGrid", _hedge.State_NeedReCalcGrid);

                    recoveryData.Add("Hedge", data);
                }

                string json = JsonConvert.SerializeObject(recoveryData, Formatting.Indented);
                File.WriteAllText(fileName, json);
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void LoadRecovery()
        {
            try
            {
                if (_startProgram == StartProgram.IsTester) return;

                string fileName = @"Engine\" + NameStrategyUniq + @"Recovery.json";

                if (!File.Exists(fileName))
                {
                    return;
                }

                string json = File.ReadAllText(fileName);

                var recoveryData = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(json);

                if (recoveryData == null) return;

                if (recoveryData.TryGetValue("Status", out JToken statusToken))
                {
                    _status = Enum.Parse<Status>(statusToken.ToString());
                }

                if (recoveryData.TryGetValue("Side", out JToken sideToken) && _main != null)
                {
                    _main.SideGrid = Enum.Parse<Side>(sideToken.ToString());
                    _hedge.SideGrid = _main.SideGrid == Side.Buy ? Side.Sell : Side.Buy;
                }

                if (recoveryData.TryGetValue("StartDepositValue", out JToken sdvToken) && _main != null)
                {
                    var rawValue = sdvToken?.ToString();

                    if (!string.IsNullOrWhiteSpace(rawValue) &&
                        decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                    {
                        _startDepositValue = value;
                    }
                }

                if (recoveryData.TryGetValue("AutoHedgeState", out JToken ahsToken) && _main != null &&
                    Enum.TryParse(ahsToken?.ToString(), ignoreCase: true, out AutoHedgeState state) &&
                    Enum.IsDefined(typeof(AutoHedgeState), state))
                {
                    _autoHedgeState = state;
                }

                if (recoveryData.TryGetValue("Main", out JToken mainToken) && _main != null)
                {
                    var mainData = mainToken as JObject;
                    if (mainData != null)
                    {
                        if (mainData.TryGetValue("ListOrders", out JToken ordersToken))
                        {
                            _main.ListOrders = ordersToken.ToObject<List<ListOrders>>();                            
                        }

                        _main.State_PlaceNewGrid = mainData["State_PlaceNewGrid"].Value<bool>();
                        _main.State_PlaceNewOrderGrid = mainData["State_PlaceNewOrderGrid"].Value<bool>();
                        _main.State_CheckDoneOpenOrder = mainData["State_CheckDoneOpenOrder"].Value<bool>();
                        _main.State_CheckDoneCloseOrder = mainData["State_CheckDoneCloseOrder"].Value<bool>();
                        _main.State_CheckCancelActiveCloseOrder = mainData["State_CheckCancelActiveCloseOrder"].Value<bool>();
                        _main.State_PlaceCloseOrder = mainData["State_PlaceCloseOrder"].Value<bool>();
                        _main.State_CheckPlaceCloseOrder = mainData["State_CheckPlaceCloseOrder"].Value<bool>();
                        _main.State_CancelOpenOrders = mainData["State_CancelOpenOrders"].Value<bool>();
                        _main.State_CheckCancelOpenOrders = mainData["State_CheckCancelOpenOrders"].Value<bool>();
                        _main.State_CheckRearrangeGrid = mainData["State_CheckRearrangeGrid"].Value<bool>();
                        _main.State_NeedRearrangeGrid = mainData["State_NeedRearrangeGrid"].Value<bool>();
                        _main.State_HasHedgeGrid = mainData["State_HasHedgeGrid"].Value<bool>();
                        _main.State_ClosePositionAtMarket = mainData["State_ClosePositionAtMarket"].Value<bool>();
                        _main.State_CheckClosePositionAtMarket = mainData["State_CheckClosePositionAtMarket"].Value<bool>();
                        _main.State_CancelActiveOrders = mainData["State_CancelActiveOrders"].Value<bool>();
                        _main.State_CheckCancelActiveOrders = mainData["State_CheckCancelActiveOrders"].Value<bool>();
                        _main.State_CheckUnrealizedPnL = mainData["State_CheckUnrealizedPnL"].Value<bool>();
                    }
                }

                if (recoveryData.TryGetValue("Hedge", out JToken hedgeToken) && _hedge != null)
                {
                    var hedgeData = hedgeToken as JObject;
                    if (hedgeData != null)
                    {
                        if (hedgeData.TryGetValue("ListOrders", out JToken ordersToken))
                        {
                            _hedge.ListOrders = ordersToken.ToObject<List<ListOrders>>();
                        }

                        _hedge.State_PlaceNewGrid = hedgeData["State_PlaceNewGrid"].Value<bool>();
                        _hedge.State_PlaceNewOrderGrid = hedgeData["State_PlaceNewOrderGrid"].Value<bool>();
                        _hedge.State_CheckDoneOpenOrder = hedgeData["State_CheckDoneOpenOrder"].Value<bool>();
                        _hedge.State_CheckDoneCloseOrder = hedgeData["State_CheckDoneCloseOrder"].Value<bool>();
                        _hedge.State_CheckCancelActiveCloseOrder = hedgeData["State_CheckCancelActiveCloseOrder"].Value<bool>();
                        _hedge.State_PlaceCloseOrder = hedgeData["State_PlaceCloseOrder"].Value<bool>();
                        _hedge.State_CheckPlaceCloseOrder = hedgeData["State_CheckPlaceCloseOrder"].Value<bool>();
                        _hedge.State_CancelOpenOrders = hedgeData["State_CancelOpenOrders"].Value<bool>();
                        _hedge.State_CheckCancelOpenOrders = hedgeData["State_CheckCancelOpenOrders"].Value<bool>();
                        _hedge.State_CheckRearrangeGrid = hedgeData["State_CheckRearrangeGrid"].Value<bool>();
                        _hedge.State_NeedRearrangeGrid = hedgeData["State_NeedRearrangeGrid"].Value<bool>();
                        _hedge.State_NeedReCalcGrid = hedgeData["State_NeedReCalcGrid"].Value<bool>();
                    }
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }
               
        private void AutoStart()
        {
            if (!_checkAutoStart) return;

            _checkAutoStart = false;

            if (!_setAutoStart.ValueBool) return;

            if (_main == null) CreateNewGrid(TypeGrid.Main);
            if (_hedge == null) CreateNewGrid(TypeGrid.Hedge);

            LoadRecovery();

            _main.State_CheckDoneOpenOrder = true;
            _main.AveragePricePosition = 0;

            _hedge.State_CheckDoneOpenOrder = true;
            _hedge.AveragePricePosition = 0;

            _timeCheckRsi = true;

            if (_main.GetListPosition().Count == 0)
            {
                SendNewLogMessage("Автостарт пропущен: нет открытых позиций.", Logging.LogMessageType.User);
                return;
            }
        }
         
        private void GetValueWallet()
        {
            _wallet = _volume.ValueDecimal;

            if (_typeVolume.ValueString == GetDescription(TypeVolume.Percent))
            {
                _wallet = _tab.Portfolio.ValueCurrent * _volume.ValueDecimal / 100;
            }
        }

        private bool _boolRsiBlocked = false;
        private bool _boolRsiUnblocked = false;
        private bool _timeCheckRsi = false;

        private void RsiFilter()
        {
            if (!_timeCheckRsi) return;            
            if (_status != Status.Work) return;
            if (_main == null) return;
            if (_main.State_HasHedgeGrid) return;

            if (_rsiOnOff.ValueString == GetDescription(RegimeOnOff.Off))
            {
                if (_rsiBlocked)
                {
                    _rsiBlocked = false;
                    SendNewLogMessage("RSI-фильтр выключен в настройках.", Logging.LogMessageType.User);
                    ResumeAfterRsiBlock(_main);
                }

                return;
            }

            bool rsiAllowed = CheckRSI();

            if (!rsiAllowed)
            {
                if (!_boolRsiBlocked)
                {
                    _rsiBlocked = true;
                    _boolRsiBlocked = true;
                    _boolRsiUnblocked = false;

                    EnterRsiBlock(_main);
                    SendNewLogMessage($"RSI-фильтр включен. (Current={_lastRsi}, Second={_lastRsiSecond}).", Logging.LogMessageType.User);
                }
            }
            else if (rsiAllowed)
            {
                List<Position> listPos = _main.GetListPosition();

                if (listPos.Count > 0) // не должны выходить из блока, если есть открытые позиции, это на случай когда туда сюда меняется значение RSI
                {
                    if (listPos[0].OpenActive)
                    {
                        return;
                    }                 
                }

                if (!_boolRsiUnblocked)
                {
                    _rsiBlocked = false;
                    _boolRsiUnblocked = true;
                    _boolRsiBlocked = false;

                    ResumeAfterRsiBlock(_main);
                    SendNewLogMessage($"RSI-фильтр выключен. (Current={_lastRsi}, Second={_lastRsiSecond}).", Logging.LogMessageType.User);
                }
            }

            _timeCheckRsi = false;
        }

        private bool CheckRSI()
        {
            decimal rsi = _lastRsi;

            if (_rsiOnOff.ValueString == GetDescription(RegimeOnOff.Off)) return true;
                       
            if (_choiseRSI.ValueString == GetDescription(ChoiseRSI.Second)) rsi = _lastRsiSecond;

            if (rsi == 0) return false;

            switch (_operationRSI.ValueString)
            {
                case ">":
                    if (rsi > _valueRsi.ValueInt)
                    {
                        return true;
                    }
                    break;

                case "<":
                    if (rsi < _valueRsi.ValueInt)
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }

        private void EnterRsiBlock(Grid grid)
        {
            int canceledOrdersCount = 0;

            List<Position> listPos = grid.GetListPosition();

            canceledOrdersCount += CancelOpenOrdersForRsiBlock(grid);

            grid.State_PlaceNewGrid = false;
            grid.State_PlaceNewOrderGrid = false;
            grid.State_CheckDoneOpenOrder = false;
            grid.State_CheckDoneCloseOrder = false;
            grid.State_CancelOpenOrders = false;
            
            grid.State_CheckRearrangeGrid = false;
            grid.State_NeedRearrangeGrid = false;

            if (listPos.Count > 0 && listPos[0].CloseActive)
            {
                grid.State_CheckDoneCloseOrder = true;
            }

            SaveRecovery();

            SendNewLogMessage($"RSI-блокировка активна. Отменено open-ордеров: {canceledOrdersCount}. Новые ордера временно запрещены.", Logging.LogMessageType.User);

            SendLogFlagsGrid(grid);
        }

        private decimal GetLastRsi(List<Candle> candles)
        {
            int index = candles.Count - 1;

            if (index - _periodRsi.ValueInt - 1 <= 0)
            {
                return 0m;
            }

            int startIndex = 1;

            if (index > _periodRsi.ValueInt)
            {
                startIndex = index - _periodRsi.ValueInt - 20;

                if (startIndex - 1 < 0)
                {
                    startIndex = index - _periodRsi.ValueInt + 1;
                }
            }

            List<decimal> priceChangeHigh = new List<decimal>();
            List<decimal> priceChangeLow = new List<decimal>();

            List<decimal> priceChangeHighAverage = new List<decimal>();
            List<decimal> priceChangeLowAverage = new List<decimal>();

            for (int i = startIndex, valueInd = 0; i <= index && i < candles.Count; i++, valueInd++)
            {
                if (candles[i].Close - candles[i - 1].Close > 0)
                {
                    priceChangeHigh.Add(candles[i].Close - candles[i - 1].Close);
                    priceChangeLow.Add(0);
                }
                else
                {
                    priceChangeLow.Add(candles[i - 1].Close - candles[i].Close);
                    priceChangeHigh.Add(0);
                }

                MovingAverageHard(priceChangeHigh, priceChangeHighAverage, _periodRsi.ValueInt, valueInd);
                MovingAverageHard(priceChangeLow, priceChangeLowAverage, _periodRsi.ValueInt, valueInd);
            }

            if (priceChangeHighAverage.Count == 0)
            {
                return 0m;
            }

            decimal averageHigh = priceChangeHighAverage[priceChangeHighAverage.Count - 1];
            decimal averageLow = priceChangeLowAverage[priceChangeLowAverage.Count - 1];

            decimal rsi;

            if (averageHigh != 0 &&
                averageLow != 0)
            {
                rsi = 100 * (1 - averageLow / (averageLow + averageHigh));
            }
            else
            {
                rsi = 100;
            }

            return Math.Round(rsi, 2);
        }

        private void MovingAverageHard(List<decimal> valuesSeries, List<decimal> moving, int length, int index)
        {
            if (index == length)
            {
                decimal lastMoving = 0;

                for (int i = index; i < valuesSeries.Count && i > index - 1 - length; i--)
                {
                    lastMoving += valuesSeries[i];
                }
                lastMoving = lastMoving / length;

                while (moving.Count <= index)
                {
                    moving.Add(0);
                }

                moving[index] = lastMoving;
            }
            else if (index > length)
            {
                decimal a = 2.0m / (length * 2);
                decimal lastValueMoving = moving[index - 1];
                decimal lastValueSeries = valuesSeries[index];
                decimal nowValueMoving;
                nowValueMoving = lastValueMoving + a * (lastValueSeries - lastValueMoving);

                while (moving.Count <= index)
                {
                    moving.Add(0);
                }

                moving[index] = nowValueMoving;
            }
        }
   
        private int CancelOpenOrdersForRsiBlock(Grid grid)
        {
            List<Position> positions = grid.GetListPosition();

            if (positions == null || positions.Count == 0 || positions[0].OpenOrders == null || positions[0].OpenOrders.Count == 0)
            {
                return 0;
            }

            int canceled = 0;

            for (int i = 0; i < positions[0].OpenOrders.Count; i++)
            {
                Order order = positions[0].OpenOrders[i];

                if (order.State != OrderStateType.Active &&
                    order.State != OrderStateType.Pending &&
                    order.State != OrderStateType.Partial &&
                    order.State != OrderStateType.None)
                {
                    continue;
                }

                decimal remainVolume = Math.Round(Math.Max(0, order.Volume - order.VolumeExecute), _tab.Security.DecimalsVolume, MidpointRounding.ToPositiveInfinity);

                if (remainVolume > 0)
                {
                    grid.ListOrders.Add(new ListOrders
                    {
                        Price = order.Price,
                        Volume = remainVolume
                    });
                }

                SendNewLogMessage($"RSI-блок: отменяем open-ордер {grid.SideGrid}. Price={order.Price}, Vol={order.Volume}, Remain={remainVolume}, State={order.State}", Logging.LogMessageType.User);
                _tab.CloseOrder(order);
                canceled++;
            }

            List<ListOrders> listOrders = new();

            if (grid.SideGrid == Side.Buy)
            {
                listOrders = grid.ListOrders.OrderByDescending(x => x.Price).ToList();
            }
            else
            {
                listOrders = grid.ListOrders.OrderBy(x => x.Price).ToList();
            }

            grid.ListOrders = listOrders;

            return canceled;
        }

        private void ResumeAfterRsiBlock(Grid grid)
        {
            List<Position> listPos = grid.GetListPosition();

            SetZeroFlagsGrid(grid);

            if (listPos.Count == 0)
            {                
                grid.State_PlaceNewGrid = true;
            }
            else
            {
                grid.State_PlaceNewOrderGrid = true;
                grid.State_CheckDoneOpenOrder = true;
                grid.State_CheckDoneCloseOrder = true;
                grid.State_CheckRearrangeGrid = true;
                grid.State_CheckUnrealizedPnL = true;

                if (_hedge.ListOrders.Count > 0)
                {
                    grid.State_HasHedgeGrid = true;
                    grid.State_CheckRearrangeGrid = false;
                }
            }

            SaveRecovery();

            SendNewLogMessage("RSI-блок снят. Возобновляем постановку/восстановление сетки.", Logging.LogMessageType.User);
            SendLogFlagsGrid(grid);
        }

        private void PushStartButton()
        {
            SendNewLogMessage("Нажата кнопка Запуск.", Logging.LogMessageType.User);

            if (_status == Status.Work) return;

            if (_tabHedge == null || _tabHedge.Security == null) 
            {
                SendNewLogMessage("Нет данных по инструменту в хеджирующей вкладке (3).", Logging.LogMessageType.Error);
                return;
            }

            if (_tabHedge.Security.Name != _tab.Security.Name)
            {
                SendNewLogMessage("Разные инструменты в основной (1) и хеджирующей вкладке (3)", Logging.LogMessageType.Error);
            }

            if (_lastPrice == 0)
            {
                SendNewLogMessage("Нет данных по цене инструмента.", Logging.LogMessageType.Error);
                return;
            }

            if (_сoveringPriceChanges.ValueDecimal == 0 ||
                _firstOrderIndent.ValueDecimal == 0 ||
                _volume.ValueDecimal == 0 ||
                _leverage.ValueDecimal == 0 ||
                _countOrdersGrid.ValueInt == 0 ||
                _profit.ValueDecimal == 0 ||
                _rearrangeGrid.ValueDecimal == 0 ||
                _countOrdersOnExchange.ValueInt == 0 ||
                _logPriceLevels.ValueDecimal == 0 ||
                _minVolumeForTicker.ValueDecimal == 0)
            {
                SendNewLogMessage("Не введены необходимые данные для работы робота.", Logging.LogMessageType.Error);
                _status = Status.NoWork;
                return;
            }

            if (_tab.PositionOpenLong.Count > 0 || _tab.PositionOpenShort.Count > 0)
            {
                DialogResult result = MessageBox.Show("Восстановить работу предыдущей сетки?", "Сообщение", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

                if (result == DialogResult.Yes)
                {
                    SendNewLogMessage("Восстанавливаем предыдущую сетку", Logging.LogMessageType.User);
                    LoadRecovery();

                    if (_main != null)
                    {
                        _main.State_CheckDoneOpenOrder = true;
                        _main.AveragePricePosition = 0;
                    }

                    if (_hedge != null)
                    {
                        _hedge.State_CheckDoneOpenOrder = true;
                        _hedge.AveragePricePosition = 0;
                    }
                }
                else
                {
                    SendNewLogMessage("Удаляем ордера предыдущей сессии", Logging.LogMessageType.User);

                    for (int i = 0; i < _tab.PositionsOpenAll.Count; i++)
                    {
                        if (_tab.PositionsOpenAll[i].CloseOrders != null &&
                            _tab.PositionsOpenAll[i].CloseOrders.Count > 0)
                        {
                            _tab.CloseOrder(_tab.PositionsOpenAll[i].CloseOrders[^1]);

                            decimal price = _lastPrice + _tab.Security.PriceStep * 100;

                            if (_tab.PositionsOpenAll[i].Direction == Side.Buy)
                            {
                                price = _lastPrice - _tab.Security.PriceStep * 100;
                            }
                           
                            _tab.CloseAtLimitUnsafe(_tab.PositionsOpenAll[i], price, _tab.PositionsOpenAll[i].OpenVolume);
                        }

                        _tab.CloseAllOrderToPosition(_tab.PositionsOpenAll[i]);
                    }                    
                }
            }

            _status = Status.Work;

            _timeCheckRsi = true;
            _boolRsiBlocked = false;
            _boolRsiUnblocked = false;

            if (_main == null) CreateNewGrid(TypeGrid.Main);
            if (_hedge == null) CreateNewGrid(TypeGrid.Hedge);

            if (_main.GetListPosition().Count == 0)
            {
                _main.State_PlaceNewGrid = true;
                _main.ListOrders.Clear();
                _hedge.ListOrders.Clear();
            }
        }

        private void PushPauseButton()
        {
            _status = Status.Pause;

            SaveRecovery();

            SendNewLogMessage("Включена пауза", Logging.LogMessageType.User);
        }

        private void PushStopButton()
        {                     
            _status = Status.Stop;
            SendNewLogMessage("Включена остановка", Logging.LogMessageType.User);
        }

        private void CancelAllOrders()
        {
            _status = Status.CancelAllOrders;

            SendNewLogMessage("Нажата кнопка отмены всех ордеров", Logging.LogMessageType.User);

            if (_main != null)
            {
                ReturnActiveOpenOrdersToGrid(_main);
            }

            if (_hedge != null)
            {
                ReturnActiveOpenOrdersToGrid(_hedge);
            }

            for (int i = 0; i < _tab.PositionsOpenAll.Count; i++)
            {
                _tab.CloseAllOrderToPosition(_tab.PositionsOpenAll[i]);
            }

            SetZeroFlagsGrid(_main);

            _status = Status.NoWork;

            SaveRecovery();
        }

        private int ReturnActiveOpenOrdersToGrid(Grid grid)
        {
            if (grid == null)
            {
                return 0;
            }

            List<Position> positions = grid.GetListPosition();

            if (positions == null ||
                positions.Count == 0 ||
                positions[0].OpenOrders == null ||
                positions[0].OpenOrders.Count == 0)
            {
                return 0;
            }

            int restored = 0;

            for (int i = 0; i < positions[0].OpenOrders.Count; i++)
            {
                Order order = positions[0].OpenOrders[i];

                if (order.State != OrderStateType.Active &&
                    order.State != OrderStateType.Pending &&
                    order.State != OrderStateType.Partial &&
                    order.State != OrderStateType.None)
                {
                    continue;
                }

                decimal remainVolume = Math.Round(
                    Math.Max(0, order.Volume - order.VolumeExecute),
                    _tab.Security.DecimalsVolume,
                    MidpointRounding.ToPositiveInfinity);

                if (remainVolume <= 0)
                {
                    continue;
                }

                grid.ListOrders.Add(new ListOrders
                {
                    Price = order.Price,
                    Volume = remainVolume
                });

                restored++;
            }

            SortGridOrders(grid);

            return restored;
        }

        private void SortGridOrders(Grid grid)
        {
            if (grid == null || grid.ListOrders == null)
            {
                return;
            }

            if (grid.SideGrid == Side.Buy)
            {
                grid.ListOrders = grid.ListOrders.OrderByDescending(x => x.Price).ToList();
            }
            else
            {
                grid.ListOrders = grid.ListOrders.OrderBy(x => x.Price).ToList();
            }
        }

        private void CloseOrdersOnMarket()
        {
            try
            {
                _status = Status.CloseOrdersOnMarket;

                SendNewLogMessage("Нажата кнопка Закрыть позицию по рынку", Logging.LogMessageType.User);

                //DialogResult result = MessageBox.Show("Остановить бота?", "Сообщение", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

                //CancelAndCloseAllOrders();

                if (_main != null)
                {
                    SetZeroFlagsGrid(_main);
                    _main.State_CancelActiveOrders = true;
                    SendLogFlagsGrid(_main);

                }

                if (_hedge != null)
                {
                    SetZeroFlagsGrid(_hedge);
                    SendLogFlagsGrid(_hedge);
                }



                /*switch (result)
                {
                    case DialogResult.Yes:

                        //_status = Status.NoWork;     
                        break;

                    case DialogResult.No:

                        //_main.State_CheckCancelOpenOrders = true;
                        //_status = Status.Work;
                        break;

                    default:
                        break;
                }*/



                SaveRecovery();
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void CancelAndCloseAllOrders()
        {
            for (int i = 0; i < _tab.PositionsOpenAll.Count; i++)
            {
                //_tab.CloseAtMarket(_tab.PositionsOpenAll[i], _tab.PositionsOpenAll[i].OpenVolume);
                decimal price = _lastPrice + _tab.Security.PriceStep * 1000;

                if (_tab.PositionsOpenAll[i].Direction == Side.Buy)
                {
                    price = _lastPrice - _tab.Security.PriceStep * 1000;
                }

                //_tab.CloseAtLimit(_tab.PositionsOpenAll[i], price, _tab.PositionsOpenAll[i].OpenVolume);
                //_tab.CloseAllOrderToPosition(_tab.PositionsOpenAll[i]);
                _tab.CloseAtLimitUnsafe(_tab.PositionsOpenAll[i], price, _tab.PositionsOpenAll[i].OpenVolume);

                
            }

            for (int i = 0; i < _tabHedge.PositionsOpenAll.Count; i++)
            {
                //_tab.CloseAtMarket(_tab.PositionsOpenAll[i], _tab.PositionsOpenAll[i].OpenVolume);
                decimal price = _lastPrice + _tabHedge.Security.PriceStep * 1000;

                if (_tabHedge.PositionsOpenAll[i].Direction == Side.Buy)
                {
                    price = _lastPrice - _tabHedge.Security.PriceStep * 1000;
                }

                _tabHedge.CloseAtLimitUnsafe(_tabHedge.PositionsOpenAll[i], price, _tabHedge.PositionsOpenAll[i].OpenVolume);
            }
        }

        private void CloseAllOrdersOnMarket()
        {
            for (int i = 0; i < _tab.PositionsOpenAll.Count; i++)
            {
                if (_tab.PositionsOpenAll[i].CloseOrders != null &&
                    _tab.PositionsOpenAll[i].CloseOrders.Count > 0)
                {
                    if (_tab.PositionsOpenAll[i].OpenOrders != null && _tab.PositionsOpenAll[i].OpenOrders.Count > 0)
                    {
                        for (int j = 0; j < _tab.PositionsOpenAll[i].OpenOrders.Count; j++)
                        {
                            if (_tab.PositionsOpenAll[i].OpenOrders[j].State == OrderStateType.Active)
                            {
                                _tab.CloseOrder(_tab.PositionsOpenAll[i].OpenOrders[j]);
                            }
                        }
                    }

                    if (_tab.PositionsOpenAll[i].CloseOrders[^1].State == OrderStateType.Active)
                    {
                        _tab.CloseOrder(_tab.PositionsOpenAll[i].CloseOrders[^1]);
                    }

                    decimal price = _lastPrice + _tab.Security.PriceStep * 100;

                    if (_tab.PositionsOpenAll[i].Direction == Side.Buy)
                    {
                        price = _lastPrice - _tab.Security.PriceStep * 100;
                    }

                    _tab.CloseAtLimitUnsafe(_tab.PositionsOpenAll[i], price, _tab.PositionsOpenAll[i].OpenVolume);                    
                    //_tab.CloseAtMarket(_tab.PositionsOpenAll[i], _tab.PositionsOpenAll[i].OpenVolume);

                    SendNewLogMessage($"Закрываем {_tab.PositionsOpenAll[i].Direction} ордер", Logging.LogMessageType.User);
                }
            }
        }

        private void CancelActiveCloseOrders(Position position)
        {
            if (position == null || position.CloseOrders == null)
            {
                return;
            }

            for (int i = 0; i < position.CloseOrders.Count; i++)
            {
                if (position.CloseOrders[i].State == OrderStateType.Active ||
                    position.CloseOrders[i].State == OrderStateType.Partial ||
                    position.CloseOrders[i].State == OrderStateType.Pending ||
                    position.CloseOrders[i].State == OrderStateType.None)
                {
                    SendNewLogMessage("Отменяем закрывающий ордер: " + position.CloseOrders[i].Side + " " + position.CloseOrders[i].Price + " - " + position.CloseOrders[i].Volume + ", State: " + position.CloseOrders[i].State, Logging.LogMessageType.User);
                    _tab.CloseOrder(position.CloseOrders[i]);
                }
            }
        }

        private double GetAveragePricePositions(Side side)
        {
            try
            {
                if (side == Side.Buy && _tab.PositionOpenLong.Count == 0)
                {
                    return 0;
                }

                if (side == Side.Sell && _tab.PositionOpenShort.Count == 0)
                {
                    return 0;
                }

                Position pos = new Position();

                if (side == Side.Sell)
                {
                    pos = _tab.PositionOpenShort[0];
                }
                else
                {
                    pos = _tab.PositionOpenLong[0];
                }
                                
                return Math.Round((double)pos.EntryPrice, _tab.Security.Decimals, MidpointRounding.AwayFromZero);
            }
            catch (Exception e)
            {
                SendNewLogMessage(e.ToString(), Logging.LogMessageType.Error);
                return 0;
            }            
        }

        private double GetTpPricePosition(Grid grid, double averagePricePosition)
        {
            double profit = (double)_profit.ValueDecimal;

            if (grid.TypeGrid == TypeGrid.Hedge)
            {
                profit = (double)_profitHedge.ValueDecimal;
            }

            if (grid.SideGrid == Side.Buy)
            {
                return Math.Round((averagePricePosition + averagePricePosition * profit / 100) / (double)_tab.Security.PriceStep * (double)_tab.Security.PriceStep, _tab.Security.Decimals, MidpointRounding.AwayFromZero);
            }
            else
            {
                return Math.Round((averagePricePosition - averagePricePosition * profit / 100) / (double)_tab.Security.PriceStep * (double)_tab.Security.PriceStep, _tab.Security.Decimals, MidpointRounding.AwayFromZero);
            }
        }

        private List<ListOrders> CalculateHedgeGrid(Side side)
        {
            List<ListOrders> list = new List<ListOrders>();

            decimal indentMinPrice = _lastPrice * _firstOrderIndent.ValueDecimal / 100;

            decimal minimumPrice = 0;
            double maximumPrice = 0;
            decimal summPositions = 0;

            if (_main.PriceTP <= 0) GetPriceCloseOrder(_main);

            if (side == Side.Buy)
            {
                minimumPrice = Math.Round(_lastPrice - indentMinPrice, _tab.Security.Decimals, MidpointRounding.AwayFromZero);
                //maximumPrice = _main.PriceTP - (double)minimumPrice * (((double)minimumPrice - _main.PriceTP) / (double)minimumPrice * ((double)_setOffsetLastOrder.ValueDecimal / 100 + 1));
                maximumPrice = _main.PriceTP - _main.PriceTP * ((double)_setOffsetLastOrder.ValueDecimal / 100);
                summPositions = _tab.PositionOpenShort[0].OpenVolume * _tab.PositionOpenShort[0].EntryPrice;

                if (_tabHedge.PositionOpenLong.Count > 0) // нужно когда сработала логика по ликвидационной цене
                {
                    decimal summHedgePosition = _tabHedge.PositionOpenLong[0].OpenVolume * _tabHedge.PositionOpenLong[0].EntryPrice;
                    summPositions -= summHedgePosition;
                    SendNewLogMessage($"В хеджирующей вкладке есть открытая позиция на сумму {summHedgePosition}", Logging.LogMessageType.User);
                }
            }
            if (side == Side.Sell)
            {
                minimumPrice = Math.Round(_lastPrice + indentMinPrice, _tab.Security.Decimals, MidpointRounding.AwayFromZero);
                //maximumPrice = _main.PriceTP + (double)minimumPrice * ((_main.PriceTP - (double)minimumPrice) / (double)minimumPrice * ((double)_setOffsetLastOrder.ValueDecimal / 100 + 1));
                maximumPrice = _main.PriceTP + _main.PriceTP * ((double)_setOffsetLastOrder.ValueDecimal / 100);
                summPositions = _tab.PositionOpenLong[0].OpenVolume * _tab.PositionOpenLong[0].EntryPrice;

                if (_tabHedge.PositionOpenShort.Count > 0)// нужно когда сработала логика по ликвидационной цене
                {
                    decimal summHedgePosition = _tabHedge.PositionOpenShort[0].OpenVolume * _tabHedge.PositionOpenShort[0].EntryPrice;
                    summPositions -= summHedgePosition;
                    SendNewLogMessage($"В хеджирующей вкладке есть открытая позиция на сумму {summHedgePosition}", Logging.LogMessageType.User);
                }
            }

            SendNewLogMessage($"PriceTP = {_main.PriceTP}, MinPrice = {minimumPrice}, MaxPrice = {maximumPrice}", Logging.LogMessageType.User);

            List<decimal> listOrders = new();
            List<decimal> listVolume = new();
                               
            listOrders = GenerateLevels((double)(minimumPrice), (double)(maximumPrice), _countOrdersGridHedge, (double)_logPriceLevelsHedge.ValueDecimal);
            listVolume = TryGenerateVolumeHedgeGrid(listOrders, summPositions);

            decimal summHedge = 0;

            for (int i = 0; i < listOrders.Count; i++)
            {
                ListOrders item = new ListOrders();
                item.Price = listOrders[i];
                item.Volume = listVolume[i];

                summHedge += item.Volume * item.Price;

                if (listVolume[i] < _minVolumeForTicker)
                {
                    SendNewLogMessage("Hedge: Рассчитанный объем меньше минимального торгуемого объема. Измените настройки хедж сетки.", Logging.LogMessageType.Error);
                    
                    return new List<ListOrders>();
                }
                list.Add(item);
            }

            SendNewLogMessage($"Сумма всех позиций основной сетки: {Math.Round(summPositions, 2)}, хеджирующей сетки: {Math.Round(summHedge, 2)}", Logging.LogMessageType.User);

            return list;
        }

        private List<decimal> TryGenerateVolumeHedgeGrid(List<decimal> listOrders, decimal generalVolume)
        {           
            // рассчитываем объем исходя из минимального лота
            List<decimal> firstListVolume = new List<decimal>();

            decimal sum = 0;

            for (int i = 0; i < listOrders.Count; i++)
            {
                if (i == 0)
                {
                    firstListVolume.Add(_minVolumeForTicker);
                }
                else
                {
                    decimal calc = firstListVolume[^1] + firstListVolume[^1] * _percentMartingaleHedge.ValueDecimal / 100;
                    firstListVolume.Add(calc);
                }

                sum += listOrders[i] * firstListVolume[i];
            }

            if (sum == 0) return null;

            // пересчитываем объем с учетом величины депозита
            decimal scale = generalVolume / sum;

            //SendNewLogMessage($"Размер депозита для торговли: {generalVolume}, сумма для сетки из расчета минимального ордера: {sum}, множитель сетки: {scale}", Logging.LogMessageType.User);

            List<decimal> secondListVolume = new List<decimal>();

            for (int i = 0; i < firstListVolume.Count; i++)
            {
                decimal scaleVolume = Math.Round(firstListVolume[i] * scale / _tab.Security.Lot, _tab.Security.DecimalsVolume, MidpointRounding.ToNegativeInfinity) * _tab.Security.Lot;
                decimal truncateVolume = Math.Round(Math.Floor(scaleVolume / _tab.Security.VolumeStep) * _tab.Security.VolumeStep, _tab.Security.DecimalsVolume, MidpointRounding.ToNegativeInfinity);
                secondListVolume.Add(truncateVolume);
            }

            return secondListVolume;
        }
        

        private bool CheckSumVolumeHedgeGrid(List<decimal> listOrders, List<decimal> listVolume, Side side, decimal summPositions)
        {
            decimal checkSumm = 0;

            // направление хедж сетки обратное основной сетки
            if (side == Side.Buy)
            {
                checkSumm = _tab.PositionOpenShort[0].OpenVolume * _tab.PositionOpenShort[0].EntryPrice;                
            }
            else
            {
                checkSumm = _tab.PositionOpenLong[0].OpenVolume * _tab.PositionOpenLong[0].EntryPrice;
            }

            decimal sum = 0;

            for (int i = 0; i < listOrders.Count; i++)
            {
                sum += listOrders[i] * listVolume[i];
            }

            if (checkSumm > sum)
            {
                return true;
            }

            return false;
        }

        private List<ListOrders> CalculateGrid(Side side)
        {
            List<ListOrders> list = new List<ListOrders>();

            if (_сoveringPriceChanges.ValueDecimal == 0 ||
                _firstOrderIndent.ValueDecimal == 0 ||
                _minVolumeForTicker == 0 ||
                _volume.ValueDecimal == 0 ||
                _leverage.ValueDecimal == 0)
            {
                SendNewLogMessage("Не введены необходимые данные для расчета сетки", Logging.LogMessageType.Error);
                _status = Status.NoWork;
                return list;
            }

            decimal indentMinPrice = _lastPrice * _firstOrderIndent.ValueDecimal / 100;
            decimal indentMaxPrice = _lastPrice * _сoveringPriceChanges.ValueDecimal / 100;

            decimal minimumPrice = 0;
            decimal maximumPrice = 0;

            if (side == Side.Buy)
            {
                minimumPrice = _lastPrice - indentMinPrice;
                maximumPrice = _lastPrice - indentMaxPrice;
            }
            else
            {
                minimumPrice = _lastPrice + indentMinPrice;
                maximumPrice = _lastPrice + indentMaxPrice;     
            }

            List<decimal> listOrders = new();
            List<decimal> listVolume = new();

            // расчет сетки по мартигейлу от минимального объема
            if (_autoCalcMartingaleAmountOrders)
            {
                List<decimal> oldListOrders = new();
                List<decimal> oldListVolume = new();

                for (int i = 2; i < 10000; i++)
                {
                    listOrders = GenerateLevels((double)(minimumPrice), (double)(maximumPrice), i, (double)_logPriceLevels.ValueDecimal);
                    listVolume = TryGenerateVolume(listOrders);
                                        
                    if (CheckSumDepositMartingale(listOrders, listVolume))
                    {
                        listOrders = oldListOrders;
                        listVolume = oldListVolume;

                        break;
                    }
                    else
                    {
                        oldListOrders = listOrders;
                        oldListVolume = listVolume;
                    }
                }
            }
            else // обычный расчет сетки
            {
                listOrders = GenerateLevels((double)(minimumPrice), (double)(maximumPrice), _countOrdersGrid.ValueInt, (double)_logPriceLevels.ValueDecimal);
                listVolume = GenerateVolume(listOrders);
            }

            if (listOrders == null ||
            listVolume == null)
            {
                SendNewLogMessage("Main: Не введены необходимые данные для расчета сетки", Logging.LogMessageType.Error);
                _status = Status.NoWork;
                return list;
            }

            if (listOrders.Count != listVolume.Count)
            {
                SendNewLogMessage("Main: Ошибка в расчетах сетки. Количество уровней цен несоответствует количеству уровней объемов", Logging.LogMessageType.Error);
                _status = Status.NoWork;
                return list;
            }

            for (int i = 0; i < listOrders.Count; i++)
            {
                ListOrders item = new ListOrders();
                item.Price = listOrders[i];
                item.Volume = listVolume[i];
                if (listVolume[i] < _minVolumeForTicker)
                {
                    SendNewLogMessage("Main: Рассчитанный объем меньше минимального торгуемого объема. Измените настройки сетки.", Logging.LogMessageType.Error);
                    _status = Status.NoWork;
                    return new List<ListOrders>();
                }
                list.Add(item);
            }

            _startDepositValue = Math.Round(_tab.Portfolio.ValueCurrent); // запоминаем значение депозита при старте сетки
            SendNewLogMessage("Main: Значение депозита при старте сетки: " + _startDepositValue, Logging.LogMessageType.User);

            return list;
        }

        private bool CheckSumDepositMartingale(List<decimal> listOrders, List<decimal> listVolume)
        {
            decimal generalVolume;

            if (_typeVolume.ValueString == GetDescription(TypeVolume.Fix))
            {
                generalVolume = _volume.ValueDecimal * _leverage.ValueDecimal;
            }
            else
            {
                generalVolume = _tab.Portfolio.ValueCurrent * _volume.ValueDecimal / 100 * _leverage.ValueDecimal;
            }

            decimal sum = 0;

            for (int i = 0; i < listOrders.Count; i++)
            {
                sum += listOrders[i] * listVolume[i];
            }

            if (generalVolume < sum)
            {
                return true;
            }

            return false;
        }

        private List<decimal> TryGenerateVolume(List<decimal> listOrders)
        { 
            List<decimal> firstListVolume = new List<decimal>();

            for (int i = 0; i < listOrders.Count; i++)
            {
                if (i == 0)
                {
                    firstListVolume.Add(_minVolumeForTicker);
                }
                else
                {
                    decimal calc = Math.Round(firstListVolume[0] * (decimal)Math.Pow((double)_percentMartingale.ValueDecimal / 100 + 1, i), _tab.Security.DecimalsVolume, MidpointRounding.ToNegativeInfinity);
                    decimal truncateVolume = Math.Floor(calc / _tab.Security.VolumeStep) * _tab.Security.VolumeStep;
                    firstListVolume.Add(truncateVolume);
                }
            }
            return firstListVolume;
        }

        private List<ListOrders> LoadSampleToGrid(Side side)
        {
            List<ListOrders> list = new List<ListOrders>();

            if (!File.Exists(_stringFileSample.ValueString))
            {
                return null;
            }

            try
            {
                using (StreamReader reader = new StreamReader(_stringFileSample.ValueString))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            string[] split = line.Split(' ');

                            if (side.ToString() != split[2])
                            {
                                continue;
                            }

                            decimal volume;
                            decimal.TryParse(split[1], out volume);

                            decimal percent;
                            decimal.TryParse(split[0], out percent);

                            decimal price = (decimal)(Math.Round(((double)percent / 100 * (double)_lastPrice + (double)_lastPrice) / (double)_tab.Security.PriceStep, MidpointRounding.AwayFromZero) * (double)_tab.Security.PriceStep);

                            if (percent == 0 || volume == 0)
                            {
                                continue;
                            }

                            ListOrders item = new ListOrders();

                            item.Price = price;
                            item.Volume = volume;
                            list.Add(item);
                        }
                    }
                    reader.Close();
                }               

                List<ListOrders> sortedList = list.OrderBy(x => x.Price).ToList();

                if (side == Side.Buy)
                {
                    sortedList = list.OrderByDescending(x => x.Price).ToList();
                }               

                return sortedList;
            }
            catch (Exception e)
            {
                SendNewLogMessage(e.ToString(), Logging.LogMessageType.Error);
                return null;
            }
        }

        private List<decimal> GenerateVolume(List<decimal> listOrders)
        {
            if (_volume.ValueDecimal == 0 ||
                _leverage.ValueDecimal == 0)
            {
                return null;
            }

            decimal generalVolume;

            // вычисляем размер депозита для торговли
            if (_typeVolume.ValueString == GetDescription(TypeVolume.Fix))
            {
                generalVolume = _volume.ValueDecimal * _leverage.ValueDecimal;
            }
            else
            {
                generalVolume= _tab.Portfolio.ValueCurrent * _volume.ValueDecimal / 100 * _leverage.ValueDecimal;
            }

            // рассчитываем объем исходя из минимального лота
            List<decimal> firstListVolume = new List<decimal>();

            decimal sum = 0;

            for (int i = 0; i < listOrders.Count; i++)
            {
                if (i == 0)
                {
                    firstListVolume.Add(_minVolumeForTicker);
                }
                else
                {
                    decimal calc = firstListVolume[^1] + firstListVolume[^1] * _percentMartingale.ValueDecimal / 100;
                    firstListVolume.Add(calc);
                }

                sum += listOrders[i] * firstListVolume[i];
            }

            if (sum == 0) return null;

            // пересчитываем объем с учетом величины депозита
            decimal scale = generalVolume / sum;

            SendNewLogMessage($"Размер депозита для торговли: {generalVolume}, сумма для сетки из расчета минимального ордера: {sum}, множитель сетки: {scale}", Logging.LogMessageType.User);

            List<decimal> secondListVolume = new List<decimal>();

            for (int i = 0; i < firstListVolume.Count; i++)
            {
                decimal scaleVolume = Math.Round(firstListVolume[i] * scale / _tab.Security.Lot, _tab.Security.DecimalsVolume, MidpointRounding.ToNegativeInfinity) * _tab.Security.Lot;
                decimal truncateVolume = Math.Round(Math.Floor(scaleVolume / _tab.Security.VolumeStep) * _tab.Security.VolumeStep, _tab.Security.DecimalsVolume, MidpointRounding.ToNegativeInfinity);
                secondListVolume.Add(truncateVolume);
            }

            return secondListVolume;
        }

        private List<decimal> GenerateLevels(double min, double max, int levels, double K)
        {
            if (levels < 2) //|| K <= 0
            {
                return null;
            }

            double logK = K;//Math.Log(K);
            List<decimal> result = new List<decimal>();

            for (int i = 0; i < levels; i++)
            {
                double t = (double)i / (levels - 1);
                double value;

                if (Math.Abs(logK) < 1e-10) // Handle K = 1 (logK ≈ 0)
                {
                    value = Math.Round((min + (max - min) * t) / (double)_tab.Security.PriceStep, MidpointRounding.AwayFromZero) * (double)_tab.Security.PriceStep;
                }
                else
                {
                    value = Math.Round((min + (max - min) * (Math.Exp(t * logK) - 1) / (Math.Exp(logK) - 1)) / (double)_tab.Security.PriceStep, MidpointRounding.AwayFromZero) * (double)_tab.Security.PriceStep;
                }

                result.Add((decimal)value);
            }

            return result;
        }

        private void WriteLogOrders(Grid grid)
        {
            string str = $"Сетка: {grid.TypeGrid}. Текущая цена: " + _lastPrice + "\n";
            decimal count= 0;

            for (int i = grid.ListOrders.Count - 1; i >= 0; i--)
            {
                str += $"{grid.ListOrders[i].Price}, {grid.SideGrid} , {grid.ListOrders[i].Volume}" + "\n";
                count += grid.ListOrders[i].Price * grid.ListOrders[i].Volume;
            }
            
            str += $"Сумма ордеров {grid.SideGrid}: " + count;

            SendNewLogMessage(str, Logging.LogMessageType.User);            
        }
            
        #endregion

        #region AutoHedge

        private decimal _startDepositValue;
        private decimal _liquidationPriceMain;
        private decimal _deviationPriceMain;
        private decimal _wallet;

        private AutoHedgeState _autoHedgeState = AutoHedgeState.Monitoring;

        public enum AutoHedgeState
        {
            Disable,
            Monitoring,
            ActiveHedgeGrid,
            ExecuteOrderRangeLiquidation
        }

        private void AutoHedge()
        {
            try
            {
                if (_status != Status.Work) return;

                Side side = _main.SideGrid == Side.Buy ? Side.Sell : Side.Buy;

                if (_setRegimeAutoHedge == RegimeOnOff.Off.ToString()) return;

                if (_autoHedgeState == AutoHedgeState.Monitoring)
                {
                    CheckActivationGridHedge(_main);                    
                }

                if (_autoHedgeState == AutoHedgeState.Monitoring ||
                    _autoHedgeState == AutoHedgeState.ActiveHedgeGrid)
                {
                    RangeLiquidation(_main);
                }

                if (_autoHedgeState == AutoHedgeState.ExecuteOrderRangeLiquidation)
                {
                    ExecuteOrderRangeLiquidation(_hedge);
                }
                                
                if (_autoHedgeState == AutoHedgeState.ActiveHedgeGrid)
                {                    
                    HedgeGrid();
                }
            }
            catch (Exception e)
            {
                SendNewLogMessage(e.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void CheckActivationGridHedge(Grid grid)
        {
            List<Position> pos = grid.GetListPosition();

            if (pos == null || pos.Count == 0) return;

            if (_setZoneActivMainPriceBool)
            {
                if (pos[0].OpenVolume > 0)
                {
                    if (grid.SideGrid == Side.Buy)
                    {
                        decimal targetPrice = pos[0].EntryPrice + pos[0].EntryPrice * _setZoneActivMainPriceValue / 100;

                        if (_lastPrice > targetPrice)
                        {
                            _autoHedgeState = AutoHedgeState.ActiveHedgeGrid;
                            SendNewLogMessage($"Запускаем режим хеджирующей сетки. Зона активации: % от цены основной позиции, (LastPrice = {_lastPrice}, TargetPrice = {targetPrice}", Logging.LogMessageType.User);
                        }
                    }
                    else
                    {
                        decimal targetPrice = pos[0].EntryPrice - pos[0].EntryPrice * _setZoneActivMainPriceValue / 100;

                        if (_lastPrice < targetPrice)
                        {
                            _autoHedgeState = AutoHedgeState.ActiveHedgeGrid;
                            SendNewLogMessage($"Запускаем режим хеджирующей сетки. Зона активации: % от цены основной позиции, (LastPrice = {_lastPrice}, TargetPrice = {targetPrice}", Logging.LogMessageType.User);
                        }
                    }
                }
            }

            if (_setZoneActivDepositLossBool)
            {
                decimal depositLossValue = _startDepositValue - (_startDepositValue * _setZoneActivDepositLossValue.ValueDecimal / 100);

                decimal currentSummPositions = pos[0].OpenVolume * _lastPrice - pos[0].OpenVolume * pos[0].EntryPrice;

                if (grid.SideGrid == Side.Sell) currentSummPositions = pos[0].OpenVolume * pos[0].EntryPrice - pos[0].OpenVolume * _lastPrice;

                decimal valueDeposit = _tab.Portfolio.ValueCurrent + currentSummPositions;

                if (valueDeposit < depositLossValue)
                {
                    _autoHedgeState = AutoHedgeState.ActiveHedgeGrid;
                    SendNewLogMessage($"Запускаем режим хеджирующей сетки. Зона активации: % от просадки депозита. ValueDeposit = {valueDeposit}, depositLossValue = {depositLossValue}", Logging.LogMessageType.User);
                }
            }

            if (_setZoneActivUnrealPnlBool)
            {            
                if (grid.UnrealizPnL < _setUnrealPnlValue)
                {
                    _autoHedgeState = AutoHedgeState.ActiveHedgeGrid;
                    SendNewLogMessage($"Запускаем режим хеджирующей сетки. Зона активации: значение UnrealizePnL = {grid.UnrealizPnL}", Logging.LogMessageType.User);
                }
            }

            CheckDeactivationRsiForHedge();            
        }

        private void CheckDeactivationRsiForHedge()
        {
            if (!_rsiBlocked) return;

            if (_setZoneDeactivRSIBool)
            {
                if (_main == null) return;

                List<Position> pos = _main.GetListPosition();

                if (pos == null) return;
                if (pos.Count == 0) return;
                if (pos[0].OpenVolume == 0) return;

                _autoHedgeState = AutoHedgeState.ActiveHedgeGrid;
                SendNewLogMessage($"Запускаем режим хеджирующей сетки. Зона активации: включен фильтр RSI", Logging.LogMessageType.User);
            }
        }

        private void RangeLiquidation(Grid grid)
        {
            if (!_setZoneActivRangeLiquidationBool) return;

            List<Position> listPos = grid.GetListPosition();

            if (listPos == null || listPos.Count == 0) return;

            decimal positionPrice = listPos[0].EntryPrice;
            decimal positionVolume = listPos[0].OpenVolume * listPos[0].EntryPrice;

            if (positionVolume == 0) return;

            _liquidationPriceMain = 0;
            _deviationPriceMain = 0;

            if (grid.SideGrid == Side.Buy)
            {
                decimal positionVolumeShort = 0;

                if (_tab.PositionOpenShort.Count > 0)
                {
                    positionVolumeShort = _tab.PositionOpenShort[0].EntryPrice * _tab.PositionOpenShort[0].OpenVolume;
                }

                decimal positionVolumeHedge = 0;

                if (_tabHedge.PositionOpenShort.Count > 0)
                {
                    positionVolumeHedge = _tabHedge.PositionOpenShort[0].EntryPrice * _tabHedge.PositionOpenShort[0].OpenVolume;
                }

                _liquidationPriceMain = Math.Round(positionPrice * ((positionVolume - positionVolumeShort - positionVolumeHedge - _wallet) / (positionVolume - positionVolumeShort - positionVolumeHedge)), _tab.Security.Decimals, MidpointRounding.AwayFromZero);
            }
            else
            {
                decimal positionVolumeLong = 0;

                if (_tab.PositionOpenLong.Count > 0)
                {
                    positionVolumeLong = _tab.PositionOpenLong[0].EntryPrice * _tab.PositionOpenLong[0].OpenVolume;
                }

                decimal positionVolumeHedge = 0;

                if (_tabHedge.PositionOpenLong.Count > 0)
                {
                    positionVolumeHedge = _tabHedge.PositionOpenLong[0].EntryPrice * _tabHedge.PositionOpenLong[0].OpenVolume;
                }

                _liquidationPriceMain = Math.Round(positionPrice * ((positionVolume - positionVolumeLong - positionVolumeHedge + _wallet) / (positionVolume - positionVolumeLong - positionVolumeHedge)), _tab.Security.Decimals, MidpointRounding.AwayFromZero);
            }

            if (grid.SideGrid == Side.Buy)
            {
                _deviationPriceMain = Math.Round(listPos[0].EntryPrice - (_setRangeLiquidationValue / 100 * listPos[0].EntryPrice), _tab.Security.Decimals, MidpointRounding.AwayFromZero);

                if (_deviationPriceMain < _liquidationPriceMain)
                {
                    SendNewLogMessage($"Начинаем добавлять лоты хеджирования. Long: DeviationPrice = {_deviationPriceMain}, LiquidationPrice = {_liquidationPriceMain}", Logging.LogMessageType.User);

                    AddLotHedgeToSell();

                    if (_autoHedgeState != AutoHedgeState.ActiveHedgeGrid)
                    {
                        SendNewLogMessage("Запускаем режим хеджирующей сетки", Logging.LogMessageType.User);
                    }

                    _autoHedgeState = AutoHedgeState.ExecuteOrderRangeLiquidation;
                }
            }
            else
            {
                _deviationPriceMain = Math.Round((_setRangeLiquidationValue / 100 * listPos[0].EntryPrice) + listPos[0].EntryPrice, _tab.Security.Decimals, MidpointRounding.AwayFromZero);

                if (_deviationPriceMain > _liquidationPriceMain)
                {
                    SendNewLogMessage($"Начинаем добавлять лоты хеджирования. Short: DeviationPrice = {_deviationPriceMain}, LiquidationPrice = {_liquidationPriceMain}", Logging.LogMessageType.User);

                    AddLotHedgeToBuy();

                    if (_autoHedgeState != AutoHedgeState.ActiveHedgeGrid)
                    {
                        SendNewLogMessage("Запускаем режим хеджирующей сетки", Logging.LogMessageType.User);
                    }

                    _autoHedgeState = AutoHedgeState.ExecuteOrderRangeLiquidation;                                        
                }
            }
        }
        
        private void AddLotHedgeToBuy()
        {
            decimal lotHedge = CalculateLotHedge();

            if (lotHedge == 0) return;

            if (_tabHedge.PositionOpenLong.Count == 0)
            {
                _tabHedge.BuyAtMarket(lotHedge);
            }
            else
            {
                _tabHedge.BuyAtMarketToPosition(_tabHedge.PositionOpenLong[0], lotHedge);
            }
        }

        private void AddLotHedgeToSell()
        {
            decimal lotHedge = CalculateLotHedge();

            if (lotHedge == 0) return;

            if (_tabHedge.PositionOpenShort.Count == 0)
            {
                _tabHedge.SellAtMarket(lotHedge);
            }
            else
            {
                _tabHedge.SellAtMarketToPosition(_tabHedge.PositionOpenShort[0], lotHedge);
            }
        }

        private decimal CalculateLotHedge()
        {
            decimal lotHedge = _setLotHedge.ValueDecimal;

            if (_setTypeLotHedge.ValueString == GetDescription(TypeVolume.Percent))
            {
                lotHedge = _wallet * _setLotHedge.ValueDecimal / 100;
            }

            if (_tabHedge == null || _tabHedge.Security == null)
            {
                SendNewLogMessage("TabHedge or Security = null", Logging.LogMessageType.User);
                return 0;
            }

            decimal lot = Math.Round(lotHedge / _lastPrice, _tabHedge.Security.DecimalsVolume, MidpointRounding.ToNegativeInfinity);

            SendNewLogMessage("Лот для хеджирования по цене ликвидации: " + lot, Logging.LogMessageType.User);

            return lot;
        }

        private void HedgeGrid()
        {
            List<Position> listPos = _main.GetListPosition();
            
            if (listPos == null || listPos.Count == 0) return;

            Position pos = listPos[0];

            if (pos.CloseOrders != null && pos.CloseOrders.Count > 0) 
            {
                if (!_main.State_HasHedgeGrid)
                {
                    if (pos.CloseOrders[^1].State == OrderStateType.Active)
                    {                      
                        CancelActiveCloseOrders(pos);
                        _main.State_PlaceCloseOrder = false;//
                        _main.State_CheckPlaceCloseOrder = false;//
                        _main.State_CheckDoneCloseOrder = false;//
                        _main.State_CheckCancelActiveCloseOrder = true;
                        _hedge.State_PlaceNewGrid = true;
                        _main.State_HasHedgeGrid = true;
                    }
                }
            }

            if (_hedge.State_CheckRearrangeGrid) CheckRearrangeGrid(_hedge);
            if (_hedge.State_NeedReCalcGrid) NeedReCalcGrid(_hedge);
            if (_hedge.State_PlaceNewGrid) PlaceNewGrid(_hedge);
            if (_hedge.State_PlaceNewOrderGrid) PlaceNewOrderFromMainGrid(_hedge);
            if (_hedge.State_CheckDoneOpenOrder) CheckDoneOpenOrder(_hedge);
            if (_hedge.State_CheckCancelActiveCloseOrder) CheckCancelActiveCloseOrder(_hedge);
            if (_hedge.State_PlaceCloseOrder) PlaceCloseOrder(_hedge);
            if (_hedge.State_CheckPlaceCloseOrder) CheckPlaceCloseOrder(_hedge);
            if (_hedge.State_CheckDoneCloseOrder) CheckDoneCloseOrder(_hedge);
            if (_hedge.State_CancelOpenOrders) CancelOpenOrders(_hedge);
            if (_hedge.State_CheckCancelOpenOrders) CheckCancelOpenOrders(_hedge);

            if (_versionDiff)
            {
                if (_hedge.State_CheckDoneOpenOrder) CheckAddonReCalcGrid(_hedge);
            }
        }

        private void CheckAddonReCalcGrid(Grid grid)
        {
            if (_status != Status.Work && _status != Status.Stop) return;

            if (/*_main.ListOrders.Count == 0*/true)
            {
                if (!_main.GetPosition().OpenActive)
                {
                    List<Position> listPos = grid.GetListPosition();

                    if (listPos.Count > 0 && listPos[0].OpenVolume == 0)
                    {
                        for (int i = 0; i < listPos[0].OpenOrders.Count; i++)
                        {
                            if (listPos[0].OpenOrders[i].State == OrderStateType.Active)
                            {
                                if (grid.SideGrid == Side.Buy)
                                {
                                    decimal diffPrice = listPos[0].OpenOrders[i].Price + listPos[0].OpenOrders[i].Price * 0.02m; // 2 процента разница

                                    if (diffPrice < _lastPrice)
                                    {
                                        SetZeroFlagsGrid(grid);
                                        grid.State_NeedReCalcGrid = true;

                                        SendNewLogMessage($"Все ордера основной сетки исполнились, цена ушла. Нужно пересчитать хедж сетку.", Logging.LogMessageType.User);

                                        SendLogFlagsGrid(grid);
                                        SaveRecovery();
                                    }
                                }
                                else
                                {
                                    decimal diffPrice = listPos[0].OpenOrders[i].Price - listPos[0].OpenOrders[i].Price * 0.02m;

                                    if (diffPrice > _lastPrice)
                                    {
                                        SetZeroFlagsGrid(grid);
                                        grid.State_NeedReCalcGrid = true;

                                        SendNewLogMessage($"Все ордера основной сетки исполнились, цена ушла. Нужно пересчитать хедж сетку.", Logging.LogMessageType.User);

                                        SendLogFlagsGrid(grid);
                                        SaveRecovery();
                                    }
                                }

                                break;
                            }
                        }
                    }
                }
            }
        }

        private void NeedReCalcGrid(Grid grid)
        {
            if (_status != Status.Work && _status != Status.Stop) return;

            SendNewLogMessage($"Нужно пересчитать хедж сетку.", Logging.LogMessageType.User);

            List<Position> listPos = grid.GetListPosition();

            SetZeroFlagsGrid(grid);//test

            if (listPos.Count != 0)
            {
                CancelActiveCloseOrders(listPos[0]);               
                grid.State_CheckCancelActiveCloseOrder = true;
            }
            
            //grid.State_NeedReCalcGrid = false;
            grid.State_CancelOpenOrders = true;

            SendLogFlagsGrid(grid);
            SaveRecovery();
        }
               
        private enum StateExecuteOrderRangeLiquidation
        {
            Disable,
            NeedCheckExecuteOrderLong,
            NeedCheckExecuteOrderShort,
            Done
        }

        private void ExecuteOrderRangeLiquidation(Grid grid)
        {
            if (_autoHedgeState == AutoHedgeState.ExecuteOrderRangeLiquidation)
            {
                if (_tabHedge.PositionsOpenAll.Count == 0) return;

                if (_tabHedge.PositionsOpenAll[0].OpenOrders[^1].State == OrderStateType.Done)
                {
                    _autoHedgeState = AutoHedgeState.ActiveHedgeGrid;

                    if (_hedge.ListOrders.Count == 0)
                    {                        
                        _hedge.State_PlaceNewGrid = true;
                    }                   
                }
            }
        }

        #endregion

        #region Descriptions

        private class Grid
        {
            public Grid(TypeGrid typeGrid, Side side, BotTabSimple tab)
            {
                TypeGrid = typeGrid;
                SideGrid = side;
                ListOrders = new();
                Tab = tab;
            }

            public BotTabSimple Tab;
            public TypeGrid TypeGrid;
            public Side SideGrid;
            public List<ListOrders> ListOrders;
            public DateTime TimerTwapOrders;
            public double AveragePricePosition;
            public decimal UnrealizPnL;
            public double PriceTP;
            public DateTime TimeRearrangeGrid;
            public DateTime TimeCycle;

            public bool State_PlaceNewGrid;
            public bool State_PlaceNewOrderGrid;
            public bool State_CheckDoneOpenOrder;
            public bool State_CheckDoneCloseOrder;
            public bool State_CheckCancelActiveCloseOrder;
            public bool State_PlaceCloseOrder;
            public bool State_CheckPlaceCloseOrder;
            public bool State_CancelOpenOrders;
            public bool State_CheckCancelOpenOrders;

            public bool State_CheckRearrangeGrid;
            public bool State_NeedRearrangeGrid;

            public bool State_HasHedgeGrid;
            public bool State_NeedReCalcGrid;

            public bool State_ClosePositionAtMarket;
            public bool State_CheckClosePositionAtMarket;
            public bool State_CancelActiveOrders;
            public bool State_CheckCancelActiveOrders;

            public bool State_CheckUnrealizedPnL;

            public List<Position> GetListPosition()
            {
                return this.SideGrid == Side.Buy ? this.Tab.PositionOpenLong : this.Tab.PositionOpenShort;
            }

            public Position GetPosition()
            {
                return this.SideGrid == Side.Buy ? this.Tab.PositionOpenLong[0] : this.Tab.PositionOpenShort[0];
            }

            public void Delete()
            {
                ListOrders = null;
                Tab = null;
            }
        }

        private enum StateGrid
        {
            NoGrid,
            PlaceNewMainGrid,
            CheckPlaceMainGrid,
            WaitExecuteOrderMainGrid,
            PlaceOneOrderMainGrid,
            CheckPlaceOneOrderMainGrid,
            PlaceCloseOrder,
            CheckPlaceCloseOrder,
            CancelActiveCloseOrder,
            CheckCancelActiveCloseOrder,   
        }

        private enum TypeGrid
        {
            Main,
            Hedge
        }

        public enum Status
        {
            [Description("Запуск")]
            Start,
            [Description("Остановка")]
            Stop,
            [Description("Пауза")]
            Pause,
            [Description("Отмена всех ордеров")]
            CancelAllOrders,
            [Description("Принудительная остановка")]
            ForcedStop,
            [Description("Закрыть позицию по рынку")]
            CloseOrdersOnMarket,
            [Description("Запущен")]
            Work,
            [Description("Остановлен")]
            NoWork
        }

        private enum Regime
        {
            [Description("Только длинные позиции")]
            Long,
            [Description("Только короткие позиции")]
            Short
        }

        private enum TypeVolume
        {
            [Description("Фиксированный")]
            Fix,
            [Description("% от депозита")]
            Percent
        }

        private enum RegimeOnOff
        {
            On,
            Off
        }

        private enum RsiOperation
        {
            [Description("<")]
            Less,
            [Description(">")]
            More,
        }

        private enum ChoiseRSI
        {
            [Description("Текущая пара")]
            Current,
            [Description("Второй источник")]
            Second           
        }

        public string GetDescription(Enum enumValue)
        {
            if (enumValue == null)
                return string.Empty;

            Type type = enumValue.GetType();
            MemberInfo[] memberInfo = type.GetMember(enumValue.ToString());

            if (memberInfo != null && memberInfo.Length > 0)
            {
                object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs != null && attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
                else
                {
                    return enumValue.ToString();
                }                
            }

            return enumValue.ToString();
        }

        public class ListOrders
        {
            public decimal Price;
            public decimal Volume;
        }
    }
    #endregion
}

