///@system  Dstar V10 api demo
///@file    ApiClient.h
///@author  Hao Lin 2021-01-20

#ifndef _CLIENT_H_
#define _CLIENT_H_

#include <string.h>
#include <stdio.h>
#include <vector>
#include <string>

#include "DstarTradeApiError.h"
#include "DstarTradeApiDataType.h"
#include "DstarTradeApiStruct.h"
#include "DstarTradeApi.h"
#include "UdpClient.h"

class ApiClient : public IDstarTradeSpi
{
public:
    ApiClient();
    virtual ~ApiClient();

    void SetAddress(const char *ip, int port);
    void SetUser(const char *account, const char *passwd, const char *appid, const char *licenseno);
    int CreateApi();

    int Init();

    bool IsReady();

    bool IsUdpAuth();
    
    DstarApiAccountIndexType GetAccountIndex();
    DstarApiAuthCodeType GetUdpAuthCode();

    std::string DoubleToString(double val);

    std::vector<DstarApiContractField> GetListContractIndex();

    std::string GetDateTimeNow();

    //API
protected:
        ///客户端与接口通信连接断开
    virtual void OnFrontDisconnected();

    ///错误应答
    virtual void OnRspError(DstarApiErrorCodeType nErrorCode);

    ///登录请求响应,错误码为0说明用户登录成功。
    virtual void OnRspUserLogin(const DstarApiRspLoginField *pRspUserLogin);
    
    // 密码修改应答
    virtual void OnRspPwdMod(const DstarApiRspPwdModField *pRspPwdModField);
    
    ///提交信息响应
    virtual void OnRspSubmitInfo(const DstarApiRspSubmitInfoField *pRspSubmitInfo);

    ///合约信息响应
    virtual void OnRspContract(const DstarApiContractField *pContract);

    ///组合合约信息响应
    virtual void OnRspCmbContract(const DstarApiCmbContractField *pCmbContract);

    ///席位信息响应
    virtual void OnRspSeat(const DstarApiSeatField* pSeat);

    ///手续费参数响应
    virtual void OnRspTrdFeeParam(const DstarApiTrdFeeParamField* pFeeParam);

    ///保证金参数响应
    virtual void OnRspTrdMarParam(const DstarApiTrdMarParamField* pMarParam);

    ///交易权限响应
    virtual void OnRspTradeRight(const DstarApiTradeRightField* pTradeRight);

    ///客户品种白名单响应
    virtual void OnRspAccountCommList(const DstarApiAccountCommListField* pAccountCommList);

    ///市场状态信息响应
    virtual void OnRspTrdExchangeState(const DstarApiTrdExchangeStateField *pTrdExchangeState);
    
    ///资金快照响应
    virtual void OnRspFund(const DstarApiFundField *pFund);

    ///昨持仓快照响应
    virtual void OnRspPrePosition(const DstarApiPrePositionField *pPrePosition);
    
    ///实时持仓快照响应
    virtual void OnRspPosition(const DstarApiPositionField *pPosition);

    ///委托响应
    virtual void OnRspOrder(const DstarApiOrderField *pOrder);
    
    ///报价响应
    virtual void OnRspOffer(const DstarApiOfferField *pOffer);
    
    ///成交响应
    virtual void OnRspMatch(const DstarApiMatchField *pMatch);

    ///出入金响应
    virtual void OnRspCashInOut(const DstarApiCashInOutField *pCashInOut);
    
    ///API准备就绪,用户只有收到此就绪通知时才能进行后续的操作
    ///@param nSerialId 数据快照对应的流号,如果依据快照数据计算,使用该流号之后的数据
    virtual void OnApiReady(const DstarApiSerialIdType nSerialId);
    
    ///UDP认证请求响应,错误码为0说明认证成功。
    virtual void OnRspUdpAuth(const DstarApiRspUdpAuthField *pRspUdpAuth);
    
