using System;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.Logging;
using System.Net.Http.Headers;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Concurrent;
using OsEngine.Market.Servers.Entity;
using OsEngine.Market.CustomConnectors.Coinglass.Entity;

namespace OsEngine.Market.CustomConnectors.Coinglass
{
    public class CoinglassConnector
    {
        private static CoinglassConnector _server;

        private readonly HttpClient _httpClient;
        private static string _apikey;
        private readonly ConcurrentQueue<RequestContent> _requestsQueue = new ConcurrentQueue<RequestContent>();
        //Задаем макс. допустимое кол-во обращений к API в минуту
        private readonly RateGate _rateGateGetData = new RateGate(30, TimeSpan.FromSeconds(60));

        /// <summary>
        /// Статический метод для получения экземпляра класса CoinglassConnector.
        /// Если экземпляр не существует, он создает новый с помощью предоставленного API-ключа.
        /// </summary>
        /// <param name="apiKey">Ключ API для доступа к API Coinglass.</param>
        /// <returns>Экземпляр класса CoinglassConnector.</returns>
        public static CoinglassConnector GetServer(string apiKey)
        {
            if (_server != null) return _server;
            _server = new CoinglassConnector(apiKey);
            _apikey = apiKey;
            return _server;
        }
        
        /// <summary>
        /// Инициализирует CoinglassConnector и запускает отдельный поток для обработки запросов из очереди.
        /// </summary>
        private CoinglassConnector(string apiKey)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("CG-API-KEY", apiKey);

            //поток разбора очереди запросов
            Thread worker = new Thread(PullRequests);
            worker.CurrentCulture = new CultureInfo("ru-RU");
            worker.IsBackground = true;
            worker.Start();

            // Создает каталог для хранения данных, если он не существует
            if (!Directory.Exists("Data\\Coinglass"))
            {
                Directory.CreateDirectory("Data\\Coinglass");
            }
        }
        /// <summary>
        /// Отвечает за непрерывное извлечение и обработку запросов из очереди.
        /// Он проверяет статус главного процесса, обрабатывает пустые или нулевые очереди и снимает запросы с очереди.
        /// В зависимости от программы запуска, указанной в запросе, он вызывает метод GetData или GetHistoricalData.
        /// Если во время обработки запроса возникает исключение, он ждет секунду, прежде чем продолжить работу.
        /// </summary>
        private void PullRequests()
        {
            Thread.Sleep(500);
            while (true)
            {
                try
                {
                    // Проверка, запущен ли главный процесс
                    if(MainWindow.ProccesIsWorked == false)
                    {
                        return;
                    }
                    
                    // Проверка очереди запросов
                    if (_requestsQueue == null)
                    {
                        Thread.Sleep(500);
                        continue;
                    }
                    
                    // Есть ли данные в очереди
                    if (_requestsQueue.Count == 0)
                    {
                        Thread.Sleep(500);
                        continue;
                    }
                                
                    // Извлечение данных из очереди
                    if(!_requestsQueue.TryDequeue(out RequestContent requestContent)) 
                        continue;
                    
                    // Если вызывающая программа OsTrader - вызываем метод GetData
                    if (requestContent.StartProgram == StartProgram.IsOsTrader)
                    {
                        GetData(requestContent);
                        continue;
                    }

                    // Если вызывающая программа OsTester - вызываем GetHistoricalData
                    GetHistoricalData(requestContent);
                }
                catch
                {
                    Thread.Sleep(1000);
                }
            }
        }
        
        public event Action<string, ResponseType, List<LongShortRatio>>? CoinglassUpdateEvent;
        
