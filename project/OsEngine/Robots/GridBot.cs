using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Globalization;
using OsEngine.Candles.Series;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Market.Servers.TraderNet.Entity;

namespace OsEngine.Robots
{
    [Bot("GridBot")]
    public class GridBot : BotPanel
    {
        private BotTabSimple _tab;
        private BotTabSimple _tabRSI;
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
        private StrategyParameterString _tag;
        private List<string> _tableListSide = new List<string> { "Buy", "Sell" };
        private Status _status = Status.NoWork;
        private StrategyParameterDecimal _minVolumeForTicker;
        private StrategyParameterBool _autoCalcMartingaleAmountOrders;
        private StrategyParameterDecimal _setTimeTwapOrders;
        private DateTime _timeTwapBuyOrders;
        private DateTime _timeTwapSellOrders;
        private StrategyParameterBool _boolTimerTwapOrders;

        Aindicator _rsi;
        Aindicator _rsiBTC;
        private StrategyParameterString _rsiOnOff;
        private StrategyParameterString _choiseRSI;
        private StrategyParameterString _operationRSI;
        private StrategyParameterInt _valueRsi;
        private StrategyParameterInt _periodRsi;
        private decimal _lastRsi;
        private decimal _lastRsiSecond;

        public GridBot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");

            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];
            TabCreate(BotTabType.Simple);
            _tabRSI = TabsSimple[1];

            string tabNameParameters = " Параметры ";
            string tabNameRsi = " Фильтр RSI ";
                       
            _typeVolume = CreateParameter("Тип объема", GetDescription(TypeVolume.Fix), new string[] { GetDescription(TypeVolume.Fix), GetDescription(TypeVolume.Percent) }, tabNameParameters);
            _volume = CreateParameter("Объем", 0m, 0m, 0m, 0m, tabNameParameters);
            _leverage = CreateParameter("Плечо", 0m, 0m, 0m, 0m, tabNameParameters);
            _regime = CreateParameter("Режим работы", GetDescription(Regime.LongShort),
                new string[] { GetDescription(Regime.LongShort),
                    GetDescription(Regime.OnlyLong),
                    GetDescription(Regime.OnlyShort) },
                tabNameParameters);
            _loadGrig = CreateParameter("Загрузить сетку из шаблона", false, tabNameParameters);
            _stringFileSample = CreateParameter("Путь к файлу шаблона", "", tabNameParameters);
            _сoveringPriceChanges = CreateParameter("Перекрытие изменения цены, %", 0m, 0m, 0m, 0m, tabNameParameters);
            _countOrdersGrid = CreateParameter("Сетка ордеров", 0, 0, 0, 0, tabNameParameters);
            _percentMartingale = CreateParameter("Процент мартингейла, %", 0m, 0m, 0m, 0m, tabNameParameters);
            _autoCalcMartingaleAmountOrders = CreateParameter("Автоматический расчет ордеров по Мартингейлу", false, tabNameParameters);
            _firstOrderIndent = CreateParameter("Отступ первого ордера, %", 0m, 0m, 0m, 0m, tabNameParameters);
            _profit = CreateParameter("Профит, %", 0m, 0m, 0m, 0m, tabNameParameters);
            _rearrangeGrid = CreateParameter("Подтяжка сетки ордеров, %", 0m, 0m, 0m, 0m, tabNameParameters);
            _countOrdersOnExchange = CreateParameter("Частичное выставление", 0, 0, 0, 0, tabNameParameters);
            _delayRearrange = CreateParameter("Задержка перед отменой сетки ордеров для подтяжки, мин", 0m, 0m, 0m, 0m, tabNameParameters);
            _delayAfterCycle = CreateParameter("Задержка после завершения цикла, мин", 0m, 0m, 0m, 0m, tabNameParameters);
            _logPriceLevels = CreateParameter("Логарифмическое распределение уровней цен", 0m, 0m, 0m, 0m, tabNameParameters);
            _tag = CreateParameter("Тэг", "", tabNameParameters);
            _minVolumeForTicker = CreateParameter("Минимальный объем по инструменту", 0m, 0m, 0m, 0m, tabNameParameters);
            //_boolTimerTwapOrders = CreateParameter("Включить TWAP ордера", false, tabNameParameters);
            //_setTimeTwapOrders = CreateParameter("Задержка выставления TWAP ордеров", 0m, 0m, 0m, 0m, tabNameParameters);

            _rsiOnOff = CreateParameter("Фильтр RSI", RsiRegime.Off.ToString(), new string[] { RsiRegime.Off.ToString(), RsiRegime.On.ToString() }, tabNameRsi);
            _periodRsi = CreateParameter("Период RSI", 14, 0, 0, 0, tabNameRsi);
            _choiseRSI = CreateParameter("Выбор по", GetDescription(ChoiseRSI.Current), new string[] { GetDescription(ChoiseRSI.Current), GetDescription(ChoiseRSI.Second) }, tabNameRsi);
            _operationRSI = CreateParameter("Условие фильтрации", GetDescription(RsiOperation.Еquals), new string[] { GetDescription(RsiOperation.Еquals), GetDescription(RsiOperation.Less), GetDescription(RsiOperation.More) }, tabNameRsi);
            _valueRsi = CreateParameter("Значение RSI", 40, 0, 0, 0, tabNameRsi); 

            _rsi = IndicatorsFactory.CreateIndicatorByName("RSI", name + "RSI", false);
            _rsi = (Aindicator)_tab.CreateCandleIndicator(_rsi, name + "NewArea");
            ((IndicatorParameterInt)_rsi.Parameters[0]).ValueInt = _periodRsi.ValueInt;
            _rsi.Save();

            _rsiBTC = IndicatorsFactory.CreateIndicatorByName("RSI", name + "RSIBTC", false);
            _rsiBTC = (Aindicator)_tabRSI.CreateCandleIndicator(_rsiBTC, name + "NewArea");
            ((IndicatorParameterInt)_rsiBTC.Parameters[0]).ValueInt = _periodRsi.ValueInt;
            _rsiBTC.Save();

            ParametrsChangeByUser += GridBot_ParametrsChangeByUser;

            this.ParamGuiSettings.Title = "Grid Bot";
            this.ParamGuiSettings.Height = 600;
            this.ParamGuiSettings.Width = 700;

            CustomTabToParametersUi customTabGrid = ParamGuiSettings.CreateCustomTab(" Ручная настройка сетки ");
            CustomTabToParametersUi customTabMonitoring = ParamGuiSettings.CreateCustomTab(" Мониторинг и управление ");

            CreateTableGrid();
            customTabGrid.AddChildren(_hostTableGrid);

            CreateTableMonitoring();
            customTabMonitoring.AddChildren(_hostMonitoring);

            _tab.ManualPositionSupport.DisableManualSupport();

            _tab.CandleUpdateEvent += _tab_CandleUpdateEvent;

            Thread worker = new Thread(ThreadRefreshTable) { IsBackground = true };
            worker.Start();

