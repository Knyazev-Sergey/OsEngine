using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using RestSharp;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using TL;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace OsEngine.Robots
{
    [Bot("GetHistoryFundingKucoin")]
    public class GetHistoryFundingKucoin : BotPanel
    {
        private BotTabSimple _tab;
        private StrategyParameterString _security;
        private StrategyParameterButton _button;
        private StrategyParameterButton _buttonSaveExcel;
        private string _baseUrl = "https://api-futures.kucoin.com";
        private List<HistoryFunding> _historyFunding;
        private CurrentFunding _currentFunding;
        

        public GetHistoryFundingKucoin(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _security = CreateParameter("Security", "");
            _button = CreateParameterButton("Получить данные");
            _buttonSaveExcel = CreateParameterButton("Сохранить данные в Excel");

            _button.UserClickOnButtonEvent += _button_UserClickOnButtonEvent;
            _buttonSaveExcel.UserClickOnButtonEvent += _buttonSaveExcel_UserClickOnButtonEvent;
        }
               
        private async void _buttonSaveExcel_UserClickOnButtonEvent()
        {
            try
            {
                var progressForm = new Form();

                progressForm.Text = "Загрузка данных - 0/0";
                progressForm.Size = new System.Drawing.Size(300, 100);
                progressForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                progressForm.StartPosition = FormStartPosition.CenterScreen;

                var label = new Label();
                label.Location = new System.Drawing.Point(20, 20);
                label.Size = new System.Drawing.Size(250, 20);
                label.Text = "Загрузка...";

                progressForm.Controls.Add(label);
                progressForm.Show();

                Thread threadTotalTable = new Thread(() => SaveExcelData(progressForm)) { IsBackground = true };
                threadTotalTable.Start();
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private void SaveExcelData(Form form)
        {
            try
            {
                List<FundingData> fundingDatas = new();

                List<GetSecurity> listSecurity = GetListSecurity();

                for (int i = 0; i < listSecurity.Count; i++)
                {
                    string security = listSecurity[i].symbol;
                    _currentFunding = GetCurrentFunding(security);

                    if (_currentFunding == null)
                    {
                        continue;
                    }

                    long period = (_currentFunding.fundingTime - _currentFunding.timePoint) / 3600000;

                    _historyFunding = GetDataFunding(security);

                    if (_historyFunding == null)
                    {
                        continue;
                    }

                    FundingData data = new FundingData();

                    data.security = security;
                    data.period = period;

                    decimal avr = GetAverageRateToTable(1);

                    data.avrRate1days = avr;
                    data.apr24h1days = avr * 24 / period;
                    data.aprMonth1days = data.apr24h1days * 30;
                    data.aprYear1days = data.apr24h1days * 365;

                    avr = GetAverageRateToTable(7);
                    data.avrRate7days = avr;
                    data.apr24h7days = avr * 24 / period;
                    data.aprMonth7days = data.apr24h7days * 30;
                    data.aprYear7days = data.apr24h7days * 365;

                    avr = GetAverageRateToTable(14);
                    data.avrRate14days = avr;
                    data.apr24h14days = avr * 24 / period;
                    data.aprMonth14days = data.apr24h14days * 30;
                    data.aprYear14days = data.apr24h14days * 365;

                    avr = GetAverageRateToTable(30);
                    data.avrRate30days = avr;
                    data.apr24h30days = avr * 24 / period;
                    data.aprMonth30days = data.apr24h30days * 30;
                    data.aprYear30days = data.apr24h30days * 365;

                    if (data != null)
                    {
                        fundingDatas.Add(data);
                    }

                    form.Invoke((Action)(() =>
                    {
                        form.Text = $"Загрузка данных - {i + 1}/{listSecurity.Count}";
                    }));
                }

                using (var writer = new StreamWriter(@"Engine\" + NameStrategyUniq + @"Data.csv", false, Encoding.UTF8))
                {
                    string str = "Security;Period;Avr per 1 day;APR per day;APR per month;APR per year;" +
                        "Avr per 7 day;APR per day;APR per month;APR per year;" +
                        "Avr per 14 day;APR per day;APR per month;APR per year;" +
                        "Avr per 30 day;APR per day;APR per month;APR per year";

                    writer.WriteLine(str);

                    foreach (var row in fundingDatas)
                    {
                        str = row.security + ";" + row.period + ";" + row.avrRate1days + ";" + row.apr24h1days + ";" + row.aprMonth1days + ";" + row.aprYear1days
                            + ";" + row.avrRate7days + ";" + row.apr24h7days + ";" + row.aprMonth7days + ";" + row.aprYear7days
                            + ";" + row.avrRate14days + ";" + row.apr24h14days + ";" + row.aprMonth14days + ";" + row.aprYear14days
                            + ";" + row.avrRate30days + ";" + row.apr24h30days + ";" + row.aprMonth30days + ";" + row.aprYear30days;

                        writer.WriteLine(str);
                    }
                }

                SendNewLogMessage("Загрузка завершена", LogMessageType.Error);

            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
                form.Invoke((Action)(() => form.Close()));
            }
        }

        private List<GetSecurity> GetListSecurity()
        {
            string requestStr = "/api/v1/contracts/active";

            RestRequest requestRest = new RestRequest(requestStr, Method.GET);
            IRestResponse responseMessage = new RestClient(_baseUrl).Execute(requestRest);

            ResponseMessageRest<List<GetSecurity>> funding = JsonConvert.DeserializeAnonymousType(responseMessage.Content, new ResponseMessageRest<List<GetSecurity>>());

            return funding.data;
        }

        private void _button_UserClickOnButtonEvent()
        {
            _currentFunding = GetCurrentFunding(_security);

            long period = (_currentFunding.fundingTime - _currentFunding.timePoint) / 3600000;

            _historyFunding = GetDataFunding(_security);

            SendNewLogMessage(GetAverageRate(_security, 7), LogMessageType.Error);
            SendNewLogMessage(GetAverageRate(_security, 14), LogMessageType.Error);
            SendNewLogMessage(GetAverageRate(_security, 30), LogMessageType.Error);
        }

        private List<HistoryFunding> GetDataFunding(string security)
        {
            try
            {
                long period = (_currentFunding.fundingTime - _currentFunding.timePoint) / 3600000;

                long to = _currentFunding.timePoint;
                long from = to - (period * 3600000 * 100);

                long allDaysTimeStamp = _currentFunding.fundingTime - 2592000000;

                List<HistoryFunding> historyFunding = new List<HistoryFunding>();

                do
                {
                    List<HistoryFunding> funding = GetHistoryFunding(security, from, to);

                    if (funding == null)
                    {
                        break;
                    }

                    historyFunding.AddRange(funding);

                    to = funding[^1].timepoint - 1000;
                    from = to - (period * 3600000 * 100);
                }
                while (to > allDaysTimeStamp);

                return historyFunding;
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
                return null;
            }
        }

        private string GetAverageRate(string security, int days)
        {
            long period = (_currentFunding.fundingTime - _currentFunding.timePoint) / 3600000;

            int countPeriods = days * 24 / (int)period;

            if (countPeriods > _historyFunding.Count)
            {
                countPeriods = _historyFunding.Count;
            }

            decimal fundingRate = 0;

            for (int i = 0; i < countPeriods; i++)
            {
                fundingRate += _historyFunding[i].fundingRate.ToDecimal();
            }

            decimal averageFunding = Math.Round(fundingRate / countPeriods * 100, 6);
            decimal apr24h = averageFunding * 24 / period;
            decimal aprMonth = apr24h * 30;
            decimal aprYear = apr24h * 365;

            string str = security + $"\nСредняя ставка за один период ({days} дней): {averageFunding}\nAPR 24h: {apr24h}, APR Month: {aprMonth}, APR Year: {aprYear}";

            return str;
        }

        private decimal GetAverageRateToTable (int days)
        {
            try
            {
                long period = (_currentFunding.fundingTime - _currentFunding.timePoint) / 3600000;

                if ((int)period == 0)
                {
                    return 0;
                }

                int countPeriods = days * 24 / (int)period;

                if (countPeriods == 0) return 0;

                if (countPeriods > _historyFunding.Count)
                {
                    countPeriods = _historyFunding.Count;
                }

                decimal fundingRate = 0;

                for (int i = 0; i < countPeriods; i++)
                {
                    fundingRate += _historyFunding[i].fundingRate.ToDecimal();
                }

                return Math.Round(fundingRate / countPeriods * 100, 6);
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), LogMessageType.Error);
                return 0;
            }
        }

        private List<HistoryFunding> GetHistoryFunding(string security, long from, long to)
        {
            string requestStr = "/api/v1/contract/funding-rates?symbol=" + security + "&from=" + from + "&to=" + to;

            RestRequest requestRest = new RestRequest(requestStr, Method.GET);
            IRestResponse responseMessage = new RestClient(_baseUrl).Execute(requestRest);

            ResponseMessageRest<List<HistoryFunding>> funding = JsonConvert.DeserializeAnonymousType(responseMessage.Content, new ResponseMessageRest<List<HistoryFunding>>());

            if (funding == null)
            {
                return null;
            }

            return funding.data;
        }

        private CurrentFunding GetCurrentFunding(string security)
        {
            string requestStr = "/api/v1/funding-rate/" + security + "/current";

            RestRequest requestRest = new RestRequest(requestStr, Method.GET);
            IRestResponse responseMessage = new RestClient(_baseUrl).Execute(requestRest);

            ResponseMessageRest<CurrentFunding> funding = JsonConvert.DeserializeAnonymousType(responseMessage.Content, new ResponseMessageRest<CurrentFunding>());

            if (funding == null)
            {
                return null;
            }

            return funding.data;
        }
    }

    public class ResponseMessageRest<T>
    {
        public string code;
        public string msg;
        public T data;
    }

    public class HistoryFunding
    {
        public string fundingRate;
        public long timepoint;
        public string symbol;
    }

    public class CurrentFunding
    {
        public long timePoint;
        public long fundingTime;
        public string value;
    }
    public class GetSecurity
    {      
        public string symbol;
    }

    public class FundingData
    {
        public string security;
        public decimal avrRate1days;
        public decimal avrRate7days;
        public decimal avrRate14days;
        public decimal avrRate30days;
        public decimal apr24h1days;
        public decimal aprMonth1days;
        public decimal aprYear1days;
        public decimal apr24h7days;
        public decimal aprMonth7days;
        public decimal aprYear7days;
        public decimal apr24h14days;
        public decimal aprMonth14days;
        public decimal aprYear14days;
        public decimal apr24h30days;
        public decimal aprMonth30days;
        public decimal aprYear30days;
        public long period;
    }
}