        /// <summary>
        /// Проверяет наличие сохраненных исторических данных, актуализирует их.
        /// Затем запускает событие для обновления бота необходимой информацией.
        /// </summary>
        /// <param name="request">Содержание запроса</param>
        public void GetHistoricalData(RequestContent request)
        {
            string botName = request.BotName;
            string securityName = request.Symbol.Split('.')[0];
            string exchange = request.Exchange;
            string timeFrame = request.Interval;
            int limit = request.Limit;
            string signal = request.ResponseType.ToString();

            _fileName = $"Data\\Coinglass\\{signal}_{exchange}_{securityName}_{timeFrame}.json";

            List<LongShortRatio> dataList = new List<LongShortRatio>();

            if (!File.Exists(_fileName)) //если файл не существует
            {
                //Получаем исторические данные
                dataList = GetCoinglassHistory(exchange,securityName,signal, timeFrame, limit);

                if (dataList.Count < 1) return; //если данных нет - выходим

                //Генерируем событие обновления данных
                CoinglassUpdateEvent?.Invoke(botName, request.ResponseType, dataList);
                    
                try
                {
                    string data = JsonConvert.SerializeObject(dataList); //Сериализируем полученные данные

                    using StreamWriter writer = new StreamWriter(_fileName, false);
                    writer.Write(data); //сохраняем данные в файл
                }
                catch
                {
                    SendLogMessage($"Error writing file: {_fileName}", LogMessageType.Error);
                }
            }
            else //файл есть на диске
            {
                //Читаем данные из файла
                try
                {
                    using StreamReader reader = new StreamReader(_fileName);
                    string text = reader.ReadToEnd();
                    
                    dataList = JsonConvert.DeserializeObject<List<LongShortRatio>>(text);
                    
                }
                catch
                {
                    //ignore
                }
                    
                //проверяем данные на актуальность
                if(dataList != null && dataList.Count > 1 && (dataList[dataList.Count - 1].Time - DateTime.UtcNow).Days >= 1 
                   && !string.IsNullOrEmpty(_apikey))
                {
                    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    long startTime = (long)(dataList[dataList.Count - 1].Time - epoch).TotalSeconds;
                        
                    List<LongShortRatio> newData = GetCoinglassHistory(exchange,securityName,signal,timeFrame,limit, startTime);
                        
                    if(newData.Count > 0)
                    {
                        for (int i = 0; i < newData.Count; i++)
                        {
                            if(newData[i].Time > dataList[dataList.Count - 1].Time)
                            {
                                dataList.Add(newData[i]);
                            }
                        }
                        
                        try
                        {
                            string data = JsonConvert.SerializeObject(dataList);

                            using StreamWriter writer = new StreamWriter(_fileName, false);
                            writer.Write(data); //сохраняем обновленные данные в файл
                        }
                        catch
                        {
                            //ignore
                        }
                    }
                }
                
                if (dataList != null && dataList.Count > 0)
                {
                    CoinglassUpdateEvent?.Invoke(botName, request.ResponseType, dataList);
                }
            }
        }
        
        private string _fileName = "";
        private string _lastTimeStamp = "";
        /// <summary>
        /// Получает исторические данные для указанной бумаги из API Coinglass.
        /// </summary>
        /// <param name="exchange">Биржа для получения параметра</param>
        /// <param name="symbol">Символ бумаги на бирже</param>
        /// <param name="type">Тип запрашиваемого параметра (LongShortRatio).</param>
        /// <param name="timeFrame">The time frame of the data (e.g., 1d).</param>
        /// <param name="limit">Максимальная длина запроса</param>
        /// <param name="startTime">С какого времени получать данные (опционально, текущее = 0).</param>
        /// <param name="endTime">До какого времени получать данные (опционально, текущее = "").</param>
        /// <returns>
        /// Список объектов класса LongShortRatio, содержащих дату и время (в UTC) и соответствующие значение данных.
        /// Если в процессе извлечения произошла ошибка, метод возвращает пустой список.
        /// </returns>
        private List<LongShortRatio> GetCoinglassHistory(string exchange, string symbol, string type, string timeFrame, int limit, long startTime = 0, string endTime = "")
        {

            // Инициализация пустых списков для хранения данных
            List<LongShortRatio> dataList = new List<LongShortRatio>();
            
            List<LongShortRatio> tmpList = CreateQuery(exchange, symbol, type, timeFrame, limit, startTime, endTime); //запрос к API
            
            dataList.AddRange(tmpList);
            
            while (tmpList.Count == limit) //получаем данные порциями, пока есть что получать
            {
                tmpList = CreateQuery(exchange, symbol, type, timeFrame, limit, startTime, _lastTimeStamp);
                
                if (tmpList.Count > 1)
                {
                    dataList.InsertRange(0, tmpList);
                }
            }

            _lastTimeStamp = "";
            
            return dataList;
        }