            Thread mainThread = new Thread(MainThread) { IsBackground = true };
            mainThread.Start();
        }
               
        private void GridBot_ParametrsChangeByUser()
        {
            ((IndicatorParameterInt)_rsi.Parameters[0]).ValueInt = _periodRsi.ValueInt;
            _rsi.Save();
            _rsi.Reload();

            ((IndicatorParameterInt)_rsiBTC.Parameters[0]).ValueInt = _periodRsi.ValueInt;
            _rsiBTC.Save();
            _rsiBTC.Reload();
        }

        public override string GetNameStrategyType()
        {
            return "GridBot";
        }

        public override void ShowIndividualSettingsDialog()
        {
        }

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

                    if (_regime.ValueString == GetDescription(Regime.OnlyLong))
                    {
                        listBuy = CalculateGrid(Side.Buy);
                    }
                    else if (_regime.ValueString == GetDescription(Regime.OnlyShort))
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
                        newRow.Cells.Add(new DataGridViewComboBoxCell()

                        {
                            DataSource = _tableListSide,
                            Value = _tableListSide[0],
                        }
                        );

                        _gridTableGrid.Rows.Add(newRow);

                        _gridTableGrid.Rows[_gridTableGrid.Rows.Count - 1].Cells[0].Value = _gridTableGrid.Rows.Count;
                    }
                }

                if (e.ColumnIndex == 6) // добавить строку
                {
                    DataGridViewRow newRow = new DataGridViewRow();

                    newRow.Cells.Add(new DataGridViewTextBoxCell());
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
                if (e.ColumnIndex == 5)
                {
                    _gridTableGrid.Rows.RemoveAt(e.RowIndex);

                    for (int i = 0; i < _gridTableGrid.Rows.Count; i++)
                    {
                        _gridTableGrid.Rows[i].Cells[0].Value = i + 1;
                    }
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
                                saveString += _gridTableGrid.Rows[i].Cells[4].FormattedValue.ToString();

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
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                }
            }         
        }

        #endregion

        #region Monitoring tab
                
        private WindowsFormsHost _hostMonitoring;
        private DataGridView _gridMonitoring;
        private DataGridView _gridMonitoringButton;

        Status _statusValue = Status.NoWork;
        private DateTime _timeStatus;
        private int _postedOrdersBuy;
        private int _postedOrdersSell;
        private int _completedOrdersBuy;
        private int _completedOrdersSell;
        private decimal _unrealizPnLBuy = 0;
        private decimal _unrealizPnLSell = 0;

        private void ThreadRefreshTable()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(1000);
                    OrdersMonitoring();
                    AddDataToGrid();
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
            newButtonGrid.Columns.Add("Col5", "");

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
            row.Cells[4].Value = GetDescription(Status.ForcedStop);
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[5].Value = GetDescription(Status.CloseOrdersOnMarket);

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
                "Текущая цена",
                "Кошелек",
                "Всего средств",
                "Свободно средств",
                "Сумма депозита в работе",
                "Состояние ордеров (выставлено/выполнено)",
                "Нереализованный PNL",                
                "Цена выставленного тейк-профита",
                "Средняя цена позиции",
                "RSI Current",
                "RSI Second"
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
            
            if (_tab.PositionOpenLong.Count > 0)
            {
                if (_status == Status.Pause ||
                    _status == Status.ForcedStop)
                {
                    _averagePriceBuy = GetAveragePricePositions(Side.Buy);
                }
                decimal volume = _tab.PositionOpenLong[0].OpenVolume;
                
                _unrealizPnLBuy = _lastPrice * volume - (decimal)_averagePriceBuy * volume;               
            }
            else
            {
                _unrealizPnLBuy = 0;
                _averagePriceBuy = 0;
                _priceTPBuy = 0;
            }
            if (_tab.PositionOpenShort.Count > 0)
            {
                if (_status == Status.Pause ||
                    _status == Status.ForcedStop)
                {
                    _averagePriceSell = GetAveragePricePositions(Side.Sell);
                }
                decimal volume = _tab.PositionOpenShort[0].OpenVolume;
                _unrealizPnLSell = (decimal)_averagePriceSell * volume - _lastPrice * volume;
            }
            else
            {
                _unrealizPnLSell = 0;
                _averagePriceSell = 0;
                _priceTPSell = 0;
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
            _gridMonitoring.Rows[2].Cells[1].Value = _lastPrice; //Текущая цена
            _gridMonitoring.Rows[3].Cells[1].Value = wallet; //Кошелек
            _gridMonitoring.Rows[4].Cells[1].Value = Math.Round(_tab.Portfolio.ValueCurrent, 4);//Всего средств
            _gridMonitoring.Rows[5].Cells[1].Value = freeMoney;//Свободно средств
            _gridMonitoring.Rows[6].Cells[1].Value = Math.Round(_tab.Portfolio.ValueBlocked, 4);//Сумма депозита в работе
            _gridMonitoring.Rows[7].Cells[1].Value = "Long: " + _postedOrdersBuy + "/" + _completedOrdersBuy + ", Short: " + _postedOrdersSell + "/" + _completedOrdersSell;//Состояние ордеров
            _gridMonitoring.Rows[8].Cells[1].Value = "Long: " + _unrealizPnLBuy + ", Short: " + _unrealizPnLSell;//Нереализованный PNL
            _gridMonitoring.Rows[9].Cells[1].Value = "Long: " + _priceTPBuy + ", Short: " + _priceTPSell; //Цена выставленного тейк-профита
            _gridMonitoring.Rows[10].Cells[1].Value = "Long: " + _averagePriceBuy + ", Short: " + _averagePriceSell;//Средняя цена позиции
            _gridMonitoring.Rows[11].Cells[1].Value = _lastRsi;//RSI Current
            _gridMonitoring.Rows[12].Cells[1].Value = _lastRsiSecond;//RSI Second
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
                    _status = Status.CancelAllOrders;
                    CancelAllOrders();
                }
                if (e.ColumnIndex == 4 && e.RowIndex == 0)
                {
                    PushForcedStop();
                }
                if (e.ColumnIndex == 5 && e.RowIndex == 0)
                {
                    _status = Status.CloseOrdersOnMarket;
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
                _postedOrdersBuy = 0;
                _completedOrdersBuy = 0;

                if (_tab.PositionOpenLong != null && _tab.PositionOpenLong.Count > 0)
                {
                    if (_tab.PositionOpenLong[0].OpenOrders.Count > 0)
                    {
                        for (int i = 0; i < _tab.PositionOpenLong[0].OpenOrders.Count; i++)
                        {
                            if (_tab.PositionOpenLong[0].OpenOrders[i].State == OrderStateType.Active ||
                                _tab.PositionOpenLong[0].OpenOrders[i].State == OrderStateType.Activ ||
                                _tab.PositionOpenLong[0].OpenOrders[i].State == OrderStateType.Done)
                            {
                                _postedOrdersBuy++;
                            }
                            if (_tab.PositionOpenLong[0].OpenOrders[i].State == OrderStateType.Done)
                            {
                                _completedOrdersBuy++;
                            }
                        }
                    }
                }

                _postedOrdersSell = 0;
                _completedOrdersSell = 0;

                if (_tab.PositionOpenShort != null && _tab.PositionOpenShort.Count > 0)
                {
                    if (_tab.PositionOpenShort[0].OpenOrders.Count > 0)
                    {
                        for (int i = 0; i < _tab.PositionOpenShort[0].OpenOrders.Count; i++)
                        {
                            if (_tab.PositionOpenShort[0].OpenOrders[i].State == OrderStateType.Active ||
                                _tab.PositionOpenShort[0].OpenOrders[i].State == OrderStateType.Activ ||
                                _tab.PositionOpenShort[0].OpenOrders[i].State == OrderStateType.Done)
                            {
                                _postedOrdersSell++;
                            }
                            if (_tab.PositionOpenShort[0].OpenOrders[i].State == OrderStateType.Done)
                            {
                                _completedOrdersSell++;
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

        private List<ListOrders> _listOrdersBuy = new List<ListOrders>();
        private List<ListOrders> _listOrdersSell = new List<ListOrders>();

        private decimal _lastPrice;

        private bool _workCycle = true;

        private DateTime _timeCycleBuy = DateTime.Now;
        private DateTime _timeCycleSell = DateTime.Now;

        private DateTime _timeRearrangeBuy = DateTime.Now;
        private DateTime _timeRearrangeSell = DateTime.Now;

        private bool _needRearrangeBuy = false;
        private bool _needRearrangeSell = false;

        private bool _checkCloseOrderBuy = false;
        private bool _checkCloseOrderSell = false;

        private bool _tryOpenOrderBuy = false;
        private bool _tryOpenOrderSell = false;
        
        private bool _checkExecuteOpenBuy = true;
        private bool _checkExecuteOpenSell = true;

        private void _tab_CandleUpdateEvent(List<Candle> obj)
        {
            _lastPrice = obj[obj.Count - 1].Close;            
        }

        private void MainThread()
        {
            while (true)
            {
                try
                {
                    if (_tab == null || _tab.Portfolio == null)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    if (!_tab.Connector.IsReadyToTrade)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    if (_rsi.DataSeries != null || _rsiBTC.DataSeries != null)
                    {
                        _lastRsi = _rsi.DataSeries[0].Last;
                        _lastRsiSecond = _rsiBTC.DataSeries[0].Last;
                    }

                    if (_status == Status.Work ||
                        _status == Status.Stop)
                    {
                        RefreshGrid();
                    }

                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                }
            }    
        }

        private void RefreshGrid()
        {
            if (_regime.ValueString == GetDescription(Regime.OnlyLong))
            {
                CheckExecuteOpenOrder(Side.Buy);
                CheckExecuteCloseOrders(Side.Buy);
                PlaceOpenOrders(Side.Buy);
            }
            else if (_regime.ValueString == GetDescription(Regime.OnlyShort))
            {
                CheckExecuteOpenOrder(Side.Sell);
                CheckExecuteCloseOrders(Side.Sell);
                PlaceOpenOrders(Side.Sell);
            }
            else
            {
                CheckExecuteOpenOrder(Side.Buy);
                CheckExecuteCloseOrders(Side.Buy);
                PlaceOpenOrders(Side.Buy);
                CheckExecuteOpenOrder(Side.Sell);
                CheckExecuteCloseOrders(Side.Sell);
                PlaceOpenOrders(Side.Sell);
            }

            TryRearrangeGrid();
        }

        private void PushPauseButton()
        {
            _status = Status.Pause;
            _workCycle = false;
            _checkCloseOrderBuy = false;
            _checkCloseOrderSell = false;
            _checkExecuteOpenBuy = false;
            _checkExecuteOpenSell = false;
            _tryOpenOrderBuy = false;
            _tryOpenOrderSell = false;
            _needRearrangeBuy = false;
            _needRearrangeSell = false;

            SendNewLogMessage("Включена пауза", Logging.LogMessageType.User);
        }

        private void PushStopButton()
        {                     
            _status = Status.Stop;
            _workCycle = false;            
        }

        private void PushForcedStop()
        {
            _status = Status.ForcedStop;
            _workCycle = false;
            _checkCloseOrderBuy = false;
            _checkCloseOrderSell = false;
            _checkExecuteOpenBuy = false;
            _checkExecuteOpenSell = false;
            _tryOpenOrderBuy = false;
            _tryOpenOrderSell = false;
            _needRearrangeBuy = false;
            _needRearrangeSell = false;

            SendNewLogMessage("Включена принудительная остановка", Logging.LogMessageType.User);

            ForcedStop();
        }

        private void ForcedStop()
        {                      
            if (_tab.PositionOpenLong != null && _tab.PositionOpenLong.Count > 0)
            {
                if (_tab.PositionOpenLong[0].OpenOrders.Count > 0)
                {
                    for (int i = 0; i < _tab.PositionOpenLong[0].OpenOrders.Count; i++)
                    {
                        if (_tab.PositionOpenLong[0].OpenOrders[i].State == OrderStateType.Active ||
                            _tab.PositionOpenLong[0].OpenOrders[i].State == OrderStateType.Activ)
                        {
                            ListOrders list = new ListOrders();
                            list.Price = _tab.PositionOpenLong[0].OpenOrders[i].Price;
                            list.Volume = _tab.PositionOpenLong[0].OpenOrders[i].Volume;
                            _listOrdersBuy.Add(list);
                            _tab.CloseOrder(_tab.PositionOpenLong[0].OpenOrders[i]);
                        }                        
                    }

                    List<ListOrders> sortedList = _listOrdersBuy.OrderByDescending(x => x.Price).ToList();
                    _listOrdersBuy.Clear();
                    _listOrdersBuy = sortedList;
                }                
            }

            if (_tab.PositionOpenShort != null && _tab.PositionOpenShort.Count > 0)
            {
                if (_tab.PositionOpenShort[0].OpenOrders.Count > 0)
                {
                    for (int i = 0; i < _tab.PositionOpenShort[0].OpenOrders.Count; i++)
                    {
                        if (_tab.PositionOpenShort[0].OpenOrders[i].State == OrderStateType.Active ||
                            _tab.PositionOpenShort[0].OpenOrders[i].State == OrderStateType.Activ)
                        {
                            ListOrders list = new ListOrders();
                            list.Price = _tab.PositionOpenShort[0].OpenOrders[i].Price;
                            list.Volume = _tab.PositionOpenShort[0].OpenOrders[i].Volume;
                            _listOrdersSell.Add(list);
                            _tab.CloseOrder(_tab.PositionOpenShort[0].OpenOrders[i]);
                        }
                    }

                    List<ListOrders> sortedList = _listOrdersSell.OrderBy(x => x.Price).ToList();
                    _listOrdersSell.Clear();
                    _listOrdersSell = sortedList;
                }
            }

            SaveRecoveryGrid();

            WriteLogOrders();
        }

		private void LoadRecoveryGrid()
		{
			if (!File.Exists(@"Engine\" + NameStrategyUniq + @"RecoveryGrid.txt"))
			{
				return;
            }
            try
            {
                List<ListOrders> listBuy = new List<ListOrders>();
				List<ListOrders> listSell = new List<ListOrders>();

				using (StreamReader reader = new StreamReader(@"Engine\" + NameStrategyUniq + @"RecoveryGrid.txt"))
				{
					string line;

					while ((line = reader.ReadLine()) != null)
					{
						if (!string.IsNullOrEmpty(line))
						{
                            string newLine = line.Replace('.', ',');

							string[] split = newLine.Split(' ');

							decimal price = 0;

							decimal.TryParse(split[0], out price);

							decimal volume = 0;

							decimal.TryParse(split[1], out volume);

							ListOrders list = new ListOrders();

                            list.Price = price;
                            list.Volume = volume;

                            if (split[2] == "Buy")
                            {
                                listBuy.Add(list);
                            }
                            else
                            {
                                listSell.Add(list);
                            }
						}
					}
					reader.Close();
				}
                				
				_listOrdersBuy.Clear();
				_listOrdersBuy = listBuy.OrderByDescending(x => x.Price).ToList();

                _listOrdersSell.Clear();
				_listOrdersSell = listSell.OrderBy(x => x.Price).ToList();
			}
			catch (Exception ex)
			{
				SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
			}
		}

		private void SaveRecoveryGrid()
        {
			try
            {
				using (StreamWriter writer = new StreamWriter(@"Engine\" + NameStrategyUniq + @"RecoveryGrid.txt"))
				{
					if (_listOrdersBuy != null)
					{
						for (int i = 0; i < _listOrdersBuy.Count; i++)
                        {
							string saveString = "";

							saveString += _listOrdersBuy[i].Price + " ";
							saveString += _listOrdersBuy[i].Volume + " ";
							saveString += "Buy";

							writer.WriteLine(saveString);
						}
					}

					if (_listOrdersSell != null)
					{
						for (int i = 0; i < _listOrdersSell.Count; i++)
						{
							string saveString = "";

							saveString += _listOrdersSell[i].Price + " ";
							saveString += _listOrdersSell[i].Volume + " ";
							saveString += "Sell";

							writer.WriteLine(saveString);
						}
					}
					writer.Close();
                }
			}
			catch (Exception ex)
			{
				SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
			}
		}

		private void PushStartButton()
        {
            if (_status == Status.Stop)
            {
                _status = Status.Work;
                _workCycle = true;
            }

            if (_status == Status.NoWork)
            {
                _status = Status.Start;
                _workCycle = true;
            }

            if (_status == Status.Pause ||
                _status == Status.ForcedStop)
            {
                RecoveryMethod();
            }

            if (_lastPrice != 0)
            {
                if (_status == Status.Start)
                {
                    if (_tab.PositionOpenLong.Count > 0 || _tab.PositionOpenShort.Count > 0)
                    {
						DialogResult result = MessageBox.Show("Восстановить работу предыдущей сетки?", "Сообщение", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

						_workCycle = false;
						_checkCloseOrderBuy = false;
						_checkCloseOrderSell = false;
						_checkExecuteOpenBuy = false;
						_checkExecuteOpenSell = false;
						_tryOpenOrderBuy = false;
						_tryOpenOrderSell = false;
						_needRearrangeBuy = false;
						_needRearrangeSell = false;

						if (result == DialogResult.Yes)
                        {
                            SendNewLogMessage("Восстанавливаем предыдущую сетку", Logging.LogMessageType.User);

                            LoadRecoveryGrid();

							RecoveryMethod();

							return;
						}
                        else
                        {
							SendNewLogMessage("Удаляем ордера предыдущей сессии", Logging.LogMessageType.User);

							for (int i = 0; i < _tab.PositionsOpenAll.Count; i++)
							{
								if (_tab.PositionsOpenAll[i].CloseOrders != null &&
									_tab.PositionsOpenAll[i].CloseOrders.Count > 0)
								{
									_tab.CloseOrder(_tab.PositionsOpenAll[i].CloseOrders.Last());

									decimal price = _lastPrice + _tab.Security.PriceStep * 100;

									if (_tab.PositionsOpenAll[i].Direction == Side.Buy)
									{
										price = _lastPrice - _tab.Security.PriceStep * 100;
										_checkCloseOrderBuy = true;
									}
									else
									{
										_checkCloseOrderSell = true;
									}

									_tab.CloseAtLimitUnsafe(_tab.PositionsOpenAll[i], price, _tab.PositionsOpenAll[i].OpenVolume);
								}

								_tab.CloseAllOrderToPosition(_tab.PositionsOpenAll[i]);
							}

							_status = Status.NoWork;

							return;
						}                        
					}

                    _status = Status.Work;
                    _workCycle = true;
                    StartGrid();
                    
                    return;
                }                
            }
            else
            { 
                SendNewLogMessage("Нет данных по цене инструмента", Logging.LogMessageType.Error);
                _status = Status.NoWork;
            }           
        }

        private void RecoveryMethod()
        {
            WriteLogOrders();

            _averagePriceBuy = GetAveragePricePositions(Side.Buy);
            _priceTPBuy = Math.Round((_averagePriceBuy + _averagePriceBuy * (double)_profit.ValueDecimal / 100) / (double)_tab.Security.PriceStep, MidpointRounding.AwayFromZero) * (double)_tab.Security.PriceStep;
            _averagePriceSell = GetAveragePricePositions(Side.Sell);
            _priceTPSell = Math.Round((_averagePriceSell - _averagePriceSell * (double)_profit.ValueDecimal / 100) / (double)_tab.Security.PriceStep, MidpointRounding.AwayFromZero) * (double)_tab.Security.PriceStep;

            if (_regime.ValueString == GetDescription(Regime.OnlyLong))
            {
                _checkExecuteOpenBuy = true;                
                RecoveryGrid(Side.Buy);
                RecoveryTpOrders(Side.Buy);
            }
            else if (_regime.ValueString == GetDescription(Regime.OnlyShort))
            {
                _checkExecuteOpenSell = true;
                RecoveryGrid(Side.Sell);
                RecoveryTpOrders(Side.Sell);
            }
            else
            {
                _checkExecuteOpenBuy = true;
                _checkExecuteOpenSell = true;
                RecoveryGrid(Side.Buy);
                RecoveryGrid(Side.Sell);
                RecoveryTpOrders(Side.Buy);
                RecoveryTpOrders(Side.Sell);
            }
                        
            _workCycle = true;
            _status = Status.Work;
        }

        private void RecoveryGrid(Side side)
        {
            SendNewLogMessage("Восстанавливаем сетку после приостановки торговли " + side + ", Текущая цена: " + _lastPrice, Logging.LogMessageType.User);

            List<Position> pos = new List<Position>();

            if (side == Side.Buy)
            {
                pos = _tab.PositionOpenLong;
            }
            else
            {
                pos = _tab.PositionOpenShort;
            }

            if (pos.Count > 0)
            {
                if (pos[0].OpenOrders != null && pos[0].OpenOrders.Count > 0)
                {
                    CheckCloseOrders(pos);
                }
            }
            else
            {
                if (side == Side.Buy)
                {
                    _tryOpenOrderBuy = true;
                }
                else
                {
                    _tryOpenOrderSell = true;
                }
            }

            SendNewLogMessage("Восстановление сетки закончено", Logging.LogMessageType.User);
        }

        private void RecoveryTpOrders(Side side)
        {
            List<Position> pos = _tab.PositionOpenShort;
            decimal price = (decimal)_priceTPSell;

            if (side == Side.Buy)
            {
                pos = _tab.PositionOpenLong;
                price = (decimal)_priceTPBuy;
            }
           
            if (pos.Count == 0)
            {
                return;
            }

            decimal volume = pos[0].OpenVolume;

            if (volume > 0)
            {
                for (int j = 0; j < pos[0].CloseOrders.Count; j++)
                {
                    for (int i = 0; i < pos[0].CloseOrders.Count; i++)
                    {
                        if (pos[0].CloseOrders[i].State == OrderStateType.Active ||
                        pos[0].CloseOrders[i].State == OrderStateType.Partial ||
                        pos[0].CloseOrders[i].State == OrderStateType.Pending)
                        {
                            SendNewLogMessage("Удаляем закрывающий ордер: " + pos[0].CloseOrders[i].Side + " " + pos[0].CloseOrders[i].Price + " - " + pos[0].CloseOrders[i].Volume + ", State: " + pos[0].CloseOrders[i].State, Logging.LogMessageType.User);
                            _tab.CloseOrder(pos[0].CloseOrders[i]);
                        }
                    }
                }                    
            }

            _tab.CloseAtLimitUnsafe(pos[0], price, volume);
            SendNewLogMessage("Ставим новый закрывающий ордер", Logging.LogMessageType.User);
        }

        private void CheckCloseOrders(List<Position> pos)
        {
            if (pos[0].CloseOrders != null)
            {
                for (int j = 0; j < pos[0].CloseOrders.Count; j++)
                {
                    if (pos[0].CloseOrders[j].State == OrderStateType.Done)
                    {
                        for (int i = 0; i < pos[0].OpenOrders.Count; i++)
                        {
                            if (pos.Last().OpenOrders[i].State == OrderStateType.Activ ||
                                pos.Last().OpenOrders[i].State == OrderStateType.Active ||
                                pos.Last().OpenOrders[i].State == OrderStateType.Patrial ||
                                pos.Last().OpenOrders[i].State == OrderStateType.Partial ||
                                pos.Last().OpenOrders[i].State == OrderStateType.Pending ||
                                pos.Last().OpenOrders[i].State == OrderStateType.None)
                            {
                                SendNewLogMessage("Закрывающий ордер Done, закрываем открытый ордер " + pos[0].Direction + ": Price = " + pos.Last().OpenOrders[i].Price + ", Vol = " + pos.Last().OpenOrders[i].Volume + ", State: " + pos[0].OpenOrders[i].State, Logging.LogMessageType.User);
                                _tab.CloseOrder(pos.Last().OpenOrders[i]);
                            }
                        }

                        if (pos[0].Direction == Side.Buy)
                        {
                            _tryOpenOrderBuy = true;
                        }
                        else
                        {
                            _tryOpenOrderSell = true;
                        }

                        return;
                    }
                }
            }

            CheckOpenOrders(pos);
        }

        private void CheckOpenOrders(List<Position> pos)
        {      
            int count = 0;

            for (int i = 0; i < pos[0].OpenOrders.Count; i++)
            {
                if (pos[0].OpenOrders[i].State == OrderStateType.Active ||
                    pos[0].OpenOrders[i].State == OrderStateType.Activ)// считаем кол-во открытых ордеров
                {
                    count++;
                }
            }            

            SendNewLogMessage("Кол-во ордеров на бирже " + pos[0].Direction + ": " + count, Logging.LogMessageType.User);
            
            if (count < _countOrdersOnExchange.ValueInt) // если ордеров на бирже меньше чем нужно, выставляем дополнительные
            {
                for (int i = 0; i < _countOrdersOnExchange.ValueInt - count; i++)
                {
                    if (pos[0].Direction == Side.Buy)
                    {
                        if (_listOrdersBuy.Count > 0)
                        {
                            if (_listOrdersBuy[0].Price >= _lastPrice)
                            {
                                decimal price = _lastPrice + _tab.Security.PriceStep * 100;
                                _tab.BuyAtLimitToPositionUnsafe(pos[0], price, _listOrdersBuy[0].Volume);
                                SendNewLogMessage("Выставляем из сетки не исполненный ордер Buy на биржу по рынку. " + "Price: " + price + "Volume: " + _listOrdersBuy[0].Volume, Logging.LogMessageType.User);
                                _listOrdersBuy.RemoveAt(0);
                                i--;
                            }
                            else
                            {
                                _tab.BuyAtLimitToPositionUnsafe(pos[0], _listOrdersBuy[0].Price, _listOrdersBuy[0].Volume);
                                SendNewLogMessage("Выставляем ордер Buy на биржу. Price: " + _listOrdersBuy[0].Price + ", volume: " + _listOrdersBuy[0].Volume, Logging.LogMessageType.User);
                                _listOrdersBuy.RemoveAt(0);

                                _checkExecuteOpenBuy = true;
                            }
                        }
                    }
                    else
                    {
                        if (_listOrdersSell.Count > 0)
                        {
                            if (_listOrdersSell[i].Price <= _lastPrice)
                            {
                                decimal price = _lastPrice - _tab.Security.PriceStep * 100;
                                _tab.SellAtLimitToPositionUnsafe(pos[0], price, _listOrdersSell[0].Volume);
                                SendNewLogMessage("Выставляем из сетки не исполненный ордер Sell на биржу по рынку. " + "Price: " + price + "Volume: " + _listOrdersSell[0].Volume, Logging.LogMessageType.User);
                                _listOrdersSell.RemoveAt(0);
                                i--;
                            }
                            else
                            {
                                _tab.SellAtLimitToPositionUnsafe(pos[0], _listOrdersSell[0].Price, _listOrdersSell[0].Volume);
                                SendNewLogMessage("Выставляем ордер Sell на биржу. Price: " + _listOrdersSell[0].Price + ", volume: " + _listOrdersSell[0].Volume, Logging.LogMessageType.User);
                                _listOrdersSell.RemoveAt(0);

                                _checkExecuteOpenSell = true;
                            }
                        }
                    }
                }

                SaveRecoveryGrid();
                WriteLogOrders();
            }           
        }

        private void StartGrid()
        {
            if (_lastPrice == 0)
            {
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
                SendNewLogMessage("Не введены необходимые данные для работы робота", Logging.LogMessageType.Error);
                _status = Status.NoWork;
                _workCycle = false;
                return;
            }

            if (!CheckRSI())
            {
                return;
            }

            SendNewLogMessage("Значение RSI при первом размещении сетки: " + _lastRsi, Logging.LogMessageType.User);

            _listOrdersBuy.Clear();
            _listOrdersSell.Clear();

            if (_loadGrig.ValueBool)
            {
                if (_regime.ValueString == GetDescription(Regime.OnlyLong))
                {
                    _listOrdersBuy = LoadSampleToGrid(Side.Buy);
                }
                else if (_regime.ValueString == GetDescription(Regime.OnlyShort))
                {
                    _listOrdersSell = LoadSampleToGrid(Side.Sell);
                }
                else
                {
                    _listOrdersBuy = LoadSampleToGrid(Side.Buy);
                    _listOrdersSell = LoadSampleToGrid(Side.Sell);
                }                
            }
            else
            {         
                if (_regime.ValueString == GetDescription(Regime.OnlyLong))
                {
                    _listOrdersBuy = CalculateGrid(Side.Buy);
                }
                else if (_regime.ValueString == GetDescription(Regime.OnlyShort))
                {
                    _listOrdersSell = CalculateGrid(Side.Sell);
                }
                else
                {
                    _listOrdersBuy = CalculateGrid(Side.Buy);
                    _listOrdersSell = CalculateGrid(Side.Sell);
                }                
            }
                        
            WriteLogOrders();

            if (_regime.ValueString == GetDescription(Regime.LongShort))
            {
                _checkExecuteOpenBuy = true;
                _checkExecuteOpenSell = true;
                PlacingOrders(Side.None);                
            }
            else if (_regime.ValueString == GetDescription(Regime.OnlyLong))
            {
                _checkExecuteOpenBuy = true;
                PlacingOrders(Side.Buy);
            }
            else
            {
                _checkExecuteOpenSell = true;
                PlacingOrders(Side.Sell);                
            }
        }

        private bool CheckRSI()
        {
            decimal rsi = _lastRsi;

            if (_rsiOnOff.ValueString == GetDescription(RsiRegime.Off))
            {
                return true;
            }

            if (_choiseRSI.ValueString == GetDescription(ChoiseRSI.Second))
            {
                rsi = _lastRsiSecond;
            }

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
                case "=":
                    if (rsi == _valueRsi.ValueInt)
                    {
                        return true;
                    }
                    break;                                        
            }
            return false;
        }

        private void CheckExecuteOpenOrder(Side side)
        {
            List<Position> pos;

            if (side == Side.Buy)
            {
                if (!_checkExecuteOpenBuy) return;
                pos = _tab.PositionOpenLong;
            }
            else
            {
                if (!_checkExecuteOpenSell) return;
                pos = _tab.PositionOpenShort;
            }

            if (pos == null || pos.Count == 0) return;

            //PlaceNewOrderFromGrid(side);

            decimal volume = 0;

            for (int i = 0; i < pos[0].OpenOrders.Count; i++)
            {
                if (pos[0].OpenOrders[i].State == OrderStateType.Done)
                {
                    volume += pos[0].OpenOrders[i].Volume;
                }
            }

            if (volume == 0) return;

            if (pos.Last().CloseOrders != null && pos.Last().CloseOrders.Count > 0)
            {
                if (volume == pos.Last().CloseOrders.Last().Volume)
                {
                    return;
                }
                else
                {
                    SendNewLogMessage("Объем открытых ордеров: " + volume, Logging.LogMessageType.User);

                    SendNewLogMessage("Исполнился открывающий ордер " + side.ToString(), Logging.LogMessageType.User);

                    for (int i = 0; i < pos[0].CloseOrders.Count; i++)
                    {
                        if (pos[0].CloseOrders[i].State == OrderStateType.Activ ||
                        pos[0].CloseOrders[i].State == OrderStateType.Active ||
                        pos[0].CloseOrders[i].State == OrderStateType.Patrial ||
                        pos[0].CloseOrders[i].State == OrderStateType.Partial ||
                        pos[0].CloseOrders[i].State == OrderStateType.Pending)
                        {
                            SendNewLogMessage("Удаляем закрывающий ордер: " + pos[0].CloseOrders[i].Side + " " + pos[0].CloseOrders[i].Price + " - " + pos[0].CloseOrders[i].Volume + ", State: " + pos[0].CloseOrders[i].State, Logging.LogMessageType.User);
                            _tab.CloseOrder(pos[0].CloseOrders[i]);
                        }
                    }           
                }
            }

            PlaceNewOrderFromGrid(side);

            Side sideClose = pos.Last().Direction == Side.Buy ? Side.Sell : Side.Buy;

            if (pos.Last().Direction == Side.Buy)
            {
                _averagePriceBuy = GetAveragePricePositions(side);
                if (_averagePriceBuy == 0)
                {
                    return;
                }
                _priceTPBuy = Math.Round((_averagePriceBuy + _averagePriceBuy * (double)_profit.ValueDecimal / 100) / (double)_tab.Security.PriceStep, MidpointRounding.AwayFromZero) * (double)_tab.Security.PriceStep;
                SendNewLogMessage("Цена TP ордера: " + _priceTPBuy, Logging.LogMessageType.User);
                SendNewLogMessage("Ставим новый закрывающий ордер: " + sideClose + " " + _priceTPBuy + " - " + volume, Logging.LogMessageType.User);

                _tab.CloseAtLimitUnsafe(pos.Last(), (decimal)_priceTPBuy, volume);

                _checkCloseOrderBuy = true;
            }
            else
            {
                _averagePriceSell = GetAveragePricePositions(side);
                if (_averagePriceSell == 0)
                {
                    return;
                }
                _priceTPSell = Math.Round((_averagePriceSell - _averagePriceSell * (double)_profit.ValueDecimal / 100) / (double)_tab.Security.PriceStep, MidpointRounding.AwayFromZero) * (double)_tab.Security.PriceStep;
                SendNewLogMessage("Цена TP ордера: " + _priceTPSell, Logging.LogMessageType.User);
                SendNewLogMessage("Ставим новый закрывающий ордер: " + sideClose + " " + _priceTPSell + " - " + volume, Logging.LogMessageType.User);

                _tab.CloseAtLimitUnsafe(pos.Last(), (decimal)_priceTPSell, volume);

                _checkCloseOrderSell = true;
            }    
        }

        private void PlaceNewOrderFromGrid(Side side)
        {
            if (side == Side.Buy)
            {
                /*if (_boolTimerTwapOrders)
                {
                    if (_timeTwapBuyOrders.AddMinutes((double)_setTimeTwapOrders.ValueDecimal) > DateTime.UtcNow) return;
                }*/

                if (_listOrdersBuy.Count == 0) return;

                int count = 0;

                for (int i = 0; i < _tab.PositionOpenLong[0].OpenOrders.Count; i++)
                {
                    if (_tab.PositionOpenLong[0].OpenOrders[i].State == OrderStateType.Activ ||
                        _tab.PositionOpenLong[0].OpenOrders[i].State == OrderStateType.Active ||
                        _tab.PositionOpenLong[0].OpenOrders[i].State == OrderStateType.None ||
                        _tab.PositionOpenLong[0].OpenOrders[i].State == OrderStateType.Pending ||
                        _tab.PositionOpenLong[0].OpenOrders[i].State == OrderStateType.Patrial ||
                        _tab.PositionOpenLong[0].OpenOrders[i].State == OrderStateType.Partial
                        )
                    {
                        count++;
                    }
                }

                if (count < _countOrdersOnExchange.ValueInt)
                {
                    SendNewLogMessage("Добавляем ордер на биржу из сетки: " + side + " " + _listOrdersBuy[0].Price + " - " + _listOrdersBuy[0].Volume, Logging.LogMessageType.User);
                    _tab.BuyAtLimitToPositionUnsafe(_tab.PositionOpenLong.Last(), _listOrdersBuy[0].Price, _listOrdersBuy[0].Volume);
                    _listOrdersBuy.RemoveAt(0);

                    /*if (_boolTimerTwapOrders)
                    {
                        _timeTwapBuyOrders = DateTime.UtcNow;
                        SendNewLogMessage($"Выставлен TWAP ордер Buy: {_timeTwapBuyOrders}, следующий ордер будет выставлен после {_timeTwapBuyOrders.AddMinutes((double)_setTimeTwapOrders.ValueDecimal)}", Logging.LogMessageType.User);
                    }*/
                }                               
            }
            else
            {
                /*if (_boolTimerTwapOrders)
                {
                    if (_timeTwapSellOrders.AddMinutes((double)_setTimeTwapOrders.ValueDecimal) > DateTime.UtcNow) return;
                }*/

                if (_listOrdersSell.Count == 0) return;

                int count = 0;
                for (int i = 0; i < _tab.PositionOpenShort[0].OpenOrders.Count; i++)
                {
                    if (_tab.PositionOpenShort[0].OpenOrders[i].State == OrderStateType.Activ ||
                        _tab.PositionOpenShort[0].OpenOrders[i].State == OrderStateType.Active ||
                        _tab.PositionOpenShort[0].OpenOrders[i].State == OrderStateType.None ||
                        _tab.PositionOpenShort[0].OpenOrders[i].State == OrderStateType.Pending ||
                        _tab.PositionOpenShort[0].OpenOrders[i].State == OrderStateType.Patrial ||
                        _tab.PositionOpenShort[0].OpenOrders[i].State == OrderStateType.Partial)
                    {
                        count++;
                    }
                }

                if (count < _countOrdersOnExchange.ValueInt)
                {
                    SendNewLogMessage("Добавляем ордер на биржу из сетки: " + side + " " + _listOrdersSell[0].Price + " - " + _listOrdersSell[0].Volume, Logging.LogMessageType.User);
                    _tab.SellAtLimitToPositionUnsafe(_tab.PositionOpenShort.Last(), _listOrdersSell[0].Price, _listOrdersSell[0].Volume);
                    _listOrdersSell.RemoveAt(0);

                    /*if (_boolTimerTwapOrders)
                    {
                        _timeTwapSellOrders = DateTime.UtcNow;
                        SendNewLogMessage($"Выставлен TWAP ордер Sell: {_timeTwapSellOrders}, следующий ордер будет выставлен после {_timeTwapSellOrders.AddMinutes((double)_setTimeTwapOrders.ValueDecimal)}", Logging.LogMessageType.User);
                    }*/
                }
            }

            SaveRecoveryGrid();
        }

        private void CheckExecuteCloseOrders(Side side)
        {
            List<Position> pos;

            if (side == Side.Buy)
            {
                if (!_checkCloseOrderBuy) return;

                pos = _tab.PositionOpenLong;
            }
            else
            {
                if (!_checkCloseOrderSell) return;

                pos = _tab.PositionOpenShort;
            }

            if (pos.Count == 0)
            {
				SendNewLogMessage("Нет позиций " + side + ". Запускаем новую сетку", Logging.LogMessageType.User);

				if (side == Side.Buy)
				{
					_checkCloseOrderBuy = false;
					_timeCycleBuy = DateTime.Now.AddMinutes((double)_delayAfterCycle.ValueDecimal);
					_tryOpenOrderBuy = true;
					_unrealizPnLBuy = 0;
					_averagePriceBuy = 0;
					_priceTPBuy = 0;
				}
				else
				{
					_checkCloseOrderSell = false;
					_timeCycleSell = DateTime.Now.AddMinutes((double)_delayAfterCycle.ValueDecimal);
					_tryOpenOrderSell = true;
					_unrealizPnLSell = 0;
					_averagePriceSell = 0;
					_priceTPSell = 0;
				}

				return;
            }

            if (pos.Last().CloseActiv)
            {
                return;
            }

            SendNewLogMessage("Исполнился закрывающий ордер # ", Logging.LogMessageType.User);

            if (side == Side.Buy)
            {
                _checkCloseOrderBuy = false;
                _timeCycleBuy = DateTime.Now.AddMinutes((double)_delayAfterCycle.ValueDecimal);
                _tryOpenOrderBuy = true;
                _unrealizPnLBuy = 0;
                _averagePriceBuy = 0;
                _priceTPBuy = 0;
            }
            else
            {
                _checkCloseOrderSell = false;
                _timeCycleSell = DateTime.Now.AddMinutes((double)_delayAfterCycle.ValueDecimal);
                _tryOpenOrderSell = true;
                _unrealizPnLSell = 0;
                _averagePriceSell = 0;
                _priceTPSell = 0;
            }

            if (pos.Last().OpenActiv)
            {
                CloseOpeningOrders(pos);
            }
        }

        private void CloseOpeningOrders(List<Position> pos)
        {
            if (pos.Count == 0) return;

            if (pos.Last().OpenOrders == null)
            {
                return;
            }

            for (int i = 0; i < pos.Last().OpenOrders.Count; i++)
            {
                if (pos.Last().OpenOrders[i].State == OrderStateType.Activ ||
                    pos.Last().OpenOrders[i].State == OrderStateType.Active ||
                    pos.Last().OpenOrders[i].State == OrderStateType.Patrial ||
                    pos.Last().OpenOrders[i].State == OrderStateType.Partial ||
                    pos.Last().OpenOrders[i].State == OrderStateType.Pending ||
                    pos.Last().OpenOrders[i].State == OrderStateType.None)
                {
                    SendNewLogMessage("Для выставления новой сетки удаляем открытый ордер " + pos.Last().Direction + ": Price = " + pos.Last().OpenOrders[i].Price + ", Vol = " + pos.Last().OpenOrders[i].Volume, Logging.LogMessageType.User);
                    _tab.CloseOrder(pos.Last().OpenOrders[i]);
                }
            }
        }

        private void PlaceOpenOrders(Side side)
        {
            if (!_workCycle)
            {
                return;
            }

            if (!CheckRSI())
            {
                return;
            }

            List<Position> pos;
                       
            if (side == Side.Buy)
            {
                if (_tryCloseRearrangeBuy) return;

                pos = _tab.PositionOpenLong;

                if (pos.Count != 0)
                {
                    return;
                }

                if (pos.Count == 0)
                {
                    _checkCloseOrderBuy = false;
                    _timeCycleBuy = DateTime.Now.AddMinutes((double)_delayAfterCycle.ValueDecimal);
                    _tryOpenOrderBuy = true;
                    _unrealizPnLBuy = 0;
                    _averagePriceBuy = 0;
                    _priceTPBuy = 0;
                }                    
            }
            else
            {
                if (_tryCloseRearrangeSell) return;

                pos = _tab.PositionOpenShort;

                if (pos.Count != 0)
                {
                    return;
                }

                if (pos.Count == 0)
                {
                    _checkCloseOrderSell = false;
                    _timeCycleSell = DateTime.Now.AddMinutes((double)_delayAfterCycle.ValueDecimal);
                    _tryOpenOrderSell = true;
                    _unrealizPnLSell = 0;
                    _averagePriceSell = 0;
                    _priceTPSell = 0;
                }
            }

            SendNewLogMessage("Значение RSI при размещении новой сетки: " + _lastRsi, Logging.LogMessageType.User);

            if (side == Side.Buy && 
                !_checkCloseOrderBuy &&
                _timeCycleBuy <= DateTime.Now)
            {
                _tryOpenOrderBuy = false;
                _listOrdersBuy.Clear();

                if (_loadGrig.ValueBool)
                {
                    _listOrdersBuy = LoadSampleToGrid(Side.Buy);
                }
                else
                {
                    _listOrdersBuy = CalculateGrid(Side.Buy);
                }

                WriteLogOrders();
                PlacingOrders(side);
            }

            if (side == Side.Sell && 
                !_checkCloseOrderSell &&
                _timeCycleSell <= DateTime.Now)
            {
                _tryOpenOrderSell = false;
                _listOrdersSell.Clear();

                if (_loadGrig.ValueBool)
                {
                    _listOrdersSell = LoadSampleToGrid(Side.Sell);
                }
                else
                {
                    _listOrdersSell = CalculateGrid(Side.Sell);
                }

                WriteLogOrders();
                PlacingOrders(side);
            }
        }

        private void TryRearrangeGrid()
        {
            CheckRearrangeGrid(Side.Buy);
            CloseOrdersForRearrangeGrid(Side.Buy);
            PlaceNewOrderRearrangeGrid(Side.Buy);
            CheckRearrangeGrid(Side.Sell);            
            CloseOrdersForRearrangeGrid(Side.Sell);            
            PlaceNewOrderRearrangeGrid(Side.Sell);
        }

        private void CheckRearrangeGrid(Side side)
        {
            if (!_workCycle)
            {
                return;
            }

            List<Position> pos = new List<Position>();

            if (side == Side.Buy)
            {
                if (_tryCloseRearrangeBuy) return;
                pos = _tab.PositionOpenLong;
            }
            else
            {
                if (_tryCloseRearrangeSell) return;
                pos = _tab.PositionOpenShort;
            }

            if (pos.Count == 0)
            {
                return;
            }

            if (pos.Last().CloseActiv)
            {
                return;
            }

            decimal priceOrder = 0;

            for (int i = 0; i < pos.Last().OpenOrders.Count; i++)
            {
                if (pos[0].OpenOrders[i].State == OrderStateType.None)
                {
                    return;
                }

                if (i == 0)
                {
                    priceOrder = pos.Last().OpenOrders[i].Price;
                }
                else
                {
                    if (side == Side.Buy)
                    {
                        if (priceOrder < pos.Last().OpenOrders[i].Price)
                        {
                            priceOrder = pos.Last().OpenOrders[i].Price;
                        }
                    }
                    else
                    {
                        if (priceOrder > pos.Last().OpenOrders[i].Price)
                        {
                            priceOrder = pos.Last().OpenOrders[i].Price;
                        }
                    }
                }
            }

            if (priceOrder != 0)
            {
                if(side == Side.Buy)
                {
                    if(_lastPrice > priceOrder + priceOrder * _rearrangeGrid.ValueDecimal / 100) // проверяем ушла ли цена для подтяжки сетки
                    {                        
                        if (_needRearrangeBuy)
                        {
                            return;
                        }
                        _needRearrangeBuy = true;
                        _timeRearrangeBuy = DateTime.Now.AddMinutes((double)_delayRearrange.ValueDecimal);
                        SendNewLogMessage("Требуется подтяжка сетки Buy", Logging.LogMessageType.User);
                    }
                    else
                    {
                        if (!_needRearrangeBuy)
                        {
                            return;
                        }
                        _needRearrangeBuy = false;
                        SendNewLogMessage("Подтяжка сетки Buy не требуется", Logging.LogMessageType.User);
                    }
                }
                else
                {
                    if (_lastPrice < priceOrder - priceOrder * _rearrangeGrid.ValueDecimal / 100)
                    {
                        if (_needRearrangeSell)
                        {
                            return;
                        }
                        _needRearrangeSell = true;
                        _timeRearrangeSell = DateTime.Now.AddMinutes((double)_delayRearrange.ValueDecimal);
                        SendNewLogMessage("Требуется подтяжка сетки Sell", Logging.LogMessageType.User);
                    }
                    else
                    {
                        if (!_needRearrangeSell)
                        {
                            return;
                        }
                        _needRearrangeSell = false;
                        SendNewLogMessage("Подтяжка сетки Sell не требуется", Logging.LogMessageType.User);
                    }
                }
            }
        }

        private bool _tryCloseRearrangeBuy = false;
        private bool _tryCloseRearrangeSell = false;

        private void CloseOrdersForRearrangeGrid(Side side)
        {            
            List<Position> pos = new List<Position>();

            if (side == Side.Buy)
            {                
                if (!_needRearrangeBuy) return;
                if (_timeRearrangeBuy > DateTime.Now) return;

                pos = _tab.PositionOpenLong;                
            }
            else
            {
                if (!_needRearrangeSell) return;
                if (_timeRearrangeSell > DateTime.Now) return;

                pos = _tab.PositionOpenShort;                
            }

            if (pos.Count == 0)
            {
                return;
            }

            for (int i = 0; i < pos[0].OpenOrders.Count; i++)
            {
                if (pos[0].OpenOrders[i].State == OrderStateType.Activ ||
                    pos[0].OpenOrders[i].State == OrderStateType.Active ||
                    pos[0].OpenOrders[i].State == OrderStateType.Partial ||
                    pos[0].OpenOrders[i].State == OrderStateType.Patrial ||
                    pos[0].OpenOrders[i].State == OrderStateType.Pending)
                {
                    SendNewLogMessage("Для подтяжки сетки удаляем открытый ордер " + pos[0].Direction + ": Price = " + pos[0].OpenOrders[i].Price + ", Vol = " + pos[0].OpenOrders[i].Volume, Logging.LogMessageType.User);
                    _tab.CloseOrder(pos[0].OpenOrders[i]);                    
                }                
            }

            if (side == Side.Buy)
            {
                _tryCloseRearrangeBuy = true;
                _needRearrangeBuy = false;
            }
            else
            {
                _tryCloseRearrangeSell = true;
                _needRearrangeSell = false;
            }
        }

        private void PlaceNewOrderRearrangeGrid(Side side)
        {
            if (!CheckRSI())
            {
                return;
            }

            List<Position> pos = new List<Position>();

            if (side == Side.Buy)
            {
                if (!_tryCloseRearrangeBuy) return;

                pos = _tab.PositionOpenLong;
            }
            else
            {
                if (!_tryCloseRearrangeSell) return;

                pos = _tab.PositionOpenShort;
            }

            if (pos.Count > 0) return;

            SendNewLogMessage("Значение RSI при подтяжки сетки: " + _lastRsi, Logging.LogMessageType.User);

            SendNewLogMessage("Подтяжка сетки: " + side, Logging.LogMessageType.User);

            if (side == Side.Buy)
            {
                _listOrdersBuy.Clear();
                if (_loadGrig.ValueBool)
                {
                    _listOrdersBuy = LoadSampleToGrid(Side.Buy);
                }
                else
                {
                    _listOrdersBuy = CalculateGrid(side);
                }
            }
            else
            {
                _listOrdersSell.Clear();

                if (_loadGrig.ValueBool)
                {
                    _listOrdersSell = LoadSampleToGrid(Side.Sell);
                }
                else
                {
                    _listOrdersSell = CalculateGrid(side);
                }
            }

            WriteLogOrders();
            PlacingOrders(side);

            if (side == Side.Buy)
            {
                _tryCloseRearrangeBuy = false;
            }
            else
            {
                _tryCloseRearrangeSell = false;
            }
        }

        private double _averagePriceBuy = 0;
        private double _priceTPBuy = 0;
        private double _averagePriceSell = 0;
        private double _priceTPSell = 0;
                
        private double GetAveragePricePositions(Side side)
        {
            try
            {
                if (side == Side.Buy && _tab.PositionOpenLong.Count == 0)
                {
                    return 0;
                }
                if (side == Side.Sell &&
                    _tab.PositionOpenShort.Count == 0)
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

                decimal amountOfCost = 0;
                decimal quantityVolume = 0;
                decimal averagePrice = 0;

                for (int i = 0; i < pos.OpenOrders.Count; i++)
                {
                    quantityVolume += pos.OpenOrders[i].VolumeExecute;
                    amountOfCost += pos.OpenOrders[i].VolumeExecute * pos.OpenOrders[i].Price;
                }

                if (amountOfCost != 0 ||
                    quantityVolume != 0)
                {
                    averagePrice = amountOfCost / quantityVolume;
                }
            
                return Math.Round((double)averagePrice, _tab.Security.Decimals, MidpointRounding.AwayFromZero);
            }
            catch (Exception e)
            {
                SendNewLogMessage(e.ToString(), Logging.LogMessageType.Error);
                return 0;
            }            
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
                SendNewLogMessage("Не введены необходимые данные для расчета сетки", Logging.LogMessageType.Error);
                _status = Status.NoWork;
                return list;
            }

            if (listOrders.Count != listVolume.Count)
            {
                SendNewLogMessage("Ошибка в расчетах сетки. Количество уровней цен несоответствует количеству уровней объемов", Logging.LogMessageType.Error);
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
                    SendNewLogMessage("Рассчитанный объем меньше минимального торгуемого объема. Измените настройки сетки.", Logging.LogMessageType.Error);
                    _status = Status.NoWork;
                    return new List<ListOrders>();
                }
                list.Add(item);
            }
            
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

            if (_regime.ValueString == GetDescription(Regime.LongShort))
            {
                generalVolume = generalVolume / 2;
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

            if (_regime.ValueString == GetDescription(Regime.LongShort))
            {
                generalVolume = generalVolume / 2;
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
                    decimal calc = firstListVolume[firstListVolume.Count - 1] + firstListVolume[firstListVolume.Count - 1] * _percentMartingale.ValueDecimal / 100;
                    firstListVolume.Add(calc);
                }

                sum += listOrders[i] * firstListVolume[i];
            }

            // пересчитываем объем с учетом величины депозита
            decimal scale = generalVolume / sum;

            SendNewLogMessage($"Размер депозита для торговли: {generalVolume}, сумма для сетки из расчета минимального ордера: {sum}, множитель сетки: {scale}", Logging.LogMessageType.User);

            List<decimal> secondListVolume = new List<decimal>();

            for (int i = 0; i < firstListVolume.Count; i++)
            {
                decimal scaleVolume = Math.Round(firstListVolume[i] * scale / _tab.Security.Lot, _tab.Security.DecimalsVolume, MidpointRounding.ToNegativeInfinity) * _tab.Security.Lot;
                decimal truncateVolume = Math.Floor(scaleVolume / _tab.Security.VolumeStep) * _tab.Security.VolumeStep;
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

        private void WriteLogOrders()
        {
            string str = "Текущая цена: " + _lastPrice + "\n";
            decimal countBuy = 0;
            decimal countSell = 0;

            for (int i = _listOrdersSell.Count - 1; i >= 0; i--)
            {
                str += _listOrdersSell[i].Price + ", Sell, " + _listOrdersSell[i].Volume + "\n";
                countSell += _listOrdersSell[i].Price * _listOrdersSell[i].Volume;
            }
            for (int i = 0; i < _listOrdersBuy.Count; i++)
            {
                str += _listOrdersBuy[i].Price + ", Buy, " + _listOrdersBuy[i].Volume + "\n";
                countBuy += _listOrdersBuy[i].Price * _listOrdersBuy[i].Volume;
            }

            str += "Сумма ордеров Buy: " + countBuy + ", сумма ордеров sell: " + countSell;
            SendNewLogMessage(str, Logging.LogMessageType.User);            
        }

        /// <summary>
        /// Если Side == None, значит размещаем ордера на покупку и на продажу.
        /// Если указана сторона, значит размещаем ордера только этой стороны
        /// </summary>
        private void PlacingOrders(Side side)
        {
            if (side == Side.None)
            {
                SendBuyOrder();
                SendSellOrder();                
            }
            else if (side == Side.Buy)
            {
                SendBuyOrder();
            }
            else
            {
                SendSellOrder();
            }
        }

        private void SendSellOrder()
        {
            if (_listOrdersSell != null)
            {
                
                int count = 1;

                for (int i = 0; i < _listOrdersSell.Count; i++)
                {
                    if (count > _countOrdersOnExchange.ValueInt)
                    {
                        break;
                    }                    

                    SendNewLogMessage("Ставим новый ордер из сетки: " + _listOrdersSell[i].Price + " " + Side.Sell + " - " + _listOrdersSell[i].Volume, Logging.LogMessageType.User);

                    if (_tab.PositionOpenShort.Count == 0)
                    {
                        _tab.SellAtLimit(_listOrdersSell[i].Volume, _listOrdersSell[i].Price);
                    }                   
                    else
                    {
                        _tab.SellAtLimitToPositionUnsafe(_tab.PositionOpenShort.Last(), _listOrdersSell[i].Price, _listOrdersSell[i].Volume);                        
                    }
                    _listOrdersSell.RemoveAt(i);
                    i--;
                    count++;

                    /*if (_boolTimerTwapOrders)
                    {
                        _timeTwapSellOrders = DateTime.UtcNow;
                        SendNewLogMessage($"Установлено время TWAP ордера Sell: {_timeTwapSellOrders}, следующий ордер будет выставлен после {_timeTwapSellOrders.AddMinutes((double)_setTimeTwapOrders.ValueDecimal)}", Logging.LogMessageType.User);
                        break;
                    }*/
                }

                SaveRecoveryGrid();

                _checkExecuteOpenSell = true;
            }
        }

        private void SendBuyOrder()
        {
            if (_listOrdersBuy != null)
            {
                int count = 1;

                for (int i = 0; i < _listOrdersBuy.Count; i++)
                {
                    if (count > _countOrdersOnExchange.ValueInt)
                    {
                        break;
                    }                    

                    SendNewLogMessage("Ставим новый ордер из сетки: " + _listOrdersBuy[i].Price + " " + Side.Buy + " - " + _listOrdersBuy[i].Volume, Logging.LogMessageType.User);

                    if (_tab.PositionOpenLong.Count == 0)
                    {
                        _tab.BuyAtLimit(_listOrdersBuy[i].Volume, _listOrdersBuy[i].Price);
                    } 
                    else
                    {
                        _tab.BuyAtLimitToPositionUnsafe(_tab.PositionOpenLong.Last(), _listOrdersBuy[i].Price, _listOrdersBuy[i].Volume);
                    }
					
					_listOrdersBuy.RemoveAt(i);
                    i--;
                    count++;

                    /*if (_boolTimerTwapOrders)
                    {
                        _timeTwapBuyOrders = DateTime.UtcNow;
                        SendNewLogMessage($"Установлено время TWAP ордера Buy: {_timeTwapBuyOrders}, следующий ордер будет выставлен после {_timeTwapBuyOrders.AddMinutes((double)_setTimeTwapOrders.ValueDecimal)}", Logging.LogMessageType.User);
                        break;
                    }*/
                }
				SaveRecoveryGrid();
				_checkExecuteOpenBuy = true;
            }
        }

        private void CancelAllOrders()
        {
            DialogResult result = MessageBox.Show("Остановить бота?", "Сообщение", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

            _checkExecuteOpenBuy = false;
            _checkExecuteOpenSell = false;
            _needRearrangeBuy = false;
            _needRearrangeSell = false;
            _workCycle = false;

            for (int i = 0; i < _tab.PositionsOpenAll.Count; i++)
            {

                for (int j = 0; j < _tab.PositionsOpenAll[i].OpenOrders.Count; j++)
                {
                    if (_tab.PositionsOpenAll[i].OpenOrders[j].State == OrderStateType.Active ||
                        _tab.PositionsOpenAll[i].OpenOrders[j].State == OrderStateType.Activ)
                    {
                        _tab.CloseOrder(_tab.PositionsOpenAll[i].OpenOrders[j]);
                    }
                }
            }

            switch (result)
            {
                case DialogResult.Yes:
                    _checkCloseOrderBuy = false;
                    _checkCloseOrderSell = false;
                    _status = Status.NoWork;
                    break;

                case DialogResult.No:
                    _status = Status.Work;
                    _workCycle = true;
                    _checkCloseOrderBuy = true;
                    _checkCloseOrderSell = true;
                    break;

                default:

                    break;
            }
        }

        private void CloseOrdersOnMarket()
        {
            DialogResult result = MessageBox.Show("Остановить бота?", "Сообщение", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

            for (int i = 0; i < _tab.PositionsOpenAll.Count; i++)
            {
                if (_tab.PositionsOpenAll[i].CloseOrders != null &&
                    _tab.PositionsOpenAll[i].CloseOrders.Count > 0)
                {
                    _tab.CloseOrder(_tab.PositionsOpenAll[i].CloseOrders.Last());

                    decimal price = _lastPrice + _tab.Security.PriceStep * 100;

                    if (_tab.PositionsOpenAll[i].Direction == Side.Buy)
                    {
                        price = _lastPrice - _tab.Security.PriceStep * 100;
                        _checkCloseOrderBuy = true;
                    }
                    else
                    {
                        _checkCloseOrderSell = true;
                    }

                    _tab.CloseAtLimitUnsafe(_tab.PositionsOpenAll[i], price, _tab.PositionsOpenAll[i].OpenVolume);
                }
            }

            switch (result)
            {
                case DialogResult.Yes:
                    _checkCloseOrderBuy = false;
                    _checkCloseOrderSell = false;
                    _workCycle = false;
                    _checkExecuteOpenBuy = false;
                    _checkExecuteOpenSell = false;
                    _tryOpenOrderBuy = false;
                    _tryOpenOrderSell = false;
                    _status = Status.NoWork;

                    _needRearrangeBuy = false;
                    _needRearrangeSell = false;

                    for (int i = 0; i < _tab.PositionsOpenAll.Count; i++)
                    {
                        _tab.CloseAllOrderToPosition(_tab.PositionsOpenAll[i]);
                    }

                    break;

                case DialogResult.No:
                    _checkExecuteOpenBuy = true;
                    _checkExecuteOpenSell = true;
                    _tryOpenOrderBuy = false;
                    _tryOpenOrderSell = false;
                    _status = Status.Work;
                    _workCycle = true;
                    break;

                default:

                    break;
            }
        }

        #endregion

        #region Descriptions

        private enum Status
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
            [Description("Длинные и короткие позиции")]
            LongShort,
            [Description("Только длинные позиции")]
            OnlyLong,
            [Description("Только короткие позиции")]
            OnlyShort
        }

        private enum TypeVolume
        {
            [Description("Фиксированный")]
            Fix,
            [Description("% от депозита")]
            Percent
        }

        private enum RsiRegime
        {
            On,
            Off
        }

        private enum RsiOperation
        {
            [Description("=")]
            Еquals,
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

