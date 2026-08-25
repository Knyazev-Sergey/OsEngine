///@system  Dstar V10
///@file    DstarTradeApiDataType.h
///@author  Hao Lin 2020-08-20


#ifndef DSTARTRADEAPIDATATYPE_H
#define DSTARTRADEAPIDATATYPE_H


// String types
// String length = 10
typedef char                STR10[11];
// String length = 20
typedef char                STR20[21];
// String length = 30
typedef char                STR30[31];
// String length = 40
typedef char                STR40[41];
// String length = 50
typedef char                STR50[51];
// String length = 100
typedef char                STR100[101];
// String length = 200
typedef char                STR200[201];

typedef int                 RESULT;


// Protocol version type
typedef unsigned char       DstarApiVersionType;
const DstarApiVersionType   DSTAR_API_PROTOCOL_VERSION      = 3;

// Protocol code type
typedef unsigned short      DstarApiProtocolCodeType;

// Data length
typedef unsigned short      DstarApiDataLen;

// Client request ID
typedef unsigned int        DstarApiClientReqId;

// Order reference, -1: no order reference
typedef long long           DstarApiReferenceType;

// Descriptor
typedef int                 DstarApiFdType;

// IP
typedef char                DstarApiIpType[41];

// IPv4
typedef char                DstarApiIPv4IpType[16];

// Port
typedef unsigned short      DstarApiPortType;

// Log path
typedef char                DstarApiPathType[256];

// CPU ID
typedef int                 DstarApiCpuIdType;

// Serial number
typedef unsigned long long  DstarApiSerialIdType;

// Account number
typedef char                DstarApiAccountNoType[21];

// Contract index
typedef unsigned int        DstarApiContractIndexType;

// Account index
typedef unsigned short      DstarApiAccountIndexType;

// Seat index
typedef unsigned char       DstarApiSeatIndexType;

// Seat number
typedef char                DstarApiSeatNoType[21];

// Password
typedef char                DstarApiPasswdType[65];

// Notification subscription ID type
typedef long long           DstarApiNoticeSubIdType;
// Subscribe from beginning
const DstarApiNoticeSubIdType DSTAR_API_SUB_HEAD            = 0;
// Subscribe from latest
const DstarApiNoticeSubIdType DSTAR_API_SUB_LAST            = -1;

// Real-time data filter
typedef int                 DstarApiRealTimeDataFilterType;
// Do not filter any data
const DstarApiRealTimeDataFilterType DSTAR_API_REAL_TIME_DATA_FILTER_NONE = 0;
// Ignore all data
const DstarApiRealTimeDataFilterType DSTAR_API_REAL_TIME_DATA_FILTER_IGNORE_ALL = -1;

// Run mode
typedef int                 DstarApiRunModeType;
// Full-load mode
const DstarApiRunModeType   DSTAR_API_RUN_MODE_TYPE_FULL_LOAD = 0;
// Non-full-load mode
const DstarApiRunModeType   DSTAR_API_RUN_MODE_TYPE_NON_FULL_LOAD = -1;

// Initialization mode
typedef char                DstarApiInitType;
// Query basic data during initialization
const DstarApiInitType      DSTAR_API_INIT_QUERY            = 0;
// Do not query basic data during initialization
const DstarApiInitType      DSTAR_API_INIT_NOQUERY          = -1;

// UDP authentication code
typedef unsigned int        DstarApiAuthCodeType;

// Request ID mode
typedef unsigned char       DstarApiReqIdModeType;
// No check: request ID is not validated
const DstarApiReqIdModeType DSTAR_API_REQIDMODE_NOCHECK     = 0;
// Increasing: request ID must be greater than previous one, otherwise cancel is invalid
const DstarApiReqIdModeType DSTAR_API_REQIDMODE_INCREASE    = 1;
// Strict increment: request ID must continuously increase, otherwise cancel is invalid
const DstarApiReqIdModeType DSTAR_API_REQIDMODE_FORCE       = 2;


// Trading code
typedef char                DstarApiTradeNoType[9];

// Exchange
typedef char                DstarApiExchangeType;
const DstarApiExchangeType  DSTAR_API_EXCHANGE_ZCE          = 'Z';          // Zhengzhou Commodity Exchange
const DstarApiExchangeType  DSTAR_API_EXCHANGE_SHFE         = 'S';          // Shanghai Futures Exchange
const DstarApiExchangeType  DSTAR_API_EXCHANGE_INE          = 'I';          // Shanghai International Energy Exchange
const DstarApiExchangeType  DSTAR_API_EXCHANGE_CFFEX        = 'C';          // China Financial Futures Exchange
const DstarApiExchangeType  DSTAR_API_EXCHANGE_DCE          = 'D';          // Dalian Commodity Exchange
const DstarApiExchangeType  DSTAR_API_EXCHANGE_GFEX         = 'F';          // Guangzhou Futures Exchange
const DstarApiExchangeType  DSTAR_API_EXCHANGE_SGE          = 'G';          // Shanghai Gold Exchange

// Commodity code
typedef char                DstarApiCommodityNoType[11];

