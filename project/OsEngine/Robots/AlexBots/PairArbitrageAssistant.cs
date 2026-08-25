using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using System.Threading;
using System;
using OsEngine.Logging;

namespace OsEngine.Robots.AlexBots
{
    [Bot("PairArbitrageAssistant")]
    public class PairArbitrageAssistant : BotPanel
    {
        public PairArbitrageAssistant(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Pair);
            _tabPair = TabsPair[0];

            if (_tabPair.Pairs == null ||
                _tabPair.Pairs.Count == 0)
            {
                _tabPair.CreatePair();
            }

            TabCreate(BotTabType.Simple);
            _tabNormalizationValue = TabsSimple[0];

            OffPositionSupport(_tabNormalizationValue);

            PriceModule = new PriceModule(_tabPair, this, _tabNormalizationValue);
            PriceModule.LogMessageEvent += this.SendNewLogMessage;

            for (int i = 0; i < _tabPair.Pairs.Count; i++)
            {
                OffPositionSupport(_tabPair.Pairs[i].Tab1);
                OffPositionSupport(_tabPair.Pairs[i].Tab2);
            }

            _tabPair.PairToTradeCreateEvent += _tabPair_PairToTradeCreateEvent;

            // Base settings

            OrderLifeTimeSeconds = CreateParameter("Order life time seconds", 30, 1, 1, 1, " Order ");
            OrderSlippageType = CreateParameter("Orders slippage type",
              PairAssistantIndexDeviationType.None.ToString(), new[]
            { PairAssistantIndexDeviationType.Percent.ToString(),
              PairAssistantIndexDeviationType.Absolute.ToString(),
              PairAssistantIndexDeviationType.None.ToString() }, " Order ");

            OrderSlippage = CreateParameter("Orders slippage value", 0, 1, 1, 1m, " Order ");

            // Volume settings

            VolumeFinalSec1 = CreateParameter("Volume Final Sec1", 5, 1, 1, 1m, " Volume ");
            VolumeOneOrderSec1 = CreateParameter("Volume One Order Sec1", 1, 1, 1, 1m, " Volume ");
            VolumeRatio = CreateParameter("Ratio", 1, 1, 1, 1m, " Volume ");

            // Scenario creation

            ScenarioOne = new ScenarioOne(this, PriceModule);
            ScenarioTwo = new ScenarioTwo(this, PriceModule);
            ScenarioThree = new ScenarioThree(this, PriceModule);
            ScenarioFour = new ScenarioFour(this, PriceModule);

            Thread worker = new Thread(WorkerPlace);
            worker.Start();

            this.ParamGuiSettings.Height = 600;
            this.ParamGuiSettings.Width = 600;
            this.ParametrsChangeByUser += PairArbitrageAssistant_ParametrsChangeByUser;
        }

        private void PairArbitrageAssistant_ParametrsChangeByUser()
        {
            try
            {
                if (_chartUi != null)
                {
                    _chartUi._chart.Clear();
                }
            }
            catch
            {
                // ignore
            }
        }

        private void _tabPair_PairToTradeCreateEvent(PairToTrade pair)
        {
            for (int i = 0; i < _tabPair.Pairs.Count; i++)
            {
                OffPositionSupport(_tabPair.Pairs[i].Tab1);
                OffPositionSupport(_tabPair.Pairs[i].Tab2);
            }
        }

        private void OffPositionSupport(BotTabSimple tab)
        {
            tab.ManualPositionSupport.DoubleExitIsOn = false;
            tab.ManualPositionSupport.ProfitIsOn = false;
            tab.ManualPositionSupport.SecondToCloseIsOn = false;
            tab.ManualPositionSupport.SecondToOpenIsOn = false;
            tab.ManualPositionSupport.SetbackToCloseIsOn = false;
            tab.ManualPositionSupport.SetbackToOpenIsOn = false;
            tab.ManualPositionSupport.StopIsOn = false;
        }

        public override string GetNameStrategyType()
        {
            return "PairArbitrageAssistant";
        }

        public override void ShowIndividualSettingsDialog()
        {


        }

        private BotTabPair _tabPair;

        private BotTabSimple _tabNormalizationValue;

        // Chart

        CandleChartUiPair _chartUi;

        public void ShowChartTripleDialog()
        {
            if (_chartUi == null)
            {
                _chartUi = new CandleChartUiPair(this.NameStrategyUniq, StartProgram.IsOsTrader, this);
                _chartUi.Closed += _chartUi_Closed;
                _chartUi.Show();
            }
            else
            {
                _chartUi.Activate();
            }

        }

        private void _chartUi_Closed(object sender, EventArgs e)
        {
            _chartUi = null;
        }


        // Other

        public StrategyParameterInt OrderLifeTimeSeconds;

        public StrategyParameterString OrderSlippageType;

        public StrategyParameterDecimal OrderSlippage;

        // Price NORMALIZATION

        public PriceModule PriceModule;

        // Volume settings and NORMALIZATION

        public StrategyParameterDecimal VolumeFinalSec1;
        public StrategyParameterDecimal VolumeOneOrderSec1;
        public StrategyParameterDecimal VolumeRatio;

        // Scenario 1
        public ScenarioOne ScenarioOne;

        // Scenario 2
        public ScenarioTwo ScenarioTwo;

        // Scenario 3
        public ScenarioThree ScenarioThree;

        // Scenario 4
        public ScenarioFour ScenarioFour;

        // Geters to interface

        public string GetScenarioInWorkString()
        {
            return ScenarioInWork.ToString();
        }

        public string GetScenarioSelectedInGuiString()
        {
            return SelectedScenario.ToString();
        }

        public string GetSpreadStateString()
        {

            if (SelectedScenario == ScenarioType.None)
            {
                return "None";
            }

            bool canSetOrders = CanTradeByCurrentScenario();

            string result = "don`t pass";

            if (canSetOrders)
            {
                result = "pass";
            }

            return result;
        }

        public string GetSpreadValuesString()
        {
            if (SelectedScenario == ScenarioType.None)
            {
                return "None";
            }

            string spread = "";

            if (SelectedScenario == ScenarioType.ScenarioOne)
            {
                spread = ScenarioOne.GetSpreadStringToGui();
            }
            if (SelectedScenario == ScenarioType.ScenarioTwo)
            {
                spread = ScenarioTwo.GetSpreadStringToGui();
            }
            if (SelectedScenario == ScenarioType.ScenarioThree)
            {
                spread = ScenarioThree.GetSpreadStringToGui();
            }
            if (SelectedScenario == ScenarioType.ScenarioFour)
            {
                spread = ScenarioFour.GetSpreadStringToGui();
            }

            return spread;
        }

        public string GetTimerValue()
        {
            if (ScenarioInWork == ScenarioType.None &&
                SelectedScenario == ScenarioType.None)
            {
                return "None";
            }
            else if (ScenarioInWork == ScenarioType.None &&
              SelectedScenario != ScenarioType.None)
            {
                string result = "left time: ";

                if (SelectedScenario == ScenarioType.ScenarioOne)
                {
                    result += ScenarioOne.GetWorkTimeMinutesInTimeSpan().ToString();
                    return result;
                }
                else if (SelectedScenario == ScenarioType.ScenarioTwo)
                {
                    result += ScenarioTwo.GetWorkTimeMinutesInTimeSpan().ToString();
                    return result;
                }
                else if (SelectedScenario == ScenarioType.ScenarioThree)
                {
                    result += ScenarioThree.GetWorkTimeMinutesInTimeSpan().ToString();
                    return result;
                }
                else if (SelectedScenario == ScenarioType.ScenarioFour)
                {
                    result += ScenarioFour.GetWorkTimeMinutesInTimeSpan().ToString();
                    return result;
                }
                else { return "None"; }
            }
            else if (ScenarioInWork != ScenarioType.None)
            {
                string result = "left time: ";

                TimeSpan timeInWork = DateTime.Now - _timeStartWork;

                if (ScenarioInWork == ScenarioType.ScenarioOne)
                {
                    timeInWork = timeInWork.Add(new TimeSpan(0, -ScenarioOne.WorkTimeMinutes.ValueInt, 0));
                    result += timeInWork.ToString();
                    return result;
                }
                else if (ScenarioInWork == ScenarioType.ScenarioTwo)
                {
                    timeInWork = timeInWork.Add(new TimeSpan(0, -ScenarioTwo.WorkTimeMinutes.ValueInt, 0));
                    result += timeInWork.ToString();
                    return result;
                }
                else if (ScenarioInWork == ScenarioType.ScenarioThree)
                {
                    timeInWork = timeInWork.Add(new TimeSpan(0, -ScenarioThree.WorkTimeMinutes.ValueInt, 0));
                    result += timeInWork.ToString();
                    return result;
                }
                else if (ScenarioInWork == ScenarioType.ScenarioFour)
                {
                    timeInWork = timeInWork.Add(new TimeSpan(0, -ScenarioFour.WorkTimeMinutes.ValueInt, 0));
                    result += timeInWork.ToString();
                    return result;
                }

                else { return "None"; }
            }

            return "";
        }

