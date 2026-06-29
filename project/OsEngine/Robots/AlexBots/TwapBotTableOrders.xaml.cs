using OsEngine.Entity;
using OsEngine.Logging;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;

namespace OsEngine.Robots.AlexBots
{
    /// <summary>
    /// Логика взаимодействия для TwapBotTableOrders.xaml
    /// </summary>
    public partial class TwapBotTableOrders : Window
    {
        public TwapBotTableOrders(TwapBot bot)
        {
            InitializeComponent();

            _bot = bot;

            CreateGrid();
            RePaintGrid();

            _bot.ArbitrationStepUpdateEvent += _bot_ArbitrationStepUpdateEvent;
            _bot.ChartClosedEvent += _bot_ChartClosedEvent;
        }

        private void _bot_ChartClosedEvent(string obj)
        {
            _bot.ArbitrationStepUpdateEvent -= _bot_ArbitrationStepUpdateEvent;
            _bot.ChartClosedEvent -= _bot_ChartClosedEvent;
        }

        private void _bot_ArbitrationStepUpdateEvent()
        {
            RePaintGrid();
        }

        private void CreateGrid()
        {
            _gridTableOrders = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.CellSelect, DataGridViewAutoSizeRowsMode.AllCells, false);
            _gridTableOrders.ScrollBars = ScrollBars.Vertical;
            DataGridViewTextBoxCell cellStyle = new DataGridViewTextBoxCell();
            cellStyle.Style = _gridTableOrders.DefaultCellStyle;

            for (int i = 0; i < 9; i++)
            {
                DataGridViewColumn columN = new DataGridViewColumn();
                columN.CellTemplate = cellStyle;
                columN.HeaderText = "";
                columN.ReadOnly = true;
                columN.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                _gridTableOrders.Columns.Add(columN);
            }

            _gridTableOrders.CellClick += _gridTableOrders_CellClick;
            _gridTableOrders.DataError += _grid_DataError;

            TableOrderHost.Child = _gridTableOrders;

            _gridActiveStatistics = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.CellSelect, DataGridViewAutoSizeRowsMode.AllCells, false);
            _gridActiveStatistics.ScrollBars = ScrollBars.Vertical;

            for (int i = 0; i < 8; i++)
            {
                DataGridViewColumn columN = new DataGridViewColumn();
                columN.CellTemplate = cellStyle;
                columN.HeaderText = "";
                columN.ReadOnly = true;
                columN.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                _gridActiveStatistics.Columns.Add(columN);
            }

            ActiveGeneralStatisticHost.Child = _gridActiveStatistics;