// Commodity type
typedef char                DstarApiCommodityType;
const DstarApiCommodityType DSTAR_API_COMMTYPE_FUTURES      = 'F';          // Futures
const DstarApiCommodityType DSTAR_API_COMMTYPE_OPTION       = 'O';          // Options
const DstarApiCommodityType DSTAR_API_COMMTYPE_SPD          = 'S';          // Calendar spread
const DstarApiCommodityType	DSTAR_API_COMMTYPE_IPS          = 'M';          // Inter-commodity spread
const DstarApiCommodityType	DSTAR_API_COMMTYPE_STD		    = 'D';          // Straddle
const DstarApiCommodityType	DSTAR_API_COMMTYPE_STG		    = 'G';          // Strip spread
const DstarApiCommodityType	DSTAR_API_COMMTYPE_PRT			= 'R';          // Covered option
const DstarApiCommodityType DSTAR_API_COMMTYPE_NONE         = 'N';          // None

// Contract
typedef char                DstarApiContractNoType[16];

// Contract size type
typedef int                 DstarApiContractSizeType;

// Minimum price tick
typedef double              DstarApiContractTickSizeType;

// Buy/Sell direction
typedef char                DstarApiDirectType;
const DstarApiDirectType    DSTAR_API_DIRECT_BUY            = 'B';          // Buy
const DstarApiDirectType    DSTAR_API_DIRECT_SELL           = 'S';          // Sell
const DstarApiDirectType    DSTAR_API_DIRECT_ALL            = 'N';          // All

// Open/Close offset
typedef char                DstarApiOffsetType;
const DstarApiOffsetType    DSTAR_API_OFFSET_OPEN           = 'O';          // Open
const DstarApiOffsetType    DSTAR_API_OFFSET_CLOSE          = 'C';          // Close
const DstarApiOffsetType    DSTAR_API_OFFSET_CLOSETODAY     = 'T';          // Close today

// Hedge type
typedef char                DstarApiHedgeType;
const DstarApiHedgeType     DSTAR_API_HEDGE_SPECULATE       = 'T';          // Speculation
const DstarApiHedgeType     DSTAR_API_HEDGE_HEDGE           = 'B';          // Hedge

// Order type
typedef char                DstarApiOrderTypeType;
const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_NONE        = '0';          // None
const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_MARKET      = '1';          // Market order
const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_LIMIT       = '2';          // Limit order
const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_EXECUTE     = '3';          // Exercise
const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_ABANDON     = '4';          // Abandon
const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_ENQUIRY     = '5';          // Inquiry
const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_OFFER       = '6';          // Quote
const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_SWAP        = '7';          // Swap
const DstarApiOrderTypeType DSTAR_API_ORDERTYPE_EFP         = '8';          // EFP

// Trading permission
typedef char                 DstarApiTradeRightType;
const DstarApiTradeRightType DSTAR_API_TRADERIGHT_NORMAL    = '0';          // Normal trading
const DstarApiTradeRightType DSTAR_API_TRADERIGHT_NOTRADE   = '1';          // Trading prohibited
const DstarApiTradeRightType DSTAR_API_TRADERIGHT_CLOSE     = '2';          // Close only

// System number
typedef char                DstarApiSystemNoType[21];

// Exchange match number
typedef char                DstarApiExchMatchNo[71];

// Date type yyyymmdd
typedef char                DstarApiDateType[9];

// Order ID
typedef unsigned long long  DstarApiOrderIdType;

// Replace order ID (for CFFEX quoting)
typedef unsigned long long  DstarApiReplaceIdType;
// Normal quote
const DstarApiReplaceIdType DSTAR_API_REPLACE_NORMAL        = 0;
// Most recent quote
const DstarApiReplaceIdType DSTAR_API_REPLACE_LAST          = 1;

// Match ID
typedef unsigned long long  DstarApiMatchIdType;

// Local order number
typedef char                DstarApiOrderLocalNoType[21];

// Price type
typedef double              DstarApiPriceType;

// Fund type
typedef double              DstarApiFundType;

// Parameter type
typedef double              DstarApiParamType;

// Quantity type U16
typedef unsigned short      DstarApiU16QuantityType;

// Quantity type
typedef unsigned int        DstarApiQuantityType;

// Match time yyyymmddhhmmss
typedef char                DstarApiMatchTimeType[20];

// Date time yyyy-mm-dd hh:mm:ss
typedef char                DstarApiDateTimeType[20];

// Validity type
typedef char                DstarApiValidTypeType;
const DstarApiValidTypeType DSTAR_API_VALID_FOK             = '1';          // Fill or Kill
const DstarApiValidTypeType DSTAR_API_VALID_IOC             = '2';          // Immediate or Cancel
const DstarApiValidTypeType DSTAR_API_VALID_GFD             = '3';          // Good for Day
const DstarApiValidTypeType DSTAR_API_VALID_GIS             = '4';          // Good in Session

// Valid date type (yyyymmdd)
typedef unsigned int        DstarApiValidDateType;

// Order status
typedef char                DstarApiOrderStateType;

