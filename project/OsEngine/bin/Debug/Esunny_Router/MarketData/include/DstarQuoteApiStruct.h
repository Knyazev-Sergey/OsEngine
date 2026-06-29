///@system  Dstar V10
///@file    DstarQuoteApiStruct.h
///@author  ln 2021-10-08

#ifndef DSTARFRONTAPISTRUCT_H
#define DSTARFRONTAPISTRUCT_H

#include "DstarQuoteApiDataType.h"

#pragma pack(push, 1)
//////////////////////////////////////////////////////////////////////////////////////////
//品种
// 查询应答
typedef struct DstarQuoteApiCommodityData
{
    DstarQuoteCommodityNoType                CommodityNo;                      // 品种编号
    DstarQuoteExchangeNoType                 ExchangeNo;                       // 交易所
    DstarQuoteCommodityType                  CommodityType;                    // 品种类型
    DstarQuoteContractSizeType               ContractSize;                     // 每手乘数
    DstarQuoteContractTickSizeType           ContractTickSize;                 // 最小变动价位
}DstarQuoteApiCommodityData;
//////////////////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////////////////////
//! 行情全文
typedef struct DstarApiQuoteData
{
    DstarApiStr                                   QContractNo;                                    ///< 合约编号
    DstarApiStamp                                 QDateTimeStamp;                                 ///< 时间戳
    DstarApiPrice                                 QPreClosingPrice;                               ///< 昨收盘价
    DstarApiPrice                                 QPreSettlePrice;                                ///< 昨结算价
    DstarApiVolume                                QPrePositionQty;                                ///< 昨持仓量
    DstarApiPrice                                 QOpeningPrice;                                  ///< 开盘价
    DstarApiPrice                                 QLastPrice;                                     ///< 最新价
    DstarApiPrice                                 QHighPrice;                                     ///< 最高价
    DstarApiPrice                                 QLowPrice;                                      ///< 最低价
    DstarApiPrice                                 QHisHighPrice;                                  ///< 历史最高价
    DstarApiPrice                                 QHisLowPrice;                                   ///< 历史最低价
    DstarApiPrice                                 QLimitUpPrice;                                  ///< 涨停价
    DstarApiPrice                                 QLimitDownPrice;                                ///< 跌停价
    DstarApiVolume                                QTotalQty;                                      ///< 当日总成交量
    DstarApiVolume                                QPositionQty;                                   ///< 持仓量
    DstarApiPrice                                 QAveragePrice;                                  ///< 均价
    DstarApiPrice                                 QClosingPrice;                                  ///< 收盘价
    DstarApiPrice                                 QSettlePrice;                                   ///< 结算价
    DstarApiVolume                                QLastQty;                                       ///< 最新成交量       
    DstarApiVolume                                QTotalBidQty;                                   ///< 委买总量                
    DstarApiVolume                                QTotalAskQty;                                   ///< 委卖总量        
    DstarApiPrice                                 QTotalTurnOver;                                 ///< 总成交额
    DstarApiPrice                                 QBidPrice1;                                     ///< 买价1
    DstarApiPrice                                 QBidPrice2;                                     ///< 买价2
    DstarApiPrice                                 QBidPrice3;                                     ///< 买价3
    DstarApiPrice                                 QBidPrice4;                                     ///< 买价4
    DstarApiPrice                                 QBidPrice5;                                     ///< 买价5
    DstarApiPrice                                 QBidPrice6;                                     ///< 买价6
    DstarApiPrice                                 QBidPrice7;                                     ///< 买价7
    DstarApiPrice                                 QBidPrice8;                                     ///< 买价8
    DstarApiPrice                                 QBidPrice9;                                     ///< 买价9
    DstarApiPrice                                 QBidPrice10;                                    ///< 买价10
    DstarApiVolume                                QBidQty1;                                       ///< 买量1
    DstarApiVolume                                QBidQty2;                                       ///< 买量2
    DstarApiVolume                                QBidQty3;                                       ///< 买量3
    DstarApiVolume                                QBidQty4;                                       ///< 买量4
    DstarApiVolume                                QBidQty5;                                       ///< 买量5
    DstarApiVolume                                QBidQty6;                                       ///< 买量6
    DstarApiVolume                                QBidQty7;                                       ///< 买量7
    DstarApiVolume                                QBidQty8;                                       ///< 买量8
    DstarApiVolume                                QBidQty9;                                       ///< 买量9
    DstarApiVolume                                QBidQty10;                                      ///< 买量10
    DstarApiPrice                                 QAskPrice1;                                     ///< 卖价1
    DstarApiPrice                                 QAskPrice2;                                     ///< 卖价2
    DstarApiPrice                                 QAskPrice3;                                     ///< 卖价3
    DstarApiPrice                                 QAskPrice4;                                     ///< 卖价4
    DstarApiPrice                                 QAskPrice5;                                     ///< 卖价5
    DstarApiPrice                                 QAskPrice6;                                     ///< 卖价6
    DstarApiPrice                                 QAskPrice7;                                     ///< 卖价7
    DstarApiPrice                                 QAskPrice8;                                     ///< 卖价8
    DstarApiPrice                                 QAskPrice9;                                     ///< 卖价9
    DstarApiPrice                                 QAskPrice10;                                    ///< 卖价10
    DstarApiVolume                                QAskQty1;                                       ///< 卖量1
    DstarApiVolume                                QAskQty2;                                       ///< 卖量2
    DstarApiVolume                                QAskQty3;                                       ///< 卖量3
    DstarApiVolume                                QAskQty4;                                       ///< 卖量4
    DstarApiVolume                                QAskQty5;                                       ///< 卖量5
    DstarApiVolume                                QAskQty6;                                       ///< 卖量6
    DstarApiVolume                                QAskQty7;                                       ///< 卖量7
    DstarApiVolume                                QAskQty8;                                       ///< 卖量8
    DstarApiVolume                                QAskQty9;                                       ///< 卖量9
    DstarApiVolume                                QAskQty10;                                      ///< 卖量10 
} DstarApiQuoteData;

#pragma pack(pop)

#endif /* DSTARFRONTAPISTRUCT_H */

