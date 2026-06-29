#include "ApiClient.h"
#include <mutex>
#include <list>
#include <string>
#include <iostream>
#include <nlohmann/json.hpp>
#include <chrono>
#include <ctime>
#include <iomanip>
#include <sstream>

using namespace std;

extern list<string> MessagesOut;
extern mutex mutex_array_out;

vector<DstarApiPositionField> listPositions;
vector<DstarApiContractField> listContractIndex;

ApiClient::ApiClient()
    : m_pApi(NULL)
    , m_bReady(false)
    , m_bUdpAuth(false)
{
        
}

ApiClient::~ApiClient()
{
    FreeDstarTradeApi(m_pApi);
}

string ApiClient::DoubleToString(double val)
{
    string s = to_string(val);
    s.erase(s.find_last_not_of('0') + 1);
    s.erase(s.find_last_not_of('.') + 1);
    return s;
}

vector<DstarApiContractField> ApiClient::GetListContractIndex()
{
    std::vector<DstarApiContractField> result;
    result.swap(listContractIndex);
    return result;
}

string ApiClient::GetDateTimeNow()
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

void ApiClient::SetAddress(const char *ip, int port)
{
    strncpy(m_FrontIp, ip, sizeof(m_FrontIp) - 1);
    m_FrontPort = port;
}

void ApiClient::SetUser(const char *account, const char *passwd, const char *appid, const char *licenseno)
{
    strncpy(m_LoginReq.AccountNo, account, sizeof(DstarApiAccountNoType) - 1);
    strncpy(m_LoginReq.Password, passwd, sizeof(DstarApiPasswdType) - 1);
    strncpy(m_LoginReq.AppId, appid, sizeof(DstarApiAppIdType) - 1);
    strncpy(m_LoginReq.LicenseNo, licenseno, sizeof(DstarApiLicenseNoType) - 1);
}

int ApiClient::CreateApi()
{
    m_pApi = CreateDstarTradeApi();
    if (!m_pApi)
    {
        cout << ApiClient::GetDateTimeNow() << "Create Api Failed" << endl;
        return 1;
    }
    else
    {
        cout << ApiClient::GetDateTimeNow() << "Create Api Successfully, version:" << m_pApi->GetApiVersion() << endl;
    }

    return 0;
}

int ApiClient::Init()
{
    if (!m_pApi)
    {
        return -1;
    }

    m_pApi->RegisterSpi(this);
    m_pApi->RegisterFrontAddress(m_FrontIp, m_FrontPort);
#if defined WIN32 || defined _WIN32
    char log_path[] = ".\\Esunny_Router\\TradeData\\TradeLogs\\";
#else
    char log_path[] = "/home/esunny/apidemo/";
#endif
    m_pApi->SetApiLogPath(log_path);
    m_pApi->SetLoginInfo(&m_LoginReq);
    m_pApi->SetCpuId(0, 1);

    // flags connection message
    DstarApiInitQryInfoField initQry = { 0 };
    
    initQry.ContractInitQryFlag = 1;
    initQry.CmbContractInitQryFlag = 0;
    initQry.SeatInitQryFlag = 0;
    initQry.TrdFeeInitQryFlag = 0;
    initQry.TrdMarInitQryFlag = 0;
    initQry.TrdRightInitQryFlag = 0;
    initQry.AccountCommListInitQryFlag = 0;
    initQry.TrdExchangeStateInitQryFlag = 0; // OnRspTrdExchangeState
    initQry.PrePositionInitQryFlag = 0;
    initQry.OrderInitQryFlag = 0;
    initQry.OfferInitQryFlag = 0;
    initQry.MatchInitQryFlag = 0;
    initQry.CashInOutInitQryFlag = 0;

    m_pApi->SetInitQryInfo(&initQry);

    m_pApi->SetSubscribeStartId(-1);

    int ret = SubmitSystemInfo();

    if (ret != 0)
    {
        return ret;
    }
    
    return m_pApi->Init();
}

bool ApiClient::IsReady()
{
    return m_bReady;
}

bool ApiClient::IsUdpAuth()
{
    return m_bUdpAuth;
}

void ApiClient::OnFrontDisconnected()
{
    cout << ApiClient::GetDateTimeNow() << "OnFrontDisconnected" << endl;

    lock_guard<mutex> lockout(mutex_array_out);
    MessagesOut.push_back("{\"type\":\"disconnect\"}");
}

