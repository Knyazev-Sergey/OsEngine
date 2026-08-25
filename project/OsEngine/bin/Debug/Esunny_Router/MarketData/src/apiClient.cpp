#include <list>
#include <string>
#include <chrono>
#include <ctime>
#include <iomanip>
#include <sstream>
#include <cstdio>
#include <windows.h>

#include "apiClient.h"

extern list<string> MessagesOut;
extern mutex mutex_array_out;
extern std::unordered_map<std::string, DstarApiQuoteData> latestQuotes;
extern std::mutex quotesMutex;

apiClient::apiClient()
            : isReady(false),
              isLast1_comm(false),
              isLast1_cont(false)
{
    m_QuoteSpi = new Notify(this);
}

apiClient::~apiClient()
{
    delete m_QuoteSpi;
}

#pragma region Helpers

string DoubleToString(double val)
{
    char buf[64];
    int len = snprintf(buf, sizeof(buf), "%.15g", val);

    if (len <= 0)
        return "0";

    std::string s(buf, static_cast<size_t>(len));

    size_t dot = s.find('.');
    if (dot != std::string::npos)
    {
        while (!s.empty() && s.back() == '0')
            s.pop_back();

        if (!s.empty() && s.back() == '.')
            s.pop_back();
    }

    return s;
}

string apiClient::GetDateTimeNow()
{
    auto now = std::chrono::system_clock::now();
    std::time_t now_time = std::chrono::system_clock::to_time_t(now);

    std::tm local_tm{};
#ifdef _WIN32
    localtime_s(&local_tm, &now_time);
#else
    localtime_r(&now_time, &local_tm);
#endif

    std::ostringstream oss;
    oss << "[" << std::put_time(&local_tm, "%Y-%m-%d %H:%M:%S") << "] ";

    return oss.str();
}

#pragma endregion

#pragma region Requests

bool apiClient::Init()
{
    int iResult = 0;
    
    m_QuoteApi = CreateDstarQuoteApi();
    if(iResult != 0)
    {
        printf("Api: CreateDelayQuoteAPI fail,err:%d",iResult);
        return false;
    }
  
    iResult = m_QuoteApi->SetSpi(m_QuoteSpi);
    if(iResult != 0)
    {
        printf("Api: SetAPINotify fail,err:%d",iResult);
        return false;
    }
    
    if(!m_QuoteApi)
    {
        return false;
    }
    else
    {
        return true;
    }
    
    return true;
}

bool apiClient::Login()
{
    if (!m_QuoteApi)
    {
        return false;
    }
    
    int ret = m_QuoteApi->Start();

    cout << apiClient::GetDateTimeNow() << "Api: apiClient Login ret "<< ret <<"\n";
    
    if (ret != 0)
    {
        return false;
    }
    else
    {
        return true;
    }
}

int apiClient::SubscribeQuote(const char * str)
{
    if (!m_QuoteApi)
    {
        return -1;
    }
    
    m_QuoteApi->Subscribe(str);
    return 0;
}

int apiClient::UnSubscribeQuote(const char *contract)
{
    if (!m_QuoteApi)
    {
        return -1;
    }
    
    m_QuoteApi->UnSubscribe(contract);
    return 0;
}

bool apiClient::SetAutoRelogin(bool relogin)
{
    if (!m_QuoteApi)
    {
        return false;
    }
    
    m_QuoteApi->SetAutoRelogin(relogin);
}

int apiClient::CommodityQry()
{
    if (!m_QuoteApi)
    {
        return -1;
    }

    m_QuoteApi->QryCommodity();

    return 0;
}

int apiClient::ContractQry()
{
    if (!m_QuoteApi)
    {
        return -1;
    }
    
    m_QuoteApi->QryContract();

    return 0;
}

void apiClient::SetCpuId(DstarApiCpuIdType nRecvNoticeDataCpuId)
{
    m_QuoteApi->SetCpuId(nRecvNoticeDataCpuId);
}

bool apiClient::SetHostAddress(const char *IP, unsigned short port)
{
    bool ret=false;
    ret = m_QuoteApi->SetHostAddress(IP, port);
    return ret;
}

const char* apiClient::GetVerion()
{
    return GetApiVersion();
}

void apiClient::SetApiLogPath(const DstarApiPathType pPath)
{
    m_QuoteApi->SetApiLogPath(pPath);
}

void apiClient::Free()
{
    FreeDstarQuoteApi(m_QuoteApi);
}

