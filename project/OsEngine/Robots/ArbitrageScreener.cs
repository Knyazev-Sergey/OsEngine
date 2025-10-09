using System;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels.Attributes;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Drawing;
using System.Linq;
using OsEngine.Market.Connectors;
using OsEngine.Market;
using System.Threading.Tasks;

namespace OsEngine.Robots
{
    [Bot("ArbitrageScreener")]

    public class ArbitrageScreener : BotPanel
    {
        #region Constructor

        public override string GetNameStrategyType()
        {
            return "ArbitrageScreener";
        }

        public override void ShowIndividualSettingsDialog()
        {
        }

        private BotTabScreener _screener0;
        private BotTabScreener _screener1;
        private BotTabScreener _screener2;
        private BotTabScreener _screener3;
        private BotTabScreener _screener4;
        private BotTabScreener _screener5;
        private BotTabScreener _screener6;
        private BotTabScreener _screener7;
        private BotTabScreener _screener8;
        private BotTabScreener _screener9;
        private BotTabScreener _screener10;
        private BotTabScreener _screener11;
        private BotTabScreener _screener12;
        private BotTabScreener _screener13;

        private StrategyParameterString _regimeFunding;
        private StrategyParameterString _regimeSpot;

        private StrategyParameterDecimal _setFundingRateMore;
        private StrategyParameterDecimal _setFundingRateLess;
        private StrategyParameterDecimal _setFundingMinVolume;
        private StrategyParameterDecimal _setFundingMinSpreadRate;
        private StrategyParameterDecimal _setFundingMinSpreadPair;

        private StrategyParameterDecimal _setSpotMinSpread;
        private StrategyParameterDecimal _setSpotMinVolume;
        private StrategyParameterDecimal _setSpotMinVolumeHedge;
        private StrategyParameterDecimal _setSpotMinRateHedge;

        public ArbitrageScreener(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Screener);
            _screener0 = TabsScreener[0];
            /*_screener0.ServerType = ServerType.Bybit;
            _screener0.PortfolioName = "BybitUNIFIED";
            _screener0.TimeFrame = TimeFrame.Min30;
            _screener0.NeedToReloadTabs = true;*/

            TabCreate(BotTabType.Screener);
            _screener1 = TabsScreener[1];
            TabCreate(BotTabType.Screener);
            _screener2 = TabsScreener[2];
            TabCreate(BotTabType.Screener);
            _screener3 = TabsScreener[3];
            TabCreate(BotTabType.Screener);
            _screener4 = TabsScreener[4];
            TabCreate(BotTabType.Screener);
            _screener5 = TabsScreener[5];
            TabCreate(BotTabType.Screener);
            _screener6 = TabsScreener[6];
            TabCreate(BotTabType.Screener);
            _screener7 = TabsScreener[7];
            TabCreate(BotTabType.Screener);
            _screener8 = TabsScreener[8];
            TabCreate(BotTabType.Screener);
            _screener9 = TabsScreener[9];
            TabCreate(BotTabType.Screener);
            _screener10 = TabsScreener[10];
            TabCreate(BotTabType.Screener);
            _screener11 = TabsScreener[11];
            TabCreate(BotTabType.Screener);
            _screener12 = TabsScreener[12];
            TabCreate(BotTabType.Screener);
            _screener13 = TabsScreener[13];

            string tabName = " Параметры ";

            _regimeFunding = CreateParameter("Отображать данные по фандингу", "On", new string[] { "Off", "On" }, tabName);
            _regimeSpot = CreateParameter("Отображать данные по споту", "On", new string[] { "Off", "On" }, tabName);

            StrategyParameterString label = CreateParameter("Настройка скринера арбитража ставки", "", tabName);
            _setFundingRateMore = CreateParameter("Ставка более, %", 0m, 0m, 0m, 0m, tabName);
            _setFundingRateLess = CreateParameter("Ставка менее, %", 0m, 0m, 0m, 0m, tabName);
            _setFundingMinVolume = CreateParameter("Мин объем торгов, USDT", 0m, 0m, 0m, 0m, tabName);
            _setFundingMinSpreadRate = CreateParameter("Мин спред ставки, %", 0m, 0m, 0m, 0m, tabName);
            _setFundingMinSpreadPair = CreateParameter("Мин спред пары, %", 0m, 0m, 0m, 0m, tabName);

            StrategyParameterString label1 = CreateParameter("", "", tabName);
            StrategyParameterString label2 = CreateParameter("Настройки скринера арбитража спот", "", tabName);
            _setSpotMinSpread = CreateParameter("Миним.спред спот, %", 0m, 0m, 0m, 0m, tabName);
            _setSpotMinVolume = CreateParameter("Мин.V спот, USDT", 0m, 0m, 0m, 0m, tabName);
            _setSpotMinVolumeHedge = CreateParameter("Мин.V хедж, USDT", 0m, 0m, 0m, 0m, tabName);
            _setSpotMinRateHedge = CreateParameter("Мин.ставка хедж, %", 0m, 0m, 0m, 0m, tabName);

            this.ParamGuiSettings.Title = "Arbitrage Screener";
            this.ParamGuiSettings.Height = 800;
            this.ParamGuiSettings.Width = 1200;

            CustomTabToParametersUi tabFunding = ParamGuiSettings.CreateCustomTab(" Фандинг ");
            CustomTabToParametersUi tabSpot = ParamGuiSettings.CreateCustomTab(" Спот ");
            CustomTabToParametersUi tabTotal = ParamGuiSettings.CreateCustomTab(" Итоги ");
            CustomTabToParametersUi tabSettingsFunding = ParamGuiSettings.CreateCustomTab(" Настройка тикеров ");
            CustomTabToParametersUi tabComission = ParamGuiSettings.CreateCustomTab(" Комиссии ");

            CreateTableFunding();
            tabFunding.AddChildren(_hostFunding);

            CreateTableSpot();
            tabSpot.AddChildren(_hostSpot);

            CreateTableTotal();
            tabTotal.AddChildren(_hostTotal);

            CreateTableSecuritySettings();
            tabSettingsFunding.AddChildren(_hostSettingsSecurity);

            CreateTabComissionSettings();
            tabComission.AddChildren(_hostComission);
            LoadComission();

            Thread threadFunding = new Thread(ThreadRefreshFunding) { IsBackground = true };
            threadFunding.Start();

            Thread threadFundingTable = new Thread(ThreadRefreshTableFunding) { IsBackground = true };
            threadFundingTable.Start();

            Thread threadSpot = new Thread(ThreadRefreshSpot) { IsBackground = true };
            threadSpot.Start();

            Thread threadSpotTable = new Thread(ThreadRefreshTableSpot) { IsBackground = true };
            threadSpotTable.Start();

            Thread threadTotalTable = new Thread(ThreadRefreshTableTotal) { IsBackground = true };
            threadTotalTable.Start();
        }

        #endregion

        #region Funding

        private Dictionary<string, (Dictionary<string, FundingData> Item, bool OnOff)> _fundingData = new();
        private Dictionary<string, (Dictionary<string, FundingData> Item, bool OnOff)> _fundingDataToTable = new();          
        private object _lockFunding = new object();

        private void ThreadRefreshFunding(object obj)
        {
            while (true)
            {
                try
                {                    
                    MainLogicFunding();
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                    Thread.Sleep(5000);
                }
            }
        }

        private void MainLogicFunding()
        {
            if (_regimeFunding == "Off")
            {
                return;
            }

            if (_securityFunding.Count == 0)
            {
                return;
            }

            lock (_lockFunding)
            {
                foreach (var token in _securityFunding)
                {
                    AddDataToFundingData(token);
                }
            }
        }