const DstarApiOrderStateType DSTAR_API_STATUS_ACCEPT        = '1';          // Accepted
const DstarApiOrderStateType DSTAR_API_STATUS_QUEUE         = '2';          // Queued
const DstarApiOrderStateType DSTAR_API_STATUS_APPLY         = '3';          // Applied (exercise/abandon/spread application succeeded)
const DstarApiOrderStateType DSTAR_API_STATUS_SUSPENDED     = '4';          // Suspended
const DstarApiOrderStateType DSTAR_API_STATUS_TRIGGERED     = '5';          // Triggered
const DstarApiOrderStateType DSTAR_API_STATUS_PARTFILL      = '6';          // Partially filled
const DstarApiOrderStateType DSTAR_API_STATUS_FILL          = '7';          // Fully filled
const DstarApiOrderStateType DSTAR_API_STATUS_FAIL          = '8';          // Command failed
const DstarApiOrderStateType DSTAR_API_STATUS_DELETE        = 'B';          // Canceled
const DstarApiOrderStateType DSTAR_API_STATUS_LEFTDELETE    = 'C';          // Remaining quantity canceled
const DstarApiOrderStateType DSTAR_API_STATUS_SYSDELETE     = 'D';          // Deleted
const DstarApiOrderStateType DSTAR_API_STATUS_TRIGGERING    = 'E';          // Strategy pending trigger

// Cash in/out type
typedef char                DstarApiCashInOutType;
// Deposit
const DstarApiCashInOutType DSTAR_API_CASH_IN               = 'I';
// Withdraw
const DstarApiCashInOutType DSTAR_API_CASH_OUT              = 'O';

// Cash in/out mode
typedef char                DstarApiCashInOutModeType;
// Transfer
const DstarApiCashInOutModeType DSTAR_API_CASHMODE_TRANSFER  = '1';
// Cheque
const DstarApiCashInOutModeType DSTAR_API_CASHMODE_CHEQUE    = '2';
// Cash
const DstarApiCashInOutModeType DSTAR_API_CASHMODE_CASH      = '3';
// FX swap
const DstarApiCashInOutModeType DSTAR_API_CASHMODE_SWAP      = '4';
// Bank-futures transfer
const DstarApiCashInOutModeType DSTAR_API_CASHMODE_BFTRNSFER = '5';

// Trading state
typedef char                DstarApiTradingStateType;
// Unknown state
const DstarApiTradingStateType DSTAR_API_TRADE_STATE_UNKNOWN    = '0';
// Call auction
const DstarApiTradingStateType DSTAR_API_TRADE_STATE_BID        = '1';
// Auction matching
const DstarApiTradingStateType DSTAR_API_TRADE_STATE_MATCH      = '2';
// Continuous trading
const DstarApiTradingStateType DSTAR_API_TRADE_STATE_CONTINUOUS = '3';
// Trading paused
const DstarApiTradingStateType DSTAR_API_TRADE_STATE_PAUSED     = '4';
// Market close
const DstarApiTradingStateType DSTAR_API_TRADE_STATE_CLOSE      = '5';
// Close processing period
const DstarApiTradingStateType DSTAR_API_TRADE_STATE_DEALLAST   = '6';
// Initializing
const DstarApiTradingStateType DSTAR_API_TRADE_STATE_INITIALIZE = '7';
// Ready
const DstarApiTradingStateType DSTAR_API_TRADE_STATE_READY      = '8';

// Software authorization type
typedef char                DstarApiAuthTypeType;
// Non-collection mode software authorization
const DstarApiAuthTypeType  DSTAR_API_AUTHTYPE_NOGATHER     = '0';
// Direct mode software authorization
const DstarApiAuthTypeType  DSTAR_API_AUTHTYPE_DIRECT       = '1';
// Relay mode software authorization
const DstarApiAuthTypeType  DSTAR_API_AUTHTYPE_RELAY        = '2';

// Collection info key version
typedef unsigned int        DstarApiAuthKeyVersion;

// System collection information
typedef char                DstarApiSystemInfoType[501];

// App ID type
typedef char                DstarApiAppIdType[31];

// Software license number type
typedef char                DstarApiLicenseNoType[51];

// Start time hhmmss
typedef unsigned int        DstarApiStartTimeType;

// Start mode
typedef unsigned char       DstarApiStartModeType;

// Account check
const DstarApiStartModeType    DSTAR_API_STARTMODE_CHECK    = 0;
// Trading
const DstarApiStartModeType    DSTAR_API_STARTMODE_TRADE    = 1;

// Seat status
typedef char                   DstarApiSeatStateType;
// Disconnected
const DstarApiSeatStateType    DSTAR_API_SEATSTATE_DISCONNECT = 'D';
// Normal
const DstarApiSeatStateType    DSTAR_API_SEATSTATE_NORMAL     = 'N';

// Yes/No
typedef unsigned char          DstarApiYesNoType;
// Yes
const DstarApiYesNoType        DSTAR_API_YES                  = 1;
// No
const DstarApiYesNoType        DSTAR_API_NO                   = 0;


#endif // DSTARTRADEAPIDATATYPE_H
