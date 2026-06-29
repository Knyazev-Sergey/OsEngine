///@system  Dstar V10
///@file    DstarQuoteApi.h
///@author  ln 2021-10-08

#ifndef DSTARQUOTEAPI_H
#define DSTARQUOTEAPI_H


#include "DstarQuoteApiStruct.h"
#include "DstarQuoteApiDataType.h"

#if defined WIN32 || defined _WIN32
#ifdef LIBDSTARQUOTEAPI_EXPORTS
#define DSTARQUOTEAPI_EXPORT __declspec(dllexport)
#else
#define DSTARQUOTEAPI_EXPORT __declspec(dllimport)
#endif
#else
#define DSTARQUOTEAPI_EXPORT
#endif
//DstarQuoteApi.h
//文件定义了DstarQuoteApi提供给开发者的对外接口和函数, DstarQuoteSpi定义了回调通知接口。
class DstarQuoteSpi
{
public:
        /**
        * @brief       通知用户Api已经就绪，可以进行业务操作; 
        * @details     只有用户回调收到此就绪通知后，才能进行后续的各种行情数据查询操作,
        *                       此回调函数是Api能否正常工作的标志。
        * @attention   就绪后才可以进行后续正常操作
        */
        virtual void OnApiReady() = 0;
        /**
        * @brief       通知用户Api和行情服务失去连接;
        * @details     在Api使用过程中主动与被动和服务器服务失去连接后，都会触发此回调通知用户与服务器的连接已经断开。
        * @param[in]   ReasonCode 断开原因代码，具体原因请参见DstarQuoteApiError.h头文件。
        */
        virtual void OnDisconnect(int ReasonCode) = 0;
        /**
        * @brief       返回错误; 
        * @details     此回调接口用来通知用户出现错误的错误码;
        * @param[in]   ErrorCode 发生错误原因代码，具体原因请参见DstarQuoteApiError.h头文件。
        */
        virtual void OnError(int ErrorCode) = 0;
        /**
        * @brief       返回品种信息;
        * @details     此回调接口返回所有品种的品种信息;
        * @param[in]   buf    指向返回的品种信息; 
        * @param[in]   IsLast 查询到最后一个品种时，由false置为true;  
        * @attention  请勿修改和删除buf所指示的数据；函数调用结束，参数不再有效。
        */
        virtual void OnRspCommodity(const DstarQuoteApiCommodityData* buf, bool IsLast) = 0;
        /**
        * @brief       返回合约信息;
        * @details     此回调接口返回所有合约的合约信息;
        * @param[in]   buf 指向返回的合约信息; 
        * @param[in]   IsLast 查询到最后一个合约时由false置为true;  
        * @attention  请勿修改和删除buf所指示的数据; 函数调用结束，参数不再有效。
        */
        virtual void OnRspContract(const char* buf, bool IsLast) = 0;
        /**
        * @brief       Return to the full market data for the subscription contract;
        * @details     This callback interface is used to return the full text of the subscribed market data, which is the market information at the current time.
        *                       If market conditions change, the latest full market information will still be returned using this API.
        * @param[in]   Info The link leads to the full quote.
        * @attention  Do not modify or delete the data indicated by Info; the parameters are no longer valid after the function call ends.
        */
        virtual void OnRtnQuote(const DstarApiQuoteData* Info) = 0;
};