void ApiClient::OnRspError(DstarApiErrorCodeType nErrorCode)
{
    cout << ApiClient::GetDateTimeNow() << "OnRspError:" << nErrorCode << endl;
}

void ApiClient::OnApiReady(const DstarApiSerialIdType nSerialId)
{
    cout << ApiClient::GetDateTimeNow() << "OnApiReady, serial:" << nSerialId << endl;

    m_bReady = true;

    lock_guard<mutex> lockout(mutex_array_out);
    MessagesOut.push_back("{\"type\":\"connect\"}");
}

void ApiClient::OnRspUserLogin(const DstarApiRspLoginField *pLoginRsp)
{
    m_LoginInfo = *pLoginRsp;
    printf("OnRspUserLogin user:%s index:%u error:%u authcode:%u\n",
           m_LoginInfo.AccountNo, m_LoginInfo.AccountIndex, m_LoginInfo.ErrorCode, m_LoginInfo.UdpAuthCode);
}

void ApiClient::OnRspSubmitInfo(const DstarApiRspSubmitInfoField *pRspSubmitInfo)
{
    printf("OnRspSubmitInfo user:%s error:%u \n",
           pRspSubmitInfo->AccountNo, pRspSubmitInfo->ErrorCode);
}

void ApiClient::OnRtnOrder(const DstarApiOrderField *pOrder) 
{
    string json;

    json.reserve(1024);
    json.append("{\"type\":\"rtnOrder\",\"accountNo\":\"");
    json.append(pOrder->AccountNo);
    json.append("\",\"contractNo1\":\"");
    json.append(pOrder->ContractNo1);
    //json.append("\",\"contractNo2\":\"");
    //json.append(pOrder->ContractNo2);
    json.append("\",\"direct\":\"");
    json.append(1, pOrder->Direct);
    json.append("\",\"matchQty\":\"");
    json.append(to_string(pOrder->MatchQty));
    json.append("\",\"offset\":\"");
    json.append(1, pOrder->Offset);
    json.append("\",\"orderId\":\"");
    json.append(to_string(pOrder->OrderId));
    //json.append("\",\"orderLocalNo\":\"");
    //json.append(pOrder->OrderLocalNo);
    json.append("\",\"orderPrice\":\"");
    json.append(DoubleToString(pOrder->OrderPrice));
    json.append("\",\"orderQty\":\"");
    json.append(DoubleToString(pOrder->OrderQty));
    json.append("\",\"orderState\":\"");
    json.append(1, pOrder->OrderState);
    json.append("\",\"orderType\":\"");
    json.append(1, pOrder->OrderType);
    json.append("\",\"updateTime\":\"");
    json.append(pOrder->UpdateTime);
    json.append("\",\"errCode\":\"");
    json.append(to_string(pOrder->ErrCode));
    json.append("\",\"reference\":\"");
    json.append(to_string(pOrder->Reference));
    json.append("\"}");

    {
        lock_guard<mutex> lockout(mutex_array_out);
        MessagesOut.push_back(json);
    }

    cout << ApiClient::GetDateTimeNow() << "API -> " << json << endl;

    /*printf("OnRtnOrder AccountNo:%s ContractNo1:%s ContractNo2:%s Direct:%c "
            "ExchInsertTime:%s Fee:%f FrozenMargin:%f Hedge:%c Margin:%f MatchQty:%d "
            "MinQty:%d Offset:%c OrderId:%llu OrderLocalNo:%s OrderPrice:%f OrderQty:%d "
            "OrderState:%c OrderType:%c UpSeatNo:%s SystemNo:%s UpdateTime:%s SeatIndex:%d "
            "CmbId:%llu ErrCode:%u\n",
            pOrder->AccountNo,
            pOrder->ContractNo1,
            pOrder->ContractNo2,
            pOrder->Direct,
            pOrder->ExchInsertTime,
            pOrder->Fee,
            pOrder->FrozenMargin,
            pOrder->Hedge,
            pOrder->Margin,
            pOrder->MatchQty,
            pOrder->MinQty,
            pOrder->Offset,
            pOrder->OrderId,
            pOrder->OrderLocalNo,
            pOrder->OrderPrice,
            pOrder->OrderQty,
            pOrder->OrderState,
            pOrder->OrderType,
            pOrder->UpSeatNo,
            pOrder->SystemNo,
            pOrder->UpdateTime,
            pOrder->SeatIndex,
            pOrder->CmbId,
            pOrder->ErrCode);*/
}



