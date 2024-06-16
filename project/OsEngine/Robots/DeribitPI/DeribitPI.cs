using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using System.Collections.Generic;
using System.Threading;
using System;
using OsEngine.Logging;
using RestSharp;
using Newtonsoft.Json;

namespace OsEngine.Robots.DeribitPI
{
    [Bot("DeribitPI")]
    public class DeribitPI : BotPanel
    {
        private BotTabSimple _tab1;
        private BotTabScreener _tab2;
        public DeribitPIUi.NameRegime Regime = DeribitPIUi.NameRegime.Off;
        private List<string> _optionSecurities = new List<string>();
        public decimal LastPrice;
        public string CurrentStrike;
        public decimal PriceOption;
        public decimal Deposit;
        public decimal PercentOfDeposit;
        public decimal SizeOption;
        public bool CheckTestServer;
        public decimal SizeBuyOption;
        public int CountIteration;
        public int TimeToCloseOption;
        public int TimeFuturesLimit;
        public bool CheckBoxMarketOrder;
        public int TimeOptionLimit;
        public int PauseBuyOption;
        public int CountWorkParts;
        public int RatioWorkParts;
        public int OneIncreaseX;
        public int OneIncreaseY;
        public int TwoIncreaseX;
        public int TwoIncreaseY;
        public int ThreeIncreaseX;
        public int ThreeIncreaseY;
        private int _currentTab;

        

        public DeribitPI(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab1 = TabsSimple[0];
            TabCreate(BotTabType.Screener);
            _tab2 = TabsScreener[0];

            _tab1.CandleUpdateEvent += _tab_CandleUpdateEvent;
                       

            _tab2.CandleUpdateEvent += _tab2_CandleUpdateEvent;
            _tab2.BestBidAskChangeEvent += _tab2_BestBidAskChangeEvent;
           
            StartThread();            
            
        }
               
        private void _tab2_BestBidAskChangeEvent(decimal bid, decimal ask, BotTabSimple tab)
        {
           
        }

        private void _tab2_CandleUpdateEvent(List<Candle> candle, BotTabSimple tab)
        {
            /*if (CurrentStrike == null)
            {
                return;
            }
            if (candle == null || candle.Count == 0)
            {
                return;
            }
            if (tab.Securiti.Name == CurrentStrike)
            {
                PriceOption = candle[candle.Count-1].Close;
            }*/
        }

        private void StartThread()
        {
            Thread worker = new Thread(StartRobot) { IsBackground = true };
            worker.Start();
        }

        private void StartRobot()
        {
            bool flagStartTradeOption = false;

            while (true)
            {
                Thread.Sleep(1000);

                // получаем список выбранных опционов

                if (_optionSecurities == null || _optionSecurities.Count == 0) 
                {
                    if (_tab2.SecuritiesNames != null || _tab2.SecuritiesNames.Count != 0)
                    {
                        for (int i = 0; i < _tab2.SecuritiesNames.Count; i++)
                        {
                            if (_tab2.SecuritiesNames[i].IsOn == true)
                            {
                                if (_tab2.SecuritiesNames[i].SecurityName.ToString().Split('-')[3] == "C")
                                {
                                    _optionSecurities.Add(_tab2.SecuritiesNames[i].SecurityName.ToString());
                                }
                            }
                        }                       
                    }
                }

                // получаем данные по теор цене
                if (_optionSecurities != null && _optionSecurities.Count != 0)
                {
                    if (CurrentStrike != null)
                    {
                        GetMarkPrice(); 
                    }
                }

                // получаем данные по депозиту
                if (_tab1.Portfolio != null)
                {
                    List<PositionOnBoard> positionOnBoard = _tab1.Portfolio.GetPositionOnBoard();
                    for (int i = 0; i < positionOnBoard.Count; i++)
                    {
                        if (positionOnBoard[i].SecurityNameCode == "ETH")
                        {
                            Deposit = positionOnBoard[0].ValueCurrent;
                        }
                    }                    
                }

                // расчитываем кол-во опционов для покупки
                if (PercentOfDeposit != 0 && Deposit != 0 && PriceOption != 0)
                {
                    SizeOption = Math.Floor(Deposit * (PercentOfDeposit / 100) / PriceOption);
                }
                else
                {
                    SizeOption = 0;
                }

                if (Regime != DeribitPIUi.NameRegime.Off)
                {
                    
                }

                
                if(Regime == DeribitPIUi.NameRegime.AssemblyConstruction)
                {
                    // выставляем начальный ордер для котирования опциона
                    if (SizeOption != 0 && 
                        SizeOption - SizeBuyOption > 0 && 
                        flagStartTradeOption == false)
                    {
                        for (int i = 0; i < _tab2.Tabs.Count; i++)
                        {
                            if (_tab2.Tabs[i].Securiti.Name == CurrentStrike)
                            {
                                _currentTab = i;
                                
                                //_tab2.Tabs[i].BuyAtLimit(1, GetOptionPriceLimit());
                                break;
                            }                            
                        }                        
                    }

                    if (flagStartTradeOption)
                    {
                        if (_tab2.Tabs[_currentTab].Securiti.Name != CurrentStrike)
                        {

                            flagStartTradeOption = false;
                        }
                    }
                }                   
            }
        }

        private decimal GetOptionPriceLimit()
        {

            return 0;
        }

        private void GetMarkPrice()
        {
            try
            {  
                string typeServer = "https://www.deribit.com";
                                
                if (CheckTestServer)
                {
                    typeServer = "https://test.deribit.com";
                }

                string url = $"{typeServer}/api/v2/public/get_book_summary_by_instrument?instrument_name={CurrentStrike}";
                RestClient client = new RestClient(url);
                RestRequest request = new RestRequest(Method.GET);
                IRestResponse responseMessage = client.Execute(request);
                string JsonResponse = responseMessage.Content;

                if (responseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    ResponseMessageMarkPrice response = JsonConvert.DeserializeObject<ResponseMessageMarkPrice>(JsonResponse);
                    PriceOption = response.result[0].mark_price;
                }
                else
                {
                    SendNewLogMessage($"Http State Code: {responseMessage.StatusCode}, {JsonResponse}", LogMessageType.Error);
                }
            }
            catch (Exception exception)
            {
                SendNewLogMessage(exception.ToString(), LogMessageType.Error);
            }
        }

        public class ResponseMessageMarkPrice
        {
            public List<Result> result { get; set; }

            public class Result
            {
                public decimal mark_price { get; set; }                
            }
        }

        private void _tab_CandleUpdateEvent(List<Candle> candle)
        {         
            if (candle == null)
            {
                return;
            }
            // обновляем последнюю цену и текущий страйк
            LastPrice = candle[candle.Count - 1].Close;
            CurrentStrike = GetCurrentStrike();
            
        }

        private string GetCurrentStrike() // вычисляем ближайший страйк
        {
            string currStrike = null;

            for (int i = 0; i < _optionSecurities.Count-1; i++)
            {
                int strike1 = int.Parse(_optionSecurities[i].Split('-')[2]);
                int strike2 = int.Parse(_optionSecurities[i+1].Split('-')[2]);

                if (LastPrice > strike1 && LastPrice < strike2)
                {
                    if (LastPrice - strike1 >= strike2 - LastPrice)
                    {
                        currStrike = _optionSecurities[i + 1];
                    }
                    else
                    {
                        currStrike = _optionSecurities[i];
                    }
                    break;
                }
            }
            return currStrike;
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
