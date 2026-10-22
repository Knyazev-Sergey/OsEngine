using OsEngine.Entity;
using OsEngine.Language;
using OsEngine.OsTrader.Panels.Tab;
using QuikSharp.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OsEngine.Robots.AlexBots
{
    /// <summary>
    /// Interaction logic for TripleArbitrageAssistantVolumeUi.xaml
    /// </summary>
    public partial class TripleArbitrageAssistantVolumeUi : Window
    {
        TripleArbitrageAssistant _arbitrage;

        public TripleArbitrageAssistantVolumeUi(TripleArbitrageAssistant arbitrage)
        {
            InitializeComponent();

            _arbitrage = arbitrage;

            Title = "  " + _arbitrage.NameStrategyUniq;

            CreateTable();

            Thread worker = new Thread(PainterThread);
            worker.Start();

            this.Closed += TripleArbitrageAssistantVolumeUi_Closed;
        }

        private void TripleArbitrageAssistantVolumeUi_Closed(object sender, EventArgs e)
        {
            _isClosed = true;
            _arbitrage = null;
        }

        private bool _isClosed = false;

        private void CreateTable()
        {
            _grid = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect,
    DataGridViewAutoSizeRowsMode.None);

            _grid.ScrollBars = ScrollBars.Vertical;

            DataGridViewTextBoxCell cell0 = new DataGridViewTextBoxCell();
            cell0.Style = _grid.DefaultCellStyle;

            DataGridViewColumn column0 = new DataGridViewColumn();
            column0.CellTemplate = cell0;
            column0.HeaderText = "Step Num";
            column0.ReadOnly = true;
            column0.Width = 100;

            _grid.Columns.Add(column0);

            DataGridViewColumn column1 = new DataGridViewColumn();
            column1.CellTemplate = cell0;
            column1.HeaderText = "Operation 1";
            column1.ReadOnly = false;
            column1.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns.Add(column1);

            DataGridViewColumn column2 = new DataGridViewColumn();
            column2.CellTemplate = cell0;
            column2.HeaderText = "Operation 2";
            column2.ReadOnly = false;
            column2.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns.Add(column2);

            DataGridViewColumn column3 = new DataGridViewColumn();
            column3.CellTemplate = cell0;
            column3.HeaderText = "Operation 3";
            column3.ReadOnly = false;
            column3.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns.Add(column3);

            Host.Child = _grid;
        }

        DataGridView _grid;

        private void PainterThread()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(2000);

                    if (_isClosed == true)
                    {
                        return;
                    }

                    PaintTable();
                }
                catch (Exception error)
                {
                    if (_isClosed == true)
                    {
                        return;
                    }
                    System.Windows.MessageBox.Show(error.ToString());
                }
            }
        }

        private void PaintTable()
        {
            if (Host.Dispatcher.CheckAccess() == false)
            {
                Host.Dispatcher.Invoke(new Action(PaintTable));
                return;
            }

            try
            {
                if (_grid.Rows.Count == 0)
                {
                    _grid.Rows.Add(GetRow(_arbitrage.VolumeTable.RowEndVolume, 0));

                    DataGridViewRow emptyRow = new DataGridViewRow();

                    _grid.Rows.Add(emptyRow);
                }

                // 1 проверяем чтобы первая строка совпадала

                DataGridViewRow firstRow = GetRow(_arbitrage.VolumeTable.RowEndVolume, 0);

                if (IsNoteEqual(firstRow, _grid.Rows[0]))
                {
                    _grid.Rows.RemoveAt(0);
                    _grid.Rows.Insert(0, firstRow);
                }

                // 2 собираем текущую последовательность

                List<DataGridViewRow> rows = new List<DataGridViewRow>();

                for (int i = 0; i < _arbitrage.VolumeTable.RowsAllToOpen.Count; i++)
                {
                    rows.Add(GetRow(_arbitrage.VolumeTable.RowsAllToOpen[i], i + 1));
                }

                if (rows.Count + 2 != _grid.Rows.Count)
                {
                    while (_grid.Rows.Count > 2)
                    {
                        _grid.Rows.RemoveAt(_grid.Rows.Count - 1);
                    }
                }

                for (int i = 0; i < rows.Count; i++)
                {
                    if (i + 2 >= _grid.Rows.Count)
                    {
                        _grid.Rows.Add(rows[i]);
                    }

                    if (IsNoteEqual(rows[i], _grid.Rows[i + 2]))
                    {
                        _grid.Rows.RemoveAt(i + 2);
                        _grid.Rows.Insert(i + 2, rows[i]);
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        private DataGridViewRow GetRow(VolumeRow volumeRow, int num)
        {
            // Num, Class, Type, Sec code, Last, Bid, Ask, Positions count, Chart 

            DataGridViewRow nRow = new DataGridViewRow();

            nRow.Cells.Add(new DataGridViewTextBoxCell());
            nRow.Cells[0].Value = num;

            nRow.Cells.Add(new DataGridViewTextBoxCell());
            if (volumeRow.VolumeCells.Count >= 1)
            {
                nRow.Cells[1].Value = volumeRow.VolumeCells[0].GetString(); // Operation 1

                if (volumeRow.VolumeCells[0].IsEnded)
                {
                    nRow.Cells[1].Style.BackColor = System.Drawing.Color.DarkGreen;
                }
            }
            else
            {
                nRow.Cells[1].Value = "";
            }

            nRow.Cells.Add(new DataGridViewTextBoxCell());
            if (volumeRow.VolumeCells.Count >= 2)
            {
                nRow.Cells[2].Value = volumeRow.VolumeCells[1].GetString(); // Operation 2

                if (volumeRow.VolumeCells[1].IsEnded)
                {
                    nRow.Cells[2].Style.BackColor = System.Drawing.Color.DarkGreen;
                }
            }
            else
            {
                nRow.Cells[2].Value = "";
            }

            nRow.Cells.Add(new DataGridViewTextBoxCell());

            if (volumeRow.VolumeCells.Count >= 3)
            {
                nRow.Cells[3].Value = volumeRow.VolumeCells[2].GetString();// Operation 3

                if (volumeRow.VolumeCells[2].IsEnded)
                {
                    nRow.Cells[3].Style.BackColor = System.Drawing.Color.DarkGreen;
                }
            }
            else
            {
                nRow.Cells[3].Value = "";
            }

            return nRow;
        }

        private bool IsNoteEqual(DataGridViewRow row1, DataGridViewRow row2)
        {
            if (row1.Cells.Count != row2.Cells.Count)
            {
                return true;
            }

            for (int i = 0; i < row1.Cells.Count; i++)
            {
                DataGridViewCell cell1 = row1.Cells[i];
                DataGridViewCell cell2 = row2.Cells[i];

                if (cell1.Value == null &&
                     cell2.Value == null)
                {
                    continue;
                }

                if (cell1.Value == null &&
                    cell2.Value != null)
                {
                    return true;
                }

                if (cell1.Value != null &&
                    cell2.Value == null)
                {
                    return true;
                }

                if (cell1.Value.ToString() !=
                    cell2.Value.ToString())
                {
                    return true;
                }

                if (cell1.Style.BackColor != cell2.Style.BackColor)
                {
                    return true;
                }
            }

            return false;
        }
    }
}