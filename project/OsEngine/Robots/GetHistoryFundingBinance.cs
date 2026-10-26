using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using RestSharp;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;

namespace OsEngine.Robots
{
    [Bot("GetHistoryFundingBinance")]
    public class GetHistoryFundingBinance : BotPanel
    {
        private StrategyParameterString _securities;
        private StrategyParameterButton _buttonSaveExcel;
        private const string _baseUrl = "https://fapi.binance.com";
        private StrategyParameterString _periodFrom;
        private StrategyParameterString _periodTo;
        private int _isLoading;

        public GetHistoryFundingBinance(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);

            _securities = CreateParameter("Security", "");
            _periodFrom = CreateParameter("Period from", "01.01.2026");
            _periodTo = CreateParameter("Period to", "01.01.2027");
            _buttonSaveExcel = CreateParameterButton("Сохранить данные в Excel");
            _buttonSaveExcel.UserClickOnButtonEvent += _buttonSaveExcel_UserClickOnButtonEvent;
        }

        #region Export

        private async void _buttonSaveExcel_UserClickOnButtonEvent()
        {
            if (Interlocked.CompareExchange(ref _isLoading, 1, 0) != 0)
            {
                SendNewLogMessage("Загрузка уже выполняется", LogMessageType.System);
                return;
            }

            Form progressForm = null;
            try
            {
                string[] securities = GetListSecurity();
                DateTime fromDate;
                DateTime toDate;
                if (!DateTime.TryParseExact((_periodFrom.ValueString ?? string.Empty).Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out fromDate)
                    || !DateTime.TryParseExact((_periodTo.ValueString ?? string.Empty).Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out toDate)
                    || fromDate > toDate || toDate == DateTime.MaxValue.Date)
                {
                    throw new ArgumentException("Укажите корректный период в формате dd.MM.yyyy. Начало не должно быть позже окончания.");
                }

                long from = new DateTimeOffset(DateTime.SpecifyKind(fromDate, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
                long to = new DateTimeOffset(DateTime.SpecifyKind(toDate.AddDays(1), DateTimeKind.Utc)).ToUnixTimeMilliseconds() - 1;
                to = Math.Min(to, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                if (from > to)
                {
                    throw new ArgumentException("Начало периода находится в будущем.");
                }

                progressForm = new Form();
                progressForm.Text = "Загрузка данных";
                progressForm.Size = new System.Drawing.Size(450, 100);
                progressForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                progressForm.StartPosition = FormStartPosition.CenterScreen;
                progressForm.ControlBox = false;
                Label label = new Label();
                label.Location = new System.Drawing.Point(20, 20);
                label.Size = new System.Drawing.Size(400, 40);
                label.Text = "Загрузка...";
                progressForm.Controls.Add(label);
                progressForm.Show();

                IProgress<string> progress = new Progress<string>(message =>
                {
                    if (!label.IsDisposed && !progressForm.IsDisposed)
                    {
                        label.Text = message;
                    }
                });
                CultureInfo culture = CultureInfo.CurrentCulture;
                await Task.Run(() => SaveExcelData(securities, from, to, culture, progress));
            }
            catch (Exception error)
            {
                SendNewLogMessage(error.Message, LogMessageType.Error);
            }
            finally
            {
                try
                {
                    if (progressForm != null && !progressForm.IsDisposed)
                    {
                        progressForm.Close();
                        progressForm.Dispose();
                    }
                }
                catch (Exception error)
                {
                    SendNewLogMessage("Не удалось закрыть окно загрузки. " + error.Message, LogMessageType.Error);
                }
                finally
                {
                    Interlocked.Exchange(ref _isLoading, 0);
                }
            }
        }

        private void SaveExcelData(string[] securities, long from, long to, CultureInfo culture, IProgress<string> progress)
        {
            try
            {
                Directory.CreateDirectory("Engine");
            }
            catch (UnauthorizedAccessException)
            {
                throw new InvalidOperationException("Нет прав для создания папки Engine.");
            }
            catch (IOException)
            {
                throw new InvalidOperationException("Не удалось создать папку Engine. Проверьте доступ и свободное место на диске.");
            }
            int saved = 0;
            int failed = 0;
            for (int i = 0; i < securities.Length; i++)
            {
                string security = securities[i];
                try
                {
                    List<HistoryFunding> history = GetDataFunding(security, from, to, progress);

                    if (history.Count == 0)
                    {
                        SendNewLogMessage(security + ": за выбранный период данных нет; файл не изменён", LogMessageType.System);
                        continue;
                    }

                    string path = Path.Combine("Engine", security + "_history_funding.csv");
                    string temporaryPath = path + ".tmp";
                    try
                    {
                        using (StreamWriter writer = new StreamWriter(temporaryPath, false, new UTF8Encoding(true)))
                        {
                            writer.WriteLine("Date;Time;Funding;MarkPrice");

                            foreach (HistoryFunding row in history)
                            {
                                writer.WriteLine(DateTimeOffset.FromUnixTimeMilliseconds(row.fundingTime).UtcDateTime
                                    .ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ";"
                                    + DateTimeOffset.FromUnixTimeMilliseconds(row.fundingTime).UtcDateTime
                                    .ToString("HHmmss", CultureInfo.InvariantCulture) + ";"
                                    + row.fundingRate.ToString(culture) + ";"
                                    + row.markPrice.ToString(culture));
                            }
                        }
                        File.Move(temporaryPath, path, true);
                    }
                    finally
                    {
                        if (File.Exists(temporaryPath))
                        {
                            try
                            {
                                File.Delete(temporaryPath);
                            }
                            catch (Exception error)
                            {
                                SendNewLogMessage(security + ": не удалось удалить временный файл. " + error.Message, LogMessageType.Error);
                            }
                        }
                    }
                    saved++;
                    SendNewLogMessage(security + ": сохранено " + history.Count + " записей в " + Path.GetFullPath(path), LogMessageType.System);
                }
                catch (Exception error)
                {
                    failed++;
                    string message = error.Message;
                    if (error is UnauthorizedAccessException)
                    {
                        message = "Не удалось записать CSV. Закройте файл в Excel и проверьте права записи и атрибут «только чтение».";
                    }
                    else if (error is IOException && !(error is InvalidDataException))
                    {
                        message = "Не удалось сохранить CSV. Закройте файл в Excel и проверьте доступ и свободное место в папке Engine.";
                    }
                    SendNewLogMessage(security + ": " + message, LogMessageType.Error);
                }
                finally
                {
                    progress.Report("Обработано инструментов " + (i + 1) + "/" + securities.Length);
                }
            }
            SendNewLogMessage("Загрузка завершена. Сохранено файлов " + saved + "/" + securities.Length + "; ошибок " + failed, LogMessageType.System);
        }

        private string[] GetListSecurity()
        {
            List<string> securities = new List<string>();
            string[] entries = (_securities.ValueString ?? string.Empty).Split(',');
            foreach (string entry in entries)
            {
                string security = entry.Trim().ToUpperInvariant();
                if (security.EndsWith(".P", StringComparison.Ordinal))
                {
                    security = security.Substring(0, security.Length - 2);
                }
                if (security.Length == 0)
                {
                    continue;
                }
                foreach (char character in security)
                {
                    if (!(character >= 'A' && character <= 'Z') && !(character >= '0' && character <= '9') && character != '_')
                    {
                        throw new ArgumentException("Некорректный инструмент. Используйте символ Binance, например XAUUSDT или XAUUSDT.P.");
                    }
                }
                if (!securities.Contains(security))
                {
                    securities.Add(security);
                }
            }
            if (securities.Count == 0)
            {
                throw new ArgumentException("Укажите хотя бы один инструмент в параметре Security.");
            }
            return securities.ToArray();
        }

        #endregion

        #region Binance history

        private List<HistoryFunding> GetDataFunding(string security, long from, long to, IProgress<string> progress)
        {
            List<HistoryFunding> history = new List<HistoryFunding>();
            while (from <= to)
            {
                // The endpoint shares a limit of 500 requests per five minutes with fundingInfo.
                Thread.Sleep(650);
                List<HistoryFunding> page = GetHistoryFunding(security, from, to);
                if (page.Count == 0)
                {
                    break;
                }
                long lastTime = from - 1;
                foreach (HistoryFunding row in page)
                {
                    if (row == null || row.symbol != security || row.fundingTime < from || row.fundingTime > to
                        || row.fundingTime < lastTime)
                    {
                        throw new InvalidDataException("Binance вернул некорректную страницу истории фандинга " + security);
                    }
                    history.Add(row);
                    lastTime = row.fundingTime;
                }
                from = lastTime + 1;
                progress.Report(security + ": загружено записей " + history.Count);
            }
            return history;
        }

        private List<HistoryFunding> GetHistoryFunding(string security, long from, long to)
        {
            string request = "/fapi/v1/fundingRate?symbol=" + Uri.EscapeDataString(security)
                + "&startTime=" + from.ToString(CultureInfo.InvariantCulture)
                + "&endTime=" + to.ToString(CultureInfo.InvariantCulture) + "&limit=1000";
            RestClient client = new RestClient(_baseUrl);
            client.Timeout = 30000;
            IRestResponse response = client.Execute(new RestRequest(request, Method.GET));
            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                throw new InvalidOperationException("Binance не ответил за 30 секунд. Повторите загрузку позже.");
            }
            if (response.ResponseStatus != ResponseStatus.Completed || (int)response.StatusCode == 0)
            {
                throw new InvalidOperationException("Не удалось подключиться к Binance. Проверьте интернет и доступ к fapi.binance.com.");
            }
            int status = (int)response.StatusCode;
            if (status != 200)
            {
                string message;
                if (status == 429 || status == 418)
                {
                    message = "Binance ограничил частоту запросов. Повторите загрузку позже.";
                }
                else if (status == 403 || status == 451)
                {
                    message = "Binance ограничил доступ к API для вашего подключения.";
                }
                else if (status >= 500)
                {
                    message = "Сервис Binance временно недоступен. Повторите загрузку позже.";
                }
                else
                {
                    message = "Ошибка Binance HTTP " + status + ".";
                    try
                    {
                        JObject error = JObject.Parse(response.Content ?? string.Empty);
                        string details = error["msg"]?.Type == JTokenType.String ? error.Value<string>("msg") : null;
                        if (error["code"]?.ToString() == "-1121")
                        {
                            message = "Binance не поддерживает указанный инструмент на рынке USD-M.";
                        }
                        else if (!string.IsNullOrWhiteSpace(details))
                        {
                            details = details.Length > 240 ? details.Substring(0, 240) + "..." : details;
                            message += " " + details.Replace('\r', ' ').Replace('\n', ' ');
                        }
                    }
                    catch (JsonException)
                    {
                        // An HTTP error may contain HTML instead of the Binance JSON error object.
                    }
                }
                throw new InvalidOperationException(message);
            }
            List<HistoryFunding> history;
            try
            {
                history = JsonConvert.DeserializeObject<List<HistoryFunding>>(response.Content);
            }
            catch (JsonException)
            {
                throw new InvalidDataException("Binance вернул некорректный формат истории фандинга. Повторите загрузку позже.");
            }
            if (history == null)
            {
                throw new InvalidDataException("Binance вернул пустой ответ вместо массива истории фандинга.");
            }
            return history;
        }

        private class HistoryFunding
        {
            [JsonProperty(Required = Required.Always)]
            public string symbol { get; set; }
            [JsonProperty(Required = Required.Always)]
            public decimal fundingRate { get; set; }
            [JsonProperty(Required = Required.Always)]
            public long fundingTime { get; set; }
            public decimal markPrice { get; set; }
            public string rateType { get; set; }
        }

        #endregion
    }
}
