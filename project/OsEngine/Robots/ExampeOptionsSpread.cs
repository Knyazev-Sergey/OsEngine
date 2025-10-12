using OsEngine.Entity;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.Integration;
using System.Windows.Forms;

namespace OsEngine.Robots
{
    [Bot("ExampleOptionsSpread")]
    public class ExampleOptionsSpread : BotPanel
    {
        #region Constructor

        private BotTabOptions _tab;
        private StrategyParameterDecimal _setRsiBuy;
        private StrategyParameterDecimal _setRsiSell;

        public ExampleOptionsSpread(string name, StartProgram startProgram) : base(name, startProgram)
        {
            _tab = (BotTabOptions)TabCreate(BotTabType.Options);

            this.ParamGuiSettings.Title = "Example Options Spread";
            this.ParamGuiSettings.Height = 400;
            this.ParamGuiSettings.Width = 400;

            string tabName = " Параметры ";

            _setRsiBuy = CreateParameter("Значение RSI для покупки вертикального колл-спреда", 0m, 0m, 0m, 0m, tabName);
            _setRsiSell = CreateParameter("Значение RSI для покупки вертикального пут-спреда", 0m, 0m, 0m, 0m, tabName);

            /*CustomTabToParametersUi customTabMonitoring = ParamGuiSettings.CreateCustomTab(" Мониторинг и управление ");
            CreateTable();
            customTabMonitoring.AddChildren(_host);*/

            Thread threadTradeLogic = new Thread(ThreadTradeLogic) { IsBackground = true };
            threadTradeLogic.Start();
        }

        private WindowsFormsHost _host;
        private DataGridView _dgv;

        private void CreateTable()
        {
            
        }

        private void ThreadTradeLogic()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(100);
                    Start();
                }
                catch(Exception ex)
                {
                    SendNewLogMessage(ex.Message, Logging.LogMessageType.Error);
                    Thread.Sleep(1000);
                }
            }
        }

        private void Start()
        {

            

            string asset = _tab.UnderlyingAssets[0];

            //double centralStrike = _tab.GetCentralStrikeOfUnderlyingAsset(asset);

            //var longStrike = GetStrikeFromCentralStrike(1);

            //var tabLong = GetOptionTab(asset, OptionType.Call, centralStrike);

        }

        public BotTabSimple GetOptionTab(string underlyingAssetTicker, OptionType optionType, double strike)
        {
            var optionData = _tab.Tabs.FirstOrDefault(o =>
                o.Security.UnderlyingAsset == underlyingAssetTicker &&
                o.Security.OptionType == optionType &&
                (double)o.Security.Strike == strike );

            return optionData;
        }

        #endregion
    }
}