        private void AddDataToFundingData(KeyValuePair<string, (Dictionary<string, string> Item, bool OnOff)> data)
        {
            try
            {
                string token = data.Key;

                if (!data.Value.OnOff)
                {
                    TryDeleteTokenFromTableFunding(token);
                    return;
                }

                for (int i = 0; i < TabsScreener?.Count; i++)
                {
                    if (TabsScreener[i].ServerType == ServerType.None)
                    {
                        continue;
                    }

                    string exchange = TabsScreener[i].ServerType.ToString();

                    if (!_securityFunding.ContainsKey(token))
                    {
                        continue;
                    }

                    if (TabsScreener[i].ServerType == ServerType.Binance ||
                        TabsScreener[i].ServerType == ServerType.BitGetSpot ||
                        TabsScreener[i].ServerType == ServerType.HTXSpot ||
                        TabsScreener[i].ServerType == ServerType.BingXSpot ||
                        TabsScreener[i].ServerType == ServerType.KuCoinSpot ||
                        TabsScreener[i].ServerType == ServerType.GateIoSpot)
                    {
                        continue;
                    }

                    if (!_securityFunding[token].Item.ContainsKey(exchange))
                    {
                        continue;
                    }

                    string securityNameFutures = "";
                    int lotFutures = 1;

                    if (_securityFunding[token].Item[exchange].Contains('|'))
                    {
                        securityNameFutures = RemoveSecuritySymbol(_securityFunding[token].Item[exchange].Split('|')[1]);

                        lotFutures = GetLot(_securityFunding[token].Item[exchange].Split('|')[1]);
                    }

                    if (!_fundingData.ContainsKey(token))
                    {
                        _fundingData[token] = (new Dictionary<string, FundingData>(), true);
                    }

                    if (!_fundingData[token].Item.ContainsKey(exchange))
                    {
                        _fundingData[token].Item[exchange] = new FundingData();
                    }

                    for (int j = 0; j < TabsScreener[i].Tabs.Count; j++)
                    {
                        BotTabSimple tab = TabsScreener[i].Tabs[j];

                        if (tab.Security?.Name == securityNameFutures)
                        {
                            FundingData fundingData = _fundingData[token].Item[exchange];
                            fundingData.BestAsk = tab.PriceBestAsk;
                            fundingData.BestBid = tab.PriceBestBid;
                            fundingData.Volume24h = Math.Round(tab.SecurityVolumes.Volume24hUSDT);
                            fundingData.LotFutures = lotFutures;
                            fundingData.Rate = Math.Round(tab.Funding.CurrentValue, 6);
                            fundingData.FundingPeriod = tab.Funding.FundingIntervalHours;
                            fundingData.MinFundingRate = tab.Funding.MinFundingRate;
                            fundingData.MaxFundingRate = tab.Funding.MaxFundingRate;

                            if (tab.Trades != null && tab.Trades.Count > 0)
                            {
                                fundingData.OpenInterest = Math.Round(tab.Trades[^1].OpenInterest);
                            }                            

                            if (tab.Funding.NextFundingTime != new DateTime(1970, 1, 1, 0, 0, 0))
                            {
                                fundingData.ExpirationTimeFunding = tab.Funding.NextFundingTime;
                            }

                            if (tab.CandlesAll?.Count > 0)
                            {
                                fundingData.Price = tab.CandlesAll[^1].Close;
                            }

                            fundingData.Tab = tab;

                            _fundingData[token].Item[exchange] = fundingData;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void TryDeleteTokenFromTableFunding(string token)
        {
            if (_fundingData.ContainsKey(token))
            {
                _fundingData.Remove(token);
            }
        }

        #endregion

        #region Funding Table

        private WindowsFormsHost _hostFunding;
        private DataGridView _gridFunding;
        private List<FundingGeneralTable> _fundingGeneralTablesOn;
        private List<FundingGeneralTable> _fundingGeneralTablesOff;
        private object _lockFundingTable = new object();

        private void CreateTableFunding()
        {
            _hostFunding = new WindowsFormsHost();

            DataGridView dataGridView =
                DataGridFactory.GetDataGridView(DataGridViewSelectionMode.CellSelect,
                DataGridViewAutoSizeRowsMode.AllCells);
            dataGridView.Dock = DockStyle.Fill;
            dataGridView.ScrollBars = ScrollBars.Both;
            dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView.GridColor = Color.Gray;

            dataGridView.CellClick += Funding_DataGridView_CellClick;
            dataGridView.DataError += Funding_DataGridView_DataError;

            dataGridView.ColumnCount = 13;
            dataGridView.RowCount = 1;

            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.Programmatic;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                column.DefaultCellStyle.SelectionBackColor = dataGridView.DefaultCellStyle.BackColor;
                column.DefaultCellStyle.SelectionForeColor = dataGridView.DefaultCellStyle.ForeColor;
                column.ReadOnly = true;
            }

            dataGridView.Rows[0].DefaultCellStyle.Font = new Font(dataGridView.Font, FontStyle.Bold | FontStyle.Italic);

            dataGridView[1, 0].Value = "Токен";
            dataGridView[2, 0].Value = "Площадка";
            dataGridView[3, 0].Value = "Частота";
            dataGridView[4, 0].Value = "Лонг";
            dataGridView[5, 0].Value = "Шорт";
            dataGridView[6, 0].Value = "Ставка";
            dataGridView[7, 0].Value = "Спред ставки";
            dataGridView[8, 0].Value = "Время";
            dataGridView[9, 0].Value = "Спред пары";
            dataGridView[10, 0].Value = "Объем торгов";
            dataGridView[11, 0].Value = "Объем ОИ";
            dataGridView[12, 0].Value = "Вкл/выкл";

            _hostFunding.Child = dataGridView;
            _gridFunding = dataGridView;
        }

        private void Funding_DataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;

            string errorMessage = $"Ошибка в ячейке [{e.ColumnIndex},{e.RowIndex}]: ";

            switch (e.Context)
            {
                case DataGridViewDataErrorContexts.Formatting:
                    errorMessage += "Ошибка форматирования";
                    break;
                case DataGridViewDataErrorContexts.Commit:
                    errorMessage += "Ошибка сохранения данных";
                    break;
                case DataGridViewDataErrorContexts.Parsing:
                    errorMessage += "Невозможно преобразовать значение";
                    break;
                case DataGridViewDataErrorContexts.Display:
                    errorMessage += "Ошибка отображения";
                    break;
                default:
                    errorMessage += "Неизвестная ошибка";
                    break;
            }

            SendNewLogMessage(errorMessage, Logging.LogMessageType.User);
        }

        private void ThreadRefreshTableFunding()
        {
            while (true)
            {
                try
                {
                    AddDataToGridFunding();

                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                    Thread.Sleep(5000);
                }
            }
        }

        private void AddDataToGridFunding()
        {
            if (_regimeFunding == "Off")
            {
                return;
            }

            lock (_lockFunding)
            {
                _fundingDataToTable = new Dictionary<string, (Dictionary<string, FundingData> Item, bool OnOff)>(_fundingData);
            }

            _fundingGeneralTablesOn = GetDataTableFunding(true);
            _fundingGeneralTablesOff = GetDataTableFunding(false);

            if (_fundingGeneralTablesOn == null || _fundingGeneralTablesOff == null)
            {
                return;
            }

            AddOrDeleteRowsInFundingTable();

            SortingTableFunding();

            int countRowsInTable = 1;
            int numberRow = 1;

            foreach (var item in _fundingGeneralTablesOn)
            {
                if (item == null)
                {
                    continue;
                }

                _gridFunding.Rows[countRowsInTable].DefaultCellStyle.ForeColor = Color.FromArgb(154, 156, 158);
                _gridFunding.Rows[countRowsInTable].DefaultCellStyle.SelectionForeColor = Color.FromArgb(154, 156, 158);

                FillTableFunding(item, countRowsInTable, numberRow);

                countRowsInTable++;
                numberRow++;
            }

            foreach (var item in _fundingGeneralTablesOff)
            {
                if (item == null)
                {
                    continue;
                }

                _gridFunding.Rows[countRowsInTable].DefaultCellStyle.ForeColor = Color.FromArgb(64, 64, 64);
                _gridFunding.Rows[countRowsInTable].DefaultCellStyle.SelectionForeColor = Color.FromArgb(64, 64, 64);

                FillTableFunding(item, countRowsInTable, numberRow);

                countRowsInTable++;
                numberRow++;
            }            
        }

        private List<FundingGeneralTable> GetDataTableFunding(bool flag)
        {
            try
            {
                lock (_lockFundingTable)
                {
                    List<FundingGeneralTable> fundingGeneralTables = new();

                    foreach (var token in _fundingDataToTable)
                    {
                        if (token.Value.OnOff != flag)
                        {
                            continue;
                        }

                        if (!CheckParametersFunding(token))
                        {
                            continue;
                        }

                        FundingGeneralTable fgt = new FundingGeneralTable();

                        // токен
                        fgt.Token = token.Key;

                        // кол-во площадок
                        fgt.CountExchange = token.Value.Item.Count;

                        // частота
                        fgt.Periodicity = GetPriodicityRate(token.Value.Item);

                        decimal minRate = GetMinRateFunding(token.Value.Item);
                        decimal maxRate = GetMaxRateFunding(token.Value.Item);

                        decimal bestBid = 0;
                        decimal bestAsk = 0;

                        // лонг спот
                        KeyValuePair<string, FundingData> exchangeLong = ChoiseExchangeLongFunding(token.Key);

                        if (exchangeLong.Key != null)
                        {
                            fgt.ExchangeLong = exchangeLong.Key;
                            bestBid = exchangeLong.Value.BestBid * exchangeLong.Value.LotFutures;
                            fgt.TabLong = exchangeLong.Value.Tab;
                        }
                        else
                        {
                            fgt.ExchangeLong = "";
                        }

                        // шорт спот
                        KeyValuePair<string, FundingData> exchangeShort = ChoiseExchangeShortFunding(token.Key);

                        if (exchangeShort.Key != null)
                        {
                            fgt.ExchangeShort = exchangeShort.Key;
                            bestAsk = exchangeShort.Value.BestAsk * exchangeShort.Value.LotFutures;
                            fgt.TabShort = exchangeShort.Value.Tab;
                        }
                        else
                        {
                            fgt.ExchangeShort = "";
                        }

                        // ставка
                        fgt.MinMaxRate = minRate + "% / " + maxRate + "%";

                        // спред ставки
                        if (exchangeLong.Key != null && exchangeShort.Key != null)
                        {
                            if (exchangeLong.Value.ExpirationTimeFunding == exchangeShort.Value.ExpirationTimeFunding)
                            {
                                fgt.SpreadFunding = Math.Round(exchangeShort.Value.Rate - exchangeLong.Value.Rate, 4);
                            }
                            else if (exchangeLong.Value.ExpirationTimeFunding < exchangeShort.Value.ExpirationTimeFunding)
                            {
                                fgt.SpreadFunding = Math.Round(0 - exchangeLong.Value.Rate, 4);
                            }
                            else if (exchangeLong.Value.ExpirationTimeFunding > exchangeShort.Value.ExpirationTimeFunding)
                            {
                                fgt.SpreadFunding = Math.Round(exchangeShort.Value.Rate - 0, 4);
                            }

                            if (fgt.SpreadFunding < _setFundingMinSpreadRate.ValueDecimal) // проверка параметра минимального спреда ставки
                            {
                                continue;
                            }
                        }

                        // время
                        fgt.FundingTime = GetFundingTime(token.Value.Item);

                        // спред пары
                        if (bestAsk != 0 && bestBid != 0)
                        {
                            fgt.SpreadPairs = Math.Round((bestAsk - bestBid) / bestBid * 100, 4);

                            if (fgt.SpreadPairs < _setFundingMinSpreadPair.ValueDecimal)
                            {
                                continue;
                            }
                        }

                        // объемы торгов
                        (fgt.Volume, fgt.OpenInterest) = GetVolumeFutures(token.Value.Item);

                        if (fgt.Volume < _setFundingMinVolume.ValueDecimal)
                        {
                            continue;
                        }

                        fgt.OnOffToken = token.Value.OnOff;

                        fundingGeneralTables.Add(fgt);
                    }

                    return fundingGeneralTables;
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.StackTrace, Logging.LogMessageType.Error);
                return null;
            }
        }

        private void FillTableFunding(FundingGeneralTable item, int countRowsInTable, int numberRow)
        {
            if (MainWindow.GetDispatcher.CheckAccess() == false)
            {
                MainWindow.GetDispatcher.Invoke(new Action<FundingGeneralTable, int, int>(FillTableFunding), item, countRowsInTable, numberRow);

                return;
            }

            _gridFunding[0, countRowsInTable].Value = numberRow;
            _gridFunding[1, countRowsInTable].Value = item.Token;
            _gridFunding[2, countRowsInTable].Value = item.CountExchange;
            _gridFunding[3, countRowsInTable].Value = item.Periodicity;
            _gridFunding[4, countRowsInTable].Value = item.ExchangeLong;
            _gridFunding[5, countRowsInTable].Value = item.ExchangeShort;
            _gridFunding[6, countRowsInTable].Value = item.MinMaxRate;
            _gridFunding[7, countRowsInTable].Value = item.SpreadFunding;
            _gridFunding[8, countRowsInTable].Value = item.FundingTime;
            _gridFunding[9, countRowsInTable].Value = item.SpreadPairs + "%";
            _gridFunding[10, countRowsInTable].Value = item.Volume;
            _gridFunding[11, countRowsInTable].Value = item.OpenInterest;

            DataGridViewCheckBoxCell checkBoxCell = new DataGridViewCheckBoxCell();
            _gridFunding[12, countRowsInTable] = checkBoxCell;
            checkBoxCell.Value = item.OnOffToken;

            for (int i = 0; i < Application.OpenForms.Count; i++)
            {
                Form form = Application.OpenForms[i];

                if (form.Text == "Funding:" + item.Token)
                {
                    AddDataToChildFormFunding(form);
                }
            }
        }
                
        private bool CheckParametersFunding(KeyValuePair<string, (Dictionary<string, FundingData> Item, bool OnOff)> token)
        {  
            foreach (var exchange in token.Value.Item)
            {
                if (exchange.Value.Rate < _setFundingRateLess.ValueDecimal &&
                    exchange.Value.Rate > _setFundingRateMore.ValueDecimal)
                {
                    return true;
                }
            }

            return false;
        }

        private string GetPriodicityRate(Dictionary<string, FundingData> exchange)
        {
            List<int> period = new List<int>();

            foreach (var item in exchange)
            {
                if (item.Value.FundingPeriod == 0)
                {
                    continue;
                }

                if (!period.Contains(item.Value.FundingPeriod))
                {                    
                    period.Add(item.Value.FundingPeriod);
                }
            }

            period.Sort();

            string periodicity = "";

            for (int i = 0; i < period.Count; i++)
            {
                if (i == 0)
                {
                    periodicity += period[i].ToString();
                }
                else
                {
                    periodicity += "/" + period[i].ToString();
                }
            }
            
            return periodicity;
        }

        private decimal GetMinRateFunding(Dictionary<string, FundingData> exchange)
        {
            decimal minRate = decimal.MaxValue;

            foreach (var item in exchange)
            {
                if (item.Value.Rate < minRate)
                {
                    minRate = item.Value.Rate;
                }
            }

            return Math.Round(minRate, 6);
        }

        private decimal GetMaxRateFunding(Dictionary<string, FundingData> exchange)
        {
            decimal maxRate = decimal.MinValue;

            foreach (var item in exchange)
            {
                if (item.Value.Rate > maxRate)
                {
                    maxRate = item.Value.Rate;
                }
            }

            return Math.Round(maxRate, 6);
        }

        private string GetFundingTime(Dictionary<string, FundingData> exchange)
        {
            DateTime time = DateTime.MaxValue;

            foreach(var item in exchange)
            {
                if (item.Value.ExpirationTimeFunding != new DateTime(1970, 1, 1, 0, 0, 0) &&
                    item.Value.ExpirationTimeFunding != new DateTime(0001, 1, 1, 0, 0, 0))
                {
                    if (item.Value.ExpirationTimeFunding < time)
                    {
                        time = item.Value.ExpirationTimeFunding;
                    }
                }
            }

            return (time - DateTime.UtcNow).ToString(@"hh\:mm\:ss");
        }

        private (decimal Volume, decimal OI) GetVolumeFutures(Dictionary<string, FundingData> exchange)
        {
            decimal volume = 0;
            decimal oi = 0;

            foreach(var item in exchange)
            {
                volume += item.Value.Volume24h;
                oi += item.Value.OpenInterest;
            }

            return (volume, oi);
        }

        private SortDirection _sortDirectionFunding = SortDirection.None;
        private int _columnSortingFunding = 0;

        private void SortingTableFunding()
        {
            if (_fundingGeneralTablesOn.Count < 1)
            {
                return;
            }

            if (_sortDirectionFunding != SortDirection.None && _columnSortingFunding != 0)
            {
                if (_sortDirectionFunding == SortDirection.Ascending)
                {
                    List<FundingGeneralTable> sortedList = new();

                    switch (_columnSortingFunding)
                    {
                        case 1:
                            sortedList = _fundingGeneralTablesOn.OrderBy(spot => spot.Token).ToList();
                            break;
                        case 2:
                            sortedList = _fundingGeneralTablesOn.OrderBy(spot => spot.CountExchange).ToList();
                            break;
                        case 3:
                            sortedList = _fundingGeneralTablesOn.OrderBy(spot => spot.Periodicity).ToList();
                            break;
                        case 4:
                            sortedList = _fundingGeneralTablesOn.OrderBy(spot => spot.ExchangeLong).ToList();
                            break;
                        case 5:
                            sortedList = _fundingGeneralTablesOn.OrderBy(spot => spot.ExchangeShort).ToList();
                            break;
                        case 6:
                            sortedList = _fundingGeneralTablesOn.OrderBy(spot => spot.MinMaxRate).ToList();
                            break;
                        case 7:
                            sortedList = _fundingGeneralTablesOn.OrderBy(spot => spot.SpreadFunding).ToList();
                            break;
                        case 8:
                            sortedList = _fundingGeneralTablesOn.OrderBy(spot => spot.FundingTime).ToList();
                            break;
                        case 9:
                            sortedList = _fundingGeneralTablesOn.OrderBy(spot => spot.SpreadPairs).ToList();
                            break;
                        case 10:
                            sortedList = _fundingGeneralTablesOn.OrderBy(spot => spot.Volume).ToList();
                            break;
                        case 11:
                            sortedList = _fundingGeneralTablesOn.OrderBy(spot => spot.OpenInterest).ToList();
                            break;
                    }

                    _fundingGeneralTablesOn.Clear();

                    if (sortedList != null)
                    {
                        _fundingGeneralTablesOn.AddRange(sortedList);
                    }
                }
                else if (_sortDirectionFunding == SortDirection.Descending)
                {
                    List<FundingGeneralTable> sortedList = new();

                    switch (_columnSortingFunding)
                    {
                        case 1:
                            sortedList = _fundingGeneralTablesOn.OrderByDescending(spot => spot.Token).ToList();
                            break;
                        case 2:
                            sortedList = _fundingGeneralTablesOn.OrderByDescending(spot => spot.CountExchange).ToList();
                            break;
                        case 3:
                            sortedList = _fundingGeneralTablesOn.OrderByDescending(spot => spot.Periodicity).ToList();
                            break;
                        case 4:
                            sortedList = _fundingGeneralTablesOn.OrderByDescending(spot => spot.ExchangeLong).ToList();
                            break;
                        case 5:
                            sortedList = _fundingGeneralTablesOn.OrderByDescending(spot => spot.ExchangeShort).ToList();
                            break;
                        case 6:
                            sortedList = _fundingGeneralTablesOn.OrderByDescending(spot => spot.MinMaxRate).ToList();
                            break;
                        case 7:
                            sortedList = _fundingGeneralTablesOn.OrderByDescending(spot => spot.SpreadFunding).ToList();
                            break;
                        case 8:
                            sortedList = _fundingGeneralTablesOn.OrderByDescending(spot => spot.FundingTime).ToList();
                            break;
                        case 9:
                            sortedList = _fundingGeneralTablesOn.OrderByDescending(spot => spot.SpreadPairs).ToList();
                            break;
                        case 10:
                            sortedList = _fundingGeneralTablesOn.OrderByDescending(spot => spot.Volume).ToList();
                            break;
                        case 11:
                            sortedList = _fundingGeneralTablesOn.OrderByDescending(spot => spot.OpenInterest).ToList();
                            break;
                    }

                    _fundingGeneralTablesOn.Clear();

                    if (sortedList != null)
                    {
                        _fundingGeneralTablesOn.AddRange(sortedList);
                    }
                }

                UpdateSortGlyphFunding();
            }
        }

        private void UpdateSortGlyphFunding()
        {
            foreach (DataGridViewColumn columns in _gridFunding.Columns)
            {
                columns.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            var column = _gridFunding.Columns[_columnSortingFunding];

            if (column != null)
            {
                column.HeaderCell.SortGlyphDirection = _sortDirectionFunding == SortDirection.Ascending
                    ? SortOrder.Ascending
                    : SortOrder.Descending;
            }
        }

        private void AddDataToChildFormFunding(Form form)
        {
            if (MainWindow.GetDispatcher.CheckAccess() == false)
            {
                MainWindow.GetDispatcher.Invoke(new Action<Form>(AddDataToChildFormFunding), form);

                return;
            }

            string token = form.Text.Split(':')[1];

            DataGridView dataGridView = null;

            foreach (Control control in form.Controls)
            {
                if (control is DataGridView dgv)
                {
                    dataGridView = dgv;
                }
            }

            if (dataGridView == null)
            {
                return;
            }

            int count = 0;

            lock (_lockFundingTable)
            {
                foreach (KeyValuePair<string, FundingData> exchange in _fundingDataToTable[token].Item)
                {
                    if (exchange.Value.OnOff == true)
                    {
                        dataGridView.Rows[count].DefaultCellStyle.ForeColor = Color.FromArgb(154, 156, 158);
                        dataGridView.Rows[count].DefaultCellStyle.SelectionForeColor = Color.FromArgb(154, 156, 158);
                        DataToChildFormFunding(dataGridView, count, exchange);

                        count++;
                    }
                }
            }

            lock (_lockFundingTable)
            {
                foreach (KeyValuePair<string, FundingData> exchange in _fundingDataToTable[token].Item)
                {
                    if (exchange.Value.OnOff == false)
                    {
                        dataGridView.Rows[count].DefaultCellStyle.ForeColor = Color.FromArgb(64, 64, 64);
                        dataGridView.Rows[count].DefaultCellStyle.SelectionForeColor = Color.FromArgb(64, 64, 64);
                        DataToChildFormFunding(dataGridView, count, exchange);

                        count++;
                    }
                }
            }
        }

        private void DataToChildFormFunding(DataGridView dataGridView, int count, KeyValuePair<string, FundingData> exchange)
        {
            dataGridView.CellValueChanged -= Funding_DataGridView_CellValueChanged;
            dataGridView.CurrentCellDirtyStateChanged -= Funding_DataGridView_CurrentCellDirtyStateChanged;

            dataGridView[0, count].Value = exchange.Key;
            dataGridView[1, count].Value = exchange.Value.FundingPeriod;
            dataGridView[2, count].Value = exchange.Value.Rate;
            dataGridView[3, count].Value = exchange.Value.MinFundingRate + "%/ " + exchange.Value.MaxFundingRate + "%";
            dataGridView[4, count].Value = (exchange.Value.ExpirationTimeFunding - DateTime.UtcNow).ToString(@"hh\:mm\:ss");
            dataGridView[5, count].Value = exchange.Value.BestBid * exchange.Value.LotFutures;
            dataGridView[6, count].Value = exchange.Value.ChoiseLong;
            dataGridView[7, count].Value = exchange.Value.BestAsk * exchange.Value.LotFutures;
            dataGridView[8, count].Value = exchange.Value.ChoiseShort;
            dataGridView[9, count].Value = exchange.Value.Volume24h;
            dataGridView[10, count].Value = exchange.Value.OpenInterest;
            dataGridView[11, count].Value = exchange.Value.OnOff;

            dataGridView.CellValueChanged += Funding_DataGridView_CellValueChanged;
            dataGridView.CurrentCellDirtyStateChanged += Funding_DataGridView_CurrentCellDirtyStateChanged;
        }

        private void Funding_DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            // сортировка
            if (e.ColumnIndex > 0 && e.ColumnIndex < 12)
            {
                if (e.RowIndex == 0)
                {
                    SetSortDirectionFunding(e.ColumnIndex);
                    return;
                }
            }

            string token = "";
            int rowIndex = e.RowIndex;

            token = _gridFunding.Rows[rowIndex].Cells[1].Value?.ToString();

            if (token == null || token == "")
            {
                return;
            }

            // открытие дополнительного окна
            if (e.ColumnIndex == 1)
            {
                if (e.RowIndex > 0)
                {
                    if (_gridFunding.Rows[rowIndex].Cells[1].Tag == null || (int)_gridFunding.Rows[rowIndex].Cells[1].Tag == 0)
                    {
                        foreach (Form form in Application.OpenForms)
                        {
                            if (form.Text == "Funding:" + token)
                            {
                                form.BringToFront();
                                form.WindowState = FormWindowState.Normal;
                                return;
                            }
                        }

                        Form childForm = new Form();
                        childForm.Text = "Funding:" + token;
                        childForm.AutoSize = true;
                        childForm.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                        childForm.Controls.Add(CreateChildTableFunding(token));
                        AddDataToChildFormFunding(childForm);
                        childForm.Show();

                        childForm.FormClosed += (sender, e) =>
                        {
                            childForm.Dispose();
                        };
                    }
                }
            }

            // чек бокс
            if (e.ColumnIndex == 12 && e.RowIndex > 0)
            {
                lock (_lockFundingTable)
                {
                    if (_fundingDataToTable[token].OnOff)
                    {
                        _fundingDataToTable[token] = (_fundingDataToTable[token].Item, false);
                    }
                    else
                    {
                        _fundingDataToTable[token] = (_fundingDataToTable[token].Item, true);
                    }
                }
            }
        }

        private void SetSortDirectionFunding(int column)
        {
            if (_columnSortingFunding != column)
            {
                _columnSortingFunding = column;
                _sortDirectionFunding = SortDirection.Descending;
            }
            else
            {
                if (_sortDirectionFunding == SortDirection.None)
                {
                    _sortDirectionFunding = SortDirection.Ascending;
                }
                else if (_sortDirectionFunding == SortDirection.Ascending)
                {
                    _sortDirectionFunding = SortDirection.Descending;
                }
                else if (_sortDirectionFunding == SortDirection.Descending)
                {
                    _sortDirectionFunding = SortDirection.Ascending;
                }
            }
        }

        private DataGridView CreateChildTableFunding(string token)
        {
            DataGridView dataGridView =
                DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect,
                DataGridViewAutoSizeRowsMode.AllCells);
            dataGridView.Dock = DockStyle.Fill;
            dataGridView.ScrollBars = ScrollBars.Both;
            dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.Black;
            dataGridView.ColumnHeadersDefaultCellStyle.Font = new Font(dataGridView.Font, FontStyle.Bold | FontStyle.Italic);
            dataGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = dataGridView.ColumnHeadersDefaultCellStyle.BackColor;
            dataGridView.ColumnHeadersDefaultCellStyle.SelectionForeColor = dataGridView.ColumnHeadersDefaultCellStyle.ForeColor;
            dataGridView.GridColor = Color.Gray;
            dataGridView.AutoSize = true;
            dataGridView.AllowUserToResizeRows = false;
            dataGridView.AllowUserToResizeColumns = false;
            dataGridView.MultiSelect = false;

            dataGridView.ColumnCount = 12;
            dataGridView.RowCount = 0;

            string[] listTable = new[] { "Площадка", "Частота", "Ставка", "Лимит ставки", "Время", "Лонг", "Выбор", "Шорт", "Выбор", "Объем торгов", "Объем ОИ", "Вкл/Выкл" };

            for (int i = 0; i < dataGridView.ColumnCount; i++)
            {
                DataGridViewColumn column = dataGridView.Columns[i];

                column.SortMode = DataGridViewColumnSortMode.Programmatic;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                column.DefaultCellStyle.SelectionBackColor = dataGridView.DefaultCellStyle.BackColor;
                column.DefaultCellStyle.SelectionForeColor = dataGridView.DefaultCellStyle.ForeColor;
                column.ReadOnly = true;

                column.HeaderText = listTable[i];
            }

            lock (_lockFundingTable)
            {
                if (_fundingDataToTable.TryGetValue(token, out var fundingData))
                {
                    for (int rowIndex = 0; rowIndex < fundingData.Item.Count; rowIndex++)
                    {
                        dataGridView.Rows.Add();

                        int[] checkboxColumns = { 6, 8, 11 };
                        foreach (int colIndex in checkboxColumns)
                        {

                            dataGridView.Rows[rowIndex].Cells[colIndex] = new DataGridViewCheckBoxCell()
                            {
                                Value = false,
                                TrueValue = true,
                                FalseValue = false
                            };

                            dataGridView.Rows[rowIndex].Cells[colIndex].ReadOnly = false;
                        }
                    }
                }
            }

            dataGridView.Tag = token;

            dataGridView.CellValueChanged += Funding_DataGridView_CellValueChanged;
            dataGridView.CurrentCellDirtyStateChanged += Funding_DataGridView_CurrentCellDirtyStateChanged;

            return dataGridView;
        }

        private void Funding_DataGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            DataGridView dataGridView = sender as DataGridView;

            if (dataGridView.IsCurrentCellDirty)
                dataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Funding_DataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {            
            DataGridView dataGridView = sender as DataGridView;

            if (e.ColumnIndex == 6)
            {
                SetBoolChoiseFunding(sender, e, e.ColumnIndex);
            }
            if (e.ColumnIndex == 8)
            {
                SetBoolChoiseFunding(sender, e, e.ColumnIndex);
            }            
            if (e.ColumnIndex == 11)
            {
                string token = dataGridView.Tag.ToString();
                string exchange = dataGridView.Rows[e.RowIndex].Cells[0].Value.ToString();
                bool value = (bool)dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

                if (exchange != null || exchange != "")
                {
                    lock (_lockFundingTable)
                    {
                        foreach (var item in _fundingDataToTable[token].Item)
                        {
                            if (item.Key == exchange)
                            {
                                if (value)
                                {
                                    _fundingDataToTable[token].Item[exchange].OnOff = true;
                                }
                                else
                                {
                                    _fundingDataToTable[token].Item[exchange].OnOff = false;
                                    _fundingDataToTable[token].Item[exchange].ChoiseLong = false;
                                    _fundingDataToTable[token].Item[exchange].ChoiseShort = false;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SetBoolChoiseFunding(object sender, DataGridViewCellEventArgs e, int columnIndex)
        {
            DataGridView dataGridView = sender as DataGridView;

            if ((bool)dataGridView.Rows[e.RowIndex].Cells[11].Value == false)
            {
                dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = false;
                return;
            }

            string token = dataGridView.Tag.ToString();
            string exchange = dataGridView.Rows[e.RowIndex].Cells[0].Value.ToString();
            bool value = (bool)dataGridView.Rows[e.RowIndex].Cells[columnIndex].Value;

            if (exchange != null || exchange != "")
            {
                lock (_lockFundingTable)
                {
                    foreach (var item in _fundingDataToTable[token].Item)
                    {
                        if (item.Key == exchange)
                        {
                            if (columnIndex == 6)
                            {
                                _fundingDataToTable[token].Item[exchange].ChoiseLong = (bool)dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                            }
                            else if (columnIndex == 8)
                            {
                                _fundingDataToTable[token].Item[exchange].ChoiseShort = (bool)dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                            }
                        }
                    }
                }

                if (value)
                {
                    for (int i = 0; i < dataGridView.Rows.Count; i++)
                    {
                        if (i != e.RowIndex)
                        {

                            dataGridView.Rows[i].Cells[columnIndex].Value = false;
                        }
                    }
                }
            }
        }

        private void AddOrDeleteRowsInFundingTable()
        {
            if (MainWindow.GetDispatcher.CheckAccess() == false)
            {
                MainWindow.GetDispatcher.Invoke(AddOrDeleteRowsInFundingTable);

                return;
            }

            int countNeedRows = _gridFunding.RowCount - (_fundingGeneralTablesOn.Count + _fundingGeneralTablesOff.Count + 1);

            if (countNeedRows < 0)
            {
                for (int i = 0; i < Math.Abs(countNeedRows); i++)
                {
                    _gridFunding.Rows.Add();
                }
            }
            else if (countNeedRows > 0)
            {
                for (int i = 0; i < countNeedRows; i++)
                {
                    _gridFunding.Rows.RemoveAt(_gridFunding.RowCount - 1);
                }
            }
        }

        private KeyValuePair<string, FundingData> ChoiseExchangeLongFunding(string key)
        {
            lock (_lockFundingTable)
            {
                foreach (var exchange in _fundingDataToTable[key].Item)
                {
                    if (exchange.Value.ChoiseLong)
                    {
                        return exchange;
                    }
                }
            }

            return default;
        }

        private KeyValuePair<string, FundingData> ChoiseExchangeShortFunding(string key)
        {
            lock (_lockFundingTable)
            {
                foreach (var exchange in _fundingDataToTable[key].Item)
                {
                    if (exchange.Value.ChoiseShort)
                    {
                        return exchange;
                    }
                }
            }

            return default;
        }

        #endregion

        #region Spot
                
        private Dictionary<string, (Dictionary<string, SpotData> Item, bool OnOff)> _spotData = new();        
        private object _lockSpot = new object();

        private void ThreadRefreshSpot(object obj)
        {
            while (true)
            {
                try
                {
                    MainLogicSpot();
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                    Thread.Sleep(5000);
                }
            }
        }

        private void MainLogicSpot()
        {
            if (_regimeSpot == "Off")
            {
                return;
            }

            if (_securitySpot.Count == 0)
            {
                return;
            }

            lock (_lockSpot)
            {
                foreach (var token in _securitySpot)
                {
                    AddDataToSpotData(token);
                }
            }
        }

        private void AddDataToSpotData(KeyValuePair<string, (Dictionary<string, string> Item, bool OnOff)> data)
        {
            try
            {
                string token = data.Key;

                if (!data.Value.OnOff)
                {
                    TryDeleteTokenFromTable(token);
                    return;
                }

                for (int i = 0; i < TabsScreener?.Count; i++)
                {
                    if (TabsScreener[i].ServerType == ServerType.None)
                    {
                        continue;
                    }

                    string exchange = TabsScreener[i].ServerType.ToString();

                    if (TabsScreener[i].ServerType == ServerType.BinanceFutures ||
                        TabsScreener[i].ServerType == ServerType.BitGetFutures ||
                        TabsScreener[i].ServerType == ServerType.HTXSwap ||
                        TabsScreener[i].ServerType == ServerType.BingXFutures ||
                        TabsScreener[i].ServerType == ServerType.KuCoinFutures ||
                        TabsScreener[i].ServerType == ServerType.GateIoFutures)
                    {
                        continue;
                    }

                    string exchangeHedge = GetHedgeExchange(TabsScreener[i].ServerType);

                    if (!_securitySpot.ContainsKey(token))
                    {
                        continue;
                    }

                    if (!_securitySpot[token].Item.ContainsKey(exchange))
                    {
                        continue;
                    }

                    string securityNameSpot = "";
                    string securityNameFutures = "";
                    int lotSpot = 1;
                    int lotFutures = 1;

                    if (exchange == exchangeHedge)
                    {
                        if (_securitySpot[token].Item[exchange].Contains('|'))
                        {
                            securityNameSpot = RemoveSecuritySymbol(_securitySpot[token].Item[exchange].Split('|')[0]);
                            securityNameFutures = RemoveSecuritySymbol(_securitySpot[token].Item[exchange].Split('|')[1]);

                            lotSpot = GetLot(_securitySpot[token].Item[exchange].Split('|')[0]);
                            lotFutures = GetLot(_securitySpot[token].Item[exchange].Split('|')[1]);
                        }
                    }
                    else
                    {
                        securityNameSpot = RemoveSecuritySymbol(_securitySpot[token].Item[exchange].Split('|')[0]);
                        lotSpot = GetLot(_securitySpot[token].Item[exchange].Split('|')[0]);

                        if (_securitySpot[token].Item.ContainsKey(exchangeHedge))
                        {
                            securityNameFutures = RemoveSecuritySymbol(_securitySpot[token].Item[exchangeHedge].Split('|')[1]);
                            lotFutures = GetLot(_securitySpot[token].Item[exchangeHedge].Split('|')[1]);
                        }                        
                    }

                    if (!_spotData.ContainsKey(token))
                    {
                        _spotData[token] = (new Dictionary<string, SpotData>(), true);
                    }

                    if (!_spotData[token].Item.ContainsKey(exchange))
                    {
                        _spotData[token].Item[exchange] = new SpotData();
                    }

                    for (int j = 0; j < TabsScreener[i].Tabs.Count; j++)
                    {
                        BotTabSimple tab = TabsScreener[i].Tabs[j];

                        if (tab.Security?.Name == securityNameSpot)
                        {
                            BotTabSimple tabHedge = FindExchangeHedge(securityNameFutures, exchangeHedge);

                            SpotData spotData = _spotData[token].Item[exchange];
                            spotData.BestAsk = tab.PriceBestAsk;
                            spotData.BestBid = tab.PriceBestBid;
                            spotData.Volume24h = Math.Round(tab.SecurityVolumes.Volume24hUSDT);
                            spotData.LotSpot = lotSpot;
                            spotData.LotFutures = lotFutures;

                            if (tabHedge != null)
                            {
                                spotData.PriceHedge = tabHedge.PriceBestAsk;
                                spotData.Rate = Math.Round(tabHedge.Funding.CurrentValue, 6);
                                spotData.Volume24hFutures = Math.Round(tabHedge.SecurityVolumes.Volume24hUSDT);

                                if (tabHedge.Funding.NextFundingTime != new DateTime(1970, 1, 1, 0, 0, 0))
                                {
                                    spotData.ExpirationTimeFunding = (tabHedge.Funding.NextFundingTime - DateTime.UtcNow).ToString(@"hh\:mm\:ss");
                                }

                                spotData.FundingPeriod = tabHedge.Funding.FundingIntervalHours;
                                spotData.TabHedge = tabHedge;
                            }

                            if (tab.CandlesAll?.Count > 0)
                            {
                                spotData.PriceSpot = tab.CandlesAll[^1].Close;
                            }

                            spotData.Tab = tab;

                            _spotData[token].Item[exchange] = spotData;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.StackTrace, Logging.LogMessageType.Error);
            }
        }

        private int GetLot(string str)
        {
            int indexStart = str.IndexOf('{') + 1;
            int indexEnd = str.IndexOf('}');

            if (indexStart == -1 || indexEnd == -1)
            {
                return 1;
            }

            int result = 1;

            if (int.TryParse(str.Substring(indexStart, indexEnd - indexStart), out result))
            {
                return result;
            }

           return 1;
        }

        private string RemoveSecuritySymbol(string str)
        {
            int index = str.IndexOf('{');

            if (index == -1)
            {
                return str;
            }

            return str.Remove(index);
        }

        private void TryDeleteTokenFromTable(string token)
        {
            if (_spotData.ContainsKey(token))
            {
                _spotData.Remove(token);
            }
        }

        private string GetHedgeExchange(ServerType exchange)
        {
            switch (exchange)
            {
                case ServerType.Binance:
                    return ServerType.BinanceFutures.ToString();

                case ServerType.BitGetSpot:
                    return ServerType.BitGetFutures.ToString();

                case ServerType.HTXSpot:
                    return ServerType.HTXSwap.ToString();

                case ServerType.BingXSpot:
                    return ServerType.BingXFutures.ToString();

                case ServerType.KuCoinSpot:
                    return ServerType.KuCoinFutures.ToString();

                case ServerType.GateIoSpot:
                    return ServerType.GateIoFutures.ToString();
            }

            return exchange.ToString();
        }

        private BotTabSimple FindExchangeHedge(string securityNameFutures, string server)
        {
            for (int i = 0; i < TabsScreener.Count; i++)
            {
                if (TabsScreener[i].ServerType.ToString() == server)
                {
                    for (int j = 0; j < TabsScreener[i].Tabs.Count; j++)
                    {
                        if (TabsScreener[i].Tabs[j].Security?.Name == securityNameFutures)
                        {
                            return TabsScreener[i].Tabs[j];
                        }
                    }
                }
            }
            
            return null;
        }

        #endregion

        #region Spot Table

        private WindowsFormsHost _hostSpot;
        private DataGridView _gridSpot;
        private Dictionary<string, (Dictionary<string, SpotData> Item, bool OnOff)> _spotDataTable = new();
        private List<SpotGeneralTable> _spotGeneralTablesOn;
        private List<SpotGeneralTable> _spotGeneralTablesOff;
        private object _lockSpotTable = new object();

        private void CreateTableSpot()
        {
            _hostSpot = new WindowsFormsHost();

            DataGridView dataGridView =
                DataGridFactory.GetDataGridView(DataGridViewSelectionMode.CellSelect,
                DataGridViewAutoSizeRowsMode.AllCells);
            dataGridView.Dock = DockStyle.Fill;
            dataGridView.ScrollBars = ScrollBars.Both;
            dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView.GridColor = Color.Gray;

            dataGridView.CellClick += DataGridView_CellClick;
            dataGridView.DataError += DataGridView_DataError;

            dataGridView.ColumnCount = 11;
            dataGridView.RowCount = 2;

            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.Programmatic;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                column.DefaultCellStyle.SelectionBackColor = dataGridView.DefaultCellStyle.BackColor;
                column.DefaultCellStyle.SelectionForeColor = dataGridView.DefaultCellStyle.ForeColor;
                column.ReadOnly = true;
            }

            dataGridView.Rows[0].DefaultCellStyle.Font = new Font(dataGridView.Font, FontStyle.Bold | FontStyle.Italic);
            dataGridView.Rows[1].DefaultCellStyle.Font = new Font(dataGridView.Font, FontStyle.Bold | FontStyle.Italic);

            dataGridView[1, 0].Value = "Токен";
            dataGridView[2, 0].Value = "Лонг спот";
            dataGridView[2, 1].Value = "цена";
            dataGridView[3, 0].Value = "Шорт спот";
            dataGridView[3, 1].Value = "цена";
            dataGridView[4, 0].Value = "Спред спот";
            dataGridView[5, 0].Value = "Хедж";
            dataGridView[5, 1].Value = "цена";
            dataGridView[6, 0].Value = "Спред хедж";
            dataGridView[7, 0].Value = "Время";
            dataGridView[7, 1].Value = "Ставка";
            dataGridView[8, 0].Value = "V и кол-во площадок";
            dataGridView[8, 1].Value = "Спот";
            dataGridView[9, 0].Value = "V и кол-во площадок";
            dataGridView[9, 1].Value = "Фьючерс";
            dataGridView[10, 0].Value = "Вкл/выкл";

            _hostSpot.Child = dataGridView;
            _gridSpot = dataGridView;
        }

        private void DataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;

            string errorMessage = $"Ошибка в ячейке [{e.ColumnIndex},{e.RowIndex}]: ";

            switch (e.Context)
            {
                case DataGridViewDataErrorContexts.Formatting:
                    errorMessage += "Ошибка форматирования";
                    break;
                case DataGridViewDataErrorContexts.Commit:
                    errorMessage += "Ошибка сохранения данных";
                    break;
                case DataGridViewDataErrorContexts.Parsing:
                    errorMessage += "Невозможно преобразовать значение";
                    break;
                case DataGridViewDataErrorContexts.Display:
                    errorMessage += "Ошибка отображения";
                    break;
                default:
                    errorMessage += "Неизвестная ошибка";
                    break;
            }

            SendNewLogMessage(errorMessage, Logging.LogMessageType.User);
        }

        private void ThreadRefreshTableSpot()
        {
            while (true)
            {
                try
                {
                    AddDataToGrid();
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                    Thread.Sleep(5000);
                }
            }
        }

        private void AddDataToGrid()
        {
            if (_regimeSpot == "Off")
            {
                return;
            }

            lock (_lockSpot)
            {
                _spotDataTable = new Dictionary<string, (Dictionary<string, SpotData> Item, bool OnOff)>(_spotData);
            }

            _spotGeneralTablesOn = GetDataTable(true);
            _spotGeneralTablesOff = GetDataTable(false);

            if (_spotGeneralTablesOn == null || _spotGeneralTablesOff == null)
            {
                return;
            }

            AddOrDeleteRowsInSpotTable();
            SortingTable();
                      
            int countRowsInTable = 2;
            int numberRow = 1;

            foreach(var item in _spotGeneralTablesOn)
            {
                if (item == null)
                {
                    continue;
                }

                _gridSpot.Rows[countRowsInTable].DefaultCellStyle.ForeColor = Color.FromArgb(154, 156, 158);
                _gridSpot.Rows[countRowsInTable].DefaultCellStyle.SelectionForeColor = Color.FromArgb(154, 156, 158);
                _gridSpot.Rows[countRowsInTable + 1].DefaultCellStyle.ForeColor = Color.FromArgb(154, 156, 158);
                _gridSpot.Rows[countRowsInTable + 1].DefaultCellStyle.SelectionForeColor = Color.FromArgb(154, 156, 158);

                FillTable(item, countRowsInTable, numberRow);

                countRowsInTable += 2;
                numberRow++;
            }

            foreach (var item in _spotGeneralTablesOff)
            {
                if (item == null)
                {
                    continue;
                }

                _gridSpot.Rows[countRowsInTable].DefaultCellStyle.ForeColor = Color.FromArgb(64, 64, 64);
                _gridSpot.Rows[countRowsInTable].DefaultCellStyle.SelectionForeColor = Color.FromArgb(64, 64, 64);
                _gridSpot.Rows[countRowsInTable + 1].DefaultCellStyle.ForeColor = Color.FromArgb(64, 64, 64);
                _gridSpot.Rows[countRowsInTable + 1].DefaultCellStyle.SelectionForeColor = Color.FromArgb(64, 64, 64);

                FillTable(item, countRowsInTable, numberRow);

                countRowsInTable += 2;
                numberRow++;
            }
        }

        private void FillTable(SpotGeneralTable item, int countRowsInTable, int numberRow)
        {
            if (MainWindow.GetDispatcher.CheckAccess() == false)
            {
                MainWindow.GetDispatcher.Invoke(new Action <SpotGeneralTable, int, int>(FillTable), item, countRowsInTable, numberRow);

                return;
            }

            _gridSpot[0, countRowsInTable].Value = numberRow;
            _gridSpot[0, countRowsInTable + 1].Value = "";

            _gridSpot[1, countRowsInTable].Value = item.Token;
            _gridSpot[1, countRowsInTable + 1].Value = "";

            _gridSpot[2, countRowsInTable].Value = item.ExchangeLong; // выбранная биржа в лонг
            _gridSpot[2, countRowsInTable + 1].Value = item.PriceLong; //последняя цена лонг

            _gridSpot[3, countRowsInTable].Value = item.ExchangeShort; // выбранная биржа в шорт
            _gridSpot[3, countRowsInTable + 1].Value = item.PriceShort; //последняя цена шорт

            _gridSpot[4, countRowsInTable].Value = item.SpreadSpotPercent + "%";
            _gridSpot[4, countRowsInTable + 1].Value = item.SpreadSpot;

            _gridSpot[5, countRowsInTable].Value = item.ExchangeHedge + " (" + item.FundingPeriod + ")";
            _gridSpot[5, countRowsInTable + 1].Value = item.HedgePrice;

            _gridSpot[6, countRowsInTable].Value = item.SpreadHedgePercent + "%";
            _gridSpot[6, countRowsInTable + 1].Value = item.SpreadHedge;

            _gridSpot[7, countRowsInTable].Value = item.FundigTime;
            _gridSpot[7, countRowsInTable + 1].Value = item.FundingRate;

            _gridSpot[8, countRowsInTable].Value = item.VolumeSpot;
            _gridSpot[8, countRowsInTable + 1].Value = item.QuantitySpot;

            _gridSpot[9, countRowsInTable].Value = item.VolumeFutures;
            _gridSpot[9, countRowsInTable + 1].Value = item.QuantityFutures;

            DataGridViewCheckBoxCell checkBoxCell = new DataGridViewCheckBoxCell();
            _gridSpot[10, countRowsInTable] = checkBoxCell;
            checkBoxCell.Value = item.OnOffToken;

            for (int i = 0; i < Application.OpenForms.Count; i++)
            {
                Form form = Application.OpenForms[i];

                if (form.Text == item.Token)
                {
                    AddDataToChildForm(form);
                }
            }
        }

        private List<SpotGeneralTable> GetDataTable(bool flag)
        {
            try
            {
                lock (_lockSpotTable)
                {
                    List<SpotGeneralTable> spotGeneralTables = new();

                    foreach (var token in _spotDataTable)
                    {
                        if (token.Value.OnOff != flag)
                        {
                            continue;
                        }

                        if (!CheckParametersSpot(token))
                        {
                            continue;
                        }

                        SpotGeneralTable sgt = new SpotGeneralTable();

                        sgt.Token = token.Key;

                        // лонг спот
                        KeyValuePair<string, SpotData> exchangeLong = ChoiseExhangeLongSpot(token.Key);

                        decimal bestBid = 0;

                        if (exchangeLong.Key != null)
                        {
                            sgt.ExchangeLong = exchangeLong.Key; // выбранная биржа в лонг                        
                            bestBid = exchangeLong.Value.BestBid * exchangeLong.Value.LotSpot;
                            sgt.PriceLong = bestBid; //последняя цена лонг
                            sgt.TabLong = exchangeLong.Value.Tab;
                        }
                        else
                        {
                            sgt.ExchangeLong = ""; // выбранная биржа в лонг
                            sgt.PriceLong = 0; //последняя цена лонг
                            bestBid = GetBid(token);
                        }

                        // шорт спот
                        KeyValuePair<string, SpotData> exchangeShort = ChoiseExhangeShortSpot(token.Key);

                        decimal bestAsk = 0;

                        if (exchangeShort.Key != null)
                        {
                            sgt.ExchangeShort = exchangeShort.Key; // выбранная биржа в шорт                        
                            bestAsk = exchangeShort.Value.BestAsk * exchangeShort.Value.LotSpot;
                            sgt.PriceShort = bestAsk; //последняя цена шорт
                            sgt.TabShort = exchangeShort.Value.Tab;
                        }
                        else
                        {
                            sgt.ExchangeShort = ""; // выбранная биржа в шорт
                            sgt.PriceShort = 0; //последняя цена шорт
                            bestAsk = GetAsk(token);
                        }

                        // спред спот
                        decimal spreadSpot = bestAsk - bestBid;

                        if (bestBid != 0)
                        {
                            sgt.SpreadSpotPercent = Math.Round(spreadSpot / bestBid * 100, 4);
                            sgt.SpreadSpot = spreadSpot;
                        }
                        else if (bestAsk == 0 || bestBid == 0)
                        {
                            sgt.SpreadSpotPercent = 0;
                            sgt.SpreadSpot = 0;
                        }

                        // Хедж/цена
                        KeyValuePair<string, SpotData> exchangeHedge = ChoiseExhangeHedge(token.Key);

                        decimal priceHedge = 0;

                        if (exchangeHedge.Key != null)
                        {
                            sgt.ExchangeHedge = exchangeHedge.Key;
                            priceHedge = exchangeHedge.Value.PriceHedge * exchangeHedge.Value.LotFutures;
                            sgt.HedgePrice = priceHedge;
                            sgt.TabHedge = exchangeHedge.Value.Tab;
                        }
                        else
                        {
                            sgt.ExchangeHedge = "";
                            sgt.HedgePrice = 0;
                        }

                        // спред хедж
                        decimal spreadHedge = priceHedge - bestBid;

                        if (bestBid != 0)
                        {
                            sgt.SpreadHedgePercent = Math.Round(spreadHedge / bestBid * 100, 4);
                            sgt.SpreadHedge = spreadHedge;
                        }
                        if (bestBid == 0 || priceHedge == 0)
                        {
                            sgt.SpreadHedgePercent = 0;
                            sgt.SpreadHedge = 0;
                        }

                        if (exchangeHedge.Key != null)
                        {
                            sgt.FundigTime = exchangeHedge.Value.ExpirationTimeFunding;
                            sgt.FundingRate = exchangeHedge.Value.Rate;
                            sgt.FundingPeriod = exchangeHedge.Value.FundingPeriod;
                        }
                        else
                        {
                            sgt.FundigTime = "";
                            sgt.FundingRate = 0;
                            sgt.FundingPeriod = 0;
                        }

                        decimal volumeSpot = 0;
                        decimal volumeFutures = 0;
                        int countExchangeSpot = 0;
                        int countExchangeFutures = 0;

                        foreach (var exchange in token.Value.Item)
                        {
                            if (exchange.Value.OnOff == false)
                            {
                                continue;
                            }

                            volumeSpot += exchange.Value.Volume24h;
                            countExchangeSpot++;

                            if (exchange.Value.Volume24hFutures != 0)
                            {
                                volumeFutures += exchange.Value.Volume24hFutures;
                                countExchangeFutures++;
                            }
                        }

                        sgt.VolumeSpot = volumeSpot;
                        sgt.QuantitySpot = countExchangeSpot;

                        sgt.VolumeFutures = volumeFutures;
                        sgt.QuantityFutures = countExchangeFutures;

                        sgt.OnOffToken = token.Value.OnOff;

                        spotGeneralTables.Add(sgt);
                    }

                    return spotGeneralTables;
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                return null;
            }
        }

        private bool CheckParametersSpot(KeyValuePair<string, (Dictionary<string, SpotData> Item, bool OnOff)> token)
        {
            decimal bestBid = 0;
            decimal bestAsk = decimal.MaxValue;
            decimal minVolumeSpot = 0;
            decimal minVolumeHedge = 0;
            decimal minRateHedge = -100;

            foreach (var exchange in token.Value.Item)
            {
                if (exchange.Value.BestBid > bestBid)
                {
                    bestBid = exchange.Value.BestBid;
                }

                if (exchange.Value.BestAsk < bestAsk)
                {
                    bestAsk = exchange.Value.BestAsk;
                }

                if (exchange.Value.Rate > minRateHedge)
                {
                    minRateHedge = exchange.Value.Rate;
                }

                minVolumeSpot += exchange.Value.Volume24h;
                minVolumeHedge += exchange.Value.Volume24hFutures;
            }

            if (bestBid != 0)
            {
                if ((bestAsk - bestBid) / bestBid * 100 >= _setSpotMinSpread.ValueDecimal)
                {
                    return true;
                }
            }            

            if (minRateHedge >= _setSpotMinRateHedge.ValueDecimal)
            {
                return true;
            }

            if (minVolumeSpot >= _setSpotMinVolume.ValueDecimal)
            {
                return true;
            }

            if (minVolumeHedge >= _setSpotMinVolumeHedge.ValueDecimal)
            {
                return true;
            }

            return false;
        }

        private decimal GetBid(KeyValuePair<string, (Dictionary<string, SpotData> Item, bool OnOff)> token)
        {
            try
            {
                decimal bid = decimal.MaxValue;

                foreach (var exchange in token.Value.Item)
                {
                    if (exchange.Value.BestBid == 0)
                    {
                        continue;
                    }

                    if (exchange.Value.BestBid < bid)
                    {
                        bid = exchange.Value.BestBid / exchange.Value.LotSpot;
                    }
                }

                if (bid == decimal.MaxValue)
                {
                    bid = 0;
                }

                return bid;

            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.StackTrace, Logging.LogMessageType.Error);
                return 0;
            }
        }

        private decimal GetAsk(KeyValuePair<string, (Dictionary<string, SpotData> Item, bool OnOff)> token)
        {
            try
            {
                decimal ask = decimal.MinValue;

                foreach (var exchange in token.Value.Item)
                {
                    if (exchange.Value.BestAsk == 0)
                    {
                        continue;
                    }

                    if (exchange.Value.BestAsk > ask)
                    {
                        ask = exchange.Value.BestAsk / exchange.Value.LotSpot;
                    }
                }

                if (ask == decimal.MinValue)
                {
                    ask = 0;
                }

                return ask;
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.StackTrace, Logging.LogMessageType.Error);
                return 0;
            }
        }

        private SortDirection _sortDirection = SortDirection.None;
        private int _columnSorting = 0;

        private void SortingTable()
        {
            if (_spotGeneralTablesOn.Count < 1)
            {
                return;
            }

            if (_sortDirection != SortDirection.None && _columnSorting != 0)
            {
                if (_sortDirection == SortDirection.Ascending)
                {
                    List<SpotGeneralTable> sortedList = new();

                    switch (_columnSorting)
                    {
                        case 1:
                            sortedList = _spotGeneralTablesOn.OrderBy(spot => spot.Token).ToList();
                            break;
                        case 2:
                            sortedList = _spotGeneralTablesOn.OrderBy(spot => spot.PriceLong).ToList();
                            break;
                        case 3:
                            sortedList = _spotGeneralTablesOn.OrderBy(spot => spot.PriceShort).ToList();
                            break;
                        case 4:
                            sortedList = _spotGeneralTablesOn.OrderBy(spot => spot.SpreadSpotPercent).ToList();
                            break;
                        case 5:
                            sortedList = _spotGeneralTablesOn.OrderBy(spot => spot.HedgePrice).ToList();
                            break;
                        case 6:
                            sortedList = _spotGeneralTablesOn.OrderBy(spot => spot.SpreadHedgePercent).ToList();
                            break;
                        case 7:
                            sortedList = _spotGeneralTablesOn.OrderBy(spot => spot.FundingRate).ToList();
                            break;
                        case 8:
                            sortedList = _spotGeneralTablesOn.OrderBy(spot => spot.VolumeSpot).ToList();
                            break;
                        case 9:
                            sortedList = _spotGeneralTablesOn.OrderBy(spot => spot.VolumeFutures).ToList();
                            break;
                    }                    

                    _spotGeneralTablesOn.Clear();

                    if (sortedList != null)
                    {
                        _spotGeneralTablesOn.AddRange(sortedList);
                    }
                }
                else if (_sortDirection == SortDirection.Descending)
                {
                    List<SpotGeneralTable> sortedList = new();

                    switch (_columnSorting)
                    {
                        case 1:
                            sortedList = _spotGeneralTablesOn.OrderByDescending(spot => spot.Token).ToList();
                            break;
                        case 2:
                            sortedList = _spotGeneralTablesOn.OrderByDescending(spot => spot.PriceLong).ToList();
                            break;
                        case 3:
                            sortedList = _spotGeneralTablesOn.OrderByDescending(spot => spot.PriceShort).ToList();
                            break;
                        case 4:
                            sortedList = _spotGeneralTablesOn.OrderByDescending(spot => spot.SpreadSpotPercent).ToList();
                            break;
                        case 5:
                            sortedList = _spotGeneralTablesOn.OrderByDescending(spot => spot.HedgePrice).ToList();
                            break;
                        case 6:
                            sortedList = _spotGeneralTablesOn.OrderByDescending(spot => spot.SpreadHedgePercent).ToList();
                            break;
                        case 7:
                            sortedList = _spotGeneralTablesOn.OrderByDescending(spot => spot.FundingRate).ToList();
                            break;
                        case 8:
                            sortedList = _spotGeneralTablesOn.OrderByDescending(spot => spot.VolumeSpot).ToList();
                            break;
                        case 9:
                            sortedList = _spotGeneralTablesOn.OrderByDescending(spot => spot.VolumeFutures).ToList();
                            break;
                    }

                    _spotGeneralTablesOn.Clear();

                    if (sortedList != null)
                    {
                        _spotGeneralTablesOn.AddRange(sortedList);
                    }
                }

                UpdateSortGlyph();
            }
        }

        private void UpdateSortGlyph()
        {
            foreach (DataGridViewColumn columns in _gridSpot.Columns)
            {
                columns.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            var column = _gridSpot.Columns[_columnSorting];

            if (column != null)
            {
                column.HeaderCell.SortGlyphDirection = _sortDirection == SortDirection.Ascending
                    ? SortOrder.Ascending
                    : SortOrder.Descending;
            }
        }

        private void AddDataToChildForm(Form form)
        {
            if (MainWindow.GetDispatcher.CheckAccess() == false)
            {
                MainWindow.GetDispatcher.Invoke(new Action<Form>(AddDataToChildForm), form);

                return;
            }

            string token = form.Text;

            DataGridView dataGridView = null;

            foreach (Control control in form.Controls)
            {
                if (control is DataGridView dgv)
                {
                    dataGridView = dgv;
                }
            }

            if (dataGridView == null)
            {
                return;
            }

            int count = 0;

            lock (_lockSpotTable)
            {
                foreach (KeyValuePair<string, SpotData> exchange in _spotDataTable[token].Item)
                {
                    if (exchange.Value.OnOff == true)
                    {
                        dataGridView.Rows[count].DefaultCellStyle.ForeColor = Color.FromArgb(154, 156, 158);
                        dataGridView.Rows[count].DefaultCellStyle.SelectionForeColor = Color.FromArgb(154, 156, 158);
                        DataToChildForm(dataGridView, count, exchange);

                        count++;
                    }
                }
            }

            lock (_lockSpotTable)
            {
                foreach (KeyValuePair<string, SpotData> exchange in _spotDataTable[token].Item)
                {
                    if (exchange.Value.OnOff == false)
                    {
                        dataGridView.Rows[count].DefaultCellStyle.ForeColor = Color.FromArgb(64, 64, 64);
                        dataGridView.Rows[count].DefaultCellStyle.SelectionForeColor = Color.FromArgb(64, 64, 64);
                        DataToChildForm(dataGridView, count, exchange);

                        count++;
                    }
                }
            }
        }

        private void DataToChildForm(DataGridView dataGridView, int count, KeyValuePair<string, SpotData> exchange)
        {
            dataGridView.CellValueChanged -= DataGridView_CellValueChanged;
            dataGridView.CurrentCellDirtyStateChanged -= DataGridView_CurrentCellDirtyStateChanged;

            dataGridView[0, count].Value = exchange.Key;
            dataGridView[1, count].Value = exchange.Value.PriceSpot * exchange.Value.LotSpot;
            dataGridView[2, count].Value = exchange.Value.BestBid * exchange.Value.LotSpot;
            dataGridView[3, count].Value = exchange.Value.ChoiseLong;
            dataGridView[4, count].Value = exchange.Value.BestAsk * exchange.Value.LotSpot;
            dataGridView[5, count].Value = exchange.Value.ChoiseShort;
            dataGridView[6, count].Value = exchange.Value.Volume24h;
            dataGridView[7, count].Value = exchange.Value.PriceHedge * exchange.Value.LotFutures;
            dataGridView[8, count].Value = exchange.Value.Rate;
            dataGridView[9, count].Value = exchange.Value.ExpirationTimeFunding;
            dataGridView[10, count].Value = exchange.Value.Volume24hFutures;
            dataGridView[11, count].Value = exchange.Value.ChoiseFutures;
            dataGridView[12, count].Value = exchange.Value.OnOff;

            dataGridView.CellValueChanged += DataGridView_CellValueChanged;
            dataGridView.CurrentCellDirtyStateChanged += DataGridView_CurrentCellDirtyStateChanged;
        }

        private void DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            // сортировка
            if (e.ColumnIndex > 0 && e.ColumnIndex < 10)
            {
                if (e.RowIndex == 0 || e.RowIndex == 1)
                {
                    SetSortDirection(e.ColumnIndex);
                    return;
                }
            }

            string token = "";
            int rowIndex = e.RowIndex;

            if (e.RowIndex % 2 != 0)
            {
                rowIndex = e.RowIndex - 1;
            }

            token = _gridSpot.Rows[rowIndex].Cells[1].Value?.ToString();

            if (token == null || token == "")
            {
                return;
            }

            // открытие дополнительного окна
            if (e.ColumnIndex == 1)
            {
                try
                {

                    if (e.RowIndex > 1)
                    {
                        if (_gridSpot.Rows[rowIndex].Cells[1].Tag == null || (int)_gridSpot.Rows[rowIndex].Cells[1].Tag == 0)
                        {
                            foreach (Form form in Application.OpenForms)
                            {
                                if (form.Text == token)
                                {
                                    form.BringToFront();
                                    form.WindowState = FormWindowState.Normal;
                                    return;
                                }
                            }

                            Form childForm = new Form();
                            childForm.Text = token;
                            childForm.AutoSize = true;
                            childForm.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                            childForm.Controls.Add(CreateChildTable(token));
                            AddDataToChildForm(childForm);
                            childForm.Show();

                            childForm.FormClosed += (sender, e) =>
                            {
                                childForm.Dispose();
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                }
            }

            // чек бокс
            if (e.ColumnIndex == 10 && e.RowIndex > 1)
            {
                lock (_lockSpotTable)
                {
                    if (_spotDataTable[token].OnOff)
                    {
                        _spotDataTable[token] = (_spotDataTable[token].Item, false);
                    }
                    else
                    {
                        _spotDataTable[token] = (_spotDataTable[token].Item, true);
                    }
                }
            }
        }

        private void SetSortDirection(int column)
        {
            if (_columnSorting != column)
            {
                _columnSorting = column;
                _sortDirection = SortDirection.Ascending;
            }
            else
            {
                if (_sortDirection == SortDirection.None)
                {
                    _sortDirection = SortDirection.Ascending;
                }
                else if (_sortDirection == SortDirection.Ascending)
                {
                    _sortDirection = SortDirection.Descending;
                }
                else if (_sortDirection == SortDirection.Descending)
                {
                    _sortDirection = SortDirection.Ascending;
                }
            }
        }

        private DataGridView CreateChildTable(string token)
        {
            DataGridView dataGridView =
                DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect,
                DataGridViewAutoSizeRowsMode.AllCells);
            dataGridView.Dock = DockStyle.Fill;
            dataGridView.ScrollBars = ScrollBars.Both;
            dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.Black;
            dataGridView.ColumnHeadersDefaultCellStyle.Font = new Font(dataGridView.Font, FontStyle.Bold | FontStyle.Italic);
            dataGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = dataGridView.ColumnHeadersDefaultCellStyle.BackColor;
            dataGridView.ColumnHeadersDefaultCellStyle.SelectionForeColor = dataGridView.ColumnHeadersDefaultCellStyle.ForeColor;
            dataGridView.GridColor = Color.Gray;
            dataGridView.AutoSize = true;
            dataGridView.AllowUserToResizeRows = false;
            dataGridView.AllowUserToResizeColumns = false;
            dataGridView.MultiSelect = false;

            dataGridView.ColumnCount = 13;
            dataGridView.RowCount = 0;

            string[] listTable = new[] { "Площадка", "Цена сделки", "Лонг", "Выбор", "Шорт", "Выбор", "Объем на споте", "Хедж", "Ставка", "Время", "Объем на фьючерсе", "Выбор", "Вкл/Выкл" };

            for (int i = 0; i < dataGridView.ColumnCount; i++)
            {
                DataGridViewColumn column = dataGridView.Columns[i];

                column.SortMode = DataGridViewColumnSortMode.Programmatic;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                column.DefaultCellStyle.SelectionBackColor = dataGridView.DefaultCellStyle.BackColor;
                column.DefaultCellStyle.SelectionForeColor = dataGridView.DefaultCellStyle.ForeColor;
                column.ReadOnly = true;

                column.HeaderText = listTable[i];
            }

            lock (_lockSpotTable)
            {
                if (_spotDataTable.TryGetValue(token, out var spotData))
                {
                    for (int rowIndex = 0; rowIndex < spotData.Item.Count; rowIndex++)
                    {
                        dataGridView.Rows.Add();

                        int[] checkboxColumns = { 3, 5, 11, 12 };
                        foreach (int colIndex in checkboxColumns)
                        {

                            dataGridView.Rows[rowIndex].Cells[colIndex] = new DataGridViewCheckBoxCell()
                            {
                                Value = false,
                                TrueValue = true,
                                FalseValue = false
                            };

                            dataGridView.Rows[rowIndex].Cells[colIndex].ReadOnly = false;
                        }
                    }
                }
            }

            dataGridView.Tag = token;

            dataGridView.CellValueChanged += DataGridView_CellValueChanged;
            dataGridView.CurrentCellDirtyStateChanged += DataGridView_CurrentCellDirtyStateChanged;

            return dataGridView;
        }

        private void DataGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            DataGridView dataGridView = sender as DataGridView;

            if (dataGridView.IsCurrentCellDirty)
                dataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void DataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dataGridView = sender as DataGridView;
            
            if (e.ColumnIndex == 3)
            {
                SetBoolChoise(sender, e, e.ColumnIndex);                
            }
            if (e.ColumnIndex == 5)
            {
                SetBoolChoise(sender, e, e.ColumnIndex);
            }
            if (e.ColumnIndex == 11)
            {
                SetBoolChoise(sender, e, e.ColumnIndex);
            }
            if (e.ColumnIndex == 12)
            {
                string token = dataGridView.Tag.ToString();
                string exchange = dataGridView.Rows[e.RowIndex].Cells[0].Value.ToString();
                bool value = (bool)dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

                if (exchange != null || exchange != "")
                {
                    lock (_lockSpotTable)
                    {
                        foreach (var item in _spotDataTable[token].Item)
                        {
                            if (item.Key == exchange)
                            {
                                if (value)
                                {
                                    _spotDataTable[token].Item[exchange].OnOff = true;
                                }
                                else
                                {
                                    _spotDataTable[token].Item[exchange].OnOff = false;
                                    _spotDataTable[token].Item[exchange].ChoiseFutures = false;
                                    _spotDataTable[token].Item[exchange].ChoiseLong = false;
                                    _spotDataTable[token].Item[exchange].ChoiseShort = false;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SetBoolChoise(object sender, DataGridViewCellEventArgs e, int columnIndex)
        {
            DataGridView dataGridView = sender as DataGridView;

            if ((bool)dataGridView.Rows[e.RowIndex].Cells[12].Value == false)
            {
                dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = false;
                return;
            }

            string token = dataGridView.Tag.ToString();
            string exchange = dataGridView.Rows[e.RowIndex].Cells[0].Value.ToString();
            bool value = (bool)dataGridView.Rows[e.RowIndex].Cells[columnIndex].Value;

            if (exchange != null || exchange != "")
            {
                lock (_lockSpotTable)
                {
                    foreach (var item in _spotDataTable[token].Item)
                    {
                        if (item.Key == exchange)
                        {
                            if (columnIndex == 3)
                            {
                                _spotDataTable[token].Item[exchange].ChoiseLong = (bool)dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                            }
                            else if (columnIndex == 5)
                            {
                                _spotDataTable[token].Item[exchange].ChoiseShort = (bool)dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                            }
                            else if (columnIndex == 11)
                            {
                                _spotDataTable[token].Item[exchange].ChoiseFutures = (bool)dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                            }
                        }
                    }
                }

                if (value)
                {
                    for (int i = 0; i < dataGridView.Rows.Count; i++)
                    {
                        if (i != e.RowIndex)
                        {
                            dataGridView.Rows[i].Cells[columnIndex].Value = false;
                        }
                    }
                }
            }
        }

        private void AddOrDeleteRowsInSpotTable()
        {
            if (MainWindow.GetDispatcher.CheckAccess() == false)
            {
                MainWindow.GetDispatcher.Invoke(AddOrDeleteRowsInSpotTable);

                return;
            }

            int countNeedRows = _gridSpot.RowCount - ((_spotGeneralTablesOn.Count + _spotGeneralTablesOff.Count) * 2 + 2);
                        
            if (countNeedRows < 0)
            {
                for (int i = 0; i < Math.Abs(countNeedRows); i++)
                {
                    _gridSpot.Rows.Add();
                }
            }
            else if (countNeedRows > 0)
            {
                for (int i = 0; i < countNeedRows; i++)
                {
                    _gridSpot.Rows.RemoveAt(_gridSpot.RowCount - 1);
                }
            }
        }

        private KeyValuePair<string, SpotData> ChoiseExhangeLongSpot(string key)
        {
            lock (_lockSpotTable)
            {
                foreach (var exchange in _spotDataTable[key].Item)
                {
                    if (exchange.Value.ChoiseLong)
                    {
                        return exchange;
                    }
                }
            }
            return default;
        }

        private KeyValuePair<string, SpotData> ChoiseExhangeShortSpot(string key)
        {
            lock (_lockSpotTable)
            {
                foreach (var exchange in _spotDataTable[key].Item)
                {
                    if (exchange.Value.ChoiseShort)
                    {
                        return exchange;
                    }
                }
            }
            return default;
        }

        private KeyValuePair<string, SpotData> ChoiseExhangeHedge(string key)
        {
            lock (_lockSpotTable)
            {
                foreach (var exchange in _spotDataTable[key].Item)
                {
                    if (exchange.Value.ChoiseFutures)
                    {
                        return exchange;
                    }
                }
            }
            return default;
        }

        #endregion

        #region Total

        private WindowsFormsHost _hostTotal;
        private DataGridView _gridTotal;

        private void CreateTableTotal()
        {
            _hostTotal = new WindowsFormsHost();

            DataGridView newGrid =
               DataGridFactory.GetDataGridView(DataGridViewSelectionMode.RowHeaderSelect,
               DataGridViewAutoSizeRowsMode.AllCells);

            newGrid.Dock = DockStyle.Fill;
            newGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            newGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            newGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            newGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.Black;
            newGrid.ColumnHeadersDefaultCellStyle.Font = new Font(newGrid.Font, FontStyle.Bold | FontStyle.Italic);
            newGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = newGrid.ColumnHeadersDefaultCellStyle.BackColor;
            newGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = newGrid.ColumnHeadersDefaultCellStyle.ForeColor;
            newGrid.GridColor = Color.Gray;

            for (int i = 1; i < 34; i++)
            {
                newGrid.Columns.Add("Col" + i, "");
            }

            for (int i = 0; i < newGrid.Columns.Count; i++)
            {
                newGrid.Columns[i].ReadOnly = true;
                DataGridViewColumn column = newGrid.Columns[i];
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                column.DefaultCellStyle.SelectionBackColor = newGrid.DefaultCellStyle.BackColor;
                column.DefaultCellStyle.SelectionForeColor = newGrid.DefaultCellStyle.ForeColor;
                column.ReadOnly = true;
            }

            DataGridViewRow newRow = new DataGridViewRow();
            newGrid.Rows.Add(newRow);

            newGrid.Rows[0].Cells[0].Value = "Позиция";
            newGrid.Rows[0].Cells[7].Value = "Открытая позиция";
            newGrid.Rows[0].Cells[10].Value = "Текущая позиция";
            newGrid.Rows[0].Cells[18].Value = "Закрытая позиция";
            newGrid.Rows[0].Cells[23].Value = "Финансирование";
            newGrid.Rows[0].Cells[27].Value = "Margin";
            newGrid.Rows[0].Cells[31].Value = "Доходность";

            newRow = new DataGridViewRow();

            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Биржа" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Инструмент" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Направление" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Частота" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Время" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Вид маржи" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Плечо" });

            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Кол-во" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Цена" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Сумма" });

            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Кол-во" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Цена" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Сумма" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Unrealized P&L, $" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Unrealized P&L, %" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Ставка фондирования, $" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Ставка фондирования, %" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Ожид доходность" });

            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Кол-во" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Цена" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Сумма" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Realized P&L, $" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Realized P&L, %" });

            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Комиссия,$" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Комиссия,%" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Фондирование" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Сумма,$" });

            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Цена ликв" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "MM" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "IM" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "СВОБ" });

            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Лимит" });
            newRow.Cells.Add(new DataGridViewTextBoxCell() { Value = "Маржа" });

            newGrid.Rows.Add(newRow);

            newRow = new DataGridViewRow();
            newGrid.Rows.Add(newRow);

            newGrid.Rows[newGrid.RowCount - 1].Cells[0].Value = "ИТОГ";

            _hostTotal.Child = newGrid;
            _gridTotal = newGrid;
        }

        private void ThreadRefreshTableTotal()
        {
            while (true)
            {
                try
                {
                    if (_regimeFunding == "On")
                    {
                        AddDataToTotalTable();
                    }
                    
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                }
            }
        }

        private void AddDataToTotalTable()
        {
            if (MainWindow.GetDispatcher.CheckAccess() == false)
            {
                MainWindow.GetDispatcher.Invoke(AddDataToTotalTable);

                return;
            }

            DeleteRowsInTotalTable();

            lock (_lockFundingTable)
            {
                if (_fundingGeneralTablesOn != null)
                {
                    for (int i = 0; i < _fundingGeneralTablesOn.Count; i++)
                    {
                        if (_fundingGeneralTablesOn[i].TabLong == null || _fundingGeneralTablesOn[i].TabShort == null)
                        {
                            continue;
                        }

                        if (_fundingGeneralTablesOn[i].ExchangeLong == null || _fundingGeneralTablesOn[i].ExchangeLong == "")
                        {
                            continue;
                        }

                        if (_fundingGeneralTablesOn[i].ExchangeShort == null || _fundingGeneralTablesOn[i].ExchangeShort == "")
                        {
                            continue;
                        }

                        for (int j = 0; j < 3; j++)
                        {
                            DataGridViewRow newRow = new DataGridViewRow();
                            int rowIndex = _gridTotal.Rows.Count - 1;
                            _gridTotal.Rows.Insert(rowIndex, newRow);
                        }

                        _gridTotal.Rows[^4].Cells[0].Value = _fundingGeneralTablesOn[i].ExchangeLong;
                        _gridTotal.Rows[^3].Cells[0].Value = _fundingGeneralTablesOn[i].ExchangeShort;
                        _gridTotal.Rows[^2].Cells[0].Value = "Spread";

                        _gridTotal.Rows[^4].Cells[1].Value = _fundingGeneralTablesOn[i].TabLong?.Security?.Name;
                        _gridTotal.Rows[^3].Cells[1].Value = _fundingGeneralTablesOn[i].TabShort?.Security?.Name;

                        _gridTotal.Rows[^4].Cells[2].Value = "Long";
                        _gridTotal.Rows[^3].Cells[2].Value = "Short";

                        _gridTotal.Rows[^4].Cells[3].Value = _fundingGeneralTablesOn[i].TabLong?.Funding.FundingIntervalHours;
                        _gridTotal.Rows[^3].Cells[3].Value = _fundingGeneralTablesOn[i].TabShort.Funding.FundingIntervalHours;

                        _gridTotal.Rows[^4].Cells[4].Value = (_fundingGeneralTablesOn[i].TabLong.Funding.NextFundingTime - DateTime.UtcNow).ToString(@"hh\:mm\:ss");
                        _gridTotal.Rows[^3].Cells[4].Value = (_fundingGeneralTablesOn[i].TabShort.Funding.NextFundingTime - DateTime.UtcNow).ToString(@"hh\:mm\:ss");

                        decimal spread = 0;

                        if (_fundingGeneralTablesOn[i].TabLong.CandlesAll != null && _fundingGeneralTablesOn[i].TabShort.CandlesAll != null)
                        {
                            if (_fundingGeneralTablesOn[i].TabLong.CandlesAll.Count > 0 && _fundingGeneralTablesOn[i].TabShort.CandlesAll.Count > 0)
                            {
                                decimal priceLong = _fundingGeneralTablesOn[i].TabLong.CandlesAll[^1].Close;
                                decimal priceShort = _fundingGeneralTablesOn[i].TabShort.CandlesAll[^1].Close;
                                spread = priceShort - priceLong;

                                _gridTotal.Rows[^4].Cells[11].Value = priceLong;
                                _gridTotal.Rows[^3].Cells[11].Value = priceShort;
                                _gridTotal.Rows[^2].Cells[11].Value = spread;

                                if (priceLong != 0)
                                {
                                    _gridTotal.Rows[^2].Cells[12].Value = Math.Round(spread / priceLong, 4);
                                }
                            }
                        }

                        decimal spreadFunding = Math.Round(_fundingGeneralTablesOn[i].TabShort.Funding.CurrentValue - _fundingGeneralTablesOn[i].TabLong.Funding.CurrentValue, 6);
                        _gridTotal.Rows[^4].Cells[15].Value = Math.Round(_fundingGeneralTablesOn[i].TabLong.Funding.CurrentValue, 6);
                        _gridTotal.Rows[^3].Cells[15].Value = Math.Round(_fundingGeneralTablesOn[i].TabShort.Funding.CurrentValue, 6);
                        _gridTotal.Rows[^2].Cells[15].Value = spreadFunding;

                        decimal comissLong = _comission[_fundingGeneralTablesOn[i].TabLong.Security?.Exchange].FuturesTaker;
                        decimal comissShort = _comission[_fundingGeneralTablesOn[i].TabLong.Security?.Exchange].FuturesMaker;
                        decimal sumComiss = comissLong + comissShort;

                        _gridTotal.Rows[^4].Cells[24].Value = comissLong;
                        _gridTotal.Rows[^3].Cells[24].Value = comissShort;
                        _gridTotal.Rows[^2].Cells[24].Value = sumComiss;

                        _gridTotal.Rows[^2].Cells[31].Value = spreadFunding + spread - sumComiss * 2;
                    }
                }
            }

            lock (_spotDataTable)
            {
                if (_spotGeneralTablesOn != null)
                {

                    for (int i = 0; i < _spotGeneralTablesOn.Count; i++)
                    {
                        if (_spotGeneralTablesOn[i].TabLong == null ||
                            _spotGeneralTablesOn[i].TabShort == null ||
                            _spotGeneralTablesOn[i].TabHedge == null)
                        {
                            continue;
                        }

                        if (_spotGeneralTablesOn[i].ExchangeLong == null ||
                            _spotGeneralTablesOn[i].ExchangeLong == "" ||
                            _spotGeneralTablesOn[i].TabLong == null)
                        {
                            continue;
                        }

                        if (_spotGeneralTablesOn[i].ExchangeShort == null ||
                            _spotGeneralTablesOn[i].ExchangeShort == "" ||
                            _spotGeneralTablesOn[i].TabShort == null)
                        {
                            continue;
                        }

                        if (_spotGeneralTablesOn[i].ExchangeHedge == null ||
                            _spotGeneralTablesOn[i].ExchangeHedge == "" ||
                            _spotGeneralTablesOn[i].TabHedge == null)
                        {
                            continue;
                        }

                        for (int j = 0; j < 4; j++)
                        {
                            DataGridViewRow newRow = new DataGridViewRow();
                            int rowIndex = _gridTotal.Rows.Count - 1;
                            _gridTotal.Rows.Insert(rowIndex, newRow);
                        }

                        _gridTotal.Rows[^5].Cells[0].Value = _spotGeneralTablesOn[i].ExchangeLong;
                        _gridTotal.Rows[^4].Cells[0].Value = _spotGeneralTablesOn[i].ExchangeShort;
                        _gridTotal.Rows[^3].Cells[0].Value = _spotGeneralTablesOn[i].ExchangeHedge;
                        _gridTotal.Rows[^2].Cells[0].Value = "Spread";

                        _gridTotal.Rows[^5].Cells[1].Value = _spotGeneralTablesOn[i].TabLong?.Security?.Name;
                        _gridTotal.Rows[^4].Cells[1].Value = _spotGeneralTablesOn[i].TabShort?.Security?.Name;
                        _gridTotal.Rows[^3].Cells[1].Value = _spotGeneralTablesOn[i].TabHedge?.Security?.Name;

                        _gridTotal.Rows[^5].Cells[2].Value = "Long";
                        _gridTotal.Rows[^4].Cells[2].Value = "Short";
                        _gridTotal.Rows[^3].Cells[2].Value = "Hedge";

                        _gridTotal.Rows[^3].Cells[3].Value = _spotGeneralTablesOn[i].TabHedge?.Funding.FundingIntervalHours;

                        _gridTotal.Rows[^3].Cells[4].Value = _spotGeneralTablesOn[i].FundigTime;

                        decimal spread = 0;

                        if (_spotGeneralTablesOn[i].TabLong?.CandlesAll != null &&
                            _spotGeneralTablesOn[i].TabShort?.CandlesAll != null &&
                            _spotGeneralTablesOn[i].TabHedge?.CandlesAll != null)
                        {
                            if (_spotGeneralTablesOn[i].TabLong.CandlesAll.Count > 0 &&
                                _spotGeneralTablesOn[i].TabShort.CandlesAll.Count > 0 &&
                                _spotGeneralTablesOn[i].TabHedge.CandlesAll.Count > 0)
                            {
                                decimal priceLong = _spotGeneralTablesOn[i].TabLong.CandlesAll[^1].Close;
                                decimal priceShort = _spotGeneralTablesOn[i].TabShort.CandlesAll[^1].Close;
                                decimal priceHedge = _spotGeneralTablesOn[i].TabHedge.CandlesAll[^1].Close;
                                spread = priceShort - priceLong;

                                _gridTotal.Rows[^5].Cells[11].Value = priceLong;
                                _gridTotal.Rows[^4].Cells[11].Value = priceShort;
                                _gridTotal.Rows[^3].Cells[11].Value = priceHedge;
                                _gridTotal.Rows[^2].Cells[11].Value = spread;

                                if (priceLong != 0)
                                {
                                    _gridTotal.Rows[^2].Cells[12].Value = Math.Round(spread / priceLong, 4);
                                }
                            }
                        }

                        _gridTotal.Rows[^3].Cells[15].Value = Math.Round(_spotGeneralTablesOn[i].TabHedge.Funding.CurrentValue, 6);

                        decimal comissLong = _comission[_spotGeneralTablesOn[i].TabLong.Security?.Exchange].SpotTaker;
                        decimal comissShort = _comission[_spotGeneralTablesOn[i].TabLong.Security?.Exchange].SpotMaker;
                        decimal sumComiss = comissLong + comissShort;

                        _gridTotal.Rows[^5].Cells[24].Value = comissLong;
                        _gridTotal.Rows[^4].Cells[24].Value = comissShort;
                        _gridTotal.Rows[^2].Cells[24].Value = sumComiss;

                        _gridTotal.Rows[^2].Cells[31].Value = spread - sumComiss * 2;
                    }
                }
            }
        }

        private void DeleteRowsInTotalTable()
        {
            for (int i = _gridTotal.Rows.Count - 2; i > 1; i--)
            {
                _gridTotal.Rows.RemoveAt(i);
            }

            /*int countNeedRows = _gridTotal.RowCount - ((_fundingGeneralTablesOn.Count * 3 + _spotGeneralTablesOn.Count) * 4 + 3);

            if (countNeedRows < 0)
            {
                for (int i = 0; i < Math.Abs(countNeedRows); i++)
                {
                    _gridTotal.Rows.Add();
                }
            }
            else if (countNeedRows > 0)
            {
                for (int i = 0; i < countNeedRows; i++)
                {
                    _gridTotal.Rows.RemoveAt(_gridTotal.RowCount - 1);
                }
            }*/
        }

        #endregion

        #region Settings Security

        private WindowsFormsHost _hostSettingsSecurity;
        private DataGridView _panelButton;
        private DataGridView _panelTable;
        private Dictionary<string, (Dictionary<string, string> Item, bool OnOff)> _securitySpot = new Dictionary<string, (Dictionary<string, string> Item, bool OnOff)>();
        private Dictionary<string, (Dictionary<string, string> Item, bool OnOff)> _securityFunding = new Dictionary<string, (Dictionary<string, string> Item, bool OnOff)>();

        private void CreateTableSecuritySettings()
        {
            _hostSettingsSecurity = new WindowsFormsHost();

            _panelButton = AddPanelButton();
            _panelTable = AddPanelTable();

            TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.ColumnCount = 1;
            tableLayoutPanel.RowCount = 2;
            tableLayoutPanel.CellBorderStyle = TableLayoutPanelCellBorderStyle.None; 
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            tableLayoutPanel.Controls.Add(_panelButton, 0, 0);
            tableLayoutPanel.Controls.Add(_panelTable, 0, 1);

            _panelTable.CellBeginEdit += _panelTable_CellBeginEdit;
            _panelTable.CellEndEdit += _panelTable_CellEndEdit;
                        
            _hostSettingsSecurity.Child = tableLayoutPanel;
        }

        private bool _isManualEdit = false;

        private void _panelTable_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            _isManualEdit = true;
        }

        private void _panelTable_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (!_isManualEdit)
            {
                return;
            }

            lock (_lockSpot)
            {
                if (e.ColumnIndex > 1)
                {
                    _isManualEdit = false;

                    string token = _panelTable.Rows[e.RowIndex].Cells[1].Value.ToString();
                    string exchange = _panelTable.Columns[e.ColumnIndex].HeaderText.ToString();

                    string security = "";
                    string value = _panelTable.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();

                    if (exchange.Contains("spot"))
                    {
                        string fut = "";

                        fut = _securitySpot[token].Item[exchange.Split('(')[0]].Split("|")[1];

                        security += value + "|" + fut;
                    }
                    else if (exchange.Contains("futures"))
                    {
                        string spot = _securitySpot[token].Item[exchange.Split('(')[0]].Split("|")[0];

                        security += spot + "|" + value;
                    }
                    else
                    {
                        if (_spotServer.Contains(exchange))
                        {
                            security = _panelTable.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() + "|";
                        }
                        else
                        {
                            if (_panelTable.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == null)
                            {
                                security = "";
                            }
                            else
                            {
                                security = "|" + _panelTable.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
                            }
                        }
                    }

                    if (security == "")
                    {
                        _securitySpot[token].Item.Remove(exchange.Split('(')[0]);
                    }
                    else
                    {
                        _securitySpot[token].Item[exchange.Split('(')[0]] = security;
                    }
                }

                CopySecurityFunding();
            }
        }

        private DataGridView AddPanelButton()
        {
            DataGridView newButtonGrid =
               DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect,
               DataGridViewAutoSizeRowsMode.AllCells);

            newButtonGrid.Dock = DockStyle.Fill;
            newButtonGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            newButtonGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            newButtonGrid.CellClick += NewButtonGrid_CellClick;

            newButtonGrid.ColumnCount = 4;

            foreach (DataGridViewColumn column in newButtonGrid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            DataGridViewRow row = new DataGridViewRow();

            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[0].Value = "Загрузить инструменты с биржи";
            
            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[1].Value = "Загрузить инструменты из файла";

            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[2].Value = "Сохранить инструменты в файл";

            row.Cells.Add(new DataGridViewButtonCell());
            row.Cells[3].Value = "Добавить инструменты в скринер";

            newButtonGrid.Rows.Add(row);

            return newButtonGrid;
        }

        private async void NewButtonGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                LoadFromExchange();
            }

            if (e.ColumnIndex == 1)
            {
                await LoadSettingsSecurity();
            }

            if (e.ColumnIndex == 2)
            {
                SaveSettingsSecurity();
            }

            if (e.ColumnIndex == 3)
            {
                AddSecurityToScreener();
            }
        }
                
        private void LoadFromExchange()
        {
            lock (_lockSpot)
            {
                _securitySpot = new();

                for (int j = 0; j < TabsScreener.Count; j++)
                {
                    for (int i = 0; i < TabsScreener[j].SecuritiesNames.Count; i++)
                    {
                        SecuritySettings securitySettings = GetSecuritySettings(TabsScreener[j].ServerType, TabsScreener[j].SecuritiesNames[i]);

                        string token = GetToken(securitySettings);

                        if (token == null)
                        {
                            continue;
                        }

                        if (!_securitySpot.ContainsKey(token))
                        {
                            _securitySpot[token] = (Item: new Dictionary<string, string>(), OnOff: false);
                        }

                        string exchange = TabsScreener[j].ServerType.ToString();

                        if (!_securitySpot[token].Item.ContainsKey(exchange))
                        {
                            _securitySpot[token].Item[exchange] = null;
                        }

                        if (string.IsNullOrEmpty(_securitySpot[token].Item[exchange]))
                        {
                            if (securitySettings.SecurityClass == TypeSecurityClass.Spot)
                            {
                                //_securitySpot[token].Item[exchange] = securitySettings.SecurityName + "{1}|";
                                _securitySpot[token].Item[exchange] = securitySettings.SecurityName + "|";
                            }
                            else
                            {
                                //_securitySpot[token].Item[exchange] = "|" + securitySettings.SecurityName + "{1}";
                                _securitySpot[token].Item[exchange] = "|" + securitySettings.SecurityName;
                            }
                        }
                        else if (_securitySpot[token].Item[exchange].Contains('|'))
                        {
                            string[] str = _securitySpot[token].Item[exchange].Split('|');
                            string spot = str[0];
                            string futures = str[1];

                            if (securitySettings.SecurityClass == TypeSecurityClass.Spot)
                            {
                                //securitySpot[token].Item[exchange] = securitySettings.SecurityName + "{1}|" + futures;
                                _securitySpot[token].Item[exchange] = securitySettings.SecurityName + "|" + futures;
                            }
                            else
                            {
                                //_securitySpot[token].Item[exchange] = spot + "|" + securitySettings.SecurityName + "{1}";
                                _securitySpot[token].Item[exchange] = spot + "|" + securitySettings.SecurityName;
                            }
                        }
                    }
                }

                AddDataToSecurity();

                CopySecurityFunding();
            }
        }

        private SecuritySettings GetSecuritySettings(ServerType serverType, ActivatedSecurity activatedSecurity)
        {
            SecuritySettings settings = new SecuritySettings();

            settings.SecurityName = activatedSecurity.SecurityName;            

            if (serverType == ServerType.Bybit)
            {              
                settings.SecurityClass = activatedSecurity.SecurityClass == "USDT" ? settings.SecurityClass = TypeSecurityClass.Spot : TypeSecurityClass.Futures;
                settings.SubString = settings.SecurityClass == TypeSecurityClass.Spot ? "USDT" : "USDT.P";
                return settings;
            }

            if (serverType == ServerType.OKX)
            {
                settings.SecurityClass = activatedSecurity.SecurityClass == "SPOT_USDT" ? settings.SecurityClass = TypeSecurityClass.Spot : TypeSecurityClass.Futures;
                settings.SubString = settings.SecurityClass == TypeSecurityClass.Spot ? "-USDT" : "-USDT-SWAP";
                return settings;                
            }

            if (serverType == ServerType.BitGetFutures)
            {
                settings.SecurityClass = TypeSecurityClass.Futures;
                settings.SubString = "USDT";
                return settings;
            }

            if (serverType == ServerType.BitGetSpot)
            {
                settings.SecurityClass = TypeSecurityClass.Spot;
                settings.SubString = "USDT";
                return settings;
            }

            if (serverType == ServerType.BinanceFutures)
            {
                settings.SecurityClass = TypeSecurityClass.Futures;
                settings.SubString = "USDT";
                return settings;
            }

            if (serverType == ServerType.Binance)
            {
                settings.SecurityClass = TypeSecurityClass.Spot;
                settings.SubString = "USDT";
                return settings;
            }

            if (serverType == ServerType.HTXSwap)
            {
                settings.SecurityClass = TypeSecurityClass.Futures;
                settings.SubString = "-USDT";
                return settings;
            }

            if (serverType == ServerType.HTXSpot)
            {
                settings.SecurityClass = TypeSecurityClass.Spot;
                settings.SubString = "usdt";
                return settings;
            }

            if (serverType == ServerType.BingXFutures)
            {
                settings.SecurityClass = TypeSecurityClass.Futures;
                settings.SubString = "-USDT";
                return settings;
            }

            if (serverType == ServerType.BingXSpot)
            {
                settings.SecurityClass = TypeSecurityClass.Spot;
                settings.SubString = "-USDT";
                return settings;
            }

            if (serverType == ServerType.KuCoinFutures)
            {
                settings.SecurityClass = TypeSecurityClass.Futures;
                settings.SubString = "USDTM";
                return settings;
            }

            if (serverType == ServerType.KuCoinSpot)
            {
                settings.SecurityClass = TypeSecurityClass.Spot;
                settings.SubString = "-USDT";
                return settings;
            }

            if (serverType == ServerType.GateIoFutures)
            {
                settings.SecurityClass = TypeSecurityClass.Futures;
                settings.SubString = "_USDT";
                return settings;
            }

            if (serverType == ServerType.GateIoSpot)
            {
                settings.SecurityClass = TypeSecurityClass.Spot;
                settings.SubString = "_USDT";
                return settings;
            }

            return settings;
        }

        private string GetToken(SecuritySettings securitySettings)
        {
            try
            {
                int usdIndex = securitySettings.SecurityName.IndexOf(securitySettings.SubString);

                if (usdIndex > 0)
                {
                    return securitySettings.SecurityName.Substring(0, usdIndex).ToUpper();
                }

                return null;
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                return null;
            }
        }

        private void AddSecurityToScreener()
        {
            try
            {
                lock (_lockSpot)
                {
                    for (int j = 0; j < TabsScreener.Count; j++)
                    {                
                        for (int indx = 0; indx < TabsScreener[j].SecuritiesNames.Count; indx++)
                        {
                            SecuritySettings securitySettings = GetSecuritySettings(TabsScreener[j].ServerType, TabsScreener[j].SecuritiesNames[indx]);

                            string token = GetToken(securitySettings);

                            if (token == null)
                            {
                                continue;
                            }

                            if (!_securitySpot.ContainsKey(token))
                            {
                                continue;
                            }

                            if (!_securitySpot[token].OnOff)
                            {
                                continue;
                            }

                            string exchange = TabsScreener[j].ServerType.ToString();

                            if (!_securitySpot[token].Item.ContainsKey(exchange))
                            {
                                continue;
                            }

                            if (_securitySpot[token].Item[exchange].Contains('|'))
                            {
                                string[] str = _securitySpot[token].Item[exchange].Split('|');

                                string spot = RemoveSecuritySymbol(str[0]);
                                string futures = RemoveSecuritySymbol(str[1]);

                                if (TabsScreener[j].SecuritiesNames[indx].SecurityName == spot ||
                                    TabsScreener[j].SecuritiesNames[indx].SecurityName == futures)
                                {
                                    TabsScreener[j].SecuritiesNames[indx].IsOn = true;
                                }
                            }
                        }
                    
                        TabsScreener[j].TimeFrame = TimeFrame.Min1;
                        TabsScreener[j].NeedToReloadTabs = true;
                    }
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.StackTrace, Logging.LogMessageType.Error);
            }
        }

        private DataGridView AddPanelTable()
        {
            DataGridView tableData =
                DataGridFactory.GetDataGridView(DataGridViewSelectionMode.FullRowSelect,
                DataGridViewAutoSizeRowsMode.AllCells);

            tableData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            tableData.Dock = DockStyle.Fill; 
            tableData.RowHeadersVisible = false;
            tableData.ScrollBars = ScrollBars.Both;
            tableData.GridColor = Color.FromArgb(64, 64, 64);
            tableData.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            tableData.ColumnHeadersDefaultCellStyle.Font = new Font(tableData.Font, FontStyle.Bold | FontStyle.Italic);
            tableData.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            tableData.ColumnHeadersDefaultCellStyle.BackColor = Color.Black;
            tableData.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            tableData.ColumnHeadersDefaultCellStyle.SelectionBackColor = tableData.ColumnHeadersDefaultCellStyle.BackColor;
            tableData.ColumnHeadersHeight = 50;

            tableData.Columns.Add(new DataGridViewCheckBoxColumn());
            tableData.Columns.Add(new DataGridViewTextBoxColumn());
            tableData.Columns[^1].ReadOnly = true;

            foreach (DataGridViewColumn column in tableData.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            tableData.Columns[1].HeaderText = "Токен/Биржи";

            tableData.CellValueChanged += TableData_CellValueChanged;
            tableData.CurrentCellDirtyStateChanged += TableData_CurrentCellDirtyStateChanged;

            return tableData;
        }

        private void TableData_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0 && e.RowIndex >= 0)
            {
                string token = _panelTable.Rows[e.RowIndex].Cells[1].Value?.ToString();

                if (token == null)
                {
                    return;
                }

                bool onOff = (bool)_panelTable.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

                lock (_lockSpot)
                {
                    _securitySpot[token] = (_securitySpot[token].Item, onOff);

                    CopySecurityFunding();
                }
            }
        }

        private void TableData_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            DataGridView dataGridView = sender as DataGridView;

            if (dataGridView.IsCurrentCellDirty)
                dataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void AddDataToSecurity()
        {
            try
            {
                ClearTableSecurity();

                lock (_lockSpot)
                {
                    foreach (var token in _securitySpot)
                    {
                        DataGridViewRow row = new DataGridViewRow();
                        _panelTable.Rows.Add(row);
                        _panelTable.Rows[^1].Cells[0].Value = token.Value.OnOff;
                        _panelTable.Rows[^1].Cells[1].Value = token.Key;

                        foreach (var exchange in token.Value.Item)
                        {
                            int count = 0;

                            for (int i = 2; i < _panelTable.Columns.Count; i++)
                            {                                
                                if (exchange.Key == ServerType.Bybit.ToString() || exchange.Key == ServerType.OKX.ToString())
                                {
                                    if (_panelTable.Columns[i].HeaderText.ToString().Contains(exchange.Key))
                                    {
                                        string spot = exchange.Value.Split('|')[0];
                                        string fut = exchange.Value.Split('|')[1];

                                        if (_panelTable.Columns[i].HeaderText.ToString().Contains("spot"))
                                        {
                                            if (spot != "")
                                            {
                                                count++;
                                                _panelTable.Rows[^1].Cells[i].Value = spot;
                                            }
                                        }

                                        if (_panelTable.Columns[i].HeaderText.ToString().Contains("futures"))
                                        {
                                            if (fut != "")
                                            {
                                                count++;
                                                _panelTable.Rows[^1].Cells[i].Value = fut;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (_panelTable.Columns[i].HeaderText.ToString() == exchange.Key)
                                    {
                                        count++;
                                        _panelTable.Rows[^1].Cells[i].Value = exchange.Value.Replace("|", "");
                                        break;
                                    }
                                }
                            }

                            if (count == 0)
                            {
                                if (exchange.Key == ServerType.Bybit.ToString() || exchange.Key == ServerType.OKX.ToString())
                                {
                                    DataGridViewTextBoxColumn newColumn = new DataGridViewTextBoxColumn();
                                    newColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                                    newColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
                                    newColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                                    int insertIndex = _panelTable.Columns.Count;
                                    _panelTable.Columns.Insert(insertIndex, newColumn);

                                    _panelTable.Columns[^1].HeaderText = exchange.Key + "(spot)";
                                    _panelTable.Rows[^1].Cells[^1].ReadOnly = false;
                                    _panelTable.Rows[^1].Cells[^1].Value = exchange.Value.Split("|")[0];

                                    newColumn = new DataGridViewTextBoxColumn();
                                    newColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                                    newColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
                                    newColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                                    insertIndex = _panelTable.Columns.Count;
                                    _panelTable.Columns.Insert(insertIndex, newColumn);

                                    _panelTable.Columns[^1].HeaderText = exchange.Key + "(futures)";
                                    _panelTable.Rows[^1].Cells[^1].ReadOnly = false;
                                    _panelTable.Rows[^1].Cells[^1].Value = exchange.Value.Split("|")[1];
                                }
                                else
                                {
                                    DataGridViewTextBoxColumn newColumn = new DataGridViewTextBoxColumn();
                                    newColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                                    newColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
                                    newColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                                    int insertIndex = _panelTable.Columns.Count;
                                    _panelTable.Columns.Insert(insertIndex, newColumn);

                                    _panelTable.Columns[^1].HeaderText = exchange.Key;
                                    _panelTable.Rows[^1].Cells[^1].ReadOnly = false;
                                    _panelTable.Rows[^1].Cells[^1].Value = exchange.Value.Replace("|", "");
                                }
                            }
                        }
                    }

                    CopySecurityFunding();
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
            }
        }

        private void ClearTableSecurity()
        {
            for (int i = _panelTable.RowCount - 1; i >= 0; i--)
            {
                _panelTable.Rows.RemoveAt(i);
            }

            for (int i = 2; i < _panelTable.Columns.Count; i++)
            {
                _panelTable.Columns.RemoveAt(i);
                i--;
            }
        }

        private void SaveSettingsSecurity()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "Файлы JSON (*.json)|*.json|Все файлы (*.*)|*.*";
            saveFileDialog.Title = "Сохранить файл";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    lock (_lockSpot)
                    {
                        string json = JsonConvert.SerializeObject(_securitySpot, Formatting.Indented);

                        string fileName = saveFileDialog.FileName;

                        File.WriteAllText(fileName, json);
                    }
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                    Thread.Sleep(5000);
                }
            }
        }

        private async Task LoadSettingsSecurity()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "Все файлы (*.*)|*.*|Файлы JSON (*.json)|*.json";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    lock (_lockSpot)
                    {
                        string json = File.ReadAllText(openFileDialog.FileName);
                        _securitySpot = JsonConvert.DeserializeObject<Dictionary<string, (Dictionary<string, string>, bool)>>(json);
                        CopySecurityFunding();
                    }

                    AddDataToSecurity();
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                    Thread.Sleep(5000);
                }
            }
        }

        private void CopySecurityFunding()
        {
            lock (_lockFunding)
            {
                _securityFunding = new Dictionary<string, (Dictionary<string, string> Item, bool OnOff)>(_securitySpot);
            }
        }

        #endregion

        #region Comission

        private WindowsFormsHost _hostComission;
        private DataGridView _gridComission;
        private Dictionary<string, Comission> _comission = new Dictionary<string, Comission>();

        private void CreateTabComissionSettings()
        {
            _hostComission = new WindowsFormsHost();

            DataGridView dataGridView =
                DataGridFactory.GetDataGridView(DataGridViewSelectionMode.CellSelect,
                DataGridViewAutoSizeRowsMode.AllCells);
            dataGridView.Dock = DockStyle.Fill;
            dataGridView.ScrollBars = ScrollBars.Both;
            dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView.GridColor = Color.Gray;
            dataGridView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

            dataGridView.ColumnCount = 5;
            dataGridView.RowCount = 14;

            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                //column.DefaultCellStyle.SelectionBackColor = dataGridView.DefaultCellStyle.BackColor;
                //column.DefaultCellStyle.SelectionForeColor = dataGridView.DefaultCellStyle.ForeColor;
                column.ReadOnly = false;
            }

            dataGridView.ColumnHeadersDefaultCellStyle.Font = new Font(dataGridView.Font, FontStyle.Bold | FontStyle.Italic);
            dataGridView.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dataGridView.Columns[0].HeaderText = "Биржи";
            dataGridView.Columns[1].HeaderText = "Спот тейкер";
            dataGridView.Columns[2].HeaderText = "Спот мейкер";
            dataGridView.Columns[3].HeaderText = "Фьючерсы тейкер";
            dataGridView.Columns[4].HeaderText = "Фьючерсы мейкер";

            dataGridView[0, 0].Value = ServerType.Binance;
            dataGridView[0, 1].Value = ServerType.BinanceFutures;
            dataGridView[0, 2].Value = ServerType.BingXSpot;
            dataGridView[0, 3].Value = ServerType.BingXFutures;
            dataGridView[0, 4].Value = ServerType.BitGetFutures;
            dataGridView[0, 5].Value = ServerType.BitGetSpot;
            dataGridView[0, 6].Value = ServerType.Bybit;
            dataGridView[0, 7].Value = ServerType.GateIoFutures;
            dataGridView[0, 8].Value = ServerType.GateIoSpot;
            dataGridView[0, 9].Value = ServerType.HTXSpot;
            dataGridView[0, 10].Value = ServerType.HTXSwap;
            dataGridView[0, 11].Value = ServerType.KuCoinFutures;
            dataGridView[0, 12].Value = ServerType.KuCoinSpot;
            dataGridView[0, 13].Value = ServerType.OKX;

           
            for (int j = 0; j < dataGridView.Rows.Count; j++)
            {
                for (int i = 1; i < dataGridView.ColumnCount; i++)
                {
                    dataGridView.Rows[j].Cells[i].Value = 0;
                }

                string exchange = dataGridView.Rows[j].Cells[0].Value.ToString();

                if (!_comission.ContainsKey(exchange))
                {
                    _comission[exchange] = new Comission();
                }

                Comission comiss = new Comission();
                _comission[exchange] = comiss;
            }

            _hostComission.Child = dataGridView;
            _gridComission = dataGridView;

            dataGridView.CellValueChanged += Comission_DataGridView_CellValueChanged;
            dataGridView.DataError += Funding_DataGridView_DataError;
        }

        private void Comission_DataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            string exchange = _gridComission.Rows[e.RowIndex].Cells[0].Value.ToString();

            decimal spotTaker = 0;
            if (!decimal.TryParse(_gridComission.Rows[e.RowIndex].Cells[1].Value?.ToString(), out spotTaker))
            {
                _gridComission.Rows[e.RowIndex].Cells[1].Value = 0;
                SendNewLogMessage("Нужно ввести число", Logging.LogMessageType.Error);
                return;
            }

            decimal spotMaker = 0;
            if (!decimal.TryParse(_gridComission.Rows[e.RowIndex].Cells[2].Value?.ToString(), out spotMaker))
            {
                _gridComission.Rows[e.RowIndex].Cells[1].Value = 0;
                SendNewLogMessage("Нужно ввести число", Logging.LogMessageType.Error);
                return;
            }

            decimal futTaker = 0;
            if (!decimal.TryParse(_gridComission.Rows[e.RowIndex].Cells[3].Value?.ToString(), out futTaker))
            {
                _gridComission.Rows[e.RowIndex].Cells[2].Value = 0;
                SendNewLogMessage("Нужно ввести число", Logging.LogMessageType.Error);
                return;
            }

            decimal futMaker = 0;
            if (!decimal.TryParse(_gridComission.Rows[e.RowIndex].Cells[4].Value?.ToString(), out futMaker))
            {
                _gridComission.Rows[e.RowIndex].Cells[3].Value = 0;
                SendNewLogMessage("Нужно ввести число", Logging.LogMessageType.Error);
                return;
            }

            Comission comiss = new Comission();

            comiss.SpotTaker = spotTaker;
            comiss.SpotMaker = spotMaker;
            comiss.FuturesTaker = futTaker;
            comiss.FuturesMaker = futMaker;

            _comission[exchange] = comiss;

            SaveComission();
        }

        private void SaveComission()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_comission, Formatting.Indented);

                string fileName = @"Engine\" + NameStrategyUniq + @"Comission.json";

                File.WriteAllText(fileName, json);
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                Thread.Sleep(5000);
            }
        }