void ApiClient::OnRtnMatch(const DstarApiMatchField *pMatch)
{
    string json;

    json.reserve(1024);
    json.append("{\"type\":\"rtnMatch\",\"accountNo\":\"");
    json.append(pMatch->AccountNo);
    json.append("\",\"contractNo\":\"");
    json.append(pMatch->ContractNo);
    json.append("\",\"direct\":\"");
    json.append(1, pMatch->Direct);
    json.append("\",\"matchId\":\"");
    json.append(to_string(pMatch->MatchId));    
    json.append("\",\"matchPrice\":\"");
    json.append(to_string(pMatch->MatchPrice));
    json.append("\",\"matchQty\":\"");
    json.append(to_string(pMatch->MatchQty));
    json.append("\",\"matchTime\":\"");
    json.append(pMatch->MatchTime);
    json.append("\",\"offset\":\"");
    json.append(1, pMatch->Offset);
    json.append("\",\"orderId\":\"");
    json.append(to_string(pMatch->OrderId));    
    json.append("\",\"orderType\":\"");
    json.append(to_string(pMatch->OrderType));    
    json.append("\",\"updateTime\":\"");
    json.append(pMatch->UpdateTime);
    
    json.append("\"}");

    {
        lock_guard<mutex> lockout(mutex_array_out);
        MessagesOut.push_back(json);
    }

    cout << ApiClient::GetDateTimeNow() << "API -> " << json << endl;

    /*printf("OnRtnMatch MatchId: %llu AccountNo:%s CloseProfit:%f ContractNo:%s Direct:%c ExchMatchNo:%s "
            "Fee:%f FrozenMargin:%f Hedge:%c Margin:%f MatchId:%llu MatchPrice:%f MatchQty:%d "
            "MatchTime:%s Offset:%c OrderId:%llu OrderType:%c Premium:%f Reference:%llu SerialId:%llu "
            "SystemNo:%s UpdateTime:%s\n", 
            pMatch->MatchId,
            pMatch->AccountNo,
            pMatch->CloseProfit,
            pMatch->ContractNo,
            pMatch->Direct,
            pMatch->ExchMatchNo,
            pMatch->Fee,
            pMatch->FrozenMargin,
            pMatch->Hedge,
            pMatch->Margin,
            pMatch->MatchId,
            pMatch->MatchPrice,
            pMatch->MatchQty,
            pMatch->MatchTime,
            pMatch->Offset,
            pMatch->OrderId,
            pMatch->OrderType,
            pMatch->Premium,
            pMatch->Reference,
            pMatch->SerialId,
            pMatch->SystemNo,
            pMatch->UpdateTime);*/
}

void ApiClient::OnRspQryPosition(const DstarApiPositionField* pPosition, bool bLast)
{
    if (pPosition != nullptr)
    {
        listPositions.push_back(*pPosition);

        /*printf("OnRspQryPosition, AccountNo:%s, ContractNo:%s, SerialId:%llu, %d, %d, %f, %d, %d, %f\n",
            pPosition->AccountNo,
            pPosition->ContractNo,
            pPosition->SerialId,
            pPosition->PreBuyQty,
            pPosition->TodayBuyQty,
            pPosition->BuyAvgPrice,
            pPosition->PreSellQty,
            pPosition->TodaySellQty,
            pPosition->SellAvgPrice);*/
    }

    if (bLast)
    {
        string msg;
        msg.reserve(listPositions.size() * 350);

        for (vector<DstarApiPositionField>::iterator it = listPositions.begin(); it != listPositions.end(); it++)
        {
            DstarApiPositionField obj = *it;

            msg.append("{\"contractNo\":\"");
            msg.append(obj.ContractNo);
            msg.append("\", \"accountNo\":\"");
            msg.append(obj.AccountNo);
            msg.append("\", \"preBuyQty\":\"");
            msg.append(DoubleToString(obj.PreBuyQty));
            msg.append("\", \"todayBuyQty\":\"");
            msg.append(DoubleToString(obj.TodayBuyQty));
            msg.append("\", \"buyAvgPrice\":\"");
            msg.append(DoubleToString(obj.BuyAvgPrice));
            msg.append("\", \"preSellQty\":\"");
            msg.append(DoubleToString(obj.PreSellQty));
            msg.append("\", \"todaySellQty\":\"");
            msg.append(DoubleToString(obj.TodaySellQty));
            msg.append("\", \"sellAvgPrice\":\"");
            msg.append(DoubleToString(obj.SellAvgPrice));
            msg.append("\"}");

            if (next(it) != listPositions.end())
            {
                msg += ",";
            }
        }

        string json = "{\"type\":\"positions\",\"list\":[" + msg + "]}";

        listPositions.clear();

        cout << ApiClient::GetDateTimeNow() << "API -> " << json << endl;

        lock_guard<mutex> outLock(mutex_array_out);
        MessagesOut.push_back(json);        
    }
}

