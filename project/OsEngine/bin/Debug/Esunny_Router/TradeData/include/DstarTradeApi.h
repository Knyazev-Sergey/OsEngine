///@system  Dstar V10
///@file    DstarTradeApi.h
///@author  Hao Lin 2020-08-20


#ifndef DSTARTRADEAPI_H
#define DSTARTRADEAPI_H

#include "DstarTradeApiError.h"
#include "DstarTradeApiDataType.h"
#include "DstarTradeApiStruct.h"
#include "string"

#if defined WIN32 || defined _WIN32
#ifdef LIBDSTARTRADEAPI_EXPORTS
#define DSTARTRADEAPI_EXPORT __declspec(dllexport)
#else
#define DSTARTRADEAPI_EXPORT __declspec(dllimport)
#endif
#else
#define DSTARTRADEAPI_EXPORT
#endif

///@brief DstarTradeApi的回调基类
class IDstarTradeSpi
{
public:
    ///客户端与接口通信连接断开
    virtual void OnFrontDisconnected() = 0;

    ///错误应答
    virtual void OnRspError(DstarApiErrorCodeType nErrorCode) = 0;

    ///登录请求响应,错误码为0说明用户登录成功。
    virtual void OnRspUserLogin(const DstarApiRspLoginField *pRspUserLogin) = 0;
    
    // 密码修改应答
    virtual void OnRspPwdMod(const DstarApiRspPwdModField *pRspPwdModField) = 0;
    
    ///Отправить информационный ответ
    virtual void OnRspSubmitInfo(const DstarApiRspSubmitInfoField *pRspSubmitInfo) = 0;

    ///Ответ на информацию о контракте
    virtual void OnRspContract(const DstarApiContractField *pContract) = 0;

    ///组合合约信息响应
    virtual void OnRspCmbContract(const DstarApiCmbContractField *pCmbContract) = 0;

    ///席位信息响应
    virtual void OnRspSeat(const DstarApiSeatField* pSeat) = 0;

    ///手续费参数响应
    virtual void OnRspTrdFeeParam(const DstarApiTrdFeeParamField* pFeeParam) = 0;

    ///保证金参数响应
    virtual void OnRspTrdMarParam(const DstarApiTrdMarParamField* pMarParam) = 0;

    ///交易权限响应
    virtual void OnRspTradeRight(const DstarApiTradeRightField* pTradeRight) = 0;

    ///客户品种白名单响应
    virtual void OnRspAccountCommList(const DstarApiAccountCommListField* pAccountCommList) = 0;

    ///市场状态信息响应
    virtual void OnRspTrdExchangeState(const DstarApiTrdExchangeStateField *pTrdExchangeState) = 0;
    
    ///Ответ на краткий обзор фонда
    virtual void OnRspFund(const DstarApiFundField *pFund) = 0;

    ///昨持仓快照响应
    virtual void OnRspPrePosition(const DstarApiPrePositionField *pPrePosition) = 0;
    
    ///实时持仓快照响应
    virtual void OnRspPosition(const DstarApiPositionField *pPosition) = 0;

    ///委托响应
    virtual void OnRspOrder(const DstarApiOrderField *pOrder) = 0;
    
    ///报价响应
    virtual void OnRspOffer(const DstarApiOfferField *pOffer) = 0;
    
    ///成交响应
    virtual void OnRspMatch(const DstarApiMatchField *pMatch) = 0;

    ///出入金响应
    virtual void OnRspCashInOut(const DstarApiCashInOutField *pCashInOut) = 0;
    
    ///APIГотово; пользователи смогут продолжить выполнение дальнейших операций только после получения этого уведомления о готовности.
    ///@param nSerialId Номер потока, соответствующий снимку данных; если он рассчитывается на основе данных снимка, будут использоваться данные, следующие за этим номером потока.
    virtual void OnApiReady(const DstarApiSerialIdType nSerialId) = 0;
    
    ///Ответ на запрос аутентификации по протоколу UDP с кодом ошибки 0 указывает на успешную аутентификацию.
    virtual void OnRspUdpAuth(const DstarApiRspUdpAuthField *pRspUdpAuth) = 0;
    
    ///Ответ на заказ
    virtual void OnRspOrderInsert(const DstarApiRspOrderInsertField *pOrderInsert) = 0;
    
    ///ответ на цитату
    virtual void OnRspOfferInsert(const DstarApiRspOfferInsertField *pOfferInsert) = 0;
    
    ///Ответ на отмену
    virtual void OnRspOrderDelete(const DstarApiRspOrderDeleteField *pOrderDelete) = 0;
    
    ///Последний ответ на запрос по номеру
    virtual void OnRspLastReqId(const DstaApiRspLastReqIdField *pLastReqId) = 0;
    
    // Уведомление об изменении пароля (API автоматически отключится после получения этого уведомления).
    virtual void OnRtnPwdMod(const DstarApiPwdModField *pPwdModField) = 0;
    
