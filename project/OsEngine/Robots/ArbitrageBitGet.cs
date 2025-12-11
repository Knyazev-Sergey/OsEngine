using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Drawing;
using System.Windows.Forms.Integration;
using System.Windows.Forms;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace OsEngine.Robots
{
    [Bot("ArbitrageBitGet")]
    public class ArbitrageBitGet : BotPanel
    {
        #region Constructor

        private BotTabSimple _tab1;
        private BotTabSimple _tab2;
        private StrategyParameterDecimal _setRatioForBuy;
        private StrategyParameterDecimal _setRatioForStop;
        private StrategyParameterDecimal _setRatioForCounterOrder;
        private StrategyParameterString _setCounterOrder;
        private StrategyParameterInt _setDelayCycle;
        private StrategyParameterDecimal _setOrderSizeForBestPrice;
        private StrategyParameterBool _setTradeOneSecurity;
        private Logging.LogMessageType _logging = Logging.LogMessageType.User;

        public ArbitrageBitGet(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab1 = TabsSimple[0];
            _tab1.ManualPositionSupport.DisableManualSupport();

            TabCreate(BotTabType.Simple);
            _tab2 = TabsSimple[1];
            _tab2.ManualPositionSupport.DisableManualSupport();

            this.ParamGuiSettings.Title = "ArbitrageBitGet";
            this.ParamGuiSettings.Height = 400;
            this.ParamGuiSettings.Width = 400;

            string tabName = " Параметры ";

            _setRatioForBuy = CreateParameter("Соотношение стоимости активов для выставления ордеров на покупку", 0m, 0m, 0m, 0m, tabName);
            _setRatioForStop = CreateParameter("Соотношение стоимости активов для остановки бота", 0m, 0m, 0m, 0m, tabName);
            _setRatioForCounterOrder = CreateParameter("Соотношение стоимости активов для удовлетворения встречных заявок", 0m, 0m, 0m, 0m, tabName);
            _setCounterOrder = CreateParameter("Брать заявку по встречной цене", "Off", new string[] { "Off", "On" }, tabName);
            _setDelayCycle = CreateParameter("Задержка перезапуска цикла, сек.", 30, 0, 0, 0, tabName);
            _setOrderSizeForBestPrice = CreateParameter("Размер ордера, меньше которого не встаем лучшей ценой", 0m, 0m, 0m, 0m, tabName);
            _setTradeOneSecurity = CreateParameter("Торговать только один инструмент", false, tabName);

            CustomTabToParametersUi customTabMonitoring = ParamGuiSettings.CreateCustomTab(" Мониторинг ");

            CreateTableMonitoring();
            customTabMonitoring.AddChildren(_hostMonitoring);

            CustomTabToParametersUi customTabGrid = ParamGuiSettings.CreateCustomTab(" Сетка ордеров ");

            CreateTableGrid();
            customTabGrid.AddChildren(_hostGrid);
            LoadTableGrid();

            Thread threadMonitoring = new Thread(ThreadRefreshMonitoring) { IsBackground = true };
            threadMonitoring.Start();

            Thread threadTradeLogic = new Thread(ThreadTradeLogic) { IsBackground = true };
            threadTradeLogic.Start();

            _tab1.BestBidAskChangeEvent += _tab1_BestBidAskChangeEvent;
            _tab2.BestBidAskChangeEvent += _tab2_BestBidAskChangeEvent;         
        }

        #endregion

        #region Monitoring

        private WindowsFormsHost _hostMonitoring;
        private Regime _regime = Regime.Off;
        private DataGridView _dgvButton;
        private DataGridView _dgvMonitoring;

        private void CreateTableMonitoring()
        {
            _hostMonitoring = new WindowsFormsHost();

            _dgvButton = GridButton();
            _dgvMonitoring = GridMonitoring();

            _dgvButton.CellClick += _dgvButton_CellClick;

            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 1;
            panel.RowCount = 2;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            panel.BackColor = Color.Black;
            panel.Controls.Add(_dgvButton, 0, 0);
            panel.Controls.Add(_dgvMonitoring, 1, 0);

            _hostMonitoring.Child = panel;
        }

        private void _dgvButton_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex == 0 && e.ColumnIndex == 0)
                {
                    if (_firstAsk == 0 || _firstBid == 0 || _secondAsk == 0 || _secondBid == 0)
                    {
                        SendNewLogMessage("Нет всех данных от биржи. Убедитесь, что есть соединение с биржей и выбраны оба инструмента", Logging.LogMessageType.Error);
                        return;
                    }

                    if (_regime == Regime.Off)
                    {
                        if (!CheckParameters())
                        {                            
                            return;
                        }

                        _needBuySecondSecurity = true;
                        _needDelayExecuteOrders = false;
                        _needBuyCounterOrders = false;
                        _needCancelOrders = false;
                        _needCheckCancelFirstOrder = false;
                        _needCheckSellFirstSecurity = false;
                        _needCheckSendOpenOrders = false;
                        _needQuoteOrdersSecondSecurity = false;
                        _needCheckDeposit = false;
                        _ratioTakenLastPositions = 0;

                        //_listOrders.Clear();

                        _regime = Regime.On;
                        _dgvButton[1, 0].Value = "Запущено";
                        
                        SendMessageFlags();
                    }
                    else if (_regime == Regime.On)
                    {
                        _regime = Regime.Shutdown;
                        _dgvButton[1, 0].Value = "Подготовка к остановке";
                        SendNewLogMessage("Робот выключен пользователем", _logging);

                        SendMessageFlags();
                    }
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private DataGridView GridButton()
        {
            DataGridView dgv =
               DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect,
               DataGridViewAutoSizeRowsMode.AllCells);

            dgv.Dock = DockStyle.Fill;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            dgv.ColumnCount = 2;

            foreach (DataGridViewColumn column in dgv.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            DataGridViewRow row = new DataGridViewRow();

            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[0].Value = "Включить/Выключить";
            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells[1].Value = "Остановлено";
            row.Cells[1].ReadOnly = true;

            dgv.Rows.Add(row);

            return dgv;
        }

        private DataGridView GridMonitoring()
        {
            DataGridView dgv =
                DataGridFactory.GetDataGridView(DataGridViewSelectionMode.CellSelect,
                DataGridViewAutoSizeRowsMode.AllCells);
            dgv.Dock = DockStyle.Fill;
            dgv.ScrollBars = ScrollBars.Both;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.GridColor = Color.Gray;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Bold | FontStyle.Italic);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgv.ColumnCount = 3;
            dgv.RowCount = 6;

            foreach (DataGridViewColumn column in dgv.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                column.DefaultCellStyle.SelectionBackColor = dgv.DefaultCellStyle.BackColor;
                column.DefaultCellStyle.SelectionForeColor = dgv.DefaultCellStyle.ForeColor;
                column.ReadOnly = true;
            }

            dgv.Columns[0].Width = 250;

            dgv.Columns[1].HeaderText = " ";
            dgv.Columns[2].HeaderText = " ";

            dgv[0, 0].Value = "Бид";
            dgv[0, 1].Value = "Аск";
            dgv[0, 2].Value = "Кросс-курс (по Бидам | по Аскам)";
            dgv[0, 3].Value = "Объем позиции в USDT";
            dgv[0, 4].Value = "Кросс-курс последней сделки";
            dgv[0, 5].Value = "Кросс-курс набранной позиции";

            return dgv;
        }

        private void ThreadRefreshMonitoring()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(1000);
                    RefreshMonitoring();
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                }
            }
        }

        private void RefreshMonitoring()
        {
            DataGridView dgv = _dgvMonitoring;

            if (_tab1 == null && _tab2 == null)
            {
                return;
            }

            if (_tab1.Security != null)
            {
                dgv.Columns[1].HeaderText = _tab1.Security.Name;
            }

            if (_tab2.Security != null)
            {
                dgv.Columns[2].HeaderText = _tab2.Security.Name;
            }

            dgv[1, 0].Value = _firstBid;
            dgv[2, 0].Value = _secondBid;
            dgv[1, 1].Value = _firstAsk;
            dgv[2, 1].Value = _secondAsk;

            decimal ratioBid = 0;
            decimal ratioAsk = 0;

            if (_firstBid != 0)
            {
                ratioBid = Math.Round(_secondBid / _firstBid, 6);
            }
            if (_firstAsk != 0)
            {
                ratioAsk = Math.Round(_secondAsk / _firstAsk, 6);
            }

            dgv[1, 2].Value = ratioBid;
            dgv[2, 2].Value = ratioAsk;
            dgv[1, 3].Value = _volumeFirstSecurity;
            dgv[2, 3].Value = _volumeSecondSecurity;
            dgv[1, 4].Value = _ratioTakenLastPositions;
            dgv[1, 5].Value = _ratioTakenPositions;
        }

        #endregion

        #region TableGrid

        private WindowsFormsHost _hostGrid;
        private DataGridView _dgvGrid;
        private List<TableGrid> _listTableGrid = new List<TableGrid> { new TableGrid(), new TableGrid(), new TableGrid(), new TableGrid(), new TableGrid() };

        private void CreateTableGrid()
        {
            _hostGrid = new WindowsFormsHost();

            DataGridView dgv =
                DataGridFactory.GetDataGridView(DataGridViewSelectionMode.CellSelect,
                DataGridViewAutoSizeRowsMode.AllCells);

            dgv.Dock = DockStyle.Fill;
            dgv.ScrollBars = ScrollBars.Both;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.GridColor = Color.Gray;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Bold | FontStyle.Italic);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgv.ColumnCount = 3;
            dgv.RowCount = 5;

            foreach (DataGridViewColumn column in dgv.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                column.ReadOnly = false;
            }

            dgv.Columns[0].HeaderText = "№";
            dgv.Columns[1].HeaderText = "Отклонение цены";
            dgv.Columns[2].HeaderText = "Объем ордера";

            dgv[0, 0].Value = "1";
            dgv[0, 1].Value = "2";
            dgv[0, 2].Value = "3";
            dgv[0, 3].Value = "4";
            dgv[0, 4].Value = "5";

            _hostGrid.Child = dgv;
            _dgvGrid = dgv;

            _dgvGrid.CellEndEdit += _dgvGrid_CellEndEdit;
        }

        private void _dgvGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                TableGrid list = new TableGrid();

                decimal.TryParse(_dgvGrid[1, e.RowIndex].Value?.ToString().Replace(".", ","), out list.DeviationPrice);
                decimal.TryParse(_dgvGrid[2, e.RowIndex].Value?.ToString().Replace(".", ","), out list.Volume);

                _listTableGrid[e.RowIndex] = list;

                SaveTableGrid();
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void LoadTableGrid()
        {
            try
            {
                string fileName = @"Engine\" + NameStrategyUniq + @"Grid.json";

                if (!File.Exists(fileName))
                {
                    return;
                }

                string json = File.ReadAllText(fileName);
                _listTableGrid = JsonConvert.DeserializeObject<List<TableGrid>>(json);

                for (int i = 0; i < _listTableGrid.Count; i++)
                {
                    _dgvGrid[1, i].Value = _listTableGrid[i].DeviationPrice;
                    _dgvGrid[2, i].Value = _listTableGrid[i].Volume;
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void SaveTableGrid()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_listTableGrid, Formatting.Indented);
                File.WriteAllText(@"Engine\" + NameStrategyUniq + @"Grid.json", json);
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        #endregion

        #region Trade Logic

        private decimal _firstAsk;
        private decimal _firstBid;
        private decimal _secondAsk;
        private decimal _secondBid;
        private decimal _ratio;
        private decimal _posUsdt;
        private decimal _posToken;
        private decimal _limitPriceRatio;
        private decimal _maxPriceInListOrders;
        private decimal _priceZeroLevel;
        private bool _needCheckSellFirstSecurity = false;
        private bool _needQuoteOrdersSecondSecurity = false;
        private DateTime _timeDelayExecuteOrder;
        private bool _needDelayExecuteOrders = false;
        private bool _needCancelOrders = false;
        private bool _needCheckSendOpenOrders = false;
        private bool _needBuyCounterOrders = false;
        private bool _needBuySecondSecurity = true;
        private bool _needCheckCancelFirstOrder = false;
        private List<ListOrders> _listOrders = new List<ListOrders>();
        private decimal _volumeFirstSecurity;
        private decimal _volumeSecondSecurity;
        private decimal _ratioTakenLastPositions;
        private decimal _ratioTakenPositions;
        private MarketDepth _mdSecond;
        private double _timeUpdateDepth;
        private DateTime _checkFlags = DateTime.UtcNow;

        private void _tab1_BestBidAskChangeEvent(decimal bid, decimal ask)
        {
            _firstAsk = Math.Round(ask, _tab1.Security.Decimals);
            _firstBid = Math.Round(bid, _tab1.Security.Decimals);
        }

        private void _tab2_BestBidAskChangeEvent(decimal bid, decimal ask)
        {
            _secondAsk = Math.Round(ask, _tab2.Security.Decimals);
            _secondBid = Math.Round(bid, _tab2.Security.Decimals);
        }

        private bool CheckParameters()
        {
            if (_setRatioForBuy == 0)
            {
                SendNewLogMessage("Не указан параметер \"Соотношение для покупки\"", Logging.LogMessageType.Error);
                return false;
            }
            if (_setRatioForCounterOrder == 0)
            {
                SendNewLogMessage("Не указан параметер \"Соотношение для встречных ордеров\"", Logging.LogMessageType.Error);
                return false;
            }
            if (_setRatioForStop == 0)
            {
                SendNewLogMessage("Не указан параметер \"Соотношение для остановки бота\"", Logging.LogMessageType.Error);
                return false;
            }
            if (_setOrderSizeForBestPrice == 0)
            {
                SendNewLogMessage("Не указан параметер \"Размер ордера для лучшей цены\"", Logging.LogMessageType.Error);
                return false;
            }

            SendNewLogMessage("----------------------------------------------------------------------------------------------", _logging);
            SendNewLogMessage("Робот запущен", _logging);

            string str = "Параметры:";
            str += "\nСоотношение для покупки: " + _setRatioForBuy.ValueDecimal;
            str += "\nСоотношение для остановки бота: " + _setRatioForStop.ValueDecimal;
            str += "\nСоотношение для встречных ордеров: " + _setRatioForCounterOrder.ValueDecimal;
            str += "\nБрать заявку по встречной цене: " + _setCounterOrder.ValueString;
            str += "\nЗадержка перезапуска цикла: " + _setDelayCycle.ValueInt;
            str += "\nРазмер ордера для лучшей цены: " + _setOrderSizeForBestPrice.ValueDecimal;
            str += "\nТорговать только один инструмент: " + _setTradeOneSecurity.ValueBool;

            SendNewLogMessage(str, _logging);

            return true;
        }

        private void ThreadTradeLogic()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(200);

                    if (_tab1 == null || _tab2 == null)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    if (_tab2.MarketDepth == null)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    if (!_tab2.IsConnected || !_tab2.IsReadyToTrade) 
                    {
                        Thread.Sleep(1000);
                        _needCancelOrders = true;
                        continue;
                    }

                    GetCurrentVolumes();

                    if (_regime == Regime.Off)
                    {
                        continue;
                    }

                    TradeLogic();
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                    Thread.Sleep(1000);
                }
            }
        }

        private void GetCurrentVolumes()
        {
            if (_tab1.PositionOpenShort.Count > 0)
            {
                _volumeFirstSecurity = Math.Round(_tab1.PositionOpenShort[0].MaxVolume * _tab1.PositionOpenShort[0].EntryPrice, 2);

                if (_tab1.PositionOpenShort[0].EntryPrice != 0 && _tab2.PositionOpenLong.Count > 0)
                {
                    if (_tab1.PositionOpenShort[0].MaxVolume > 0 && _tab2.PositionOpenLong[0].MaxVolume > 0)
                    {
                        _ratioTakenPositions = Math.Round(_tab2.PositionOpenLong[0].EntryPrice / _tab1.PositionOpenShort[0].EntryPrice, 6);
                    }
                }
            }
            else
            {
                _volumeFirstSecurity = 0;
                _ratioTakenLastPositions = 0;
                _ratioTakenPositions = 0;
            }

            if (_tab2.PositionOpenLong.Count > 0)
            {
                _volumeSecondSecurity = Math.Round(_tab2.PositionOpenLong[0].MaxVolume * _tab2.PositionOpenLong[0].EntryPrice, 2);
            }
            else
            {
                _volumeSecondSecurity = 0;
            }
        }

        private void TradeLogic()
        {            
            CheckExecuteFirstOrders();
            CheckStopBot();
            CheckCancelFirstOrder();
            CheckSendSecondOrders();
            CheckDelayCancelOrders();
            CheckCancelSecondOrder();                       
            CheckExecuteSecondOrders();            
            SellFirstSecurity();

            if (!CheckRegime())
            {
                return;
            }

            BuyCounterOrders();
            QuoterSecondOrders();
            BuySecondSecurity();

            PrintFlags();
        }

        private void PrintFlags()
        {
            if (_checkFlags.AddMinutes(1) > DateTime.UtcNow) return;

            _checkFlags = DateTime.UtcNow;

            SendNewLogMessage("*************************************************************************", _logging);
            SendMessageFlags();
            PrintMassive();
            PrintMD();
            SendNewLogMessage("*************************************************************************", _logging);
        }

        private bool CheckRegime()
        {
            if (_regime == Regime.Shutdown)
            {
                if (_needQuoteOrdersSecondSecurity || _needCheckSendOpenOrders)
                {
                    _needCancelOrders = true;
                    CheckCancelSecondOrder();
                    _dgvButton[1, 0].Value = "Подготовка к остановке";
                    SendNewLogMessage("Подготовка к остановке робота", _logging);

                    return false;
                }

                if (!_needCheckSendOpenOrders)
                {
                    if (!_needCancelOrders)
                    {
                        if (!_needCheckSellFirstSecurity)
                        {
                            _regime = Regime.Off;
                            _dgvButton[1, 0].Value = "Остановлено";
                            SendNewLogMessage("Робот остановлен", _logging);
                            
                            SendMessageFlags();

                            return false;
                        }
                    }
                }
            }
            
            return true;
        }

        private void SendMessageFlags()
        {
            string str = "";
            str += "_ratio: " + _ratio;
            str += "\n_posUsdt: " + _posUsdt;
            str += "\n_limitPriceRatio: " + _limitPriceRatio;
            str += "\n_maxPriceInListOrders: " + _maxPriceInListOrders;
            str += "\n_priceZeroLevel: " + _priceZeroLevel;
            str += "\n_needCheckSellFirstSecurity: " + _needCheckSellFirstSecurity;
            str += "\n_needQuoteOrdersSecondSecurity: " + _needQuoteOrdersSecondSecurity;
            str += "\n_needDelayExecuteOrders: " + _needDelayExecuteOrders;
            str += "\n_needCancelOrders: " + _needCancelOrders;
            str += "\n_needCheckSendOpenOrders: " + _needCheckSendOpenOrders;
            str += "\n_needBuyCounterOrders: " + _needBuyCounterOrders;
            str += "\n_needBuySecondSecurity: " + _needBuySecondSecurity;
            str += "\n_needCheckCancelFirstOrder: " + _needCheckCancelFirstOrder;
            str += "\n_volumeFirstSecurity: " + _volumeFirstSecurity;
            str += "\n_volumeSecondSecurity: " + _volumeSecondSecurity;
            str += "\n_ratioTakenPositions: " + _ratioTakenLastPositions;            

            SendNewLogMessage(str, _logging);
        }

        private void CheckSendSecondOrders()
        {
            if (!_needCheckSendOpenOrders)
            {
                return;
            }

            if (_needQuoteOrdersSecondSecurity)
            {
                return;
            }

            if (_needCancelOrders)
            {
                _needCheckSendOpenOrders = false;
                return;
            }

            if (_tab2.PositionOpenLong.Count == 0) return;

            List<Order> openOrders = _tab2.PositionOpenLong[0].OpenOrders;

            if (openOrders.Count == 0)
            {
                return;
            }

            int countOpenOrders = openOrders.Count - 100;

            if (countOpenOrders < 0)
            {
                countOpenOrders = 0;
            }

            int count = 0;

            for (int j = openOrders.Count - 1; j >= countOpenOrders; j--)
            {
                for (int i = 0; i < _listOrders.Count; i++)
                {      
                    if (_listOrders[i].State == OrderStateType.Done)
                    {
                        continue;
                    }

                    if (_listOrders[i].NumberUser == openOrders[j].NumberUser.ToString())
                    {
                        _listOrders[i].State = openOrders[j].State;

                        if (openOrders[j].State != OrderStateType.Active)
                        {
                            count++;
                        }

                        if (openOrders[j].State == OrderStateType.Fail)
                        {
                            _needCancelOrders = true;
                            return;
                        }
                    }
                }
            }

            if (count == 0)
            {
                if (!_needBuyCounterOrders)
                {
                    _needQuoteOrdersSecondSecurity = true;
                }
                
                _needCheckSendOpenOrders = false;

                DateTime utcNow = DateTime.UtcNow;
                TimeSpan utcTimeSinceMidnight = utcNow - utcNow.Date;
                _timeUpdateDepth = utcTimeSinceMidnight.TotalMilliseconds;
            }
        }

        private DateTime _timeCheckCancelOrders;

        private void CheckCancelSecondOrder()
        {
            if (!_needCancelOrders)
            {
                return;
            }

            List<Order> openOrders = _tab2.PositionsLast.OpenOrders;

            int count = 0;

            int countOpenOrders = openOrders.Count - 100;

            if (countOpenOrders < 0)
            {
                countOpenOrders = 0;
            }

            for (int i = 0; i < _listOrders.Count; i++)
            {
                for (int j = openOrders.Count - 1; j >= countOpenOrders; j--)
                {
                    if (openOrders[j].State == OrderStateType.Active ||
                        openOrders[j].State == OrderStateType.Partial ||
                        openOrders[j].State == OrderStateType.Fail ||
                        openOrders[j].State == OrderStateType.None)
                    {
                        if (_listOrders[i].NumberUser == openOrders[j].NumberUser.ToString())
                        {
                            if (_listOrders[i].State != OrderStateType.Cancel)
                            {
                                SendNewLogMessage($"Отменяем ордер №{openOrders[j].NumberUser}, Price = {openOrders[j].Price}, Volume = {openOrders[j].Volume}", _logging);
                                _tab2.CloseOrder(_tab2.PositionsLast.OpenOrders[j]);                      
                                _listOrders[i].State = OrderStateType.Cancel;
                                count++;
                                _timeCheckCancelOrders = DateTime.UtcNow;
                                break;
                            }
                        }
                    }

                    if (openOrders[j].State != OrderStateType.Done &&
                        openOrders[j].State != OrderStateType.Cancel &&
                        openOrders[j].State != OrderStateType.Fail &&
                        openOrders[j].State != OrderStateType.None)
                    {
                        count++;
                        break;                      
                    }
                }
            }

            if (_timeCheckCancelOrders.AddSeconds(10) < DateTime.UtcNow)
            {
                for (int j = openOrders.Count - 1; j >= countOpenOrders; j--)
                {
                    if (openOrders[j].State == OrderStateType.Active ||
                        openOrders[j].State == OrderStateType.Partial ||
                        openOrders[j].State == OrderStateType.Fail ||
                        openOrders[j].State == OrderStateType.None)
                    {
                        SendNewLogMessage($"Повторяем аварийную отмену ордера №{openOrders[j].NumberUser}", _logging);
                        _tab2.CloseOrder(_tab2.PositionsLast.OpenOrders[j]);
                        _timeCheckCancelOrders = DateTime.UtcNow;
                        count++;
                    }
                }
            }

            if (count == 0)
            {
                _needCancelOrders = false;
                _needQuoteOrdersSecondSecurity = false;
                _needDelayExecuteOrders = false;
                _needCheckSendOpenOrders = false;
                _needBuyCounterOrders = false;
                _needBuySecondSecurity = true;
                _listOrders.Clear();

                DateTime utcNow = DateTime.UtcNow;
                TimeSpan utcTimeSinceMidnight = utcNow - utcNow.Date;
                _timeUpdateDepth = utcTimeSinceMidnight.TotalMilliseconds;
            }
        }

        private void UpdateRatio()
        {
            if (_firstBid != 0)
            {
                _ratio = Math.Round(_secondBid / _firstBid, 5);
                _limitPriceRatio = Math.Round(_firstBid * _setRatioForBuy, 2);
            }
        }

        private void CheckStopBot()
        {
            if (!_needBuySecondSecurity) return;

            if (_regime == Regime.Shutdown)
            {
                return;
            }

            if (_ratioTakenLastPositions > _setRatioForStop)
            {
                decimal firstPrice = _tab1.PositionOpenShort[0].EntryPrice;
                decimal secondPrice = _tab2.PositionOpenLong[0].EntryPrice;

                SendNewLogMessage($"Ср. цена ордера первого инструмента = {firstPrice}, Ср. цена ордера второго инструмента = {secondPrice}, Соотношение активов = {Math.Round(secondPrice/firstPrice, 6)}", _logging);
                SendNewLogMessage($"Соотношение активов ({_ratioTakenLastPositions}) превышает установленное значение ({_setRatioForStop.ValueDecimal}). Останавливаем робота.", _logging);
                _regime = Regime.Shutdown;
                return;
            }
        }

        private bool _sendMessage = false;

        private void BuyCounterOrders()
        {
            if (_setCounterOrder == "Off") return;            
            if (_needCheckSellFirstSecurity) return;
            if (_needCancelOrders) return;
            if (_needCheckSendOpenOrders) return;
            
            if (_needBuyCounterOrders)
            {
                if (_listOrders[0].Price <= (decimal)_tab2.MarketDepth.Bids[0].Price)
                {
                    SendNewLogMessage($"Заявка по встречному ордеру встала в Бид, отменяем этот ордер", _logging);
                    _needCancelOrders = true;
                }

                return;
            }

            decimal ratio = Math.Round(_secondAsk / _firstBid, 5);

            if (_setTradeOneSecurity)
            {
                if (_setRatioForCounterOrder <= (decimal)_tab2.MarketDepth.Asks[0].Price)
                {
                    _needBuySecondSecurity = true;
                    return;
                }
                else
                {
                    if (_needQuoteOrdersSecondSecurity)
                    {
                        _needCancelOrders = true;
                        return;
                    }
                }
            }
            else
            {
                if (_setRatioForCounterOrder < ratio)
                {
                    _needBuySecondSecurity = true;
                    return;
                }
                else
                {
                    if (_needQuoteOrdersSecondSecurity)
                    {
                        _needCancelOrders = true;
                        return;
                    }                    
                }
            }

            decimal summOrders = (decimal)_tab2.MarketDepth.Asks[0].Ask * (decimal)_tab2.MarketDepth.Asks[0].Price;
            decimal volume = 0;

            if (CheckDepositForOrders(summOrders))
            {
                volume = Math.Round(summOrders / _secondAsk, _tab2.Security.DecimalsVolume, MidpointRounding.ToZero);

                if (volume == 0)
                {                   
                    return;
                }
                                
                //SendNewLogMessage($"Лучшая цена Аск: {_secondAsk}, объем Аск: {_tab2.MarketDepth.Asks[0].Ask}", _logging);
                //SendNewLogMessage($"Депозита хватает на весь Аск, покупаем {volume} лотов, по цене {_secondAsk}, сумма ордера = {Math.Round(volume * _secondAsk, 2)}", _logging);               
            }
            else
            {                
                if (_posToken < 2)
                {
                    if (!_setTradeOneSecurity)
                    {
                        if (!_needCheckDeposit)
                        {
                            _needCheckDeposit = true;
                            SendNewLogMessage($"Не хватает {_tab1.Security.Name} ({_posToken}USDT) чтобы приступить к покупке встречных заявок. (нужно эквивалент {summOrders}USDT)", _logging);
                            //_regime = Regime.Shutdown;
                        }
                        
                        return;
                    }
                }

                if (_posUsdt < 2)
                {
                    if (!_needCheckDeposit)
                    {
                        _needCheckDeposit = true;
                        SendNewLogMessage($"Не хватает USDT (нужно {summOrders}USDT, а на депозите{_posUsdt}USDT) на покупку встречных заявок.", _logging);
                        //_regime = Regime.Shutdown;
                    }
                    return;
                }

                if (_setTradeOneSecurity)
                {                    
                    volume = Math.Round(_posUsdt / _secondAsk, _tab2.Security.DecimalsVolume, MidpointRounding.ToZero);
                }
                else
                {
                    if (_posUsdt < _posToken)
                    {
                        volume = Math.Round(_posUsdt / _secondAsk, _tab2.Security.DecimalsVolume, MidpointRounding.ToZero);
                        //SendNewLogMessage($"Рассчитываем объем по USDT = {_posUsdt}", _logging);
                    }
                    else
                    {
                        volume = Math.Round(_posToken / _secondAsk, _tab2.Security.DecimalsVolume, MidpointRounding.ToZero);
                        //SendNewLogMessage($"Рассчитываем объем по {_tab1.Security.Name} = {_posToken}", _logging);
                    }
                }

                if (volume == 0)
                {
                    if (!_needCheckDeposit)
                    {
                        _needCheckDeposit = true;
                        SendNewLogMessage($"Депозита не хватает на покупку встречных заявок.", _logging);
                        //_regime = Regime.Shutdown;
                    }
                    return;
                }                
            }

            /*if (volume * _secondAsk < 2)
            {
                if (!_sendMessage)
                {
                    _sendMessage = true;
                    SendNewLogMessage($"Объем в аске ({summOrders}) меньше чем 2 USDT", _logging);
                }
                return;
            }*/

            _sendMessage = false;

            if (_listOrders.Count > 0)
            {
                SendNewLogMessage($"Нужно забрать встречный ордер, но есть открытые ордера, нужно их отменить", _logging);
                PrintMassive();
                _needCancelOrders = true;
                return;
            }

            SendNewLogMessage($"Лучшая цена Аск: {_secondAsk}, объем Аск: {_tab2.MarketDepth.Asks[0].Ask}", _logging);
            SendNewLogMessage($"Покупаем с Аска {volume} лотов, по цене {_secondAsk}, сумма ордера: {Math.Round(volume * _secondAsk, 2)}", _logging);

            SendNewLogMessage($"FirstBid: {_firstBid}, SecondAsk: {_secondAsk}, Ratio: {ratio}, SetRatio: {_setRatioForCounterOrder.ValueDecimal}", _logging);
            SendNewLogMessage($"Deposit USDT = {_posUsdt}", _logging);
            SendNewLogMessage($"Deposit {_tab1.Security.Name} в пересчете на USDT: {_posToken}", _logging);

            _needCheckDeposit = false;
            SendOrderBuy(_secondAsk, volume);
            _listOrders = new List<ListOrders>();
            _listOrders.Add(new ListOrders { Price = _secondAsk, Volume = volume, NumberUser = _tab2.PositionsLast.OpenOrders[^1].NumberUser.ToString() });

            _needBuyCounterOrders = true;
            _needQuoteOrdersSecondSecurity = false;
            _needBuySecondSecurity = false;
            _needCheckSendOpenOrders = true;
        }

        private void QuoterSecondOrders()
        {
            if (!_needQuoteOrdersSecondSecurity) return;
            if (_needDelayExecuteOrders) return;
            if (_needCancelOrders) return;
            if (_regime == Regime.Shutdown) return;

            // проверка изменения стакана
            CheckChangeDepth();
        }

        private void CheckExecuteSecondOrders()
        {
            try
            {
                if (_tab2.PositionOpenLong.Count == 0) return;

                List<Order> openOrders = _tab2.PositionOpenLong[0].OpenOrders;

                for (int i = 0; i < _listOrders.Count; i++)
                {
                    if (_listOrders[i].State == OrderStateType.Done)
                    {
                        continue;
                    }

                    int countOpenOrders = openOrders.Count - 15;

                    if (countOpenOrders < 0)
                    {
                        countOpenOrders = 0;
                    }

                    for (int j = openOrders.Count - 1; j >= countOpenOrders; j--)
                    {
                        if (_listOrders[i].NumberUser == openOrders[j].NumberUser.ToString())
                        {
                            // частичное исполнение ордера
                            if (openOrders[j].VolumeExecute > 0 &&
                                openOrders[j].VolumeExecute != openOrders[j].Volume)
                            {
                                if (_listOrders[i].VolumeExecute != openOrders[j].VolumeExecute)
                                {
                                    if (_listOrders[i].State != OrderStateType.Done)
                                    {
                                        if (_setTradeOneSecurity)
                                        {
                                            _needDelayExecuteOrders = true;
                                            _timeDelayExecuteOrder = DateTime.UtcNow;
                                        }

                                        _listOrders[i].VolumeExecute = openOrders[j].VolumeExecute;

                                        SendNewLogMessage($"Частично исполнился ордер: {openOrders[j].NumberUser}, Price: {openOrders[j].Price}, VolEx: {openOrders[j].VolumeExecute}", _logging);                                                                             
                                    }
                                }
                            }

                            // полное исполнения ордера
                            if (openOrders[j].VolumeExecute > 0 &&
                                openOrders[j].VolumeExecute == openOrders[j].Volume)
                            {
                                if (_listOrders[i].VolumeExecute != openOrders[j].VolumeExecute)
                                {
                                    if (_listOrders[i].State != OrderStateType.Done)
                                    {
                                        _listOrders[i].State = OrderStateType.Done;

                                        if (_setTradeOneSecurity)
                                        {
                                            _needDelayExecuteOrders = true;
                                            _timeDelayExecuteOrder = DateTime.UtcNow;
                                        }

                                        SendNewLogMessage($"Ордер исполнился: {openOrders[j].NumberUser}, Price: {openOrders[j].Price}, VolEx: {openOrders[j].VolumeExecute}", _logging);
                                    
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }
                
        private void CancelAllOpenOrders()
        {
            SendNewLogMessage("Отменяем все открытые ордера", _logging);
            _needCancelOrders = true;
        }

        private void CheckChangeDepth()
        {
            UpdateRatio();

            // если цена ордера в сетке больше чем коэффициент
            if (!_setTradeOneSecurity &&
                _limitPriceRatio < _maxPriceInListOrders)
            {
                SendNewLogMessage($"Наибольшая цена в сетке ({_maxPriceInListOrders}) больше, чем уставновленное соотношение ({_limitPriceRatio})", _logging);
                CancelAllOpenOrders();
                return;
            }
            
            CheckMD();
        }

        private void CheckMD()
        {
            if (_priceZeroLevel == 0) return;

            _mdSecond = _tab2.MarketDepth;

            if ((decimal)_mdSecond.Bids[0].Price < _priceZeroLevel) return;

            if (_timeUpdateDepth > _mdSecond.Time.TimeOfDay.TotalMilliseconds &&
                _timeUpdateDepth + 5000 > DateTime.UtcNow.TimeOfDay.TotalMilliseconds) return;

            if ((decimal)_mdSecond.Bids[^1].Price >= _priceZeroLevel)
            {
                SendNewLogMessage($"Цена ордера ({_priceZeroLevel}) вне массива стакана (мин цена в стакане = {_mdSecond.Bids[^1].Price}, отменяем ордера", _logging);
                CancelAllOpenOrders();
                return;
            }

            for (int i = 0; i < _mdSecond.Bids.Count; i++)
            {
                MarketDepthLevel bid = _mdSecond.Bids[i];

                if ((decimal)bid.Price > _limitPriceRatio) continue;

                if ((decimal)bid.Bid > _setOrderSizeForBestPrice)
                {
                    ListOrders orderByPrice = _listOrders.Find(o => o.Price == (decimal)bid.Price);

                    if (orderByPrice != null)
                    {
                        if ((decimal)bid.Bid - orderByPrice.Volume >= _setOrderSizeForBestPrice)
                        {
                            if ((decimal)bid.Price > _priceZeroLevel)
                            {
                                SendNewLogMessage($"Цена из массива ({orderByPrice.Price}) равна цене в уровне бида ({bid.Price})", _logging);
                                SendNewLogMessage($"Изменился стакан, нулевой ордер можно переставить выше. MD: Price = {Math.Round(bid.Price, _tab2.Security.Decimals)}, Vol = {bid.Bid}, zeroPrice = {_priceZeroLevel}", _logging);
                                PrintMD();
                                CancelAllOpenOrders();
                                return;
                            }

                            if ((decimal)bid.Price < _priceZeroLevel)
                            {
                                if ((decimal)bid.Price < _priceZeroLevel - _tab2.Security.PriceStep)
                                {
                                    SendNewLogMessage($"Цена из массива ({orderByPrice.Price}) равна цене в уровне бида ({bid.Price})", _logging);
                                    SendNewLogMessage($"Изменился стакан, нулевой ордер можно переставить ниже. MD: Price = {Math.Round(bid.Price, _tab2.Security.Decimals)}, Vol = {bid.Bid}, zeroPrice = {_priceZeroLevel}", _logging);
                                    PrintMD();
                                    CancelAllOpenOrders();
                                    return;
                                }
                                else if ((decimal)bid.Price == _priceZeroLevel - _tab2.Security.PriceStep)
                                {
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        if ((decimal)bid.Price > _priceZeroLevel)
                        {
                            SendNewLogMessage($"Изменился стакан, нулевой ордер можно переставить выше. MD: Price = {Math.Round(bid.Price, _tab2.Security.Decimals)}, Vol = {bid.Bid}, zeroPrice = {_priceZeroLevel}", _logging);
                            PrintMD();
                            CancelAllOpenOrders();
                            return;
                        }

                        if ((decimal)bid.Price < _priceZeroLevel)
                        {
                            if ((decimal)bid.Price < _priceZeroLevel - _tab2.Security.PriceStep)
                            {
                                SendNewLogMessage($"Изменился стакан, нулевой ордер можно переставить ниже. MD: Price = {Math.Round(bid.Price, _tab2.Security.Decimals)}, Vol = {bid.Bid}, zeroPrice = {_priceZeroLevel}", _logging);
                                PrintMD();
                                CancelAllOpenOrders();
                                return;
                            }
                            else if ((decimal)bid.Price == _priceZeroLevel - _tab2.Security.PriceStep)
                            {
                                return;
                            }
                        }
                    }                    
                }                       
            }
        }

        private void PrintMassive()
        {
            string strMsg = "Massive orders: ";

            for (int i = 0; i < _listOrders.Count; i++)
            {
                strMsg += $"\nNumber: {_listOrders[i].NumberUser}, Price: {_listOrders[i].Price}, Volume: {_listOrders[i].Volume}, Dev.Price: {_listOrders[i].DeviationPrice}, State: {_listOrders[i].State}";
            }

            SendNewLogMessage(strMsg, _logging);
        }

        private void PrintMD()
        {
            string str = "Market Depth Bids: ";

            List<MarketDepthLevel> bids = _tab2.MarketDepth.Bids;

            for (int i = 0; i < bids.Count; i++)
            {
                str += $"\nPrice: {Math.Round(bids[i].Price, _tab2.Security.Decimals)}, Vol: {bids[i].Bid}";
            }

            str += $"\n_timeUpdateDepth = {_timeUpdateDepth}, время обновления стакана = {_mdSecond.Time.TimeOfDay.TotalMilliseconds}, текущее время = {DateTime.UtcNow.TimeOfDay.TotalMilliseconds}";
            SendNewLogMessage(str, _logging);
        }

        private void CheckDelayCancelOrders()
        {
            if (!_needDelayExecuteOrders)
            {
                return;
            }

            if (_needCancelOrders)
            {
                return;
            }

            if (_timeDelayExecuteOrder.AddSeconds(_setDelayCycle) > DateTime.UtcNow)
            {
                return;
            }

            SendNewLogMessage("Отменяем ордера по таймеру", _logging);

            _needDelayExecuteOrders = false;

            CancelAllOpenOrders();
        }

        private bool _needCheckDeposit = false;

        private void BuySecondSecurity()
        {
            if (!_needBuySecondSecurity) return;
            if (_needQuoteOrdersSecondSecurity) return;
            if (_needCheckSendOpenOrders) return;
            if (_needCancelOrders) return;
            if (_needCheckSellFirstSecurity) return;
            if (_regime == Regime.Shutdown) return;

            _mdSecond = _tab2.MarketDepth;

            if (_timeUpdateDepth > _mdSecond.Time.TimeOfDay.TotalMilliseconds &&
                _timeUpdateDepth + 2000 > DateTime.UtcNow.TimeOfDay.TotalMilliseconds) return;

            if (_tab2.PositionOpenLong.Count > 0)
            {
                List<Order> openOrders = _tab2.PositionOpenLong[0].OpenOrders;
                int countOpenOrders = openOrders.Count - 15;

                if (countOpenOrders < 0)
                {
                    countOpenOrders = 0;
                }

                for (int j = openOrders.Count - 1; j >= countOpenOrders; j--)
                {
                    if (openOrders[j].State == OrderStateType.Active ||
                        openOrders[j].State == OrderStateType.None)
                    {
                        return;
                    }
                }
            }

            UpdateRatio();

            decimal bestPrice = GetBestPrice();

            if (bestPrice == 0) return;

            _listOrders = new List<ListOrders>();

            GetListOrders(bestPrice);

            decimal summOrders = GetSummOrders();

            if ((decimal)_mdSecond.Bids[^1].Price > _priceZeroLevel) return;

            if (summOrders <= 0)
            {
                return;
            }

            if (!CheckDepositForOrders(summOrders))
            {
                if (!_needCheckDeposit)
                {
                    _needCheckDeposit = true;

                    SendNewLogMessage($"На депозите недостаточно активов, USDT осталось ~{_posUsdt} USDT и {_tab1.Security.Name} осталось {_posToken} USDT, а необходимо {summOrders} USDT", _logging);

                    /*if (_setCounterOrder == "Off")
                    {
                        _regime = Regime.Shutdown;
                    }*/

                    //_needBuySecondSecurity = false;                    
                }

                return;
            }

            _needCheckDeposit = false;

            SendNewLogMessage($"Deposit USDT = {_posUsdt}", _logging);
            SendNewLogMessage($"Deposit {_tab1.Security.Name} в пересчете на USDT: {_posToken}", _logging);

            PrintMD();

            SendNewLogMessage($"Цена соотношения = {_limitPriceRatio}, BestPrice = {bestPrice}", _logging);

            string strMsg = "Massive orders: ";

            if (!_tab2.IsConnected || !_tab2.IsReadyToTrade)
            {
                SendNewLogMessage($"Нет подключения к бирже.", _logging);
                return;
            }

            for (int i = 0; i < _listOrders.Count; i++)
            {
                SendOrderBuy(_listOrders[i].Price, _listOrders[i].Volume);
                _listOrders[i].NumberUser = _tab2.PositionsLast.OpenOrders[^1].NumberUser.ToString();

                strMsg += $"\nNumber: {_listOrders[i].NumberUser}, Price: {_listOrders[i].Price}, Volume: {_listOrders[i].Volume}, Dev.Price: {_listOrders[i].DeviationPrice}";
            }

            SendNewLogMessage(strMsg, _logging);

            _needCheckSendOpenOrders = true;
            _needBuySecondSecurity = false;
        }

        private void GetListOrders(decimal bestPrice)
        {
            for (int i = 0; i < _listTableGrid.Count; i++)
            {
                if (_listTableGrid[i].Volume == 0)
                {
                    continue;
                }

                decimal price = Math.Round(_listTableGrid[i].DeviationPrice + bestPrice, _tab2.Security.Decimals);
                decimal volume = _listTableGrid[i].Volume;

                if (price * volume < 2)
                {
                    SendNewLogMessage($"Объем позиции меньше 2 USDT, измените объемы. Робот будет остановлен", _logging);
                    _regime = Regime.Shutdown;
                    return;
                }

                if (price >= _secondAsk)
                {
                    continue;
                }

                if (_setTradeOneSecurity)
                {
                    if (price > _setRatioForBuy)
                    {
                        continue;
                    }
                }
                else
                {
                    if (price >= _limitPriceRatio)
                    {
                        continue;
                    }
                }

                _listOrders.Add(new ListOrders { Price = price, Volume = volume, DeviationPrice = _listTableGrid[i].DeviationPrice });
            }

            if (_listOrders.Count == 0 && _limitPriceRatio <= _secondBid)
            {
                for (int i = 0; i < _listTableGrid.Count; i++)
                {
                    if (_listTableGrid[i].Volume == 0)
                    {
                        continue;
                    }

                    if (_listTableGrid[i].DeviationPrice > 0)
                    {
                        continue;
                    }

                    decimal price = Math.Round(_listTableGrid[i].DeviationPrice + _limitPriceRatio, _tab2.Security.Decimals);

                    if (price >= _secondAsk)
                    {
                        continue;
                    }

                    decimal volume = _listTableGrid[i].Volume;

                    if (price * volume < 2)
                    {
                        SendNewLogMessage($"Объем позиции меньше 2 USDT, измените объемы. Робот будет остановлен", _logging);
                        _regime = Regime.Shutdown;
                        return;
                    }

                    _listOrders.Add(new ListOrders { Price = price, Volume = volume, DeviationPrice = _listTableGrid[i].DeviationPrice });
                }
            }
        }

        private decimal GetSummOrders()
        {
            decimal summOrders = 0;
            _maxPriceInListOrders = 0;
            _priceZeroLevel = 0;

            for (int i = 0; i < _listOrders.Count; i++)
            {
                summOrders += _listOrders[i].Price * _listOrders[i].Volume;

                if (_maxPriceInListOrders < _listOrders[i].Price)
                {
                    _maxPriceInListOrders = _listOrders[i].Price;
                }

                if (_listOrders[i].DeviationPrice == 0 &&
                    _listOrders[i].Volume != 0)
                {
                    _priceZeroLevel = _listOrders[i].Price;
                }
            }

            return summOrders;
        }

        private bool CheckDepositForOrders(decimal summOrders)
        {
            List<PositionOnBoard> positions = _tab2.Portfolio.GetPositionOnBoard();

            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i].SecurityNameCode == "USDT")
                {
                    _posUsdt = positions[i].ValueCurrent - 2;
                    
                    break;
                }
            }

            if (!_setTradeOneSecurity)
            {
                positions = _tab1.Portfolio.GetPositionOnBoard();

                for (int i = 0; i < positions.Count; i++)
                {
                    if (positions[i].PortfolioName.Contains("Spot"))
                    {
                        if (positions[i].SecurityNameCode == _tab1.Security.Name.Split("USDT")[0])
                        {
                            _posToken = Math.Round(positions[i].ValueCurrent * _tab1.PriceBestBid - 2);                            
                        }
                    }
                }

                if (_posToken < summOrders)
                {
                    return false;
                }
            }

            if (_posUsdt < summOrders)
            {
                return false;
            }

            return true;
        }

        private decimal GetBestPrice()
        {
            MarketDepth md = _tab2.MarketDepth;

            for (int i = 0; i < md.Bids.Count; i++)
            {
                decimal price = Math.Round((decimal)md.Bids[i].Price + _tab2.Security.PriceStep, _tab2.Security.Decimals);

                if (_limitPriceRatio < price)
                {
                    continue;
                }

                if ((decimal)md.Bids[i].Bid >= _setOrderSizeForBestPrice)
                {
                    return price;
                }
            }

            return 0;
        }

        private void SendOrderBuy(decimal price, decimal volume)
        {
            if (_tab2.PositionOpenLong.Count == 0)
            {
                _tab2.BuyAtLimit(volume, price);
            }
            else
            {
                Position position = _tab2.PositionOpenLong[0];
                _tab2.BuyAtLimitToPosition(position, price, volume);
            }
        }

        private void SellFirstSecurity()
        {
            if (_setTradeOneSecurity) return;
            if (_needCheckSellFirstSecurity) return;

            decimal volumeSecond = 0;
            decimal volumeFirst = 0;
            decimal priceSecond = 0;
            decimal priceFirst = 0;
            decimal summFirst = 0;
            decimal summSecond = 0;

            if (_tab2.PositionOpenLong.Count != 0)
            {
                volumeSecond = _tab2.PositionOpenLong[0].MaxVolume;
                priceSecond = _tab2.PositionOpenLong[0].EntryPrice;
                summSecond = Math.Round(volumeSecond * priceSecond);
            }

            if (_tab1.PositionOpenShort.Count != 0)
            {
                volumeFirst = _tab1.PositionOpenShort[0].MaxVolume;
                priceFirst = _tab1.PositionOpenShort[0].EntryPrice;
                summFirst = Math.Round(volumeFirst * priceFirst);
            }
            
            if (summFirst < summSecond &&
                summSecond - summFirst > 1.9m)
            {
                decimal volume = Math.Round((volumeSecond * priceSecond - volumeFirst * priceFirst) / _firstBid, _tab1.Security.DecimalsVolume, MidpointRounding.ToZero);
                decimal price = Math.Round(_firstBid - _tab1.Security.PriceStep * 100, _tab1.Security.Decimals);

                if (_tab1.PositionOpenShort.Count == 0)
                {
                    _tab1.SellAtLimit(volume, price);
                }
                else
                {
                    _tab1.SellAtLimitToPosition(_tab1.PositionOpenShort[0], price, volume);
                }

                SendNewLogMessage($"Куплено {_tab2.Security.Name} = {volumeSecond}, есть {_tab1.Security.Name} = {volumeFirst}, нужно продать {_tab1.Security.Name}: {volume}", _logging);
                SendNewLogMessage("Ордер на продажу первого инструмента отправлен на исполнение", _logging);
                _needCheckSellFirstSecurity = true;
            }            
        }
              
        private void CheckExecuteFirstOrders()
        {
            if (!_needCheckSellFirstSecurity) return;
            if (_needCheckCancelFirstOrder) return;                      
            if (_tab1.PositionOpenShort.Count == 0) return;

            if (_tab1.PositionOpenShort[0].OpenOrders?[^1].State == OrderStateType.Done ||
                _tab1.PositionOpenShort[0].OpenOrders?[^1].State == OrderStateType.Cancel ||
                _tab1.PositionOpenShort[0].OpenOrders?[^1].State == OrderStateType.Fail)
            {
                SendNewLogMessage("Ордер на продажу первого инструмента исполнен", _logging);
                _needCheckSellFirstSecurity = false;

                _needDelayExecuteOrders = true;
                _timeDelayExecuteOrder = DateTime.UtcNow;

                GetRatioTakenPosition();

                return;
            }

            if (_tab1.PositionOpenShort[0].OpenOrders?[^1].State == OrderStateType.Active ||
                _tab1.PositionOpenShort[0].OpenOrders?[^1].State == OrderStateType.Partial)
            {
                if (_tab1.PositionOpenShort[0].OpenOrders?[^1].Price >= _firstBid)
                {
                    SendNewLogMessage("Ордер на продажу первого инструмента встал в аск, отправляем его на отмену", _logging);
                    _tab1.CloseOrder(_tab1.PositionOpenShort[0].OpenOrders?[^1]);
                    _needCheckCancelFirstOrder = true;
                }
            }                      
        }

        private void GetRatioTakenPosition()
        {
            _ratioTakenLastPositions = Math.Round(_tab2.PositionOpenLong[0].OpenOrders[^1].Price / _tab1.PositionOpenShort[0].OpenOrders[^1].Price, 6);
            SendNewLogMessage($"Соотношение купленных и проданных ордеров = {_ratioTakenLastPositions}", _logging);
        }

        private void CheckCancelFirstOrder()
        {
            if (!_needCheckCancelFirstOrder) return;

            if (_tab1.PositionOpenShort.Count == 0)
            {
                SendNewLogMessage("Ордер на продажу первого инструмента отменен", _logging);
                _needCheckCancelFirstOrder = false;
                _needCheckSellFirstSecurity = false;
                return;
            }

            if (_tab1.PositionOpenShort[0].OpenOrders?.Count == 0)
            {
                SendNewLogMessage("Ордер на продажу первого инструмента отменен", _logging);
                _needCheckCancelFirstOrder = false;
                _needCheckSellFirstSecurity = false;
                return;
            }

            if (_tab1.PositionOpenShort[0].OpenOrders[^1].State == OrderStateType.Cancel ||
                _tab1.PositionOpenShort[0].OpenOrders[^1].State == OrderStateType.Fail)
            {
                SendNewLogMessage("Ордер на продажу первого инструмента отменен", _logging);
                _needCheckCancelFirstOrder = false;
                _needCheckSellFirstSecurity = false;
            }

            if (_tab1.PositionOpenShort[0].OpenOrders[^1].State == OrderStateType.Done)
            {
                SendNewLogMessage("Ордер на продажу первого инструмента исполнился во время отмены", _logging);
                _needCheckCancelFirstOrder = false;
            }
        }

        #endregion

        private class TableGrid
        {
            public decimal DeviationPrice = 0;
            public decimal Volume = 0;
        }

        private class ListOrders
        {
            public decimal Price = 0;
            public decimal Volume = 0;
            public string NumberUser = "";
            public decimal VolumeExecute = 0;
            public decimal DeviationPrice = 0;
            public OrderStateType State = OrderStateType.None;
        }

        private enum Regime
        {
            Off,
            On,
            Shutdown
        }
    }
}