void ApiClient::OnRspQryFund(const DstarApiFundField* pFund)
{
    if (!pFund) return;

    /*printf("OnRspFund AccountNo:%s, PreEquity:%f, Equity:%f, Avail:%f, Fee:%f, Margin:%f, "
        "FrozenMargin:%f, Premium:%f, CloseProfit:%f, PositionProfit:%f, CashIn:%f, CashOut:%f\n",
        pFund->AccountNo, pFund->PreEquity, pFund->Equity, pFund->Avail,
        pFund->Fee, pFund->Margin, pFund->FrozenMargin, pFund->Premium,
        pFund->CloseProfit, pFund->PositionProfit, pFund->CashIn, pFund->CashOut);*/

    string json;

    json.reserve(128);
    json.append("{\"type\":\"account\",\"accountNo\":\"");
    json.append(pFund->AccountNo);
    json.append("\",\"equity\":\"");
    json.append(DoubleToString(pFund->Equity));
    json.append("\",\"avail\":\"");
    json.append(DoubleToString(pFund->Avail));
    json.append("\",\"margin\":\"");
    json.append(DoubleToString(pFund->Margin));
    json.append("\",\"positionProfit\":\"");
    json.append(DoubleToString(pFund->PositionProfit));
    json.append("\"}");

    cout << ApiClient::GetDateTimeNow() << "API -> " << json << endl;

    lock_guard<mutex> lockout(mutex_array_out);
    MessagesOut.push_back(json);
}

int ApiClient::SubmitSystemInfo()
{
    char systeminfo[1024] = { 0 };
    int nLen = 1024;
    unsigned int nVersion = 0;
    int ret = m_pApi->GetSystemInfo(systeminfo, &nLen, &nVersion);
    printf("GetSystemInfo, ret:%d, len:%d, version:%d\n", ret, nLen, nVersion);
    if (ret != 0)
    {
        return ret;
    }

    DstarApiSubmitInfoField pSubmitInfo = { 0 };
    strncpy(pSubmitInfo.AccountNo, m_LoginReq.AccountNo, sizeof(DstarApiAccountNoType) - 1);
    pSubmitInfo.AuthType = DSTAR_API_AUTHTYPE_DIRECT;
    pSubmitInfo.AuthKeyVersion = nVersion;
    memcpy(pSubmitInfo.SystemInfo, systeminfo, nLen);
    strncpy(pSubmitInfo.LicenseNo, m_LoginReq.LicenseNo, sizeof(DstarApiLicenseNoType) - 1);
    strncpy(pSubmitInfo.ClientAppId, m_LoginReq.AppId, sizeof(DstarApiAppIdType) - 1);

    m_pApi->SetSubmitInfo(&pSubmitInfo);

    return ret;
}

void ApiClient::ReqOrderInsert(const DstarApiReqOrderInsertField* pOrder)
{
    m_pApi->ReqOrderInsert(pOrder);
}

void ApiClient::ReqOrderDelete(const DstarApiReqOrderDeleteField* pOrder)
{
    m_pApi->ReqOrderDelete(pOrder);
}

void ApiClient::ReqQryFund()
{
    m_pApi->ReqQryFund();
}

void ApiClient::ReqQryPosition()
{
    m_pApi->ReqQryPosition();
}

