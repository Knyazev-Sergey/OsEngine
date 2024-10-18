using OsEngine.Charts.CandleChart;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Language;
using OsEngine.Logging;
using OsEngine.Market;
using OsEngine.Market.Connectors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Shapes;

namespace OsEngine.OsTrader.Panels.Tab
{
    public class BotTabOption : IIBotTab
    {
        public BotTabOption(string name, StartProgram startProgram)
        {
            TabName = name;
            _startProgram = startProgram;
            /*
                        _valuesToFormula = new List<ValueSave>();
                        _chartMaster = new ChartCandleMaster(TabName, _startProgram);

                        Load();

                        AutoFormulaBuilder = new IndexFormulaBuilder(this, TabName, _startProgram);
                        AutoFormulaBuilder.LogMessageEvent += SendNewLogMessage;*/
        }

        /// <summary>
        /// source type
        /// </summary>
        public BotTabType TabType
        {
            get
            {
                return BotTabType.Option;
            }
        }

        /// <summary>
        /// program that created the source
        /// </summary>
        private StartProgram _startProgram;

        /// <summary>
        /// unique robot name
        /// </summary>
        public string TabName { get; set; }

        /// <summary>
        /// tab number
        /// </summary>
        public int TabNum { get; set; }

        /// <summary>
        /// custom name robot
        /// </summary>
        public string NameStrategy
        {
            get
            {
                if (TabName.Contains("tab"))
                {
                    return TabName.Remove(TabName.LastIndexOf("tab"), TabName.Length - TabName.LastIndexOf("tab"));
                }
                return "";
            }
        }

        /// <summary>
        /// is the emulator enabled
        /// </summary>
        public bool EmulatorIsOn { get; set; }

        /// <summary>
        /// clear memory to initial state
        /// </summary>
        public void Clear()
        {
            /* _valuesToFormula = new List<ValueSave>();
             _chartMaster.Clear();*/
        }

        /// <summary>
        /// remove source and all child structures
        /// </summary>
        public void Delete()
        {
            /*_chartMaster.Delete();
            _chartMaster = null;

            if (File.Exists(@"Engine\" + TabName + @"SpreadSet.txt"))
            {
                File.Delete(@"Engine\" + TabName + @"SpreadSet.txt");
            }

            for (int i = 0; Tabs != null && i < Tabs.Count; i++)
            {
                Tabs[i].Delete();
            }

            AutoFormulaBuilder.Delete();
            AutoFormulaBuilder.LogMessageEvent -= SendNewLogMessage;
            AutoFormulaBuilder = null;

            if (TabDeletedEvent != null)
            {
                TabDeletedEvent();
            }*/
        }

        /// <summary>
        /// whether the submission of events to the top is enabled or not
        /// </summary>
        public bool EventsIsOn
        {
            get
            {
                return _eventsIsOn;
            }
            set
            {
                if (_eventsIsOn == value)
                {
                    return;
                }
                _eventsIsOn = value;
                //Save();
            }
        }
        private bool _eventsIsOn = true;

        /// <summary>
        /// whether the tab is connected to download data
        /// </summary>
        public DateTime LastTimeCandleUpdate { get; set; }

        public event Action TabDeletedEvent;

        #region Drawing the source table

        /// <summary>
        /// Thread drawing interface
        /// </summary>
        Thread painterThread;

        /// <summary>
        /// A method in which interface drawing methods are periodically called
        /// </summary>
        private void PainterThread()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(1000);

                    if (_isDeleted)
                    {
                        return;
                    }

                    TryRePaintGrid();
                }
                catch (Exception error)
                {
                    SendNewLogMessage(error.ToString(), LogMessageType.Error);
                }
            }
        }

        /// <summary>
        /// Flag indicating whether the source has been removed or not. false - not deleted. true - source deleted
        /// </summary>
        private bool _isDeleted;

        /// <summary>
        /// The area where the table of options is drawn
        /// </summary>
        private WindowsFormsHost _host;

        /// <summary>
        /// Option desk for the visual interface
        /// </summary>
        private DataGridView _grid;

        /// <summary>
        /// Start drawing the table of options
        /// </summary>
        public void StartPaint(WindowsFormsHost host)
        {
            try
            {
                _host = host;

                if (_grid == null)
                {
                    CreateGrid();
                }

                RePaintGrid();

                _host.Child = _grid;

                if (painterThread == null)
                {
                    painterThread = new Thread(PainterThread);
                    painterThread.Start();
                }
            }
            catch (Exception error)
            {
                SendNewLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

        /// <summary>
        /// Stop drawing the table of pairs
        /// </summary>
        public void StopPaint()
        {
            if (_host != null)
            {
                _host.Child = null;
            }
        }

        /// <summary>
        /// Method for creating a table for drawing pairs
        /// </summary>
        private void CreateGrid()
        {
            DataGridView newGrid = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.CellSelect, DataGridViewAutoSizeRowsMode.AllCells);

            newGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            newGrid.ScrollBars = ScrollBars.Vertical;
            DataGridViewCellStyle style = newGrid.DefaultCellStyle;
            DataGridViewTextBoxCell cell0 = new DataGridViewTextBoxCell();
            cell0.Style = style;

            DataGridViewColumn colum0 = new DataGridViewColumn();
            colum0.CellTemplate = cell0;
            colum0.HeaderText = "";// pairNum
            colum0.ReadOnly = true;
            colum0.Width = 70;
            newGrid.Columns.Add(colum0);

            for (int i = 0; i < 5; i++)
            {
                DataGridViewColumn columN = new DataGridViewColumn();
                columN.CellTemplate = cell0;
                columN.HeaderText = "";
                columN.ReadOnly = false;
                columN.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                newGrid.Columns.Add(columN);
            }

            _grid = newGrid;
            //_grid.CellClick += _grid_CellClick;
        }

        /// <summary>
        /// The method of full redrawing of the table with pairs
        /// </summary>
        private void RePaintGrid()
        {
            try
            {
                if (_grid.InvokeRequired)
                {
                    _grid.Invoke(new Action(RePaintGrid));
                    return;
                }

                int showRow = _grid.FirstDisplayedScrollingRowIndex;

                _grid.Rows.Clear();

                List<DataGridViewRow> rows = GetRowsToGrid();

                if (rows == null)
                {
                    return;
                }

                for (int i = 0; i < rows.Count; i++)
                {
                    _grid.Rows.Add(rows[i]);
                }

                if (showRow > 0 &&
                    showRow < _grid.Rows.Count)
                {
                    _grid.FirstDisplayedScrollingRowIndex = showRow;
                }
            }
            catch (Exception error)
            {
                SendNewLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

        #endregion

        #region Logging

        /// <summary>
        /// Send new log message
        /// </summary>
        private void SendNewLogMessage(string message, LogMessageType type)
        {
            if (LogMessageEvent != null)
            {
                LogMessageEvent(message, type);
            }
            else if (type == LogMessageType.Error)
            {
                System.Windows.MessageBox.Show(message);
            }
        }

        /// <summary>
        /// New log message event
        /// </summary>
        public event Action<string, LogMessageType> LogMessageEvent;

        #endregion
    }
}
