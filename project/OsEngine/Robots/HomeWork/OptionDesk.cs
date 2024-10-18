using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.Robots.HomeWork
{
    [Bot("OptionDesk")]
    public class OptionDesk : BotPanel
    {
        private BotTabOption _tab;
        public OptionDesk(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Option);
           
        }

        public override string GetNameStrategyType()
        {
            return "OptionDesk";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }
    }
}