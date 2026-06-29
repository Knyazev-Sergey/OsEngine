///@system  Dstar V10
///@file    DstarTradeApiStruct.h
///@author  Hao Lin 2020-08-20

#ifndef DSTARTRADEAPISTRUCT_H
#define DSTARTRADEAPISTRUCT_H

#include "DstarTradeApiDataType.h"


#pragma pack(push, 1)

// з™»еЅ•иЇ·ж±‚
typedef struct DstarApiReqLoginField
{
    DstarApiAccountNoType                  AccountNo;                   // A cc ou nt No
    DstarApiPasswdType                     Password;                    // P as sw or d
    DstarApiAppIdType                      AppId;                       // A pp Id
    DstarApiLicenseNoType                  LicenseNo;                   // L ic en se No
}DstarApiReqLoginField;

// з™»еЅ•еє”з­”
typedef struct DstarApiRspLoginField
{
    DstarApiAccountIndexType               AccountIndex;                // A cc ou nt In de x
    DstarApiAccountNoType                  AccountNo;                   // A cc ou nt No
    DstarApiDateType                       TradeDate;                   // T ra de Da te
    DstarApiAuthCodeType                   UdpAuthCode;                 // U dp Au th Co de
    DstarApiErrorCodeType                  ErrorCode;                   // E rr or Co de
    DstarApiStartTimeType                  StartTime;                   // S ta rt Ti me
    DstarApiStartModeType                  StartMode;                   // S ta rt Mo de
    DstarApiYesNoType                      FloatFlag;                   // F lo at Fl ag
}DstarApiRspLoginField;


// дёЉжЉҐдїЎжЃЇ
typedef struct DstarApiSubmitInfoField
{
    DstarApiAccountNoType                  AccountNo;                   // A cc ou nt No
    DstarApiAuthTypeType                   AuthType;                    // A ut hT yp e
    DstarApiAuthKeyVersion                 AuthKeyVersion;              // A ut hK ey Ve rs io n
    DstarApiSystemInfoType                 SystemInfo;                  // S ys te mI nf o
    DstarApiIpType                         ClientLoginIp;               // C li en tL og in Ip
    DstarApiPortType                       ClientLoginPort;             // C li en tL og in Po rt
    DstarApiDateTimeType                   ClientLoginDateTime;         // C li en tL og in Da te Ti me
    DstarApiAppIdType                      ClientAppId;                 // app id
    DstarApiLicenseNoType                  LicenseNo;                   // L ic en se No
}DstarApiSubmitInfoField;

// дёЉжЉҐдїЎжЃЇеє”з­”
typedef struct DstarApiRspSubmitInfoField
{
    DstarApiAccountNoType                  AccountNo;                   // A cc ou nt No
    DstarApiErrorCodeType                  ErrorCode;                   // E rr or Co de
}DstarApiRspSubmitInfoField;

// е€ќе§‹еЊ–ж•°жЌ®жџҐиЇў
typedef struct DstarApiInitQryInfoField
{
    DstarApiYesNoType                      ContractInitQryFlag;         // C on tr ac tI ni tQ ry Fl ag
    DstarApiYesNoType                      CmbContractInitQryFlag;      // C mb Co nt ra ct In it Qr yF la g
    DstarApiYesNoType                      SeatInitQryFlag;             // S ea tI ni tQ ry Fl ag
    DstarApiYesNoType                      TrdFeeInitQryFlag;           // T rd Fe eI ni tQ ry Fl ag
    DstarApiYesNoType                      TrdMarInitQryFlag;           // T rd Ma rI ni tQ ry Fl ag
    DstarApiYesNoType                      TrdRightInitQryFlag;         // T rd Ri gh tI ni tQ ry Fl ag
    DstarApiYesNoType                      AccountCommListInitQryFlag;  // A cc ou nt Co mm Li st In it Qr yF la g
    DstarApiYesNoType                      TrdExchangeStateInitQryFlag; // T rd Ex ch an ge St at eI ni tQ ry Fl ag
    DstarApiYesNoType                      PrePositionInitQryFlag;      // P re Po si ti on In it Qr yF la g
    DstarApiYesNoType                      OrderInitQryFlag;            // O rd er In it Qr yF la g
    DstarApiYesNoType                      OfferInitQryFlag;            // O ff er In it Qr yF la g
    DstarApiYesNoType                      MatchInitQryFlag;            // M at ch In it Qr yF la g
    DstarApiYesNoType                      CashInOutInitQryFlag;        // C as hI nO ut In it Qr yF la g

}DstarApiInitQryInfoField;

