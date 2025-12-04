using OsEngine.Language;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.Market.Servers.MoexAlgopack.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
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

            PaintLeverageTable();

            _server = server;
            _server.SecuritiesChangeEvent += _server_SecuritiesChangeEvent;

            Title = OsLocalization.Entity.TitleSetLeverageUi + " " + _server.ServerType;

            this.Activate();
            this.Focus();

            this.Closed += SetLeverageUi_Closed;
        }

        private void PaintLeverageTable()
        {
            try
            {

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
