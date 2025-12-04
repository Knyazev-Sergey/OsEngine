using OsEngine.Language;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.Market.Servers.MoexAlgopack.Entity;
using OsEngine.Market.Servers.TraderNet.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace OsEngine.Entity
{
  
    public partial class SetLeverageUi : Window
    {
        private IServer _server;

        public SetLeverageUi(IServer server)
        {
            InitializeComponent();
            OsEngine.Layout.StickyBorders.Listen(this);
            OsEngine.Layout.StartupLocation.Start_MouseInCentre(this);

            CreateTable();
            PaintLeverageTable(server.Securities);

            _server = server;
            _server.SecuritiesChangeEvent += _server_SecuritiesChangeEvent;

            Title = OsLocalization.Entity.TitleSetLeverageUi + " " + _server.ServerType;

            this.Activate();
            this.Focus();

            this.Closed += SetLeverageUi_Closed;
        }

        private DataGridView _dgv;

        private void CreateTable()
        {
            try
            {
                _dgv = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect, DataGridViewAutoSizeRowsMode.AllCells);

                _dgv.Dock = DockStyle.Fill;
                _dgv.ScrollBars = ScrollBars.Vertical;
                _dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
                _dgv.GridColor = System.Drawing.Color.Gray;
                _dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
                _dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                _dgv.ColumnCount = 7;
                _dgv.RowCount = 0;

                _dgv.Columns[0].HeaderText = OsLocalization.ConvertToLocString("Eng:Amount margin_" + "Ru:Сумма непокрытой позиции_");
                _dgv.Columns[1].HeaderText = OsLocalization.ConvertToLocString("Eng:Type rate_" + "Ru:Вид ставки_");
                _dgv.Columns[2].HeaderText = OsLocalization.ConvertToLocString("Eng:Rate_" + "Ru:Ставка_");

                _dgv.Columns[0].ReadOnly = true;
                _dgv.Columns[1].ReadOnly = true;

                _dgv.Columns[0].Width = 250;
                _dgv.Columns[1].Width = 100;
                _dgv.Columns[2].Width = 100;

                foreach (DataGridViewColumn column in _dgv.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

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
            
        }

        private void _dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            
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

                /*if (_gridSecurities.InvokeRequired)
                {
                    _gridSecurities.Invoke(new Action<List<Security>>(PaintSecurities), securities);
                    return;
                }*/

                if (ComboBoxClass.SelectedItem == null)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                ServerMaster.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void SetLeverageUi_Closed(object sender, EventArgs e)
        {
            _server.SecuritiesChangeEvent -= _server_SecuritiesChangeEvent;
            _server = null;
        }

        private void _server_SecuritiesChangeEvent(List<Security> list)
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                ServerMaster.SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }
    }
}
