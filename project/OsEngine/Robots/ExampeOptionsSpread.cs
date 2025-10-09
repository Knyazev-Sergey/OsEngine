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

        private BotTabOptions _tab1;
        private StrategyParameterDecimal _setRsiBuy;
        private StrategyParameterDecimal _setRsiSell;

        public ExampleOptionsSpread(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Options);
            
            

            this.ParamGuiSettings.Title = "Example Options Spread";
            this.ParamGuiSettings.Height = 400;
            this.ParamGuiSettings.Width = 400;

            string tabName = " Параметры ";

            _setRsiBuy = CreateParameter("Значение RSI для покупки вертикального колл-спреда", 0m, 0m, 0m, 0m, tabName);
            _setRsiSell = CreateParameter("Значение RSI для покупки вертикального пут-спреда", 0m, 0m, 0m, 0m, tabName);      
        }

        #endregion
    }
}