// еЇ†з Ѓдї®ж”№иЇ·ж±‚
typedef struct DstarApiReqPwdModField
{
    DstarApiPasswdType                     Passwd;                      // P as sw d
    DstarApiPasswdType                     OldPasswd;                   // O ld Pa ss wd
}DstarApiReqPwdModField;

// еЇ†з Ѓдї®ж”№еє”з­”
typedef struct DstarApiRspPwdModField
{
    DstarApiAccountNoType                  AccountNo;                   // A cc ou nt No
    DstarApiErrorCodeType                  ErrorCode;                   // E rr or Co de
}DstarApiRspPwdModField;

// еЇ†з Ѓдї®ж”№йЂљзџҐ
typedef struct DstarApiPwdModField
{
    DstarApiAccountNoType                  AccountNo;                   // A cc ou nt No
}DstarApiPwdModField;


// UDPи®¤иЇЃеє”з­”
typedef struct DstarApiRspUdpAuthField
{
    DstarApiAccountIndexType               AccountIndex;                // A cc ou nt In de x
    DstarApiAuthCodeType                   UdpAuthCode;                 // U dp Au th Co de
    DstarApiReqIdModeType                  ReqIdMode;                   // R eq Id Mo de
    DstarApiErrorCodeType                  ErrorCode;                   // E rr or Co de
}DstarApiRspUdpAuthField;


// её­дЅЌ
typedef struct DstarApiSeatField
{
    DstarApiSeatIndexType                  SeatIndex;                   // S ea tI nd ex
    DstarApiSeatNoType                     SeatNo;                      // S ea tN o
    DstarApiExchangeType                   Exchange;                    // E xc ha ng e
    DstarApiSeatStateType                  SeatState;                   // S ea tS ta te
    DstarApiIPv4IpType                     Ip;                          // I p
}DstarApiSeatField;


// еђ€зє¦ж•°жЌ®
typedef struct DstarApiContractField
{
    DstarApiExchangeType                   ExchangeId;                  // E xc ha ng eI d
    DstarApiCommodityType                  CommodityType;               // C om mo di ty Ty pe
    DstarApiContractIndexType              ContractIndex;               // C on tr ac tI nd ex
    DstarApiContractSizeType               ContractSize;                // C on tr ac tS iz e
    DstarApiContractNoType                 ContractNo;                  // C on tr ac tN o
    DstarApiContractTickSizeType           ContractTickSize;            // C on tr ac tT ic kS iz e
    DstarApiPriceType                      PreSettlePrice;              // P re Se tt le Pr ic e
    DstarApiDateType                       ExpDate;                     // E xp Da te
    DstarApiPriceType                      LimitUpPrice;                // L im it Up Pr ic e
    DstarApiPriceType                      LimitDownPrice;              // L im it Do wn Pr ic e
}DstarApiContractField;

// з»„еђ€еђ€зє¦ж•°жЌ®
typedef struct DstarApiCmbContractField
{
    DstarApiExchangeType                   ExchangeId;                  // E xc ha ng eI d
    DstarApiCommodityType                  CommodityType;               // C om mo di ty Ty pe
    DstarApiContractIndexType              ContractIndex1;              // C on tr ac tI nd ex1
    DstarApiContractNoType                 ContractNo1;                 // C on tr ac tN o1
    DstarApiContractIndexType              ContractIndex2;              // C on tr ac tI nd ex2
    DstarApiContractNoType                 ContractNo2;                 // C on tr ac tN o2
}DstarApiCmbContractField;


