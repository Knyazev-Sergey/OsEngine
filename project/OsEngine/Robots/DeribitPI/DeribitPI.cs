using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using System.Collections.Generic;
using OsEngine.Market.Servers.Deribit;
using System.Threading;

namespace OsEngine.Robots.DeribitPI
{
    [Bot("DeribitPI")]
    public class DeribitPI : BotPanel
    {
        private BotTabSimple _tab1;
        private BotTabSimple _tab2;
        public string Regime = "Off";
        public string TextInfo;
        public string ViewModel;
        public List<string> ListOption = new List<string>();
        private DeribitServer _connector;
        private List<Security> _securities;

        public DeribitPI(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab1 = TabsSimple[0];
            TabCreate(BotTabType.Simple);
            _tab2 = TabsSimple[1];

            _connector = new DeribitServer();

            _tab1.CandleUpdateEvent += _tab_CandleUpdateEvent;
            _tab2.CandleUpdateEvent += _tab2_CandleUpdateEvent;
                
            _securities = new List<Security>();

            _connector.SecuritiesChangeEvent += _connector_SecuritiesChangeEvent;

            _tab1.Connector
           
            StartThread();

            
        }

        private void StartThread()
        {
            Thread worker = new Thread(StartRobot) { IsBackground = true };
            worker.Start();
        }

        private void StartRobot()
        {            
            while (true)
            {
                Thread.Sleep(500);
                
                if (_connector.Securities != null && _connector.Securities.Count != 0)
                {
                    SendNewLogMessage($"Derbit: {_connector.GetSecurityForName("ETH", "Option")}", Logging.LogMessageType.Error);
                }
            }
        }

        private void _connector_SecuritiesChangeEvent(List<Security> obj)
        {
            
            SendNewLogMessage($"Count Deribit: {obj.Count}", Logging.LogMessageType.Error);
        }

        private void _tab2_CandleUpdateEvent(List<Candle> candle)
        {
           
        }

        private void _tab_CandleUpdateEvent(List<Candle> candle)
        {         
            if (candle == null)
            {
                return;
            }
            ViewModel = candle[candle.Count - 1].Close.ToString();
            
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
