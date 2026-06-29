///@system  Dstar V10 api demo
///@file    main.cpp
///@author  Hao Lin 2021-01-20

#include "ApiClient.h"
#include "UdpClient.h"
#include <iostream>
#include <list>
#include <mutex>
#include <string>
#include <nlohmann/json.hpp>

#include <chrono>
#include <iomanip>
#include <sstream>

#include <stdio.h>
#if defined WIN32 || defined _WIN32
#include <windows.h>
#else
#include <unistd.h>
#endif

using namespace std;

char    front_ip[] = "61.163.243.173";
int     front_port = 6668;

char    account[] = "Q123567";
char    passwd[] = "Apri2026$";
char    app_id[] = "Demo_TestCollect";
char    license_no[] = "Demo_TestCollect";

int     contindex = 402;
char    contractno[] = "AG2605";
int     option_contindex = 24;
char    option_contractno[] = "CF001C14600";
char    direct = DSTAR_API_DIRECT_BUY;
char    offset = DSTAR_API_OFFSET_OPEN;
int     qty = 1;
int     min_qty = 0;
double  price = 17870;
int     client_reqid = 1;
int     reference = 1;
int     cmb_contindex1 = 374;
char    cmb_contractno1[] = "SR903C5600";
int     cmb_contindex2 = 400;
char    cmb_contractno2[] = "SR903P5600";
double  cmb_price = 1200;

ApiClient apiClient;

list<string> MessagesIn;
list<string> MessagesOut;
mutex mutex_array_all;
mutex mutex_array_in;
mutex mutex_array_out;
bool isDisconnected = false;

#pragma region Server

bool RecvAll(SOCKET s, char* data, size_t n)
{
    size_t got = 0;
    while (got < n)
    {
        int ret = recv(s, data + got, (int)(n - got), 0);
        if (ret <= 0)
        {
            return false;
        }
        got += (size_t)ret;
    }
    return true;
}

bool SendAll(SOCKET s, const char* data, size_t n)
{
    size_t sent = 0;
    while (sent < n)
    {
        int ret = send(s, data + sent, (int)(n - sent), 0);
        if (ret <= 0)
        {
            return false;
        }
        sent += (size_t)ret;
    }
    return true;
}

bool RecvFrame(SOCKET s, string& out)
{
    uint32_t lenNet = 0;
    if (!RecvAll(s, reinterpret_cast<char*>(&lenNet), sizeof(lenNet)))
    {
        return false;
    }

    const uint32_t len = ntohl(lenNet);
    const uint32_t maxLen = 10U * 1024U * 1024U;
    if (len > maxLen)
    {
        return false;
    }

    out.clear();
    out.resize(len);
    if (len > 0 && !RecvAll(s, &out[0], len))
    {
        return false;
    }

    return true;
}

bool SendFrame(SOCKET s, const string& msg)
{
    const uint32_t len = (uint32_t)msg.size();
    const uint32_t lenNet = htonl(len);

    if (!SendAll(s, reinterpret_cast<const char*>(&lenNet), sizeof(lenNet)))
    {
        return false;
    }
    if (len > 0 && !SendAll(s, msg.data(), len))
    {
        return false;
    }
    return true;
}

DWORD WINAPI serverReceive(LPVOID lpParam)
{
    SOCKET client = *(SOCKET*)lpParam;

    MessagesIn.clear();
    MessagesOut.clear();

    while (true)
    {
        try
        {
            string incoming;

            if (!RecvFrame(client, incoming))
            {                
                cout << apiClient.GetDateTimeNow() << "recv function failed with error " << WSAGetLastError() << endl;
                return -1;
            }

            if (incoming.empty())
            {
                continue;
            }

            if (incoming == "Process")
            {               
                lock_guard<mutex> outLock(mutex_array_out);

                if (MessagesOut.size() > 0)
                {
                    list <string>::iterator it;

                    for (it = MessagesOut.begin(); it != MessagesOut.end(); it++)
                    {
                        string str = *it;
                        SendFrame(client, str);
                        MessagesOut.erase(it);

                        break;
                    }
                }
                else
                {
                    SendFrame(client, incoming);
                }
                continue;
            }   

            lock_guard<mutex> inLock(mutex_array_in);
            MessagesIn.push_back(incoming);
        }
        catch (...)
        {

        }
    }

    return 1;
}