// е§”ж‰ж•°жЌ®
typedef struct DstarApiOrderField
{
    DstarApiDirectType                     Direct;                      // D ir ec t
    DstarApiOffsetType                     Offset;                      // O ff se t
    DstarApiHedgeType                      Hedge;                       // жЉ•жњєеҐ—дїќ
    DstarApiValidTypeType                  ValidType;                   // V al id Ty pe
    DstarApiPriceType                      OrderPrice;                  // O rd er Pr ic e
    DstarApiQuantityType                   OrderQty;                    // O rd er Qt y
    DstarApiQuantityType                   MinQty;                      // M in Qt y
    DstarApiQuantityType                   MatchQty;                    // M at ch Qt y
    DstarApiErrorCodeType                  ErrCode;                     // E rr Co de
    DstarApiSerialIdType                   SerialId;                    // S er ia lI d
    DstarApiOrderIdType                    OrderId;                     // O rd er Id
    DstarApiFundType                       FrozenMargin;                // F ro ze nM ar gi n
    DstarApiFundType                       Margin;                      // дїќиЇЃй‡‘
    DstarApiFundType                       Fee;                         // F ee
    DstarApiAccountNoType                  AccountNo;                   // A cc ou nt No
    DstarApiOrderLocalNoType               OrderLocalNo;                // O rd er Lo ca lN o
    DstarApiSystemNoType                   SystemNo;                    // S ys te mN o
    DstarApiDateTimeType                   UpdateTime;                  // U pd at eT im e
    DstarApiDateTimeType                   ExchInsertTime;              // E xc hI ns er tT im e
    DstarApiReferenceType                  Reference;                   // R ef er en ce
    DstarApiContractNoType                 ContractNo1;                 // C on tr ac tN o1
    DstarApiOrderTypeType                  OrderType;                   // O rd er Ty pe
    DstarApiOrderStateType                 OrderState;                  // O rd er St at e
    DstarApiSeatIndexType                  SeatIndex;                   // S ea tI nd ex
    DstarApiSeatNoType                     UpSeatNo;                    // U pS ea tN o
    DstarApiContractNoType                 ContractNo2;                 // C on tr ac tN o2
    DstarApiOrderIdType                    CmbId;                       // C mb Id
    DstarApiFundType                       OrderFee;                    // з”іжЉҐиґ№
}DstarApiOrderField;


// жЉҐеЌ•еє”з­”
typedef struct DstarApiRspOrderInsertField
{
    DstarApiSeatIndexType                  SeatIndex;                   // S ea tI nd ex
    DstarApiAccountNoType                  AccountNo;                   // A cc ou nt No
    DstarApiClientReqId                    ClientReqId;                 // C li en tR eq Id
    DstarApiReferenceType                  Reference;                   // R ef er en ce
    DstarApiClientReqId                    MaxClientReqId;              // M ax Cl ie nt Re qI d
    DstarApiOrderIdType                    OrderId;                     // O rd er Id
    DstarApiDateTimeType                   InsertTime;                  // I ns er tT im e
    DstarApiErrorCodeType                  ErrCode;                     // E rr Co de
}DstarApiRspOrderInsertField;


// ж’¤еЌ•еє”з­”
typedef DstarApiRspOrderInsertField        DstarApiRspOrderDeleteField;


// жЉҐд»·еє”з­”
typedef DstarApiRspOrderInsertField        DstarApiRspOfferInsertField;


// жЉҐд»·йЂљзџҐ
typedef struct DstarApiOfferField
{
    DstarApiOffsetType                     BuyOffset;                   // B uy Of fs et
    DstarApiOffsetType                     SellOffset;                  // S el lO ff se t
    union
    {
        DstarApiQuantityType               OrderQty;                    // O rd er Qt y
        DstarApiQuantityType               BuyOrderQty;                 // B uy Or de rQ ty
    };
    DstarApiPriceType                      BuyPrice;                    // B uy Pr ic e
    DstarApiPriceType                      SellPrice;                   // S el lP ri ce
    DstarApiQuantityType                   BuyMatchQty;                 // B uy Ma tc hQ ty
    DstarApiQuantityType                   SellMatchQty;                // S el lM at ch Qt y
    DstarApiAccountNoType                  AccountNo;                   // A cc ou nt No
    DstarApiOrderLocalNoType               OrderLocalNo;                // O rd er Lo ca lN o
    DstarApiSystemNoType                   SystemNo;                    // S ys te mN o
    DstarApiSystemNoType                   EnquiryNo;                   // E nq ui ry No
    DstarApiDateTimeType                   UpdateTime;                  // U pd at eT im e
    DstarApiDateTimeType                   ExchInsertTime;              // E xc hI ns er tT im e
    DstarApiFundType                       FrozenMargin;                // F ro ze nM ar gi n
    DstarApiFundType                       Margin;                      // дїќиЇЃй‡‘
    DstarApiSerialIdType                   SerialId;                    // S er ia lI d
    DstarApiOrderIdType                    OrderId;                     // O rd er Id
    DstarApiErrorCodeType                  ErrCode;                     // E rr Co de
    DstarApiOrderStateType                 OrderState;                  // O rd er St at e
    DstarApiReferenceType                  Reference;                   // R ef er en ce
    DstarApiContractNoType                 ContractNo;                  // C on tr ac tN o
    DstarApiSeatIndexType                  SeatIndex;                   // S ea tI nd ex
    DstarApiSeatNoType                     UpSeatNo;                    // U pS ea tN o
    DstarApiQuantityType                   SellOrderQty;                // S el lO rd er Qt y
}DstarApiOfferField;


