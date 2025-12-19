using System;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using System.Threading;
using System.Collections.Generic;

namespace OsEngine.Robots
{
    [Bot("Test")]
    public class Test : BotPanel
    {
        private BotTabSimple _tab;

        public Test(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            Thread worker = new Thread(ThreadRefreshTableSpot) { IsBackground = true };
            worker.Start();
        }

        private void ThreadRefreshTableSpot(object obj)
        {
            while (true)
            {
                try
                {
                    if (_tab == null || _tab.Connector.MyServer == null)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    if (_tab.Connector.MyServer.ListLeverageData == null || _tab.Connector.MyServer.ListLeverageData.Count == 0)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    Security security = _tab.Security;

                    decimal list = _tab.Connector.MyServer.GetLeverage(security);

                    if (list != 3)
                    {
                        SetLeverage(_tab.Security, 3);
                    }

                    Thread.Sleep(1000);                    
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.StackTrace, Logging.LogMessageType.Error);
                }
            }
        }

        private void SetLeverage(Security security, int leverage)
        {
            _tab.Connector.MyServer.SetLeverage(security, leverage);
        }


    }
}