void ApiClient::OnRspContract(const DstarApiContractField* pContract)
{
    //printf("OnRspContract ContractIndex:%u ContractNo:%s\n", pContract->ContractIndex, pContract->ContractNo);

    listContractIndex.push_back(*pContract);
}

void ApiClient::OnRspTrdExchangeState(const DstarApiTrdExchangeStateField* pTrdExchangeState)
{
    printf("OnRspTrdExchangeState ExchangeId: %c, CommodityType: %c, CommodityNo: %s, TradingState: %c ExchangeTime: %s\n",
        pTrdExchangeState->ExchangeId,
        pTrdExchangeState->CommodityType,
        pTrdExchangeState->CommodityNo,
        pTrdExchangeState->TradingState,
        pTrdExchangeState->ExchangeTime);
}

void ApiClient::OnRtnOffer(const DstarApiOfferField* pOffer)
{
    printf("OnRtnOffer AccountNo:%s BuyMatchQty:%d BuyOffset:%c BuyPrice:%f ContractNo:%s EnquiryNo:%s "
        "ExchInsertTime:%s FrozenMargin:%f Margin:%f OrderId:%llu OrderLocalNo:%s OrderQty:%d "
        "OrderState:%c Reference:%llu SellMatchQty:%d SellOffset:%c SellPrice:%f SerialId:%llu "
        "SystemNo:%s UpSeatNo:%s UpdateTime:%s ErrCode:%u\n",
        pOffer->AccountNo,
        pOffer->BuyMatchQty,
        pOffer->BuyOffset,
        pOffer->BuyPrice,
        pOffer->ContractNo,
        pOffer->EnquiryNo,
        pOffer->ExchInsertTime,
        pOffer->FrozenMargin,
        pOffer->Margin,
        pOffer->OrderId,
        pOffer->OrderLocalNo,
        pOffer->OrderQty,
        pOffer->OrderState,
        pOffer->Reference,
        pOffer->SellMatchQty,
        pOffer->SellOffset,
        pOffer->SellPrice,
        pOffer->SerialId,
        pOffer->SystemNo,
        pOffer->UpSeatNo,
        pOffer->UpdateTime,
        pOffer->ErrCode);
}

void ApiClient::OnRtnEnquiry(const DstarApiEnquiryField *pEnquiry)
{
    printf("OnRtnEnquiry ContractNo:%s Direct:%c EnquiryNo:%s\n",
           pEnquiry->ContractNo, 
           pEnquiry->Direct, 
           pEnquiry->EnquiryNo);
}

void ApiClient::OnRtnTrdExchangeState(const DstarApiTrdExchangeStateField *pTrdExchangeState) 
{
    printf("OnRspTrdExchangeState ExchangeId: %c, CommodityType: %c, CommodityNo: %s, TradingState: %c ExchangeTime: %s\n",
            pTrdExchangeState->ExchangeId,
            pTrdExchangeState->CommodityType,
            pTrdExchangeState->CommodityNo,
            pTrdExchangeState->TradingState,
            pTrdExchangeState->ExchangeTime);
}

void ApiClient::OnRtnPosiProfit(const DstarApiPosiProfitField *pPosiProfit)
{
    /*printf("OnRtnPosiProfit AccountNo: %s PosiProfit: %f SerialId: %llu\n",
            pPosiProfit->AccountNo,
            pPosiProfit->PosiProfit,
            pPosiProfit->SerialId);*/
}

void ApiClient::OnRspSeat(const DstarApiSeatField *pSeatInfo)
{
    printf("OnRspSeat seatindex:%d exchange:%c seatno:%s ip:%s\n", pSeatInfo->SeatIndex, pSeatInfo->Exchange, pSeatInfo->SeatNo, pSeatInfo->Ip);
}

void ApiClient::OnRspCmbContract(const DstarApiCmbContractField *pCmbContract)
{
    printf("OnRspCmbContract ContractIndex:%u ContractNo:%s\n", pCmbContract->ContractIndex1, pCmbContract->ContractNo1);
}