        public string GetEstimateQty()
        {
            if(_tabPair.Pairs == null)
            {
                return "";
            }

            if (_tabPair.Pairs.Count == 0)
            {
                return "Securities is note active";
            }

            if (_tabPair.Pairs[0].Tab1 == null ||
                _tabPair.Pairs[0].Tab2 == null ||
                _tabPair.Pairs[0].Tab1.Securiti == null ||
                _tabPair.Pairs[0].Tab2.Securiti == null)
            {
                return "Securities is note active";
            }

            decimal volumeAllSec1 = Math.Round(VolumeFinalSec1.ValueDecimal, _tabPair.Pairs[0].Tab1.Securiti.DecimalsVolume);
            decimal volumeOneOrderSec1 = Math.Round(VolumeOneOrderSec1.ValueDecimal, _tabPair.Pairs[0].Tab1.Securiti.DecimalsVolume);

            string result = "Sec 1: " + volumeAllSec1 + " / " + volumeOneOrderSec1;



            decimal volumeAllSec2 = Math.Round(VolumeFinalSec1.ValueDecimal * VolumeRatio.ValueDecimal, _tabPair.Pairs[0].Tab2.Securiti.DecimalsVolume);
            decimal volumeOneOrderSec2 = Math.Round(VolumeOneOrderSec1.ValueDecimal * VolumeRatio.ValueDecimal, _tabPair.Pairs[0].Tab2.Securiti.DecimalsVolume);

            result += " || Sec 2: " + volumeAllSec2
                + " / " + volumeOneOrderSec2;

            if (SelectedScenario == ScenarioType.ScenarioOne)
            {
                result += "  " + ScenarioOne.ScenarioOneSide.ValueString;
            }
            if (SelectedScenario == ScenarioType.ScenarioTwo)
            {
                result += "  " + ScenarioTwo.ScenarioTwoSide.ValueString;
            }
            if (SelectedScenario == ScenarioType.ScenarioThree)
            {
                result += "  " + ScenarioThree.ScenarioThreeSide.ValueString;
            }
            if (SelectedScenario == ScenarioType.ScenarioFour)
            {
                result += "  " + ScenarioFour.ScenarioFourSide.ValueString;
            }


            return result;
        }

        public string GetCurrentQty()
        {
            if (_tabPair.Pairs == null ||
                _tabPair.Pairs.Count == 0 ||
                _tabPair.Pairs[0] == null)
            {
                return "None";
            }

            if (ScenarioInWork == ScenarioType.None)
            {
                return "None";
            }

            string result = "";

            StrategyParameterString side = GetCurScenarioSide();

            if (side.ValueString == "FirstBuy_SecondSell")
            {
                result = "Sec 1, +: " + GetVolumeExecute(_tabPair.Pairs[0].Tab1);
                result += " Sec 2, -" + GetVolumeExecute(_tabPair.Pairs[0].Tab2);
            }
            else // FirstSell_SecondBuy
            {
                result = "Sec 1: -" + GetVolumeExecute(_tabPair.Pairs[0].Tab1);
                result += " Sec 2 +" + GetVolumeExecute(_tabPair.Pairs[0].Tab2);
            }

            return result;
        }

        public StrategyParameterString GetCurScenarioSide()
        {
            if (SelectedScenario == ScenarioType.ScenarioOne)
            {
                return ScenarioOne.ScenarioOneSide;
            }
            if (SelectedScenario == ScenarioType.ScenarioTwo)
            {
                return ScenarioTwo.ScenarioTwoSide;
            }
            if (SelectedScenario == ScenarioType.ScenarioThree)
            {
                return ScenarioThree.ScenarioThreeSide;
            }
            if (SelectedScenario == ScenarioType.ScenarioFour)
            {
                return ScenarioFour.ScenarioFourSide;
            }

            return null;
        }

        // logic

        public void SetScenarioInWork(ScenarioType startedScenario)
        {
            SelectedScenario = startedScenario;

            if (_tabPair.Pairs == null &&
                _tabPair.Pairs.Count == 0)
            {
                TabsSimple[0].SetNewLogMessage("Can`t start new scenario. You have to customize the securities for trading", Logging.LogMessageType.Error);
                return;
            }

            if (_tabPair.Pairs.Count == 0)
            {
                TabsSimple[0].SetNewLogMessage("Can`t start new scenario. You have to customize the securities for trading", Logging.LogMessageType.Error);
                return;
            }

            BotTabSimple tabOne = _tabPair.Pairs[0].Tab1;
            BotTabSimple tabTwo = _tabPair.Pairs[0].Tab2;

            if (tabOne.Portfolio == null)
            {
                tabOne.SetNewLogMessage("First Security are not set up. No PORTFOLIO", Logging.LogMessageType.Error);
                return;
            }

            if (tabOne.Securiti == null)
            {
                tabOne.SetNewLogMessage("First Security are not set up. No SECURITY", Logging.LogMessageType.Error);
                return;
            }

            if (tabOne.IsConnected == false ||
                tabOne.IsReadyToTrade == false)
            {
                tabOne.SetNewLogMessage("First Security are not set up. No ready to trade", Logging.LogMessageType.Error);
                return;
            }

            if (tabTwo.Portfolio == null)
            {
                tabTwo.SetNewLogMessage("Second Security are not set up. No PORTFOLIO", Logging.LogMessageType.Error);
                return;
            }

            if (tabTwo.Securiti == null)
            {
                tabTwo.SetNewLogMessage("Second Security are not set up. No SECURITY", Logging.LogMessageType.Error);
                return;
            }

            if (tabTwo.IsConnected == false ||
                tabTwo.IsReadyToTrade == false)
            {
                tabTwo.SetNewLogMessage("Second Security are not set up. No ready to trade", Logging.LogMessageType.Error);
                return;
            }


            if (_threadIsWork == true)
            {
                TabsSimple[0].SetNewLogMessage("Can`t start new scenario. Old scenario in the works", Logging.LogMessageType.Error);
                return;
            }

            if (ScenarioInWork != ScenarioType.None)
            {
                TabsSimple[0].SetNewLogMessage("Can`t start new scenario. Old scenario in the works", Logging.LogMessageType.Error);
                return;
            }

            if (SelectedScenario == ScenarioType.None)
            {
                TabsSimple[0].SetNewLogMessage("Can`t start new scenario. You have to pick it", Logging.LogMessageType.Error);
                return;
            }

            if (VolumeRatio.ValueDecimal == 0)
            {
                TabsSimple[0].SetNewLogMessage("Can`t start new scenario. Volume Ratio settings can`t be a zero", Logging.LogMessageType.Error);
                return;
            }

            decimal volumeAllSecondTab = Math.Round(VolumeFinalSec1.ValueDecimal * VolumeRatio.ValueDecimal, _tabPair.Pairs[0].Tab2.Securiti.DecimalsVolume);

            if (volumeAllSecondTab == 0)
            {
                TabsSimple[0].SetNewLogMessage("Can`t start new scenario. Final volume by second security is zero", Logging.LogMessageType.Error);
                return;
            }

            string sec1FinalVol = VolumeFinalSec1.ValueDecimal.ToString();
            string sec2FinalVol = volumeAllSecondTab.ToString();

            StrategyParameterString side = GetCurScenarioSide();

            if (side != null)
            {
                if (side.ValueString == PairAssistantTradeSide.FirstBuy_SecondSell.ToString())
                {
                    sec1FinalVol = "+" + sec1FinalVol;
                    sec2FinalVol = "-" + volumeAllSecondTab;
                }
                else if (side.ValueString == PairAssistantTradeSide.FirstSell_SecondBuy.ToString())
                {
                    sec1FinalVol = "-" + sec1FinalVol;
                    sec2FinalVol = "+" + volumeAllSecondTab;
                }
            }

            if (tabOne.Securiti.Name == tabTwo.Securiti.Name)
            {
                TabsSimple[0].SetNewLogMessage("Can`t start new scenario. You can`t trade one security. Set other securities", Logging.LogMessageType.Error);
                return;
            }

            string message = "Are you sure you want to start a new script?. \r";
            message += "Scenario: " + startedScenario + "\r";
            message += "Old transaction data in the robot will be erased. \r";
            message += "Final Volume in FIRST security to Open: " + sec1FinalVol + "\r";
            message += "Final Volume in SECOND security to Open: " + sec2FinalVol + "\r";

            AcceptDialogUi acceptUi = new AcceptDialogUi(message);
            acceptUi.ShowDialog();

            if (acceptUi.UserAcceptAction == false)
            {
                return;
            }

            IsOnPause = false;

            ScenarioInWork = startedScenario;
        }

        public void StopScenarioInWork()
        {
            if (ScenarioInWork == ScenarioType.None)
            {
                TabsSimple[0].SetNewLogMessage("Can`t stop scenario. No scenario in work", Logging.LogMessageType.Error);
                return;
            }

            _needToStop = true;
        }

