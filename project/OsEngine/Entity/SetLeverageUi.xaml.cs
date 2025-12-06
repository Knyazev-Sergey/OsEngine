using OsEngine.Language;
using OsEngine.Market;
using OsEngine.Market.Servers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace OsEngine.Entity
{  
    public partial class SetLeverageUi : Window
    {
        private IServer _server;
        private IServerRealization _serverRealization;
        private ConcurrentQueue<SecurityLeverageData> _queueLeverage = new();

        public SetLeverageUi(IServer server, IServerRealization serverRealization)
        {
            InitializeComponent();
            OsEngine.Layout.StickyBorders.Listen(this);
            OsEngine.Layout.StartupLocation.Start_MouseInCentre(this);

            _server = server;
            _server.SecuritiesChangeEvent += _server_SecuritiesChangeEvent;
            _serverRealization = serverRealization;

            //IServerPermission ServerPermission = ServerMaster.GetServerPermission(server.ServerType);
            TextBoxLeverage.Text = "1";

            UpdateClassComboBox(server.Securities);
            ComboBoxClass.SelectionChanged += ComboBoxClass_SelectionChanged;

            CreateTable();
            PaintLeverageTable(server.Securities);

            Title = OsLocalization.Entity.TitleSetLeverageUi + " " + _server.ServerType;

            this.Activate();
            this.Focus();

            this.Closed += SetLeverageUi_Closed;

            Thread worker = new Thread(ThreadSetLeverage);
            worker.Start();
        }

        private void ThreadSetLeverage(object obj)
        {
            while (true)
            {
                try
                {
                    if(_serverRealization.ServerStatus == ServerConnectStatus.Disconnect)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    if (_queueLeverage == null ||
                         _queueLeverage.Count == 0)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    SecurityLeverageData data = null;

                    if (!_queueLeverage.TryDequeue(out data))
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    SetLeverageOnExchange(data);

                    Thread.Sleep(200);
                }
                catch (Exception ex)
                {
                    ServerMaster.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                }
            }
        }

        private void SetLeverageOnExchange(SecurityLeverageData data)
        {
            _serverRealization.SetLeverage(data.Security, data.Leverage);
        }

        private DataGridView _dgv;

        private void CreateTable()
        {
            try
            {
                _dgv = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect, DataGridViewAutoSizeRowsMode.AllCells);

                _dgv.Dock = DockStyle.Fill;
                _dgv.ScrollBars = ScrollBars.Both;
                _dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
              
                _dgv.ColumnCount = 7;
                _dgv.RowCount = 0;

                _dgv.Columns[0].HeaderText = "#";
                _dgv.Columns[1].HeaderText = OsLocalization.Entity.SecuritiesColumn1; // Name
                _dgv.Columns[2].HeaderText = OsLocalization.Entity.SecuritiesColumn9; // Name Full
                _dgv.Columns[3].HeaderText = OsLocalization.Entity.SecuritiesColumn10; // Name ID
                _dgv.Columns[4].HeaderText = OsLocalization.Entity.SecuritiesColumn11; // Class
                _dgv.Columns[5].HeaderText = OsLocalization.Entity.SecuritiesColumn2; // Type
                _dgv.Columns[6].HeaderText = OsLocalization.ConvertToLocString("Eng:Leverage_Ru:Плечо");

                foreach (DataGridViewColumn column in _dgv.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                    column.ReadOnly = true;
                }

                _dgv.Columns[6].ReadOnly = false;

                HostLeverage.Child = _dgv;
                HostLeverage.Child.Show();
                HostLeverage.Child.Refresh();

                _dgv.CellValueChanged += _dgv_CellValueChanged;
                _dgv.DataError += _dgv_DataError;
            }
            catch
            {                
            }
        }

        private void _dgv_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            ServerMaster.SendNewLogMessage(e.ToString(), Logging.LogMessageType.Error);
        }

        private void _dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == 6 && e.RowIndex >= 0)
                {
                    decimal defaultLeverage = 1;
                    decimal.TryParse(TextBoxLeverage.Text, out defaultLeverage);

                    decimal leverage = defaultLeverage;
                    if (!decimal.TryParse(_dgv.Rows[e.RowIndex].Cells[6].Value.ToString(), out leverage))
                    {
                        _dgv.Rows[e.RowIndex].Cells[6].Value = defaultLeverage;
                    }

                    for (int i = 0; i < _server.Securities.Count; i++)
                    {
                        Security sec = _server.Securities[i];

                        if (sec.Name == _dgv.Rows[e.RowIndex].Cells[1].Value.ToString() &&
                            sec.NameClass == _dgv.Rows[e.RowIndex].Cells[4].Value.ToString())
                        {
                            SecurityLeverageData data = new();
                            data.Security = sec;
                            data.Leverage = leverage;

                            _queueLeverage.Enqueue(data);
                        }                       
                    }

                    SaveLeverageToFile();
                }                
            }
            catch (Exception ex)
            {
                ServerMaster.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void SaveLeverageToFile()
        {
            try
            {
                if (Directory.Exists(@"Engine\ServerDopSettings\") == false)
                {
                    Directory.CreateDirectory(@"Engine\ServerDopSettings\");
                }

                string fileName = _server.ServerType + "SecuritiesLeverage";

                string filePath = @"Engine\ServerDopSettings\" + fileName + ".txt";

                using (StreamWriter writer = new StreamWriter(filePath, false))
                {
                    for (int i = 0; i < _dgv.Rows.Count; i++)
                    {
                        string str = "";

                        str += _dgv.Rows[i].Cells[1].Value;
                        str += "|" + _dgv.Rows[i].Cells[4].Value;
                        str += "|" + _dgv.Rows[i].Cells[6].Value;

                        writer.WriteLine(str);
                    }

                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                ServerMaster.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void PaintLeverageTable(List<Security> securities)
        {
            try
            {
                if (securities == null)
                {
                    return;
                }

                if (_dgv == null)
                {
                    return;
                }

                if (_dgv.InvokeRequired)
                {
                    _dgv.Invoke(new Action<List<Security>>(PaintLeverageTable), securities);
                    return;
                }

                if (ComboBoxClass.SelectedItem == null)
                {
                    return;
                }

                int num = 1;

                string selectedClass = ComboBoxClass.SelectedItem.ToString();

                List<DataGridViewRow> rows = new List<DataGridViewRow>();

                for (int i = 0; i < securities.Count; i++)
                {
                    Security curSec = securities[i];

                    if (curSec.SecurityType != SecurityType.Futures)
                    {
                        continue;
                    }

                    if (selectedClass != "All"
                        && curSec.NameClass != selectedClass)
                    {
                        continue;
                    }
                                        
                    DataGridViewRow nRow = new DataGridViewRow();

                    nRow.Cells.Add(new DataGridViewTextBoxCell());
                    nRow.Cells[0].Value = num; num++;

                    nRow.Cells.Add(new DataGridViewTextBoxCell());
                    nRow.Cells[1].Value = curSec.Name;

                    nRow.Cells.Add(new DataGridViewTextBoxCell());
                    nRow.Cells[2].Value = curSec.NameFull;

                    nRow.Cells.Add(new DataGridViewTextBoxCell());
                    nRow.Cells[3].Value = curSec.NameId;

                    nRow.Cells.Add(new DataGridViewTextBoxCell());
                    nRow.Cells[4].Value = curSec.NameClass;

                    nRow.Cells.Add(new DataGridViewTextBoxCell());
                    nRow.Cells[5].Value = curSec.SecurityType;

                    nRow.Cells.Add(new DataGridViewTextBoxCell());
                    nRow.Cells[6].Value = GetLeverage(curSec);

                    rows.Add(nRow);
                }

                HostLeverage.Child = null;

                _dgv.Rows.Clear();

                if (rows.Count > 0)
                {
                    _dgv.Rows.AddRange(rows.ToArray());
                }

                HostLeverage.Child = _dgv;                
            }
            catch (Exception ex)
            {
                ServerMaster.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private string GetLeverage(Security security)
        {
            string leverage = GetLeverageFromFile(security);

            if (string.IsNullOrEmpty(leverage))
            {
                leverage = GetLeverageFromExchange(security);
            }

            return leverage;
        }

        private List<LeverageData> _listLeverage;

        private string GetLeverageFromFile(Security security)
        {
            try
            {
                if (_listLeverage == null)
                {
                    _listLeverage = new List<LeverageData>();

                    if (_server == null)
                    {
                        return null;
                    }

                    string fileName = _server.ServerType + "SecuritiesLeverage";

                    string filePath = @"Engine\ServerDopSettings\" + fileName + ".txt";

                    if (!File.Exists(filePath))
                    {
                        return null;
                    }

                    /*decimal defaultLeverage = 1;
                    decimal.TryParse(TextBoxLeverage.Text, out defaultLeverage);*/

                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                string[] split = line.Split('|');

                                /*decimal leverage = defaultLeverage;
                                decimal.TryParse(split[2], out leverage);*/

                                LeverageData list = new();

                                list.Name = split[0];
                                list.Class = split[1];
                                list.Leverage = split[2];

                                _listLeverage.Add(list);
                            }                            
                        }

                        reader.Close();
                    }
                }

                for (int i = 0; i < _listLeverage.Count; i++)
                {
                    if (security.Name == _listLeverage[i].Name &&
                        security.NameClass == _listLeverage[i].Class)
                    {
                        return _listLeverage[i].Leverage;
                    }
                }

                return null;

            }
            catch (Exception ex)
            {
                ServerMaster.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                return null;
            }
        }

        private string GetLeverageFromExchange(Security security)
        {
            return TextBoxLeverage.Text;
        }

        private void SetLeverageUi_Closed(object sender, EventArgs e)
        {
            _queueLeverage.Clear();
            _dgv.Rows.Clear();
            _dgv.CellValueChanged -= _dgv_CellValueChanged;
            _dgv.DataError -= _dgv_DataError;
            _dgv = null;
            HostLeverage.Child = null;
            _server.SecuritiesChangeEvent -= _server_SecuritiesChangeEvent;
            _server = null;
            _serverRealization = null;

        }

        private void _server_SecuritiesChangeEvent(List<Security> securities)
        {
            try
            {
                UpdateClassComboBox(securities);
                PaintLeverageTable(securities);
            }
            catch (Exception ex)
            {
                ServerMaster.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void ComboBoxClass_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            PaintLeverageTable(_server.Securities);
        }

        private void UpdateClassComboBox(List<Security> securities)
        {
            try
            {
                if (ComboBoxClass.Dispatcher.CheckAccess() == false)
                {
                    ComboBoxClass.Dispatcher.Invoke(new Action<List<Security>>(UpdateClassComboBox), securities);
                    return;
                }

                string startClass = null;

                if (ComboBoxClass.SelectedItem != null)
                {
                    startClass = ComboBoxClass.SelectedItem.ToString();
                }

                List<string> classes = new List<string>();

                classes.Add("All");

                for (int i = 0; securities != null && i < securities.Count; i++)
                {
                    string curClass = securities[i].NameClass;

                    if (string.IsNullOrEmpty(curClass))
                    {
                        continue;
                    }

                    bool isInArray = false;

                    for (int i2 = 0; i2 < classes.Count; i2++)
                    {
                        if (classes[i2] == curClass)
                        {
                            isInArray = true;
                            break;
                        }
                    }

                    if (isInArray == false)
                    {
                        classes.Add(curClass);
                    }
                }

                ComboBoxClass.Items.Clear();

                for (int i = 0; i < classes.Count; i++)
                {
                    ComboBoxClass.Items.Add(classes[i]);
                }

                if (ComboBoxClass.SelectedItem == null)
                {
                    ComboBoxClass.SelectedItem = classes[0];
                }

                if (startClass != null)
                {
                    ComboBoxClass.SelectedItem = startClass;
                }

                if (ComboBoxClass.SelectedItem.ToString() == "All"
                    && securities.Count > 10000
                    && classes.Count > 1)
                {
                    ComboBoxClass.SelectedItem = classes[1];
                }
            }
            catch (Exception ex)
            {
                ServerMaster.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private class LeverageData
        {
            public string Name;
            public string Class;
            public string Leverage;
        }

        private class SecurityLeverageData
        {            
            public decimal Leverage;
            public Security Security;
        }
    }
}