// иЇўд»·йЂљзџҐ
typedef struct DstarApiEnquiryField
{
    DstarApiContractNoType                 ContractNo;                  // C on tr ac tN o
    DstarApiDirectType                     Direct;                      // D ir ec t
    DstarApiSystemNoType                   EnquiryNo;                   // E nq ui ry No
}DstarApiEnquiryField;


// ж€ђдє¤ж•°жЌ®
typedef struct DstarApiMatchField
{
    DstarApiContractNoType                 ContractNo;                  // C on tr ac tN o
    DstarApiQuantityType                   MatchQty;                    // M at ch Qt y
    DstarApiPriceType                      MatchPrice;                  // M at ch Pr ic e
    DstarApiOffsetType                     Offset;                      // O ff se t
    DstarApiDirectType                     Direct;                      // D ir ec t
    DstarApiHedgeType                      Hedge;                       // жЉ•жњєеҐ—дїќ
    DstarApiOrderTypeType                  OrderType;                   // O rd er Ty pe
    DstarApiReferenceType                  Reference;                   // R ef er en ce
    DstarApiSerialIdType                   SerialId;                    // S er ia lI d
    DstarApiOrderIdType                    OrderId;                     // O rd er Id
    DstarApiMatchIdType                    MatchId;                     // M at ch Id
    DstarApiMatchTimeType                  MatchTime;                   // M at ch Ti me
    DstarApiExchMatchNo                    ExchMatchNo;                 // E xc hM at ch No
    DstarApiSystemNoType                   SystemNo;                    // S ys te mN o
    DstarApiFundType                       Fee;                         // F ee
    DstarApiFundType                       Margin;                      // дїќиЇЃй‡‘
    DstarApiFundType                       FrozenMargin;                // F ro ze nM ar gi n
    union
    {
        DstarApiFundType                   Premium;                     // P re mi um
        DstarApiFundType                   CloseProfit;                 // C lo se Pr of it
    };
    DstarApiAccountNoType                  AccountNo;                   // A cc ou nt No
    DstarApiDateTimeType                   UpdateTime;                  // U pd at eT im e
    DstarApiOrderIdType                    CmbId;                       // C mb Id
    DstarApiFundType                       OrderFee;                    // з”іжЉҐиґ№
}DstarApiMatchField;

// жЁжЊЃд»“ж•°жЌ®
typedef struct DstarApiPrePositionField
{
    DstarApiAccountNoType                  AccountNo;                   // A cc ou nt No
    DstarApiContractNoType                 ContractNo;                  // C on tr ac tN o
    DstarApiQuantityType                   PreBuyQty;                   // P re Bu yQ ty
    DstarApiPriceType                      PreBuyAvgPrice;              // P re Bu yA vg Pr ic e
    DstarApiQuantityType                   PreSellQty;                  // P re Se ll Qt y
    DstarApiPriceType                      PreSellAvgPrice;             // P re Se ll Av gP ri ce

}DstarApiPrePositionField;

// е®ћж—¶жЊЃд»“
typedef struct DstarApiPositionField
{
    DstarApiAccountNoType                  AccountNo;                   // A cc ou nt No
    DstarApiContractNoType                 ContractNo;                  // C on tr ac tN o
    DstarApiQuantityType                   PreBuyQty;                   // P re Bu yQ ty
    DstarApiQuantityType                   TodayBuyQty;                 // T od ay Bu yQ ty
    DstarApiPriceType                      BuyAvgPrice;                 // B uy Av gP ri ce
    DstarApiQuantityType                   PreSellQty;                  // P re Se ll Qt y
    DstarApiQuantityType                   TodaySellQty;                // T od ay Se ll Qt y
    DstarApiPriceType                      SellAvgPrice;                // S el lA vg Pr ic e
    DstarApiSerialIdType                   SerialId;                    // S er ia lI d
}DstarApiPositionField;


