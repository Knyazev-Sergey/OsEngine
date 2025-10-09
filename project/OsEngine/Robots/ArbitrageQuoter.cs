using OsEngine.Entity;
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
using System.Drawing;
using System.Globalization;

namespace OsEngine.Robots
{
    [Bot("ArbitrageQuoter")]
    public class ArbitrageQuoter : BotPanel
    {
        private BotTabSimple _tab0;
        private BotTabSimple _tab1;
        private BotTabSimple _tab2;
        private BotTabSimple _tab3;
        private BotTabSimple _firstTab;
        private BotTabSimple _secondTab;
        private StrategyParameterString _regime1;
        private string _regime;
        private string _exercise;
        private string _schemeOrderSettings;
        private string _typeSpread = TypeSpread.Percent.ToString();
        private decimal _setSpread;
        private string _compareSpread;
        private decimal _currentSpread;
        private decimal _firstBestBid;
        private decimal _firstBestAsk;
        private decimal _secondBestBid;
        private decimal _secondBestAsk;
        private decimal _firstLimitVolume;
        private decimal _secondLimitVolume;
        private decimal _firstLimitVolumeTable;
        private decimal _secondLimitVolumeTable;
        private decimal _maxVolume;
        private string _firstTypeLimit = TypeLimit.USDT.ToString();
        private string _secondTypeLimit = TypeLimit.USDT.ToString();
        private decimal _longСomissionTaker;
        private decimal _shortСomissionTaker;
        private decimal _longСomissionMaker;
        private decimal _shortСomissionMaker;
        private decimal _firstStepOrder;
        private decimal _secondStepOrder;
        private DateTime _timeExecute;
        private string _firstTypeStepOrder;
        private string _secondTypeStepOrder;
        private bool _firstChangePos;
        private bool _secondChangePos;

        public ArbitrageQuoter(string name, StartProgram startProgram) : base(name, startProgram)
        {
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");

            TabCreate(BotTabType.Simple);
            _tab0 = TabsSimple[0];
            TabCreate(BotTabType.Simple);
            _tab1 = TabsSimple[1];
            TabCreate(BotTabType.Simple);
            _tab2 = TabsSimple[2];
            TabCreate(BotTabType.Simple);
            _tab3 = TabsSimple[3];

            _regime1 = CreateParameter("", "", " ");

            this.ParamGuiSettings.Title = "Arbitrage Quoter";
            this.ParamGuiSettings.Height = 750;
            this.ParamGuiSettings.Width = 1200;

            _regime = GetDescription(Regime.Off);

            CustomTabToParametersUi customTabOpen = ParamGuiSettings.CreateCustomTab(" Торговля ");

            CreateTableOpen();
            customTabOpen.AddChildren(_hostTableOpen);

            _tab0.ManualPositionSupport.DisableManualSupport();
            _tab1.ManualPositionSupport.DisableManualSupport();
            _tab2.ManualPositionSupport.DisableManualSupport();
            _tab3.ManualPositionSupport.DisableManualSupport();


            _tab0.SecuritySubscribeEvent += TabsSecuritySubscribeEvent;
            _tab1.SecuritySubscribeEvent += TabsSecuritySubscribeEvent;
            _tab2.SecuritySubscribeEvent += TabsSecuritySubscribeEvent;
            _tab3.SecuritySubscribeEvent += TabsSecuritySubscribeEvent;

            LoadSettings();

            Thread worker = new Thread(ThreadRefreshTable) { IsBackground = true };
            worker.Start();

            Thread trade = new Thread(TradeLogic) { IsBackground = true };
            trade.Start();
        }