        /// <summary>
        /// Метод отправляет запрос к API Coinglass.
        /// </summary>
        /// <param name="exchange"></param>
        /// <param name="symbol"></param>
        /// <param name="type"></param>
        /// <param name="timeFrame"></param>
        /// <param name="limit"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns>Список объектов класса LongShortRatio.</returns>
        private List<LongShortRatio> CreateQuery(string exchange, string symbol, string type, string timeFrame,
            int limit, long startTime, string endTime)
        {
            List<LongShortRatio> tmpList = new List<LongShortRatio>();
            
            // Формируем URL для API запроса
            string url = $"https://open-api-v3.coinglass.com/api/futures/globalLongShortAccountRatio/history?exchange={exchange}&symbol={symbol}&interval={timeFrame}&limit={limit}";
            
            // Если указано время начала
            if(startTime > 1)
            {
                url = $"https://open-api-v3.coinglass.com/api/futures/globalLongShortAccountRatio/history?exchange={exchange}&symbol={symbol}&interval={timeFrame}&limit={limit}&startTime={startTime}";
            }

            if (endTime != "") //Если указано время завершения
            {
                url = $"https://open-api-v3.coinglass.com/api/futures/globalLongShortAccountRatio/history?exchange={exchange}&symbol={symbol}&interval={timeFrame}&limit={limit}&endTime={endTime}";
            }
            try
            {
                // Отправляем GET запрос в API
                HttpResponseMessage response = _httpClient.GetAsync(url).Result;

                // Проверяем успешность запроса
                if (response.IsSuccessStatusCode)
                {
                    // Читаем содержимое ответа
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                
                    // Десериализуем содержимое ответа
                    RestResponse<object>? stateResponse = JsonConvert.DeserializeAnonymousType(responseContent, new RestResponse<object>());
                
                    if(stateResponse != null)
                    {
                        if(stateResponse.code.Equals("0") && stateResponse.msg.Equals("success"))
                        {
                            RestResponse<List<GlobalAccountRatio>>? responseRatio= JsonConvert.DeserializeAnonymousType(responseContent, new RestResponse<List<GlobalAccountRatio>>());
                        
                            if(responseRatio == null) return tmpList;

                            // Преобразуем данные в нужный формат и добавляем их в список
                            for (int i = 0; i < responseRatio.data.Count; i++)
                            {
                                LongShortRatio lsr = new LongShortRatio();

                                lsr.Time = DateTimeOffset.FromUnixTimeSeconds(long.Parse(responseRatio.data[i].time)).UtcDateTime;
                                lsr.LSR = responseRatio.data[i].longShortRatio.ToDecimal();
                                lsr.Long = responseRatio.data[i].longAccount.ToDecimal();
                                lsr.Short = responseRatio.data[i].shortAccount.ToDecimal();

                                tmpList.Add(lsr);
                            }

                            _lastTimeStamp = responseRatio.data[0].time; //запоминаем время начала полученного блока данных
                            
                            return tmpList;
                        }
                    
                        SendLogMessage($"Coinglass error response: Code: {stateResponse.code}, Message: {stateResponse.msg}",LogMessageType.Error);
                        return tmpList;
                    }
                
                    SendLogMessage($"Coinglass: deserialisation error - returned null",LogMessageType.Error);
                    return tmpList;
                }
                
                SendLogMessage($"Http request error > {response.StatusCode}",LogMessageType.Error);
                return tmpList;
            }
            catch(Exception ex) 
            {
                SendLogMessage($"GetCoinGlassHistory error: {ex.Message}",LogMessageType.Error);
            }
            
            return tmpList;
        }

        /// <summary>
        /// Получает последние доступные данные через API для указанной бумаги и запускает событие для обновления бота.
        /// </summary>
        /// <param name="request">Содержимое запроса</param>
        public void GetData(RequestContent request)
        {
            _rateGateGetData.WaitToProceed(); //контроль количества обращений к API
            
            List<LongShortRatio> data = new List<LongShortRatio>();

            string url = "https://open-api-v3.coinglass.com/api/futures/globalLongShortAccountRatio/history?exchange=" 
                         + request.Exchange + "&symbol=" + request.Symbol + "&interval=" + request.Interval +"&limit=1";
            try
            {
                HttpResponseMessage response = _httpClient.GetAsync(url).Result;
                
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                
                    RestResponse<object>? stateResponse = JsonConvert.DeserializeAnonymousType(responseContent, new RestResponse<object>());
                
                    if(stateResponse != null)
                    {
                        if(stateResponse.code.Equals("0") && stateResponse.msg.Equals("success"))
                        {
                            RestResponse<List<GlobalAccountRatio>>? responseRatio= JsonConvert.DeserializeAnonymousType(responseContent, new RestResponse<List<GlobalAccountRatio>>());

                            if (responseRatio == null) return;

                            LongShortRatio lsr = new LongShortRatio();
                            
                            lsr.Time = DateTimeOffset.FromUnixTimeSeconds(long.Parse(responseRatio.data[0].time)).UtcDateTime;
                            lsr.LSR = responseRatio.data[0].longShortRatio.ToDecimal();
                            lsr.Long = responseRatio.data[0].longAccount.ToDecimal();
                            lsr.Short = responseRatio.data[0].shortAccount.ToDecimal();

                            data.Add(lsr); 
                            
                            CoinglassUpdateEvent?.Invoke(request.BotName, request.ResponseType, data);

                            return;
                        }
                    
                        SendLogMessage($"Coinglass error response: Code: {stateResponse.code}, Message: {stateResponse.msg}",LogMessageType.Error);
                    }

                    SendLogMessage($"Coinglass deserialisation error - returned null",LogMessageType.Error);
                }
                else
                {
                    SendLogMessage($"Coinglass query > Http State Code: {response.StatusCode}", LogMessageType.Error); 
                }
            }
            catch (Exception ex) 
            {
                SendLogMessage($"Error while getting {request.ResponseType}: {ex.Message}", LogMessageType.Error);
            }
        }
        
        /// <summary>
        /// Записывает новый запрос в очередь запросов.
        /// </summary>
        /// <param name="requestContent">Содержимое запроса</param>
        public void SendRequest(RequestContent requestContent)
        {
            _requestsQueue.Enqueue(requestContent); //отправляем запрос в очередь
        }
        
        /// <summary>
        /// Отправляет сообщение в лог.
        /// </summary>
        /// <param name="message">Сообщение для отправки</param>
        /// <param name="messageType">Тип отправляемого сообщения</param>
        private void SendLogMessage(string message, LogMessageType messageType)
        {
            LogMessageEvent?.Invoke(message, messageType);
        }

        public event Action<string, LogMessageType>? LogMessageEvent;
    }
}