        public void PauseScenarioInWork()
        {
            if (ScenarioInWork == ScenarioType.None)
            {
                return;
            }

            if (IsOnPause == false)
            {
                IsOnPause = true;
                this.SendNewLogMessage("Pause regime ON " + this.NameStrategyUniq, LogMessageType.System);
                return;
            }
            else if (IsOnPause == true)
            {
                IsOnPause = false;
                this.SendNewLogMessage("Pause regime OFF " + this.NameStrategyUniq, LogMessageType.System);
                return;
            }
        }

        public bool IsOnPause;

        private bool _cancelOnPause;

        public bool CanTradeByCurrentScenario()
        {
            bool canSetOrders = false;

            if (SelectedScenario == ScenarioType.None)
            {
                return false;
            }

            if (SelectedScenario == ScenarioType.ScenarioOne)
            {
                canSetOrders = ScenarioOne.CanOpenPositionsBySpread();
            }
            if (SelectedScenario == ScenarioType.ScenarioTwo)
            {
                canSetOrders = ScenarioTwo.CanOpenPositionsBySpread();
            }
            if (SelectedScenario == ScenarioType.ScenarioThree)
            {
                canSetOrders = ScenarioThree.CanOpenPositionsBySpread();
            }
            if (SelectedScenario == ScenarioType.ScenarioFour)
            {
                canSetOrders = ScenarioFour.CanOpenPositionsBySpread();
            }

            return canSetOrders;
        }

        public bool CanTradeByWorkScenario()
        {
            bool canSetOrders = false;

            if (ScenarioInWork == ScenarioType.None)
            {
                return false;
            }

            if (ScenarioInWork == ScenarioType.ScenarioOne)
            {
                canSetOrders = ScenarioOne.CanOpenPositionsBySpread();
            }
            if (ScenarioInWork == ScenarioType.ScenarioTwo)
            {
                canSetOrders = ScenarioTwo.CanOpenPositionsBySpread();
            }
            if (ScenarioInWork == ScenarioType.ScenarioThree)
            {
                canSetOrders = ScenarioThree.CanOpenPositionsBySpread();
            }
            if (ScenarioInWork == ScenarioType.ScenarioFour)
            {
                canSetOrders = ScenarioFour.CanOpenPositionsBySpread();
            }

            return canSetOrders;
        }

        public ScenarioType SelectedScenario;

        public ScenarioType ScenarioInWork;

        private bool _threadIsWork;

        private bool _needToStop;

        private void WorkerPlace()
        {
            while (true)
            {
                Thread.Sleep(500);

                try
                {
                    PriceModule.Process();

                    if (ScenarioInWork != ScenarioType.None)
                    {
                        TradeLogic();
                    }
                }
                catch (Exception e)
                {
                    SendNewLogMessage(e.ToString(), LogMessageType.Error);
                    Thread.Sleep(10000);
                }
            }
        }