        private void TabsSecuritySubscribeEvent(Security obj)
        {
            if (MainWindow.GetDispatcher.CheckAccess() == false)
            {
                MainWindow.GetDispatcher.Invoke(new Action<Security>(TabsSecuritySubscribeEvent), obj);

                return;
            }
            try
            {
                DataGridViewComboBoxCell cell = _gridSettingsPosition.Rows[3].Cells[1] as DataGridViewComboBoxCell;

                if (!cell.Items.Contains(obj.Exchange + "/" + obj.Name))
                {
                    cell.Items.Add(obj.Exchange + "/" + obj.Name);
                }

                DataGridViewComboBoxCell cell1 = _gridSettingsPosition.Rows[3].Cells[2] as DataGridViewComboBoxCell;

                if (!cell1.Items.Contains(obj.Exchange + "/" + obj.Name))
                {
                    cell1.Items.Add(obj.Exchange + "/" + obj.Name);
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        public override string GetNameStrategyType()
        {
            return "ArbitrageQuoter";
        }

        public override void ShowIndividualSettingsDialog()
        {
        }

        #region Отрисовка таблиц 

        private WindowsFormsHost _hostTableOpen;
        private DataGridView _gridSchemePosition;
        private DataGridView _gridSettingsPosition;
        private DataGridView _gridResultPosition;
        private List<string> _listExercise;
        private List<string> _listScheme;
        private List<string> _listTypelimit;
        private List<string> _listTypeSpread;
        private List<string> _listTypeStepOrder;
        private List<string> _listRegime;
        private List<string> _listCompareSpread;

        private void CreateTableOpen()
        {
            _hostTableOpen = new WindowsFormsHost();

            _gridSettingsPosition = GridSettingsPosition();
            _gridSchemePosition = GridSchemePosition();
            _gridResultPosition = GridResultPosition();

            _gridSettingsPosition.CurrentCellDirtyStateChanged += _gridSettingsPosition_CurrentCellDirtyStateChanged;
            _gridSettingsPosition.CellValueChanged += __gridSettingsSetPosition_CellValueChanged;
            _gridSettingsPosition.CellBeginEdit += _gridSettingsSetPosition_CellBeginEdit;
            _gridSettingsPosition.DataError += __gridSettingsSetPosition_DataError;
            _gridSettingsPosition.CellPainting += __gridSettingsSetPosition_CellPainting;

            TableLayoutPanel panelSettingsScheme = new TableLayoutPanel();
            panelSettingsScheme.Dock = DockStyle.Fill;
            panelSettingsScheme.ColumnCount = 2;
            panelSettingsScheme.RowCount = 1;
            panelSettingsScheme.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panelSettingsScheme.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panelSettingsScheme.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            panelSettingsScheme.BackColor = Color.Black;
            panelSettingsScheme.Controls.Add(_gridSettingsPosition, 0, 0);
            panelSettingsScheme.Controls.Add(_gridSchemePosition, 1, 0);

            TableLayoutPanel panelUpDown = new TableLayoutPanel();
            panelUpDown.Dock = DockStyle.Fill;
            panelUpDown.ColumnCount = 1;
            panelUpDown.RowCount = 2;
            panelUpDown.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
            panelUpDown.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            panelUpDown.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            panelUpDown.BackColor = Color.Black;
            panelUpDown.Controls.Add(panelSettingsScheme, 0, 0);
            panelUpDown.Controls.Add(_gridResultPosition, 0, 1);

            _hostTableOpen.Child = panelUpDown;
        }

        private DataGridView GridSettingsPosition()
        {
            try
            {
                DataGridView newGrid =
               DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect,
               DataGridViewAutoSizeRowsMode.AllCells);

                newGrid.Dock = DockStyle.Fill;
                newGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                newGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

                newGrid.Columns.Add("Col1", "Настройки");
                newGrid.Columns.Add("Col2", "");
                newGrid.Columns.Add("Col3", "");

                newGrid.Columns[0].ReadOnly = true;

                foreach (DataGridViewColumn column in newGrid.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }

                // 0 Исполнение задания
                _listExercise = new List<string> { GetDescription(Exercise.Open), GetDescription(Exercise.Close), GetDescription(Exercise.Change) };
                _listRegime = new List<string> { GetDescription(Regime.Off), GetDescription(Regime.On), GetDescription(Regime.Pause) };

                DataGridViewRow newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Исполнение задания" });
                newRow.Cells.Add(new DataGridViewComboBoxCell() { DataSource = _listExercise, Value = GetDescription(Exercise.Open) });
                newRow.Cells.Add(new DataGridViewComboBoxCell() { DataSource = _listRegime, Value = _listRegime[0] });

                newGrid.Rows.Add(newRow);

                // 1 Сторона
                newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Сторона" });
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Long" });
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Short" });
                newGrid.Rows.Add(newRow);

                // 2 Изменение позиции
                newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Изменение позиции" });
                newRow.Cells.Add(new DataGridViewCheckBoxCell() { Value = false });
                newRow.Cells.Add(new DataGridViewCheckBoxCell() { Value = false });
                newGrid.Rows.Add(newRow);

                // 3 Площадка
                newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Площадка" });
                newRow.Cells.Add(new DataGridViewComboBoxCell());
                newRow.Cells.Add(new DataGridViewComboBoxCell());
                newGrid.Rows.Add(newRow);

                // 4 Инструмент
                newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Инструмент" });
                newGrid.Rows.Add(newRow);

                // 5 Тип лимита
                _listTypelimit = new List<string> { TypeLimit.USDT.ToString(), TypeLimit.Token.ToString() };

                newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Тип лимита" });
                newRow.Cells.Add(new DataGridViewComboBoxCell() { DataSource = _listTypelimit, Value = TypeLimit.USDT.ToString() });
                newRow.Cells.Add(new DataGridViewComboBoxCell() { DataSource = _listTypelimit, Value = TypeLimit.USDT.ToString() });
                newGrid.Rows.Add(newRow);

                // 6 Лимит задания
                newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Лимит задания" });
                newGrid.Rows.Add(newRow);

                // 7 Схема торгов

                _listScheme = new List<string>{
                        GetDescription(SchemeOrderSettings.MakerTaker),
                        GetDescription(SchemeOrderSettings.TakerMaker),
                        GetDescription(SchemeOrderSettings.MakerMaker),
                        GetDescription(SchemeOrderSettings.TakerTaker) }
                ;

                newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Схема торгов" });
                newRow.Cells.Add(new DataGridViewComboBoxCell() { DataSource = _listScheme, Value = _listScheme[0] });
                newGrid.Rows.Add(newRow);

                // 8 Тип установки ордера
                _listTypeStepOrder = new List<string> { TypeStepOrder.PriceStep.ToString(), TypeStepOrder.Percent.ToString() };

                newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Тип установки ордера" });
                newRow.Cells.Add(new DataGridViewComboBoxCell() { DataSource = _listTypeStepOrder, Value = _listTypeStepOrder[0] });
                newRow.Cells.Add(new DataGridViewComboBoxCell() { DataSource = _listTypeStepOrder, Value = _listTypeStepOrder[0] });
                newGrid.Rows.Add(newRow);

                // 9 Установки ордера
                newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Установки ордера" });
                newGrid.Rows.Add(newRow);

                // 10 Максимальный размер единовременного ордера
                newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Максимальный размер единовременного ордера" });
                newGrid.Rows.Add(newRow);

                // 11 Тип спреда
                _listTypeSpread = new List<string>() { TypeSpread.Percent.ToString(), TypeSpread.USDT.ToString() };

                newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Тип спреда" });
                newRow.Cells.Add(new DataGridViewComboBoxCell() { DataSource = _listTypeSpread, Value = _listTypeSpread[0] });
                newGrid.Rows.Add(newRow);

                // 12 Размер спреда
                _listCompareSpread = new List<string> { GetDescription(CompareSpreadEnum.More), GetDescription(CompareSpreadEnum.Less) };

                newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Размер спреда" });
                newRow.Cells.Add(new DataGridViewTextBoxCell());
                newRow.Cells.Add(new DataGridViewComboBoxCell() { DataSource = _listCompareSpread, Value = _listCompareSpread[0] });
                newGrid.Rows.Add(newRow);

                // 13 Комиссия Тейкер
                newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Комиссия Тейкер" });
                newGrid.Rows.Add(newRow);

                // 14 Комиссия Мейкер
                newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Комиссия Мейкер" });
                newGrid.Rows.Add(newRow);

                // 15 Время исполнения
                newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Время исполнения" });
                newGrid.Rows.Add(newRow);

                // 16 Прогресс
                newRow = new DataGridViewRow();
                newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Прогресс" });
                newGrid.Rows.Add(newRow);

                newGrid.Rows[1].Cells[1].ReadOnly = true;
                newGrid.Rows[1].Cells[2].ReadOnly = true;
                newGrid.Rows[4].Cells[1].ReadOnly = true;
                newGrid.Rows[4].Cells[2].ReadOnly = true;

                _exercise = newGrid.Rows[0].Cells[1].Value.ToString();

                return newGrid;
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                return null;
            }
        }

        private DataGridView GridSchemePosition()
        {
            DataGridView newGrid =
               DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect,
               DataGridViewAutoSizeRowsMode.AllCells);

            newGrid.Dock = DockStyle.Fill;
            newGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            newGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            newGrid.Columns.Add("Col1", "Виды действий");
            newGrid.Columns.Add("Col2", "Long");
            newGrid.Columns.Add("Col3", "Short");
            newGrid.Columns.Add("Col4", "Spread");
            newGrid.Columns.Add("Col5", "Sum");

            for (int i = 0; i < newGrid.Columns.Count; i++)
            {
                newGrid.Columns[i].ReadOnly = true;
                DataGridViewColumn column = newGrid.Columns[i];
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }          

            // 0 Мейк/Тейк
            DataGridViewRow newRow = new DataGridViewRow();

            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Мейк/Тейк" });
            newGrid.Rows.Add(newRow);

            // 1 Мейк/Тейк комиссия
            newRow = new DataGridViewRow();
            newGrid.Rows.Add(newRow);

            // 2 Тейк/Мейк 
            newRow = new DataGridViewRow();

            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Тейк/Мейк" });
            newGrid.Rows.Add(newRow);

            // 3 Тейк/Мейк комиссия
            newRow = new DataGridViewRow();
            newGrid.Rows.Add(newRow);

            // 4 Тейк/Тейк
            newRow = new DataGridViewRow();
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Тейк/Тейк" });          
            newGrid.Rows.Add(newRow);

            // 5 Тейк/Тейк комиссия
            newRow = new DataGridViewRow();
            newGrid.Rows.Add(newRow);

            newGrid.ReadOnly = false;

            return newGrid;
        }

        private DataGridView GridResultPosition()
        {
            DataGridView newGrid =
               DataGridFactory.GetDataGridView(DataGridViewSelectionMode.RowHeaderSelect,
               DataGridViewAutoSizeRowsMode.AllCells);

            newGrid.Dock = DockStyle.Fill;
            newGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            newGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            for (int i = 1; i < 32; i++)
            {
                newGrid.Columns.Add("Col" + i, "");
            }
            
            for (int i = 0; i < newGrid.Columns.Count; i++)
            {
                newGrid.Columns[i].ReadOnly = true;
                DataGridViewColumn column = newGrid.Columns[i];
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }

            DataGridViewRow newRow = new DataGridViewRow();
            newGrid.Rows.Add(newRow);

            newGrid.Rows[0].Cells[0].Value = "Позиция";
            newGrid.Rows[0].Cells[7].Value = "Открытая позиция";
            newGrid.Rows[0].Cells[10].Value = "Текущая позиция";
            newGrid.Rows[0].Cells[18].Value = "Закрытая позиция";
            newGrid.Rows[0].Cells[23].Value = "Финансирование";
            newGrid.Rows[0].Cells[27].Value = "Margin";

            newRow = new DataGridViewRow();

            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Биржа" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Инструмент" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Направление" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Частота" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Время" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Вид маржи" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Плечо" });

            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Кол-во" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Цена" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Сумма" });

            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Кол-во" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Цена" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Сумма" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Unrealized P&L, $" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Unrealized P&L, %" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Ставка фондирования, $" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Ставка фондирования, %" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Ожид доходность" });

            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Кол-во" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Цена" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Сумма" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Realized P&L, $" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Realized P&L, %" });
                        
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Комиссия,$" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Комиссия,%" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Фондирование" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Сумма,$" });

            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Цена ликв" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "MM" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "IM" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "СВОБ" });


            newGrid.Rows.Add(newRow);
                                  
            newRow = new DataGridViewRow();
            newGrid.Rows.Add(newRow);

            newGrid.Rows[newGrid.RowCount - 1].Cells[0].Value = "ИТОГ";

            return newGrid;
        }

        #endregion

        #region Обновление таблиц

        private void ThreadRefreshTable()
        {
            while (true)
            {
                try
                {
                    RefreshResultTable();

                    if (_firstTab == null || _secondTab == null)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    RefreshSettingsTable();
                    RefreshSchemeTable();


                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                    Thread.Sleep(5000);
                }
            }
        }

        private void RefreshSettingsTable()
        {
            if (_firstTab.PositionOpenLong == null || _secondTab.PositionOpenShort == null)
            {
                return;
            }

            if (_regime == GetDescription(Regime.On))
            {
                string formattedTime = (DateTime.Now - _timeExecute).ToString("hh\\:mm\\:ss");
                _gridSettingsPosition.Rows[15].Cells[1].Value = formattedTime;

                if (_exercise == GetDescription(Exercise.Open))
                {
                    if (_firstTab.PositionOpenLong.Count == 0 || _secondTab.PositionOpenShort.Count == 0)
                    {
                        _gridSettingsPosition.Rows[16].Cells[1].Value = "0%";
                    }
                    else
                    {
                        if (_firstTab.PositionOpenLong[0].OpenVolume * _firstTab.Security.Lot == _secondTab.PositionOpenShort[0].OpenVolume * _secondTab.Security.Lot)
                        {
                            decimal percent = Math.Round(_firstTab.PositionOpenLong[0].OpenVolume * _firstTab.Security.Lot / _firstLimitVolume * 100, 2);
                            _gridSettingsPosition.Rows[16].Cells[1].Value = percent + "%";

                            if (percent == 100)
                            {
                                _regime = GetDescription(Regime.Off);
                                _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];
                            }
                        }
                    }
                }

                if (_exercise == GetDescription(Exercise.Close))
                {
                    if (_firstTab.PositionOpenLong.Count == 0 && _secondTab.PositionOpenShort.Count == 0)
                    {
                        _gridSettingsPosition.Rows[16].Cells[1].Value = "100%";

                        _regime = GetDescription(Regime.Off);
                        _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];
                    }
                    else if (_firstTab.PositionOpenLong.Count > 0 && _secondTab.PositionOpenShort.Count > 0)
                    {
                        if (_firstTab.PositionOpenLong[0].OpenVolume * _firstTab.Security.Lot == _secondTab.PositionOpenShort[0].OpenVolume * _secondTab.Security.Lot)
                        {
                            _gridSettingsPosition.Rows[16].Cells[1].Value = Math.Round(100 - (_firstTab.PositionOpenLong[0].OpenVolume * _firstTab.Security.Lot / _firstLimitVolume * 100), 2) + "%";
                        }
                    }
                }

                if (_exercise == GetDescription(Exercise.Change))
                {
                    if (_firstChangePos)
                    {
                        if (_secondTab.PositionOpenShort.Count == 0)
                        {
                            _gridSettingsPosition.Rows[16].Cells[1].Value = "0%";
                        }
                        else
                        {
                            if (_secondTab.PositionsLast.OpenVolume * _secondTab.Security.Lot == (_firstTab.PositionsLast.MaxVolume - _firstTab.PositionsLast.OpenVolume) * _firstTab.Security.Lot)
                            {
                                decimal percent = Math.Round(_secondTab.PositionsLast.OpenVolume * _secondTab.Security.Lot / _firstLimitVolume * 100, 2);
                                _gridSettingsPosition.Rows[16].Cells[1].Value = percent + "%";

                                if (percent == 100)
                                {
                                    _regime = GetDescription(Regime.Off);
                                    _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];
                                }
                            }                                                      
                        }                        
                    }

                    if (_secondChangePos)
                    {
                        if (_firstTab.PositionOpenLong.Count == 0)
                        {
                            _gridSettingsPosition.Rows[16].Cells[1].Value = "0%";
                        }
                        else
                        {
                            if (_firstTab.PositionsLast.OpenVolume * _firstTab.Security.Lot == (_secondTab.PositionsLast.MaxVolume - _secondTab.PositionsLast.OpenVolume) * _secondTab.Security.Lot)
                            {
                                decimal percent = Math.Round(_firstTab.PositionsLast.OpenVolume * _firstTab.Security.Lot / _firstLimitVolume * 100, 2);
                                _gridSettingsPosition.Rows[16].Cells[1].Value = percent + "%";

                                if (percent == 100)
                                {
                                    _regime = GetDescription(Regime.Off);
                                    _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];
                                }
                            }                                
                        }                            
                    }
                }
            }            
        }

        private void RefreshSchemeTable()
        {
            decimal sum = 0;
            // maker/taker
            _gridSchemePosition.Rows[0].Cells[1].Value = _firstBestBid; // long maker
            _gridSchemePosition.Rows[0].Cells[2].Value = _secondBestBid; // short taker
            _gridSchemePosition.Rows[0].Cells[3].Value = _secondBestBid - _firstBestBid; // spread maker/taker

            if (_firstBestBid == 0)
            {
                sum = 0;
            }
            else
            {
                sum = Math.Round((_secondBestBid - _firstBestBid) / _firstBestBid * 100, 4);
            }                
            _gridSchemePosition.Rows[0].Cells[4].Value = sum.ToString() + "%"; // sum maker/taker
            _gridSchemePosition.Rows[1].Cells[1].Value = _longСomissionMaker;
            _gridSchemePosition.Rows[1].Cells[2].Value = _shortСomissionTaker;
                        
            decimal sumKom = _longСomissionMaker + _shortСomissionTaker;
            _gridSchemePosition.Rows[1].Cells[3].Value = sumKom;
            _gridSchemePosition.Rows[1].Cells[4].Value = (sum - sumKom).ToString() + "%";

            if (_schemeOrderSettings == GetDescription(SchemeOrderSettings.MakerTaker))
            {
                if (_typeSpread == TypeSpread.Percent.ToString())
                {
                    _currentSpread = sum - sumKom;
                }
                else
                {
                    _currentSpread = _secondBestBid - _firstBestBid;
                }
            }

            // Taker/maker
            _gridSchemePosition.Rows[2].Cells[1].Value = _firstBestAsk; // long taker
            _gridSchemePosition.Rows[2].Cells[2].Value = _secondBestAsk; // short maker
            _gridSchemePosition.Rows[2].Cells[3].Value = _secondBestAsk - _firstBestAsk; // spread taker/maker

            if (_firstBestAsk == 0)
            {
                sum = 0;
            }
            else
            {
                sum = Math.Round((_secondBestAsk - _firstBestAsk) / _firstBestAsk * 100, 4);
            }
                
            _gridSchemePosition.Rows[2].Cells[4].Value = sum.ToString() + "%"; // sum taker/maker
            _gridSchemePosition.Rows[3].Cells[1].Value = _longСomissionTaker;
            _gridSchemePosition.Rows[3].Cells[2].Value = _shortСomissionMaker;
                        
            sumKom = _longСomissionTaker + _shortСomissionMaker;
            _gridSchemePosition.Rows[3].Cells[3].Value = sumKom;
            _gridSchemePosition.Rows[3].Cells[4].Value = (sum - sumKom).ToString() + "%";

            if (_schemeOrderSettings == GetDescription(SchemeOrderSettings.TakerMaker))
            {
                if (_typeSpread == TypeSpread.Percent.ToString())
                {
                    _currentSpread = sum - sumKom;
                }
                else
                {
                    _currentSpread = _secondBestAsk - _firstBestAsk;
                }
            }

            // taker/taker
            _gridSchemePosition.Rows[4].Cells[1].Value = _firstBestAsk; // long taker
            _gridSchemePosition.Rows[4].Cells[2].Value = _secondBestBid; // short taker
            _gridSchemePosition.Rows[4].Cells[3].Value = _secondBestBid - _firstBestAsk; // spread taker/taker

            if (_firstBestAsk == 0)
            {
                sum = 0;
            }
            else
            {
                sum = Math.Round((_secondBestBid - _firstBestAsk) / _firstBestAsk * 100, 4);
            }
            
            _gridSchemePosition.Rows[4].Cells[4].Value = sum.ToString() + "%"; // sum taker/taker
            _gridSchemePosition.Rows[5].Cells[1].Value = _longСomissionTaker;
            _gridSchemePosition.Rows[5].Cells[2].Value = _shortСomissionTaker;
            
            sumKom = _longСomissionTaker + _shortСomissionTaker;
            _gridSchemePosition.Rows[5].Cells[3].Value = sumKom;
            _gridSchemePosition.Rows[5].Cells[4].Value = (sum - sumKom).ToString() + "%";

            if (_schemeOrderSettings == GetDescription(SchemeOrderSettings.TakerTaker) ||
                _schemeOrderSettings == GetDescription(SchemeOrderSettings.MakerMaker))
            {
                if (_typeSpread == TypeSpread.Percent.ToString())
                {
                    _currentSpread = sum - sumKom;
                }
                else
                {
                    _currentSpread = _secondBestBid - _firstBestAsk;
                }
            }
        }

        private void RefreshResultTable()
        {
            if (MainWindow.GetDispatcher.CheckAccess() == false)
            {
                MainWindow.GetDispatcher.Invoke(RefreshResultTable);

                return;
            }

            decimal sumOpen = 0;
            decimal sumClose = 0;
            decimal sumComOpen = 0;
            decimal sumComClose = 0;
            decimal openCom = 0;
            decimal closeCom = 0;
            decimal realizPL = 0;

            for (int i = 2; i < _gridResultPosition.RowCount - 1; i++)
            {
                _gridResultPosition.Rows.RemoveAt(i);
                i--;
            }
            
            if (TabsSimple == null)
            {
                return;
            }

            for (int i = 0; i < TabsSimple.Count; i++)
            {
                if (TabsSimple[i].Security == null)
                {
                    continue;
                }

                if (TabsSimple[i].PositionsAll.Count != 0)
                {
                    int index = _gridResultPosition.Rows.Count - 1;

                    DataGridViewRow newRow = new DataGridViewRow();
                    _gridResultPosition.Rows.Insert(index, newRow);

                    Position pos = TabsSimple[i].PositionsAll[TabsSimple[i].PositionsAll.Count - 1];

                    DataGridViewRow row = _gridResultPosition.Rows[index];

                    // Позиция
                    row.Cells[0].Value = TabsSimple[i].Security.Exchange;
                    row.Cells[1].Value = TabsSimple[i].Security.Name;
                    row.Cells[2].Value = pos.Direction == Side.Buy ? "Long" : "Short";

                    //Открытая позиция
                    row.Cells[7].Value = pos.MaxVolume * TabsSimple[i].Security.Lot;
                    decimal entryPrice = pos.EntryPrice;
                    row.Cells[8].Value = entryPrice;
                    sumOpen = Math.Round(pos.MaxVolume * TabsSimple[i].Security.Lot * pos.EntryPrice, 2);
                    row.Cells[9].Value = sumOpen;

                    // Текущая позиция
                    decimal currentVolume = pos.OpenVolume * TabsSimple[i].Security.Lot;
                    row.Cells[10].Value = currentVolume;

                    decimal currentPrice = 0;

                    if (TabsSimple[i].CandlesAll != null && TabsSimple[i].CandlesAll.Count > 0)
                    {
                        currentPrice = TabsSimple[i].CandlesAll[TabsSimple[i].CandlesAll.Count - 1].Close;
                    }

                    row.Cells[11].Value = currentPrice;
                    decimal currentSum = currentVolume * currentPrice;
                    row.Cells[12].Value = currentSum;
                    decimal unrealizPL = Math.Round((currentPrice - entryPrice) * currentVolume, 4);
                    row.Cells[13].Value = unrealizPL;

                    if (currentSum == 0)
                    {
                        row.Cells[14].Value = 0;
                    }
                    else
                    {
                        row.Cells[14].Value = Math.Round(unrealizPL / currentSum, 4);
                    }                        
                    decimal currentFunding = 0;
                    row.Cells[15].Value = currentFunding;
                    row.Cells[16].Value = 0;
                    row.Cells[17].Value = unrealizPL + currentFunding;

                    // Закрытая позиция
                    if (pos.CloseOrders != null && pos.CloseOrders.Count > 0)
                    {
                        decimal closeVolume = (pos.MaxVolume - pos.OpenVolume) * TabsSimple[i].Security.Lot;
                        row.Cells[18].Value = closeVolume;
                        row.Cells[19].Value = pos.ClosePrice;
                        sumClose = Math.Round(pos.MaxVolume * TabsSimple[i].Security.Lot * pos.ClosePrice, 2);
                        row.Cells[20].Value = sumOpen;
                        realizPL = Math.Round((sumClose - sumOpen) * closeVolume, 4);
                        row.Cells[21].Value = realizPL;

                        if (realizPL == 0)
                        {
                            row.Cells[22].Value = 0;
                        }
                        else
                        {
                            row.Cells[22].Value = realizPL / closeVolume;
                        }      
                    }

                    // Финансирование
                    openCom = GetComission(pos.Comment, 1);
                    sumComOpen = Math.Round(openCom / 100 * sumOpen, 4);
                    closeCom = GetComission(pos.Comment, 3);
                    sumComClose = Math.Round(closeCom / 100 * sumClose, 4);
                    decimal sumCom = sumComOpen + sumComClose; ;
                    row.Cells[23].Value = sumCom;

                    if (sumOpen + sumClose == 0)
                    {
                        row.Cells[24].Value = 0;
                    }
                    else
                    {
                        row.Cells[24].Value = Math.Round(sumCom / (sumOpen + sumClose) * 100, 4);
                    }
                        
                    decimal funding = 0;
                    row.Cells[25].Value = funding;
                    row.Cells[26].Value = realizPL - sumCom + funding;

                    // Margin
                    row.Cells[27].Value = 0;
                    row.Cells[28].Value = 0;
                    row.Cells[29].Value = 0;
                    row.Cells[30].Value = 0;   
                }
            }

            // Итог
            DataGridViewRow lastRow = _gridResultPosition.Rows[_gridResultPosition.RowCount - 1];            

            _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[8].Value = CalculationSumMinus(8);
            _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[9].Value = CalculationSumMinus(9);
            _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[11].Value = CalculationSumMinus(11);
            _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[12].Value = CalculationSumMinus(12);
            _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[13].Value = CalculationSumPlus(13);
            _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[14].Value = CalculationSumPlus(14);
            _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[15].Value = CalculationSumMinus(15);
            _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[16].Value = CalculationSumPlus(16);
            _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[17].Value = CalculationSumPlus(17);
            _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[19].Value = CalculationSumMinus(19);
            _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[20].Value = CalculationSumMinus(20);
            _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[21].Value = CalculationSumPlus(21);
            _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[22].Value = CalculationSumPlus(22);
            _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[23].Value = CalculationSumPlus(23);

            if (_gridResultPosition.RowCount <= 3)
            {
                _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[24].Value = 0;
            }
            else
            {
                _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[24].Value = CalculationSumPlus(24) / (_gridResultPosition.RowCount - 3);
            }

            _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[25].Value = CalculationSumPlus(25);
            _gridResultPosition.Rows[_gridResultPosition.Rows.Count - 1].Cells[26].Value = CalculationSumPlus(26);
        }

        private decimal CalculationSumMinus(int index)
        {
            decimal sum = 0;

            for (int j = 2; j < _gridResultPosition.RowCount - 1; j++)
            {
                decimal value = 0;

                if (_gridResultPosition.Rows[j].Cells[index].Value == null)
                {
                    value = 0;
                }
                else
                {
                    decimal.TryParse(_gridResultPosition.Rows[j].Cells[index].Value.ToString(), out value);
                }                    

                if (_gridResultPosition.Rows[j].Cells[2].Value.ToString() == "Long")
                {
                    sum -= value;
                }
                else
                {
                    sum += value;
                }
            }

            return sum;
        }

        private decimal CalculationSumPlus(int index)
        {
            decimal sum = 0;

            for (int j = 2; j < _gridResultPosition.RowCount - 1; j++)
            {
                decimal value = 0;

                if (_gridResultPosition.Rows[j].Cells[index].Value == null)
                {
                    value = 0;
                }
                else
                {
                    decimal.TryParse(_gridResultPosition.Rows[j].Cells[index].Value.ToString(), out value);
                }
                    
                sum += value;               
            }

            return sum;
        }

        private decimal GetComission(string comment, int ind)
        {
            if (comment == null)
            {
                return 0;
            }
            if (comment.Length == 0)
            {
                return 0;
            }

            decimal com = 0;

            string[] listCom = comment.Split('/');

            if (listCom.Length > 1)
            {
                decimal.TryParse(listCom[ind], out com);
            }

            return com;
        }

        #endregion

        #region События таблиц

        private void __gridSettingsSetPosition_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.ColumnIndex == 1)
            {
                if (e.RowIndex == 7 || e.RowIndex == 10 || e.RowIndex == 11)
                {
                    e.AdvancedBorderStyle.Right = DataGridViewAdvancedCellBorderStyle.None;
                    _gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex + 1].ReadOnly = true;
                }
                if (e.RowIndex == 15 || e.RowIndex == 16)
                {
                    e.AdvancedBorderStyle.Right = DataGridViewAdvancedCellBorderStyle.None;
                    _gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].ReadOnly = true;
                    _gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex + 1].ReadOnly = true;
                }
            }
        }

        private void __gridSettingsSetPosition_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            SendNewLogMessage(e.Exception.ToString(), Logging.LogMessageType.Error);
        }

        private void _gridSettingsSetPosition_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex == 3)
            {
                if (e.ColumnIndex == 1)
                {
                    if (_gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == null)
                    {
                        return;
                    }
                    UnSubscribeBidAsk("first");
                }
                if (e.ColumnIndex == 2)
                {
                    if (_gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == null)
                    {
                        return;
                    }
                    UnSubscribeBidAsk("second");
                }
            }
        }

        private void _gridSettingsPosition_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (_gridSettingsPosition.CurrentCell is DataGridViewCheckBoxCell)
            {
                _gridSettingsPosition.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }            
        }

        private void __gridSettingsSetPosition_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // задание
            if (e.ColumnIndex == 1 && e.RowIndex == 0)
            {
                _exercise = _gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();

                _regime = GetDescription(Regime.Off);
                _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                _gridSettingsPosition.Rows[15].Cells[1].Value = "";
                _gridSettingsPosition.Rows[16].Cells[1].Value = "";

                if (_exercise == GetDescription(Exercise.Open))
                {
                    DialogResult result = MessageBox.Show("Удалить позиции из таблицы результатов?", "Сообщение", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

                    if (result == DialogResult.Yes)
                    {
                        for (int i = 0; i < TabsSimple.Count; i++)
                        {
                            TabsSimple[i].PositionsOpenAll.Clear();
                            TabsSimple[i].PositionsAll.Clear();
                            TabsSimple[i].PositionOpenLong.Clear();
                            TabsSimple[i].PositionOpenShort.Clear();
                        }
                    }                   
                }

                SaveSettings();
            }

            // режим
            if (e.ColumnIndex == 2 && e.RowIndex == 0)
            {
                _regime = _gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();

                if (_regime == GetDescription(Regime.On))
                {
                    SetTypeLimit();
                    
                    _timeExecute = DateTime.Now;
                    _gridSettingsPosition.Rows[15].Cells[1].Value = "";
                    _gridSettingsPosition.Rows[16].Cells[1].Value = "";
                }
            }

            // изменение позиции
            if (e.RowIndex == 2)
            {
                if (e.ColumnIndex == 1)
                {
                    bool.TryParse(_gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString(), out _firstChangePos);
                    if (_firstChangePos)
                    {
                        _gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex + 1].Value = false;
                    }   
                }

                if (e.ColumnIndex == 2)
                {                    
                    bool.TryParse(_gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString(), out _secondChangePos);
                    if (_secondChangePos)
                    {
                        _gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex - 1].Value = false;
                    }                    
                }
            }

            // смена площадки
            if (e.ColumnIndex == 1 && e.RowIndex == 3)
            {
                string[] field = _gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Split('/');
                string exchange = field[0];
                string ticker = field[1];

                for (int i = 0; i < TabsSimple.Count; i++)
                {
                    if (TabsSimple[i].Security != null && 
                        TabsSimple[i].Security.Exchange == exchange &&
                        TabsSimple[i].Security.Name == ticker)
                    {
                        _firstTab = TabsSimple[i];
                    }
                }

                _gridSettingsPosition.Rows[e.RowIndex + 1].Cells[e.ColumnIndex].Value = ticker;

                SubscribeBidAsk("first");
            }
            if (e.ColumnIndex == 2 && e.RowIndex == 3)
            {
                string[] field = _gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Split('/');
                string exchange = field[0];
                string ticker = field[1];

                for (int i = 0; i < TabsSimple.Count; i++)
                {
                    if (TabsSimple[i].Security != null && 
                        TabsSimple[i].Security.Exchange == exchange &&
                        TabsSimple[i].Security.Name == ticker)
                    {
                        _secondTab = TabsSimple[i];
                    }
                }

                _gridSettingsPosition.Rows[e.RowIndex + 1].Cells[e.ColumnIndex].Value = ticker;

                SubscribeBidAsk("second");
            }

            // тип лимита
            if (e.ColumnIndex == 1 && e.RowIndex == 5)
            {
                _firstTypeLimit = _gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();

            }
            if (e.ColumnIndex == 2 && e.RowIndex == 5)
            {
                _secondTypeLimit = _gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
            }

            // лимит задания
            if (e.ColumnIndex == 1 && e.RowIndex == 6)
            {               
                decimal.TryParse(_gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Replace(".", ","), out _firstLimitVolumeTable);
                SaveSettings();
            }
            if (e.ColumnIndex == 2 && e.RowIndex == 6)
            {
                decimal.TryParse(_gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Replace(".", ","), out _secondLimitVolumeTable);
                SaveSettings();
            }
                        
            // схема торгов
            if (e.ColumnIndex == 1 && e.RowIndex == 7)
            {
                _schemeOrderSettings = _gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                SaveSettings();
            }

            // тип установки ордера
            if (e.ColumnIndex == 1 && e.RowIndex == 8)
            {
                _firstTypeStepOrder = _gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                SaveSettings();
            }
            if (e.ColumnIndex == 2 && e.RowIndex == 8)
            {
                _secondTypeStepOrder = _gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                SaveSettings();
            }

            // установки ордера
            if (e.ColumnIndex == 1 && e.RowIndex == 9)
            {
                decimal.TryParse(_gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Replace(".", ","), out _firstStepOrder);
                SaveSettings();
            }
            if (e.ColumnIndex == 2 && e.RowIndex == 9)
            {
                decimal.TryParse(_gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Replace(".", ","), out _secondStepOrder);
                SaveSettings();
            }

            // максимальный размер ордера
            if (e.ColumnIndex == 1 && e.RowIndex == 10)
            {
                decimal.TryParse(_gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Replace(".", ","), out _maxVolume);
                SaveSettings();
            }

            // смена типа ордера
            if (e.ColumnIndex == 1 && e.RowIndex == 11)
            {
                _typeSpread = _gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                SaveSettings();
            }

            // размер спреда
            if (e.ColumnIndex == 1 && e.RowIndex == 12)
            {
                decimal.TryParse(_gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Replace(".", ","), out _setSpread);
                SaveSettings();
            }

            // сравнение спреда
            if (e.ColumnIndex == 2 && e.RowIndex == 12)
            {
                _compareSpread = _gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                SaveSettings();
            }

            // комиссия тейкер лонг
            if (e.ColumnIndex == 1 && e.RowIndex == 13)
            {
                decimal.TryParse(_gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Replace(".", ","), out _longСomissionTaker);
                SaveSettings();
            }

            // комиссия тейкер шорт
            if (e.ColumnIndex == 2 && e.RowIndex == 13)
            {
                decimal.TryParse(_gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Replace(".", ","), out _shortСomissionTaker);
                SaveSettings();
            }

            // комиссия мейкер лонг
            if (e.ColumnIndex == 1 && e.RowIndex == 14)
            {
                decimal.TryParse(_gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Replace(".", ","), out _longСomissionMaker);
                SaveSettings();
            }

            // комиссия мейкер шорт
            if (e.ColumnIndex == 2 && e.RowIndex == 14)
            {
                decimal.TryParse(_gridSettingsPosition.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Replace(".", ","), out _shortСomissionMaker);
                SaveSettings();
            }
        }

        private void SetTypeLimit()
        {            
            try
            {
                if (_firstBestAsk == 0 || _secondBestBid == 0)
                {
                    _firstLimitVolume = 0;
                    _secondLimitVolume = 0;
                    return;
                }
                _firstLimitVolume = _firstLimitVolumeTable;

                if (_firstTypeLimit == GetDescription(TypeLimit.USDT))
                {
                    _firstLimitVolume = Math.Floor(_firstLimitVolumeTable / _firstBestAsk / _maxVolume) * _maxVolume;
                }

                _secondLimitVolume = _secondLimitVolumeTable;

                if (_secondTypeLimit == GetDescription(TypeLimit.USDT))
                {
                    _secondLimitVolume = Math.Floor(_secondLimitVolumeTable / _secondBestBid / _maxVolume) * _maxVolume;
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
           
        }

        private void SubscribeBidAsk(string str)
        {          
            if (str == "first")
            {
                if (_firstTab.MarketDepth == null)
                {
                    return;
                }

                _firstTab.BestBidAskChangeEvent += First_BestBidAskChangeEvent;
                _firstBestAsk = _firstTab.MarketDepth.Asks[0].Price;
                _firstBestBid = _firstTab.MarketDepth.Bids[0].Price;
            }
            else
            {
                if (_secondTab.MarketDepth == null)
                {
                    return;
                }

                _secondTab.BestBidAskChangeEvent += Second_BestBidAskChangeEvent;
                _secondBestAsk = _secondTab.MarketDepth.Asks[0].Price;
                _secondBestBid = _secondTab.MarketDepth.Bids[0].Price;
            }
        }

        private void UnSubscribeBidAsk(string str)
        {
            if (str == "first")
            {
                _firstTab.BestBidAskChangeEvent -= First_BestBidAskChangeEvent;
                _firstBestAsk = 0;
                _firstBestBid = 0;
            }
            else
            {
                _secondTab.BestBidAskChangeEvent -= Second_BestBidAskChangeEvent;
                _secondBestBid = 0;
                _secondBestAsk = 0;
            }
        }

        private void First_BestBidAskChangeEvent(decimal bid, decimal ask)
        {
            _firstBestBid = bid;
            _firstBestAsk = ask;
        }

        private void Second_BestBidAskChangeEvent(decimal bid, decimal ask)
        {
            _secondBestBid = bid;
            _secondBestAsk = ask;
        }

        #endregion

        #region Trade Logic

        private void TradeLogic()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(100);
        
                    if (_regime == GetDescription(Regime.Off))
                    {
                        continue;                      
                    }

                    if (!CheckTryWork())
                    {
                        _regime = GetDescription(Regime.Off);
                        _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];
                        _gridSettingsPosition.Rows[15].Cells[1].Value = "";
                        _gridSettingsPosition.Rows[16].Cells[1].Value = "";

                        continue;
                    }

                    if (_schemeOrderSettings == null)
                    {
                        _schemeOrderSettings = GetDescription(SchemeOrderSettings.MakerTaker);
                    }

                    if (_exercise == GetDescription(Exercise.Open))
                    {
                        ExerciseOpen();
                    }
                    else if (_exercise == GetDescription(Exercise.Close))
                    {
                        ExerciseClose();
                    }
                    else if (_exercise == GetDescription(Exercise.Change))
                    {
                        ExerciseChange();
                    }
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                    Thread.Sleep(5000);
                }                
            }      
        }

        private bool CheckTryWork()
        {
            if (_firstTab == null || _secondTab == null)
            {
                SendNewLogMessage("Не выбраны биржи", Logging.LogMessageType.Error);
                return false;
            }

            if (_firstBestAsk == 0 ||
                    _firstBestBid == 0 ||
                    _secondBestAsk == 0 ||
                    _secondBestBid == 0)
            {
                SendNewLogMessage("Нет данных бид/аск от бирж", Logging.LogMessageType.Error);
                return false;
            }

            if (_firstLimitVolume == 0 || _secondLimitVolume == 0)
            {
                SendNewLogMessage("Не указан лимит задания", Logging.LogMessageType.Error);
                return false;
            }

            if (_maxVolume == 0)
            {
                SendNewLogMessage("Не указан максимальный размер ордера", Logging.LogMessageType.Error);
                return false;
            }

            if (_setSpread == 0)
            {
                SendNewLogMessage("Не указан размер спреда", Logging.LogMessageType.Error);
                return false;
            }

            return true;
        }

        private void ExerciseOpen()
        {
            if (_firstTab.PositionsOpenAll.Count > 0 && _secondTab.PositionsOpenAll.Count > 0)
            {
                if (_firstTab.PositionsOpenAll[0].OpenVolume * _firstTab.Security.Lot == _firstLimitVolume &&
                    _secondTab.PositionsOpenAll[0].OpenVolume * _secondTab.Security.Lot == _secondLimitVolume)
                {
                    return;
                }
            }

            if (_schemeOrderSettings == GetDescription(SchemeOrderSettings.TakerTaker))
            {
                OpenTradeTakerTaker();
            }

            if (_schemeOrderSettings == GetDescription(SchemeOrderSettings.MakerTaker))
            {
                OpenTradeWithMakerOrder(_firstTab, _secondTab);
            }

            if (_schemeOrderSettings == GetDescription(SchemeOrderSettings.TakerMaker))
            {
                OpenTradeWithMakerOrder(_secondTab, _firstTab);
            }

            if (_schemeOrderSettings == GetDescription(SchemeOrderSettings.MakerMaker))
            {
                OpenTradeMakerMaker();
            }
        }

        private void OpenTradeTakerTaker()
        {
            if (CheckOpenPosition())
            {
                decimal volumeLong = GetVolumeOpen(Side.Buy);
                decimal volumeShort = GetVolumeOpen(Side.Sell);

                if (volumeLong > 0)
                {
                    if (_firstTab.PositionsOpenAll.Count > 0)
                    {

                        _firstTab.BuyAtMarketToPosition(_firstTab.PositionsOpenAll[0], volumeLong);
                        SetComissionOpen(_firstTab, _longСomissionTaker);
                    }
                    else
                    {
                        _firstTab.BuyAtMarket(volumeLong);
                        SetComissionOpen(_firstTab, _longСomissionTaker);
                    }
                }
                
                if (volumeShort > 0)
                {
                    if (_secondTab.PositionsOpenAll.Count > 0)
                    {
                        _secondTab.SellAtMarketToPosition(_secondTab.PositionsOpenAll[0], volumeShort);
                        SetComissionOpen(_secondTab, _shortСomissionTaker);
                    }
                    else
                    {
                        _secondTab.SellAtMarket(volumeShort);
                        SetComissionOpen(_secondTab, _shortСomissionTaker);
                    }
                }                
            }                
        }

        private void SetComissionOpen(BotTabSimple tab, decimal comiss)
        {
            string closeCom = "";

            if (tab.PositionsLast.Comment != null && tab.PositionsLast.Comment != "")
            {
                closeCom = tab.PositionsLast.Comment.Split('/')[3];
            }

            tab.PositionsLast.Comment = "open/" + comiss + "/close/" + closeCom;
        }

        private bool CheckOpenPosition()
        {
            try
            {
                if (_regime == GetDescription(Regime.Pause))
                {
                    _regime = GetDescription(Regime.Off);
                    _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                    return false;
                }

                if (!CompareSpread())
                {
                    return false;
                }

                if (_firstTab.PositionsOpenAll.Count == 0 && _secondTab.PositionsOpenAll.Count == 0)
                {
                    return true;
                }

                else if (_firstTab.PositionsOpenAll.Count != 0 && _secondTab.PositionsOpenAll.Count != 0)
                {
                    if (_firstTab.PositionsOpenAll[0].OpenVolume * _firstTab.Security.Lot + _maxVolume > _firstLimitVolume ||
                        _secondTab.PositionsOpenAll[0].OpenVolume * _secondTab.Security.Lot + _maxVolume > _secondLimitVolume)
                    {
                        return false;
                    }

                    if (_firstTab.PositionsOpenAll[0].OpenVolume * _firstTab.Security.Lot == _secondTab.PositionsOpenAll[0].OpenVolume * _secondTab.Security.Lot)
                    {
                        if (_firstTab.PositionsOpenAll[0].OpenOrders.Last().State == OrderStateType.Done &&
                            _secondTab.PositionsOpenAll[0].OpenOrders.Last().State == OrderStateType.Done)
                        {
                            return true;
                        }

                        if (_firstTab.PositionsOpenAll[0].OpenOrders.Last().State == OrderStateType.Cancel ||
                            _secondTab.PositionsOpenAll[0].OpenOrders.Last().State == OrderStateType.Cancel)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                return false;
            }
        }

        private decimal GetVolumeOpen(Side side)
        {
            decimal volume = 0;

            List<Position> pos = _secondTab.PositionsOpenAll;
            decimal limitVolume = _secondLimitVolume;

            BotTabSimple tab = _secondTab;

            if (side == Side.Buy)
            {
                pos = _firstTab.PositionsOpenAll;
                limitVolume = _firstLimitVolume;
                tab = _firstTab;
            }
            
            if (pos.Count == 0)
            {
                if (_maxVolume > limitVolume)
                {
                    volume = limitVolume;
                }
                else
                {
                    volume = _maxVolume;
                }                    
            }
            else
            {
                if (pos[0].OpenVolume * tab.Security.Lot < limitVolume)
                {
                    if (_maxVolume <= limitVolume - pos[0].OpenVolume * tab.Security.Lot)
                    {                        
                        if ((pos[0].OpenVolume * tab.Security.Lot) % _maxVolume == 0)
                        {
                            volume = _maxVolume;
                        }
                        else
                        {
                            volume = _maxVolume - (pos[0].OpenVolume * tab.Security.Lot) % _maxVolume;
                        }
                    }
                    else
                    {
                        volume = (limitVolume - pos[0].OpenVolume * tab.Security.Lot);
                    }
                }
            }

            return volume / tab.Security.Lot;
        }

        private void ExerciseClose()
        {
            if (_firstTab.PositionsOpenAll.Count == 0 && _secondTab.PositionsOpenAll.Count == 0)
            {
                return;
            }

            if (_schemeOrderSettings == GetDescription(SchemeOrderSettings.TakerTaker))
            {
                CloseTradeTakerTaker();
            }

            if (_schemeOrderSettings == GetDescription(SchemeOrderSettings.MakerTaker))
            {
                CloseTradeWithMaker(_firstTab, _secondTab);
            }

            if (_schemeOrderSettings == GetDescription(SchemeOrderSettings.TakerMaker))
            {
                CloseTradeWithMaker(_secondTab, _firstTab);
            }

            if (_schemeOrderSettings == GetDescription(SchemeOrderSettings.MakerMaker))
            {
                CloseTradeMakerMaker();
            }
        }

        private void CloseTradeTakerTaker()
        {
            if (CheckClosePosition())
            {
                decimal volumeLong = GetVolumeClose(_firstTab);
                decimal volumeShort = GetVolumeClose(_secondTab);

                decimal priceLong = _firstBestBid - _firstTab.Security.PriceStep * 500;
                decimal priceShort = _secondBestAsk + _secondTab.Security.PriceStep * 500;

                _firstTab.CloseAtLimitUnsafe(_firstTab.PositionsOpenAll[0], priceLong, volumeLong);
                SetComissionClose(_firstTab, _longСomissionTaker);
                _secondTab.CloseAtLimitUnsafe(_secondTab.PositionsOpenAll[0], priceShort, volumeShort);
                SetComissionClose(_secondTab, _shortСomissionTaker);
            }
        }

        private bool _tryCloseTaker = false;

        private void CloseTradeWithMaker(BotTabSimple makerTab, BotTabSimple takerTab)
        {
            decimal priceMaker = _secondBestBid;
            decimal priceTaker = _firstBestBid - _firstTab.Security.PriceStep * 500;
            decimal comissTaker = _longСomissionTaker;
            decimal comissMaker = _shortСomissionMaker;

            if (makerTab.Equals(_firstTab))
            {
                priceMaker = _firstBestAsk;
                priceTaker = _secondBestAsk + _secondTab.Security.PriceStep * 500;
                comissTaker = _shortСomissionTaker;
                comissMaker = _longСomissionMaker;
            }

            if (makerTab.PositionsLast.CloseOrders != null && makerTab.PositionsLast.CloseOrders.Count > 0)
            {       
                if (!_firstCancelOrder || !_secondCancelOrder)
                {
                    if (takerTab.PositionsLast.CloseOrders != null && takerTab.PositionsLast.CloseOrders.Count > 0)
                    {
                        if (makerTab.PositionsLast.CloseOrders.Last().State == OrderStateType.Done &&
                            takerTab.PositionsLast.CloseOrders.Last().State == OrderStateType.Done)
                        {
                            if (makerTab.PositionsLast.OpenVolume * makerTab.Security.Lot >= takerTab.PositionsLast.OpenVolume * takerTab.Security.Lot)
                            {
                                if (_regime == GetDescription(Regime.Pause))
                                {
                                    _regime = GetDescription(Regime.Off);
                                    _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                                    return;
                                }

                                if (!CompareSpread())
                                {
                                    return;
                                }

                                if (makerTab.PositionsLast.OpenVolume == 0)
                                {
                                    return;
                                }

                                decimal volume = GetVolumeClose(makerTab);

                                makerTab.CloseAtMarket(makerTab.PositionsLast, volume);
                                SetComissionClose(makerTab, comissMaker);
                                _tryCloseTaker = false;
                                _firstCancelOrder = false;
                                _secondCancelOrder = false;
                                //SendNewLogMessage("Send Close Maker", Logging.LogMessageType.Error);
                            }
                        }
                    }

                    if (makerTab.PositionsLast.OpenVolume * makerTab.Security.Lot < takerTab.PositionsLast.OpenVolume * takerTab.Security.Lot &&
                        makerTab.PositionsLast.CloseOrders.Last().State == OrderStateType.Done)
                    {
                        decimal volumeTaker = GetVolumeClose(takerTab);

                        if (volumeTaker > 0)
                        {                           
                            takerTab.CloseAtMarket(takerTab.PositionsLast, volumeTaker);
                            SetComissionClose(takerTab, comissTaker);
                            _tryCloseTaker = true;
                            _firstCancelOrder = true;
                            _secondCancelOrder = true;
                            //SendNewLogMessage("Send Close Taker", Logging.LogMessageType.Error);
                            return;
                        }
                    }

                    CloseOrderChangeDepth(makerTab);                   
                }
                else
                {
                    if (takerTab.PositionsLast.CloseOrders.Last().State == OrderStateType.Done &&
                        makerTab.PositionsLast.OpenVolume * makerTab.Security.Lot <= takerTab.PositionsLast.OpenVolume * takerTab.Security.Lot)
                    {
                        _tryCloseTaker = false; 
                        _firstCancelOrder = false;
                        _secondCancelOrder = false;
                    }
                }
            }
            else
            {
                if (_regime == GetDescription(Regime.Pause))
                {
                    _regime = GetDescription(Regime.Off);
                    _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                    return;
                }

                if (!CompareSpread())
                {
                    return;
                }

                if (makerTab.PositionsLast.OpenVolume == 0)
                {
                    return;
                }
                decimal volumeMaker = GetVolumeClose(makerTab);
                //SendNewLogMessage("Send First Close Maker", Logging.LogMessageType.Error);
                makerTab.CloseAtLimit(makerTab.PositionsLast, priceMaker, volumeMaker);
                SetComissionClose(makerTab, comissMaker);
                _tryCloseTaker = false;
                _firstCancelOrder = false;
                _secondCancelOrder = false;
            }                
        }

        private void CloseTradeMakerMaker()
        {
            if (_firstTab.PositionsOpenAll.Count == 0 && _secondTab.PositionsOpenAll.Count == 0)
            {
                return;
            }

            if (_firstTab.PositionsLast.CloseOrders != null &&
                _firstTab.PositionsLast.CloseOrders.Count > 0 &&
                _secondTab.PositionsLast.CloseOrders != null &&
                _secondTab.PositionsLast.CloseOrders.Count > 0)
            {
                if (_firstTab.PositionsLast.CloseOrders.Last().State == OrderStateType.Done &&
                    _secondTab.PositionsLast.CloseOrders.Last().State == OrderStateType.Done)
                {                    
                    if (_firstTab.PositionsLast.OpenVolume * _firstTab.Security.Lot == _secondTab.PositionsLast.OpenVolume * _secondTab.Security.Lot)
                    {
                        if (_regime == GetDescription(Regime.Pause))
                        {
                            _regime = GetDescription(Regime.Off);
                            _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                            return;
                        }

                        if (!CompareSpread())
                        {
                            return;
                        }

                        //SendNewLogMessage("Выставление закрытия лонга мейк ", Logging.LogMessageType.Error);
                        decimal volumeFirst = GetVolumeClose(_firstTab);
                        _firstTab.CloseAtLimit(_firstTab.PositionsLast, _firstBestAsk, volumeFirst);
                        SetComissionClose(_firstTab, _longСomissionMaker);

                        //SendNewLogMessage("Выставление закрытия шорта мейк ", Logging.LogMessageType.Error);
                        decimal volumeSecond = GetVolumeClose(_secondTab);
                        _secondTab.CloseAtLimit(_secondTab.PositionsLast, _secondBestBid, volumeSecond);
                        SetComissionClose(_secondTab, _shortСomissionMaker);

                        _firstCancelOrder = false;
                        _secondCancelOrder = false;

                        return;
                    }
                }

                if (_firstTab.PositionsLast.OpenVolume * _firstTab.Security.Lot < _secondTab.PositionsLast.OpenVolume * _secondTab.Security.Lot)
                {
                    if (_firstTab.PositionsLast.CloseOrders.Last().State == OrderStateType.Done)
                    {
                        if (!_secondCancelOrder)
                        {
                            Order order = _secondTab.PositionsLast.CloseOrders.Last();

                            if (order.NumberMarket != "")
                            {
                                decimal volume = GetVolumeClose(_secondTab);

                                SendNewLogMessage("FirstTab исполнился (OV = " + _firstTab.PositionsLast.OpenVolume + ", отменяем закрывающий ордер, State = " + order.State + ", price = " + order.Price + ", volume = " +
                                        order.Volume + ", VolEx = " + order.VolumeExecute, Logging.LogMessageType.User);

                                _secondTab.CloseOrder(order);
                                _secondCancelOrder = true;

                                SendNewLogMessage("SecondTab OV = " + _secondTab.PositionsLast.OpenVolume + ", ставим рыночный закрывающий ордер, volume = " + volume, Logging.LogMessageType.User);

                                _secondTab.CloseAtMarket(_secondTab.PositionsLast, volume);
                                SetComissionClose(_secondTab, _shortСomissionTaker);

                                return;
                            }
                        }
                    }
                }

                CloseOrderChangeDepth(_secondTab);

                if (_secondTab.PositionsLast.OpenVolume * _secondTab.Security.Lot < _firstTab.PositionsLast.OpenVolume * _firstTab.Security.Lot)
                {
                    if (_secondTab.PositionsLast.CloseOrders.Last().State == OrderStateType.Done)
                    {
                        if (!_firstCancelOrder)
                        {
                            Order order = _firstTab.PositionsLast.CloseOrders.Last();

                            if (order.NumberMarket != "")
                            {
                                decimal volume = GetVolumeClose(_firstTab);

                                SendNewLogMessage("SecondTab исполнился (OV = " + _secondTab.PositionsLast.OpenVolume + ", отменяем закрывающий ордер, State = " + order.State + ", price = " + order.Price + ", volume = " +
                                        order.Volume + ", VolEx = " + order.VolumeExecute, Logging.LogMessageType.User);

                                _firstTab.CloseOrder(order);
                                _firstCancelOrder = true;

                                SendNewLogMessage("FirstTab OV = " + _firstTab.PositionsLast.OpenVolume + ", ставим рыночный закрывающий ордер, volume = " + volume, Logging.LogMessageType.User);

                                _firstTab.CloseAtMarket(_firstTab.PositionsLast, volume);
                                SetComissionOpen(_firstTab, _longСomissionTaker);

                                return;
                            }
                        }
                    }
                }

                CloseOrderChangeDepth(_firstTab);
            }
            else
            {
                if (_regime == GetDescription(Regime.Pause))
                {
                    _regime = GetDescription(Regime.Off);
                    _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                    return;
                }

                if (!CompareSpread())
                {
                    return;
                }

                //SendNewLogMessage("Закрытие лонга мейк ", Logging.LogMessageType.Error);
                decimal volumeFirst = GetVolumeClose(_firstTab);
                _firstTab.CloseAtLimit(_firstTab.PositionsLast, _firstBestAsk, volumeFirst);
                SetComissionClose(_firstTab, _longСomissionMaker);

                //SendNewLogMessage("Закрытие шорта мейк ", Logging.LogMessageType.Error);
                decimal volumeSecond = GetVolumeClose(_secondTab);
                _secondTab.CloseAtLimit(_secondTab.PositionsLast, _secondBestBid, volumeSecond);
                SetComissionClose(_secondTab, _shortСomissionMaker);

                _firstCancelOrder = false;
                _secondCancelOrder = false;
            }
        }

        private void SetComissionClose(BotTabSimple tab, decimal comiss)
        {           
            string openCom = "";

            if (tab.PositionsLast.Comment != null && tab.PositionsLast.Comment != "")
            {
                openCom = tab.PositionsLast.Comment.Split('/')[1];
            }

            tab.PositionsLast.Comment = "open/" + openCom + "/close/" + comiss;           
        }

        private bool CheckClosePosition()
        {
            try
            {
                if (_regime == GetDescription(Regime.Pause))
                {
                    _regime = GetDescription(Regime.Off);
                    _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                    return false;
                }

                if (!CompareSpread())
                {
                    return false;
                }

                if (_firstTab.PositionsOpenAll.Count == 0 || _secondTab.PositionsOpenAll.Count == 0)
                {
                    return false;
                }

                else if (_firstTab.PositionsOpenAll.Count != 0 && _secondTab.PositionsOpenAll.Count != 0)
                {
                    if (_firstTab.PositionsOpenAll[0].OpenVolume * _firstTab.Security.Lot - _maxVolume < 0 ||
                        _secondTab.PositionsOpenAll[0].OpenVolume * _secondTab.Security.Lot - _maxVolume < 0)
                    {
                        return false;
                    }

                    if (_firstTab.PositionsOpenAll[0].OpenVolume * _firstTab.Security.Lot == _secondTab.PositionsOpenAll[0].OpenVolume * _secondTab.Security.Lot)
                    {
                        if (_firstTab.PositionsOpenAll[0].CloseOrders == null && _secondTab.PositionsOpenAll[0].CloseOrders == null)
                        {
                            return true;
                        }

                        if (_firstTab.PositionsOpenAll[0].CloseOrders.Last().State == OrderStateType.Done &&
                            _secondTab.PositionsOpenAll[0].CloseOrders.Last().State == OrderStateType.Done)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                return false;
            }
        }

        private decimal GetVolumeClose(BotTabSimple tab)
        {
            decimal volume = 0;

            List<Position> pos = tab.PositionsOpenAll;
           
            if (pos.Count > 0)
            {
                if (pos[0].OpenVolume * tab.Security.Lot > 0)
                {
                    if (_maxVolume <= pos[0].OpenVolume * tab.Security.Lot)
                    {
                        if ((pos[0].OpenVolume * tab.Security.Lot) % _maxVolume == 0)
                        {
                            volume = _maxVolume;
                        }
                        else
                        {
                            volume = (pos[0].OpenVolume * tab.Security.Lot) % _maxVolume;
                        }
                    }
                    else
                    {
                        volume = pos[0].OpenVolume * tab.Security.Lot;
                    }
                }
            }

            return volume / tab.Security.Lot;
        }

        private void ExerciseChange()
        {
            if (!_firstChangePos && !_secondChangePos)
            {
                SendNewLogMessage("Нет данных какую позицию менять. Поставьте чек-бокс в строке \"Изменение позиции\".", Logging.LogMessageType.Error);
                _regime = GetDescription(Regime.Off);
                _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];
                return;
            }
            if (_firstChangePos && _secondChangePos)
            {
                SendNewLogMessage("Нельзя выбрать одновременно два чек-бокса в строке \"Изменение позиции\".", Logging.LogMessageType.Error);
                _regime = GetDescription(Regime.Off);
                _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];
                return;
            }

            if (_schemeOrderSettings == GetDescription(SchemeOrderSettings.TakerTaker))
            {
                if (!CheckChangePositions())
                {
                    return;
                }

                ChangeTradeTakerTaker();
            }

            if (_schemeOrderSettings == GetDescription(SchemeOrderSettings.MakerTaker))
            {
                ChangeTradeWithMaker(_firstTab, _secondTab);
            }

            if (_schemeOrderSettings == GetDescription(SchemeOrderSettings.TakerMaker))
            {
                ChangeTradeWithMaker(_secondTab, _firstTab);
            }
            if (_schemeOrderSettings == GetDescription(SchemeOrderSettings.MakerMaker))
            {
                ChangeTradeMakerMaker();
            }
        }

        private bool CheckChangePositions()
        {
            if (_firstTab.PositionsOpenAll.Count != 0 && _secondTab.PositionsOpenAll.Count != 0)
            {
                if (_firstTab.PositionsOpenAll[0].MaxVolume * _firstTab.Security.Lot == _secondTab.PositionsOpenAll[0].MaxVolume * _secondTab.Security.Lot)
                {
                    return false;
                }
            }

            decimal firstVolume = 0;
            decimal secondVolume = 0;

            if (_firstTab.PositionsOpenAll.Count == 0)
            {
                //open
                firstVolume = 0;
            }
            else
            {
                if (_firstTab.PositionsLast.OpenOrders.Count > 0)
                {
                    if (_firstTab.PositionsLast.OpenOrders.Last().State != OrderStateType.Done)
                    {
                        return false;
                    }
                }

                if (_firstTab.PositionsLast.CloseOrders != null && _firstTab.PositionsLast.CloseOrders.Count > 0)
                {
                    if (_firstTab.PositionsLast.CloseOrders.Last().State != OrderStateType.Done)
                    {
                        return false;
                    }
                }

                if (_firstTab.PositionsLast.Direction == Side.Buy)
                {
                    //open
                    firstVolume = _firstTab.PositionsLast.OpenVolume * _firstTab.Security.Lot;
                }
                else
                {
                    //close
                    firstVolume = _firstTab.PositionsLast.MaxVolume * _firstTab.Security.Lot - _firstTab.PositionsLast.OpenVolume * _firstTab.Security.Lot;
                }
            }

            if (_secondTab.PositionsOpenAll.Count == 0)
            {
                //open
                secondVolume = 0;
            }
            else
            {
                if (_secondTab.PositionsLast.OpenOrders.Count > 0)
                {
                    if (_secondTab.PositionsLast.OpenOrders.Last().State != OrderStateType.Done)
                    {
                        return false;
                    }
                }

                if (_secondTab.PositionsLast.CloseOrders != null && _secondTab.PositionsLast.CloseOrders.Count > 0)
                {
                    if (_secondTab.PositionsLast.CloseOrders.Last().State != OrderStateType.Done)
                    {
                        return false;
                    }
                }

                if (_secondTab.PositionsLast.Direction == Side.Sell)
                {
                    // open
                    secondVolume = _secondTab.PositionsLast.OpenVolume * _secondTab.Security.Lot;
                }
                else
                {
                    //close
                    secondVolume = _secondTab.PositionsLast.MaxVolume * _secondTab.Security.Lot - _secondTab.PositionsLast.OpenVolume * _secondTab.Security.Lot;
                }
            }

            if (firstVolume != secondVolume)
            {
                return false;
            }

            return true;
        }

        private void ChangeTradeTakerTaker()
        {
            if (_regime == GetDescription(Regime.Pause))
            {
                _regime = GetDescription(Regime.Off);
                _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                return;
            }

            if (!CompareSpread())
            {
                return;
            }

            if (_firstTab.PositionsOpenAll.Count == 0)
            {
                decimal volume = GetVolumeOpen(Side.Buy);
                _firstTab.BuyAtMarket(volume);
                SetComissionOpen(_firstTab, _longСomissionTaker);
            }
            else
            {
                if (_firstTab.PositionsLast.Direction == Side.Buy)
                {
                    Position pos = _firstTab.PositionsLast;
                    decimal volume = GetVolumeOpen(Side.Buy);
                    _firstTab.BuyAtMarketToPosition(pos, volume);
                    SetComissionOpen(_firstTab, _longСomissionTaker);
                }
                else
                {
                    decimal volumeLong = GetVolumeClose(_firstTab);
                    decimal priceLong = _firstBestAsk + _firstTab.Security.PriceStep * 100;

                    _firstTab.CloseAtLimitUnsafe(_firstTab.PositionsLast, priceLong, volumeLong);
                    SetComissionClose(_firstTab, _longСomissionTaker);
                }
            }

            if (_secondTab.PositionsOpenAll.Count == 0)
            {
                decimal volume = GetVolumeOpen(Side.Sell);
                _secondTab.SellAtMarket(volume);
                SetComissionOpen(_secondTab, _shortСomissionTaker);
            }
            else
            {
                if (_secondTab.PositionsLast.Direction == Side.Sell)
                {
                    Position pos = _secondTab.PositionsLast;
                    decimal volume = GetVolumeOpen(Side.Sell);
                    _secondTab.SellAtMarketToPosition(pos, volume);
                    SetComissionOpen(_secondTab, _shortСomissionTaker);
                }
                else
                {
                    decimal volumeShort = GetVolumeClose(_secondTab);
                    decimal priceShort = _secondBestBid - _secondTab.Security.PriceStep * 100;

                    _secondTab.CloseAtLimitUnsafe(_secondTab.PositionsLast, priceShort, volumeShort);
                    SetComissionClose(_secondTab, _shortСomissionTaker);
                }
            }
        }

        private void ChangeTradeWithMaker(BotTabSimple tabMaker, BotTabSimple tabTaker)
        {          
            if (tabMaker.Equals(_firstTab))
            {
                if (_firstChangePos) // close
                {
                    ChangeOpenTakerOrder(tabMaker, tabTaker);
                }

                if (_secondChangePos) //open
                {
                    ChangeCloseTakerOrder(tabMaker, tabTaker);
                }
            }
            else
            {
                if (_firstChangePos) //open
                {
                    ChangeCloseTakerOrder(tabMaker, tabTaker);
                }

                if (_secondChangePos) //close
                {
                    ChangeOpenTakerOrder(tabMaker, tabTaker);
                }
            }
        }

        private void RunChange(BotTabSimple tab)
        {
            if (tab.Equals(_firstTab))
            {
                if (_firstChangePos)
                {
                    decimal price = _firstBestBid;
                    decimal volume = GetVolumeClose(tab);
                    tab.CloseAtLimit(tab.PositionsLast, price, volume);
                    SetComissionClose(tab, _longСomissionTaker);
                }

                if (_secondChangePos)
                {
                    SendOpenOrders(tab);
                }
            }
            else
            {
                if (_firstChangePos)
                {
                    SendOpenOrders(tab);
                }

                if (_secondChangePos)
                {
                    decimal price = _secondBestAsk;
                    decimal volume = GetVolumeClose(tab);
                    tab.CloseAtLimit(tab.PositionsLast, price, volume);
                    SetComissionClose(tab, _shortСomissionTaker);
                }
            }

            _firstCancelOrder = false;
            _secondCancelOrder = false;
            _setTakerOrder = false;
            _tryCloseTaker = false;
        }

        private void ChangeCloseTakerOrder(BotTabSimple tabMaker, BotTabSimple tabTaker)
        {
            if (tabMaker.PositionsOpenAll.Count > 0)
            {
                if (tabTaker.PositionsLast.CloseOrders != null && tabTaker.PositionsLast.CloseOrders.Count > 0)
                {
                    if (tabMaker.PositionsLast.OpenOrders.Last().State == OrderStateType.Done &&
                        tabTaker.PositionsLast.CloseOrders.Last().State == OrderStateType.Done)
                    {
                        if (tabMaker.PositionsLast.OpenVolume * tabMaker.Security.Lot == (tabTaker.PositionsLast.MaxVolume - tabTaker.PositionsLast.OpenVolume) * tabTaker.Security.Lot)
                        {
                            if (tabMaker.PositionsLast.OpenVolume == 0)
                            {
                                return;
                            }

                            if (_regime == GetDescription(Regime.Pause))
                            {
                                _regime = GetDescription(Regime.Off);
                                _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                                return;
                            }

                            if (!CompareSpread())
                            {
                                return;
                            }

                            RunChange(tabMaker);
                        }
                    }                    
                }
                if (!TryCloseTaker(tabMaker, tabTaker))
                {
                    return;
                }
            }
            else
            {
                if (_regime == GetDescription(Regime.Pause))
                {
                    _regime = GetDescription(Regime.Off);
                    _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                    return;
                }

                if (!CompareSpread())
                {
                    return;
                }

                RunChange(tabMaker);
            }
        }

        private bool TryCloseTaker(BotTabSimple tabMaker, BotTabSimple tabTaker)
        {
            if (!_tryCloseTaker)
            {
                decimal priceTaker = _firstBestAsk + _firstTab.Security.PriceStep * 500;
                decimal comissTaker = _longСomissionTaker;

                if (tabMaker.Equals(_firstTab))
                {
                    priceTaker = _secondBestBid - _secondTab.Security.PriceStep * 500;
                    comissTaker = _shortСomissionTaker;
                }

                if (tabMaker.PositionsLast.OpenVolume * tabMaker.Security.Lot > (tabTaker.PositionsLast.MaxVolume - tabTaker.PositionsLast.OpenVolume) * tabTaker.Security.Lot &&
                    tabMaker.PositionsLast.OpenOrders.Last().State == OrderStateType.Done)
                {
                    if (tabMaker.PositionsLast.OpenVolume == 0)
                    {
                        return false;
                    }

                    if (tabTaker.PositionsLast.CloseOrders != null && tabTaker.PositionsLast.CloseOrders.Count > 0)
                    {
                        if (tabTaker.PositionsLast.CloseOrders.Last().NumberMarket == "")
                        {
                            return false;
                        }
                    }                   

                    decimal volumeTaker = GetVolumeClose(tabTaker);

                    if (volumeTaker > 0)
                    {
                        tabTaker.CloseAtLimit(tabTaker.PositionsLast, priceTaker, volumeTaker);
                        SetComissionClose(tabTaker, comissTaker);
                        _tryCloseTaker = true;
                        //SendNewLogMessage("Send Close Taker", Logging.LogMessageType.Error);
                        return false;
                    }
                }

                if (tabMaker.PositionsLast.OpenOrders.Last().State != OrderStateType.Done)
                {
                    OpenOrderChangeDepth(tabMaker);
                }
            }
            else
            {
                if (tabTaker.PositionsLast.CloseOrders != null && tabTaker.PositionsLast.CloseOrders.Count > 0)
                {
                    if (tabTaker.PositionsLast.CloseOrders.Last().State == OrderStateType.Done &&
                        tabMaker.PositionsLast.OpenVolume * tabMaker.Security.Lot == (tabTaker.PositionsLast.MaxVolume - tabTaker.PositionsLast.OpenVolume) * tabTaker.Security.Lot)
                    {
                        _tryCloseTaker = false;
                    }
                }
            }

            return true;
        }

        private void ChangeOpenTakerOrder(BotTabSimple tabMaker, BotTabSimple tabTaker)
        {
            if (tabMaker.PositionsLast.CloseOrders != null && tabMaker.PositionsLast.CloseOrders.Count > 0)
            {
                if (!SetOpenTakerOrderChangeTrade(tabMaker, tabTaker))
                {
                    return;
                }

                if (tabTaker.PositionsOpenAll.Count > 0)
                {                    
                    if (tabMaker.PositionsLast.OpenVolume > 0)
                    {
                        if (tabTaker.PositionsLast.OpenOrders.Last().State == OrderStateType.Done)
                        {
                            if ((tabMaker.PositionsLast.MaxVolume - tabMaker.PositionsLast.OpenVolume) * tabMaker.Security.Lot == tabTaker.PositionsLast.OpenVolume * tabTaker.Security.Lot)
                            {
                                if (_regime == GetDescription(Regime.Pause))
                                {
                                    _regime = GetDescription(Regime.Off);
                                    _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                                    return;
                                }

                                if (!CompareSpread())
                                {
                                    return;
                                }

                                RunChange(tabMaker);
                            }
                        }
                    }                    
                }
            }
            else
            {
                if (_regime == GetDescription(Regime.Pause))
                {
                    _regime = GetDescription(Regime.Off);
                    _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                    return;
                }

                if (!CompareSpread())
                {
                    return;
                }

                RunChange(tabMaker);
            }
        }

        private bool SetOpenTakerOrderChangeTrade(BotTabSimple tabMaker, BotTabSimple tabTaker)
        {
            if (!_setTakerOrder)
            {
                if (tabTaker.PositionsOpenAll.Count == 0)
                {
                    if (tabMaker.PositionsLast.CloseOrders.Last().State == OrderStateType.Done)
                    {
                        decimal volume = (tabMaker.PositionsLast.MaxVolume - tabMaker.PositionsLast.OpenVolume) * tabMaker.Security.Lot / tabTaker.Security.Lot;

                        if (tabMaker.Equals(_firstTab))
                        {
                            tabTaker.SellAtMarket(volume);
                            SetComissionOpen(tabTaker, _shortСomissionTaker);
                        }
                        else
                        {
                            tabTaker.BuyAtMarket(volume);
                            SetComissionOpen(tabTaker, _longСomissionTaker);
                        }

                        _setTakerOrder = true;

                        return false;
                    }
                }
                else
                {
                    if (tabMaker.PositionsLast.CloseOrders.Last().State == OrderStateType.Done &&
                        (tabMaker.PositionsLast.MaxVolume - tabMaker.PositionsLast.OpenVolume) * tabMaker.Security.Lot > tabTaker.PositionsLast.OpenVolume * tabTaker.Security.Lot)
                    {
                        decimal volume = ((tabMaker.PositionsLast.MaxVolume - tabMaker.PositionsLast.OpenVolume) * tabMaker.Security.Lot - tabTaker.PositionsLast.OpenVolume * tabTaker.Security.Lot) / tabTaker.Security.Lot;

                        if (tabMaker.Equals(_firstTab))
                        {
                            tabTaker.SellAtMarketToPosition(tabTaker.PositionsLast, volume);
                            SetComissionOpen(tabTaker, _shortСomissionTaker);
                        }
                        else
                        {
                            tabTaker.BuyAtMarketToPosition(tabTaker.PositionsLast, volume);
                            SetComissionOpen(tabTaker, _longСomissionTaker);
                        }

                        _setTakerOrder = true;

                        return false;
                    }
                    else if (tabMaker.PositionsLast.CloseOrders.Last().State == OrderStateType.Fail &&
                        (tabMaker.PositionsLast.MaxVolume - tabMaker.PositionsLast.OpenVolume) * tabMaker.Security.Lot > tabTaker.PositionsLast.OpenVolume * tabTaker.Security.Lot)
                    {
                        decimal volume = ((tabMaker.PositionsLast.MaxVolume - tabMaker.PositionsLast.OpenVolume) * tabMaker.Security.Lot - tabTaker.PositionsLast.OpenVolume * tabTaker.Security.Lot) / tabTaker.Security.Lot;

                        if (tabMaker.Equals(_firstTab))
                        {
                            tabTaker.SellAtMarketToPosition(tabTaker.PositionsLast, volume);
                            SetComissionOpen(tabTaker, _shortСomissionTaker);
                        }
                        else
                        {
                            tabTaker.BuyAtMarketToPosition(tabTaker.PositionsLast, volume);
                            SetComissionOpen(tabTaker, _longСomissionTaker);
                        }

                        _setTakerOrder = true;

                        return false;
                    }
                }

                if (tabMaker.PositionsLast.CloseOrders.Last().State != OrderStateType.Done)
                {
                    CloseOrderChangeDepth(tabMaker);
                    return false;
                }
            }

            return true;
        }

        private void CloseOrderChangeDepth(BotTabSimple tab)
        {
            decimal bestDepth = _secondBestBid;
            Side side = Side.Sell;
            decimal setDiffPrice = _secondStepOrder * _secondTab.Security.PriceStep;
            decimal comissMaker = _shortСomissionMaker;

            if (_secondTypeStepOrder == GetDescription(TypeStepOrder.Percent))
            {
                setDiffPrice = _secondStepOrder / 100 * _secondBestAsk;
            }

            if (tab.Equals(_firstTab))
            {
                bestDepth = _firstBestAsk;
                side = Side.Buy;
                comissMaker = _longСomissionMaker;

                setDiffPrice = _secondStepOrder * _secondTab.Security.PriceStep;

                if (_secondTypeStepOrder == GetDescription(TypeStepOrder.Percent))
                {
                    setDiffPrice = _secondStepOrder / 100 * _secondBestAsk;
                }
            }

            Order order = tab.PositionsLast.CloseOrders.Last();

            decimal diffPrice = Math.Abs(bestDepth - order.Price);

            if (diffPrice >= setDiffPrice)
            {
                if (order.NumberMarket == "")
                {
                    return;
                }

                if (order.State == OrderStateType.Active || order.State == OrderStateType.Partial)
                {
                    if (side == Side.Buy)
                    {
                        if (!_firstCancelOrder)
                        {
                            SendNewLogMessage("Отменяем закрывающий ордер на котировании " + side + ", price = " + order.Price + ", volume = " +
                                order.Volume + ", VolEx = " + order.VolumeExecute, Logging.LogMessageType.User);
                            tab.CloseOrder(order);
                            _firstCancelOrder = true;
                            return;
                        }
                    }
                    else if (side == Side.Sell)
                    {
                        if (!_secondCancelOrder)
                        {
                            SendNewLogMessage("Отменяем закрывающий ордер на котировании " + side + ", price = " + order.Price + ", volume = " +
                                order.Volume + ", VolEx = " + order.VolumeExecute, Logging.LogMessageType.User);
                            tab.CloseOrder(order);
                            _secondCancelOrder = true;
                            return;
                        }
                    }
                }

                if (order.State == OrderStateType.Cancel)
                {
                    decimal volume = GetVolumeClose(tab);

                    SendNewLogMessage("Отправляем закрывающий ордер на котировании " + side + ", price = " + bestDepth + ", volume = " + volume +
                        " (vol = " + order.Volume + ", volEx = " + order.VolumeExecute + ")", Logging.LogMessageType.User);

                    if (side == Side.Buy)
                    {
                        tab.CloseAtLimitUnsafe(tab.PositionsLast, bestDepth, volume);
                        SetComissionOpen(tab, _longСomissionMaker);
                        _firstCancelOrder = false;
                    }
                    else if (side == Side.Sell)
                    {
                        tab.CloseAtLimitUnsafe(tab.PositionsLast, bestDepth, volume);
                        SetComissionOpen(tab, _shortСomissionMaker);
                        _secondCancelOrder = false;
                    }
                }

                if (order.State == OrderStateType.Done)
                {
                    if (side == Side.Buy)
                    {
                        _firstCancelOrder = false;
                    }
                    else if (side == Side.Sell)
                    {
                        _secondCancelOrder = false;
                    }
                }
            }
        }

        private void ChangeTradeMakerMaker()
        {            
            if (_firstChangePos) // close
            {
                if (_firstTab.PositionsLast.CloseOrders != null && _firstTab.PositionsLast.CloseOrders.Count > 0)
                {
                    if (_firstTab.PositionsLast.CloseOrders.Last().State == OrderStateType.Done &&
                        _secondTab.PositionsLast.OpenOrders.Last().State == OrderStateType.Done)
                    {
                        if (_secondTab.PositionsLast.OpenVolume * _secondTab.Security.Lot == (_firstTab.PositionsLast.MaxVolume - _firstTab.PositionsLast.OpenVolume) * _firstTab.Security.Lot)
                        {
                            if (_firstTab.PositionsLast.OpenVolume == 0)
                            {
                                return;
                            }
                            if (_regime == GetDescription(Regime.Pause))
                            {
                                _regime = GetDescription(Regime.Off);
                                _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                                return;
                            }

                            if (!CompareSpread())
                            {
                                return;
                            }
                            
                            RunChange(_firstTab);
                            RunChange(_secondTab);
                        }
                    }

                    SetOpenTakerOrderChangeTrade(_firstTab, _secondTab);
                    TryCloseTaker(_secondTab, _firstTab);
                }
                else
                {
                    if (_regime == GetDescription(Regime.Pause))
                    {
                        _regime = GetDescription(Regime.Off);
                        _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                        return;
                    }

                    if (!CompareSpread())
                    {
                        return;
                    }

                    RunChange(_firstTab);
                    RunChange(_secondTab);
                }                
            }
            else
            {
                if (_secondTab.PositionsLast.CloseOrders != null && _secondTab.PositionsLast.CloseOrders.Count > 0)
                {
                    if (_firstTab.PositionsLast.OpenOrders.Last().State == OrderStateType.Done &&
                        _secondTab.PositionsLast.CloseOrders.Last().State == OrderStateType.Done)
                    {
                        if (_firstTab.PositionsLast.OpenVolume * _firstTab.Security.Lot == (_secondTab.PositionsLast.MaxVolume - _secondTab.PositionsLast.OpenVolume) * _secondTab.Security.Lot)
                        {
                            if (_secondTab.PositionsLast.OpenVolume == 0)
                            {
                                return;
                            }

                            if (_regime == GetDescription(Regime.Pause))
                            {
                                _regime = GetDescription(Regime.Off);
                                _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                                return;
                            }

                            if (!CompareSpread())
                            {
                                return;
                            }

                            RunChange(_firstTab);
                            RunChange(_secondTab);
                        }
                    }
                    
                    SetOpenTakerOrderChangeTrade(_secondTab, _firstTab);
                    TryCloseTaker(_firstTab, _secondTab);
                }
                else
                {
                    if (_regime == GetDescription(Regime.Pause))
                    {
                        _regime = GetDescription(Regime.Off);
                        _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                        return;
                    }

                    if (!CompareSpread())
                    {
                        return;
                    }

                    RunChange(_firstTab);
                    RunChange(_secondTab);
                }
            }
        }

        private bool CompareSpread()
        {
            if (_compareSpread == GetDescription(CompareSpreadEnum.More))
            {
                if (_currentSpread > _setSpread)
                {
                    return true;
                }
            }
            else
            {
                if (_currentSpread < _setSpread)
                {
                    return true;
                }
            }

            return false;
        }

        private bool _setTakerOrder = false;        

        private void OpenTradeWithMakerOrder(BotTabSimple tabMaker, BotTabSimple tabTaker)
        {
            if (!CheckOpenMakerOrders(tabMaker))
            {
                return;
            }

            // check position
            if (tabMaker.PositionsOpenAll.Count > 0)
            {
                if (!_setTakerOrder)
                {
                    if (tabTaker.PositionsOpenAll.Count == 0)
                    {
                        if (tabMaker.PositionsLast.OpenOrders.Last().State == OrderStateType.Done)
                        {
                            decimal volume = tabMaker.PositionsLast.OpenVolume * tabMaker.Security.Lot / tabTaker.Security.Lot;

                            if (tabMaker.Equals(_firstTab))
                            {
                                tabTaker.SellAtMarket(volume);
                                SetComissionOpen(tabTaker, _shortСomissionTaker);
                            }
                            else
                            {
                                tabTaker.BuyAtMarket(volume);
                                SetComissionOpen(tabTaker, _longСomissionTaker);
                            }

                            _setTakerOrder = true;

                            return;
                        }
                    }
                    else
                    {
                        if (tabMaker.PositionsLast.OpenOrders.Last().State == OrderStateType.Done &&
                             tabMaker.PositionsLast.OpenVolume * tabMaker.Security.Lot > tabTaker.PositionsLast.OpenVolume * tabTaker.Security.Lot)
                        {
                            decimal volume = (tabMaker.PositionsLast.OpenVolume * tabMaker.Security.Lot - tabTaker.PositionsLast.OpenVolume * tabTaker.Security.Lot) / tabTaker.Security.Lot;

                            if (tabMaker.Equals(_firstTab))
                            {
                                tabTaker.SellAtMarketToPosition(tabTaker.PositionsLast, volume);
                                SetComissionOpen(tabTaker, _shortСomissionTaker);
                            }
                            else
                            {
                                tabTaker.BuyAtMarketToPosition(tabTaker.PositionsLast, volume);
                                SetComissionOpen(tabTaker, _longСomissionTaker);
                            }

                            _setTakerOrder = true;

                            return;
                        }                        
                    }

                    if (tabMaker.PositionsLast.OpenOrders.Last().State != OrderStateType.Done)
                    {
                        OpenOrderChangeDepth(tabMaker);
                        return;
                    }
                }
                else
                {
                    if (tabTaker.PositionsLast.OpenOrders.Last().State == OrderStateType.Done &&
                     tabMaker.PositionsLast.OpenVolume * tabMaker.Security.Lot == tabTaker.PositionsLast.OpenVolume * tabTaker.Security.Lot)
                    {
                        _setTakerOrder = false;
                    }
                }
                                
                if (tabTaker.PositionsOpenAll.Count > 0)
                {
                    if (tabTaker.PositionsLast.OpenOrders.Last().State == OrderStateType.Done)
                    {
                        if (tabMaker.PositionsLast.OpenVolume * tabMaker.Security.Lot == tabTaker.PositionsLast.OpenVolume * tabTaker.Security.Lot)
                        {
                            if (_regime == GetDescription(Regime.Pause))
                            {
                                _regime = GetDescription(Regime.Off);
                                _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                                return;
                            }

                            if (!CompareSpread())
                            {
                                return;
                            }

                            SendOpenOrders(tabMaker);
                        }
                    }
                }                          
            }
            else
            {
                if (_regime == GetDescription(Regime.Pause))
                {
                    _regime = GetDescription(Regime.Off);
                    _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                    return;
                }

                if (!CompareSpread())
                {
                    return;
                }

                SendOpenOrders(tabMaker);
                _setTakerOrder = false;
            }                
        }

        private void SendOpenOrders(BotTabSimple tab)
        {
            decimal bestDepth = 0;
            Side side = Side.None;

            if (tab.Equals(_firstTab))
            {
                bestDepth = _firstBestBid;
                side = Side.Buy;                
            }
            if (tab.Equals(_secondTab))
            {
                bestDepth = _secondBestAsk;
                side = Side.Sell;                
            }

            // ставим первый ордер
            if (tab.PositionsOpenAll.Count == 0)
            {
                decimal price = bestDepth;
                decimal volume = GetVolumeOpen(side);

                SendNewLogMessage("Открываем начальный ордер " + side + ", price = " + bestDepth + ", vol = " + volume, Logging.LogMessageType.User);

                if (side == Side.Buy)
                {
                    tab.BuyAtLimit(volume, price);
                    SetComissionOpen(tab, _longСomissionMaker);
                    _firstCancelOrder = false;
                }
                else if (side == Side.Sell)
                {
                    tab.SellAtLimit(volume, price);
                    SetComissionOpen(tab, _shortСomissionMaker);
                    _secondCancelOrder = false;
                }
            }

            // ставим последующие ордера
            if (tab.PositionsOpenAll.Count > 0)
            {
                if (tab.PositionsLast.OpenOrders.Last().State == OrderStateType.Done ||
                    tab.PositionsLast.OpenOrders.Last().State == OrderStateType.Cancel)
                {
                    Position pos = tab.PositionsLast;
                    decimal price = bestDepth;
                    decimal volume = GetVolumeOpen(side);

                    SendNewLogMessage("Открываем ордер в имеющуюся позицию " + side + ", price = " + bestDepth + ", vol = " + volume, Logging.LogMessageType.User);

                    if (side == Side.Buy)
                    {
                        tab.BuyAtLimitToPosition(pos, price, volume);
                        SetComissionOpen(tab, _longСomissionMaker);
                        _firstCancelOrder = false;
                    }
                    else if (side == Side.Sell)
                    {
                        tab.SellAtLimitToPosition(pos, price, volume);
                        SetComissionOpen(tab, _shortСomissionMaker);
                        _secondCancelOrder = false;
                    }                    
                }                
            }
        }

        private bool _firstCancelOrder = false;
        private bool _secondCancelOrder = false;

        private void OpenOrderChangeDepth(BotTabSimple tab)
        {
            decimal bestDepth = _secondBestAsk;
            Side side = Side.Sell;
            decimal setDiffPrice = _secondStepOrder * _secondTab.Security.PriceStep;

            if (_secondTypeStepOrder == GetDescription(TypeStepOrder.Percent))
            {
                setDiffPrice = _secondStepOrder / 100 * _secondBestAsk;
            }            

            if (tab.Equals(_firstTab))
            {
                bestDepth = _firstBestBid;
                side = Side.Buy;

                setDiffPrice = _secondStepOrder * _secondTab.Security.PriceStep;

                if (_secondTypeStepOrder == GetDescription(TypeStepOrder.Percent))
                {
                    setDiffPrice = _secondStepOrder / 100 * _secondBestAsk;
                }
            }

            Order order = tab.PositionsLast.OpenOrders.Last();

            decimal diffPrice = Math.Abs(bestDepth - order.Price);

            if (diffPrice >= setDiffPrice)
            {               
                if (order.NumberMarket == "")
                {
                    return;
                }

                if (order.State == OrderStateType.Active || order.State == OrderStateType.Partial)
                {
                    if (side == Side.Buy)
                    {
                        if (!_firstCancelOrder)
                        {
                            SendNewLogMessage("Отменяем открывающий ордер на котировании " + side + ", price = " + order.Price + ", volume = " +
                                order.Volume + ", VolEx = " + order.VolumeExecute, Logging.LogMessageType.User);
                            tab.CloseOrder(order);
                            _firstCancelOrder = true;
                            return;
                        }                            
                    }
                    else if(side == Side.Sell)
                    {
                        if (!_secondCancelOrder)
                        {
                            SendNewLogMessage("Отменяем открывающий ордер на котировании " + side + ", price = " + order.Price + ", volume = " +
                                order.Volume + ", VolEx = " + order.VolumeExecute, Logging.LogMessageType.User);
                            tab.CloseOrder(order);
                            _secondCancelOrder = true;
                            return;
                        }
                    }
                }                         

                if (order.State == OrderStateType.Cancel)
                {
                    decimal volume = GetVolumeOpen(side);

                    SendNewLogMessage("Отправляем открывающий ордер на котировании " + side + ", price = " + bestDepth + ", volume = " + volume + 
                        " (vol = " + order.Volume + ", volEx = " + order.VolumeExecute + ")", Logging.LogMessageType.User);

                    if (side == Side.Buy)
                    {
                        tab.BuyAtLimitToPosition(tab.PositionsLast, bestDepth, volume);
                        SetComissionOpen(tab, _longСomissionMaker);
                        _firstCancelOrder = false;
                    }
                    else if (side == Side.Sell)
                    {
                        tab.SellAtLimitToPosition(tab.PositionsLast, bestDepth, volume);
                        SetComissionOpen(tab, _shortСomissionMaker);
                        _secondCancelOrder = false;
                    }
                }
                
                if (order.State == OrderStateType.Done)
                {
                    if (side == Side.Buy)
                    {
                        _firstCancelOrder = false;
                    }
                    else if (side == Side.Sell)
                    {
                        _secondCancelOrder = false;
                    }
                }
            }
        }

        private bool CheckOpenMakerOrders(BotTabSimple tab)
        {
            if (_firstTab.PositionsOpenAll.Count > 0 && _secondTab.PositionsOpenAll.Count > 0)
            {
                if (_firstTab.PositionsOpenAll[0].OpenVolume * _firstTab.Security.Lot >= _firstLimitVolume &&
                _secondTab.PositionsOpenAll[0].OpenVolume * _secondTab.Security.Lot >= _secondLimitVolume)
                {                    
                    return false;
                }
            }

            return true;
        }

        private void OpenTradeMakerMaker()
        {
            if (!CheckOpenMakerOrders(_firstTab) || !CheckOpenMakerOrders(_secondTab))
            {
                return;
            }

            if (_firstTab.PositionsOpenAll.Count > 0 && _secondTab.PositionsOpenAll.Count > 0)
            {
                if (_firstTab.PositionsLast.OpenOrders.Last().State == OrderStateType.Done &&
                    _secondTab.PositionsLast.OpenOrders.Last().State == OrderStateType.Done)
                {
                    if (_firstTab.PositionsLast.OpenVolume * _firstTab.Security.Lot == _secondTab.PositionsLast.OpenVolume * _secondTab.Security.Lot)
                    {
                        SendNewLogMessage("FirstTab OV = " + _firstTab.PositionsLast.OpenVolume + ", SecondTab OV = " + _secondTab.PositionsLast.OpenVolume, Logging.LogMessageType.User);

                        if (_regime == GetDescription(Regime.Pause))
                        {
                            _regime = GetDescription(Regime.Off);
                            _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                            return;
                        }

                        if (!CompareSpread())
                        {
                            return;
                        }

                        SendOpenOrders(_firstTab);
                        SendOpenOrders(_secondTab);

                        _firstCancelOrder = false;
                        _secondCancelOrder = false;

                        return;
                    }
                }

                if (_firstTab.PositionsLast.OpenVolume * _firstTab.Security.Lot > _secondTab.PositionsLast.OpenVolume * _secondTab.Security.Lot)
                {
                    if (_firstTab.PositionsLast.OpenOrders.Last().State == OrderStateType.Done)
                    {
                        if (!_secondCancelOrder)
                        {
                            Order order = _secondTab.PositionsLast.OpenOrders.Last();

                            if (order.NumberMarket != "")
                            {
                                decimal volume = GetVolumeOpen(Side.Sell);

                                SendNewLogMessage("FirstTab исполнился (OV = " + _firstTab.PositionsLast.OpenVolume + ", отменяем открывающий ордер, State = " + order.State + ", price = " + order.Price + ", volume = " +
                                        order.Volume + ", VolEx = " + order.VolumeExecute, Logging.LogMessageType.User);

                                _secondTab.CloseOrder(order);
                                _secondCancelOrder = true;

                                SendNewLogMessage("SecondTab OV = " + _secondTab.PositionsLast.OpenVolume + ", ставим рыночный открывающий ордер, volume = " + volume, Logging.LogMessageType.User);

                                _secondTab.SellAtMarketToPosition(_secondTab.PositionsLast, volume);
                                SetComissionOpen(_secondTab, _shortСomissionTaker);

                                return;
                            }                            
                        }
                    }
                }
                
                OpenOrderChangeDepth(_firstTab);

                if (_firstTab.PositionsLast.OpenVolume * _firstTab.Security.Lot < _secondTab.PositionsLast.OpenVolume * _secondTab.Security.Lot)
                {
                    if (_secondTab.PositionsLast.OpenOrders.Last().State == OrderStateType.Done)
                    {
                        if (!_firstCancelOrder)
                        {
                            Order order = _firstTab.PositionsLast.OpenOrders.Last();

                            if (order.NumberMarket != "")
                            {
                                decimal volume = GetVolumeOpen(Side.Buy);

                                SendNewLogMessage("SecondTab исполнился (OV = " + _secondTab.PositionsLast.OpenVolume + ", отменяем открывающий ордер, State = " + order.State + ", price = " + order.Price + ", volume = " +
                                        order.Volume + ", VolEx = " + order.VolumeExecute, Logging.LogMessageType.User);

                                _firstTab.CloseOrder(order);
                                _firstCancelOrder = true;

                                SendNewLogMessage("FirstTab OV = " + _firstTab.PositionsLast.OpenVolume + ", ставим рыночный открывающий ордер, volume = " + volume, Logging.LogMessageType.User);

                                _firstTab.BuyAtMarketToPosition(_firstTab.PositionsLast, volume);
                                SetComissionOpen(_firstTab, _longСomissionTaker);

                                return;
                            }                            
                        }
                    }
                }
                
                OpenOrderChangeDepth(_secondTab);
            }
            else
            {
                if (_regime == GetDescription(Regime.Pause))
                {
                    _regime = GetDescription(Regime.Off);
                    _gridSettingsPosition.Rows[0].Cells[2].Value = _listRegime[0];

                    return;
                }

                if (!CompareSpread())
                {
                    return;
                }

                SendOpenOrders(_firstTab);
                SendOpenOrders(_secondTab);

                _firstCancelOrder = false;
                _secondCancelOrder = false;
            }
        }

        #endregion

        #region Enum

        private enum CompareSpreadEnum
        {
            [Description("Больше")]
            More,
            [Description("Меньше")]
            Less
        }

        private enum Regime
        {
            [Description("Включено")]
            On,
            [Description("Выключено")]
            Off,
            [Description("Пауза")]
            Pause
        }
        private enum TypeStepOrder
        {
            PriceStep,
            Percent
        }
        private enum TypeLimit
        {
            USDT,
            Token
        }

        private enum SchemeOrderSettings
        {
            [Description("Maker/Taker")]
            MakerTaker,
            [Description("Taker/Maker")]
            TakerMaker,
            [Description("Maker(taker)/Maker(taker)")]
            MakerMaker,
            [Description("Taker/Taker")]
            TakerTaker 
        }
        private enum TypeSpread
        {
            Percent,
            USDT
        }

        private enum Exercise
        {
            [Description("Набор позиции")]
            Open,
            [Description("Закрытие позиции")]
            Close,
            [Description("Изменение позиции")]
            Change
        }

        private string GetDescription(Enum enumValue)
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

        #endregion

        #region Save/Load

        private void SaveSettings()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Engine\" + NameStrategyUniq + @"TablesParam.txt", false)
                    )
                {                  
                    writer.WriteLine("_exercise|" + _exercise);
                    writer.WriteLine("_firstTypeLimit|" + _firstTypeLimit);
                    writer.WriteLine("_secondTypeLimit|" + _secondTypeLimit);
                    writer.WriteLine("_firstLimitVolume|" + _firstLimitVolumeTable);
                    writer.WriteLine("_secondLimitVolume|" + _secondLimitVolumeTable);
                    writer.WriteLine("_schemeOrderSettings|" + _schemeOrderSettings);
                    writer.WriteLine("_firstTypeStepOrder|" + _firstTypeStepOrder);
                    writer.WriteLine("_secondTypeStepOrder|" + _secondTypeStepOrder);
                    writer.WriteLine("_firstStepOrder|" + _firstStepOrder);
                    writer.WriteLine("_secondStepOrder|" + _secondStepOrder);
                    writer.WriteLine("_maxVolume|" + _maxVolume);
                    writer.WriteLine("_typeSpread|" + _typeSpread);
                    writer.WriteLine("_setSpread|" + _setSpread);
                    writer.WriteLine("_compareSpread|" + _compareSpread);
                    writer.WriteLine("_longСomissionTaker|" + _longСomissionTaker);
                    writer.WriteLine("_shortСomissionTaker|" + _shortСomissionTaker);
                    writer.WriteLine("_longСomissionMaker|" + _longСomissionMaker);
                    writer.WriteLine("_shortСomissionMaker|" + _shortСomissionMaker);

                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                Thread.Sleep(5000);
            }            
        }

        private void LoadSettings()
        {
            if (MainWindow.GetDispatcher.CheckAccess() == false)
            {
                MainWindow.GetDispatcher.Invoke(new Action(LoadSettings));
                return;
            }

            if (!File.Exists(@"Engine\" + NameStrategyUniq + @"TablesParam.txt"))
            {
                return;
            }

            try
            {
                List<string> lines = new List<string>();

                using (StreamReader reader = new StreamReader(@"Engine\" + NameStrategyUniq + @"TablesParam.txt"))
                {
                    for(int i = 0; i < 18;  i++) 
                    {
                        lines.Add(reader.ReadLine().Split('|')[1]);
                    }                        

                    reader.Close();
                }

                if (lines.Count == 0)
                {
                    return;
                }

                _gridSettingsPosition.Rows[0].Cells[1].Value = _listExercise.Contains(lines[0]) ? _listExercise[_listExercise.IndexOf(lines[0])] : _listExercise[0];
                _gridSettingsPosition.Rows[5].Cells[1].Value = _listTypelimit.Contains(lines[1]) ? _listTypelimit[_listTypelimit.IndexOf(lines[1])] : _listTypelimit[0];
                _gridSettingsPosition.Rows[5].Cells[2].Value = _listTypelimit.Contains(lines[2]) ? _listTypelimit[_listTypelimit.IndexOf(lines[2])] : _listTypelimit[0];
                _gridSettingsPosition.Rows[6].Cells[1].Value = lines[3];
                _gridSettingsPosition.Rows[6].Cells[2].Value = lines[4];
                _gridSettingsPosition.Rows[7].Cells[1].Value = _listScheme.Contains(lines[5]) ? _listScheme[_listScheme.IndexOf(lines[5])] : _listScheme[0];
                _gridSettingsPosition.Rows[8].Cells[1].Value = _listTypeStepOrder.Contains(lines[6]) ? _listTypeStepOrder[_listTypeStepOrder.IndexOf(lines[6])] : _listTypeStepOrder[0];
                _gridSettingsPosition.Rows[8].Cells[2].Value = _listTypeStepOrder.Contains(lines[7]) ? _listTypeStepOrder[_listTypeStepOrder.IndexOf(lines[7])] : _listTypeStepOrder[0];
                _gridSettingsPosition.Rows[9].Cells[1].Value = lines[8];
                _gridSettingsPosition.Rows[9].Cells[2].Value = lines[9];
                _gridSettingsPosition.Rows[10].Cells[1].Value = lines[10];
                _gridSettingsPosition.Rows[11].Cells[1].Value = _listTypeSpread.Contains(lines[11]) ? _listTypeSpread[_listTypeSpread.IndexOf(lines[11])] : _listTypeSpread[0];
                _gridSettingsPosition.Rows[12].Cells[1].Value = lines[12];
                _gridSettingsPosition.Rows[12].Cells[2].Value = _listCompareSpread.Contains(lines[13]) ? _listCompareSpread[_listCompareSpread.IndexOf(lines[13])] : _listCompareSpread[0];
                _gridSettingsPosition.Rows[13].Cells[1].Value = lines[14];
                _gridSettingsPosition.Rows[13].Cells[2].Value = lines[15];
                _gridSettingsPosition.Rows[14].Cells[1].Value = lines[16];
                _gridSettingsPosition.Rows[14].Cells[2].Value = lines[17];
                }
            catch (Exception e)
            {
                SendNewLogMessage(e.ToString(), Logging.LogMessageType.Error);
                Thread.Sleep(5000);
            }
        }

        #endregion       
    }
}


