using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Globalization;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Charts.CandleChart.Elements;
using System.Drawing;
using System.IO;
using OsEngine.Market.Servers.BingX.BingXFutures.Entity;

namespace OsEngine.Robots
{
    [Bot("FractalsHedgeBot")]
    public class FractalsHedgeBot : BotPanel
    {
        public override string GetNameStrategyType()
        {
            return "FractalsHedgeBot";
        }

        public override void ShowIndividualSettingsDialog()
        {
        }

        private Logging.LogMessageType _logType = Logging.LogMessageType.User;
        private StartProgram _startProgram;
        private BotTabSimple _tab;
        private StrategyParameterString _typeVolume;
        private StrategyParameterDecimal _volume;
        private StrategyParameterDecimal _leverage;
        private StrategyParameterString _typeMinVolume;
        private StrategyParameterDecimal _minVolumeData;
        private StrategyParameterDecimal _positionIncreaseFactor;
        private StrategyParameterString _indentIndicator;
        private StrategyParameterDecimal _indentationSize;
        private StrategyParameterCheckBox _onTakeProfit;
        private StrategyParameterDecimal _setTakeProfit;

        private Fractal _fractal;
        private decimal _upFractal;
        private decimal _downFractal;

        public FractalsHedgeBot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");

            _startProgram = startProgram;

            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _fractal = new Fractal(name + "Fractal", false);

            string tabNameParameters = " Параметры ";

            _typeVolume = CreateParameter("Тип максимального объема позиции", GetDescription(TypeVolume.Fix), new string[] { GetDescription(TypeVolume.Fix), GetDescription(TypeVolume.Percent) }, tabNameParameters);
            _volume = CreateParameter("Максимальный объем позиции (USDT/%)", 0m, 0m, 0m, 0m, tabNameParameters);
            _leverage = CreateParameter("Плечо", 0m, 0m, 0m, 0m, tabNameParameters);
            _typeMinVolume = CreateParameter("Тип минимального объема позиции", GetDescription(TypeVolume.Fix), new string[] { GetDescription(TypeVolume.Fix), GetDescription(TypeVolume.Percent) }, tabNameParameters);
            _minVolumeData = CreateParameter("Минимальный объем позиции (USDT/%)", 0m, 0m, 0m, 0m, tabNameParameters);
            _positionIncreaseFactor = CreateParameter("Коэффициент увеличения позиции", 0m, 0m, 0m, 0m, tabNameParameters);
            _indentIndicator = CreateParameter("Отступ от индикатора", GetDescription(TypeIndential.Fix), new string[] { GetDescription(TypeIndential.Fix), GetDescription(TypeIndential.Percent) }, tabNameParameters);
            _indentationSize = CreateParameter("Величина отступа", 0m, 0m, 0m, 0m, tabNameParameters);
            _onTakeProfit = CreateParameterCheckBox("Включить функцию тейк-профита", false, tabNameParameters);
            _setTakeProfit = CreateParameter("Величина профита, %", 0m, 0m, 0m, 0m, tabNameParameters);

            // non trade periods

            NonTradePeriod1OnOff
                = CreateParameter("Non trade. Period " + "1",
                "Off", new string[] { "Off", "On" }, " Non Trade periods ");
            NonTradePeriod1Start = CreateParameterTimeOfDay("Start period " + "1", 9, 0, 0, 0, " Non Trade periods ");
            NonTradePeriod1End = CreateParameterTimeOfDay("End period " + "1", 10, 5, 0, 0, " Non Trade periods ");

            NonTradePeriod2OnOff
                = CreateParameter("Non trade. Period " + "2",
                "Off", new string[] { "Off", "On" }, " Non Trade periods ");
            NonTradePeriod2Start = CreateParameterTimeOfDay("Start period " + "2", 13, 55, 0, 0, " Non Trade periods ");
            NonTradePeriod2End = CreateParameterTimeOfDay("End period " + "2", 14, 5, 0, 0, " Non Trade periods ");

            NonTradePeriod3OnOff
                = CreateParameter("Non trade. Period " + "3",
                "Off", new string[] { "Off", "On" }, " Non Trade periods ");
            NonTradePeriod3Start = CreateParameterTimeOfDay("Start period " + "3", 18, 40, 0, 0, " Non Trade periods ");
            NonTradePeriod3End = CreateParameterTimeOfDay("End period " + "3", 19, 5, 0, 0, " Non Trade periods ");

            NonTradePeriod4OnOff
                = CreateParameter("Non trade. Period " + "4",
                "Off", new string[] { "Off", "On" }, " Non Trade periods ");
            NonTradePeriod4Start = CreateParameterTimeOfDay("Start period " + "4", 23, 40, 0, 0, " Non Trade periods ");
            NonTradePeriod4End = CreateParameterTimeOfDay("End period " + "4", 23, 59, 0, 0, " Non Trade periods ");

            NonTradePeriod5OnOff
                = CreateParameter("Non trade. Period " + "5",
                "Off", new string[] { "Off", "On" }, " Non Trade periods ");
            NonTradePeriod5Start = CreateParameterTimeOfDay("Start period " + "5", 23, 40, 0, 0, " Non Trade periods ");

            NonTradePeriod5End = CreateParameterTimeOfDay("End period " + "5", 23, 59, 0, 0, " Non Trade periods ");

            CreateParameterLabel("Empty string tp", "", "", 20, 20, System.Drawing.Color.Black, " Non Trade periods ");

            TradeInMonday = CreateParameter("Trade in Monday. Is on", true, " Non Trade periods ");
            TradeInTuesday = CreateParameter("Trade in Tuesday. Is on", true, " Non Trade periods ");
            TradeInWednesday = CreateParameter("Trade in Wednesday. Is on", true, " Non Trade periods ");
            TradeInThursday = CreateParameter("Trade in Thursday. Is on", true, " Non Trade periods ");
            TradeInFriday = CreateParameter("Trade in Friday. Is on", true, " Non Trade periods ");
            TradeInSaturday = CreateParameter("Trade in Saturday. Is on", true, " Non Trade periods ");
            TradeInSunday = CreateParameter("Trade in Sunday. Is on", true, " Non Trade periods ");

            this.ParamGuiSettings.Title = "Fractals Hedge Bot";
            this.ParamGuiSettings.Height = 600;
            this.ParamGuiSettings.Width = 700;

            CustomTabToParametersUi customTabMonitoring = ParamGuiSettings.CreateCustomTab(" Мониторинг и управление ");

            CreateTableMonitoring();
            customTabMonitoring.AddChildren(_hostMonitoring);

            _tab.ManualPositionSupport.DisableManualSupport();
            _tab.CandleUpdateEvent += _tab_CandleUpdateEvent;
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tab.OrderUpdateEvent += _tab_OrderUpdateEvent;

            _tab.PositionOpenerToStop?.Clear();

            Thread worker = new Thread(ThreadRefreshTable) { IsBackground = true };
            worker.Start();

            if (_startProgram == StartProgram.IsOsTrader)
            {
                Thread mainThread = new Thread(MainThread) { IsBackground = true };
                mainThread.Start();
            }
        }
                
        #region Fractal

        private bool _upFractalIsChange;
        private bool _downFractalIsChange;

        private void _tab_CandleFinishedEvent(List<Candle> obj)
        {
            if (_startProgram == StartProgram.IsTester)
            {
                _lastPrice = obj[obj.Count - 1].Close;

                CheckStatus();
            }

            AddFractalsToChart(obj);
        }

        private void AddFractalsToChart(List<Candle> obj)
        {
            if (obj.Count < 5)
            {
                _upFractal = 0;
                _downFractal = 0;
                _fractal.ValuesUp?.Clear();
                _fractal.ValuesDown?.Clear();
                _tab.BuyAtStopCancel();
                _tab.SellAtStopCancel();

                return;
            }

            _fractal.Process(obj);

            for (int i = _fractal.ValuesUp.Count - 1; i >= 0; i--)
            {
                if (_fractal.ValuesUp[i] != 0)
                {
                    if (_upFractal != _fractal.ValuesUp[i])
                    {
                        _upFractal = _fractal.ValuesUp[i];
                        _upFractalIsChange = true;

                        PointElement point = new PointElement("UpFractal", "Prime");

                        point.Y = obj[i].High + _tab.Security.PriceStep * 10;
                        point.TimePoint = obj[i].TimeStart;
                        point.Color = Color.Green;
                        point.Style = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Star4;
                        point.Size = 12;

                        _tab.SetChartElement(point);
                        SendNewLogMessage("Изменился верхний фрактал: " + _upFractal, _logType);
                    }

                    break;
                }
            }

            for (int i = _fractal.ValuesDown.Count - 1; i >= 0; i--)
            {
                if (_fractal.ValuesDown[i] != 0)
                {
                    if (_downFractal != _fractal.ValuesDown[i])
                    {
                        _downFractal = _fractal.ValuesDown[i];
                        _downFractalIsChange = true;

                        PointElement point = new PointElement("DownFractal", "Prime");

                        point.Y = obj[i].Low - _tab.Security.PriceStep * 10;
                        point.TimePoint = obj[i].TimeStart;
                        point.Color = Color.Red;
                        point.Style = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Star4;
                        point.Size = 12;

                        _tab.SetChartElement(point);
                        SendNewLogMessage("Изменился нижний фрактал: " + _downFractal, _logType);
                    }

                    break;
                }
            }
        }