string GetDstarErrorString(int code)
{
    switch (code)
    {
    case ERROR_DISCONNECT_CLOSE_PASS:        return "ERROR_DISCONNECT_CLOSE_PASS";
    case ERROR_DISCONNECT_CONNECT_TIMEOUT:   return "ERROR_DISCONNECT_CONNECT_TIMEOUT";
    case ERROR_DISCONNECT_RECONNECT_TIMEOUT: return "ERROR_DISCONNECT_RECONNECT_TIMEOUT";
    case ERROR_SEND_LOGIN_DATA:              return "ERROR_SEND_LOGIN_DATA";
    case ERROR_SEND_HEARTBEATDATA:           return "ERROR_SEND_HEARTBEATDATA";
    case ERROR_SEND_COMMODITYDATA:           return "ERROR_SEND_COMMODITYDATA";
    case ERROR_SEND_CONTRACTDATA:            return "ERROR_SEND_CONTRACTDATA";
    case ERROR_SUBSNAPSHOTDATA:              return "ERROR_SUBSNAPSHOTDATA";
    case ERROR_UNSUBSNAPSHOTDATA:            return "ERROR_UNSUBSNAPSHOTDATA";
    case ERROR_NO_DECIMAL:                   return "ERROR_NO_DECIMAL";
    case ERROR_OPEN_LOGDIR:                  return "ERROR_OPEN_LOGDIR";
    case ERROR_INPUT_NULL:                   return "ERROR_INPUT_NULL";
    case ERROR_UNKNOWN_CONTRACT:             return "ERROR_UNKNOWN_CONTRACT";
    case ERROR_UNSUBCONT:                    return "ERROR_UNSUBCONT";
    case ERROR_INIT_LOG:                     return "ERROR_INIT_LOG";
    case ERROR_SET_CPUID:                    return "ERROR_SET_CPUID";
    case ERROR_IP_FORMAT:                    return "ERROR_IP_FORMAT";
    case ERROR_LOG_PATH:                     return "ERROR_LOG_PATH";
    default:                                 return "Unknown error";
    }
}

#pragma endregion

#pragma region Notify

Notify::Notify(apiClient *testApi)
            : m_Api(testApi)
{

}

void Notify::OnApiReady()
{
    cout << m_Api->GetDateTimeNow() << "Api: OnApiReady" << endl;
    m_Api->isReady = true;
}

void Notify::OnRspCommodity(const DstarQuoteApiCommodityData* buf, bool isLast)
{
    std::string str(buf->CommodityNo);    
    m_Api->map_commodity.insert(make_pair(str, *buf));
    
    if(isLast)
    {
        m_Api->isLast1_comm=isLast;
        cout << m_Api->GetDateTimeNow() << "Api: OnRspCommodity END" << endl;
    }
}

void Notify::OnError(int reasonCode)
{
    if(reasonCode)
    {
        cout << m_Api->GetDateTimeNow() << "Api: OnError ErrorNo=" << reasonCode << endl;
    }
}

void Notify::OnRspContract(const char* buf, bool isLast)
{
    std::string str(buf);
    
    m_Api->set_contract.insert(str);
    
    if(isLast)
    {
        m_Api->isLast1_cont=isLast;
        printf("Api: OnRspContract END \n");
    }
}

void Notify::OnDisconnect(int reasonCode)
{
    string error = GetDstarErrorString(reasonCode);

    string str = R"({"type":"disconnect","code":")" + to_string(reasonCode) + R"(","message":")" + error + "\"}";

    cout << m_Api->GetDateTimeNow() << "Api: Disconnect to server, reason=" << reasonCode << endl;

    lock_guard<mutex> outLock(mutex_array_out);
    MessagesOut.push_back(str);
}

void Notify::OnRtnQuote(const DstarApiQuoteData* info)
{
    try
    {
        m_Api->PushQuote(*info);          
    }
    catch (const std::exception& e)
    {
        cout << m_Api->GetDateTimeNow() << "Notify::OnRtnQuote error: " << e.what() << '\n';
        Sleep(2000);
    }

    //cout << "subcribeQuote: " << "info->QContractNo: " << info->QContractNo << "info->QLastPrice: "<<info->QLastPrice<< "info->QBidPrice1: " << info->QBidPrice1 << "info->QAskPrice1: " << info->QAskPrice1 << endl;       
}

void apiClient::PushQuote(const DstarApiQuoteData& quote)
{
    try
    {
        std::lock_guard<std::mutex> lock(quotesMutex);
        latestQuotes.insert_or_assign(quote.QContractNo, quote);        
    }
    catch (const std::exception& e)
    {
        cout << GetDateTimeNow() << "PushQuote error: " << e.what() << '\n';
        Sleep(2000);
    }
}

#pragma endregion