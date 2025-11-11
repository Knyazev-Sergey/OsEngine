using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Indicators.Samples;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.Robots
{
    [Bot("BotMobility")]
    public class BotMobility : BotPanel
    {
        private BotTabSimple _tab;
        private Aindicator _mobility;

        public BotMobility(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _mobility = IndicatorsFactory.CreateIndicatorByName("MobilityIndicator", name + "Mobility", false);
            _mobility = (Aindicator)_tab.CreateCandleIndicator(_mobility, "MobilityArea");
            //_mobility.ParametersDigit[0].Value = _indLength.ValueInt;
            //_mobility.ParametersDigit[1].Value = _bollingerDeviation.ValueDecimal;
            _mobility.Save();

            MobilityIndicator mInd = (MobilityIndicator)_mobility;
            mInd.SendTab(_tab);
        }
    }
}
