using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using System.Collections.Generic;

namespace OsEngine.Robots.DeribitPI
{
    [Bot("DeribitPI")]
    public class DeribitPI : BotPanel
    {
        private BotTabSimple _tab;
        public string Regime;
        public string TextInfo;
        private DeribitPIUiModel _viewModel;

        public DeribitPI(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _tab.CandleUpdateEvent += _tab_CandleUpdateEvent;

            _viewModel = new DeribitPIUiModel();

        }

        private void _tab_CandleUpdateEvent(List<Candle> obj)
        {         
            if (_viewModel == null)
            {
                return;
            }
            
            _viewModel.LastPrice = "Новые данные";
        }

        public override string GetNameStrategyType()
        {
            return "DeribitPI";
        }

        public override void ShowIndividualSettingsDialog()
        {
            DeribitPIUi ui = new DeribitPIUi(this);
            ui.ShowDialog();
        }
    }
}
