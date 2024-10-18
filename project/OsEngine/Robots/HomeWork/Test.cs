using System;
using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using System.Threading;

namespace OsEngine.Robots.HomeWork
{
    [Bot("Test")]
    public class Test : BotPanel
    {
        private BotTabSimple _tab;
        
        public Test(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _tab.Connector.OptionGreeksEvent += Connector_NewOptionGreeksEvent;
        }

        private void Connector_NewOptionGreeksEvent(OptionGreeks obj)
        {
            SendNewLogMessage($"{obj.SecurityName}: Mark IV = {obj.MarkIV}, Delta = {obj.Delta}, " +
                $"Gamma = {obj.Gamma}, Vega = {obj.Vega}, Theta = {obj.Theta}", Logging.LogMessageType.User);
        }

        public override string GetNameStrategyType()
        {
            return "Test";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

    }
}