// иµ„й‡‘ж•°жЌ®
typedef struct DstarApiFundField
{
    DstarApiAccountNoType                  AccountNo;                   // A cc ou nt No
    DstarApiFundType                       PreEquity;                   // P re Eq ui ty
    DstarApiFundType                       Equity;                      // жќѓз›Љ
    DstarApiFundType                       Avail;                       // еЏЇз”Ё
    DstarApiFundType                       Fee;                         // F ee
    DstarApiFundType                       Margin;                      // дїќиЇЃй‡‘
    DstarApiFundType                       FrozenMargin;                // F ro ze nM ar gi n
    DstarApiFundType                       Premium;                     // P re mi um
    DstarApiFundType                       CloseProfit;                 // C lo se Pr of it
    DstarApiFundType                       PositionProfit;              // P os it io nP ro fi t
    DstarApiFundType                       CashIn;                      // е…Ґй‡‘
    DstarApiFundType                       CashOut;                     // е‡єй‡‘
    DstarApiFundType                       OrderFee;                    // з”іжЉҐиґ№
    DstarApiFundType                       Frozen;                      // F ro ze n
    DstarApiFundType                       DeliveryFrozen;              // D el iv er yF ro ze n
}DstarApiFundField;


// е‡єе…Ґй‡‘йЂљзџҐ
typedef struct DstarApiCashInOutField
{
    DstarApiSerialIdType                   SerialId;                    // S er ia lI d
    DstarApiCashInOutType                  CashInOutType;               // C as hI nO ut Ty pe
    DstarApiCashInOutModeType              CashInOutMode;               // е‡єе…Ґй‡‘ж–№ејЏ
    DstarApiFundType                       CashInOutValue;              // е‡єе…Ґй‡‘й‡‘йўќ
    DstarApiAccountNoType                  AccountNo;                   // A cc ou nt No
    DstarApiDateTimeType                   OperateTime;                 // O pe ra te Ti me
}DstarApiCashInOutField;

// жњЂж–°иЇ·ж±‚еЏ·еє”з­”
typedef struct DstaApiRspLastReqIdField
{
    DstarApiClientReqId                    LastClientReqId;             // L as tC li en tR eq Id
}DstaApiRspLastReqIdField;


// её‚ењєзЉ¶жЂЃ
typedef struct DstarApiTrdExchangeStateField
{
    DstarApiExchangeType                   ExchangeId;                     // E xc ha ng eI d
    DstarApiCommodityType                  CommodityType;                  // C om mo di ty Ty pe
    DstarApiCommodityNoType                CommodityNo;                    // C om mo di ty No
    DstarApiTradingStateType               TradingState;                   // T ra di ng St at e
    DstarApiDateTimeType                   ExchangeTime;                   // E xc ha ng eT im e
}DstarApiTrdExchangeStateField;

// ж‰‹з»­иґ№еЏ‚ж•°
typedef struct DstarApiTrdFeeParamField
{
    DstarApiAccountNoType                  AccountNo;                      // A cc ou nt No
    DstarApiContractNoType                 ContractNo;                     // C on tr ac tN o
    DstarApiParamType                      OpenRatio;                      // O pe nR at io
    DstarApiParamType                      OpenVolume;                     // O pe nV ol um e
    DstarApiParamType                      CloseRatio;                     // C lo se Ra ti o
    DstarApiParamType                      CloseVolume;                    // C lo se Vo lu me
    DstarApiParamType                      CloseTRatio;                    // C lo se TR at io
    DstarApiParamType                      CloseTVolume;                   // C lo se TV ol um e
}DstarApiTrdFeeParamField;