    ///Уведомление о заказе (возвращается в случае неудачной отмены заказа; статус заказа остается неизменным, включает код ошибки, указывающий на неудачную отмену).
    virtual void OnRtnOrder(const DstarApiOrderField *pOrder) = 0;

    ///Уведомление о транзакции
    virtual void OnRtnMatch(const DstarApiMatchField *pMatch) = 0;
    
    ///Уведомления о пополнении и снятии средств
    virtual void OnRtnCashInOut(const DstarApiCashInOutField *pCashInOut) = 0;
    
    ///Уведомление о предложении цены (Уведомление о предложении цены возвращается в случае неудачной отмены заказа; статус предложения остается неизменным и включает код ошибки, указывающий на неудачную отмену заказа).
    virtual void OnRtnOffer(const DstarApiOfferField *pOffer) = 0;
    
    ///Уведомление о запросе
    virtual void OnRtnEnquiry(const DstarApiEnquiryField *pEnquiry) = 0;

    ///Уведомления о статусе рынка
    virtual void OnRtnTrdExchangeState(const DstarApiTrdExchangeStateField *pTrdExchangeState) = 0;
    
    ///Уведомление о плавающей прибыли
    virtual void OnRtnPosiProfit(const DstarApiPosiProfitField *pPosiProfit) = 0;
    
    ///Уведомление о месте
    virtual void OnRtnSeat(const DstarApiSeatField* pSeat) = 0;

    ///Уведомление о разрешении на совершение сделок (Это уведомление будет отправлено при добавлении или изменении разрешений на совершение сделок с конкретным инструментом.)
    virtual void OnRtnTradeRight(const DstarApiTradeRightField* pTradeRight) = 0;

    ///Уведомление об отзыве разрешения на торговлю (Это уведомление будет отправлено при отзыве разрешения на торговлю конкретным инструментом.)
    virtual void OnRtnTradeRightDel(const DstarApiTradeRightDelField* pTradeRightDel) = 0;

    ///Ответ на запрос о местоположении в реальном времени (pPosition может быть NULL, если bLast равно true).
    virtual void OnRspQryPosition(const DstarApiPositionField *pPosition, bool bLast) = 0;

    ///Ответ на запрос фонда
    virtual void OnRspQryFund(const DstarApiFundField *pFund) = 0;
};

///@brief DstarTradeApi开放函数接口
class DSTARTRADEAPI_EXPORT IDstarTradeApi {
public:
    ///注册回调接口
    ///@param pSpi 派生自回调接口类的实例
    virtual void RegisterSpi(IDstarTradeSpi *pSpi) = 0;

    ///注册接口地址
    ///@param pIp IP地址
    ///@param nPort 端口号
    virtual void RegisterFrontAddress(DstarApiIpType pIp, DstarApiPortType nPort) = 0;

    ///Sets the directory for storing API transaction data and working logs
    //@param pPath directory. The directory must be available.
    virtual void SetApiLogPath(DstarApiPathType pPath) = 0;

    ///设置用户信息
    ///@param pLoginInfo User information
    virtual void SetLoginInfo(DstarApiReqLoginField *pLoginInfo) = 0;
    
    ///Bind CPU Information
    //@param nRecvNoticeDataCpuId Binds the CPU Id of the data receiving thread, optional. -1: Do not bind
    //@param nLogCpuId Binds the CPU Id of the log thread, optional. -1: Do not bind, no transaction data logs are recorded, and the log thread is not started.
    virtual void SetCpuId(DstarApiCpuIdType nRecvNoticeDataCpuId, DstarApiCpuIdType nLogCpuId) = 0;
    
    /// Sets the notification stream subscription position
    // @param nStartId Notification stream subscription position -1: Subscribe from the latest 0: Subscribe from the beginning >0: Subscribe from the specified position
    virtual void SetSubscribeStartId(DstarApiNoticeSubIdType nStartId) = 0;

    ///设置实时数据过滤器
    ///@param nFilter Флаги фильтрации: -1: Отфильтровывать все данные в реальном времени после OnApiReady (в этом случае рабочий поток API не будет работать на полную мощность, и интерфейс SetRunMode не будет действовать). 0: Не фильтровать данные в реальном времени (значение по умолчанию).
    virtual void SetRealTimeDataFilter(DstarApiRealTimeDataFilterType nFilter) = 0;

    ///Установите режим работы. По умолчанию используется режим полной нагрузки.
    ///@param nMode Режим работы 0: Режим полной нагрузки (по умолчанию) -1: Режим неполной нагрузки (в этом режиме ускорение сетевой карты не работает, и система будет работать с полной нагрузкой)
    virtual void SetRunMode(DstarApiRunModeType nMode) = 0;
    