//DstarQuoteApi 对外功能接口。包含了用户可以调用的功能函数。
//class DstarQuoteApi
class DstarQuoteApi
{
public:
        /**
        * @brief       设置Api的回调接口对象;
        * @details     系统对Api的通知将通过设置的回调对象通知给使用者;
        *                       DstarQuoteSpi是Api的回调接口，用户需要继承实现此接口类对象来完成用户需要的功能。
        *                       如果用户没有设置回调接口，则Api不会向用户返回任何有用的信息。
        *                       IDstarQuoteSpi类的详细内容请查看DstarQuoteApi.h 文件。
        * @param[in]   ApiNotify是实现了DstarQuoteSpi接口的对象实例指针。
        * @retval      0    为正确返回
        * @retval      非0  为发生错误，返回错误码; 同时，OnError返回错误码。
        */
        virtual int SetSpi(DstarQuoteSpi* ApiNotify) = 0;
        /**
        * @brief       设置Api要连接行情服务器的Ip地址和Port端口号;
        * @param[in]   Ip 地址; Port 端口号;
        * @retval      true   为正确返回;
        * @retval      false  为发生错误，返回错误码; 同时，OnError返回错误码。
        */
        virtual bool SetHostAddress(const char* Ip, unsigned short Port) = 0;        
        /**
        * @brief       设置Api的工作日志文件路径;
        * @details     此路径必须可用;
        * @param[in]   Path 路径;
        * @Note        发生错误时， OnError返回错误码。
        */
        virtual void SetApiLogPath(const DstarApiPathType Path) = 0;
        /**
        * @brief        线程绑定CPU核心
        * @details      设置接收数据的线程工作在哪个Cpu核心,ID号必须是有效的Cpu Id号
        * @param[in]    RecvNoticeDataCpuId 接收数据线程所在的CPU Id号
        */
        virtual void SetCpuId(DstarApiCpuIdType RecvNoticeDataCpuId) = 0;
        /**
        * @brief       设置Api与行情服务器断连后是否自动重新连接，默认不自动连接;
        * @details     如果设置了自动重连（IsAuto=1），当Api与行情服务器发生断连之后Api将自动重新向服务器
        *                       发出连接服务器申请。成功连接到服务器后，将重新订阅用户在断连之前订阅的合约。
        *                       如果不设置自动重连，用户需要自行重新设定要订阅那些合约。 
        * @param[in]   IsAuto 是否自动重连标志位。
        */
        virtual void SetAutoRelogin(bool IsAuto) = 0;  
        /**
        * @brief       发起登录请求。Api连接行情服务发起登录认证,同时完成品种查询和合约查询;
        * @details     在使用函数函数前需要设置好回调接口;
        *                       连接建立并完成全部查询任务后，通过回调OnApiReady()告知用户。
        *                       指示用户Api初始化完成，可以进行后续的操作了。
        * @retval      0    登录成功
        * @retval      非0  错误码; 同时，OnError返回错误码。
        */
        virtual int Start() = 0;
        /**
        * @brief      品种查询;
        * @details    函数向服务器请求查询品种，通过OnRspCommodity()回调返回所有品种的信息;
        * @retval     0   请求成功
        * @retval     非0 错误码
        */
        virtual int QryCommodity() = 0;
        /**
        * @brief      合约品种;
        * @details    函数向服务器请求查询品种，通过OnRspContract()回调返回所有合约的信息;
        * @retval     0   请求成功
        * @retval     非0 错误码
        */
        virtual int QryContract() = 0;
        /**
	    * @brief       订阅指定合约的行情;
        * @details     函数向服务器请求订阅Contract描述合约的行情信息，行情订阅成功后服务器将持续向用户推送行情信息，
        *		        直到用户退订行情信息或者断开与服务器的通信。
        * @param[in]   Contract 指定合约;
        * @retval      0   请求成功
        * @retval      非0 错误码
        */
        virtual int Subscribe(const char* Contract) = 0;
        /**
        * @brief       Unsubscribe from the market data for the specified contract
        * @details     The function requests the server to cancel the market data subscription for the contract described in the contract. After the market data subscription is successfully canceled, the server will not provide any market data to the user.
        * @param[in]   Contract Specify the contract.
        * @retval      0   Request successful
        * @retval      Non-zero error code
        */
        virtual int UnSubscribe(const char* Contract) = 0;
};

//-----------------------------DstarQuoteApi导出函数------------------------------------


#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

/**
* @brief       创建DstarQuoteApi的接口对象。
* @details     创建整个行情API的接口
* @retval      NULL    创建失败。
* @retval      !NULL   实现了DstarQuoteApi接口的对象指针。
*/
DSTARQUOTEAPI_EXPORT DstarQuoteApi * CreateDstarQuoteApi();
/**
* @brief       销毁通过CreateDstarQuoteApi函数创建的DstarQuoteApi对象。
* @param[in]   ApiObj是DstarQuoteApi对象实例指针。
*/
DSTARQUOTEAPI_EXPORT void  FreeDstarQuoteApi(DstarQuoteApi* ApiObj);
/**
* @brief       获取DstarQuoteApi的版本信息
*/
DSTARQUOTEAPI_EXPORT const char* GetApiVersion();

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* DSTARQUOTEAPI_H */