void ApiClient::OnRspTrdFeeParam(const DstarApiTrdFeeParamField* pFeeParam) 
{
    printf("OnRspTrdFeeParam AccountNo:%s ContractNo:%s OpenRatio:%f OpenVolume:%f CloseRatio:%f CloseVolume:%f CloseTRatio:%f CloseTVolume:%f\n",
            pFeeParam->AccountNo,
            pFeeParam->ContractNo,
            pFeeParam->OpenRatio,
            pFeeParam->OpenVolume,
            pFeeParam->CloseRatio,
            pFeeParam->CloseVolume,
            pFeeParam->CloseTRatio,
            pFeeParam->CloseTVolume);
}

void ApiClient::OnRspTrdMarParam(const DstarApiTrdMarParamField* pMarParam) 
{
    printf("OnRspTrdMarParam AccountNo:%s ContractNo:%s BuySpeculateParam:%f BuyHedgeParam:%f SellSpeculateParam:%f SellHedgeParam:%f\n",
            pMarParam->AccountNo,
            pMarParam->ContractNo,
            pMarParam->BuySpeculateParam,
            pMarParam->BuyHedgeParam,
            pMarParam->SellSpeculateParam,
            pMarParam->SellHedgeParam);
}

void ApiClient::OnRspTradeRight(const DstarApiTradeRightField* pTradeRight)
{
    printf("OnRspTradeRight AccountNo:%s ExchangeId:%c CommodityType:%c CommodityNo:%s BuyTradeRight:%c SellTradeRight:%c\n",
           pTradeRight->AccountNo,
           pTradeRight->ExchangeId,
           pTradeRight->CommodityType,
           pTradeRight->CommodityNo,
           pTradeRight->BuyTradeRight,
           pTradeRight->SellTradeRight);
}

void ApiClient::OnRspAccountCommList(const DstarApiAccountCommListField* pAccountCommList)
{
    printf("OnRspTradeRight AccountNo:%s ExchangeId:%s CommodityType:%c CommodityNo:%s\n",
           pAccountCommList->AccountNo,
           pAccountCommList->ExchangeId,
           pAccountCommList->CommodityType,
           pAccountCommList->CommodityNo);
}

void ApiClient::OnRspPrePosition(const DstarApiPrePositionField *pPrePosition)
{
    printf("OnRspPrePosition,AccountNo:%s, ContractNo:%s, %d, %f, %d, %f\n",
            pPrePosition->AccountNo, 
            pPrePosition->ContractNo, 
            pPrePosition->PreBuyQty, 
            pPrePosition->PreBuyAvgPrice,
            pPrePosition->PreSellQty,
            pPrePosition->PreSellAvgPrice);
}

void ApiClient::OnRspPosition(const DstarApiPositionField *pPosition)
{
    /*printf("OnRspPosition,AccountNo:%s, ContractNo:%s, SerialId:%llu, %d, %d, %f, %d, %d, %f\n",
            pPosition->AccountNo, 
            pPosition->ContractNo, 
            pPosition->SerialId,
            pPosition->PreBuyQty,
            pPosition->TodayBuyQty,
            pPosition->BuyAvgPrice,
            pPosition->PreSellQty,
            pPosition->TodaySellQty,
            pPosition->SellAvgPrice);*/
}

void ApiClient::OnRspFund(const DstarApiFundField *pFund)
{
    /*printf("OnRspFund AccountNo:%s, PreEquity:%f, Equity:%f, Avail:%f, Fee:%f, Margin:%f, "
            "FrozenMargin:%f, Premium:%f, CloseProfit:%f, PositionProfit:%f, CashIn:%f, CashOut:%f\n",
            pFund->AccountNo, pFund->PreEquity, pFund->Equity, pFund->Avail,
            pFund->Fee, pFund->Margin, pFund->FrozenMargin, pFund->Premium,
            pFund->CloseProfit, pFund->PositionProfit, pFund->CashIn, pFund->CashOut);*/
}