// дїќиЇЃй‡‘еЏ‚ж•°
typedef struct DstarApiTrdMarParamField
{
    DstarApiAccountNoType                  AccountNo;                      // A cc ou nt No
    DstarApiContractNoType                 ContractNo;                     // C on tr ac tN o
    DstarApiParamType                      BuySpeculateParam;              // B uy Sp ec ul at eP ar am
    DstarApiParamType                      BuyHedgeParam;                  // B uy He dg eP ar am
    DstarApiParamType                      SellSpeculateParam;             // S el lS pe cu la te Pa ra m
    DstarApiParamType                      SellHedgeParam;                 // S el lH ed ge Pa ra m
}DstarApiTrdMarParamField;

// жµ®з›€йЂљзџҐ
typedef struct DstarApiPosiProfitField
{
    DstarApiAccountNoType                  AccountNo;                      // A cc ou nt No
    DstarApiFundType                       PosiProfit;                     // P os iP ro fi t
    DstarApiSerialIdType                   SerialId;                       // S er ia lI d
}DstarApiPosiProfitField;

// дє¤ж“жќѓй™ђ
typedef struct DstarApiTradeRightField
{
    DstarApiAccountNoType                  AccountNo;                      // A cc ou nt No
    DstarApiExchangeType                   ExchangeId;                     // E xc ha ng eI d
    DstarApiCommodityType                  CommodityType;                  // C om mo di ty Ty pe
    DstarApiCommodityNoType                CommodityNo;                    // C om mo di ty No
    DstarApiTradeRightType                 BuyTradeRight;                  // B uy Tr ad eR ig ht
    DstarApiTradeRightType                 SellTradeRight;                 // S el lT ra de Ri gh t
}DstarApiTradeRightField;

// дє¤ж“жќѓй™ђе€ й™¤
typedef struct DstarApiTradeRightDelField
{
    DstarApiAccountNoType                  AccountNo;                      // A cc ou nt No
    DstarApiExchangeType                   ExchangeId;                     // E xc ha ng eI d
    DstarApiCommodityType                  CommodityType;                  // C om mo di ty Ty pe
    DstarApiCommodityNoType                CommodityNo;                    // C om mo di ty No
}DstarApiTradeRightDelField;


// е®ўж€·е“Ѓз§Ќз™ЅеђЌеЌ•
typedef struct DstarApiAccountCommListField
{
    DstarApiAccountNoType                  AccountNo;                      // A cc ou nt No
    DstarApiExchangeType                   ExchangeId;                     // E xc ha ng eI d
    DstarApiCommodityType                  CommodityType;                  // C om mo di ty Ty pe
    DstarApiCommodityNoType                CommodityNo;                    // C om mo di ty No
}DstarApiAccountCommListField;


// UDPеЌЏи®®е¤ґ
typedef struct DstarApiHead
{
    DstarApiProtocolCodeType               ProtocolCode;                // P ro to co lC od e
    DstarApiDataLen                        DataLen;                     // D at aL en
}DstarApiHead;


// Udpи®¤иЇЃиЇ·ж±‚
static const DstarApiProtocolCodeType      CMD_API_Req_UdpAuth      = 0xEC00;

// UDPи®¤иЇЃиЇ·ж±‚
typedef struct DstarApiReqUdpAuthField
{
    DstarApiAccountIndexType               AccountIndex;                // A cc ou nt In de x
    DstarApiAuthCodeType                   UdpAuthCode;                 // U dp Au th Co de
    DstarApiReqIdModeType                  ReqIdMode;                   // R eq Id Mo de
}DstarApiReqUdpAuthField;


// ж‰№й‡ЏжЉҐж’¤еЌ•, UDPеЌ•еЊ…жЉҐж–‡жњЂе¤§й•їеє¦дёє1024дёЄе­—иЉ‚

// жЉҐеЌ•иЇ·ж±‚
static const DstarApiProtocolCodeType      CMD_API_Req_OrderInsert  = 0xEC01;
// жЉҐд»·иЇ·ж±‚
static const DstarApiProtocolCodeType      CMD_API_Req_OfferInsert  = 0xEC02;
// ж–°жЉҐд»·иЇ·ж±‚
static const DstarApiProtocolCodeType      CMD_API_Req_OfferInsertNew = 0xEC09;
// ж’¤еЌ•иЇ·ж±‚
static const DstarApiProtocolCodeType      CMD_API_Req_OrderDelete  = 0xEC03;
// з»„еђ€жЉҐеЌ•иЇ·ж±‚
static const DstarApiProtocolCodeType      CMD_API_Req_CmbOrderInsert = 0xEC04;

