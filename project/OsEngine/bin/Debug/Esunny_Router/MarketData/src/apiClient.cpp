#include <map>
#include <list>
#include <string>
#include <chrono>
#include <ctime>
#include <iomanip>
#include <sstream>

#include "apiClient.h"

extern list<string> MessagesOut;
extern mutex mutex_array_out;

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

string DoubleToString(double val)
{
    string s = to_string(val);
    s.erase(s.find_last_not_of('0') + 1);
    s.erase(s.find_last_not_of('.') + 1);
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
    //cout<<"apiClient::CommodityQry 1"<<endl;
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
//    if (!m_QuoteApi)
//    {
//        return -1;
//    }
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
    //cout<<"Notify::OnCommodityQry "<<str<<endl;
    
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
    //cout<<"Notify::OnCommodityQry "<<str<<endl;
    
    m_Api->set_contract.insert(str);
    
    if(isLast)
    {
        m_Api->isLast1_cont=isLast;
        printf("Api: OnRspContract END \n");
    }
}

void Notify::OnRtnQuote(const DstarApiQuoteData* info)
{   
    string msg;
    msg.reserve(2000);

    msg.append("\"contractNo\":\"");
    msg.append(info->QContractNo);
    msg.append("\",\"dateTimeStamp\":\"");
    msg.append(info->QDateTimeStamp);
    msg.append("\",\"lastPrice\":\"");
    msg.append(DoubleToString(info->QLastPrice));
    msg.append("\",\"lastQty\":\"");
    msg.append(DoubleToString(info->QLastQty));

    msg.append("\",\"bidPrice1\":\"");
    msg.append(DoubleToString(info->QBidPrice1));
    msg.append("\",\"bidPrice2\":\"");
    msg.append(DoubleToString(info->QBidPrice2));
    msg.append("\",\"bidPrice3\":\"");
    msg.append(DoubleToString(info->QBidPrice3));
    msg.append("\",\"bidPrice4\":\"");
    msg.append(DoubleToString(info->QBidPrice4));
    msg.append("\",\"bidPrice5\":\"");
    msg.append(DoubleToString(info->QBidPrice5));
    msg.append("\",\"bidPrice6\":\"");
    msg.append(DoubleToString(info->QBidPrice6));
    msg.append("\",\"bidPrice7\":\"");
    msg.append(DoubleToString(info->QBidPrice7));
    msg.append("\",\"bidPrice8\":\"");
    msg.append(DoubleToString(info->QBidPrice8));
    msg.append("\",\"bidPrice9\":\"");
    msg.append(DoubleToString(info->QBidPrice9));
    msg.append("\",\"bidPrice10\":\"");
    msg.append(DoubleToString(info->QBidPrice10));

    msg.append("\",\"bidQty1\":\"");
    msg.append(DoubleToString(info->QBidQty1));
    msg.append("\",\"bidQty2\":\"");
    msg.append(DoubleToString(info->QBidQty2));
    msg.append("\",\"bidQty3\":\"");
    msg.append(DoubleToString(info->QBidQty3));
    msg.append("\",\"bidQty4\":\"");
    msg.append(DoubleToString(info->QBidQty4));
    msg.append("\",\"bidQty5\":\"");
    msg.append(DoubleToString(info->QBidQty5));
    msg.append("\",\"bidQty6\":\"");
    msg.append(DoubleToString(info->QBidQty6));
    msg.append("\",\"bidQty7\":\"");
    msg.append(DoubleToString(info->QBidQty7));
    msg.append("\",\"bidQty8\":\"");
    msg.append(DoubleToString(info->QBidQty8));
    msg.append("\",\"bidQty9\":\"");
    msg.append(DoubleToString(info->QBidQty9));
    msg.append("\",\"bidQty10\":\"");
    msg.append(DoubleToString(info->QBidQty10));

    msg.append("\",\"askPrice1\":\"");
    msg.append(DoubleToString(info->QAskPrice1));
    msg.append("\",\"askPrice2\":\"");
    msg.append(DoubleToString(info->QAskPrice2));
    msg.append("\",\"askPrice3\":\"");
    msg.append(DoubleToString(info->QAskPrice3));
    msg.append("\",\"askPrice4\":\"");
    msg.append(DoubleToString(info->QAskPrice4));
    msg.append("\",\"askPrice5\":\"");
    msg.append(DoubleToString(info->QAskPrice5));
    msg.append("\",\"askPrice6\":\"");
    msg.append(DoubleToString(info->QAskPrice6));
    msg.append("\",\"askPrice7\":\"");
    msg.append(DoubleToString(info->QAskPrice7));
    msg.append("\",\"askPrice8\":\"");
    msg.append(DoubleToString(info->QAskPrice8));
    msg.append("\",\"askPrice9\":\"");
    msg.append(DoubleToString(info->QAskPrice9));
    msg.append("\",\"askPrice10\":\"");
    msg.append(DoubleToString(info->QAskPrice10));

    msg.append("\",\"askQty1\":\"");
    msg.append(DoubleToString(info->QAskQty1));
    msg.append("\",\"askQty2\":\"");
    msg.append(DoubleToString(info->QAskQty2));
    msg.append("\",\"askQty3\":\"");
    msg.append(DoubleToString(info->QAskQty3));
    msg.append("\",\"askQty4\":\"");
    msg.append(DoubleToString(info->QAskQty4));
    msg.append("\",\"askQty5\":\"");
    msg.append(DoubleToString(info->QAskQty5));
    msg.append("\",\"askQty6\":\"");
    msg.append(DoubleToString(info->QAskQty6));
    msg.append("\",\"askQty7\":\"");
    msg.append(DoubleToString(info->QAskQty7));
    msg.append("\",\"askQty8\":\"");
    msg.append(DoubleToString(info->QAskQty8));
    msg.append("\",\"askQty9\":\"");
    msg.append(DoubleToString(info->QAskQty9));
    msg.append("\",\"askQty10\":\"");
    msg.append(DoubleToString(info->QAskQty10));
    msg.append("\"");
    
    string json = "{\"type\":\"quote\"," + msg + "}";

    lock_guard<mutex> outLock(mutex_array_out);
    MessagesOut.push_back(json);

    cout << m_Api->GetDateTimeNow() << "API -> " << json << endl;

    //cout << "subcribeQuote: " << "info->QContractNo: " << info->QContractNo << "info->QLastPrice: "<<info->QLastPrice<< "info->QBidPrice1: " << info->QBidPrice1 << "info->QAskPrice1: " << info->QAskPrice1 << endl;

    /*std::cout.setf(std::ios::fixed);
   
    cout<<"info->QContractNo:      "<<info->QContractNo<<endl;              ///< 合约  
    cout<<"info->QDateTimeStamp:   "<<info->QDateTimeStamp<<endl;           ///< 时间戳   
    cout<<"info->QPreClosingPrice: "<<info->QPreClosingPrice<<endl;         ///< 昨收盘价      
    cout<<"info->QPreSettlePrice:  "<<info->QPreSettlePrice<<endl;          ///< 昨结算价      	
    cout<<"info->QPrePositionQty:  "<<info->QPrePositionQty<<endl;          ///< 昨持仓量      	
    cout<<"info->QOpeningPrice:    "<<info->QOpeningPrice<<endl;            ///< 开盘价       
    cout<<"info->QLastPrice:       "<<info->QLastPrice<<endl;               ///< 最新价   
    cout<<"info->QHighPrice:       "<<info->QHighPrice<<endl;               ///< 最高价    
    cout<<"info->QLowPrice:        "<<info->QLowPrice<<endl;                ///< 最低价    
    cout<<"info->QHisHighPrice:    "<<info->QHisHighPrice<<endl;            ///< 历史最高价   
    cout<<"info->QHisLowPrice:     "<<info->QHisLowPrice<<endl;             ///< 历史最低价   
    cout<<"info->QLimitUpPrice:    "<<info->QLimitUpPrice<<endl;            ///< 涨停价   
    cout<<"info->QLimitDownPrice:  "<<info->QLimitDownPrice<<endl;          ///< 跌停价    
    cout<<"info->QTotalQty:        "<<info->QTotalQty<<endl;                ///< 成交量    
    cout<<"info->QPositionQty:     "<<info->QPositionQty<<endl;             ///< 持仓量     
    cout<<"info->QAveragePrice:    "<<info->QAveragePrice<<endl;            ///< 均价     
    cout<<"info->QClosingPrice:    "<<info->QClosingPrice<<endl;            ///< 收盘价     
    cout<<"info->QSettlePrice:     "<<info->QSettlePrice<<endl;             ///< 结算价     
    cout<<"info->QLastQty:         "<<info->QLastQty<<endl;                 ///< 最新成交量     
    cout<<"info->QTotalBidQty:     "<<info->QTotalBidQty<<endl;             ///< 委买总量       
    cout<<"info->QTotalAskQty:     "<<info->QTotalAskQty<<endl;             ///< 委卖总量          
    cout<<"info->QBidPrice1:       "<<info->QBidPrice1<<endl;			    ///< 买价1
    cout<<"info->QBidPrice2:       "<<info->QBidPrice2<<endl;			    ///< 买价2
    cout<<"info->QBidPrice3:       "<<info->QBidPrice3<<endl;			    ///< 买价3
    cout<<"info->QBidPrice4:       "<<info->QBidPrice4<<endl;			    ///< 买价4
    cout<<"info->QBidPrice5:       "<<info->QBidPrice5<<endl;			    ///< 买价5
    cout<<"info->QBidPrice6:       "<<info->QBidPrice6<<endl;			    ///< 买价6
    cout<<"info->QBidPrice7:       "<<info->QBidPrice7<<endl;			    ///< 买价7
    cout<<"info->QBidPrice8:       "<<info->QBidPrice8<<endl;			    ///< 买价8
    cout<<"info->QBidPrice9:       "<<info->QBidPrice9<<endl;			    ///< 买价9
    cout<<"info->QBidPrice10:      "<<info->QBidPrice10<<endl;			    ///< 买价10
    cout<<"info->QBidQty1:         "<<info->QBidQty1<<endl;					///< 买量1
    cout<<"info->QBidQty2:         "<<info->QBidQty2<<endl;					///< 买量2
    cout<<"info->QBidQty3:         "<<info->QBidQty3<<endl;					///< 买量3
    cout<<"info->QBidQty4:         "<<info->QBidQty4<<endl;					///< 买量4
    cout<<"info->QBidQty5:         "<<info->QBidQty5<<endl;					///< 买量5
    cout<<"info->QBidQty6:         "<<info->QBidQty6<<endl;					///< 买量6
    cout<<"info->QBidQty7:         "<<info->QBidQty7<<endl;					///< 买量7
    cout<<"info->QBidQty8:         "<<info->QBidQty8<<endl;					///< 买量8
    cout<<"info->QBidQty9:         "<<info->QBidQty9<<endl;					///< 买量9
    cout<<"info->QBidQty10:        "<<info->QBidQty10<<endl;				///< 买量10
    cout<<"info->QAskPrice1:       "<<info->QAskPrice1<<endl;				///< 卖价1
    cout<<"info->QAskPrice2:       "<<info->QAskPrice2<<endl;				///< 卖价2
    cout<<"info->QAskPrice3:       "<<info->QAskPrice3<<endl;				///< 卖价3
    cout<<"info->QAskPrice4:       "<<info->QAskPrice4<<endl;				///< 卖价4
    cout<<"info->QAskPrice5:       "<<info->QAskPrice5<<endl;				///< 卖价5
    cout<<"info->QAskPrice6:       "<<info->QAskPrice6<<endl;				///< 卖价6
    cout<<"info->QAskPrice7:       "<<info->QAskPrice7<<endl;				///< 卖价7
    cout<<"info->QAskPrice8:       "<<info->QAskPrice8<<endl;				///< 卖价8
    cout<<"info->QAskPrice9:       "<<info->QAskPrice9<<endl;				///< 卖价9
    cout<<"info->QAskPrice10:      "<<info->QAskPrice10<<endl;				///< 卖价10
    cout<<"info->QAskQty1:         "<<info->QAskQty1<<endl;					///< 卖量1
    cout<<"info->QAskQty2:         "<<info->QAskQty2<<endl;					///< 卖量2
    cout<<"info->QAskQty3:         "<<info->QAskQty3<<endl;					///< 卖量3
    cout<<"info->QAskQty4:         "<<info->QAskQty4<<endl;					///< 卖量4
    cout<<"info->QAskQty5:         "<<info->QAskQty5<<endl;					///< 卖量5
    cout<<"info->QAskQty6:         "<<info->QAskQty6<<endl;					///< 卖量6
    cout<<"info->QAskQty7:         "<<info->QAskQty7<<endl;					///< 卖量7
    cout<<"info->QAskQty8:         "<<info->QAskQty8<<endl;					///< 卖量8
    cout<<"info->QAskQty9:         "<<info->QAskQty9<<endl;					///< 卖量9
    cout<<"info->QAskQty10:        "<<info->QAskQty10<<endl;				///< 卖量10
    cout<<"info->QTotalTurnOver:   "<<info->QTotalTurnOver<<endl;			///< 成交额
    cout<<"-----------------------------------------------------"<<endl;*/
}

void Notify::OnDisconnect(int reasonCode)
{
    string error = GetDstarErrorString(reasonCode);

    string str = R"({"type":"disconnect","code":")" + to_string(reasonCode) + R"(","message":")" + error + "\"}";

    cout << m_Api->GetDateTimeNow() << "Api: Disconnect to server, reason=" << reasonCode << endl;

    lock_guard<mutex> outLock(mutex_array_out);
    MessagesOut.push_back(str);    
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