void ApiClient::OnRspOrder(const DstarApiOrderField *pOrder)
{
    printf("OnRspOrder ContractNo:%s, OrderPrice:%f, OrderQty:%d, MinQty:%d, MatchQty:%d, Direct:%c, Offset:%c, Hedge:%c, OrderType:%c, "
            "ValidType:%c, OrderState:%c, ErrCode:%d, OrderId:%llu, OrderLocalNo:%s, SystemNo:%s, ExchInsertTime:%s, "
            "SerialId:%llu, FrozenMargin:%f, AccountNo:%s, Ref:%d, SeatIndex:%d\n",
            pOrder->ContractNo1, pOrder->OrderPrice, pOrder->OrderQty, pOrder->MinQty,
            pOrder->MatchQty, pOrder->Direct, pOrder->Offset, pOrder->Hedge,
            pOrder->OrderType, pOrder->ValidType, pOrder->OrderState, pOrder->ErrCode,
            pOrder->OrderId, pOrder->OrderLocalNo, pOrder->SystemNo, pOrder->ExchInsertTime,
            pOrder->SerialId, pOrder->FrozenMargin, pOrder->AccountNo, pOrder->Reference, pOrder->SeatIndex);
}

void ApiClient::OnRspOffer(const DstarApiOfferField *pOffer)
{
    printf("OnRspOffer BuyOffset:%c, SellOffset:%c, ContractNo:%s, OrderQty:%d, ErrCode:%d, "
            "BuyPrice:%f, SellPrice:%f, OrderId:%llu, "
            "OrderLocalNo:%s, SystemNo:%s, QueryNo:%s, OrderState:%c, SerialId:%lld, FrozenMargin:%f, "
            "AccountNo:%s, SeatIndex:%d Reference:%llu\n",
            pOffer->BuyOffset, pOffer->SellOffset, pOffer->ContractNo,
            pOffer->OrderQty, pOffer->ErrCode, pOffer->BuyPrice, pOffer->SellPrice,
            pOffer->OrderId, pOffer->OrderLocalNo, pOffer->SystemNo,
            pOffer->EnquiryNo, pOffer->OrderState, pOffer->SerialId, pOffer->FrozenMargin,
            pOffer->AccountNo, pOffer->SeatIndex, pOffer->Reference);
}

void ApiClient::OnRspMatch(const DstarApiMatchField *pMatch)
{
    printf("OnRspMatch contract:%s qty:%d price:%f offset:%c direct:%c hedge:%c time:%s orderid:%llu "
            "matchid:%llu systemno:%s serial:%llu fee:%f margin:%f frozen:%f premium:%f closeprofit:%f "
            "acccount:%s reference:%d ordertype:%c\n",
            pMatch->ContractNo, pMatch->MatchQty, pMatch->MatchPrice, pMatch->Offset,
            pMatch->Direct, pMatch->Hedge, pMatch->MatchTime,
            pMatch->OrderId, pMatch->MatchId, pMatch->SystemNo, pMatch->SerialId,
            pMatch->Fee, pMatch->Margin, pMatch->FrozenMargin, pMatch->Premium,
            pMatch->CloseProfit, pMatch->AccountNo, pMatch->Reference, pMatch->OrderType);
}

void ApiClient::OnRspCashInOut(const DstarApiCashInOutField *pCashInOut)
{
    printf("OnRspCashInOut SerialId:%llu CashInOutType:%c CashInOutValue:%f AccountNo:%s OperateTime:%s\n",
            pCashInOut->SerialId, pCashInOut->CashInOutType, pCashInOut->CashInOutValue, pCashInOut->AccountNo,
            pCashInOut->OperateTime);
}

void ApiClient::OnRspUdpAuth(const DstarApiRspUdpAuthField *pRspUdpAuth)
{
    if (pRspUdpAuth->ErrorCode == 0)
    {
        m_bUdpAuth = true;
    }
    printf("OnRspUdpAuth AccountIndex:%d UdpAuthCode:%u ErrorCode:%u\n",
           pRspUdpAuth->AccountIndex, pRspUdpAuth->UdpAuthCode, pRspUdpAuth->ErrorCode);
}

void ApiClient::OnRspOrderInsert(const DstarApiRspOrderInsertField *pOrderInsert)
{
    /*string json;

    json.reserve(256);
    json.append("{\"type\":\"rspOrderInsert\",\"accountNo\":\"");
    json.append(pOrderInsert->AccountNo);
    json.append("\",\"clientReqId\":\"");
    json.append(to_string(pOrderInsert->ClientReqId));
    json.append("\",\"orderId\":\"");
    json.append(to_string(pOrderInsert->OrderId));    

    json.append("\"}");

    {
        lock_guard<mutex> lockout(mutex_array_out);
        MessagesOut.push_back(json);
    }

    cout << "API -> " << json << endl;

    printf("OnRspOrderInsert AccountNo:%s ClientReqId:%d SeatIndex:%d OrderId:%llu ErrCode:%u MaxClientReqId:%u InsertTime:%s Reference:%llu\n",
            pOrderInsert->AccountNo,
            pOrderInsert->ClientReqId, 
            pOrderInsert->SeatIndex, 
            pOrderInsert->OrderId, 
            pOrderInsert->ErrCode, 
            pOrderInsert->MaxClientReqId,
            pOrderInsert->InsertTime,
            pOrderInsert->Reference);*/
}