// жЉҐеЌ•иЇ·ж±‚
typedef struct DstarApiReqOrderInsertField
{
    DstarApiDirectType                     Direct;                      // D ir ec t
    DstarApiOffsetType                     Offset;                      // O ff se t
    DstarApiHedgeType                      Hedge;                       // жЉ•жњєеҐ—дїќ
    DstarApiOrderTypeType                  OrderType;                   // O rd er Ty pe
    DstarApiValidTypeType                  ValidType;                   // V al id Ty pe
    DstarApiSeatIndexType                  SeatIndex;                   // S ea tI nd ex
    DstarApiAccountIndexType               AccountIndex;                // A cc ou nt In de x
    DstarApiContractIndexType              ContractIndex;               // C on tr ac tI nd ex
    DstarApiContractNoType                 ContractNo;                  // C on tr ac tN o
    DstarApiQuantityType                   OrderQty;                    // O rd er Qt y
    DstarApiQuantityType                   MinQty;                      // M in Qt y
    DstarApiPriceType                      OrderPrice;                  // O rd er Pr ic e
    DstarApiClientReqId                    ClientReqId;                 // C li en tR eq Id
    DstarApiReferenceType                  Reference;                   // R ef er en ce
    DstarApiAuthCodeType                   UdpAuthCode;                 // U dp Au th Co de
}DstarApiReqOrderInsertField;

// жЉҐд»·иЇ·ж±‚
// еЇ№дєЋдё­й‡‘ж‰ЂгЂЃе¤§е•†ж‰ЂгЂЃе№їжњџж‰ЂдЅїз”Ёж­¤з»“жћ„дЅ“жЉҐд»·иЎЁз¤єз›ёеђЊзљ„д№°еЌ–ж•°й‡Џ
// еЇ№дєЋдё­й‡‘ж‰ЂдЅїз”Ёж­¤з»“жћ„дЅ“жЉҐд»·иЎЁз¤єж™®йЂљжЉҐд»·пјЊдёЌж”ЇжЊЃйЎ¶еЌ•еЉџиѓЅ
typedef struct DstarApiReqOfferInsertField
{
    DstarApiOffsetType                     BuyOffset;                   // B uy Of fs et
    DstarApiOffsetType                     SellOffset;                  // S el lO ff se t
    DstarApiAccountIndexType               AccountIndex;                // A cc ou nt In de x
    DstarApiClientReqId                    ClientReqId;                 // C li en tR eq Id
    DstarApiContractIndexType              ContractIndex;               // C on tr ac tI nd ex
    DstarApiContractNoType                 ContractNo;                  // C on tr ac tN o
    DstarApiQuantityType                   OrderQty;                    // O rd er Qt y
    DstarApiPriceType                      BuyPrice;                    // B uy Pr ic e
    DstarApiPriceType                      SellPrice;                   // S el lP ri ce
    DstarApiSeatIndexType                  SeatIndex;                   // S ea tI nd ex
    DstarApiSystemNoType                   EnquiryNo;                   // E nq ui ry No
    DstarApiReferenceType                  Reference;                   // жЉҐеЌ•еј•з”Ё
    DstarApiAuthCodeType                   UdpAuthCode;                 // U dp Au th Co de
    
}DstarApiReqOfferInsertField;