        private void LoadComission()
        {            
            try
            {
                string fileName = @"Engine\" + NameStrategyUniq + @"Comission.json";

                if (!File.Exists(fileName))
                {
                    return;
                }
                                
                string json = File.ReadAllText(fileName);
                _comission = JsonConvert.DeserializeObject<Dictionary<string, Comission>>(json);

                int count = 0;

                foreach (var exchange in _comission)
                {
                    _gridComission.Rows[count].Cells[1].Value = exchange.Value.SpotTaker;
                    _gridComission.Rows[count].Cells[2].Value = exchange.Value.SpotMaker;
                    _gridComission.Rows[count].Cells[3].Value = exchange.Value.FuturesTaker;
                    _gridComission.Rows[count].Cells[4].Value = exchange.Value.FuturesMaker;

                    count++;
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                Thread.Sleep(5000);
            }           
        }

        #endregion

        #region Classes

        private void MergeCells(DataGridView dataGridView, int rowIndex, int columnIndex,
                            int rowSpan, int colSpan, bool readOnly, string text = null)
        {
            if (dataGridView == null) throw new ArgumentNullException(nameof(dataGridView));
            if (rowIndex < 0 || rowIndex >= dataGridView.RowCount) return;
            if (columnIndex < 0 || columnIndex >= dataGridView.ColumnCount) return;
            if (rowSpan < 1) rowSpan = 1;
            if (colSpan < 1) colSpan = 1;

            // Устанавливаем текст в основную ячейку
            if (text != null)
            {
                dataGridView[columnIndex, rowIndex].Value = text;
            }

            dataGridView[columnIndex, rowIndex].ReadOnly = readOnly;

            // Очищаем объединяемые ячейки
            for (int r = rowIndex; r < rowIndex + rowSpan; r++)
            {
                for (int c = columnIndex; c < columnIndex + colSpan; c++)
                {
                    if (r == rowIndex && c == columnIndex) continue;

                    if (r < dataGridView.RowCount && c < dataGridView.ColumnCount)
                    {
                        dataGridView[c, r].Value = string.Empty;
                        dataGridView[c, r].ReadOnly = true;
                    }
                }
            }

            // Обработчик для отрисовки объединенных ячеек
            dataGridView.CellPainting += (sender, e) =>
            {
                if (e.RowIndex >= rowIndex && e.RowIndex < rowIndex + rowSpan &&
                    e.ColumnIndex >= columnIndex && e.ColumnIndex < columnIndex + colSpan)
                {
                    // Пропускаем отрисовку для внутренних ячеек
                    if (e.RowIndex != rowIndex || e.ColumnIndex != columnIndex)
                    {
                        e.Handled = true;
                        return;
                    }

                    // Вычисляем область объединения с точными границами
                    Rectangle rect = dataGridView.GetCellDisplayRectangle(columnIndex, rowIndex, true);
                    rect.X -= 1;
                    rect.Y -= 1;

                    for (int i = 1; i < colSpan; i++)
                    {
                        rect.Width += dataGridView.GetCellDisplayRectangle(columnIndex + i, rowIndex, true).Width;
                    }

                    for (int i = 1; i < rowSpan; i++)
                    {
                        rect.Height += dataGridView.GetCellDisplayRectangle(columnIndex, rowIndex + i, true).Height;
                    }

                    // Отрисовываем фон
                    using (Brush brush = new SolidBrush(e.CellStyle.BackColor))
                    {
                        e.Graphics.FillRectangle(brush, rect);
                    }

                    // Отрисовываем текст
                    if (e.Value != null)
                    {
                        TextFormatFlags flags = TextFormatFlags.HorizontalCenter |
                                             TextFormatFlags.VerticalCenter |
                                             TextFormatFlags.WordBreak;

                        TextRenderer.DrawText(e.Graphics, e.Value.ToString(),
                                            e.CellStyle.Font, rect,
                                            e.CellStyle.ForeColor, flags);
                    }

                    // Отрисовываем границы точно по краям
                    using (Pen pen = new Pen(dataGridView.GridColor))
                    {
                        // Левая граница
                        e.Graphics.DrawLine(pen, rect.Left, rect.Top, rect.Left, rect.Bottom);
                        // Верхняя граница
                        e.Graphics.DrawLine(pen, rect.Left, rect.Top, rect.Right, rect.Top);
                        // Правая граница
                        e.Graphics.DrawLine(pen, rect.Right, rect.Top, rect.Right, rect.Bottom);
                        // Нижняя граница
                        e.Graphics.DrawLine(pen, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
                    }

                    e.Handled = true;
                }
            };
        }

        private class Comission
        {
            public decimal SpotTaker = 0;
            public decimal SpotMaker = 0;
            public decimal FuturesTaker = 0;
            public decimal FuturesMaker = 0;
        }
        
        private class SpotData
        {
            public decimal PriceSpot;
            public decimal BestBid;
            public decimal BestAsk;
            public bool ChoiseLong;
            public bool ChoiseShort;
            public decimal Volume24h;
            public decimal PriceHedge;
            public decimal Rate;
            public string ExpirationTimeFunding;
            public decimal Volume24hFutures;
            public bool ChoiseFutures;
            public bool OnOff;
            public int FundingPeriod;
            public int LotSpot = 1;
            public int LotFutures = 1;
            public BotTabSimple Tab;
            public BotTabSimple TabHedge;
        }

        private class SpotGeneralTable
        {
            public string Token;
            public string ExchangeLong;
            public decimal PriceLong;
            public string ExchangeShort;
            public decimal PriceShort;
            public decimal SpreadSpot;
            public decimal SpreadSpotPercent;
            public string ExchangeHedge;
            public decimal HedgePrice;
            public decimal SpreadHedge;
            public decimal SpreadHedgePercent;
            public string FundigTime;
            public decimal FundingRate;
            public int FundingPeriod;
            public decimal VolumeSpot;
            public int QuantitySpot;
            public decimal VolumeFutures;
            public int QuantityFutures;
            public bool OnOffExchange;
            public bool OnOffToken;
            public BotTabSimple TabLong;
            public BotTabSimple TabShort;
            public BotTabSimple TabHedge;
        }

        private class FundingData
        {
            public decimal Price;
            public decimal BestBid;
            public decimal BestAsk;
            public bool ChoiseLong;
            public bool ChoiseShort;
            public decimal Volume24h;
            public decimal Rate;
            public DateTime ExpirationTimeFunding;
            public bool OnOff;
            public int FundingPeriod;
            public decimal MinFundingRate;
            public decimal MaxFundingRate;
            public int LotFutures = 1;
            public decimal OpenInterest;
            public BotTabSimple Tab;
        }

        private class FundingGeneralTable
        {
            public string Token;
            public int CountExchange;
            public string Periodicity;
            public string ExchangeLong;
            public string ExchangeShort;
            public string MinMaxRate;
            public decimal SpreadFunding;
            public string FundingTime;
            public decimal SpreadPairs;
            public decimal Volume;
            public decimal OpenInterest;
            public bool OnOffToken;
            public BotTabSimple TabLong;
            public BotTabSimple TabShort;
        }

        private enum SortDirection
        {
            None,
            Ascending,
            Descending
        }

        private class SecuritySettings
        {
            public string SecurityName;
            public TypeSecurityClass SecurityClass;
            public string SubString;
        }

       private enum TypeSecurityClass
        {
            Spot,
            Futures
        }

        private string[] _spotServer = new string[] 
        { 
            ServerType.Binance.ToString(), 
            ServerType.BitGetSpot.ToString(),
            ServerType.BingXSpot.ToString(),
            ServerType.GateIoSpot.ToString(),
            ServerType.HTXSpot.ToString(),
            ServerType.KuCoinSpot.ToString()
        }; 
    }

    #endregion
}