    ///采集系统信息
    ///@remark 该函数需要系统权限。
    ///Linux用户: 如果不使用root运行,需要单独设置权限(dmidecode/lshw; /sys/class/dmi/id/product_serial; /sys/devices/virtual/dmi/id/product_serial)
    ///@param pSystemInfo 接收系统信息,内存由客户申请
    ///@param nLen pSystemInfo的长度
    ///@param nAuthKeyVersion 返回密钥版本
    ///@return 错误码 0:成功,-1:获取Ip失败,-2:获取Mac失败,-3:获取设备名称失败,-4:获取操作系统版本失败,-5:获取硬盘序列号失败,
    ///                    -6:获取Cpu序列号失败,-7:获取Bios序列号失败,-8:获取系统分盘信息失败,-9:获取MacOS设备序列号失败
    virtual int GetSystemInfo(char* pSystemInfo, int *nLen, unsigned int *nAuthKeyVersion) = 0;
    
    ///设置上报信息
    ///@param pSubmitInfo Представленная информация включает в себя системные данные и данные для входа пользователей в систему.
    virtual void SetSubmitInfo(DstarApiSubmitInfoField *pSubmitInfo) = 0;

    ///Установите исходную информацию запроса
    ///@param pInitQryInfo Инициализировать информацию запроса
    virtual void SetInitQryInfo(DstarApiInitQryInfoField *pInitQryInfo) = 0;
    
    ///初始化
    ///@remark 初始化运行环境,只有调用后,API才开始工作
    ///Linux用户: 如果不使用root运行,需要单独设置权限(dmidecode/lshw; /sys/class/dmi/id/product_serial; /sys/devices/virtual/dmi/id/product_serial)
    ///@return Коды ошибок: 0: Успех, -3: Соединение установлено, -4: Не удалось создать сокет, -5: Соединение не удалось.
    // Коды ошибок сбора системных данных: -11: Не удалось получить IP-адрес, -12: Не удалось получить MAC-адрес, -13: Не удалось получить имя устройства, -14: Не удалось получить версию операционной системы, -15: Не удалось получить серийный номер жесткого диска.
    // -16: Не удалось получить серийный номер процессора, -17: Не удалось получить серийный номер BIOS, -18: Не удалось получить информацию о системном разделе, -19: Не удалось получить серийный номер устройства macOS.
    virtual int Init() = 0;
    
    ///查询最新客户请求号
    ///@remark 可以用来检测报撤单丢包情况,查询间隔不小于5s
    ///@return 错误码 0:成功,-1:Api未就绪,-2:频率超限, -3:网络连接已断开
    virtual int ReqLastClientReqId() = 0;
    
    /// 密码修改请求
    /// \param pPwdModField 请求域
    /// \return  错误码 0:成功, -1:Api未就绪,-2:网络连接已断开
    virtual int ReqPwdMod(const DstarApiReqPwdModField *pReqPwdModField) = 0;
    
    ///报单请求
    ///@param pOrder 输入报单
    ///@return Код ошибки 0: Успех, -1: API не готов, -2: Потеряно сетевое соединение
    virtual int ReqOrderInsert(const DstarApiReqOrderInsertField *pOrder) = 0;
    
    ///报价请求
    ///@param pOffer 输入报价
    ///@return 错误码 0:成功,-1:Api未就绪,-2:网络连接已断开
    virtual int ReqOfferInsert(const DstarApiReqOfferInsertField *pOffer) = 0;

    ///新报价请求
    ///@param pOffer 输入报价
    ///@return 错误码 0:成功,-1:Api未就绪,-2:网络连接已断开
    virtual int ReqOfferInsertNew(const DstarApiReqOfferInsertNewField *pOffer) = 0;
    
    ///撤单请求
    ///@param pOrder 输入报单
    ///@return 错误码 0:成功,-1:Api未就绪,-2:网络连接已断开
    virtual int ReqOrderDelete(const DstarApiReqOrderDeleteField *pOrder) = 0;
    
    ///组合报单请求
    ///@param pCmbOrder 输入组合报单
    ///@return 错误码 0:成功,-1:Api未就绪,-2:网络连接已断开
    virtual int ReqCmbOrderInsert(const DstarApiReqCmbOrderInsertField *pCmbOrder) = 0;

    ///资金查询请求
    ///@return 错误码 0:成功,-1:Api未就绪,-2:网络连接已断开,-3:频率超限(最小间隔1s),-4:上次查询未结束
    virtual int ReqQryFund() = 0;

    ///持仓查询请求
    ///@return 错误码 0:成功,-1:Api未就绪,-2:网络连接已断开,-3:频率超限(最小间隔1s),-4:上次查询未结束
    virtual int ReqQryPosition() = 0;

    ///获取API的版本信息
    ///@retrun 获取到的版本号
    virtual const char *GetApiVersion() = 0;        
};


extern "C" {

///创建Api实例 
///@return Api实例
DSTARTRADEAPI_EXPORT IDstarTradeApi *CreateDstarTradeApi();

///释放Api实例 
///@remark !!!不要在任何SPI回调中调用此函数
///@param pApiObj 需要释放的Api实例
DSTARTRADEAPI_EXPORT void FreeDstarTradeApi(IDstarTradeApi *pApiObj);

}


#endif // DSTARTRADEAPI_H
