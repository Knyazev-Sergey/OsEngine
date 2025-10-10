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

            //Thread threadTradeLogic = new Thread(ThreadTradeLogic) { IsBackground = true };
            //threadTradeLogic.Start();
        }

        private void ThreadTradeLogic()
        {
            while (true)
            {
                try
                {
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
            for (int i = 0; i < _tab.Tabs.Count; i++)
            {
                
            }
        }

        #endregion
    }
}
