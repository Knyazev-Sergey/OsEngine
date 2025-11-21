using Newtonsoft.Json;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using JournalAll = OsEngine.Journal.Journal;
using OsEngine.Journal;
using OsEngine.OsTrader;

namespace OsEngine.Robots
{
    [Bot ("TaxAccounting")]
    public class TaxAccounting : BotPanel
    {
        private BotTabSimple _tab;
        private StrategyParameterString _regime;

        public TaxAccounting(string name, StartProgram startProgram) : base(name, startProgram)
        {
            if (startProgram != StartProgram.IsTester)
            {
                SendNewLogMessage("Бот работает только в Тестере", Logging.LogMessageType.Error);                
                return;
            }

            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            string tabName = " Параметры ";

            _regime = CreateParameter("Режим", "Off", new string[] { "Off", "On" }, tabName);

            CustomTabToParametersUi customTab = ParamGuiSettings.CreateCustomTab(" Периоды ");

            CreateTable();
            customTab.AddChildren(_host);

            LoadTable();

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
        }

        #region Table Periods

        private WindowsFormsHost _host;

        private DataGridView _dgv;

        private List<ListTablePeriods> _listTable = new();

        private void CreateTable()
        {
            _host = new WindowsFormsHost();

            _dgv = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.CellSelect, DataGridViewAutoSizeRowsMode.AllCells);

            _dgv.Dock = DockStyle.Fill;
            _dgv.ScrollBars = ScrollBars.Vertical;
            _dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _dgv.GridColor = Color.Gray;
            _dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            _dgv.ColumnHeadersDefaultCellStyle.Font = new Font(_dgv.Font, FontStyle.Bold | FontStyle.Italic);
            _dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _dgv.ColumnCount = 3;
            _dgv.RowCount = 1;

            _dgv.Columns[0].HeaderText = "Год";
            _dgv.Columns[1].HeaderText = "Ставка";

            DataGridViewButtonCell cellButton = new();

            _dgv.Rows[^1].Cells[0] = cellButton;
            _dgv.Rows[^1].Cells[0].Value = "Добавить строку";
            _dgv.Rows[^1].Cells[0].ReadOnly = true;
            _dgv.Rows[^1].Cells[1].ReadOnly = true;


            _dgv.Rows[^1].Cells[2].ReadOnly = true;

            foreach (DataGridViewColumn column in _dgv.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            _dgv.CellClick += _dgv_CellClick;
            _dgv.CellValueChanged += _dgv_CellValueChanged;
            _dgv.DataError += _dgv_DataError;

            _host.Child = _dgv;
        }

        private void _dgv_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            
        }

        private void _dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex == _dgv.RowCount - 1 && e.ColumnIndex == 0)
                {
                    AddRow();
                }

                if (e.ColumnIndex == 2)
                {
                    if (e.RowIndex > -1 && e.RowIndex < _dgv.RowCount - 1)
                    {
                        DeleteRow(e.RowIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void AddRow()
        {
            DataGridViewButtonCell cellButton = new();
            _dgv.Rows.Insert(_dgv.RowCount - 1);

            _dgv.Rows[^2].Cells[2] = cellButton;
            _dgv.Rows[^2].Cells[2].Value = "Удалить строку";
            _dgv.Rows[^2].Cells[2].ReadOnly = true;

            SaveTable();
        }

        private void DeleteRow(int rowIndex)
        {
            int year = int.Parse(_dgv[0, rowIndex].Value.ToString());
            int deleteIndex = _listTable.FindIndex(x => x.Year == year);

            if (deleteIndex > -1)
            {
                _listTable.RemoveAt(deleteIndex);
            }
            
            _dgv.Rows.RemoveAt(rowIndex);

            SaveTable();
        }

        private void _dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex != _dgv.RowCount - 1 && e.ColumnIndex != 2)
                {
                    int year = 0;
                    int.TryParse(_dgv.Rows[e.RowIndex].Cells[0].Value?.ToString(), out year);

                    if (year == 0)
                    {
                        return;
                    }

                    decimal rate = 0;
                    decimal.TryParse(_dgv.Rows[e.RowIndex].Cells[1].Value?.ToString().Replace(".", ","), out rate);

                    ListTablePeriods list = new();
                    list.Year = year;
                    list.Rate = rate;

                    for (int i = 0; i < _dgv.RowCount - 1; i++)
                    {
                        int valueYear = 0;
                        int.TryParse(_dgv.Rows[i].Cells[0].Value?.ToString(), out valueYear);

                        if (valueYear == year)
                        {
                            if (i == e.RowIndex)
                            {
                                int index = _listTable.FindIndex(x => x.Year == year);

                                if (index > -1)
                                {
                                    _listTable[index] = list;
                                }
                                else
                                {
                                    _listTable.Add(list);
                                }

                                SaveTable();
                            }
                            else
                            {
                                _dgv.Rows[e.RowIndex].Cells[0].Value = "";
                                SendNewLogMessage("В таблице уже есть такой год", Logging.LogMessageType.Error);
                            }
                        }
                    }

                    for (int i = 0; i < _listTable.Count; i++)
                    {
                        int count = 0;

                        for (int j = 0; j < _dgv.RowCount - 1; j++)
                        {
                            int valueYear = 0;
                            int.TryParse(_dgv.Rows[j].Cells[0].Value?.ToString(), out valueYear);

                            if (_listTable[i].Year == valueYear)
                            {
                                count++;
                                break;
                            }
                        }

                        if (count == 0)
                        {
                            _listTable.RemoveAt(i);
                            i--;
                            SaveTable();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void LoadTable()
        {
            try
            {
                string fileName = @"Engine\" + NameStrategyUniq + @"TablePeriod.json";

                if (!File.Exists(fileName))
                {
                    return;
                }

                string json = File.ReadAllText(fileName);
                _listTable = JsonConvert.DeserializeObject<List<ListTablePeriods>>(json);

                for (int i = 0; i < _listTable.Count; i++)
                {
                    DataGridViewRow row = new();
                    row.Cells.Add(new DataGridViewTextBoxCell() { Value = _listTable[i].Year });
                    row.Cells.Add(new DataGridViewTextBoxCell() { Value = _listTable[i].Rate });
                    row.Cells.Add(new DataGridViewButtonCell() { Value = "Удалить строку" });

                    _dgv.Rows.Insert(_dgv.RowCount - 1, row);
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void SaveTable()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_listTable, Formatting.Indented);
                File.WriteAllText(@"Engine\" + NameStrategyUniq + @"TablePeriod.json", json);
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        #endregion

        #region Main Logic

        private void _tab_CandleFinishedEvent(List<Candle> candle)
        {
            try
            {
                if (candle.Count < 2)
                {
                    return;
                }

                if (candle[^1].TimeStart.Year > candle[^2].TimeStart.Year)
                {
                    MainLogic(candle[^2].TimeStart.Year);
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        BotPanelJournal allJournal;

        private void MainLogic(int year)
        {
            decimal profit = 0;

            List<BotPanel> bots = OsTraderMaster.Master.PanelsArray;

            List<Journal.Journal> journals = bots[0].GetJournals();


            for (int i = 0; i < allJournal._Tabs.Count; i++)
            {   
                for (int i1 = 0; i1 < allJournal._Tabs[i].Journal.CloseAllPositions.Count; i1++)
                {
                    
                }
                
            }
        }


        #endregion
    }

    public class ListTablePeriods
    {
        public int Year;
        public decimal Rate;
    }
}
