using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using OsEngine.Entity;
using System.Drawing;
using System.Windows.Forms;
using OsEngine.Language;
using OsEngine.Market;
using System.Threading;
using OsEngine.Market.Servers.MoexAlgopack.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace OsEngine.Robots.AlexBots
{
    /// <summary>
    /// Interaction logic for DashBordUi.xaml
    /// </summary>
    public partial class DashBordUi : Window
    {
        public DashBordUi()
        {
            InitializeComponent();

            CreateTable();

            Closed += DashBordUi_Closed;

            Thread worker = new Thread(PainterThread);
            worker.Start();
        }

        private void DashBordUi_Closed(object sender, EventArgs e)
        {
            _isClosed = true;
        }

        DataGridView _grid;

        private void CreateTable()
        {
            _grid = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect, DataGridViewAutoSizeRowsMode.AllCells);

            _grid.ScrollBars = ScrollBars.Vertical;

            DataGridViewTextBoxCell cell0 = new DataGridViewTextBoxCell();
            cell0.Style = _grid.DefaultCellStyle;

            DataGridViewColumn column0 = new DataGridViewColumn();
            column0.CellTemplate = cell0;
            column0.HeaderText = "Strategy name"; // StrategyName
            column0.ReadOnly = true;
            column0.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            _grid.Columns.Add(column0);

            DataGridViewColumn column2 = new DataGridViewColumn();
            column2.CellTemplate = cell0;
            column2.HeaderText = "Cur spread"; // Current Spread
            column2.ReadOnly = true;
            column2.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns.Add(column2);

            DataGridViewColumn column3 = new DataGridViewColumn();
            column3.CellTemplate = cell0;
            column3.HeaderText = "Today min"; // Today min
            column3.ReadOnly = true;
            column3.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns.Add(column3);

            DataGridViewColumn column4 = new DataGridViewColumn();
            column4.CellTemplate = cell0;
            column4.HeaderText = "Today max"; // Today max
            column4.ReadOnly = true;
            column4.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns.Add(column4);

            DataGridViewColumn column5 = new DataGridViewColumn();
            column5.CellTemplate = cell0;
            column5.HeaderText = "Yesterday close"; // Yesterday close
            column5.ReadOnly = true;
            column5.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns.Add(column5);

            DataGridViewColumn column6 = new DataGridViewColumn();
            column6.CellTemplate = cell0;
            column6.HeaderText = "Daily change"; // Daily Change
            column6.ReadOnly = true;
            column6.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns.Add(column6);

            DataGridViewColumn column7 = new DataGridViewColumn();
            column7.CellTemplate = cell0;
            column7.HeaderText = "Weekly change"; // Weekly Change
            column7.ReadOnly = true;
            column7.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns.Add(column7);

            HostTable.Child = _grid;
            HostTable.Child.Show();
        }

        bool _isClosed = false;

        private void PainterThread()
        {
            while(true)
            {
                Thread.Sleep(5000);

                if (_isClosed == true)
                {
                    return;
                }

                try
                {
                    PaintTable();
                }
                catch(Exception ex)
                {
                    ServerMaster.SendNewLogMessage("DashBoard error: " +  ex.ToString(),Logging.LogMessageType.Error);
                    Thread.Sleep(10000);
                }
            }
        }

        private void PaintTable()
        {
            if (_grid.InvokeRequired)
            {
                _grid.Invoke(new Action(PaintTable));
                return;
            }

            try
            {
                List<BotPanel> bots = GetRobots();

                if (bots.Count == 0 &&
                    _grid.Rows.Count != 0)
                {
                    _grid.Rows.Clear();
                    return;
                }

                if (bots.Count == 0)
                {
                    return;
                }

                if (bots.Count != _grid.Rows.Count)
                {
                    RePaintAll(bots);
                }
                else
                {
                    TryRePaintValues(bots);
                }
            }
            catch (Exception ex)
            {
                ServerMaster.SendNewLogMessage("DashBoard error: " + ex.ToString(), Logging.LogMessageType.Error);
                return;
            }
        }

        private void RePaintAll(List<BotPanel> bots)
        {
            _grid.Rows.Clear();

            for(int i = 0; i < bots.Count; i++)
            {
                DataGridViewRow row = GetRow(bots[i]);
                _grid.Rows.Add(row);
            }
        }

        private void TryRePaintValues(List<BotPanel> bots)
        {
            for(int i = 0;i < bots.Count; i++)
            {
                DataGridViewRow curRow = _grid.Rows[i];
                DataGridViewRow newRow = GetRow(bots[i]);

                for(int j = 0; j < curRow.Cells.Count; j++)
                {
                    if (curRow.Cells[j].Value != newRow.Cells[j].Value)
                    {
                        curRow.Cells[j].Value = newRow.Cells[j].Value;
                    }
                }
            }
        }

        private DataGridViewRow GetRow(BotPanel bot)
        {
            // 0 StrategyName
            // 1 Current Spread
            // 2 Today min
            // 3 Today max
            // 4 Yesterday close
            // 5 Daily Change
            // 6 Weekly Change


            List<Candle> candles = null;
                
            if(bot.GetNameStrategyType() == "TripleArbitrageAssistant")
            {
                candles = ((TripleArbitrageAssistant)bot).PriceModule.GetSpreadCandles();
            }
            else //if(bot.NameStrategyUniq == "PairArbitrageAssistant")
            {
                candles = ((PairArbitrageAssistant)bot).PriceModule.GetSpreadCandles();
            }
               
            decimal curSpread = GetCurrentSpread(candles);
            decimal todayMin = GetTodayMin(candles);
            decimal todayMax = GetTodayMax(candles);
            decimal yesterdayClose = GetYesterdayClose(candles);
            decimal dailyChange = GetDailyChange(candles, yesterdayClose);
            decimal weeklyChange = GetWeeklyChange(candles);

            DataGridViewRow nRow = new DataGridViewRow();

            nRow.Cells.Add(new DataGridViewTextBoxCell());
            nRow.Cells[0].Value = bot.NameStrategyUniq;

            nRow.Cells.Add(new DataGridViewTextBoxCell());
            nRow.Cells[1].Value = curSpread.ToStringWithNoEndZero();

            nRow.Cells.Add(new DataGridViewTextBoxCell());
            nRow.Cells[2].Value = todayMin.ToStringWithNoEndZero();

            nRow.Cells.Add(new DataGridViewTextBoxCell());
            nRow.Cells[3].Value = todayMax.ToStringWithNoEndZero();

            nRow.Cells.Add(new DataGridViewTextBoxCell());
            nRow.Cells[4].Value = yesterdayClose.ToStringWithNoEndZero();

            nRow.Cells.Add(new DataGridViewTextBoxCell());
            nRow.Cells[5].Value = dailyChange.ToStringWithNoEndZero();

            nRow.Cells.Add(new DataGridViewTextBoxCell()); 
            nRow.Cells[6].Value = weeklyChange.ToStringWithNoEndZero();

            return nRow;
        }

        private List<BotPanel> GetRobots()
        {
            List<BotPanel> bots = new List<BotPanel>();

            List<BotPanel> allBots = OsTraderMaster.Master.PanelsArray;

            for (int i = 0; allBots != null && i < allBots.Count; i++)
            {
                BotPanel curBot = allBots[i];

                string curBotType = curBot.GetNameStrategyType();

                if(curBotType == "PairArbitrageAssistant" ||
                    curBotType == "TripleArbitrageAssistant")
                {
                    bots.Add(curBot);
                }
            }

            return bots;
        }

        private decimal GetCurrentSpread(List<Candle> candles)
        {
            if(candles == null
                || candles.Count == 0)
            {
                return 0;
            }

            decimal result = candles[candles.Count - 1].Close;

            return Math.Round(result,4);
        }

        private decimal GetTodayMin(List<Candle> candles)
        {
            if (candles == null
                || candles.Count == 0)
            {
                return 0;
            }

            DateTime today = candles[candles.Count - 1].TimeStart.Date;

            decimal min = Decimal.MaxValue;

            for(int i = candles.Count - 1; i >= 0;  i--)
            {
                Candle curCandle = candles[i];

                if(curCandle.TimeStart.Date != today)
                {
                    break;
                }

                if(curCandle.Low < min)
                {
                    min = curCandle.Low;
                }
            }

            if(min == Decimal.MaxValue)
            {
                return 0;
            }

            return Math.Round(min,4);
        }

        private decimal GetTodayMax(List<Candle> candles)
        {
            if (candles == null
               || candles.Count == 0)
            {
                return 0;
            }

            DateTime today = candles[candles.Count - 1].TimeStart.Date;

            decimal max = Decimal.MinValue;

            for (int i = candles.Count - 1; i >= 0; i--)
            {
                Candle curCandle = candles[i];

                if (curCandle.TimeStart.Date != today)
                {
                    break;
                }

                if (curCandle.High > max)
                {
                    max = curCandle.High;
                }
            }

            if (max == Decimal.MinValue)
            {
                return 0;
            }

            return Math.Round(max,4);
        }

        private decimal GetYesterdayClose(List<Candle> candles)
        {
            if (candles == null
               || candles.Count == 0)
            {
                return 0;
            }

            DateTime today = candles[candles.Count - 1].TimeStart.Date;

            decimal close = 0;

            for (int i = candles.Count - 1; i >= 0; i--)
            {
                Candle curCandle = candles[i];

                if (curCandle.TimeStart.Date != today)
                {
                    close = curCandle.Close;
                    break;
                }

            }

            return Math.Round(close, 4);
        }

        private decimal GetDailyChange(List<Candle> candles, decimal yesterdayClose)
        {
            if (candles == null
              || candles.Count == 0
              || candles.Count == 1)
            {
                return 0;
            }

            if(yesterdayClose == 0)
            {
                yesterdayClose = candles[0].Open;
            }

            decimal close = candles[candles.Count-1].Close;

            decimal result = (close - yesterdayClose);

            return Math.Round(result,4);
        }

        private decimal GetWeeklyChange(List<Candle> candles)
        {
            if (candles == null
            || candles.Count == 0)
            {
                return 0;
            }

            decimal open = 0;
            int dayCount = 0;

            DateTime today = candles[candles.Count - 1].TimeStart.Date;

            for (int i = candles.Count-1; i >= 0;i --)
            {
                Candle curCandle = candles[i];

                if (curCandle.TimeStart.Date != today)
                {
                    today = curCandle.TimeStart.Date;
                    dayCount++;

                    if(dayCount == 7)
                    {
                        break;
                    }
                }

                open = curCandle.Open;
            }

            decimal close = candles[candles.Count - 1].Close;

            decimal result = (close - open);

            return Math.Round(result, 4);
        }
    }
}