        #endregion

        #region Monitoring tab

        private WindowsFormsHost _hostMonitoring;
        private DataGridView _gridMonitoring;
        private DataGridView _gridMonitoringButton;

        private void ThreadRefreshTable()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(1000);
                    AddDataToGrid();
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                }
            }
        }

        private void CreateTableMonitoring()
        {
            _hostMonitoring = new WindowsFormsHost();

            _gridMonitoringButton = AddMonitoringButton();
            _gridMonitoring = AddMonitoringTable();

            TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.ColumnCount = 1;
            tableLayoutPanel.RowCount = 2;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));

            tableLayoutPanel.Controls.Add(_gridMonitoringButton, 0, 0);
            tableLayoutPanel.Controls.Add(_gridMonitoring, 0, 1);

            _gridMonitoringButton.CellClick += _gridMonitoringButton_CellClick;

            _hostMonitoring.Child = tableLayoutPanel;
        }

        private DataGridView AddMonitoringButton()
        {
            DataGridView newButtonGrid =
               DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect,
               DataGridViewAutoSizeRowsMode.AllCells);

            newButtonGrid.Dock = DockStyle.Fill;
            newButtonGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            newButtonGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            newButtonGrid.Columns.Add("Col0", "");
            newButtonGrid.Columns.Add("Col1", "");
            newButtonGrid.Columns.Add("Col2", "");
            newButtonGrid.Columns.Add("Col3", "");

            foreach (DataGridViewColumn column in newButtonGrid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            DataGridViewRow row = new DataGridViewRow();

            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[0].Value = GetDescription(Status.Start);
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[1].Value = GetDescription(Status.Stop);
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[2].Value = GetDescription(Status.CloseOrdersOnMarket);
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[3].Value = GetDescription(Status.EqualizePositions);

            newButtonGrid.Rows.Add(row);

            return newButtonGrid;
        }

        private DataGridView AddMonitoringTable()
        {
            DataGridView newTableGrid =
                DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect,
                DataGridViewAutoSizeRowsMode.AllCells);

            newTableGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            newTableGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            newTableGrid.Dock = DockStyle.Fill;

            DataGridViewTextBoxCell cell0 = new DataGridViewTextBoxCell();
            cell0.Style = newTableGrid.DefaultCellStyle;

            newTableGrid.Columns.Add("Col1", "");
            newTableGrid.Columns.Add("Col2", "");

            foreach (DataGridViewColumn column in newTableGrid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                column.ReadOnly = true;
            }

            List<string> listMonitoring = new List<string>()
            {
                "Текущий режим",
                "Торговая пара",
                "Текущая цена",
                "Кошелек",
                "Всего средств",
                "Свободно средств",
                "Сумма депозита в работе",
                "Объем и цена позиции ЛОНГ (отклонение от рыночной цены в %)",
                "Объем и цена позиции ШОРТ (отклонение от рыночной цены в %)",
                "Расстояние в процентах и пунтакх между открытыми позициями ЛОНГ и ШОРТ",
                "Состояние ордеров (выставленный объем в лонг и шорт)",
                "Нереализованный PNL"
            };

            for (int i = 0; i < listMonitoring.Count; i++)
            {
                DataGridViewRow row = new DataGridViewRow();

                row.Cells.Add(new DataGridViewTextBoxCell());
                row.Cells[0].Value = listMonitoring[i];
                row.Cells.Add(new DataGridViewTextBoxCell());
                newTableGrid.Rows.Add(row);
            }

            return newTableGrid;
        }

        private void AddDataToGrid()
        {
            if (MainWindow.GetDispatcher.CheckAccess() == false)
            {
                MainWindow.GetDispatcher.Invoke(new Action(AddDataToGrid));
                return;
            }

            if (_tab.Portfolio == null)
            {
                return;
            }

            if (_lastPrice == 0)
            {
                return;
            }

            decimal freeMoney = Math.Round(_tab.Portfolio.ValueCurrent - _tab.Portfolio.ValueBlocked, 4);
            decimal wallet = Math.Round(_volume.ValueDecimal * _leverage.ValueDecimal, 4);

            if (_typeVolume.ValueString == GetDescription(TypeVolume.Percent))
            {
                wallet = Math.Round(_tab.Portfolio.ValueCurrent * _volume.ValueDecimal / 100 * _leverage.ValueDecimal, 4);
            }

            decimal openVolumeLong = GetOpenVolume(_tab.PositionOpenLong);
            decimal openVolumeShort = GetOpenVolume(_tab.PositionOpenShort);

            decimal avrPriceLong = AveragePricePositions(_tab.PositionOpenLong);
            decimal avrPriceShort = AveragePricePositions(_tab.PositionOpenShort);

            decimal deviationPriceShort = 0;
            decimal deviationPriceLong = 0;
            decimal unrealizLong = 0;
            decimal unrealizShort = 0;
            decimal diffLongShort = 0;
            decimal diffLongShortPercent = 0;

            if (avrPriceShort != 0)
            {
                deviationPriceShort = Math.Round((1 - _lastPrice / avrPriceShort) * 100, 2);
                unrealizShort = avrPriceShort * openVolumeShort - _lastPrice * openVolumeShort;
            }

            if (avrPriceLong != 0)
            {
                deviationPriceLong = Math.Round((1 - avrPriceLong / _lastPrice) * 100, 2);
                unrealizLong = _lastPrice * openVolumeLong - avrPriceLong * openVolumeLong;
            }

            decimal unrealizPNL = unrealizLong + unrealizShort;

            decimal openOrderVolumeLong = 0;
            decimal openOrderVolumeShort = 0;

            for (int i = 0; i < _tab.PositionOpenerToStop.Count; i++)
            {
                if (_tab.PositionOpenerToStop[i].Side == Side.Buy)
                {
                    openOrderVolumeLong = _tab.PositionOpenerToStop[i].Volume;
                }
                else
                {
                    openOrderVolumeShort = _tab.PositionOpenerToStop[i].Volume;
                }
            }

            if (avrPriceLong != 0 && avrPriceShort != 0)
            {
                diffLongShort = avrPriceShort - avrPriceLong;
                diffLongShortPercent = Math.Round(diffLongShort / avrPriceShort * 100, 2);
            }

            _gridMonitoring.Rows[0].Cells[1].Value = GetDescription(_status); //статус
            if (_tab.Security != null)
            {
                _gridMonitoring.Rows[1].Cells[1].Value = _tab.Security.Name; //Торговая пара
            }
            _gridMonitoring.Rows[2].Cells[1].Value = _lastPrice; //Текущая цена
            _gridMonitoring.Rows[3].Cells[1].Value = wallet; //Кошелек
            _gridMonitoring.Rows[4].Cells[1].Value = Math.Round(_tab.Portfolio.ValueCurrent, 4);//Всего средств
            _gridMonitoring.Rows[5].Cells[1].Value = freeMoney;//Свободно средств
            _gridMonitoring.Rows[6].Cells[1].Value = Math.Round(_tab.Portfolio.ValueBlocked, 4);//Сумма депозита в работе
            _gridMonitoring.Rows[7].Cells[1].Value = "Объем: " + openVolumeLong + ", Ср.Цена: " + avrPriceLong + " ( " + deviationPriceLong + "% )";//Объем и цена позиции ЛОНГ (отклонение от рыночной цены в %)
            _gridMonitoring.Rows[8].Cells[1].Value = "Объем: " + openVolumeShort + ", Ср.Цена: " + avrPriceShort + " ( " + deviationPriceShort + "% )";//Объем и цена позиции ШОРТ (отклонение от рыночной цены в %)
            _gridMonitoring.Rows[9].Cells[1].Value = diffLongShort + " (" + diffLongShortPercent + "%)"; //Расстояние в процентах и пунтакх между открытыми позициями ЛОНГ и ШОРТ
            _gridMonitoring.Rows[10].Cells[1].Value = "Long: " + openOrderVolumeLong + ", Short: " + openOrderVolumeShort;//Состояние ордеров (выставленный объем в шорт и лонг)
            _gridMonitoring.Rows[11].Cells[1].Value = "Long: " + unrealizLong + ", Short: " + unrealizShort + ", Sum: " + unrealizPNL;//Нереализованный PNL
        }

        private decimal AveragePricePositions(List<Position> pos)
        {
            if (pos.Count == 0)
            {
                return 0;
            }

            if (_tab.Security == null)
            {
                return 0;
            }

            decimal openVolume = 0;
            decimal sum = 0;

            for (int i = 0; i < pos.Count; i++)
            {
                openVolume += pos[i].OpenVolume;
                sum += pos[i].OpenVolume * pos[i].EntryPrice;
            }

            if (openVolume == 0)
            {
                return 0;
            }

            return Math.Round(sum / openVolume, _tab.Security.Decimals);
        }

        private void _gridMonitoringButton_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0 && e.RowIndex == 0)
            {
                SendNewLogMessage("Нажата кнопка Запуск", _logType);
                PushStartButton();
            }
            if (e.ColumnIndex == 1 && e.RowIndex == 0)
            {
                PushStopButton();
            }
            if (e.ColumnIndex == 2 && e.RowIndex == 0)
            {
                PushCloseAndStopButton();
            }
            if (e.ColumnIndex == 3 && e.RowIndex == 0)
            {
                PushEqualizePositionsButton();
            }
        }

        #endregion

        #region Non trade periods

        public StrategyParameterString NonTradePeriod1OnOff;
        public StrategyParameterTimeOfDay NonTradePeriod1Start;
        public StrategyParameterTimeOfDay NonTradePeriod1End;

        public StrategyParameterString NonTradePeriod2OnOff;
        public StrategyParameterTimeOfDay NonTradePeriod2Start;
        public StrategyParameterTimeOfDay NonTradePeriod2End;

        public StrategyParameterString NonTradePeriod3OnOff;
        public StrategyParameterTimeOfDay NonTradePeriod3Start;
        public StrategyParameterTimeOfDay NonTradePeriod3End;

        public StrategyParameterString NonTradePeriod4OnOff;
        public StrategyParameterTimeOfDay NonTradePeriod4Start;
        public StrategyParameterTimeOfDay NonTradePeriod4End;

        public StrategyParameterString NonTradePeriod5OnOff;
        public StrategyParameterTimeOfDay NonTradePeriod5Start;
        public StrategyParameterTimeOfDay NonTradePeriod5End;

        public StrategyParameterBool TradeInMonday;
        public StrategyParameterBool TradeInTuesday;
        public StrategyParameterBool TradeInWednesday;
        public StrategyParameterBool TradeInThursday;
        public StrategyParameterBool TradeInFriday;
        public StrategyParameterBool TradeInSaturday;
        public StrategyParameterBool TradeInSunday;

        private bool IsBlockNonTradePeriods(DateTime curTime)
        {
            if (NonTradePeriod1OnOff.ValueString == "On")
            {
                if (NonTradePeriod1Start.Value < curTime
                 && NonTradePeriod1End.Value > curTime)
                {
                    return true;
                }

                if (NonTradePeriod1Start.Value > NonTradePeriod1End.Value)
                { // overnight transfer
                    if (NonTradePeriod1Start.Value > curTime
                        || NonTradePeriod1End.Value < curTime)
                    {
                        return true;
                    }
                }
            }

            if (NonTradePeriod2OnOff.ValueString == "On")
            {
                if (NonTradePeriod2Start.Value < curTime
                 && NonTradePeriod2End.Value > curTime)
                {
                    return true;
                }

                if (NonTradePeriod2Start.Value > NonTradePeriod2End.Value)
                { // overnight transfer
                    if (NonTradePeriod2Start.Value > curTime
                        || NonTradePeriod2End.Value < curTime)
                    {
                        return true;
                    }
                }
            }

            if (NonTradePeriod3OnOff.ValueString == "On")
            {
                if (NonTradePeriod3Start.Value < curTime
                 && NonTradePeriod3End.Value > curTime)
                {
                    return true;
                }

                if (NonTradePeriod3Start.Value > NonTradePeriod3End.Value)
                { // overnight transfer
                    if (NonTradePeriod3Start.Value > curTime
                        || NonTradePeriod3End.Value < curTime)
                    {
                        return true;
                    }
                }
            }

            if (NonTradePeriod4OnOff.ValueString == "On")
            {
                if (NonTradePeriod4Start.Value < curTime
                 && NonTradePeriod4End.Value > curTime)
                {
                    return true;
                }

                if (NonTradePeriod4Start.Value > NonTradePeriod4End.Value)
                { // overnight transfer
                    if (NonTradePeriod4Start.Value > curTime
                        || NonTradePeriod4End.Value < curTime)
                    {
                        return true;
                    }
                }
            }

            if (NonTradePeriod5OnOff.ValueString == "On")
            {
                if (NonTradePeriod5Start.Value < curTime
                 && NonTradePeriod5End.Value > curTime)
                {
                    return true;
                }

                if (NonTradePeriod5Start.Value > NonTradePeriod5End.Value)
                { // overnight transfer
                    if (NonTradePeriod5Start.Value > curTime
                        || NonTradePeriod5End.Value < curTime)
                    {
                        return true;
                    }
                }
            }

            if (TradeInMonday.ValueBool == false
                && curTime.DayOfWeek == DayOfWeek.Monday)
            {
                return true;
            }

            if (TradeInTuesday.ValueBool == false
                && curTime.DayOfWeek == DayOfWeek.Tuesday)
            {
                return true;
            }

            if (TradeInWednesday.ValueBool == false
                && curTime.DayOfWeek == DayOfWeek.Wednesday)
            {
                return true;
            }

            if (TradeInThursday.ValueBool == false
                && curTime.DayOfWeek == DayOfWeek.Thursday)
            {
                return true;
            }

            if (TradeInFriday.ValueBool == false
                && curTime.DayOfWeek == DayOfWeek.Friday)
            {
                return true;
            }

            if (TradeInSaturday.ValueBool == false
                && curTime.DayOfWeek == DayOfWeek.Saturday)
            {
                return true;
            }

            if (TradeInSunday.ValueBool == false
                && curTime.DayOfWeek == DayOfWeek.Sunday)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Trade Logic

        private decimal _lastPrice;
        private Status _status = Status.NoWork;
        private decimal _minVolume;
        private decimal _maxVolume;
        private List<OpenOrders> _openOrders = new List<OpenOrders>();

        private void _tab_CandleUpdateEvent(List<Candle> obj)
        {
            _lastPrice = obj[^1].Close;
        }

        private void _tab_OrderUpdateEvent(Order order)
        {
            SendNewLogMessage("OrderUpdateEvent: Type = " + order.PositionConditionType + ", Side = " + order.Side + ", Price = " + order.Price + ", Volume = " + order.Volume + ", State = " + order.State, _logType);

            if (order.State == OrderStateType.Done)
            {
                for (int i = 0; i < _openOrders?.Count; i++)
                {
                    if (order.PositionConditionType == OrderPositionConditionType.Close)
                    {
                        if (order.Side != _openOrders[i].Side)
                        {
                            if (order.Volume == _openOrders[i].Volume &&
                                order.PositionConditionType == _openOrders[i].Type)
                            {
                                _openOrders.RemoveAt(i);
                                SendNewLogMessage(WriteOpenOrders(), _logType);
                                i--;
                            }
                        }
                    }
                    else if (order.Volume == _openOrders[i].Volume &&
                        order.Side == _openOrders[i].Side &&
                        order.PositionConditionType == _openOrders[i].Type)
                    {
                        _openOrders.RemoveAt(i);
                        SendNewLogMessage(WriteOpenOrders(), _logType);
                        i--;
                    }
                }
            }

            if (order.State == OrderStateType.Done)
            {
                for (int i = 0; i < _tpOrders?.Count; i++)
                {
                    if (_tpOrders[i].NumberUser == order.NumberUser)
                    {
                        SendNewLogMessage($"Исполнился ТП ордер #{order.NumberUser}", _logType);
                        _needCheckExecuteTPFirstOrders = true;
                        _tpOrders.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        private bool _isConnected = false;

        private void MainThread()
        {
            while (true)
            {
                try
                {
                    if (!_isConnected)
                    {
                        if (_tab.CandlesFinishedOnly != null)
                        {
                            _isConnected = true;
                            AddFractalsToChart(_tab.CandlesFinishedOnly);
                        }
                    }

                    if (IsBlockNonTradePeriods(DateTime.Now))
                    {
                        EqualizePositions();
                        _status = Status.NonTradePeriod;
                    }
                    else
                    {
                        if (_status == Status.NonTradePeriod)
                        {
                            _status = Status.Start;
                            ContinueWork();
                        }
                    }

                    CheckStatus();

                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                }
            }
        }

        private void CheckStatus()
        {
            if (_status == Status.Start)
            {
                CheckPosition();
                TakeProfit();
            }

            if (_status == Status.Stop)
            {
                ChangeUpFractal();
                ChangeDownFractal();
            }

            if (_status == Status.CloseOrdersOnMarket)
            {
                CloseOrdersOnMarket();
            }

            if (_status == Status.EqualizePositions)
            {
                EqualizePositions();
            }
        }

        

        private void PushStartButton()
        {
            if (!CheckParameters())
            {
                SendNewLogMessage("Не введены параметры", Logging.LogMessageType.Error);
                return;
            }

            if (_upFractal == 0 || _downFractal == 0)
            {
                SendNewLogMessage("Нет данных по фракталам", Logging.LogMessageType.Error);
                return;
            }

            if (_lastPrice == 0)
            {
                SendNewLogMessage("Нет данных по цене инструмента", Logging.LogMessageType.Error);
                return;
            }

            SendNewLogMessage("--------------------------------------------------------------------------------------------------------------------------------", _logType);
            SendNewLogMessage("Включен режим Запуск", _logType);

            if (_tab.PositionOpenLong.Count > 0 && _tab.PositionOpenShort.Count > 0)
            {
                decimal openVolumeLong = GetOpenVolume(_tab.PositionOpenLong);
                decimal openVolumeShort = GetOpenVolume(_tab.PositionOpenShort);

                if (openVolumeLong == openVolumeShort)
                {
                    SendNewLogMessage("Запускаем продолжение торговли", _logType);
                    ContinueWork();
                    _status = Status.Start;
                    return;
                }
                else
                {
                    SendNewLogMessage("Запускаем восстановление торговли. Загружаем данные объемов.", _logType);
                    LoadData();
                    _status = Status.Start;
                    return;
                }
            }

            if (_tab.PositionOpenLong.Count > 0 || _tab.PositionOpenShort.Count > 0)
            {
                SendNewLogMessage("Запускаем восстановление торговли. Загружаем данные объемов.", _logType);
                LoadData();
                _status = Status.Start;
                return;
            }

            GetMinVolume();
            GetMaxVolume();

            if (_minVolume == 0 || _maxVolume == 0)
            {
                return;
            }

            SaveData();

            _status = Status.Start;

            if (_upFractal < _downFractal)
            {
                return;
            }

            if (_lastPrice < _downFractal || _lastPrice > _upFractal)
            {
                return;
            }

            StartCycle();
        }

        private bool CheckParameters()
        {
            if (_volume.ValueDecimal == 0)
            {
                return false;
            }

            if (_leverage.ValueDecimal == 0)
            {
                return false;
            }
            
            if (_minVolumeData.ValueDecimal == 0)
            {
                return false;
            }

            if (_positionIncreaseFactor.ValueDecimal == 0)
            {
                return false;
            }

            if (_indentationSize.ValueDecimal == 0)
            {
                return false;
            }

            return true;                      
        }

        private void PushStopButton()
        {
            SendNewLogMessage("Включен режим Стоп", _logType);
            _status = Status.Stop;

            _tab.BuyAtStopCancel();
            _tab.SellAtStopCancel();

            if (_tab.PositionOpenLong.Count > 0)
            {
                for (int i = 0; i < _tab.PositionOpenLong.Count; i++)
                {
                    _tab.PositionOpenLong[i].StopOrderIsActive = false;
                    _tab.PositionOpenLong[i].StopOrderPrice = 0;
                    _tab.PositionOpenLong[i].StopOrderRedLine = 0;                    
    }
            }

            if (_tab.PositionOpenShort.Count > 0)
            {
                for (int i = 0; i < _tab.PositionOpenShort.Count; i++)
                {
                    _tab.PositionOpenShort[i].StopOrderIsActive = false;
                    _tab.PositionOpenShort[i].StopOrderPrice = 0;
                    _tab.PositionOpenShort[i].StopOrderRedLine = 0;                    
                }
            }

            CancelTpOrders();

            _openOrders.Clear();
        }

        private void PushCloseAndStopButton()
        {
            _status = Status.CloseOrdersOnMarket;
            SendNewLogMessage("Включен режим Закрыть позиции по рынку и остановить", _logType);
        }

        private void PushEqualizePositionsButton()
        {
            _status = Status.EqualizePositions;
            SendNewLogMessage("Включен режим Уравнять позиции", _logType);
        }

        private void ContinueWork()
        {
            DateTime longTime = DateTime.MinValue;
            DateTime shortTime = DateTime.MinValue;

            if (_tab.PositionOpenLong[^1].CloseOrders?.Count > 0)
            {
                longTime = _tab.PositionOpenLong[^1].CloseOrders[^1].TimeCreate;
            }

            if (_tab.PositionOpenShort[^1].CloseOrders?.Count > 0)
            {
                shortTime = _tab.PositionOpenShort[^1].CloseOrders[^1].TimeCreate;
            }

            if (longTime > shortTime)
            {
                decimal volume = GetBuyOrderVolume();
                string message = "Продолжаем работу, ставим ордер на покупку";
                SendBuyOrder(volume, message);

                return;
            }
            else
            {
                decimal volume = GetSellOrderVolume();
                string message = "Продолжаем работу, ставим ордер на продажу";
                SendSellOrder(volume, message);
                return;
            }
        }

        private void CloseOrdersOnMarket()
        {
            _tab.BuyAtStopCancel();
            _tab.SellAtStopCancel();

            CancelTpOrders();

            _tab.CloseAllAtMarket();

            _status = Status.Stop;
            _openOrders.Clear();
        }

        private void EqualizePositions()
        {
            _tab.BuyAtStopCancel();
            _tab.SellAtStopCancel();

            decimal openVolumeLong = GetOpenVolume(_tab.PositionOpenLong);
            decimal openVolumeShort = GetOpenVolume(_tab.PositionOpenShort);

            if (openVolumeLong > openVolumeShort)
            {
                decimal volume = openVolumeLong - openVolumeShort;
                _tab.CloseAtMarket(_tab.PositionOpenLong[^1], volume);
            }
            else if (openVolumeShort > openVolumeLong)
            {
                decimal volume = openVolumeShort - openVolumeLong;
                _tab.CloseAtMarket(_tab.PositionOpenShort[^1], volume);
            }

            CancelTpOrders();

            _status = Status.Stop;
            _openOrders.Clear();
        }

        private void CheckPosition()
        {
            if (_needCheckExecuteTPSecondOrdersLong && _needCheckExecuteTPSecondOrdersShort) return;
            if (_needCheckExecuteTPFirstOrders) return;

            if (_status != Status.Start)
            {
                return;
            }

            if (_upFractal == 0 || _downFractal == 0)
            {
                return;
            }

            if (_minVolume == 0 || _maxVolume == 0)
            {
                return;
            }

            if (_upFractalIsChange)
            {
                ChangeUpFractal();
                return;
            }

            if (_downFractalIsChange)
            {
                ChangeDownFractal();
                return;
            }

            if (_tab.PositionOpenerToStop.Count == 0)
            {
                //Thread.Sleep(2000); // бывает так, что стоп ордер сработал, но данные с биржи по нему еще не пришли

                if (_openOrders.Count != 0) return;

                // запускаем новый цикл, когда нет открытых позиций и открытых стоп ордеров
                if (_tab.PositionOpenLong.Count == 0 && _tab.PositionOpenShort.Count == 0)
                {
                    if (_upFractal < _downFractal)
                    {
                        return;
                    }

                    if (_lastPrice < _downFractal || _lastPrice > _upFractal)
                    {
                        return;
                    }

                    PushStartButton();
                    return;
                }

                // отправляем ордер на покупку, если исполнился шорт, а лонга нет
                if (_tab.PositionOpenLong.Count == 0 && _tab.PositionOpenShort.Count > 0)
                {
                    if (_upFractal < _downFractal)
                    {
                        return;
                    }

                    bool isStopOrderActive = GetValueStopOrderActive(_tab.PositionOpenShort); // чтобы не не ставился окрывающий ордер, если есть стоп профит               

                    if (!isStopOrderActive)
                    {
                        decimal volume = GetBuyOrderVolume();
                        string message = "Отправляем ордер на покупку, если исполнился шорт, а лонга нет";
                        SendBuyOrder(volume, message);
                        return;
                    }
                }

                //отправляем ордер на продажу, если исполнился лонг, а шорта нет
                if (_tab.PositionOpenLong.Count > 0 && _tab.PositionOpenShort.Count == 0)
                {
                    if (_upFractal < _downFractal)
                    {
                        return;
                    }

                    bool isStopOrderActive = GetValueStopOrderActive(_tab.PositionOpenLong); // чтобы не не ставился окрывающий ордер, если есть стоп профит               

                    if (!isStopOrderActive)
                    {
                        decimal volume = GetSellOrderVolume();
                        string message = "Отправляем ордер на продажу, если исполнился лонг, а шорта нет";
                        SendSellOrder(volume, message);
                        return;
                    }
                }

                // когда обе позиции есть, и одна из них больше другой и нет стоп ордеров
                decimal openVolumeLong = GetOpenVolume(_tab.PositionOpenLong);
                decimal openVolumeShort = GetOpenVolume(_tab.PositionOpenShort);

                if (_tab.PositionOpenLong.Count > 0 && _tab.PositionOpenShort.Count > 0)
                {
                    if (openVolumeLong < openVolumeShort)
                    {
                        if (!GetValueStopOrderActive(_tab.PositionOpenShort))
                        {
                            if (!GetValueStopOrderActive(_tab.PositionOpenLong))
                            {
                                decimal volume = GetBuyOrderVolume();

                                if (volume + openVolumeLong <= _maxVolume)
                                {
                                    string message = $"Нет ордеров, есть лонг {openVolumeLong} и шорт {openVolumeShort}, лонг меньше шорта";
                                    SendBuyOrder(volume, message);
                                }

                                // когда набрана максимальная позиция в шорте, закрываем часть ее
                                if (openVolumeShort >= _maxVolume)
                                {
                                    SendNewLogMessage("Набрана максимальная позиция, закрываем частично Шорт", _logType);
                                    Position pos = GetCloseOrderVolume(_tab.PositionOpenShort);
                                    _tab.CloseAtStopMarket(pos, _upFractal);

                                    RemoveCloseOpenOrders(pos);
                                    _openOrders.Add(new OpenOrders(pos.Direction, _upFractal, pos.OpenVolume, OrderPositionConditionType.Close));
                                    SendNewLogMessage(WriteOpenOrders(), _logType);
                                    return;
                                }
                            }                            
                        }
                    }

                    if (openVolumeLong > openVolumeShort)
                    {
                        if (!GetValueStopOrderActive(_tab.PositionOpenLong))
                        {
                            if (!GetValueStopOrderActive(_tab.PositionOpenShort))
                            {
                                decimal volume = GetSellOrderVolume();

                                if (volume + openVolumeShort <= _maxVolume)
                                {
                                    string message = $"Нет ордеров, есть лонг {openVolumeLong} и шорт {openVolumeShort}, шорт меньше лонга";
                                    SendSellOrder(volume, message);
                                }

                                // когда набрана максимальная позиция
                                if (openVolumeLong >= _maxVolume)
                                {
                                    SendNewLogMessage("Набрана максимальная позиция, закрываем частично Лонг", _logType);
                                    Position pos = GetCloseOrderVolume(_tab.PositionOpenLong);
                                    _tab.CloseAtStopMarket(pos, _downFractal);

                                    RemoveCloseOpenOrders(pos);
                                    _openOrders.Add(new OpenOrders(pos.Direction, _downFractal, pos.OpenVolume, OrderPositionConditionType.Close));
                                    SendNewLogMessage(WriteOpenOrders(), _logType);
                                    return;
                                }
                            }                            
                        }
                    }
                }
            }

            // когда в начале цикла одна сторона исполняется, у другой увеличивается объем
            if (_tab.PositionOpenerToStop.Count > 0)
            {
                if (_tab.PositionOpenShort.Count > 0 && _tab.PositionOpenLong.Count == 0)
                {                    
                    decimal volume = GetBuyOrderVolume();

                    if (volume == 0)
                    {
                        return;
                    }

                    string message = "В начале цикла Шорт исполнился, у Лонга увеличиваем объем";
                    SendBuyOrder(volume, message);
                    return;
                }

                if (_tab.PositionOpenLong.Count > 0 && _tab.PositionOpenShort.Count == 0)
                {              
                    decimal volume = GetSellOrderVolume();

                    if (volume == 0)
                    {
                        return;
                    }

                    string message = "В начале цикла Лонг исполнился, у Шорта увеличиваем объем";
                    SendSellOrder(volume, message);
                    return;
                }
            }
        }

        private void ChangeUpFractal()
        {
            if (!_upFractalIsChange)
            {
                return;
            }

            if (_downFractal == 0)
            {
                return;
            }

            _upFractalIsChange = false;

            decimal openVolumeLong = GetOpenVolume(_tab.PositionOpenLong);
            decimal openVolumeShort = GetOpenVolume(_tab.PositionOpenShort);

            if (_tab.PositionOpenerToStop.Count == 0)
            {
                if (_tab.PositionOpenShort.Count > 0)
                {
                    // когда набрана максимальная позиция в шорте, закрываем часть ее
                    if (openVolumeShort >= _maxVolume)
                    {
                        SendNewLogMessage("Изменился верхний фрактал. Набрана максимальная позиция (" + openVolumeShort + ", закрываем частично Шорт ", _logType);
                        Position pos = GetCloseOrderVolume(_tab.PositionOpenShort);
                        _tab.CloseAtStopMarket(pos, _upFractal);

                        RemoveCloseOpenOrders(pos);
                        _openOrders.Add(new OpenOrders(pos.Direction, _upFractal, pos.OpenVolume, OrderPositionConditionType.Close));
                        SendNewLogMessage(WriteOpenOrders(), _logType);
                        return;
                    }

                    if (openVolumeShort > openVolumeLong)
                    {
                        if (_tab.PositionOpenShort[^1].EntryPrice > _upFractal)
                        {
                            SendNewLogMessage("Изменился верхний фрактал, перевыставляем Шорт на закрытие", _logType);
                            Position pos = GetCloseOrderVolume(_tab.PositionOpenShort);
                            _tab.CloseAtStopMarket(pos, _upFractal);

                            RemoveCloseOpenOrders(pos);
                            _openOrders.Add(new OpenOrders(pos.Direction, _upFractal, pos.OpenVolume, OrderPositionConditionType.Close));
                            SendNewLogMessage(WriteOpenOrders(), _logType);
                            return;
                        }
                    }
                }
            }

            for (int i = 0; i < _tab.PositionOpenerToStop.Count; i++)
            {
                if (_tab.PositionOpenerToStop[i].Side == Side.Buy)
                {
                    if (_tab.PositionOpenShort.Count > 0)
                    {
                        if (_tab.PositionOpenShort[^1].EntryPrice > _upFractal)
                        {
                            if (openVolumeShort > openVolumeLong)
                            {
                                SendNewLogMessage("Изменился верхний фрактал, цена Шорта больше фрактала, выставляем Шорт на закрытие", _logType);

                                _tab.BuyAtStopCancel();
                                _tab.SellAtStopCancel();

                                _openOrders.Clear();

                                Position pos = GetCloseOrderVolume(_tab.PositionOpenShort);
                                _tab.CloseAtStopMarket(pos, _upFractal);

                                RemoveCloseOpenOrders(pos);
                                _openOrders.Add(new OpenOrders(pos.Direction, _upFractal, pos.OpenVolume, OrderPositionConditionType.Close));
                                SendNewLogMessage(WriteOpenOrders(), _logType);
                                return;
                            }
                        }
                        else
                        {
                            string message = "Изменился верхний фрактал, переставляем Лонг";
                            decimal volume = GetBuyOrderVolume();
                            SendBuyOrder(volume, message);
                            return;
                        }
                    }
                    else
                    {
                        string message = "Изменился верхний фрактал, переставляем Лонг с минимальным объемом";
                        decimal volume = _minVolume;
                        SendBuyOrder(volume, message);
                        break;
                    }
                }
            }
        }

        private void ChangeDownFractal()
        {
            if (!_downFractalIsChange)
            {
                return;
            }

            _downFractalIsChange = false;

            if (_upFractal == 0)
            {
                return;
            }

            decimal openVolumeLong = GetOpenVolume(_tab.PositionOpenLong);
            decimal openVolumeShort = GetOpenVolume(_tab.PositionOpenShort);

            if (_tab.PositionOpenerToStop.Count == 0)
            {
                if (_tab.PositionOpenLong.Count > 0)
                {
                    // когда набрана максимальная позиция
                    if (openVolumeLong >= _maxVolume)
                    {
                        SendNewLogMessage("Изменился нижний фрактал. Набрана максимальная позиция, закрываем частично Лонг", _logType);
                        Position pos = GetCloseOrderVolume(_tab.PositionOpenLong);
                        _tab.CloseAtStopMarket(pos, _downFractal);

                        RemoveCloseOpenOrders(pos);
                        _openOrders.Add(new OpenOrders(pos.Direction, _downFractal, pos.OpenVolume, OrderPositionConditionType.Close));
                        SendNewLogMessage(WriteOpenOrders(), _logType);
                        return;
                    }

                    if (openVolumeLong > openVolumeShort)
                    {
                        if (_tab.PositionOpenLong[^1].EntryPrice < _downFractal)
                        {
                            SendNewLogMessage("Изменился нижний фрактал, перевыставляем Лонг на закрытие", _logType);
                            Position pos = GetCloseOrderVolume(_tab.PositionOpenLong);
                            _tab.CloseAtStopMarket(pos, _downFractal);

                            RemoveCloseOpenOrders(pos);
                            _openOrders.Add(new OpenOrders(pos.Direction, _downFractal, pos.OpenVolume, OrderPositionConditionType.Close));
                            SendNewLogMessage(WriteOpenOrders(), _logType);
                            return;
                        }
                    }
                }
            }

            for (int i = 0; i < _tab.PositionOpenerToStop.Count; i++)
            {
                if (_tab.PositionOpenerToStop[i].Side == Side.Sell)
                {
                    if (_tab.PositionOpenLong.Count > 0)
                    {
                        if (_tab.PositionOpenLong[^1].EntryPrice < _downFractal)
                        {
                            if (openVolumeLong > openVolumeShort)
                            {
                                SendNewLogMessage("Изменился нижний фрактал, цена Лонга меньше фрактала, выставляем Лонг на закрытие", _logType);

                                _tab.SellAtStopCancel();
                                _tab.BuyAtStopCancel();

                                _openOrders.Clear();

                                Position pos = GetCloseOrderVolume(_tab.PositionOpenLong);
                                _tab.CloseAtStopMarket(pos, _downFractal);

                                RemoveCloseOpenOrders(pos);
                                _openOrders.Add(new OpenOrders(pos.Direction, _downFractal, pos.OpenVolume, OrderPositionConditionType.Close));
                                SendNewLogMessage(WriteOpenOrders(), _logType);
                                break;
                            }
                        }
                        else
                        {
                            string message = "Изменился нижний фрактал, переставляем Шорт";
                            decimal volume = GetSellOrderVolume();
                            SendSellOrder(volume, message);
                            break;
                        }
                    }
                    else
                    {
                        string message = "Изменился нижний фрактал, переставляем Шорт с минимальным объемом";
                        decimal volume = _minVolume;
                        SendSellOrder(volume, message);
                        break;
                    }
                }
            }
        }

        private bool GetValueStopOrderActive(List<Position> positions)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i].StopOrderIsActive)
                {
                    return true;
                }

                for (int j = 0; j < _openOrders.Count; j++)
                {
                    if (_openOrders[j].Type == OrderPositionConditionType.Close &&
                        _openOrders[j].Side == positions[i].Direction &&
                        _openOrders[j].Volume == positions[i].OpenVolume)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Position GetCloseOrderVolume(List<Position> positions)
        {          
            return positions[^1];
        }

        private decimal GetBuyOrderVolume()
        {
            decimal openVolumeLong = GetOpenVolume(_tab.PositionOpenLong);
            decimal openVolumeShort = GetOpenVolume(_tab.PositionOpenShort);

            int decimalsVolume = 4;

            if (_startProgram == StartProgram.IsOsTrader)
            {
                decimalsVolume = _tab.Security.DecimalsVolume;
            }

            return Math.Round(openVolumeShort * _positionIncreaseFactor.ValueDecimal, decimalsVolume, MidpointRounding.AwayFromZero) - openVolumeLong;
        }

        private decimal GetSellOrderVolume()
        {
            decimal openVolumeLong = GetOpenVolume(_tab.PositionOpenLong);
            decimal openVolumeShort = GetOpenVolume(_tab.PositionOpenShort);

            int decimalsVolume = 4;

            if (_startProgram == StartProgram.IsOsTrader)
            {
                decimalsVolume = _tab.Security.DecimalsVolume;
            }

            return Math.Round(openVolumeLong * _positionIncreaseFactor.ValueDecimal, decimalsVolume, MidpointRounding.AwayFromZero) - openVolumeShort;
        }

        private decimal GetOpenVolume(List<Position> positions)
        {
            decimal volume = 0;

            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i].State != PositionStateType.Done)
                {
                    volume += positions[i].OpenVolume;
                }                
            }

            return volume;
        }

        private void GetMinVolume()
        {
            if (_lastPrice == 0)
            {
                return;
            }

            decimal minVolumeUSDT = _minVolumeData.ValueDecimal * _leverage.ValueDecimal;

            if (_typeMinVolume.ValueString == GetDescription(TypeVolume.Percent))
            {
                minVolumeUSDT = _tab.Portfolio.ValueCurrent * _minVolumeData.ValueDecimal / 100 * _leverage.ValueDecimal;
            }

            decimal volumeStep = _tab.Security.VolumeStep;
            int decimalsVolume = _tab.Security.DecimalsVolume;

            if (StartProgram == StartProgram.IsTester)
            {
                volumeStep = 0.1m;
                decimalsVolume = 4;
            }

            decimal minVolume = Math.Round(minVolumeUSDT / _lastPrice, decimalsVolume, MidpointRounding.AwayFromZero);
            
            _minVolume = Math.Round(minVolume / volumeStep, 0, MidpointRounding.AwayFromZero) * volumeStep;

            SendNewLogMessage("MinVolume = " + _minVolume, _logType);
        }

        private void GetMaxVolume()
        {
            if (_lastPrice == 0 || _minVolume == 0)
            {
                return;
            }

            decimal maxValue = _volume.ValueDecimal * _leverage.ValueDecimal - _tab.Portfolio.ValueBlocked;

            if (_typeVolume.ValueString == GetDescription(TypeVolume.Percent))
            {
                maxValue = _tab.Portfolio.ValueCurrent * _volume.ValueDecimal / 100 * _leverage.ValueDecimal - _tab.Portfolio.ValueBlocked;
            }

            maxValue = Math.Round(maxValue / _lastPrice, _tab.Security.DecimalsVolume, MidpointRounding.ToNegativeInfinity);

            int maxN = (int)(Math.Log((double)maxValue / (double)_minVolume, (double)_positionIncreaseFactor.ValueDecimal)) + 1;

            string strN = "";
            for (int i = 0; i < maxN; i++)
            {
                decimal result = Math.Round(_minVolume * (decimal)Math.Pow((double)_positionIncreaseFactor.ValueDecimal, i), _tab.Security.DecimalsVolume, MidpointRounding.AwayFromZero);

                int count = i + 1;
                strN += "v" + count + " = " + result + ", ";

                decimal volumeStep = _tab.Security.VolumeStep;

                if (StartProgram == StartProgram.IsTester)
                {
                    volumeStep = 0.1m;
                }

                if (result % volumeStep != 0)
                {
                    SendNewLogMessage("MaxValue = " + maxValue + ", maxN = " + maxN + ", MaxVolume = " + _maxVolume, _logType);
                    SendNewLogMessage(strN, _logType);
                    SendNewLogMessage("Инструмент имеет кратность объема (" + volumeStep + "). Невозможно создать логику с такими параметрами", Logging.LogMessageType.Error);
                    _maxVolume = 0;
                    return;
                }
            }

            _maxVolume = (decimal)((double)_minVolume * Math.Pow((double)_positionIncreaseFactor.ValueDecimal, maxN - 1));

            SendNewLogMessage("MaxValue = " + maxValue + ", maxN = " + maxN + ", MaxVolume = " + _maxVolume, _logType);
            SendNewLogMessage(strN, _logType);
        }

        private void StartCycle()
        {
            if (_tab.PositionsOpenAll.Count > 0)
            {
                return;
            }

            _tab.BuyAtStopCancel();
            _tab.SellAtStopCancel();

            _openOrders.Clear();

            decimal buyPriceRedLine = _upFractal + GetIndentationSize(_upFractal);
            decimal buyPrice = buyPriceRedLine - _tab.Security.PriceStep * 100;

            SendNewLogMessage("Ставим Buy ордер: " + buyPriceRedLine + ", Vol = " + _minVolume, _logType);

            _tab.BuyAtStopMarket(_minVolume, buyPrice, buyPriceRedLine, StopActivateType.HigherOrEqual, 0, null, PositionOpenerToStopLifeTimeType.NoLifeTime);
            _openOrders.Add(new OpenOrders(Side.Buy, buyPriceRedLine, _minVolume, OrderPositionConditionType.Open));
            SendNewLogMessage(WriteOpenOrders(), _logType);

            decimal sellPriceRedLine = _downFractal - GetIndentationSize(_downFractal);
            decimal sellPrice = sellPriceRedLine - _tab.Security.PriceStep * 100;

            SendNewLogMessage("Ставим Sell ордер: " + sellPriceRedLine + ", Vol = " + _minVolume, _logType);

            _tab.SellAtStopMarket(_minVolume, sellPrice, sellPriceRedLine, StopActivateType.LowerOrEqual, 0, null, PositionOpenerToStopLifeTimeType.NoLifeTime);
            _openOrders.Add(new OpenOrders(Side.Sell, sellPriceRedLine, _minVolume, OrderPositionConditionType.Open));
            SendNewLogMessage(WriteOpenOrders(), _logType);
        }

        private decimal GetIndentationSize(decimal price)
        {
            if (_indentIndicator.ValueString == GetDescription(TypeIndential.Percent))
            {
                return price * _indentationSize.ValueDecimal / 100;
            }

            return _indentationSize.ValueDecimal;
        }

        private void SendBuyOrder(decimal volume, string message = "")
        {
            decimal buyPriceRedLine = Math.Round(_upFractal + GetIndentationSize(_upFractal), _tab.Security.Decimals);
            decimal buyPrice = Math.Round(buyPriceRedLine - _tab.Security.PriceStep * 100, _tab.Security.Decimals);

            for (int i = 0; i < _openOrders?.Count; i++)
            {
                if (volume == _openOrders[i].Volume &&
                    Side.Buy == _openOrders[i].Side &&
                    _openOrders[i].Type == OrderPositionConditionType.Open &&
                    _openOrders[i].Price == buyPriceRedLine)
                {
                    return;
                }
            }

            _tab.BuyAtStopCancel();

            for (int i = 0; i < _openOrders.Count; i++)
            {
                if (Side.Buy == _openOrders[i].Side &&
                    _openOrders[i].Type == OrderPositionConditionType.Open)
                {
                    _openOrders.RemoveAt(i);
                    SendNewLogMessage(WriteOpenOrders(), _logType);
                    i--;
                }
            }

            if (message != "")
            {
                SendNewLogMessage(message, _logType);
            }

            SendNewLogMessage("Размещаем Buy ордер: " + buyPriceRedLine + ", Vol = " + volume, _logType);

            decimal openVolumeLong = GetOpenVolume(_tab.PositionOpenLong);
            decimal openVolumeShort = GetOpenVolume(_tab.PositionOpenShort);

            if (openVolumeLong == openVolumeShort)
            {
                if (openVolumeLong != 0 && openVolumeShort != 0)
                {
                    Position pos = _tab.PositionOpenLong[^1];
                    _tab.BuyAtStopMarketToPosition(pos, volume, buyPriceRedLine, StopActivateType.HigherOrEqual, 0, PositionOpenerToStopLifeTimeType.NoLifeTime);
                    _openOrders.Add(new OpenOrders(Side.Buy, buyPriceRedLine, volume, OrderPositionConditionType.Open));
                    SendNewLogMessage(WriteOpenOrders(), _logType);
                    return;
                }
            }

            _tab.BuyAtStopMarket(volume, buyPrice, buyPriceRedLine, StopActivateType.HigherOrEqual, 0, null, PositionOpenerToStopLifeTimeType.NoLifeTime);

            _openOrders.Add(new OpenOrders(Side.Buy, buyPriceRedLine, volume, OrderPositionConditionType.Open));
            SendNewLogMessage(WriteOpenOrders(), _logType);
        }

        private void SendSellOrder(decimal volume, string message = "")
        {
            decimal sellPriceRedLine = Math.Round(_downFractal - GetIndentationSize(_downFractal), _tab.Security.Decimals);
            decimal sellPrice = Math.Round(sellPriceRedLine - _tab.Security.PriceStep * 100, _tab.Security.Decimals);

            for (int i = 0; i < _openOrders?.Count; i++)
            {
                if (volume == _openOrders[i].Volume &&
                    Side.Sell == _openOrders[i].Side &&
                    _openOrders[i].Type == OrderPositionConditionType.Open &&
                    _openOrders[i].Price == sellPriceRedLine)
                {
                    return;
                }
            }

            _tab.SellAtStopCancel();

            for (int i = 0; i < _openOrders.Count; i++)
            {
                if (Side.Sell == _openOrders[i].Side &&
                    _openOrders[i].Type == OrderPositionConditionType.Open)
                {
                    _openOrders.RemoveAt(i);
                    SendNewLogMessage(WriteOpenOrders(), _logType);
                    i--;
                }
            }

            if (message != "")
            {
                SendNewLogMessage(message, _logType);
            }

            SendNewLogMessage("Переставляем шорт: " + sellPriceRedLine + ", Vol = " + volume, _logType);

            decimal openVolumeLong = GetOpenVolume(_tab.PositionOpenLong);
            decimal openVolumeShort = GetOpenVolume(_tab.PositionOpenShort);

            if (openVolumeLong == openVolumeShort)
            {
                if (openVolumeLong != 0 && openVolumeShort != 0)
                {
                    Position pos = _tab.PositionOpenShort[^1];
                    _tab.SellAtStopMarketToPosition(pos, volume, sellPriceRedLine, StopActivateType.LowerOrEqual, 0, PositionOpenerToStopLifeTimeType.NoLifeTime);
                    _openOrders.Add(new OpenOrders(Side.Sell, sellPriceRedLine, volume, OrderPositionConditionType.Open));
                    SendNewLogMessage(WriteOpenOrders(), _logType);
                    return;
                }
            }

            _tab.SellAtStopMarket(volume, sellPrice, sellPriceRedLine, StopActivateType.LowerOrEqual, 0, null, PositionOpenerToStopLifeTimeType.NoLifeTime);
            _openOrders.Add(new OpenOrders(Side.Sell, sellPriceRedLine, volume, OrderPositionConditionType.Open));
            SendNewLogMessage(WriteOpenOrders(), _logType);
        }

        private string WriteOpenOrders()
        {
            string str = "OpenOrders:";
            for (int i = 0; i < _openOrders?.Count; i++)
            {
                str += "\nType = " + _openOrders[i].Type + ", Side = " + _openOrders[i].Side + ", Price = " + _openOrders[i].Price + ", Vol = " + _openOrders[i].Volume;
            }

            return str;
        }

        private void RemoveCloseOpenOrders(Position pos)
        {

            for (int i = 0; i < _openOrders?.Count; i++)
            {
                if (pos.OpenOrders[0].Volume == _openOrders[i].Volume &&
                    pos.OpenOrders[0].Side == _openOrders[i].Side &&
                    _openOrders[i].Type == OrderPositionConditionType.Close)
                {
                    _openOrders.RemoveAt(i);
                    return;
                }
            }
        }

        #endregion

        #region Take Profit

        private decimal _openVolumeLong = 0;
        private decimal _openVolumeShort = 0;
        private decimal _priceLongAverage = 0;
        private decimal _priceShortAverage = 0;
        private bool _needCancelTakeProfitOrders = false;
        private bool _needCheckExecuteTPFirstOrders = false;
        private bool _needCheckExecuteTPFirstOrdersShort = false;
        private bool _needCheckExecuteTPSecondOrdersLong = false;
        private bool _needCheckExecuteTPSecondOrdersShort = false;
        private List<TakeProfitOrders> _tpOrders = new();

        private void TakeProfit()
        {
            if (!_onTakeProfit)
            {
                if (_tpOrders.Count > 0)
                {
                    CancelTpOrders();
                }

                return;
            }

            CheckPositionForTakeProfit();
            CheckCancelTakeProfitOrders();
            CheckExecuteFirstOrders();
            CheckExecuteSecondOrders();
        }

        private void CheckCancelTakeProfitOrders()
        {
            if (!_needCancelTakeProfitOrders) return;

            int count = 0;

            for (int i = 0; i < _tab.PositionOpenLong.Count; i++)
            {
                if (_tab.PositionOpenLong[i].CloseActive)
                {
                    count++;
                }
            }

            for (int i = 0; i < _tab.PositionOpenShort.Count; i++)
            {
                if (_tab.PositionOpenShort[i].CloseActive)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                _needCancelTakeProfitOrders = false;
                SendNewLogMessage("Ставим ТП ордер", _logType);
                SendTakeProfitOrders();
            }
        }

        private void CheckPositionForTakeProfit()
        {
            if (_needCheckExecuteTPSecondOrdersLong ||
                _needCheckExecuteTPSecondOrdersShort)
            {
                return;
            }

            if (_needCheckExecuteTPFirstOrders)
            {
                _openVolumeLong = 0;
                _openVolumeShort = 0;
                _priceLongAverage = 0;
                _priceShortAverage = 0;

                return;
            }

            decimal openVolumeLong = 0;
            decimal openVolumeShort = 0;
            decimal priceLongAverage = 0;
            decimal priceShortAverage = 0;
            decimal sumShort = 0;
            decimal sumLong = 0;

            for (int i = 0; i < _tab.PositionOpenLong.Count; i++)
            {
                openVolumeLong += _tab.PositionOpenLong[i].OpenVolume;
                //priceLongAverage += _tab.PositionOpenLong[i].EntryPrice;
                sumLong += _tab.PositionOpenLong[i].OpenVolume * _tab.PositionOpenLong[i].EntryPrice;
            }

            if (openVolumeLong != 0)
            {
                priceLongAverage = sumLong / openVolumeLong;
            }
            else
            {
                priceLongAverage = 0;
            }

            for (int i = 0; i < _tab.PositionOpenShort.Count; i++)
            {
                openVolumeShort += _tab.PositionOpenShort[i].OpenVolume;
                //priceShortAverage += _tab.PositionOpenShort[i].EntryPrice;
                sumShort += _tab.PositionOpenShort[i].OpenVolume * _tab.PositionOpenShort[i].EntryPrice;
            }

            if (openVolumeShort != 0)
            {
                priceShortAverage = sumShort / openVolumeShort;
            }
            else
            {
                priceShortAverage = 0;
            }

            if (openVolumeLong != _openVolumeLong ||
                openVolumeShort != _openVolumeShort)
            {
                if (openVolumeLong > openVolumeShort)
                {
                    _openVolumeLong = openVolumeLong;
                    _openVolumeShort = openVolumeShort;
                    _priceLongAverage = priceLongAverage;
                    _priceShortAverage = priceShortAverage;

                    SendNewLogMessage("Нужно открыть лонговый ТП ордер", _logType);
                    CancelTakeProfitOrders();
                }

                if (openVolumeShort > openVolumeLong)
                {
                    _openVolumeShort = openVolumeShort;
                    _openVolumeLong = openVolumeLong;
                    _priceShortAverage = priceShortAverage;
                    _priceLongAverage = priceLongAverage;

                    SendNewLogMessage("Нужно открыть шортовый ТП ордер", _logType);
                    CancelTakeProfitOrders();
                }
            }
        }

        private void CancelTakeProfitOrders()
        {
            SendNewLogMessage("Перед постановкой ТП ордера, отменяем открытые", _logType);

            for (int i = 0; i < _tab.PositionOpenLong.Count; i++)
            {
                if (_tab.PositionOpenLong[i].CloseActive)
                {
                    _tab.CloseOrder(_tab.PositionOpenLong[i].CloseOrders[^1]);
                }
            }

            for (int i = 0; i < _tab.PositionOpenShort.Count; i++)
            {
                if (_tab.PositionOpenShort[i].CloseActive)
                {
                    _tab.CloseOrder(_tab.PositionOpenShort[i].CloseOrders[^1]);
                }
            }

            _needCancelTakeProfitOrders = true;
            _tpOrders.Clear();
        }

        private void CancelTpOrders()
        {
            for (int i = 0; i < _tab.PositionOpenLong.Count; i++)
            {
                if (_tab.PositionOpenLong[i].CloseActive)
                {
                    _tab.CloseOrder(_tab.PositionOpenLong[i].CloseOrders[^1]);
                }
            }

            for (int i = 0; i < _tab.PositionOpenShort.Count; i++)
            {
                if (_tab.PositionOpenShort[i].CloseActive)
                {
                    _tab.CloseOrder(_tab.PositionOpenShort[i].CloseOrders[^1]);
                }
            }

            _openVolumeLong = 0;
            _openVolumeShort = 0;
            _priceLongAverage = 0;
            _priceShortAverage = 0;
            _tpOrders.Clear();
        }

        private void SendTakeProfitOrders()
        {
            // когда открыт только лонг
            if (_openVolumeLong > 0 && _openVolumeShort == 0)
            {
                for (int i = 0; i < _tab.PositionOpenLong.Count; i++)
                {
                    decimal price = _priceLongAverage + _priceLongAverage * _setTakeProfit / 100;
                    decimal volume = _tab.PositionOpenLong[i].OpenVolume;
                    _tab.CloseAtLimit(_tab.PositionOpenLong[i], price, volume);
                    _tpOrders.Add(new TakeProfitOrders(Side.Buy, price, volume, _tab.PositionOpenLong[i].CloseOrders[^1].NumberUser));
                    SendNewLogMessage($"Открыт лонг ордер по цене {_priceLongAverage}, выставляем тейк-профит по цене {price}", _logType);
                }
            }

            // когда открыт только шорт
            if (_openVolumeLong == 0 && _openVolumeShort > 0)
            {
                for (int i = 0; i < _tab.PositionOpenShort.Count; i++)
                {
                    decimal price = _priceShortAverage - _priceShortAverage * _setTakeProfit / 100;
                    decimal volume = _tab.PositionOpenShort[i].OpenVolume;
                    _tab.CloseAtLimit(_tab.PositionOpenShort[i], price, volume);
                    //_needCheckExecuteTPFirstOrdersShort = true;
                    //_numberUserTPOrder = _tab.PositionOpenShort[i].CloseOrders[^1].NumberUser;
                    _tpOrders.Add(new TakeProfitOrders(Side.Sell, price, volume, _tab.PositionOpenShort[i].CloseOrders[^1].NumberUser));
                    SendNewLogMessage($"Открыт шорт ордер по цене {_priceShortAverage}, выставляем тейк-профит по цене {price}", _logType);
                }
            }

            // когда открыты лонг и шорт
            if (_openVolumeLong > 0 && _openVolumeShort > 0)
            {
                decimal priceBreakeven = (_priceLongAverage * _openVolumeLong - _priceShortAverage * _openVolumeShort) / (_openVolumeLong - _openVolumeShort);
                decimal price = 0;

                if (_openVolumeLong > _openVolumeShort)
                {
                    price = priceBreakeven + priceBreakeven * _setTakeProfit / 100;
                    SendNewLogMessage($"Открыты лонг ср.цена - {_priceLongAverage}, vol={_openVolumeLong} и шорт ср.цена {_priceShortAverage}, vol={_openVolumeShort}, выставляем тейк-профит по цене {price}", _logType);

                    for (int i = 0; i < _tab.PositionOpenLong.Count; i++)
                    {
                        decimal volume = _tab.PositionOpenLong[i].OpenVolume;
                        _tab.CloseAtLimit(_tab.PositionOpenLong[i], price, volume);
                        //_needCheckExecuteTPFirstOrdersLong = true;
                        _tpOrders.Add(new TakeProfitOrders(Side.Buy, price, volume, _tab.PositionOpenLong[i].CloseOrders[^1].NumberUser));
                        SendNewLogMessage($"Открыт лонговый тейк-профит по цене {price}, vol = {volume}", _logType);
                    }
                }
                else
                {
                    price = priceBreakeven - priceBreakeven * _setTakeProfit / 100;
                    SendNewLogMessage($"Открыты лонг ср.цена - {_priceLongAverage}, vol={_openVolumeLong} и шорт ср.цена {_priceShortAverage}, vol={_openVolumeShort}, выставляем тейк-профит по цене {price}", _logType);

                    for (int i = 0; i < _tab.PositionOpenShort.Count; i++)
                    {
                        decimal volume = _tab.PositionOpenShort[i].OpenVolume;
                        _tab.CloseAtLimit(_tab.PositionOpenShort[i], price, volume);
                        //_needCheckExecuteTPFirstOrdersShort = true;
                        _tpOrders.Add(new TakeProfitOrders(Side.Sell, price, volume, _tab.PositionOpenShort[i].CloseOrders[^1].NumberUser));
                        SendNewLogMessage($"Открыт шортовый тейк-профит по цене {price}, vol = {volume}", _logType);
                    }
                }
            }
        }

        private void CheckExecuteFirstOrders()
        {
            if (_needCheckExecuteTPFirstOrders)
            {
                if (_tab.PositionOpenLong.Count == 0)
                {
                    if (_tab.PositionOpenShort.Count > 0)
                    {
                        if (_tpOrders.Count == 0)
                        {
                            for (int i = 0; i < _tab.PositionOpenShort.Count; i++)
                            {
                                decimal volume = _tab.PositionOpenShort[i].OpenVolume;
                                _tab.CloseAtMarket(_tab.PositionOpenShort[i], volume);                                
                            }

                            SendNewLogMessage($"Закрылась лонг позиция, закрываем шортовую позицию.", _logType);
                            _needCheckExecuteTPFirstOrders = false;
                            _needCheckExecuteTPSecondOrdersLong = true;

                            _tab.BuyAtStopCancel();
                            _tab.SellAtStopCancel();
                            _openOrders.Clear();
                            _tpOrders.Clear();

                            return;
                        }
                    }
                    else
                    {
                        SendNewLogMessage($"Закрылась лонг позиция по ТП", _logType);
                        _needCheckExecuteTPFirstOrders = false;

                        _tab.BuyAtStopCancel();
                        _tab.SellAtStopCancel();
                        _openOrders.Clear();
                        _tpOrders.Clear();

                        return;
                    }
                }

                if (_tab.PositionOpenShort.Count == 0)
                {
                    if (_tab.PositionOpenLong.Count > 0)
                    {
                        if (_tpOrders.Count == 0)
                        {
                            for (int i = 0; i < _tab.PositionOpenLong.Count; i++)
                            {
                                decimal volume = _tab.PositionOpenLong[i].OpenVolume;
                                _tab.CloseAtMarket(_tab.PositionOpenLong[i], volume);                                
                            }

                            SendNewLogMessage($"Закрылась шорт позиция, закрываем лонговую позицию.", _logType);
                            _needCheckExecuteTPFirstOrders = false;
                            _needCheckExecuteTPSecondOrdersShort = true;

                            _tab.BuyAtStopCancel();
                            _tab.SellAtStopCancel();
                            _openOrders.Clear();
                            _tpOrders.Clear();

                            return;
                        }
                    }
                    else
                    {
                        SendNewLogMessage($"Закрылась шорт позиция по ТП", _logType);
                        _needCheckExecuteTPFirstOrders = false;

                        _tab.BuyAtStopCancel();
                        _tab.SellAtStopCancel();
                        _openOrders.Clear();
                        _tpOrders.Clear();

                        return;
                    }
                }
            }            
        }

        private void CheckExecuteSecondOrders()
        {
            if (_needCheckExecuteTPSecondOrdersLong)
            {
                if (_tab.PositionOpenLong.Count == 0)
                {
                    _needCheckExecuteTPSecondOrdersLong = false;
                }
            }

            if (_needCheckExecuteTPSecondOrdersShort)
            {
                if (_tab.PositionOpenShort.Count == 0)
                {
                    _needCheckExecuteTPSecondOrdersShort = false;
                }
            }
        }

        #endregion

        #region Descriptions

        private enum Status
        {
            [Description("Запуск")]
            Start,
            [Description("Остановка")]
            Stop,
            [Description("Закрыть все позиции по рынку и остановить")]
            CloseOrdersOnMarket,
            [Description("Уравнять позиции")]
            EqualizePositions,
            [Description("Неторговый период")]
            NonTradePeriod,
            [Description("Не запущен")]
            NoWork
        }

        private enum TypeVolume
        {
            [Description("Фиксированный")]
            Fix,
            [Description("% от депозита")]
            Percent
        }

        private enum TypeIndential
        {
            [Description("Фиксированный")]
            Fix,
            [Description("% от цены")]
            Percent
        }

        public string GetDescription(Enum enumValue)
        {
            if (enumValue == null)
                return string.Empty;

            Type type = enumValue.GetType();
            MemberInfo[] memberInfo = type.GetMember(enumValue.ToString());

            if (memberInfo != null && memberInfo.Length > 0)
            {
                object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs != null && attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
                else
                {
                    return enumValue.ToString();
                }
            }

            return enumValue.ToString();
        }

        #endregion

        #region Save/Load

        private void LoadData()
        {
            if (!File.Exists(@"Engine\" + NameStrategyUniq + @"Data.txt"))
            {
                return;
            }

            try
            {              
                using (StreamReader reader = new StreamReader(@"Engine\" + NameStrategyUniq + @"Data.txt"))
                {                    
                    decimal.TryParse(reader.ReadLine().Split('|')[1].Replace('.', ','), out _minVolume);
                    decimal.TryParse(reader.ReadLine().Split('|')[1].Replace('.', ','), out _maxVolume);

                    reader.Close();

                    SendNewLogMessage("Загружены из файла: minVolume = " + _minVolume + ", maxVolume = " + _maxVolume, _logType);
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void SaveData()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Engine\" + NameStrategyUniq + @"Data.txt"))
                {
                    writer.WriteLine("minVolume|" + _minVolume);
                    writer.WriteLine("maxVolume|" + _maxVolume);

                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }
        #endregion

        #region Class

        private class OpenOrders 
        {            
            public decimal Volume;
            public decimal Price;
            public Side Side;
            public OrderPositionConditionType Type;

            public OpenOrders(Side side, decimal price, decimal volume, OrderPositionConditionType type) 
            {
                Side = side;
                Price = price;
                Volume = volume;
                Type = type;
            }
        }

        private class TakeProfitOrders
        {
            public decimal Volume;
            public decimal Price;
            public Side Side;
            public int NumberUser;

            public TakeProfitOrders(Side side, decimal price, decimal volume, int numberUser)
            {
                Side = side;
                Price = price;
                Volume = volume;
                NumberUser = numberUser;
            }
        }

        #endregion
    }
}

