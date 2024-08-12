using System;
using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using System.Threading;
using OsEngine.Market.Connectors;
using System.Windows.Forms;
using OsEngine.Candles.Series;
using OsEngine.Candles;

namespace OsEngine.Robots.HomeWork
{
    [Bot("Test1")]
    public class Test1 : BotPanel
    {
        private BotTabScreener _screener;
        private BotTabSimple _tabSimple;


        public Test1(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tabSimple = TabsSimple[0];
            TabCreate(BotTabType.Screener);
            _screener = TabsScreener[0];

            
            

            StartThread();
        }

        private ActivatedSecurity GetSecurity(Security security, string str)
        {
            ActivatedSecurity sec = new ActivatedSecurity();
            sec.SecurityClass = security.NameClass.ToString();
            sec.SecurityName = security.Name.ToString();

            if (security.Name.Contains(str))
            {
                sec.IsOn = true;
            }

            return sec;
        }

        private void StartThread()
        {
            Thread worker = new Thread(StartPaintChart) { IsBackground = true };
            worker.Start();
        }

        private void StartPaintChart()
        {
            string date = "14AUG24";

            while (true)
            {
                if (!_tabSimple.Connector.IsReadyToTrade)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                _screener.Clear();
                
                _screener.PortfolioName = _tabSimple.Connector.PortfolioName;
                _screener.ServerType = Market.ServerType.Deribit;
                _screener.SecuritiesClass = SecurityType.Option.ToString();
                //ACandlesSeriesRealization asd =  _screener.CandleSeriesRealization;
                //_screener.CandleSeriesRealization = CandleFactory.CreateCandleSeriesRealization("Simple");
                //_screener.TimeFrame = TimeFrame.Min30;


                if (date == "ETH-14AUG24")
                {
                    date = "ETH-13AUG24";
                }
                else
                {
                    date = "ETH-14AUG24";
                }

                List<ActivatedSecurity> securities = new List<ActivatedSecurity>();

                for (int i = 0; i < _tabSimple.Connector.MyServer.Securities.Count; i++)
                {
                    ActivatedSecurity sec = GetSecurity(_tabSimple.Connector.MyServer.Securities[i], date);

                    if (sec == null)
                    {
                        continue;
                    }

                    securities.Add(sec);
                }

                _screener.SecuritiesNames = securities;
                _screener.SaveSettings();

                _screener.NeadToReloadTabs = true;

                Thread.Sleep(25000);
            }
        }



        public override string GetNameStrategyType()
        {
            return "Test1";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

    }
}