            _gridFinishStatistics = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.CellSelect, DataGridViewAutoSizeRowsMode.AllCells, false);
            _gridFinishStatistics.ScrollBars = ScrollBars.Vertical;

            for (int i = 0; i < 8; i++)
            {
                DataGridViewColumn columN = new DataGridViewColumn();
                columN.CellTemplate = cellStyle;
                columN.HeaderText = "";
                columN.ReadOnly = true;
                columN.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                _gridFinishStatistics.Columns.Add(columN);
            }

            FinishGeneralStatisticHost.Child = _gridFinishStatistics;

            _gridCancelStatistics = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.CellSelect, DataGridViewAutoSizeRowsMode.AllCells, false);
            _gridCancelStatistics.ScrollBars = ScrollBars.Vertical;

            for (int i = 0; i < 8; i++)
            {
                DataGridViewColumn columN = new DataGridViewColumn();
                columN.CellTemplate = cellStyle;
                columN.HeaderText = "";
                columN.ReadOnly = true;
                columN.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                _gridCancelStatistics.Columns.Add(columN);
            }

            CancelGeneralStatisticHost.Child = _gridCancelStatistics;
        }

        private void RePaintGrid()
        {
            try
            {
                UpdateGridTableOrders();
                UpdateGridActiveStatistics();
                UpdateGridFinishStatistics();
                UpdateGridCancelStatistics();
            }
            catch (Exception ex)
            {
                _bot.SendNewLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private void UpdateGridCancelStatistics()
        {
            if (_gridCancelStatistics == null) return;
            if (_gridCancelStatistics.InvokeRequired)
            {
                _gridCancelStatistics.Invoke(new Action(UpdateGridCancelStatistics));
                return;
            }

            List<DataGridViewRow> rowsCancel = new List<DataGridViewRow>();
            rowsCancel.Add(GetFirstGridRowGeneralStatistics());

            if (_bot.TwapCancelStatistics != null && _bot.TwapCancelStatistics.Count > 0)
            {
                for (int i = _bot.TwapCancelStatistics.Count - 1; i >= 0; i--)
                {
                    rowsCancel.Add(GetRowsToGridGeneralStatistics(_bot.TwapCancelStatistics[i]));
                }

                UpdateDataGridView(_gridCancelStatistics, rowsCancel);
            }
            else
            {
                UpdateDataGridView(_gridCancelStatistics, rowsCancel);
            }
        }

        private void UpdateGridFinishStatistics()
        {
            if (_gridFinishStatistics == null) return;
            if (_gridFinishStatistics.InvokeRequired)
            {
                _gridFinishStatistics.Invoke(new Action(UpdateGridFinishStatistics));
                return;
            }

            List<DataGridViewRow> rowsFinish = new List<DataGridViewRow>();
            rowsFinish.Add(GetFirstGridRowGeneralStatistics());

            if (_bot.TwapFinishStatistics != null && _bot.TwapFinishStatistics.Count > 0)
            {
                for (int i = _bot.TwapFinishStatistics.Count - 1; i >= 0; i--)
                {
                    rowsFinish.Add(GetRowsToGridGeneralStatistics(_bot.TwapFinishStatistics[i]));
                }

                UpdateDataGridView(_gridFinishStatistics, rowsFinish);
            }
            else
            {
                UpdateDataGridView(_gridFinishStatistics, rowsFinish);
            }
        }

        private void UpdateGridActiveStatistics()
        {
            if (_gridActiveStatistics == null) return;
            if (_gridActiveStatistics.InvokeRequired)
            {
                _gridActiveStatistics.Invoke(new Action(UpdateGridActiveStatistics));
                return;
            }

            List<DataGridViewRow> rowsActive = new List<DataGridViewRow>();
            rowsActive.Add(GetFirstGridRowGeneralStatistics());

            if (_bot.TwapActiveStatistics != null)
            {
                rowsActive.Add(GetRowsToGridGeneralStatistics(_bot.TwapActiveStatistics));
                UpdateDataGridView(_gridActiveStatistics, rowsActive);
            }
            else
            {
                UpdateDataGridView(_gridActiveStatistics, rowsActive);
            }
        }

        private void UpdateGridTableOrders()
        {
            if (_gridTableOrders == null) return;
            if (_gridTableOrders.InvokeRequired)
            {
                _gridTableOrders.Invoke(new Action(UpdateGridTableOrders));
                return;
            }

            UpdateDataGridView(_gridTableOrders, GetRowsToGridTableOrders());
        }

        private void UpdateDataGridView(DataGridView grid, List<DataGridViewRow> newRows)
        {
            if (newRows == null) return;

            int existingCount = grid.Rows.Count;
            int newCount = newRows.Count;

            if (existingCount != newCount)
            {
                int showRow = grid.FirstDisplayedScrollingRowIndex;
                grid.Rows.Clear();

                for (int i = 0; i < newCount; i++)
                    grid.Rows.Add(newRows[i]);

                if (showRow > 0 && showRow < grid.Rows.Count)
                    grid.FirstDisplayedScrollingRowIndex = showRow;
            }
            else
            {
                for (int i = 0; i < newCount; i++)
                {
                    for (int j = 0; j < grid.ColumnCount; j++)
                    {
                        DataGridViewCell existingCell = grid.Rows[i].Cells[j];
                        DataGridViewCell newCell = newRows[i].Cells[j];

                        if (!Equals(existingCell.Value, newCell.Value))
                            existingCell.Value = newCell.Value;

                        if (existingCell.Style.ForeColor != newCell.Style.ForeColor)
                            existingCell.Style.ForeColor = newCell.Style.ForeColor;

                        if (existingCell.Style.BackColor != newCell.Style.BackColor)
                            existingCell.Style.BackColor = newCell.Style.BackColor;
                    }
                }
            }
        }

        private DataGridViewRow GetFirstGridRowGeneralStatistics()
        {
            DataGridViewRow nRow = new DataGridViewRow();

            DataGridViewTextBoxCell cell0 = new DataGridViewTextBoxCell(); // 0 Время старта
            cell0.Value = "Номер";
            cell0.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell0);

            DataGridViewTextBoxCell cell1 = new DataGridViewTextBoxCell(); // 0 Время старта
            cell1.Value = "Время старта";
            cell1.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell1);

            DataGridViewTextBoxCell cell2 = new DataGridViewTextBoxCell(); // 1 Время окончания
            cell2.Value = "Время окончания";
            cell2.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell2);

            DataGridViewTextBoxCell cell3 = new DataGridViewTextBoxCell(); // 2 Средняя цена
            cell3.Value = "Средняя цена";
            cell3.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell3);

            DataGridViewTextBoxCell cell4 = new DataGridViewTextBoxCell(); // 3 Объем набран
            cell4.Value = "Объем набран (лот)";
            cell4.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell4);

            DataGridViewTextBoxCell cell5 = new DataGridViewTextBoxCell(); // 4 Нужный объем
            cell5.Value = "Нужный объем (лот)";
            cell5.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell5);

            DataGridViewTextBoxCell cell6 = new DataGridViewTextBoxCell(); // 5 Направление
            cell6.Value = "Направление";
            cell6.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell6);

            DataGridViewTextBoxCell cell7 = new DataGridViewTextBoxCell(); // 6 Объем набран в %
            cell7.Value = "Объем набран в %";
            cell7.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell7);

            return nRow;
        }

        private DataGridViewRow GetFirstGridRowTableOrders()
        {
            DataGridViewRow nRow = new DataGridViewRow();

            DataGridViewTextBoxCell cell0 = new DataGridViewTextBoxCell(); // 0 Номер шага
            cell0.Value = "Шаг";
            cell0.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell0);

            DataGridViewTextBoxCell cell1 = new DataGridViewTextBoxCell(); // 1 Время старта шага
            cell1.Value = "Время старта";
            cell1.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell1);

            DataGridViewTextBoxCell cell2 = new DataGridViewTextBoxCell(); // 2 Время окончания шага
            cell2.Value = "Время окончания";
            cell2.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell2);

            DataGridViewTextBoxCell cell3 = new DataGridViewTextBoxCell(); // 3 Id ордера
            cell3.Value = "ID ордера";
            cell3.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell3);

            DataGridViewTextBoxCell cell4 = new DataGridViewTextBoxCell(); // 4 Статус
            cell4.Value = "Статус";
            cell4.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell4);

            DataGridViewTextBoxCell cell5 = new DataGridViewTextBoxCell(); // 5 Открытый объем
            cell5.Value = "Открытый объем";
            cell5.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell5);

            DataGridViewTextBoxCell cell6 = new DataGridViewTextBoxCell(); // 6 Нужный объем
            cell6.Value = "Нужный объем";
            cell6.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell6);

            DataGridViewTextBoxCell cell7 = new DataGridViewTextBoxCell(); // 7 Направление
            cell7.Value = "Направление";
            cell7.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell7);

            DataGridViewTextBoxCell cell8 = new DataGridViewTextBoxCell(); // 8 Отмена ордера
            cell8.Value = "Отмена ордера";
            cell8.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell8);

            return nRow;
        }

        private DataGridViewRow GetRowsToGridGeneralStatistics(TwapStatistic statistic)
        {
            DataGridViewRow nRow = new DataGridViewRow();

            if (statistic == null)
            {
                return nRow;
            }

            DataGridViewTextBoxCell cell0 = new DataGridViewTextBoxCell(); // 0 Номер
            cell0.Value = statistic.Number;
            cell0.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell0);

            DataGridViewTextBoxCell cell1 = new DataGridViewTextBoxCell(); // 1 Время старта
            cell1.Value = statistic.StartTime;
            cell1.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell1);

            DataGridViewTextBoxCell cell2 = new DataGridViewTextBoxCell(); // 2 Время окончания
            cell2.Value = statistic.EndTime;
            cell2.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell2);

            DataGridViewTextBoxCell cell3 = new DataGridViewTextBoxCell(); // 3 Средняя цена
            cell3.Value = statistic.AveragePrice;
            cell3.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell3);

            DataGridViewTextBoxCell cell4 = new DataGridViewTextBoxCell(); // 4 Объем набран
            cell4.Value = statistic.OpenVolumeInLot;
            cell4.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell4);

            DataGridViewTextBoxCell cell5 = new DataGridViewTextBoxCell(); // 5 Нужный объем
            cell5.Value = statistic.NeedVolumeInLot;
            cell5.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell5);

            DataGridViewTextBoxCell cell6 = new DataGridViewTextBoxCell(); // 6 Направление
            cell6.Value = statistic.Direction;
            cell6.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell6);

            DataGridViewTextBoxCell cell7 = new DataGridViewTextBoxCell(); // 7 Объем набран в %

            if (statistic.OpenVolumeInLot == 0 || statistic.NeedVolumeInLot == 0)
            {
                cell7.Value = "0";
            }
            else
            {
                cell7.Value = statistic.OpenVolumeInPercent;
            }
            cell7.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nRow.Cells.Add(cell7);

            return nRow;
        }

        private List<DataGridViewRow> GetRowsToGridTableOrders()
        {
            List<DataGridViewRow> rows = new List<DataGridViewRow>();

            rows.Add(GetFirstGridRowTableOrders());

            for (int i = 0; _bot.ArbitrationStep != null && i < _bot.ArbitrationStep.Count; i++)
            {
                DataGridViewRow nRow = new DataGridViewRow();
                ArbitrationStep step = _bot.ArbitrationStep[i];

                if (step == null)
                {
                    continue;
                }

                DataGridViewTextBoxCell cell0 = new DataGridViewTextBoxCell(); // 0 Шаг
                cell0.Value = step.NumberStep.ToString();
                cell0.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                nRow.Cells.Add(cell0);

                DataGridViewTextBoxCell cell1 = new DataGridViewTextBoxCell(); // 1 Время старта шага
                cell1.Value = step.StartTime.ToString();
                cell1.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                nRow.Cells.Add(cell1);

                DataGridViewTextBoxCell cell2 = new DataGridViewTextBoxCell(); // 2 Время окончания шага
                cell2.Value = step.EndTime.ToString();
                cell2.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                nRow.Cells.Add(cell2);

                DataGridViewTextBoxCell cell3 = new DataGridViewTextBoxCell(); // 3 Id ордера

                if (step.OrderStep == null ||
                    (step.OrderStep != null && string.IsNullOrEmpty(step.OrderStep.NumberMarket)))
                {
                    cell3.Value = "None";
                }
                else
                {
                    cell3.Value = step.OrderStep.NumberMarket.ToString();
                }

                cell3.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                nRow.Cells.Add(cell3);

                DataGridViewTextBoxCell cell4 = new DataGridViewTextBoxCell(); // 4 Статус

                if (step.OrderStep == null)
                {
                    cell4.Value = "Не выставлен";
                }
                else
                {
                    cell4.Value = step.OrderStep.State;
                }

                cell4.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                nRow.Cells.Add(cell4);

                DataGridViewTextBoxCell cell5 = new DataGridViewTextBoxCell(); // 5 Открытый объем
                cell5.Value = step.OpenVolume;
                cell5.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                nRow.Cells.Add(cell5);

                DataGridViewTextBoxCell cell6 = new DataGridViewTextBoxCell(); // 6 Нужный объем
                cell6.Value = Math.Round(step.VolumeStep, 1);
                cell6.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                nRow.Cells.Add(cell6);

                DataGridViewTextBoxCell cell7 = new DataGridViewTextBoxCell(); // // 7 Направление
                cell7.Value = step.OrderDirection;
                cell7.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                nRow.Cells.Add(cell7);

                DataGridViewButtonCell cell8 = new DataGridViewButtonCell(); // 8 Отмена ордера

                if ((step.OrderStep != null &&
                    (step.OrderStep.State == OrderStateType.Active || step.OrderStep.State == OrderStateType.Partial)))
                {
                    cell8.Value = "Отменить";
                }
                else
                {
                    cell8.Value = "Нельзя отменить";
                }

                cell8.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                nRow.Cells.Add(cell8);

                rows.Add(nRow);
            }

            return rows;
        }

        private void _grid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            _bot.SendNewLogMessage(e.Exception.ToString(), LogMessageType.Error);
        }

        private void _gridTableOrders_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_gridTableOrders == null ||
                    (_gridTableOrders != null && _gridTableOrders.Rows == null) ||
                    (_gridTableOrders != null && _gridTableOrders.Rows != null && _gridTableOrders.Rows.Count == 0))
            {
                return;
            }

            int column = e.ColumnIndex;
            int row = e.RowIndex;

            if (column == 8 && row > 0)
            {
                for (int i = 0; _bot.ArbitrationStep != null && i < _bot.ArbitrationStep.Count; i++)
                {
                    ArbitrationStep step = _bot.ArbitrationStep[i];

                    if (row == step.NumberStep)
                    {
                        if (step.Status == OrderStateType.Active || step.Status == OrderStateType.Partial)
                        {
                            AcceptDialogUi dialogUi = new AcceptDialogUi("Отменить ордер? После отмены, бот будет выключен");
                            dialogUi.ShowDialog();

                            if (!dialogUi.UserAcceptAction)
                            {
                                break;
                            }

                            step.NeedCancelOrder = true;
                        }

                        break;
                    }
                }
            }
        }

        #region Private fields

        private DataGridView _gridTableOrders;

        private TwapBot _bot;
        private DataGridView _gridActiveStatistics;
        private DataGridView _gridFinishStatistics;
        private DataGridView _gridCancelStatistics;

        #endregion
    }
}