        private void TradeLogic()
        {
            try
            {
                if (ScenarioInWork == ScenarioType.ScenarioOne)
                {
                    ExecutionPlace(_tabPair.Pairs[0].Tab1,
                                   _tabPair.Pairs[0].Tab2,
                                   ScenarioOne.ScenarioOneSide,
                                   ScenarioOne.WorkTimeMinutes,
                                   PriceModule.PriceInMarketDepthSecOne,
                                   PriceModule.PriceInMarketDepthSecTwo,
                                   VolumeOneOrderSec1, VolumeRatio,
                                   VolumeFinalSec1);
                }
                if (ScenarioInWork == ScenarioType.ScenarioTwo)
                {
                    ExecutionPlace(_tabPair.Pairs[0].Tab1,
                                   _tabPair.Pairs[0].Tab2,
                                   ScenarioTwo.ScenarioTwoSide,
                                   ScenarioTwo.WorkTimeMinutes,
                                   PriceModule.PriceInMarketDepthSecOne,
                                   PriceModule.PriceInMarketDepthSecTwo,
                                   VolumeOneOrderSec1, VolumeRatio,
                                   VolumeFinalSec1);
                }
                if (ScenarioInWork == ScenarioType.ScenarioThree)
                {
                    ExecutionPlace(_tabPair.Pairs[0].Tab1,
                                   _tabPair.Pairs[0].Tab2,
                                   ScenarioThree.ScenarioThreeSide,
                                   ScenarioThree.WorkTimeMinutes,
                                   PriceModule.PriceInMarketDepthSecOne,
                                   PriceModule.PriceInMarketDepthSecTwo,
                                   VolumeOneOrderSec1, VolumeRatio,
                                   VolumeFinalSec1);
                }
                if (ScenarioInWork == ScenarioType.ScenarioFour)
                {
                    ExecutionPlace(_tabPair.Pairs[0].Tab1,
                                   _tabPair.Pairs[0].Tab2,
                                   ScenarioFour.ScenarioFourSide,
                                   ScenarioFour.WorkTimeMinutes,
                                   PriceModule.PriceInMarketDepthSecOne,
                                   PriceModule.PriceInMarketDepthSecTwo,
                                   VolumeOneOrderSec1, VolumeRatio,
                                   VolumeFinalSec1);
                }


            }
            catch (Exception ex)
            {
                TabsSimple[0].SetNewLogMessage("Error on execution: " + ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        DateTime _timeStartWork;

        private void ExecutionPlace(
            BotTabSimple tabOne,
            BotTabSimple tabTwo,
            StrategyParameterString SideToTrade,
            StrategyParameterInt MinutesToWork,
            StrategyParameterString PriceToOrderInMarketDepthSecOne,
            StrategyParameterString PriceToOrderInMarketDepthSecTwo,
            StrategyParameterDecimal VolumeToOneOrder,
            StrategyParameterDecimal Ratio,
            StrategyParameterDecimal VolumeToExecute)
        {
            Side tabOneSide = Side.Buy;
            Side tabTwoSide = Side.Sell;

            if (SideToTrade.ValueString == "FirstBuy_SecondSell")
            {
                // do nothin
            }
            else if (SideToTrade.ValueString == "FirstSell_SecondBuy")
            {
                tabOneSide = Side.Sell;
                tabTwoSide = Side.Buy;
            }

            ClearPositions(tabOne);
            ClearPositions(tabTwo);


            _timeStartWork = DateTime.Now;
            _threadIsWork = true;

            while (true)
            {
                Thread.Sleep(1000);

                PriceModule.Process();

                if (IsOnPause == true)
                {
                    if (_cancelOnPause == false)
                    {
                        CanselOrders(tabOne);
                        CanselOrders(tabTwo);
                        _cancelOnPause = true;
                    }

                    continue;
                }
                _cancelOnPause = false;

                if (_needToStop == true)
                {
                    _needToStop = false;
                    _threadIsWork = false;
                    CanselOrders(tabOne);
                    CanselOrders(tabTwo);
                    ScenarioInWork = ScenarioType.None;
                    this.SendNewLogMessage("Operation ended by user ", Logging.LogMessageType.Error);
                    return;
                }

                if (tabOne.IsConnected == false ||
                    tabOne.IsReadyToTrade == false ||
                    tabTwo.IsConnected == false ||
                    tabTwo.IsReadyToTrade == false)
                {
                    this.SendNewLogMessage("Connection lost. Waiting for restoration. Robot logic interrupted. ", Logging.LogMessageType.Error);
                    Thread.Sleep(7000);
                    continue;
                }

                // 0 проверяем не закончилось ли время на работу

                TimeSpan timeInWork = DateTime.Now - _timeStartWork;

                if (timeInWork.TotalMinutes >= MinutesToWork.ValueInt)
                {
                    _needToStop = false;
                    _threadIsWork = false;
                    CanselOrders(tabOne);
                    CanselOrders(tabTwo);
                    ScenarioInWork = ScenarioType.None;
                    this.SendNewLogMessage("Operation ended by time ", Logging.LogMessageType.Error);

                    return;
                }

                // 1 проверяем отзыв ордеров по времени

                if (CheckTabToCanselOrder(tabOne))
                {
                    Thread.Sleep(2000);
                    continue;
                }
                if (CheckTabToCanselOrder(tabTwo))
                {
                    Thread.Sleep(2000);
                    continue;
                }

                if(_lastTimeSendOrder.AddSeconds(3) > DateTime.Now)
                {
                    continue;
                }

                // 2 проверяем не доисполненные ордера

                if (LastOrderExecuteAll(tabOne) == false)
                {
                    if (CanTradeByCurrentScenario() == true)
                    {
                        ReplaseDontExecuteLastOrder(GetDontExecuteOrderVolume(tabOne), tabOneSide, tabOne, PriceToOrderInMarketDepthSecOne.ValueString);
                    }
                    Thread.Sleep(2000);
                    continue;
                }

                if (LastOrderExecuteAll(tabTwo) == false)
                {
                    if (CanTradeByCurrentScenario() == true)
                    {
                        ReplaseDontExecuteLastOrder(GetDontExecuteOrderVolume(tabTwo), tabTwoSide, tabTwo, PriceToOrderInMarketDepthSecTwo.ValueString);
                    }
                    Thread.Sleep(2000);
                    continue;
                }

                // 3 Вкладка 1. Здесь нет недоисполненных ордеров

                decimal volumeOnMarketTabOne = GetVolumeInMarket(tabOne);

                if (volumeOnMarketTabOne == 0)
                {
                    // 3.1. сначала проверяем размер спреда

                    if (CanTradeByCurrentScenario() == true)
                    {
                        // 3.2. теперь проверяем сколько уже объема исполнилось 

                        decimal executeVolTabOne = Math.Round(GetVolumeExecute(tabOne) * Ratio.ValueDecimal, tabTwo.Securiti.DecimalsVolume);
                        decimal executeVolTabTwo = GetVolumeExecute(tabTwo);

                        if (executeVolTabOne <= executeVolTabTwo)
                        {
                            if (executeVolTabOne < Math.Round(VolumeToExecute.ValueDecimal * Ratio.ValueDecimal, tabTwo.Securiti.DecimalsVolume))
                            {
                                CheckTabOneToOpenOrder(VolumeToExecute.ValueDecimal, tabOneSide, tabOne,
                                    PriceToOrderInMarketDepthSecOne.ValueString, VolumeToOneOrder);
                                continue;
                            }
                        }
                    }
                }

                // 4 Вкладка 2

                decimal volumeOnMarketTabTwo = GetVolumeInMarket(tabTwo);

                if (volumeOnMarketTabTwo == 0)
                {
                    decimal executeVolTabTwo = GetVolumeExecute(tabTwo);
                    decimal executeVolTabOne = Math.Round(GetVolumeExecute(tabOne) * Ratio.ValueDecimal, tabTwo.Securiti.DecimalsVolume);

                    decimal volNeadToExecuteInTabTwo = executeVolTabOne - executeVolTabTwo;

                    //if (volNeadToExecuteInTabTwo >= Math.Round(VolumeToOneOrder.ValueDecimal * Ratio.ValueDecimal, tabTwo.Securiti.DecimalsVolume))
                    if (volNeadToExecuteInTabTwo > 0)
                    {
                        CheckTabTwoToOpenOrder(volNeadToExecuteInTabTwo, tabTwoSide,
                            tabTwo, PriceToOrderInMarketDepthSecTwo.ValueString, tabOne, VolumeToOneOrder, VolumeRatio);
                        continue;
                    }
                }

                // 5 если все объёмы исполнены - заканчиваем

                decimal executeTabOne = GetVolumeExecute(tabOne);
                decimal executeTabTwo = GetVolumeExecute(tabTwo);

                if (volumeOnMarketTabOne == 0
                    && volumeOnMarketTabTwo == 0
                    && executeTabOne == VolumeToExecute.ValueDecimal
                    && executeTabTwo == executeTabOne * VolumeRatio.ValueDecimal)
                {
                    string result = "Scenario ended " + ScenarioInWork.ToString() + "\n";

                    if (SideToTrade.ValueString == "FirstBuy_SecondSell")
                    {
                        result += "Final qty Security 1: +" + executeTabOne + "\n";
                        result += "Final qty Security 2: -" + executeTabTwo + "\n";
                    }
                    else
                    {
                        result += "Final qty Security 1: -" + executeTabOne + "\n";
                        result += "Final qty Security 2: +" + executeTabTwo + "\n";
                    }

                    result += "Work time :" + timeInWork.ToString() + "\n";

                    this.SendNewLogMessage(result, LogMessageType.Error);
                    _threadIsWork = false;
                    _needToStop = false;
                    ScenarioInWork = ScenarioType.None;
                    return;
                }
            }

            _threadIsWork = false;
        }

        private bool LastOrderExecuteAll(BotTabSimple tab)
        {
            List<Position> poses = tab.PositionsAll;

            if (poses.Count > 0)
            {
                Position lastPos = poses[poses.Count - 1];

                if (lastPos.OpenActiv == true)
                {
                    return true;
                }

                Order lastOrder = lastPos.OpenOrders[0];

                if (lastOrder.VolumeExecute == 0)
                {
                    return false;
                }

                if (lastOrder.Volume != lastOrder.VolumeExecute)
                {
                    return false;
                }
            }

            return true;
        }

        private decimal GetDontExecuteOrderVolume(BotTabSimple tab)
        {
            List<Position> poses = tab.PositionsAll;

            decimal dontExecuteVolume = 0;

            if (poses.Count > 0)
            {
                Position lastPos = poses[poses.Count - 1];

                if (lastPos.OpenActiv == true)
                {
                    return 0;
                }

                Order lastOrder = poses[poses.Count - 1].OpenOrders[0];

                dontExecuteVolume = lastOrder.Volume - lastOrder.VolumeExecute;
            }

            return dontExecuteVolume;
        }

        private void ReplaseDontExecuteLastOrder(decimal volume, Side side, BotTabSimple tab, string priceType)
        {
            decimal price = 0;

            if (volume == 0)
            {
                return;
            }

            if (priceType == PriceOrderType.Offer.ToString())
            {
                price = tab.PriceBestAsk;
            }
            else if (priceType == PriceOrderType.Bid.ToString())
            {
                price = tab.PriceBestBid;
            }
            else if (priceType == PriceOrderType.Mid.ToString())
            {
                price = tab.PriceCenterMarketDepth;
            }

            if (side == Side.Buy)
            {
                if (OrderSlippageType.ValueString == "Absolute"
                 && OrderSlippage.ValueDecimal != 0)
                {
                    decimal slippageValue = OrderSlippage.ValueDecimal;
                    price = price - slippageValue;
                }
                else if (OrderSlippageType.ValueString == "Percent"
                         && OrderSlippage.ValueDecimal != 0)
                {
                    decimal slippageValue = price * (OrderSlippage.ValueDecimal / 100);
                    price = price - slippageValue;
                }

                tab.BuyAtLimit(volume, price);
                Thread.Sleep(1000);
            }
            else if (side == Side.Sell)
            {
                if (OrderSlippageType.ValueString == "Absolute"
                 && OrderSlippage.ValueDecimal != 0)
                {
                    decimal slippageValue = OrderSlippage.ValueDecimal;
                    price = price + slippageValue;
                }
                else if (OrderSlippageType.ValueString == "Percent"
                         && OrderSlippage.ValueDecimal != 0)
                {
                    decimal slippageValue = price * (OrderSlippage.ValueDecimal / 100);
                    price = price + slippageValue;
                }

                tab.SellAtLimit(volume, price);
                Thread.Sleep(1000);
            }
        }

        private void ClearPositions(BotTabSimple tab)
        {
            List<Position> positions = tab.PositionsAll;

            for (int i = 0; i < positions.Count; i++)
            {
                tab._journal.DeletePosition(positions[i]);
                i--;
            }

            tab._journal.Clear();
        }

        private bool CheckTabToCanselOrder(BotTabSimple tab)
        {
            List<Position> positions = tab.PositionsOpenAll;

            for (int i = 0; i < positions.Count; i++)
            {
                Position curPos = positions[i];

                if (curPos.OpenActiv == false)
                {
                    continue;
                }

                Order order = curPos.OpenOrders[0];

                DateTime serverTime = tab.TimeServerCurrent;
                TimeSpan timeSpan = serverTime - order.TimeCreate;

                if (timeSpan.TotalSeconds > OrderLifeTimeSeconds.ValueInt)
                {
                    tab.CloseAllOrderToPosition(curPos);
                    Thread.Sleep(1000);
                    return true;
                }
            }

            return false;
        }

        private void CheckTabOneToOpenOrder(decimal allVolumeToExecute, Side side, BotTabSimple tab, string priceType,
            StrategyParameterDecimal VolumeToOneOrder)
        {
            decimal volume = allVolumeToExecute;

            if (volume > VolumeToOneOrder.ValueDecimal)
            {
                volume = VolumeToOneOrder.ValueDecimal;
            }

            decimal price = 0;

            if (priceType == PriceOrderType.Offer.ToString())
            {
                price = tab.PriceBestAsk;
            }
            else if (priceType == PriceOrderType.Bid.ToString())
            {
                price = tab.PriceBestBid;
            }
            else if (priceType == PriceOrderType.Mid.ToString())
            {
                price = tab.PriceCenterMarketDepth;
            }

            if (side == Side.Buy)
            {
                if (OrderSlippageType.ValueString == "Absolute"
                  && OrderSlippage.ValueDecimal != 0)
                {
                    decimal slippageValue = OrderSlippage.ValueDecimal;
                    price = price - slippageValue;
                }
                else if (OrderSlippageType.ValueString == "Percent"
                         && OrderSlippage.ValueDecimal != 0)
                {
                    decimal slippageValue = price * (OrderSlippage.ValueDecimal / 100);
                    price = price - slippageValue;
                }


                tab.BuyAtLimit(volume, price);
                _lastTimeSendOrder = DateTime.Now;
                Thread.Sleep(1000);
            }
            else if (side == Side.Sell)
            {
                if (OrderSlippageType.ValueString == "Absolute"
                  && OrderSlippage.ValueDecimal != 0)
                {
                    decimal slippageValue = OrderSlippage.ValueDecimal;
                    price = price + slippageValue;
                }
                else if (OrderSlippageType.ValueString == "Percent"
                         && OrderSlippage.ValueDecimal != 0)
                {
                    decimal slippageValue = price * (OrderSlippage.ValueDecimal / 100);
                    price = price + slippageValue;
                }

                tab.SellAtLimit(volume, price);
                _lastTimeSendOrder = DateTime.Now;
                Thread.Sleep(1000);
            }
        }

        private void CheckTabTwoToOpenOrder(decimal allVolumeToExecute, Side side,
            BotTabSimple tabTwo, string priceType, BotTabSimple tabOne,
            StrategyParameterDecimal VolumeToOneOrder, StrategyParameterDecimal Ratio)
        {
            decimal volume = allVolumeToExecute;

            if (volume > Math.Round(VolumeToOneOrder.ValueDecimal * Ratio.ValueDecimal, tabTwo.Securiti.DecimalsVolume))
            {
                volume = Math.Round(VolumeToOneOrder.ValueDecimal * Ratio.ValueDecimal, tabTwo.Securiti.DecimalsVolume);
            }

            decimal price = 0;

            if (priceType == PriceOrderType.Offer.ToString())
            {
                price = tabTwo.PriceBestAsk;
            }
            else if (priceType == PriceOrderType.Bid.ToString())
            {
                price = tabTwo.PriceBestBid;
            }
            else if (priceType == PriceOrderType.Mid.ToString())
            {
                price = tabTwo.PriceCenterMarketDepth;
            }

            /* decimal lastPriceOrderOnTabOne = 0;

             List<Position> posesTabOne = tabOne.PositionsAll;

             for (int i = posesTabOne.Count - 1; i >= 0; i--)
             {
                 if (posesTabOne[i].State == PositionStateType.Open)
                 {
                     lastPriceOrderOnTabOne = posesTabOne[i].OpenOrders[0].PriceReal;

                     if (lastPriceOrderOnTabOne == 0)
                     {
                         lastPriceOrderOnTabOne = posesTabOne[i].OpenOrders[0].Price;
                     }

                     break;
                 }
             }

             if (lastPriceOrderOnTabOne == 0)
             {
                 SendNewLogMessage("Last price in poses on sec 1, equals zero", Logging.LogMessageType.Error);
                 return;
             }*/

            if (side == Side.Buy)
            {
                if (price > tabTwo.PriceBestAsk)
                {
                    price = tabTwo.PriceBestAsk;
                }

                if (OrderSlippageType.ValueString == "Absolute"
                    && OrderSlippage.ValueDecimal != 0)
                {
                    decimal slippageValue = OrderSlippage.ValueDecimal;
                    price = price - slippageValue;
                }
                else if (OrderSlippageType.ValueString == "Percent"
                         && OrderSlippage.ValueDecimal != 0)
                {
                    decimal slippageValue = price * (OrderSlippage.ValueDecimal / 100);
                    price = price - slippageValue;
                }

                tabTwo.BuyAtLimit(volume, price);
                _lastTimeSendOrder = DateTime.Now;
                Thread.Sleep(1000);
            }
            else if (side == Side.Sell)
            {
                if (price < tabTwo.PriceBestBid)
                {
                    price = tabTwo.PriceBestBid;
                }

                if (OrderSlippageType.ValueString == "Absolute"
                    && OrderSlippage.ValueDecimal != 0)
                {
                    decimal slippageValue = OrderSlippage.ValueDecimal;
                    price = price + slippageValue;
                }
                else if (OrderSlippageType.ValueString == "Percent"
                         && OrderSlippage.ValueDecimal != 0)
                {
                    decimal slippageValue = price * (OrderSlippage.ValueDecimal / 100);
                    price = price + slippageValue;
                }

                tabTwo.SellAtLimit(volume, price);
                _lastTimeSendOrder = DateTime.Now;
                Thread.Sleep(1000);
            }
        }

        private DateTime _lastTimeSendOrder = DateTime.MinValue;

        public decimal GetVolumeExecute(BotTabSimple tab)
        {
            List<Position> poses = tab.PositionsAll;

            decimal volume = 0;

            for (int i = 0; i < poses.Count; i++)
            {
                if (poses[i].State == PositionStateType.Open ||
                    poses[i].State == PositionStateType.Opening)
                {
                    volume += poses[i].OpenVolume;
                }
            }

            return volume;
        }

        public decimal GetVolumeInMarket(BotTabSimple tab)
        {
            List<Position> poses = tab.PositionsAll;

            decimal volume = 0;

            for (int i = 0; i < poses.Count; i++)
            {
                if (poses[i].State == PositionStateType.Opening)
                {
                    volume += poses[i].WaitVolume;
                }
            }

            return volume;
        }

        private void CanselOrders(BotTabSimple tab)
        {
            List<Position> poses = tab.PositionsOpenAll;

            for (int i = 0; i < poses.Count; i++)
            {
                if (poses[i].OpenActiv)
                {
                    tab.CloseAllOrderToPosition(poses[i]);
                    Thread.Sleep(1000);
                }
            }
        }
    }

    public class PriceModule
    {
        public PriceModule(BotTabPair pair, BotPanel panel, BotTabSimple tabNormalizing)
        {
            _pair = pair;
            _tabNormalizing = tabNormalizing;

            if (_pair.Pairs.Count != 0)
            {
                _tab1 = _pair.Pairs[0].Tab1;
                _tab2 = _pair.Pairs[0].Tab2;
            }

            _pair.PairToTradeCreateEvent += _pair_PairToTradeCreateEvent;

            PriceInMarketDepthSecOne
                  = panel.CreateParameter("Base price location. Sec 1",
                  PriceOrderType.Mid.ToString(), new[]
                { PriceOrderType.Mid.ToString(),
                    PriceOrderType.Offer.ToString(),
                    PriceOrderType.Bid.ToString()}, " Price ");

            PriceInMarketDepthSecTwo
                = panel.CreateParameter("Base price location. Sec 2",
                PriceOrderType.Mid.ToString(), new[]
                { PriceOrderType.Mid.ToString(),
                    PriceOrderType.Offer.ToString(),
                    PriceOrderType.Bid.ToString()}, " Price ");

            PriceInMarketDepthSecThree
                                = panel.CreateParameter("Base price location. Sec 3",
                                PriceOrderType.Mid.ToString(), new[]
                { PriceOrderType.Mid.ToString(),
                    PriceOrderType.Offer.ToString(),
                    PriceOrderType.Bid.ToString()}, " Price ");

            PriceReverseQuoteSecOne = panel.CreateParameter("Revers quote. Sec1",false, " Price ");
            PriceReverseQuoteSecTwo = panel.CreateParameter("Revers quote. Sec2", false, " Price ");
            PriceReverseQuoteSecThree = panel.CreateParameter("Revers quote. Sec3", false, " Price ");

            SpreadDeviationType
            = panel.CreateParameter("Index deviation type",
            PairAssistantIndexDeviationType.Absolute.ToString(), new[]
          { PairAssistantIndexDeviationType.Absolute.ToString(),
            PairAssistantIndexDeviationType.Percent.ToString() }, " Price ");

            HoursOffsetSecOne = panel.CreateParameter("Hours offset. Sec 1", 0, 1, 1, 1, " Price ");
            HoursOffsetSecTwo = panel.CreateParameter("Hours offset. Sec 2", 0, 1, 1, 1, " Price ");
            HoursOffsetSecThree = panel.CreateParameter("Hours offset. Sec 3", 0, 1, 1, 1, " Price ");

            PriceKoeffSec1 = panel.CreateParameter("Price koeff. Sec 1", 1, 1, 1, 1m, " Price ");
            PriceKoeffSec2 = panel.CreateParameter("Price koeff. Sec 2", 1, 1, 1, 1m, " Price ");

            NormalizationByPriceSec3Type
                = panel.CreateParameter("Normalization By Price Sec 3", NormalizationPriceByPriceSec3Type.Off.ToString(), new[]
                { NormalizationPriceByPriceSec3Type.Off.ToString(),
                  NormalizationPriceByPriceSec3Type.DivideSec1.ToString(),
                  NormalizationPriceByPriceSec3Type.DivideSec2.ToString(),
                  NormalizationPriceByPriceSec3Type.MyltiplySec1.ToString(),
                  NormalizationPriceByPriceSec3Type.MyltiplySec2.ToString()
                },
               " Price ");



        }

        private void _pair_PairToTradeCreateEvent(PairToTrade newPair)
        {
            if (_pair.Pairs.Count != 0)
            {
                _tab1 = _pair.Pairs[0].Tab1;
                _tab2 = _pair.Pairs[0].Tab2;
            }
        }

        BotTabPair _pair;
        BotTabSimple _tab1;
        BotTabSimple _tab2;
        BotTabSimple _tabNormalizing;

        public StrategyParameterString PriceInMarketDepthSecOne;
        public StrategyParameterString PriceInMarketDepthSecTwo;
        public StrategyParameterString PriceInMarketDepthSecThree;

        public StrategyParameterBool PriceReverseQuoteSecOne;
        public StrategyParameterBool PriceReverseQuoteSecTwo;
        public StrategyParameterBool PriceReverseQuoteSecThree;

        public StrategyParameterInt HoursOffsetSecOne;
        public StrategyParameterInt HoursOffsetSecTwo;
        public StrategyParameterInt HoursOffsetSecThree;

        public StrategyParameterString SpreadDeviationType;

        public StrategyParameterDecimal PriceKoeffSec1;
        public StrategyParameterDecimal PriceKoeffSec2;
        public StrategyParameterString NormalizationByPriceSec3Type;

        public void Process()
        {
            if (_tab1 != null)
            {
                decimal priceBaseSec1 = GetBasePriceFromTab(_tab1, PriceKoeffSec1.ValueDecimal, PriceInMarketDepthSecOne.ValueString, PriceReverseQuoteSecOne.ValueBool);

                if (priceBaseSec1 != 0 &&
                    NormalizationByPriceSec3Type.ValueString == NormalizationPriceByPriceSec3Type.MyltiplySec1.ToString())
                {
                    decimal priceBaseSec3 = GetBasePriceFromTab(_tabNormalizing, 1, PriceInMarketDepthSecThree.ValueString, PriceReverseQuoteSecThree.ValueBool);

                    if (priceBaseSec3 != 0)
                    {
                        priceBaseSec1 = priceBaseSec1 * priceBaseSec3;
                    }
                }
                else if (priceBaseSec1 != 0 &&
                    NormalizationByPriceSec3Type.ValueString == NormalizationPriceByPriceSec3Type.DivideSec1.ToString())
                {
                    decimal priceBaseSec3 = GetBasePriceFromTab(_tabNormalizing, 1, PriceInMarketDepthSecThree.ValueString, PriceReverseQuoteSecThree.ValueBool);

                    if (priceBaseSec3 != 0)
                    {
                        priceBaseSec1 = priceBaseSec1 / priceBaseSec3;
                    }
                }

                priceBaseSec1 = Math.Round(priceBaseSec1,5);

                if (_priceSec1 != priceBaseSec1)
                {
                    _priceSec1 = priceBaseSec1;

                    if (PriceChangeEvent != null)
                    {
                        PriceChangeEvent();
                    }
                }
            }

            if (_tab2 != null)
            {
                decimal priceBaseSec2 = GetBasePriceFromTab(_tab2, PriceKoeffSec2.ValueDecimal, PriceInMarketDepthSecTwo.ValueString, PriceReverseQuoteSecTwo.ValueBool);

                if (priceBaseSec2 != 0 &&
                    NormalizationByPriceSec3Type.ValueString == NormalizationPriceByPriceSec3Type.MyltiplySec2.ToString())
                {
                    decimal priceBaseSec3 = GetBasePriceFromTab(_tabNormalizing, 1, PriceInMarketDepthSecThree.ValueString, PriceReverseQuoteSecThree.ValueBool);

                    if (priceBaseSec3 != 0)
                    {
                        priceBaseSec2 = priceBaseSec2 * priceBaseSec3;
                    }
                }
                else if (priceBaseSec2 != 0 &&
                    NormalizationByPriceSec3Type.ValueString == NormalizationPriceByPriceSec3Type.DivideSec2.ToString())
                {
                    decimal priceBaseSec3 = GetBasePriceFromTab(_tabNormalizing, 1, PriceInMarketDepthSecThree.ValueString, PriceReverseQuoteSecThree.ValueBool);

                    if (priceBaseSec3 != 0)
                    {
                        priceBaseSec2 = priceBaseSec2 / priceBaseSec3;
                    }
                }

                priceBaseSec2 = Math.Round(priceBaseSec2, 5);

                if (_priceSec2 != priceBaseSec2)
                {
                    _priceSec2 = priceBaseSec2;
                    if (PriceChangeEvent != null)
                    {
                        PriceChangeEvent();
                    }
                }
            }
        }

        private decimal GetBasePriceFromTab(BotTabSimple tab, decimal koeff, string priceInMarketDepth, bool revers)
        {
            if (tab == null)
            {
                return 0;
            }
            if (tab.IsConnected == false)
            {
                return 0;
            }

            if (tab.IsReadyToTrade == false)
            {
                return 0;
            }

            PriceOrderType priceType;
            Enum.TryParse(priceInMarketDepth, out priceType);

            decimal result = 0;

            if (priceType == PriceOrderType.Mid)
            {
                result = tab.PriceCenterMarketDepth;
            }
            else if (priceType == PriceOrderType.Bid)
            {
                result = tab.PriceBestBid;
            }
            else if (priceType == PriceOrderType.Offer)
            {
                result = tab.PriceBestAsk;
            }

            if(revers == true
                && result != 0)
            {
                result = 1 / result;
            }

            if (koeff != 0)
            {
                result = result * koeff;
            }

            return result;
        }

        public decimal PriceSec1
        {
            get
            {
                return _priceSec1;
            }
        }
        private decimal _priceSec1;

        public decimal PriceSec2
        {
            get
            {
                return _priceSec2;
            }
        }
        private decimal _priceSec2;

        public decimal CurrentSpread()
        {
            decimal price1 = PriceSec1;
            decimal price2 = PriceSec2;

            if (price1 == 0 ||
                price2 == 0)
            {
                return 0;
            }

            decimal spread = price1 - price2;

            if (SpreadDeviationType.ValueString == PairAssistantIndexDeviationType.Absolute.ToString())
            {
                return Math.Round(spread, 4);
            }
            else if (SpreadDeviationType.ValueString == PairAssistantIndexDeviationType.Percent.ToString())
            {
                decimal percent = spread / (price1 / 100);

                return Math.Round(percent, 4);
            }

            return 0;
        }

        public string GetStringToGui()
        {
            string result = "Sec 1: ";
            result += Math.Round(PriceSec1, 4).ToStringWithNoEndZero();
            result += "  Sec 2: ";
            result += Math.Round(PriceSec2, 4).ToStringWithNoEndZero();

            return result;
        }

        public event Action PriceChangeEvent;

        // log messages / сообщения для лога

        private void SendLogMessage(string message, LogMessageType type)
        {
            if (LogMessageEvent != null)
            {
                LogMessageEvent(message, type);
            }
        }

        public event Action<string, LogMessageType> LogMessageEvent;

        // работа с отдачей свечек для чарта

        public List<Candle> GetSpreadCandles()
        {
            // Price1, Price2 имеют коэффициенты PriceKoeffSec1
            // Price 2 может быть нормализована по инструменту 3 NormalizationPriceByPriceSec3Type
            // Формула: Price1 - Price2. Может быть в абсолюте или процентах SpreadDeviationType


            List<Candle> candles1 = _tab1.CandlesAll;
            List<Candle> candles2 = _tab2.CandlesAll;
            List<Candle> candles3 = _tabNormalizing.CandlesAll;

            if (candles1 == null ||
                candles1.Count == 0)
            {
                return null;
            }

            if (candles2 == null ||
                candles2.Count == 0)
            {
                return null;
            }

            if (NormalizationByPriceSec3Type.ValueString != NormalizationPriceByPriceSec3Type.Off.ToString())
            {
                if (candles3 == null ||
                    candles3.Count == 0)
                {
                    return null;
                }

                if (PriceReverseQuoteSecThree.ValueBool == true)
                {
                    candles3 = GetReversQuoteCandles(candles3);
                }
            }

            // 0 делаем Реверс

            if(PriceReverseQuoteSecOne.ValueBool == true)
            {
                candles1 = GetReversQuoteCandles(candles1);
            }

            if (PriceReverseQuoteSecTwo.ValueBool == true)
            {
                candles2 = GetReversQuoteCandles(candles2);
            }

            // 1 сдвигаем время

            candles1 = GetOffsetCandles(candles1, HoursOffsetSecOne.ValueInt);
            candles2 = GetOffsetCandles(candles2, HoursOffsetSecTwo.ValueInt);
            candles3 = GetOffsetCandles(candles3, HoursOffsetSecThree.ValueInt);

            // 2 умножаем на коэффициенты

            candles1 = GetCoeffCandles(candles1, PriceKoeffSec1.ValueDecimal);
            candles2 = GetCoeffCandles(candles2, PriceKoeffSec2.ValueDecimal);

            // 3 нормализуем 

            /* NormalizationByPriceSec3Type
     = panel.CreateParameter("Normalization By Price Sec 3", NormalizationPriceByPriceSec3Type.Off.ToString(), new[]
     { NormalizationPriceByPriceSec3Type.Off.ToString(),
                   NormalizationPriceByPriceSec3Type.DivideSec1.ToString(),
                   NormalizationPriceByPriceSec3Type.DivideSec2.ToString(),
                   NormalizationPriceByPriceSec3Type.MyltiplySec1.ToString(),
                   NormalizationPriceByPriceSec3Type.MyltiplySec2.ToString()*/

            if (NormalizationByPriceSec3Type.ValueString == NormalizationPriceByPriceSec3Type.DivideSec1.ToString())
            {
                candles1 = GetDivision(candles1, candles3);
            }
            else if (NormalizationByPriceSec3Type.ValueString == NormalizationPriceByPriceSec3Type.DivideSec2.ToString())
            {
                candles2 = GetDivision(candles2, candles3);
            }
            else if (NormalizationByPriceSec3Type.ValueString == NormalizationPriceByPriceSec3Type.MyltiplySec1.ToString())
            {
                candles1 = GetMult(candles1, candles3);
            }
            else if (NormalizationByPriceSec3Type.ValueString == NormalizationPriceByPriceSec3Type.MyltiplySec2.ToString())
            {
                candles2 = GetMult(candles2, candles3);
            }

            // 4 Отнимаем одно от другого

            List<Candle> result = GetSubtractTheCandles(candles1, candles2);

            return result;
        }

        private List<Candle> GetReversQuoteCandles(List<Candle> candles)
        {
            List<Candle> result = new List<Candle>();

            for(int i = 0;i < candles.Count;i++)
            {
                Candle candle = candles[i];
                Candle newCandle = new Candle();

                newCandle.Open = Math.Round(1 / candle.Open,5);
                newCandle.High = Math.Round(1 / candle.High, 5);
                newCandle.Low = Math.Round(1 / candle.Low, 5);
                newCandle.Close = Math.Round(1 / candle.Close, 5);

                newCandle.Volume = candle.Volume;
                newCandle.TimeStart = candle.TimeStart;
                newCandle.State = candle.State;

                if(newCandle.Open > newCandle.High)
                {
                    newCandle.High = newCandle.Open;
                }
                if(newCandle.Open < newCandle.Low)
                {
                    newCandle.Low = newCandle.Open;
                }
                if (newCandle.Close > newCandle.High)
                {
                    newCandle.High = newCandle.Close;
                }
                if (newCandle.Close < newCandle.Low)
                {
                    newCandle.Low = newCandle.Close;
                }
                result.Add(newCandle);
            }

            return result;
        }


        private List<Candle> GetOffsetCandles(List<Candle> candles, int hoursOffset)
        {
            if (hoursOffset == 0)
            {
                return candles;
            }

            if (candles == null ||
                candles.Count == 0)
            {
                return candles;
            }


            List<Candle> newCandles = new List<Candle>();

            for (int i = 0; i < candles.Count; i++)
            {
                Candle curCandle = candles[i];

                Candle newCandle = new Candle();
                newCandle.Low = curCandle.Low;
                newCandle.High = curCandle.High;
                newCandle.Open = curCandle.Open;
                newCandle.Close = curCandle.Close;
                newCandle.Volume = curCandle.Volume;
                newCandle.TimeStart = curCandle.TimeStart.AddHours(hoursOffset);

                newCandles.Add(newCandle);
            }

            return newCandles;
        }

        private List<Candle> GetCoeffCandles(List<Candle> candles, decimal coeff)
        {
            if (coeff == 0)
            {
                return candles;
            }

            if (candles == null ||
                candles.Count == 0)
            {
                return candles;
            }

            List<Candle> newCandles = new List<Candle>();

            for (int i = 0; i < candles.Count; i++)
            {
                Candle curCandle = candles[i];

                Candle newCandle = new Candle();
                newCandle.Low = curCandle.Low * coeff;
                newCandle.High = curCandle.High * coeff;
                newCandle.Open = curCandle.Open * coeff;
                newCandle.Close = curCandle.Close * coeff;
                newCandle.Volume = curCandle.Volume;
                newCandle.TimeStart = curCandle.TimeStart;

                newCandles.Add(newCandle);
            }

            return newCandles;
        }

        private List<Candle> GetDivision(List<Candle> candlesOne, List<Candle> candlesTwo)
        {
            List<Candle> newCandles = new List<Candle>();

            for (int i = candlesOne.Count - 1, i2 = candlesTwo.Count - 1; i > -1 && i2 > -1; i--, i2--)
            {
                Candle first = candlesOne[i];
                Candle second = candlesTwo[i2];



                if (first.TimeStart > second.TimeStart)
                { // в случае если время не равно
                    i--;
                    continue;
                }
                if (second.TimeStart > first.TimeStart)
                { // в случае если время не равно
                    i2--;
                    continue;
                }

                decimal valueOpen = Math.Round(first.Open / second.Open, 6);
                decimal valueHigh = Math.Round(first.High / second.High, 6);
                decimal valueLow = Math.Round(first.Low / second.Low, 6);
                decimal valueClose = Math.Round(first.Close / second.Close, 6);

                if (valueClose < valueLow)
                {
                    valueLow = valueClose;
                }

                if (valueOpen < valueLow)
                {
                    valueLow = valueOpen;
                }

                if (valueClose > valueHigh)
                {
                    valueHigh = valueClose;
                }
                if (valueOpen > valueHigh)
                {
                    valueHigh = valueOpen;
                }

                Candle newCandle = new Candle();
                newCandle.Open = valueOpen;
                newCandle.Low = valueLow;
                newCandle.High = valueHigh;
                newCandle.Close = valueClose;

                newCandle.Volume = 1;
                newCandle.TimeStart = first.TimeStart;

                newCandles.Add(newCandle);
            }

            if (newCandles != null &&
                newCandles.Count > 1)
            {
                newCandles.Reverse();
            }

            return newCandles;
        }

        private List<Candle> GetMult(List<Candle> candlesOne, List<Candle> candlesTwo)
        {
            List<Candle> newCandles = new List<Candle>();

            for (int i = candlesOne.Count - 1, i2 = candlesTwo.Count - 1; i > -1 && i2 > -1; i--, i2--)
            {
                Candle first = candlesOne[i];
                Candle second = candlesTwo[i2];

                if (first.TimeStart > second.TimeStart)
                { // в случае если время не равно
                    i--;
                    continue;
                }
                if (second.TimeStart > first.TimeStart)
                { // в случае если время не равно
                    i2--;
                    continue;
                }

                decimal valueOpen = Math.Round(first.Open * second.Open, 6);
                decimal valueHigh = Math.Round(first.High * second.High, 6);
                decimal valueLow = Math.Round(first.Low * second.Low, 6);
                decimal valueClose = Math.Round(first.Close * second.Close, 6);

                if (valueClose < valueLow)
                {
                    valueLow = valueClose;
                }

                if (valueOpen < valueLow)
                {
                    valueLow = valueOpen;
                }

                if (valueClose > valueHigh)
                {
                    valueHigh = valueClose;
                }
                if (valueOpen > valueHigh)
                {
                    valueHigh = valueOpen;
                }

                Candle newCandle = new Candle();
                newCandle.Open = valueOpen;
                newCandle.Low = valueLow;
                newCandle.High = valueHigh;
                newCandle.Close = valueClose;

                newCandle.Volume = 1;
                newCandle.TimeStart = first.TimeStart;

                newCandles.Add(newCandle);
            }

            if (newCandles != null &&
                newCandles.Count > 1)
            {
                newCandles.Reverse();
            }

            return newCandles;
        }

        private List<Candle> GetSubtractTheCandles(List<Candle> candlesOne, List<Candle> candlesTwo)
        {
            List<Candle> newCandles = new List<Candle>();

            for (int i = candlesOne.Count - 1, i2 = candlesTwo.Count - 1; i > -1 && i2 > -1; i--, i2--)
            {
                Candle first = candlesOne[i];
                Candle second = candlesTwo[i2];

                if (first.TimeStart > second.TimeStart)
                { // в случае если время не равно
                    i--;
                    continue;
                }
                if (second.TimeStart > first.TimeStart)
                { // в случае если время не равно
                    i2--;
                    continue;
                }

                decimal valueOpen = first.Open - second.Open;
                decimal valueHigh = first.High - second.High;
                decimal valueLow = first.Low - second.Low;
                decimal valueClose = first.Close - second.Close;

                if (SpreadDeviationType.ValueString == PairAssistantIndexDeviationType.Percent.ToString())
                {
                    valueOpen = Math.Round(valueOpen / (first.Open / 100), 5);
                    valueHigh = Math.Round(valueHigh / (first.High / 100), 5);
                    valueLow = Math.Round(valueLow / (first.Low / 100), 5);
                    valueClose = Math.Round(valueClose / (first.Close / 100), 5);
                }

                if (valueClose < valueLow)
                {
                    valueLow = valueClose;
                }

                if (valueOpen < valueLow)
                {
                    valueLow = valueOpen;
                }

                if (valueClose > valueHigh)
                {
                    valueHigh = valueClose;
                }
                if (valueOpen > valueHigh)
                {
                    valueHigh = valueOpen;
                }

                Candle newCandle = new Candle();
                newCandle.Open = valueOpen;
                newCandle.Low = valueLow;
                newCandle.High = valueHigh;
                newCandle.Close = valueClose;

                newCandle.Volume = 1;
                newCandle.TimeStart = first.TimeStart;

                newCandles.Add(newCandle);
            }

            if (newCandles != null &&
                newCandles.Count > 1)
            {
                newCandles.Reverse();
            }

            return newCandles;
        }
    }

    public class ScenarioOne
    {
        public StrategyParameterString ScenarioOneSide;

        public StrategyParameterInt WorkTimeMinutes;

        public string MessageOnStartScenario1 = "Scenario 1. \n Start?";

        PriceModule _priceModule;

        public ScenarioOne(BotPanel panel, PriceModule priceModule)
        {
            _priceModule = priceModule;

            ScenarioOneSide
            = panel.CreateParameter("Trade side. Scenario 1",
              PairAssistantTradeSide.FirstBuy_SecondSell.ToString(), new[]
            { PairAssistantTradeSide.FirstBuy_SecondSell.ToString(),
              PairAssistantTradeSide.FirstSell_SecondBuy.ToString() }, " Scenario 1 ");

            WorkTimeMinutes = panel.CreateParameter("Work time minutes. Scenario 1", 10, 1, 1, 1, " Scenario 1 ");
        }

        public bool CanOpenPositionsBySpread()
        {
            if (_priceModule.CurrentSpread() == 0)
            {
                return false;
            }


            return true;
        }

        public string GetSpreadStringToGui()
        {
            string result = "";

            result += "Cur spread: " + _priceModule.CurrentSpread();

            return result;
        }

        public TimeSpan GetWorkTimeMinutesInTimeSpan()
        {
            TimeSpan span = new TimeSpan(0, WorkTimeMinutes.ValueInt, 0);

            return span;
        }
    }

    public class ScenarioTwo
    {
        public StrategyParameterString ScenarioTwoSide;
        public StrategyParameterDecimal ScenarioTwoMaxSpreadValue;
        public StrategyParameterInt WorkTimeMinutes;
        public string MessageOnStartScenario2 = "Scenario 2. \n Start?";

        PriceModule _priceModule;

        public ScenarioTwo(BotPanel panel, PriceModule priceModule)
        {
            _priceModule = priceModule;

            ScenarioTwoSide
            = panel.CreateParameter("Trade side. Scenario 2",
            PairAssistantTradeSide.FirstBuy_SecondSell.ToString(), new[]
          { PairAssistantTradeSide.FirstBuy_SecondSell.ToString(),
            PairAssistantTradeSide.FirstSell_SecondBuy.ToString() }, " Scenario 2 ");

            ScenarioTwoMaxSpreadValue = panel.CreateParameter("Max spread value. Scenario 2", 1, 1, 1, 1m, " Scenario 2 ");

            WorkTimeMinutes = panel.CreateParameter("Work time minutes. Scenario 2", 10, 1, 1, 1, " Scenario 2 ");
        }

        public bool CanOpenPositionsBySpread()
        {
            decimal curSpread = _priceModule.CurrentSpread();

            if (curSpread == 0)
            {
                return false;
            }

            decimal maxValue = ScenarioTwoMaxSpreadValue.ValueDecimal;

            if (curSpread > maxValue)
            {
                return false;
            }

            return true;
        }

        public string GetSpreadStringToGui()
        {
            string result = "";

            result += "Cur spread: " + _priceModule.CurrentSpread() + " Max spread: " + ScenarioTwoMaxSpreadValue.ValueDecimal.ToString();

            return result;
        }

        public TimeSpan GetWorkTimeMinutesInTimeSpan()
        {
            TimeSpan span = new TimeSpan(0, WorkTimeMinutes.ValueInt, 0);

            return span;
        }
    }

    public class ScenarioThree
    {
        public StrategyParameterString ScenarioThreeSide;
        public StrategyParameterDecimal ScenarioThreeMinSpreadValue;
        public StrategyParameterInt WorkTimeMinutes;

        public string MessageOnStartScenario3 = "Scenario 3. \n Start?";

        PriceModule _priceModule;

        public ScenarioThree(BotPanel panel, PriceModule priceModule)
        {
            _priceModule = priceModule;
            ScenarioThreeSide
           = panel.CreateParameter("Trade side. Scenario 3",
           PairAssistantTradeSide.FirstBuy_SecondSell.ToString(), new[]
         { PairAssistantTradeSide.FirstBuy_SecondSell.ToString(),
            PairAssistantTradeSide.FirstSell_SecondBuy.ToString() }, " Scenario 3 ");

            ScenarioThreeMinSpreadValue = panel.CreateParameter("Min spread value. Scenario 3", 1, 1, 1, 1m, " Scenario 3 ");

            WorkTimeMinutes = panel.CreateParameter("Work time minutes. Scenario 3", 10, 1, 1, 1, " Scenario 3 ");
        }

        public bool CanOpenPositionsBySpread()
        {
            decimal curSpread = _priceModule.CurrentSpread();

            if (curSpread == 0)
            {
                return false;
            }

            decimal minValue = ScenarioThreeMinSpreadValue.ValueDecimal;

            if (curSpread < minValue)
            {
                return false;
            }

            return true;
        }

        public string GetSpreadStringToGui()
        {
            string result = "";

            result += "Min spread: " + ScenarioThreeMinSpreadValue.ValueDecimal.ToString()
                + " Cur spread: " + _priceModule.CurrentSpread();

            return result;
        }

        public TimeSpan GetWorkTimeMinutesInTimeSpan()
        {
            TimeSpan span = new TimeSpan(0, WorkTimeMinutes.ValueInt, 0);

            return span;
        }
    }

    public class ScenarioFour
    {
        public StrategyParameterString ScenarioFourSide;
        public StrategyParameterDecimal ScenarioFourMaxSpreadValue;
        public StrategyParameterDecimal ScenarioFourMinSpreadValue;
        public StrategyParameterInt WorkTimeMinutes;

        public string MessageOnStartScenario4 = "Scenario 4. \n Start?";

        PriceModule _priceModule;

        public ScenarioFour(BotPanel panel, PriceModule priceModule)
        {
            _priceModule = priceModule;

            ScenarioFourSide
            = panel.CreateParameter("Trade side. Scenario 4",
            PairAssistantTradeSide.FirstBuy_SecondSell.ToString(), new[]
          { PairAssistantTradeSide.FirstBuy_SecondSell.ToString(),
            PairAssistantTradeSide.FirstSell_SecondBuy.ToString() }, " Scenario 4 ");

            ScenarioFourMaxSpreadValue = panel.CreateParameter("Max spread value. Scenario 4", 1, 1, 1, 1m, " Scenario 4 ");
            ScenarioFourMinSpreadValue = panel.CreateParameter("Min spread value. Scenario 4", 1, 1, 1, 1m, " Scenario 4 ");

            WorkTimeMinutes = panel.CreateParameter("Work time minutes. Scenario 4", 10, 1, 1, 1, " Scenario 4 ");
        }

        public bool CanOpenPositionsBySpread()
        {
            decimal curSpread = _priceModule.CurrentSpread();

            if (curSpread == 0)
            {
                return false;
            }

            decimal minValue = ScenarioFourMinSpreadValue.ValueDecimal;

            decimal maxValue = ScenarioFourMaxSpreadValue.ValueDecimal;

            if (curSpread < minValue)
            {
                return false;
            }
            if (curSpread > maxValue)
            {
                return false;
            }

            return true;
        }

        public string GetSpreadStringToGui()
        {
            string result = "";

            result += "Min spread: " + ScenarioFourMinSpreadValue.ValueDecimal.ToString()
                + " Cur spread: " + _priceModule.CurrentSpread()
                + " Max spread: " + ScenarioFourMaxSpreadValue.ValueDecimal.ToString();

            return result;
        }

        public TimeSpan GetWorkTimeMinutesInTimeSpan()
        {
            TimeSpan span = new TimeSpan(0, WorkTimeMinutes.ValueInt, 0);

            return span;
        }
    }

    public enum NormalizationPriceByPriceSec3Type
    {
        Off,
        DivideSec1,
        DivideSec2,
        MyltiplySec1,
        MyltiplySec2,
    }

    public enum PriceOrderType
    {
        Mid,
        Offer,
        Bid
    }

    public enum PairAssistantIndexDeviationType
    {
        None,
        Absolute,
        Percent
    }

    public enum PairAssistantTradeSide
    {
        FirstBuy_SecondSell,
        FirstSell_SecondBuy,
    }

    public enum PairAssistantDeviationType
    {
        Abs,
        Percent,
    }

    public enum ScenarioType
    {
        None,
        ScenarioOne,
        ScenarioTwo,
        ScenarioThree,
        ScenarioFour
    }
}