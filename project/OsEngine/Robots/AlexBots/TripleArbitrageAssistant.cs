using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using System.Threading;
using System;
using OsEngine.Logging;
using System.Collections.Generic;
using System.Windows.Documents;
using OsEngine.Charts.CandleChart;
using WebSocket4Net.Command;
using ru.micexrts.cgate.message;

namespace OsEngine.Robots.AlexBots
{
    [Bot("TripleArbitrageAssistant")]
    public class TripleArbitrageAssistant : BotPanel
    {
        public TripleArbitrageAssistant(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            TabCreate(BotTabType.Simple);
            TabCreate(BotTabType.Simple);

            Tab1 = TabsSimple[0];
            Tab2 = TabsSimple[1];
            Tab3 = TabsSimple[2];

            PriceModule =
                new PriceModuleTripleArbitrage(this, Tab1, Tab2, Tab3);

            PriceModule.LogMessageEvent += this.SendNewLogMessage;

            // Base settings

            OrderLifeTimeSeconds = CreateParameter("Order life time seconds", 30, 1, 1, 1, " Order ");
            OrderSlippageType = CreateParameter("Orders slippage type",
              PairAssistantIndexDeviationType.None.ToString(), new[]
            { PairAssistantIndexDeviationType.Percent.ToString(),
              PairAssistantIndexDeviationType.Absolute.ToString(),
              PairAssistantIndexDeviationType.None.ToString() }, " Order ");

            OrderSlippage = CreateParameter("Orders slippage value", 0, 1, 1, 1m, " Order ");

            // Volume settings

            VolumeFinalSec1 = CreateParameter("Volume Final Sec1", 5, 1, 1, 1m, " Volume and side ");
            VolumeOneOrderSec1 = CreateParameter("Volume One Order Sec1", 1, 1, 1, 1m, " Volume and side ");
            VolumeRatioSec2 = CreateParameter("Ratio Sec 2", 1, 1, 1, 1m, " Volume and side ");
            VolumeRatioSec3 = CreateParameter("Ratio Sec 3", 1, 1, 1, 1m, " Volume and side ");

            // Trade side Settings

            TradeSideSec1 = CreateParameter("Trade side Sec 1",
             Side.None.ToString(), new[]
           {  Side.None.ToString(),
              Side.Buy.ToString(),
              Side.Sell.ToString() }, " Volume and side ");

            TradeSideSec2 = CreateParameter("Trade side Sec 2",
              Side.None.ToString(), new[]
            {  Side.None.ToString(),
              Side.Buy.ToString(),
              Side.Sell.ToString() }, " Volume and side ");

            TradeSideSec3 = CreateParameter("Trade side Sec 3",
              Side.None.ToString(), new[]
            {  Side.None.ToString(),
              Side.Buy.ToString(),
              Side.Sell.ToString() }, " Volume and side ");

            // Scenario creation

            ScenarioOne = new ScenarioOneTriple(this, PriceModule);
            ScenarioTwo = new ScenarioTwoTriple(this, PriceModule);
            ScenarioThree = new ScenarioThreeTriple(this, PriceModule);
            ScenarioFour = new ScenarioFourTriple(this, PriceModule);

            this.ParamGuiSettings.Height = 600;
            this.ParamGuiSettings.Width = 800;

            Thread worker = new Thread(WorkerPlace);
            worker.Start();

            Thread worker2 = new Thread(UpdatePriceModuleThread);
            worker2.Start();

            this.ParametrsChangeByUser += TripleArbitrageAssistant_ParametrsChangeByUser;
        }

        public override string GetNameStrategyType()
        {
            return "TripleArbitrageAssistant";
        }

        public override void ShowIndividualSettingsDialog()
        {



        }

        public PriceModuleTripleArbitrage PriceModule;

        public BotTabSimple Tab1;

        public BotTabSimple Tab2;

        public BotTabSimple Tab3;

        // Chart

        CandleChartUiTriple _chartUi;

