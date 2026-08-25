///@system  Dstar V10
///@file    DstarQuoteApiError.h
///@author  ln 2021-10-08

#ifndef DSTARQUOTEAPIERROR_H
#define DSTARQUOTEAPIERROR_H

//! 被动断开
const int ERROR_DISCONNECT_CLOSE_PASS                                = -1;
//! 连接超时
const int ERROR_DISCONNECT_CONNECT_TIMEOUT                           = -2;
//! 断开重连超时
const int ERROR_DISCONNECT_RECONNECT_TIMEOUT                         = -3;
//! 发送连接请求数据失败
const int ERROR_SEND_LOGIN_DATA                                      = -4;
//! 心跳数据发送错误
const int ERROR_SEND_HEARTBEATDATA                                   = -5;
//! 品种订阅数据发送错误
const int ERROR_SEND_COMMODITYDATA                                   = -6;
//! 合约订阅数据发送错误
const int ERROR_SEND_CONTRACTDATA                                    = -7;
//! 行情订阅数据发送错误
const int ERROR_SUBSNAPSHOTDATA                                      = -8;
//! 行情订阅数据发送错误
const int ERROR_UNSUBSNAPSHOTDATA                                    = -9;
//! 未拿到合约价格精度
const int ERROR_NO_DECIMAL                                           = -10;
//! 日志文件夹打开失败
const int ERROR_OPEN_LOGDIR                                          = -11;
//! 输入数据为NULL
const int ERROR_INPUT_NULL                                           = -12;
//! 行情查询中合约不存在或合约未更新
const int ERROR_UNKNOWN_CONTRACT                                     = -13;
//! 取消未订阅的合约
const int ERROR_UNSUBCONT                                            = -14;
//! 初始化日志失败
const int ERROR_INIT_LOG                                             = -15;
//! 设置线程绑核失败
const int ERROR_SET_CPUID                                            = -16;
//! Ip格式错误
const int ERROR_IP_FORMAT                                            = -17;
//! 路径不是一个文件夹
const int ERROR_LOG_PATH                                             = -18;
#endif /* DstarQuoteApiEROOR_H */

