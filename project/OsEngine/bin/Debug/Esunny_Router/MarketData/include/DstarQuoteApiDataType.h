///@system  Dstar V10
///@file    DstarQuoteApiType.h
///@author  Mouron 2021-10-08

#ifndef DSTARFRONTAPITYPE_H
#define DSTARFRONTAPITYPE_H

// 行情各字段含义定义
typedef unsigned char              U8;
typedef signed int                 I32;
typedef unsigned int               U32;
typedef unsigned long long         U64;
typedef char                       C8;
typedef float                      F32;
typedef double                     F64;

////////////////////////////////////////////////////////////////////////////////
//! unsigned 32
typedef unsigned int            DstarApiVolume;
//! 行情价格
typedef double                  DstarApiPrice;
//! 时间戳类型(格式 yyyy-MM-dd hh:nn:ss.xxx)
typedef char                    DstarApiStamp[24];
//! 长度为50的字符串
typedef char                    DstarApiStr[51];

// 日志路径
typedef char                    DstarApiPathType[256];
////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
// 品种
typedef char                    DstarQuoteCommodityNoType[21];
// 交易所编号
typedef char                    DstarQuoteExchangeNoType[11];
// 品种类型
typedef char                    DstarQuoteCommodityType;
// 合约乘数类型
typedef I32                     DstarQuoteContractSizeType;
// 最小变动价位
typedef F64                     DstarQuoteContractTickSizeType;
////////////////////////////////////////////////////////////////////////////////
// Cpu Id
typedef int                     DstarApiCpuIdType;

#endif /* DSTARFRONTAPITYPE_H */