    ///报单应答(报单应答中错误编码字段不为0的情况下将不会再反馈其他通知)
    virtual void OnRspOrderInsert(const DstarApiRspOrderInsertField *pOrderInsert);
    
    ///报价应答(报价应答中错误编码字段不为0的情况下将不会再反馈其他通知)
    virtual void OnRspOfferInsert(const DstarApiRspOfferInsertField *pOfferInsert);
    
    ///撤单应答(撤单应答中错误编码字段不为0的情况下将不会再反馈其他通知)
    virtual void OnRspOrderDelete(const DstarApiRspOrderDeleteField *pOrderDelete);
    
    ///最新请求号应答
    virtual void OnRspLastReqId(const DstaApiRspLastReqIdField *pLastReqId);
    
    // 密码修改通知（收到此通知后API会主动断开连接）
    virtual void OnRtnPwdMod(const DstarApiPwdModField *pPwdModField);
    
    ///委托通知 (撤单失败时返回委托通知,委托状态不变,包含撤单失败的错误码)
    virtual void OnRtnOrder(const DstarApiOrderField *pOrder);

    ///成交通知
    virtual void OnRtnMatch(const DstarApiMatchField *pMatch);
    
    ///出入金通知
    virtual void OnRtnCashInOut(const DstarApiCashInOutField *pCashInOut);
    
    ///报价通知 (撤单失败时返回报价通知,报价状态不变,包含撤单失败的错误码)
    virtual void OnRtnOffer(const DstarApiOfferField *pOffer);
    
    ///Inquiry Notice
    virtual void OnRtnEnquiry(const DstarApiEnquiryField *pEnquiry);

    ///市场状态通知
    virtual void OnRtnTrdExchangeState(const DstarApiTrdExchangeStateField *pTrdExchangeState);
    
    ///浮盈通知
    virtual void OnRtnPosiProfit(const DstarApiPosiProfitField *pPosiProfit);
    
    ///席位信息通知
    virtual void OnRtnSeat(const DstarApiSeatField* pSeat);

    ///交易权限通知 (当添加或者修改某个品种交易权限时，会推送此通知)
    virtual void OnRtnTradeRight(const DstarApiTradeRightField* pTradeRight);

    ///交易权限删除通知 (当删除某个品种交易权限时，会推送此通知)
    virtual void OnRtnTradeRightDel(const DstarApiTradeRightDelField* pTradeRightDel);

    ///Real-time position query response (when bLast is true, pPosition is NULL)
    virtual void OnRspQryPosition(const DstarApiPositionField *pPosition, bool bLast);

    ///资金查询响应
    virtual void OnRspQryFund(const DstarApiFundField *pFund);
    
public:
    

    //udp认证
    void UdpAuth();
    
    //提交采集信息
    int SubmitSystemInfo();
    
    // 报单
    void ReqOrderInsert(const DstarApiReqOrderInsertField *pOrder);
    
    // 报价
    void ReqOfferInsert(const DstarApiReqOfferInsertField *pOffer);

    // 新报价
    void ReqOfferInsertNew(const DstarApiReqOfferInsertNewField *pOffer);
    
    // 撤单
    void ReqOrderDelete(const DstarApiReqOrderDeleteField *pOrder);
    
    // 组合报单
    void ReqCmbOrderInsert(const DstarApiReqCmbOrderInsertField *pCmbOrder);

    void ReqQryFund();

    void ReqQryPosition();

public:
    IDstarTradeApi         *m_pApi;                 
    DstarApiRspLoginField   m_LoginInfo;
    
    char                    m_FrontIp[21];
    int                     m_FrontPort;
    
    DstarApiReqLoginField   m_LoginReq;

    bool                    m_bReady;
    bool                    m_bUdpAuth;

private:
    std::vector<DstarApiPositionField> listPositions;
    std::vector<DstarApiContractField> listContractIndex;
};

#endif