        public void ShowChartTripleDialog()
        {
            if (_chartUi == null)
            {
                _chartUi = new CandleChartUiTriple(this.NameStrategyUniq, StartProgram.IsOsTrader, this);
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

        // Volume

        public StrategyParameterDecimal VolumeFinalSec1;

        public StrategyParameterDecimal VolumeOneOrderSec1;

        public StrategyParameterDecimal VolumeRatioSec2;

        public StrategyParameterDecimal VolumeRatioSec3;

        // Other

        public StrategyParameterInt OrderLifeTimeSeconds;

        public StrategyParameterString OrderSlippageType;

        public StrategyParameterDecimal OrderSlippage;

        // Trade Side

        public StrategyParameterString TradeSideSec1;

        public StrategyParameterString TradeSideSec2;

        public StrategyParameterString TradeSideSec3;

        // Scenario 1
        public ScenarioOneTriple ScenarioOne;

        // Scenario 2
        public ScenarioTwoTriple ScenarioTwo;

        // Scenario 3
        public ScenarioThreeTriple ScenarioThree;

        // Scenario 4
        public ScenarioFourTriple ScenarioFour;

        // Geters to interface

        public string GetEstimateQty()
        {
            if (TradeSideSec1.ValueString != Side.None.ToString()
                && Tab1.Securiti == null)
            {
                return "Security 1 is note active";
            }

            if (TradeSideSec2.ValueString != Side.None.ToString()
                && Tab2.Securiti == null)
            {
                return "Security 2 is note active";
            }

            if (TradeSideSec3.ValueString != Side.None.ToString()
                && Tab3.Securiti == null)
            {
                return "Security 3 is note active";
            }

            string result = "";

            if (TradeSideSec1.ValueString != Side.None.ToString())
            {
                decimal volumeAllSec1 = Math.Round(VolumeFinalSec1.ValueDecimal, Tab1.Securiti.DecimalsVolume);
                decimal volumeOneOrderSec1 = Math.Round(VolumeOneOrderSec1.ValueDecimal, Tab1.Securiti.DecimalsVolume);

                result += "Sec1 " + TradeSideSec1.ValueString + " " + volumeAllSec1 + " / " + volumeOneOrderSec1;
            }

            if (TradeSideSec2.ValueString != Side.None.ToString())
            {
                decimal volumeAllSec2 = Math.Round(VolumeFinalSec1.ValueDecimal * VolumeRatioSec2.ValueDecimal, Tab2.Securiti.DecimalsVolume);
                decimal volumeOneOrderSec2 = Math.Round(VolumeOneOrderSec1.ValueDecimal * VolumeRatioSec2.ValueDecimal, Tab2.Securiti.DecimalsVolume);

                result += " | Sec2 " + TradeSideSec2.ValueString + " " + volumeAllSec2
                    + " / " + volumeOneOrderSec2;
            }

            if (TradeSideSec3.ValueString != Side.None.ToString())
            {
                decimal volumeAllSec3 = Math.Round(VolumeFinalSec1.ValueDecimal * VolumeRatioSec3.ValueDecimal, Tab3.Securiti.DecimalsVolume);
                decimal volumeOneOrderSec3 = Math.Round(VolumeOneOrderSec1.ValueDecimal * VolumeRatioSec3.ValueDecimal, Tab3.Securiti.DecimalsVolume);

                result += " | Sec3 " + TradeSideSec3.ValueString + " " + volumeAllSec3
                    + " / " + volumeOneOrderSec3;
            }

            if (result == "")
            {
                result = "Volume or side note set";
            }

            return result;
        }

        public string GetScenarioInWorkString()
        {
            return ScenarioInWork.ToString();
        }

        public string GetScenarioSelectedInGuiString()
        {
            return SelectedScenario.ToString();
        }

        public string GetSpreadValuesString()
        {
            if (SelectedScenario == ScenarioTypeTriple.None)
            {
                return "None";
            }

            string spread = "";

            if (SelectedScenario == ScenarioTypeTriple.ScenarioOne)
            {
                spread = ScenarioOne.GetSpreadStringToGui();
            }
            if (SelectedScenario == ScenarioTypeTriple.ScenarioTwo)
            {
                spread = ScenarioTwo.GetSpreadStringToGui();
            }
            if (SelectedScenario == ScenarioTypeTriple.ScenarioThree)
            {
                spread = ScenarioThree.GetSpreadStringToGui();
            }
            if (SelectedScenario == ScenarioTypeTriple.ScenarioFour)
            {
                spread = ScenarioFour.GetSpreadStringToGui();
            }

            return spread;
        }

        public string GetSpreadStateString()
        {

            if (SelectedScenario == ScenarioTypeTriple.None)
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

        public string GetTimerValue()
        {
            if (ScenarioInWork == ScenarioTypeTriple.None &&
                SelectedScenario == ScenarioTypeTriple.None)
            {
                return "None";
            }
            else if (ScenarioInWork == ScenarioTypeTriple.None &&
              SelectedScenario != ScenarioTypeTriple.None)
            {
                string result = "left time: ";

                if (SelectedScenario == ScenarioTypeTriple.ScenarioOne)
                {
                    result += ScenarioOne.GetWorkTimeMinutesInTimeSpan().ToString();
                    return result;
                }
                else if (SelectedScenario == ScenarioTypeTriple.ScenarioTwo)
                {
                    result += ScenarioTwo.GetWorkTimeMinutesInTimeSpan().ToString();
                    return result;
                }
                else if (SelectedScenario == ScenarioTypeTriple.ScenarioThree)
                {
                    result += ScenarioThree.GetWorkTimeMinutesInTimeSpan().ToString();
                    return result;
                }
                else if (SelectedScenario == ScenarioTypeTriple.ScenarioFour)
                {
                    result += ScenarioFour.GetWorkTimeMinutesInTimeSpan().ToString();
                    return result;
                }
                else { return "None"; }
            }
            else if (ScenarioInWork != ScenarioTypeTriple.None)
            {
                string result = "left time: ";

                TimeSpan timeInWork = DateTime.Now - _timeStartWork;

                if (ScenarioInWork == ScenarioTypeTriple.ScenarioOne)
                {
                    timeInWork = timeInWork.Add(new TimeSpan(0, -ScenarioOne.WorkTimeMinutes.ValueInt, 0));
                    result += timeInWork.ToString();
                    return result;
                }
                else if (ScenarioInWork == ScenarioTypeTriple.ScenarioTwo)
                {
                    timeInWork = timeInWork.Add(new TimeSpan(0, -ScenarioTwo.WorkTimeMinutes.ValueInt, 0));
                    result += timeInWork.ToString();
                    return result;
                }
                else if (ScenarioInWork == ScenarioTypeTriple.ScenarioThree)
                {
                    timeInWork = timeInWork.Add(new TimeSpan(0, -ScenarioThree.WorkTimeMinutes.ValueInt, 0));
                    result += timeInWork.ToString();
                    return result;
                }
                else if (ScenarioInWork == ScenarioTypeTriple.ScenarioFour)
                {
                    timeInWork = timeInWork.Add(new TimeSpan(0, -ScenarioFour.WorkTimeMinutes.ValueInt, 0));
                    result += timeInWork.ToString();
                    return result;
                }

                else { return "None"; }
            }

            return "";
        }

        public string GetCurrentQty()
        {
            if (Tab1.IsConnected == false)
            {
                return "None";
            }

            if (ScenarioInWork == ScenarioTypeTriple.None)
            {
                return "None";
            }

            string result = "";

            //TradeSideSec1;
            //TradeSideSec2;
            //TradeSideSec3;

            if (TradeSideSec1.ValueString == Side.Buy.ToString())
            {
                result += "Sec 1 + " + GetVolumeExecute(Tab1);
            }
            else if (TradeSideSec1.ValueString == Side.Sell.ToString())
            {
                result += "Sec 1 - " + GetVolumeExecute(Tab1);
            }

            if (TradeSideSec2.ValueString == Side.Buy.ToString())
            {
                result += " Sec 2 + " + GetVolumeExecute(Tab2);
            }
            else if (TradeSideSec2.ValueString == Side.Sell.ToString())
            {
                result += " Sec 2 - " + GetVolumeExecute(Tab2);
            }

            if (TradeSideSec3.ValueString == Side.Buy.ToString())
            {
                result += " Sec 3 + " + GetVolumeExecute(Tab3);
            }
            else if (TradeSideSec3.ValueString == Side.Sell.ToString())
            {
                result += " Sec 3 - " + GetVolumeExecute(Tab3);
            }

            return result;
        }

        // Table volume creation

        public VolumeTable VolumeTable = new VolumeTable();

        public void CreateVolumeTable()
        {
            if (ScenarioInWork != ScenarioTypeTriple.None)
            {
                //SendNewLogMessage("Расчёт объёма во время работы не возможен", LogMessageType.Error);
                return;
            }

            // очищаем старую таблицу с объёмами

            VolumeTable.Clear();
            VolumeTable.IsEnded = false;

            VolumeTable newTable = new VolumeTable();

            if (TradeSideSec1.ValueString == Side.None.ToString())
            {
                //SendNewLogMessage("У бумаги 1 не выставлена сторона входа. Объём расчитать не выйдет", LogMessageType.Error);
                return;
            }

            if (TradeSideSec1.ValueString != Side.None.ToString()
                            && Tab1.Securiti == null)
            {
                //SendNewLogMessage("У бумаги 1 не подключен инструмент. Объём расчитать не выйдет", LogMessageType.Error);
                return;
            }

            if (TradeSideSec2.ValueString != Side.None.ToString()
                && Tab2.Securiti == null)
            {
                //SendNewLogMessage("У бумаги 2 не подключен инструмент. Объём расчитать не выйдет", LogMessageType.Error);
                return;
            }

            if (TradeSideSec3.ValueString != Side.None.ToString()
                && Tab3.Securiti == null)
            {
                //SendNewLogMessage("У бумаги 3 не подключен инструмент. Объём расчитать не выйдет", LogMessageType.Error);
                return;
            }

            // рассчитываем итоговый объём

            VolumeRow RowEndVolume = new VolumeRow();

            if (TradeSideSec1.ValueString != Side.None.ToString())
            {
                decimal volumeAllSec1 = Math.Round(VolumeFinalSec1.ValueDecimal, Tab1.Securiti.DecimalsVolume);

                VolumeCell endVolumeCellSec1 = new VolumeCell();
                endVolumeCellSec1.VolumeToOpen = volumeAllSec1;
                endVolumeCellSec1.IsEnded = false;
                endVolumeCellSec1.SecurityTab = Tab1;
                endVolumeCellSec1.Side = GetSideFromString(TradeSideSec1.ValueString);
                RowEndVolume.VolumeCells.Add(endVolumeCellSec1);
            }

            if (TradeSideSec2.ValueString != Side.None.ToString())
            {
                decimal volumeAllSec2 = Math.Round(VolumeFinalSec1.ValueDecimal * VolumeRatioSec2.ValueDecimal, Tab2.Securiti.DecimalsVolume);

                VolumeCell endVolumeCellSec2 = new VolumeCell();
                endVolumeCellSec2.VolumeToOpen = volumeAllSec2;
                endVolumeCellSec2.IsEnded = false;
                endVolumeCellSec2.SecurityTab = Tab2;
                endVolumeCellSec2.Side = GetSideFromString(TradeSideSec2.ValueString);
                RowEndVolume.VolumeCells.Add(endVolumeCellSec2);
            }

            if (TradeSideSec3.ValueString != Side.None.ToString())
            {
                decimal volumeAllSec3 = Math.Round(VolumeFinalSec1.ValueDecimal * VolumeRatioSec3.ValueDecimal, Tab3.Securiti.DecimalsVolume);

                VolumeCell endVolumeCellSec3 = new VolumeCell();
                endVolumeCellSec3.VolumeToOpen = volumeAllSec3;
                endVolumeCellSec3.IsEnded = false;
                endVolumeCellSec3.SecurityTab = Tab3;
                endVolumeCellSec3.Side = GetSideFromString(TradeSideSec3.ValueString);
                RowEndVolume.VolumeCells.Add(endVolumeCellSec3);
            }

            VolumeTable.RowEndVolume = RowEndVolume;

            // рассчитываем последовательности

            List<VolumeRow> RowsVolumeStep = new List<VolumeRow>();
            VolumeTable.RowsAllToOpen = RowsVolumeStep;

            if (TradeSideSec1.ValueString != Side.None.ToString())
            {
                decimal volumeAllSec1 = Math.Round(VolumeFinalSec1.ValueDecimal, Tab1.Securiti.DecimalsVolume);
                decimal volumeOneOrderSec1 = Math.Round(VolumeOneOrderSec1.ValueDecimal, Tab1.Securiti.DecimalsVolume);

                decimal allVolumeInRows = 0;

                while (allVolumeInRows < volumeAllSec1)
                {
                    VolumeRow CurRowVolume = new VolumeRow();

                    VolumeCell curVolumeCell = new VolumeCell();
                    curVolumeCell.IsEnded = false;
                    curVolumeCell.SecurityTab = Tab1;
                    curVolumeCell.Side = GetSideFromString(TradeSideSec1.ValueString);

                    decimal volumeOnLevel = volumeOneOrderSec1;

                    if ((volumeOnLevel + allVolumeInRows) > volumeAllSec1)
                    {
                        volumeOnLevel = volumeAllSec1 - allVolumeInRows;
                    }

                    if (volumeOnLevel > 0)
                    {
                        curVolumeCell.VolumeToOpen = volumeOnLevel;
                        CurRowVolume.VolumeCells.Add(curVolumeCell);
                        RowsVolumeStep.Add(CurRowVolume);
                    }
                    allVolumeInRows += volumeOnLevel;
                }

                decimal allVolumeInTables = VolumeTable.GetVolumeFromRows(Tab1.Securiti.Name);

                if (allVolumeInTables < volumeAllSec1)
                {
                    decimal lostVol = volumeAllSec1 - allVolumeInTables;

                    VolumeRow CurRowVolume = new VolumeRow();

                    VolumeCell curVolumeCell = new VolumeCell();
                    curVolumeCell.IsEnded = false;
                    curVolumeCell.SecurityTab = Tab1;
                    curVolumeCell.Side = GetSideFromString(TradeSideSec1.ValueString);
                    curVolumeCell.VolumeToOpen = lostVol;
                    CurRowVolume.VolumeCells.Add(curVolumeCell);
                    RowsVolumeStep.Add(CurRowVolume);
                }
            }

            if (TradeSideSec2.ValueString != Side.None.ToString())
            {
                decimal volumeAllSec2 = Math.Round(VolumeFinalSec1.ValueDecimal * VolumeRatioSec2.ValueDecimal, Tab2.Securiti.DecimalsVolume);
                decimal volumeOneOrderSec2 = Math.Round(VolumeOneOrderSec1.ValueDecimal * VolumeRatioSec2.ValueDecimal, Tab2.Securiti.DecimalsVolume);

                decimal allVolumeInRows = 0;
                int rowIndex = 0;

                while (allVolumeInRows < volumeAllSec2)
                {
                    if (rowIndex >= RowsVolumeStep.Count)
                    {
                        break;
                    }
                    VolumeRow CurRowVolume = RowsVolumeStep[rowIndex];
                    rowIndex++;

                    VolumeCell curVolumeCell = new VolumeCell();
                    curVolumeCell.IsEnded = false;
                    curVolumeCell.SecurityTab = Tab2;
                    curVolumeCell.Side = GetSideFromString(TradeSideSec2.ValueString);

                    decimal volumeOnLevel = volumeOneOrderSec2;

                    if ((volumeOnLevel + allVolumeInRows) > volumeAllSec2)
                    {
                        volumeOnLevel = volumeAllSec2 - allVolumeInRows;
                    }

                    if (volumeOnLevel > 0)
                    {
                        curVolumeCell.VolumeToOpen = volumeOnLevel;
                        CurRowVolume.VolumeCells.Add(curVolumeCell);
                    }
                    allVolumeInRows += volumeOnLevel;
                }

                decimal allVolumeInTables = VolumeTable.GetVolumeFromRows(Tab2.Securiti.Name);

                if (allVolumeInTables < volumeAllSec2)
                {
                    decimal lostVol = volumeAllSec2 - allVolumeInTables;

                    VolumeRow CurRowVolume = new VolumeRow();

                    VolumeCell curVolumeCell = new VolumeCell();
                    curVolumeCell.IsEnded = false;
                    curVolumeCell.SecurityTab = Tab2;
                    curVolumeCell.Side = GetSideFromString(TradeSideSec2.ValueString);
                    curVolumeCell.VolumeToOpen = lostVol;
                    CurRowVolume.VolumeCells.Add(curVolumeCell);
                    RowsVolumeStep.Add(CurRowVolume);
                }
            }

            if (TradeSideSec3.ValueString != Side.None.ToString())
            {
                decimal volumeAllSec3 = Math.Round(VolumeFinalSec1.ValueDecimal * VolumeRatioSec3.ValueDecimal, Tab3.Securiti.DecimalsVolume);
                decimal volumeOneOrderSec3 = Math.Round(VolumeOneOrderSec1.ValueDecimal * VolumeRatioSec3.ValueDecimal, Tab3.Securiti.DecimalsVolume);

                decimal allVolumeInRows = 0;
                int rowIndex = 0;

                while (allVolumeInRows < volumeAllSec3)
                {
                    if (rowIndex >= RowsVolumeStep.Count)
                    {
                        break;
                    }
                    VolumeRow CurRowVolume = RowsVolumeStep[rowIndex];
                    rowIndex++;

                    VolumeCell curVolumeCell = new VolumeCell();
                    curVolumeCell.IsEnded = false;
                    curVolumeCell.SecurityTab = Tab3;
                    curVolumeCell.Side = GetSideFromString(TradeSideSec3.ValueString);

                    decimal volumeOnLevel = volumeOneOrderSec3;

                    if ((volumeOnLevel + allVolumeInRows) > volumeAllSec3)
                    {
                        volumeOnLevel = volumeAllSec3 - allVolumeInRows;
                    }

                    if (volumeOnLevel > 0)
                    {
                        curVolumeCell.VolumeToOpen = volumeOnLevel;
                        CurRowVolume.VolumeCells.Add(curVolumeCell);
                    }
                    allVolumeInRows += volumeOnLevel;
                }

                decimal allVolumeInTables = VolumeTable.GetVolumeFromRows(Tab3.Securiti.Name);

                if (allVolumeInTables < volumeAllSec3)
                {
                    decimal lostVol = volumeAllSec3 - allVolumeInTables;

                    VolumeRow CurRowVolume = new VolumeRow();

                    VolumeCell curVolumeCell = new VolumeCell();
                    curVolumeCell.IsEnded = false;
                    curVolumeCell.SecurityTab = Tab3;
                    curVolumeCell.Side = GetSideFromString(TradeSideSec3.ValueString);
                    curVolumeCell.VolumeToOpen = lostVol;
                    CurRowVolume.VolumeCells.Add(curVolumeCell);
                    RowsVolumeStep.Add(CurRowVolume);
                }

            }
        }

        public string GetStringVolumes()
        {
            string result = "Volumes step \n";

            result += VolumeTable.RowEndVolume.GetString() + " \n";

            result += "\n";

            for (int i = 0; i < VolumeTable.RowsAllToOpen.Count; i++)
            {
                result += VolumeTable.RowsAllToOpen[i].GetString() + " \n";
            }

            return result;
        }

        public bool NeadToUpdeateVolume = false;

        private void TripleArbitrageAssistant_ParametrsChangeByUser()
        {
            try
            {
                NeadToUpdeateVolume = true;

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

        // start or stop Scenario

        public void SetScenarioInWork(ScenarioTypeTriple startedScenario)
        {
            if (startedScenario == ScenarioTypeTriple.None)
            {
                SendNewLogMessage("Сценарий не выбран", LogMessageType.Error);
                return;
            }

            // 1 проверяем активацию бумаг

            if (TradeSideSec1.ValueString == Side.None.ToString())
            {
                SendNewLogMessage("У бумаги 1 не выставлена сторона входа", LogMessageType.Error);
                return;
            }

            if (TradeSideSec1.ValueString != Side.None.ToString()
                            && Tab1.Securiti == null)
            {
                SendNewLogMessage("У бумаги 1 не подключен инструмент", LogMessageType.Error);
                return;
            }

            if (TradeSideSec2.ValueString != Side.None.ToString()
                && Tab2.Securiti == null)
            {
                SendNewLogMessage("У бумаги 2 не подключен инструмент", LogMessageType.Error);
                return;
            }

            if (TradeSideSec3.ValueString != Side.None.ToString()
                && Tab3.Securiti == null)
            {
                SendNewLogMessage("У бумаги 3 не подключен инструмент", LogMessageType.Error);
                return;
            }

            // 2 проверяем есть ли портфели у торгуемых бумаг

            if (TradeSideSec1.ValueString != Side.None.ToString()
                && Tab1.Portfolio == null)
            {
                SendNewLogMessage("У бумаги 1 не подключен портфель", LogMessageType.Error);
                return;
            }

            if (TradeSideSec2.ValueString != Side.None.ToString()
                && Tab2.Portfolio == null)
            {
                SendNewLogMessage("У бумаги 2 не подключен портфель", LogMessageType.Error);
                return;
            }

            if (TradeSideSec3.ValueString != Side.None.ToString()
                && Tab3.Portfolio == null)
            {
                SendNewLogMessage("У бумаги 3 не подключен портфель", LogMessageType.Error);
                return;
            }

            // 2 проверяем готовность торгуемых бумаг к торгам

            if (TradeSideSec1.ValueString != Side.None.ToString()
                && (Tab1.IsConnected == false || Tab1.IsReadyToTrade == false))
            {
                SendNewLogMessage("Бумага 1 ещё не готова к торгам", LogMessageType.Error);
                return;
            }

            if (TradeSideSec2.ValueString != Side.None.ToString()
                && (Tab2.IsConnected == false || Tab2.IsReadyToTrade == false))
            {
                SendNewLogMessage("Бумага 2 ещё не готова к торгам", LogMessageType.Error);
                return;
            }

            if (TradeSideSec3.ValueString != Side.None.ToString()
                && (Tab3.IsConnected == false || Tab3.IsReadyToTrade == false))
            {
                SendNewLogMessage("Бумага 3 ещё не готова к торгам", LogMessageType.Error);
                return;
            }

            SelectedScenario = startedScenario;

            if (_threadIsWork == true)
            {
                TabsSimple[0].SetNewLogMessage("Предыдущий сценарий ещё в работе", Logging.LogMessageType.Error);
                return;
            }

            if (ScenarioInWork != ScenarioTypeTriple.None)
            {
                TabsSimple[0].SetNewLogMessage("Предыдущий сценарий ещё в работе", Logging.LogMessageType.Error);
                return;
            }

            if (SelectedScenario == ScenarioTypeTriple.None)
            {
                TabsSimple[0].SetNewLogMessage("Сценарий не выбран", Logging.LogMessageType.Error);
                return;
            }

            if (VolumeTable.RowEndVolume == null ||
                VolumeTable.RowEndVolume.VolumeCells == null ||
                VolumeTable.RowEndVolume.VolumeCells.Count == 0 ||
                VolumeTable.RowsAllToOpen == null ||
                VolumeTable.RowsAllToOpen.Count == 0)
            {
                TabsSimple[0].SetNewLogMessage("Объёмы не сформированы!", Logging.LogMessageType.Error);
                return;
            }

            string message = "Are you sure you want to start a new script?. \r";
            message += "Scenario: " + startedScenario + "\r";
            message += "Old transaction data in the robot will be erased. \r";
            message += "End volume: " + VolumeTable.RowEndVolume.GetString();

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
            if (ScenarioInWork == ScenarioTypeTriple.None)
            {
                TabsSimple[0].SetNewLogMessage("Can`t stop scenario. No scenario in work", Logging.LogMessageType.Error);
                return;
            }

            TabsSimple[0].SetNewLogMessage("Stop scenario by User click", Logging.LogMessageType.Error);
            _needToStop = true;
        }

        public void PauseScenarioInWork()
        {
            if (ScenarioInWork == ScenarioTypeTriple.None)
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

        // logic

        public ScenarioTypeTriple ScenarioInWork;

        public ScenarioTypeTriple SelectedScenario;

        DateTime _timeStartWork;

        private bool _threadIsWork;

        private bool _needToStop;

        public bool CanTradeByCurrentScenario()
        {
            bool canSetOrders = false;

            if (SelectedScenario == ScenarioTypeTriple.None)
            {
                return false;
            }

            if (SelectedScenario == ScenarioTypeTriple.ScenarioOne)
            {
                canSetOrders = ScenarioOne.CanOpenPositionsBySpread();
            }
            if (SelectedScenario == ScenarioTypeTriple.ScenarioTwo)
            {
                canSetOrders = ScenarioTwo.CanOpenPositionsBySpread();
            }
            if (SelectedScenario == ScenarioTypeTriple.ScenarioThree)
            {
                canSetOrders = ScenarioThree.CanOpenPositionsBySpread();
            }
            if (SelectedScenario == ScenarioTypeTriple.ScenarioFour)
            {
                canSetOrders = ScenarioFour.CanOpenPositionsBySpread();
            }

            return canSetOrders;
        }

        private void UpdatePriceModuleThread()
        {
            while (true)
            {
                Thread.Sleep(500);

                try
                {
                    PriceModule.Process();

                    if (NeadToUpdeateVolume == true)
                    {
                        NeadToUpdeateVolume = false;
                        CreateVolumeTable();
                    }
                }
                catch (Exception e)
                {
                    SendNewLogMessage(e.ToString(), LogMessageType.Error);
                    Thread.Sleep(10000);
                }
            }
        }


        private void WorkerPlace()
        {
            while (true)
            {
                Thread.Sleep(500);

                try
                {
                    if (ScenarioInWork != ScenarioTypeTriple.None)
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
                for (int i = 0; i < VolumeTable.RowsAllToOpen.Count; i++)
                {
                    for (int j = 0; j < VolumeTable.RowsAllToOpen[i].VolumeCells.Count; j++)
                    {
                        VolumeTable.RowsAllToOpen[i].VolumeCells[j].IsEnded = false;
                    }
                }

                if (ScenarioInWork == ScenarioTypeTriple.ScenarioOne)
                {
                    ExecutionPlace(ScenarioOne.WorkTimeMinutes,
                                   PriceModule.PriceInMarketDepthSecOne,
                                   PriceModule.PriceInMarketDepthSecTwo,
                                   PriceModule.PriceInMarketDepthSecThree,
                                   VolumeTable);
                }
                else if (ScenarioInWork == ScenarioTypeTriple.ScenarioTwo)
                {
                    ExecutionPlace(ScenarioTwo.WorkTimeMinutes,
                                   PriceModule.PriceInMarketDepthSecOne,
                                   PriceModule.PriceInMarketDepthSecTwo,
                                    PriceModule.PriceInMarketDepthSecThree,
                                   VolumeTable);
                }
                else if (ScenarioInWork == ScenarioTypeTriple.ScenarioThree)
                {
                    ExecutionPlace(ScenarioThree.WorkTimeMinutes,
                                   PriceModule.PriceInMarketDepthSecOne,
                                   PriceModule.PriceInMarketDepthSecTwo,
                                   PriceModule.PriceInMarketDepthSecThree,
                                   VolumeTable);
                }
                else if (ScenarioInWork == ScenarioTypeTriple.ScenarioFour)
                {
                    ExecutionPlace(ScenarioFour.WorkTimeMinutes,
                                   PriceModule.PriceInMarketDepthSecOne,
                                   PriceModule.PriceInMarketDepthSecTwo,
                                   PriceModule.PriceInMarketDepthSecThree,
                                   VolumeTable);
                }


            }
            catch (Exception ex)
            {
                TabsSimple[0].SetNewLogMessage("Error on execution: " + ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void ExecutionPlace(
    StrategyParameterInt minutesToWork,
    StrategyParameterString priceToOrderInMarketDepthSecOne,
    StrategyParameterString priceToOrderInMarketDepthSecTwo,
    StrategyParameterString priceToOrderInMarketDepthSecThree,
    VolumeTable table)
        {
            _threadIsWork = true;
            _needToStop = false;
            _timeStartWork = DateTime.Now;

            ClearPositions(Tab1);
            ClearPositions(Tab2);
            ClearPositions(Tab3);

            for (int i = 0; i < table.RowsAllToOpen.Count; i++)
            {
                Thread.Sleep(1000);

                if (_timeStartWork.AddMinutes(minutesToWork.ValueInt) < DateTime.Now)
                {
                    _threadIsWork = false;
                    _needToStop = false;
                    ScenarioInWork = ScenarioTypeTriple.None;
                    SendNewLogMessage(this.NameStrategyUniq + " Завершение сценария по времени", LogMessageType.Error);
                    return;
                }

                if (CanTradeByCurrentScenario() == false)
                {
                    i--;
                    continue;
                }

                VolumeRow curRow = table.RowsAllToOpen[i];

                ExecuteRow(minutesToWork,
                    priceToOrderInMarketDepthSecOne,
                    priceToOrderInMarketDepthSecTwo,
                    priceToOrderInMarketDepthSecThree,
                    curRow);

                if (_needToStop == true)
                {
                    _threadIsWork = false;
                    _needToStop = false;
                    ScenarioInWork = ScenarioTypeTriple.None;
                    return;
                }
            }


            SendNewLogMessage("Сценарий завешрён штатно. " + VolumeTable.RowEndVolume.GetString(), LogMessageType.Error);
            _threadIsWork = false;
            _needToStop = false;
            ScenarioInWork = ScenarioTypeTriple.None;
            return;
        }

        private void ExecuteRow(StrategyParameterInt minutesToWork,
    StrategyParameterString priceToOrderInMarketDepthSecOne,
    StrategyParameterString priceToOrderInMarketDepthSecTwo,
    StrategyParameterString priceToOrderInMarketDepthSecThree,
    VolumeRow row)
        {
            for (int i = 0; i < row.VolumeCells.Count; i++)
            {
                VolumeCell curCell = row.VolumeCells[i];

                ExecuteCell(minutesToWork,
                    priceToOrderInMarketDepthSecOne,
                    priceToOrderInMarketDepthSecTwo,
                    priceToOrderInMarketDepthSecThree,
                    curCell);

                if (_needToStop == true)
                {
                    return;
                }
            }
        }

        private void ExecuteCell(StrategyParameterInt minutesToWork,
    StrategyParameterString priceToOrderInMarketDepthSecOne,
    StrategyParameterString priceToOrderInMarketDepthSecTwo,
    StrategyParameterString priceToOrderInMarketDepthSecThree,
    VolumeCell cell)
        {

            List<Position> myPoses = new List<Position>();

            DateTime lastOrderTime = DateTime.MinValue;

            Position lastPosition = null;

            while (true)
            {
                Thread.Sleep(1000);

                if (_needToStop == true)
                {
                    return;
                }

                if (IsOnPause)
                {
                    continue;
                }

                if (Tab1.ServerStatus == Market.Servers.ServerConnectStatus.Disconnect)
                {
                    continue;
                }

                if (TradeSideSec1.ValueString != Side.None.ToString()
                && (Tab1.IsConnected == false || Tab1.IsReadyToTrade == false))
                {
                    continue;
                }

                if (TradeSideSec2.ValueString != Side.None.ToString()
                    && (Tab2.IsConnected == false || Tab2.IsReadyToTrade == false))
                {
                    continue;
                }

                if (TradeSideSec3.ValueString != Side.None.ToString()
                    && (Tab3.IsConnected == false || Tab3.IsReadyToTrade == false))
                {
                    continue;
                }

                // 1 проверяем завершение по времени

                if (_timeStartWork.AddMinutes(minutesToWork.ValueInt) < DateTime.Now)
                {
                    CanselOrders(cell.SecurityTab);
                    _needToStop = true;
                    SendNewLogMessage(this.NameStrategyUniq + " Завершение сценария по времени", LogMessageType.Error);
                    return;
                }

                // 2 проверяем завершённость ячейки. Если набрали объёмы

                decimal executeVolume = GetExecuteVolumes(myPoses);

                if (executeVolume == cell.VolumeToOpen)
                {
                    cell.IsEnded = true;
                    return;
                }

                bool haveOrdersInMarket = HaveOpenOrdersByPositions(myPoses);

                if (haveOrdersInMarket == false)
                {
                    // 3 если ордеров в рынке по вкладке нет, и объёмы не набраны текущие, выставляем

                    decimal volumeToEntry = cell.VolumeToOpen - executeVolume;

                    decimal priceToEntry = GetPriceToEntry(cell.SecurityTab,
                        priceToOrderInMarketDepthSecOne,
                        priceToOrderInMarketDepthSecTwo,
                        priceToOrderInMarketDepthSecThree,
                        cell.Side);

                    if (priceToEntry == 0)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    lastOrderTime = DateTime.Now;

                    if (cell.Side == Side.Buy)
                    {
                        Position newPos = cell.SecurityTab.BuyAtLimit(volumeToEntry, priceToEntry);

                        if (newPos == null)
                        {
                            SendNewLogMessage("Позиция не выставлена. Критическая ошибка! ", LogMessageType.Error);
                            Thread.Sleep(5000);
                            continue;
                        }

                        myPoses.Add(newPos);
                        lastPosition = newPos;
                    }
                    if (cell.Side == Side.Sell)
                    {
                        Position newPos = cell.SecurityTab.SellAtLimit(volumeToEntry, priceToEntry);

                        if (newPos == null)
                        {
                            SendNewLogMessage("Позиция не выставлена. Критическая ошибка! ", LogMessageType.Error);
                            Thread.Sleep(5000);
                            continue;
                        }

                        myPoses.Add(newPos);
                        lastPosition = newPos;
                    }
                    Thread.Sleep(2000);
                }
                else //if (haveOrdersInMarket == true)
                {
                    // 4 если ордера есть, проверяем отзыв по времени

                    if (lastOrderTime.AddSeconds(OrderLifeTimeSeconds.ValueInt) < DateTime.Now)
                    {
                        cell.SecurityTab.CloseAllOrderToPosition(lastPosition);
                        Thread.Sleep(2000);
                    }
                }
            }
        }

        // helpers in trading

        private decimal GetPriceToEntry(BotTabSimple tab,
            StrategyParameterString priceToOrderInMarketDepthSecOne,
            StrategyParameterString priceToOrderInMarketDepthSecTwo,
            StrategyParameterString priceToOrderInMarketDepthSecThree,
            Side side)
        {
            /*
            public StrategyParameterInt OrderLifeTimeSeconds;
            public StrategyParameterString OrderSlippageType;
            public StrategyParameterDecimal OrderSlippage;

            PairAssistantIndexDeviationType.Percent.ToString(),
            PairAssistantIndexDeviationType.Absolute.ToString(),

            PriceOrderType.Mid.ToString(),
            PriceOrderType.Offer.ToString(),
            PriceOrderType.Bid.ToString()
            */

            string priceToEntry = "";

            if (Tab1.Securiti != null &&
                tab.Securiti.Name == Tab1.Securiti.Name)
            {
                priceToEntry = priceToOrderInMarketDepthSecOne.ValueString;
            }
            if (Tab2.Securiti != null &&
                tab.Securiti.Name == Tab2.Securiti.Name)
            {
                priceToEntry = priceToOrderInMarketDepthSecTwo.ValueString;
            }
            if (Tab3.Securiti != null &&
                tab.Securiti.Name == Tab3.Securiti.Name)
            {
                priceToEntry = priceToOrderInMarketDepthSecThree.ValueString;
            }

            if (priceToEntry == "")
            {
                return 0;
            }

            decimal basePrice = 0;

            if (priceToEntry == "Mid")
            {
                basePrice = tab.PriceCenterMarketDepth;
            }
            else if (priceToEntry == "Bid")
            {
                basePrice = tab.PriceBestBid;
            }
            else if (priceToEntry == "Offer")
            {
                basePrice = tab.PriceBestAsk;
            }

            if (OrderSlippageType.ValueString == "Percent"
                && OrderSlippage.ValueDecimal != 0)
            {
                decimal slippage = basePrice * OrderSlippage.ValueDecimal / 100;

                if (side == Side.Buy)
                {
                    basePrice -= slippage;
                }
                else if (side == Side.Sell)
                {
                    basePrice += slippage;
                }
            }
            else if (OrderSlippageType.ValueString == "Absolute"
                 && OrderSlippage.ValueDecimal != 0)
            {
                if (side == Side.Buy)
                {
                    basePrice -= OrderSlippage.ValueDecimal;
                }
                else if (side == Side.Sell)
                {
                    basePrice += OrderSlippage.ValueDecimal;
                }
            }

            return basePrice;
        }

        private bool HaveOpenOrdersByPositions(List<Position> positions)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i] == null)
                {
                    continue;
                }

                if (positions[i].OpenOrders != null &&
                    positions[i].OpenOrders[0].State == OrderStateType.Active)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearPositions(BotTabSimple tab)
        {
            List<Position> positions = tab.PositionsAll;

            for (int i = 0; i < positions.Count; i++)
            {
                tab._journal.DeletePosition(positions[i]);
                i--;
            }
        }

        private decimal GetExecuteVolumes(List<Position> positions)
        {
            decimal volume = 0;
            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i] == null)
                {
                    continue;
                }

                if (positions[i].State == PositionStateType.Open ||
                    positions[i].State == PositionStateType.Opening)
                {
                    volume += positions[i].OpenVolume;

                }
            }
            return volume;
        }

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

        private Side GetSideFromString(string side)
        {
            Side result = Side.None;

            Enum.TryParse(side, out result);

            return result;
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
    }

    public class PriceModuleTripleArbitrage
    {
        public PriceModuleTripleArbitrage(
             BotPanel panel,
             BotTabSimple tab1,
             BotTabSimple tab2,
             BotTabSimple tab3)
        {
            _tab1 = tab1;
            _tab2 = tab2;
            _tab3 = tab3;

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

            PriceReverseQuoteSecOne = panel.CreateParameter("Revers quote. Sec1", false, " Price ");
            PriceReverseQuoteSecTwo = panel.CreateParameter("Revers quote. Sec2", false, " Price ");
            PriceReverseQuoteSecThree = panel.CreateParameter("Revers quote. Sec3", false, " Price ");

            HoursOffsetSecOne = panel.CreateParameter("Hours offset. Sec 1", 0, 1, 1, 1, " Price ");
            HoursOffsetSecTwo = panel.CreateParameter("Hours offset. Sec 2", 0, 1, 1, 1, " Price ");
            HoursOffsetSecThree = panel.CreateParameter("Hours offset. Sec 3", 0, 1, 1, 1, " Price ");

            PriceKoeffSec1 = panel.CreateParameter("Price koeff. Sec 1", 1, 1, 1, 1m, " Price ");
            PriceKoeffSec2 = panel.CreateParameter("Price koeff. Sec 2", 1, 1, 1, 1m, " Price ");
            PriceKoeffSec3 = panel.CreateParameter("Price koeff. Sec 3", 1, 1, 1, 1m, " Price ");
            DifferenceConst = panel.CreateParameter("Difference const", 0, 0, 0, 1m, " Price ");

            DifferenceType
                = panel.CreateParameter("Difference type", NormalizationPriceByPriceSec3Type.Off.ToString(), new[]
                { DifferenceTypeEnum.Off.ToString(),
                  DifferenceTypeEnum.DifferenceConstant.ToString(),
                  DifferenceTypeEnum.Sec3.ToString()
                },
               " Price ");

            FirstOperationType
                = panel.CreateParameter("First operation type", "/", new[]
                { "/",
                  "-",
                  "+",
                  "*"
                },
               " Price ");

            SecondOperationType
                 = panel.CreateParameter("Second operation type", "-", new[]
                { "/",
                  "-",
                  "+",
                  "*"
                },
                " Price ");
        }

        BotTabSimple _tab1;
        BotTabSimple _tab2;
        BotTabSimple _tab3;

        public StrategyParameterString PriceInMarketDepthSecOne;
        public StrategyParameterString PriceInMarketDepthSecTwo;
        public StrategyParameterString PriceInMarketDepthSecThree;

        public StrategyParameterBool PriceReverseQuoteSecOne;
        public StrategyParameterBool PriceReverseQuoteSecTwo;
        public StrategyParameterBool PriceReverseQuoteSecThree;

        public StrategyParameterInt HoursOffsetSecOne;
        public StrategyParameterInt HoursOffsetSecTwo;
        public StrategyParameterInt HoursOffsetSecThree;

        public StrategyParameterDecimal PriceKoeffSec1;
        public StrategyParameterDecimal PriceKoeffSec2;
        public StrategyParameterDecimal PriceKoeffSec3;

        public StrategyParameterDecimal DifferenceConst;

        public StrategyParameterString DifferenceType;

        public StrategyParameterString FirstOperationType;

        public StrategyParameterString SecondOperationType;

        public void Process()
        {
            // нормализуем бумаги

            bool isChanged = false;

            if (_tab1.IsConnected == true &&
                _tab1.IsReadyToTrade == true)
            {
                decimal lastPrice = 0;

                if (PriceInMarketDepthSecOne.ValueString == PriceOrderType.Mid.ToString())
                {
                    lastPrice = _tab1.PriceCenterMarketDepth;
                }
                else if (PriceInMarketDepthSecOne.ValueString == PriceOrderType.Bid.ToString())
                {
                    lastPrice = _tab1.PriceBestBid;
                }
                else if (PriceInMarketDepthSecOne.ValueString == PriceOrderType.Offer.ToString())
                {
                    lastPrice = _tab1.PriceBestAsk;
                }

                if(PriceReverseQuoteSecOne.ValueBool == true
                    && lastPrice != 0)
                {
                    lastPrice = 1 / lastPrice;
                }

                lastPrice = lastPrice * PriceKoeffSec1.ValueDecimal;

                if (lastPrice != _priceSec1)
                {
                    _priceSec1 = Math.Round(lastPrice,5);
                    isChanged = true;
                }
            }


            if (_tab2.IsConnected == true &&
                _tab2.IsReadyToTrade == true)
            {
                decimal lastPrice = 0;

                if (PriceInMarketDepthSecTwo.ValueString == PriceOrderType.Mid.ToString())
                {
                    lastPrice = _tab2.PriceCenterMarketDepth;
                }
                else if (PriceInMarketDepthSecTwo.ValueString == PriceOrderType.Bid.ToString())
                {
                    lastPrice = _tab2.PriceBestBid;
                }
                else if (PriceInMarketDepthSecTwo.ValueString == PriceOrderType.Offer.ToString())
                {
                    lastPrice = _tab2.PriceBestAsk;
                }

                if (PriceReverseQuoteSecTwo.ValueBool == true
                    && lastPrice != 0)
                {
                    lastPrice = 1 / lastPrice;
                }

                lastPrice = lastPrice * PriceKoeffSec2.ValueDecimal;

                if (lastPrice != _priceSec2)
                {
                    _priceSec2 = Math.Round(lastPrice,5);
                    isChanged = true;
                }
            }


            if (_tab3.IsConnected == true &&
                _tab3.IsReadyToTrade == true)
            {
                decimal lastPrice = 0;

                if (PriceInMarketDepthSecThree.ValueString == PriceOrderType.Mid.ToString())
                {
                    lastPrice = _tab3.PriceCenterMarketDepth;
                }
                else if (PriceInMarketDepthSecThree.ValueString == PriceOrderType.Bid.ToString())
                {
                    lastPrice = _tab3.PriceBestBid;
                }
                else if (PriceInMarketDepthSecThree.ValueString == PriceOrderType.Offer.ToString())
                {
                    lastPrice = _tab3.PriceBestAsk;
                }

                if (PriceReverseQuoteSecThree.ValueBool == true
                    && lastPrice != 0)
                {
                    lastPrice = 1 / lastPrice;
                }

                lastPrice = lastPrice * PriceKoeffSec3.ValueDecimal;

                if (lastPrice != _priceSec3)
                {
                    _priceSec3 = Math.Round(lastPrice, 5);
                    isChanged = true;
                }

            }

            if (isChanged &&
                PriceChangeEvent != null)
            {
                PriceChangeEvent();
            }
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

        public decimal PriceSec3
        {
            get
            {
                return _priceSec3;
            }
        }
        private decimal _priceSec3;

        public decimal CurrentSpread()
        {
            decimal price1 = PriceSec1;
            decimal price2 = PriceSec2;
            decimal price3 = PriceSec3;

            //Алексей, привет! Подскажи, пожалуйста, формулу спрэда в triple assistant можно подкрутить следующим образом Sec1-Sec2+Sec3?

            if (DifferenceType.ValueString == DifferenceTypeEnum.Off.ToString())
            {
               
                if (price1 == 0 ||
                    price2 == 0)
                {
                    return 0;
                }
                decimal spread = 0;

                if (FirstOperationType.ValueString == "/")
                {
                    spread = price1 / price2;
                }
                else if (FirstOperationType.ValueString == "*")
                {
                    spread = price1 * price2;
                }
                else if (FirstOperationType.ValueString == "-")
                {
                    spread = price1 - price2;
                }
                else // if (FirstOperationType.ValueString == "+")
                {
                    spread = price1 + price2;
                }

                return Math.Round(spread, 5);
            }
            else if (DifferenceType.ValueString == DifferenceTypeEnum.DifferenceConstant.ToString())
            {
                if (price1 == 0 ||
                    price2 == 0)
                {
                    return 0;
                }

                decimal spread = 0;

                if (FirstOperationType.ValueString == "/")
                {
                    spread = price1 / price2;
                }
                else if (FirstOperationType.ValueString == "*")
                {
                    spread = price1 * price2;
                }
                else if (FirstOperationType.ValueString == "-")
                {
                    spread = price1 - price2;
                }
                else // if (FirstOperationType.ValueString == "+")
                {
                    spread = price1 + price2;
                }

                decimal spreadResult = 0;

                if (SecondOperationType.ValueString == "/")
                {
                    spreadResult = spread / DifferenceConst.ValueDecimal;
                }
                else if (SecondOperationType.ValueString == "*")
                {
                    spreadResult = spread * DifferenceConst.ValueDecimal;
                }
                else if (SecondOperationType.ValueString == "-")
                {
                    spreadResult = spread - DifferenceConst.ValueDecimal;
                }
                else // if (SecondOperationType.ValueString == "+")
                {
                    spreadResult = spread + DifferenceConst.ValueDecimal;
                }


                return Math.Round(spreadResult, 5);
            }
            else if (DifferenceType.ValueString == DifferenceTypeEnum.Sec3.ToString())
            {
                if (price1 == 0 ||
                    price2 == 0 ||
                    price3 == 0)
                {
                    return 0;
                }

                decimal spread = 0;

                if (FirstOperationType.ValueString == "/")
                {
                    spread = price1 / price2;
                }
                else if (FirstOperationType.ValueString == "*")
                {
                    spread = price1 * price2;
                }
                else if (FirstOperationType.ValueString == "-")
                {
                    spread = price1 - price2;
                }
                else // if (FirstOperationType.ValueString == "+")
                {
                    spread = price1 + price2;
                }

                decimal spreadResult = 0;

                if (SecondOperationType.ValueString == "/")
                {
                    spreadResult = spread / price3;
                }
                else if (SecondOperationType.ValueString == "*")
                {
                    spreadResult = spread * price3;
                }
                else if (SecondOperationType.ValueString == "-")
                {
                    spreadResult = spread - price3;
                }
                else // if (SecondOperationType.ValueString == "+")
                {
                    spreadResult = spread + price3;
                }

                return Math.Round(spreadResult, 5);
            }

            return 0;
        }

        public string CurrentSpreadString()
        {
            string result = "";
            decimal price1 = PriceSec1;
            decimal price2 = PriceSec2;
            decimal price3 = PriceSec3;

            if (DifferenceType.ValueString == DifferenceTypeEnum.Off.ToString())
            {
                if (price1 == 0 ||
                    price2 == 0)
                {
                    return "";
                }

                result = price1.ToStringWithNoEndZero() + FirstOperationType.ValueString + price2.ToStringWithNoEndZero() + "= " + CurrentSpread();
            }
            else if (DifferenceType.ValueString == DifferenceTypeEnum.DifferenceConstant.ToString())
            {
                if (price1 == 0 ||
                    price2 == 0)
                {
                    return "";
                }
                result = "(" + price1.ToStringWithNoEndZero() + FirstOperationType.ValueString + price2.ToStringWithNoEndZero()
                    + ") " + SecondOperationType.ValueString + " " + DifferenceConst.ValueDecimal + " = "
                    + CurrentSpread();
            }
            else if (DifferenceType.ValueString == DifferenceTypeEnum.Sec3.ToString())
            {
                if (price1 == 0 ||
                    price2 == 0 ||
                    price3 == 0)
                {
                    return "";
                }

                result 
                    = "(" + price1.ToStringWithNoEndZero() + FirstOperationType.ValueString + price2.ToStringWithNoEndZero() + ") " 
                    + SecondOperationType.ValueString + price3.ToStringWithNoEndZero() + " = "
                    + CurrentSpread();
            }

            return result;
        }

        public string GetStringToGui()
        {
            string result = "Sec 1: ";
            result += Math.Round(PriceSec1, 4).ToStringWithNoEndZero();
            result += "  Sec 2: ";
            result += Math.Round(PriceSec2, 4).ToStringWithNoEndZero();
            result += "  Sec 3: ";
            result += Math.Round(PriceSec3, 4).ToStringWithNoEndZero();
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
            decimal price1 = PriceSec1;
            decimal price2 = PriceSec2;
            decimal price3 = PriceSec3;

            List<Candle> candles1 = _tab1.CandlesAll;
            List<Candle> candles2 = _tab2.CandlesAll;
            List<Candle> candles3 = _tab3.CandlesAll;

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

            if (DifferenceType.ValueString == DifferenceTypeEnum.Sec3.ToString())
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

            if (PriceReverseQuoteSecOne.ValueBool == true)
            {
                candles1 = GetReversQuoteCandles(candles1);
            }

            if (PriceReverseQuoteSecTwo.ValueBool == true)
            {
                candles2 = GetReversQuoteCandles(candles2);
            }

            candles1 = GetOffsetCandles(candles1, HoursOffsetSecOne.ValueInt);
            candles2 = GetOffsetCandles(candles2, HoursOffsetSecTwo.ValueInt);
            candles3 = GetOffsetCandles(candles3, HoursOffsetSecThree.ValueInt);

            candles1 = GetCoeffCandles(candles1, PriceKoeffSec1.ValueDecimal);
            candles2 = GetCoeffCandles(candles2, PriceKoeffSec2.ValueDecimal);
            candles3 = GetCoeffCandles(candles3, PriceKoeffSec3.ValueDecimal);

            // операция 1

            List<Candle> resultFirstOperation = null;

            if (FirstOperationType.ValueString == "/")
            {
                resultFirstOperation = GetDivisionTheCandles(candles1, candles2);
            }
            if (FirstOperationType.ValueString == "-")
            {
                resultFirstOperation = GetSubtractTheCandles(candles1, candles2);
            }
            if (FirstOperationType.ValueString == "+")
            {
                resultFirstOperation = GetAdditionTheCandles(candles1, candles2);
            }
            if (FirstOperationType.ValueString == "*")
            {
                resultFirstOperation = GetMultiplicationTheCandles(candles1, candles2);
            }

            if (resultFirstOperation == null
                || resultFirstOperation.Count == 0)
            {
                return null;
            }

            if (DifferenceType.ValueString == DifferenceTypeEnum.Off.ToString())
            {
                return resultFirstOperation;
            }
            else if (DifferenceType.ValueString == DifferenceTypeEnum.DifferenceConstant.ToString())
            {
                List<Candle> result = null;

                if (SecondOperationType.ValueString == "-")
                {
                    result = GetSubtractTheValue(resultFirstOperation, DifferenceConst.ValueDecimal);
                }
                if (SecondOperationType.ValueString == "+")
                {
                    result = GetAdditionTheValue(resultFirstOperation, DifferenceConst.ValueDecimal);
                }
                if (SecondOperationType.ValueString == "/")
                {
                    result = GetDivisionTheValue(resultFirstOperation, DifferenceConst.ValueDecimal);
                }
                if (SecondOperationType.ValueString == "*")
                {
                    result = GetMultiplicationTheValue(resultFirstOperation, DifferenceConst.ValueDecimal);
                }

                return result;
            }
            else if (DifferenceType.ValueString == DifferenceTypeEnum.Sec3.ToString())
            {
                List<Candle> result = null;

                if (SecondOperationType.ValueString == "-")
                {
                    result = GetSubtractTheCandles(resultFirstOperation, candles3);
                }
                if (SecondOperationType.ValueString == "+")
                {
                    result = GetAdditionTheCandles(resultFirstOperation, candles3);
                }
                if (SecondOperationType.ValueString == "/")
                {
                    result = GetDivisionTheCandles(resultFirstOperation, candles3);
                }
                if (SecondOperationType.ValueString == "*")
                {
                    result = GetMultiplicationTheCandles(resultFirstOperation, candles3);
                }

                return result;
            }

            List<Candle> candles = new List<Candle>();

            return candles;
        }

        private List<Candle> GetReversQuoteCandles(List<Candle> candles)
        {
            List<Candle> result = new List<Candle>();

            for (int i = 0; i < candles.Count; i++)
            {
                Candle candle = candles[i];
                Candle newCandle = new Candle();

                newCandle.Open = Math.Round(1 / candle.Open, 5);
                newCandle.High = Math.Round(1 / candle.High, 5);
                newCandle.Low = Math.Round(1 / candle.Low, 5);
                newCandle.Close = Math.Round(1 / candle.Close, 5);

                newCandle.Volume = candle.Volume;
                newCandle.TimeStart = candle.TimeStart;
                newCandle.State = candle.State;

                if (newCandle.Open > newCandle.High)
                {
                    newCandle.High = newCandle.Open;
                }
                if (newCandle.Open < newCandle.Low)
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


        private List<Candle> GetDivisionTheCandles(List<Candle> candlesOne, List<Candle> candlesTwo)
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

        private List<Candle> GetAdditionTheCandles(List<Candle> candlesOne, List<Candle> candlesTwo)
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

                decimal valueOpen = first.Open + second.Open;
                decimal valueHigh = first.High + second.High;
                decimal valueLow = first.Low + second.Low;
                decimal valueClose = first.Close + second.Close;

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

        private List<Candle> GetMultiplicationTheCandles(List<Candle> candlesOne, List<Candle> candlesTwo)
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

                decimal valueOpen = first.Open * second.Open;
                decimal valueHigh = first.High * second.High;
                decimal valueLow = first.Low * second.Low;
                decimal valueClose = first.Close * second.Close;

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


        private List<Candle> GetSubtractTheValue(List<Candle> candlesOne, decimal valueToSubstract)
        {
            if (valueToSubstract == 0)
            {
                return candlesOne;
            }

            List<Candle> newCandles = new List<Candle>();

            for (int i = candlesOne.Count - 1; i > -1; i--)
            {
                Candle first = candlesOne[i];

                decimal valueOpen = first.Open - valueToSubstract;
                decimal valueHigh = first.High - valueToSubstract;
                decimal valueLow = first.Low - valueToSubstract;
                decimal valueClose = first.Close - valueToSubstract;

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

        private List<Candle> GetDivisionTheValue(List<Candle> candlesOne, decimal valueToSubstract)
        {
            if (valueToSubstract == 0)
            {
                return candlesOne;
            }

            List<Candle> newCandles = new List<Candle>();

            for (int i = candlesOne.Count - 1; i > -1; i--)
            {
                Candle first = candlesOne[i];

                decimal valueOpen = first.Open / valueToSubstract;
                decimal valueHigh = first.High / valueToSubstract;
                decimal valueLow = first.Low / valueToSubstract;
                decimal valueClose = first.Close / valueToSubstract;

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

        private List<Candle> GetAdditionTheValue(List<Candle> candlesOne, decimal valueToSubstract)
        {
            if (valueToSubstract == 0)
            {
                return candlesOne;
            }

            List<Candle> newCandles = new List<Candle>();

            for (int i = candlesOne.Count - 1; i > -1; i--)
            {
                Candle first = candlesOne[i];

                decimal valueOpen = first.Open + valueToSubstract;
                decimal valueHigh = first.High + valueToSubstract;
                decimal valueLow = first.Low + valueToSubstract;
                decimal valueClose = first.Close + valueToSubstract;

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

        private List<Candle> GetMultiplicationTheValue(List<Candle> candlesOne, decimal valueToSubstract)
        {
            if (valueToSubstract == 0)
            {
                return candlesOne;
            }

            List<Candle> newCandles = new List<Candle>();

            for (int i = candlesOne.Count - 1; i > -1; i--)
            {
                Candle first = candlesOne[i];

                decimal valueOpen = first.Open * valueToSubstract;
                decimal valueHigh = first.High * valueToSubstract;
                decimal valueLow = first.Low * valueToSubstract;
                decimal valueClose = first.Close * valueToSubstract;

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

    public class ScenarioOneTriple
    {
        public StrategyParameterInt WorkTimeMinutes;

        public string MessageOnStartScenario1 = "Scenario 1. \n Start?";

        PriceModuleTripleArbitrage _priceModule;

        public ScenarioOneTriple(BotPanel panel, PriceModuleTripleArbitrage priceModule)
        {
            _priceModule = priceModule;

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

            result += _priceModule.CurrentSpreadString();

            return result;
        }

        public TimeSpan GetWorkTimeMinutesInTimeSpan()
        {
            TimeSpan span = new TimeSpan(0, WorkTimeMinutes.ValueInt, 0);

            return span;
        }
    }

    public class ScenarioTwoTriple
    {
        public StrategyParameterString ScenarioTwoSide;
        public StrategyParameterDecimal ScenarioTwoMaxSpreadValue;
        public StrategyParameterInt WorkTimeMinutes;
        public string MessageOnStartScenario2 = "Scenario 2. \n Start?";

        PriceModuleTripleArbitrage _priceModule;

        public ScenarioTwoTriple(BotPanel panel, PriceModuleTripleArbitrage priceModule)
        {
            _priceModule = priceModule;

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

            result += _priceModule.CurrentSpreadString() + " Max spread: " + ScenarioTwoMaxSpreadValue.ValueDecimal.ToString();

            return result;
        }

        public TimeSpan GetWorkTimeMinutesInTimeSpan()
        {
            TimeSpan span = new TimeSpan(0, WorkTimeMinutes.ValueInt, 0);

            return span;
        }
    }

    public class ScenarioThreeTriple
    {
        public StrategyParameterDecimal ScenarioThreeMinSpreadValue;
        public StrategyParameterInt WorkTimeMinutes;

        public string MessageOnStartScenario3 = "Scenario 3. \n Start?";

        PriceModuleTripleArbitrage _priceModule;

        public ScenarioThreeTriple(BotPanel panel, PriceModuleTripleArbitrage priceModule)
        {
            _priceModule = priceModule;

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

            result += "Min: " + ScenarioThreeMinSpreadValue.ValueDecimal.ToString()
                + " Cur spread: " + _priceModule.CurrentSpreadString();

            return result;
        }

        public TimeSpan GetWorkTimeMinutesInTimeSpan()
        {
            TimeSpan span = new TimeSpan(0, WorkTimeMinutes.ValueInt, 0);

            return span;
        }
    }

    public class ScenarioFourTriple
    {

        public StrategyParameterDecimal ScenarioFourMaxSpreadValue;
        public StrategyParameterDecimal ScenarioFourMinSpreadValue;
        public StrategyParameterInt WorkTimeMinutes;

        public string MessageOnStartScenario4 = "Scenario 4. \n Start?";

        PriceModuleTripleArbitrage _priceModule;

        public ScenarioFourTriple(BotPanel panel, PriceModuleTripleArbitrage priceModule)
        {
            _priceModule = priceModule;

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

            result += "Min: " + ScenarioFourMinSpreadValue.ValueDecimal.ToString()
                + " Cur spread: " + _priceModule.CurrentSpreadString()
                + " Max: " + ScenarioFourMaxSpreadValue.ValueDecimal.ToString();

            return result;
        }

        public TimeSpan GetWorkTimeMinutesInTimeSpan()
        {
            TimeSpan span = new TimeSpan(0, WorkTimeMinutes.ValueInt, 0);

            return span;
        }
    }

    public enum DifferenceTypeEnum
    {
        Off,
        DifferenceConstant,
        Sec3
    }

    public enum DifferenceOperationTypeEnum
    {
        Difference,
        Dividing,
        Multiplication,
        Addition
    }

    public enum ScenarioTypeTriple
    {
        None,
        ScenarioOne,
        ScenarioTwo,
        ScenarioThree,
        ScenarioFour
    }

    public class VolumeTable
    {
        public List<VolumeRow> RowsAllToOpen = new List<VolumeRow>();

        public decimal GetVolumeFromRows(string securityName)
        {
            decimal volume = 0;

            for (int i = 0; i < RowsAllToOpen.Count; i++)
            {
                VolumeCell cell = null;

                for (int j = 0; j < RowsAllToOpen[i].VolumeCells.Count; j++)
                {
                    if (RowsAllToOpen[i].VolumeCells[j].SecurityTab.Securiti.Name == securityName)
                    {
                        cell = RowsAllToOpen[i].VolumeCells[j];
                        break;
                    }
                }

                if (cell != null)
                {
                    volume += cell.VolumeToOpen;
                }
            }

            return volume;
        }

        public VolumeRow RowEndVolume = new VolumeRow();

        public bool IsEnded;

        public void Clear()
        {
            RowsAllToOpen.Clear();
            RowEndVolume = new VolumeRow();
        }
    }

    public class VolumeRow
    {
        public List<VolumeCell> VolumeCells = new List<VolumeCell>();

        public string GetString()
        {
            string result = "";

            for (int i = 0; i < VolumeCells.Count; i++)
            {
                result += VolumeCells[i].GetString() + " | ";
            }

            return result;
        }
    }

    public class VolumeCell
    {
        public BotTabSimple SecurityTab;

        public Side Side;

        public decimal VolumeToOpen;

        public bool IsEnded;

        public string GetString()
        {
            string result = "";

            result += SecurityTab.Securiti.Name + " ";
            result += Side.ToString() + " ";
            result += VolumeToOpen.ToStringWithNoEndZero() + " ";

            return result;
        }
    }
}