int SocketWorkPlace()
{
    WSADATA WSAData;
    SOCKET server, client;
    SOCKADDR_IN serverAddr, clientAddr;

    if (WSAStartup(MAKEWORD(2, 0), &WSAData) != 0) {
        cout << apiClient.GetDateTimeNow() << "WSAStartup failed with error:" << WSAGetLastError() << endl;
        return -1;
    }

    server = socket(AF_INET, SOCK_STREAM, 0);
    if (server == INVALID_SOCKET) {
        cout << apiClient.GetDateTimeNow() << "Socket creation failed with error:" << WSAGetLastError() << endl;
        return -1;
    }

    serverAddr.sin_addr.s_addr = INADDR_ANY;
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_port = htons(5556);

    if (::bind(server, (SOCKADDR*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR) {
        cout << apiClient.GetDateTimeNow() << "Bind function failed with error: " << WSAGetLastError() << endl;
        return -1;
    }

    if (listen(server, 0) == SOCKET_ERROR) {
        cout << apiClient.GetDateTimeNow() << "Listen function failed with error:" << WSAGetLastError() << endl;
        return -1;
    }

    cout << apiClient.GetDateTimeNow() << "Listening for incoming connections...." << endl;

    int clientAddrSize = sizeof(clientAddr);

    if ((client = accept(server, (SOCKADDR*)&clientAddr, &clientAddrSize)) != INVALID_SOCKET) {
        cout << apiClient.GetDateTimeNow() << "Client connected!" << endl;

        DWORD tid;
        HANDLE t1 = CreateThread(NULL, 0, serverReceive, &client, 0, &tid);
        if (t1 == NULL) {
            cout << apiClient.GetDateTimeNow() << "Thread Creation Error: " << WSAGetLastError() << endl;
        }

        else {
            WaitForSingleObject(t1, INFINITE);
            CloseHandle(t1);
        }

        closesocket(client);

        if (closesocket(server) == SOCKET_ERROR) {
            cout << apiClient.GetDateTimeNow() << "Close socket failed with error: " << WSAGetLastError() << endl;
            return -1;
        }

        cout << apiClient.GetDateTimeNow() << "Client disconnected!" << endl;

        WSACleanup();
    }

    return -1;
}

void ThreadWorkerPlace()
{
    while (true)
    {
        try 
        {
            SocketWorkPlace();
        }
        catch (const std::exception& e)
        {
            cout << apiClient.GetDateTimeNow() << "Exception ThreadWorkerPlace Exception: " << e.what() << endl;
        }
    }
}

#pragma endregion

#pragma region Quaries

void GetPortfolio()
{
    apiClient.ReqQryFund();
}

void GetPositions()
{
    apiClient.ReqQryPosition();
}

void GetSecuritiesList()
{
    try {
        vector<DstarApiContractField> listContractIndex = apiClient.GetListContractIndex();

        string msg;
        msg.reserve(listContractIndex.size() * 500);

        for (vector<DstarApiContractField>::iterator it = listContractIndex.begin(); it != listContractIndex.end(); it++)
        {
            DstarApiContractField obj = *it;

            msg.append("{\"exchangeId\":\"");
            msg.push_back(obj.ExchangeId);
            msg.append("\", \"commodityType\":\"");
            msg.push_back(obj.CommodityType);
            msg.append("\", \"contractIndex\":\"");
            msg.append(to_string(obj.ContractIndex));
            msg.append("\", \"contractSize\":\"");
            msg.append(apiClient.DoubleToString(obj.ContractSize));
            msg.append("\", \"contractNo\":\"");
            msg.append(obj.ContractNo);
            msg.append("\", \"contractTickSize\":\"");
            msg.append(apiClient.DoubleToString(obj.ContractTickSize));
            msg.append("\", \"preSettlePrice\":\"");
            msg.append(apiClient.DoubleToString(obj.PreSettlePrice));
            msg.append("\", \"limitUpPrice\":\"");
            msg.append(apiClient.DoubleToString(obj.LimitUpPrice));
            msg.append("\", \"limitDownPrice\":\"");
            msg.append(apiClient.DoubleToString(obj.LimitDownPrice));
            msg.append("\", \"expDate\":\"");
            msg.append(obj.ExpDate);
            msg.append("\"}");

            if (next(it) != listContractIndex.end())
            {
                msg += ",";
            }
        }

        string str = "{\"type\":\"security\",\"list\":[" + msg + "]}";

        lock_guard<mutex> outLock(mutex_array_out);
        MessagesOut.push_back(str);
    }
    catch (const std::exception& e)
    {
        cout << apiClient.GetDateTimeNow() << "Exception GetSecuritiesList error: " << e.what() << endl;
    }
}

struct ResponceMessagePlaceOrder
{
    string symbol;
    string symbolIndex;
    string side;
    string price;
    string volume;
    string numberUser;
    string offset;
    string orderType;
    string hedge;
};

void from_json(const nlohmann::json& json, ResponceMessagePlaceOrder& responce)
{
    json.at("symbol").get_to(responce.symbol);
    json.at("symbolIndex").get_to(responce.symbolIndex);
    json.at("side").get_to(responce.side);
    json.at("price").get_to(responce.price);
    json.at("volume").get_to(responce.volume);
    json.at("numberUser").get_to(responce.numberUser);
    json.at("offset").get_to(responce.offset);
    json.at("orderType").get_to(responce.orderType);
    json.at("hedge").get_to(responce.hedge);
}

DstarApiOffsetType GetOffset(const string& offset)
{
    if (offset == "Open")
    {
        return DSTAR_API_OFFSET_OPEN;
    }
    else if (offset == "Close")
    {
        return DSTAR_API_OFFSET_CLOSE;
    }
    else if (offset == "None")
    {
        return DSTAR_API_OFFSET_CLOSETODAY;
    }
}

void PlaceOrder(string* str)
{
    try {
        nlohmann::json json = nlohmann::json::parse(*str);
        ResponceMessagePlaceOrder responce = json.get<ResponceMessagePlaceOrder>();

        DstarApiReqOrderInsertField req = { 0 };
        req.Direct = responce.side == "Buy" ? DSTAR_API_DIRECT_BUY : DSTAR_API_DIRECT_SELL;
        req.Offset = responce.offset == "Open" ? DSTAR_API_OFFSET_OPEN : DSTAR_API_OFFSET_CLOSE;
        //req.Offset = GetOffset(responce.offset);
        req.Hedge = responce.hedge == "Speculate" ? DSTAR_API_HEDGE_SPECULATE : DSTAR_API_HEDGE_HEDGE;
        req.Hedge = DSTAR_API_HEDGE_HEDGE;
        req.OrderType = responce.orderType == "Limit" ? DSTAR_API_ORDERTYPE_LIMIT : DSTAR_API_ORDERTYPE_MARKET;
        req.ValidType = DSTAR_API_VALID_GFD;
        req.Reference = stoi(responce.numberUser);
        req.SeatIndex = 1;
        req.AccountIndex = apiClient.GetAccountIndex();
        req.ContractIndex = stoi(responce.symbolIndex);
        strncpy(req.ContractNo, responce.symbol.c_str(), sizeof(DstarApiContractNoType) - 1);
        req.OrderQty = stoi(responce.volume);
        req.OrderPrice = stod(responce.price);
        req.MinQty = 0;

        apiClient.ReqOrderInsert(&req);
    }
    catch (const std::exception& e)
    {
        cout << apiClient.GetDateTimeNow() << "Exception PlaceOrder error: " << e.what() << endl;
    }
}

struct ResponceMessageCancelOrder
{
    string orderId;    
};

void from_json(const nlohmann::json& json, ResponceMessageCancelOrder& responce)
{
    json.at("orderId").get_to(responce.orderId);
}

void CancelOrder(string* str)
{
    try {
        nlohmann::json json = nlohmann::json::parse(*str);
        ResponceMessageCancelOrder responce = json.get<ResponceMessageCancelOrder>();

        DstarApiReqOrderDeleteField req = { 0 };
        req.AccountIndex = apiClient.GetAccountIndex();
        req.SeatIndex = 1;
        req.OrderId = stoll(responce.orderId);
       
        apiClient.ReqOrderDelete(&req);
    }
    catch (const std::exception& e)
    {
        cout << apiClient.GetDateTimeNow() << "Exception CancelOrder error: " << e.what() << endl;
    }
}

struct ResponceMessageConnect
{
    string cmd;
    string accountId;
    string password;
    string appId;
    string authCode;
    string tradeServerUrl;
    string tradeServerPort;
};

void from_json(const nlohmann::json& json, ResponceMessageConnect& responce)
{
    json.at("cmd").get_to(responce.cmd);
    json.at("accountId").get_to(responce.accountId);
    json.at("password").get_to(responce.password);
    json.at("appId").get_to(responce.appId);
    json.at("authCode").get_to(responce.authCode);
    json.at("tradeServerUrl").get_to(responce.tradeServerUrl);
    json.at("tradeServerPort").get_to(responce.tradeServerPort);
}

int Connection(string* str)
{
    try 
    {        
        if (apiClient.CreateApi() != 0)
        {
            return 1;
        }

        if (*str == "test")
        {
            apiClient.SetAddress(front_ip, front_port);
            apiClient.SetUser(account, passwd, app_id, license_no);
        }
        else
        {
            nlohmann::json json = nlohmann::json::parse(*str);
            ResponceMessageConnect responce = json.get<ResponceMessageConnect>();

            apiClient.SetAddress(responce.tradeServerUrl.c_str(), stoi(responce.tradeServerPort));
            apiClient.SetUser(responce.accountId.c_str(), responce.password.c_str(), responce.appId.c_str(), responce.authCode.c_str());
        }

        int ret = apiClient.Init();

        if (ret < 0)
        {
            cout << apiClient.GetDateTimeNow() << "Api init failed, ret=" << ret << endl;
            return 1;
        }
    }
    catch (const std::exception& e)
    {
        cout << apiClient.GetDateTimeNow() << "json/init error: " << e.what() << endl;
        return 1;
    }

    return 0;
}

void TSleep(int sec)
{
#if defined WIN32 || defined _WIN32
    Sleep(1000 * sec);
#else
    sleep(sec);
#endif
}

#pragma endregion

#pragma region Test API

void TcpInsertOrder()
{
    DstarApiReqOrderInsertField req = { 0 };
    req.Direct = direct;
    req.Offset = offset;
    req.Hedge = DSTAR_API_HEDGE_SPECULATE;
    req.OrderType = DSTAR_API_ORDERTYPE_LIMIT;
    req.ValidType = DSTAR_API_VALID_GFD;
    req.Reference = reference++;
    req.SeatIndex = 0;
    req.AccountIndex = apiClient.GetAccountIndex();
    // req.UdpAuthCode =  !!! TCPЗаполнять никакую форму не нужно.
    req.ClientReqId = client_reqid++;
    req.ContractIndex = contindex;
    strncpy(req.ContractNo, contractno, sizeof(DstarApiContractNoType) - 1);
    req.OrderQty = qty;
    req.OrderPrice = price;
    req.MinQty = min_qty;

    apiClient.ReqOrderInsert(&req);
}

void TcpInsertEnquiry()
{
    DstarApiReqOrderInsertField req = { 0 };
    req.Direct = DSTAR_API_DIRECT_ALL;
    req.OrderType = DSTAR_API_ORDERTYPE_ENQUIRY;
    req.Reference = reference++;
    req.SeatIndex = 1;
    req.AccountIndex = apiClient.GetAccountIndex();
    // req.UdpAuthCode =  !!! TCP报单无需填写
    req.ClientReqId = client_reqid++;
    req.ContractIndex = contindex;
    strncpy(req.ContractNo, contractno, sizeof(DstarApiContractNoType) - 1);

    apiClient.ReqOrderInsert(&req);
}

void TcpInsertOffer(char* enquiryno)
{
    DstarApiReqOfferInsertField req = { 0 };
    req.BuyOffset = offset;
    req.SellOffset = offset;
    req.SeatIndex = 1;
    req.AccountIndex = apiClient.GetAccountIndex();
    // req.UdpAuthCode =  !!! TCP报单无需填写
    req.ClientReqId = client_reqid++;
    req.ContractIndex = contindex;
    strncpy(req.ContractNo, contractno, sizeof(DstarApiContractNoType) - 1);
    req.OrderQty = qty;
    req.BuyPrice = 14900;
    req.SellPrice = 15100;
    req.Reference = reference++;
    //询价号
    if (enquiryno != NULL && strlen(enquiryno) > 0) {
        strncpy(req.EnquiryNo, enquiryno, sizeof(DstarApiSystemNoType) - 1);
    }

    apiClient.ReqOfferInsert(&req);
}

void TcpInsertOfferNew(char* enquiryno)
{
    DstarApiReqOfferInsertNewField req = { 0 };
    req.BuyOffset = offset;
    req.SellOffset = offset;
    req.SeatIndex = 1;
    req.AccountIndex = apiClient.GetAccountIndex();
    // req.UdpAuthCode =  !!! TCP报单无需填写
    req.ClientReqId = client_reqid++;
    req.ContractIndex = contindex;
    strncpy(req.ContractNo, contractno, sizeof(DstarApiContractNoType) - 1);
    req.BuyOrderQty = qty;
    req.SellOrderQty = qty;
    req.BuyPrice = 14900;
    req.SellPrice = 15100;
    req.Reference = reference++;
    // req.ReplaceId = DSTAR_API_REPLACE_NORMAL;
    // req.ReplaceId = DSTAR_API_REPLACE_LAST;
    //询价号
    if (enquiryno != NULL && strlen(enquiryno) > 0) {
        strncpy(req.EnquiryNo, enquiryno, sizeof(DstarApiSystemNoType) - 1);
    }

    apiClient.ReqOfferInsertNew(&req);
}

void TcpInsertOptionExec()
{
    DstarApiReqOrderInsertField req = { 0 };
    req.OrderType = DSTAR_API_ORDERTYPE_ABANDON;
    req.Reference = reference++;
    req.SeatIndex = 1;
    req.AccountIndex = apiClient.GetAccountIndex();
    // req.UdpAuthCode =  !!! TCP报单无需填写
    req.ClientReqId = client_reqid++;
    req.ContractIndex = contindex;
    strncpy(req.ContractNo, contractno, sizeof(DstarApiContractNoType) - 1);
    req.OrderQty = 1;
    apiClient.ReqOrderInsert(&req);
}

void TcpInsertCmbOrder()
{
    DstarApiReqCmbOrderInsertField req = { 0 };
    req.Direct = direct;
    req.Offset = offset;
    req.Hedge = DSTAR_API_HEDGE_SPECULATE;
    req.OrderType = DSTAR_API_ORDERTYPE_LIMIT;
    req.ValidType = DSTAR_API_VALID_IOC;
    req.Reference = reference++;
    req.SeatIndex = 1;
    req.AccountIndex = apiClient.GetAccountIndex();
    // req.UdpAuthCode =  !!! TCP报单无需填写
    req.ClientReqId = client_reqid++;
    req.ContractIndex1 = cmb_contindex1;
    strncpy(req.ContractNo1, cmb_contractno1, sizeof(DstarApiContractNoType) - 1);
    req.ContractIndex2 = cmb_contindex2;
    strncpy(req.ContractNo2, cmb_contractno2, sizeof(DstarApiContractNoType) - 1);
    req.OrderQty = qty;
    req.OrderPrice = cmb_price;
    req.MinQty = min_qty;
    apiClient.ReqCmbOrderInsert(&req);
}

void TcpDeleteOrder(DstarApiOrderIdType orderid, char* sysno)
{
    DstarApiReqOrderDeleteField req = { 0 };
    req.AccountIndex = apiClient.GetAccountIndex();
    // req.UdpAuthCode =  !!! TCP报单无需填写
    req.ClientReqId = client_reqid++;
    req.Reference = reference++;
    req.SeatIndex = 0;
    req.OrderId = orderid;
    //系统号
    if (sysno != NULL && strlen(sysno) > 0)
    {
        strncpy(req.SystemNo, sysno, sizeof(DstarApiSystemNoType) - 1);
    }
    apiClient.ReqOrderDelete(&req);
}

void Test()
{
    string str = "test";

    Connection(&str);

    apiClient.ReqQryFund(); // request deposit
    //apiClient.ReqQryPosition(); // request positions
    //TcpInsertOrder() // place order

    while (1)
    {
        TSleep(5);
    }

    return;
}

#pragma endregion

int main(int argc, char *argv[])
{
    //Test();
    //return 0;

    thread thread(ThreadWorkerPlace);
       
    std::chrono::milliseconds timespan(10);
    bool isStarted = false;

    while (true)
    {
        try
        {
            std::this_thread::sleep_for(timespan);

            if (isDisconnected)
            {
                return -1;
            }

            list<string>::iterator it;

            mutex_array_in.lock();

            for (it = MessagesIn.begin(); it != MessagesIn.end(); it++)
            {
                string& str = *it;

                if (str.find("{\"cmd\":\"connect\"") != string::npos)
                {
                    cout << apiClient.GetDateTimeNow() << "Client -> {\"cmd\":\"connect\....\"" << endl;
                    isStarted = Connection(&str);
                }
                else if (str.find("{\"cmd\":\"disconnect\"") != string::npos)
                {
                    cout << apiClient.GetDateTimeNow() << "Client -> " << str << endl;
                    isDisconnected = true;
                }
                else if (str == "getPortfolio")
                {
                    cout << apiClient.GetDateTimeNow() << "Client -> " << str << endl;
                    GetPortfolio();
                }
                else if (str == "getPositions")
                {
                    cout << apiClient.GetDateTimeNow() << "Client -> " << str << endl;
                    GetPositions();
                }
                else if (str == "getSecurities")
                {
                    cout << apiClient.GetDateTimeNow() << "Client -> " << str << endl;
                    GetSecuritiesList();
                }
                else if (str.find("{\"cmd\":\"placeOrder\"") != string::npos)
                {
                    cout << apiClient.GetDateTimeNow() << "Client -> " << str << endl;
                    PlaceOrder(&str);
                }
                else if (str.find("{\"cmd\":\"cancelOrder\"") != string::npos)
                {
                    cout << apiClient.GetDateTimeNow() << "Client -> " << str << endl;
                    CancelOrder(&str);
                }

                MessagesIn.erase(it);

                break;
            }

            mutex_array_in.unlock();

            if (isStarted == false)
            {
                continue;
            }
        }
        catch (...)
        {
        }
    }
}