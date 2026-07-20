#ifndef APICLIENT_H
#define APICLIENT_H

#include "../include/DstarQuoteApiError.h"
#include "../include/DstarQuoteApi.h"
#include "../include/DstarQuoteApiStruct.h"
#include "../include/DstarQuoteApiDataType.h"

#include <stdio.h>
#include <string>
#include "string.h"
#include <vector>
#include <iostream>

#include <unordered_map>
#include <map>
#include <set>
#include <deque>
#include <mutex>

using namespace std;

string DoubleToString(double val);
string GetDstarErrorString(int code);
extern bool fullLog;

class apiClient
{
public:
        apiClient();
        ~apiClient();
        
        bool Init();
        bool Login();
        
        
        int  SubscribeQuote(const char *str);
        int  UnSubscribeQuote(const char *contract);
        bool SetAutoRelogin(bool relogin);

		const char* GetVerion();
        
        int CommodityQry();
        int ContractQry();
        void SetCpuId(DstarApiCpuIdType nRecvNoticeDataCpuId);
        bool SetHostAddress(const char *IP, unsigned short port);
        void SetApiLogPath(const DstarApiPathType pPath);

		void Free();
        std::vector<DstarApiQuoteData> GetQuoteHistorySnapshot();
        size_t GetQuoteHistorySize();

        string GetDateTimeNow();		

        void PushQuote(const DstarApiQuoteData& quote);

public:
        //std::vector<const DstarFrontApiCommodityDataRsp *> vec_commdodity;        //Variety search results saved locally
        bool isReady;
        bool isLast1_comm;
        bool isLast1_cont;
        std::map<std::string, const DstarQuoteApiCommodityData>    map_commodity;   //保存到本地的品种查询结果
        std::set<std::string>                                      set_contract;    //保存到本地的合约查询结果
        DstarApiQuoteData all;
        
private:
        DstarQuoteApi  *m_QuoteApi;
        DstarQuoteSpi  *m_QuoteSpi;   
};

class Notify : public DstarQuoteSpi
{
    public:
        Notify(apiClient *testApi);
        
        //继承
        virtual void OnApiReady();
	    virtual void OnDisconnect(int reasonCode);
        virtual void OnError(int reasonCode);
        virtual void OnRspCommodity(const DstarQuoteApiCommodityData* buf, bool isLast);
        virtual void OnRspContract(const char* buf, bool isLast);
        virtual void OnRtnQuote(const DstarApiQuoteData *info);
        
        //business
        void subQuote();
        //bool IsReady(){return isReady;}
                  
    public:  
        apiClient *m_Api;
};

#endif /* APICLIENT_H */