void ApiClient::OnRspOfferInsert(const DstarApiRspOfferInsertField *pOfferInsert)
{
    printf("OnRspOfferInsert AccountNo:%s ClientReqId:%d SeatIndex:%d OrderId:%llu ErrCode:%u MaxClientReqId:%u InsertTime:%s Reference:%llu\n",
            pOfferInsert->AccountNo,
            pOfferInsert->ClientReqId,
            pOfferInsert->SeatIndex,
            pOfferInsert->OrderId,
            pOfferInsert->ErrCode,
            pOfferInsert->MaxClientReqId,
            pOfferInsert->InsertTime,
            pOfferInsert->Reference);
}

void ApiClient::OnRspLastReqId(const DstaApiRspLastReqIdField *pLastReqId)
{
    printf("OnRspLastReqId ClientReqId:%d \n", pLastReqId->LastClientReqId);
}

void ApiClient::OnRspOrderDelete(const DstarApiRspOrderDeleteField *pOrderDelete)
{
    printf("OnRspOrderDelete AccountNo:%s ClientReqId:%d SeatIndex:%d OrderId:%llu ErrCode:%u MaxClientReqId:%u InsertTime:%s Reference:%llu\n",
            pOrderDelete->AccountNo,
            pOrderDelete->ClientReqId, 
            pOrderDelete->SeatIndex, 
            pOrderDelete->OrderId, 
            pOrderDelete->ErrCode, 
            pOrderDelete->MaxClientReqId,
            pOrderDelete->InsertTime,
            pOrderDelete->Reference);
}

void ApiClient::OnRtnCashInOut(const DstarApiCashInOutField *pCashInOut)
{
    printf("OnRtnCashInOut\n");
}

void ApiClient::OnRtnSeat(const DstarApiSeatField* pSeat)
{
    printf("OnRspSeat seatindex:%d exchange:%c seatno:%s ip:%s\n", pSeat->SeatIndex, pSeat->Exchange, pSeat->SeatNo, pSeat->Ip);
}

void ApiClient::OnRspPwdMod(const DstarApiRspPwdModField* pRspPwdModField)
{
    printf("OnRspPwdMod AccountNo:%s ErrorCode:%d\n", pRspPwdModField->AccountNo, pRspPwdModField->ErrorCode);
}

void ApiClient::OnRtnPwdMod(const DstarApiPwdModField* pPwdModField)
{
    printf("OnRtnPwdMod AccountNo:%s\n", pPwdModField->AccountNo);
}

void ApiClient::OnRtnTradeRight(const DstarApiTradeRightField* pTradeRight)
{
    printf("OnRtnTradeRight AccountNo:%s\n", pTradeRight->AccountNo);
}

void ApiClient::OnRtnTradeRightDel(const DstarApiTradeRightDelField* pTradeRightDel)
{
    printf("OnRtnTradeRightDel AccountNo:%s\n", pTradeRightDel->AccountNo);
}

void ApiClient::ReqOfferInsert(const DstarApiReqOfferInsertField* pOffer)
{
    m_pApi->ReqOfferInsert(pOffer);
}

void ApiClient::ReqOfferInsertNew(const DstarApiReqOfferInsertNewField *pOffer)
{
    m_pApi->ReqOfferInsertNew(pOffer);
}

void ApiClient::ReqCmbOrderInsert(const DstarApiReqCmbOrderInsertField* pCmbOrder)
{
    m_pApi->ReqCmbOrderInsert(pCmbOrder);
}

DstarApiAccountIndexType ApiClient::GetAccountIndex()
{
    return m_LoginInfo.AccountIndex;
}

DstarApiAuthCodeType ApiClient::GetUdpAuthCode()
{
    return m_LoginInfo.UdpAuthCode;
}