// ж–°жЉҐд»·иЇ·ж±‚
// еЇ№дєЋйѓ‘е•†ж‰ЂгЂЃдёЉжњџж‰ЂдЅїз”Ёж­¤з»“жћ„дЅ“жЉҐд»·пјЊд№°гЂЃеЌ–ж–№еђ‘жЉҐд»·ж•°й‡Џеї…йЎ»еЎ«е†™дёЂи‡ґ
// еЇ№дєЋдё­й‡‘ж‰ЂдЅїз”Ёж­¤з»“жћ„дЅ“жЉҐд»·ж”ЇжЊЃйЎ¶еЌ•еЉџиѓЅпјЊвЂње®љеЌ•е§”ж‰еЏ·вЂќеЎ«е†™0иЎЁз¤єж™®йЂљжЉҐд»·пјЊ1иЎЁз¤єйЎ¶еЌ•жњЂиї‘дёЂз¬”жЉҐд»·пјЊе…¶д»–иЎЁз¤єйЎ¶еЌ•жЊ‡е®љзљ„жЉҐд»·
typedef struct DstarApiReqOfferInsertNewField
{
    DstarApiOffsetType                     BuyOffset;                   // B uy Of fs et
    DstarApiOffsetType                     SellOffset;                  // S el lO ff se t
    DstarApiAccountIndexType               AccountIndex;                // A cc ou nt In de x
    DstarApiClientReqId                    ClientReqId;                 // C li en tR eq Id
    DstarApiContractIndexType              ContractIndex;               // C on tr ac tI nd ex
    DstarApiContractNoType                 ContractNo;                  // C on tr ac tN o
    DstarApiU16QuantityType                BuyOrderQty;                 // B uy Or de rQ ty
    DstarApiU16QuantityType                SellOrderQty;                // S el lO rd er Qt y
    DstarApiPriceType                      BuyPrice;                    // B uy Pr ic e
    DstarApiPriceType                      SellPrice;                   // S el lP ri ce
    DstarApiSeatIndexType                  SeatIndex;                   // S ea tI nd ex
    DstarApiSystemNoType                   EnquiryNo;                   // E nq ui ry No
    DstarApiReferenceType                  Reference;                   // жЉҐеЌ•еј•з”Ё
    DstarApiAuthCodeType                   UdpAuthCode;                 // U dp Au th Co de
    DstarApiReplaceIdType                  ReplaceId;                   // R ep la ce Id

}DstarApiReqOfferInsertNewField;

// ж’¤еЌ•иЇ·ж±‚ (ж’¤еЌ•е¤±иґҐж—¶иї”е›ће§”ж‰йЂљзџҐж€–жЉҐд»·йЂљзџҐ,и®ўеЌ•зЉ¶жЂЃдёЌеЏ,еЊ…еђ«ж’¤еЌ•е¤±иґҐзљ„й”™иЇЇз Ѓ)
typedef struct DstarApiReqOrderDeleteField
{
    DstarApiAccountIndexType               AccountIndex;                // A cc ou nt In de x
    DstarApiClientReqId                    ClientReqId;                 // C li en tR eq Id
    DstarApiAuthCodeType                   UdpAuthCode;                 // U dp Au th Co de
    DstarApiReferenceType                  Reference;                   // R ef er en ce
    DstarApiSeatIndexType                  SeatIndex;                   // S ea tI nd ex
    DstarApiOrderIdType                    OrderId;                     // O rd er Id
    DstarApiSystemNoType                   SystemNo;                    // S ys te mN o
}DstarApiReqOrderDeleteField;

// з»„еђ€жЉҐеЌ•иЇ·ж±‚
typedef struct DstarApiReqCmbOrderInsertField
{
    DstarApiDirectType                     Direct;                      // D ir ec t
    DstarApiOffsetType                     Offset;                      // O ff se t
    DstarApiHedgeType                      Hedge;                       // жЉ•жњєеҐ—дїќ
    DstarApiOrderTypeType                  OrderType;                   // O rd er Ty pe
    DstarApiValidTypeType                  ValidType;                   // V al id Ty pe
    DstarApiSeatIndexType                  SeatIndex;                   // S ea tI nd ex
    DstarApiAccountIndexType               AccountIndex;                // A cc ou nt In de x
    DstarApiContractIndexType              ContractIndex1;              // C on tr ac tI nd ex1
    DstarApiContractNoType                 ContractNo1;                 // C on tr ac tN o1
    DstarApiContractIndexType              ContractIndex2;              // C on tr ac tI nd ex2
    DstarApiContractNoType                 ContractNo2;                 // C on tr ac tN o2
    DstarApiQuantityType                   OrderQty;                    // O rd er Qt y
    DstarApiQuantityType                   MinQty;                      // M in Qt y
    DstarApiPriceType                      OrderPrice;                  // O rd er Pr ic e
    DstarApiClientReqId                    ClientReqId;                 // C li en tR eq Id
    DstarApiReferenceType                  Reference;                   // жЉҐеЌ•еј•з”Ё
    DstarApiAuthCodeType                   UdpAuthCode;                 // U dp Au th Co de
}DstarApiReqCmbOrderInsertField;


#pragma pack(pop)


#endif // DSTARTRADEAPISTRUCT_H
