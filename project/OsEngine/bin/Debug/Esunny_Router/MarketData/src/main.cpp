#include <cstdlib>
#include "apiClient.h"
#include <string>
#include <string.h>
#include <windows.h>
#include <direct.h>
#include <errno.h>
#include <stdio.h>
#include <vector>
#include <conio.h>
#include <list>
#include <cstdint>
#include <thread>
#include <chrono>
#include <mutex>
#include <iostream>
#include <nlohmann/json.hpp>

using namespace std;

list<string> MessagesIn;
list<string> MessagesOut;
mutex mutex_array_all;
mutex mutex_array_in;
mutex mutex_array_out;
bool isDisconnected = false;

apiClient* api = new apiClient();

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
                cout << api->GetDateTimeNow() << "recv function failed with error " << WSAGetLastError() << endl;
                return -1;
            }

            if (incoming.empty())
            {
                continue;
            }

            if (incoming == "Process")
            {
                lock_guard<mutex> outLock(mutex_array_out);

                if (!MessagesOut.empty())
                {
                    while (!MessagesOut.empty())
                    {
                        string out = MessagesOut.front();
                        MessagesOut.pop_front();
                        SendFrame(client, out);
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

    WSAStartup(MAKEWORD(2, 0), &WSAData);

    server = socket(AF_INET, SOCK_STREAM, 0);
    if (server == INVALID_SOCKET) {
        cout << api->GetDateTimeNow() << "Socket creation failed with error:" << WSAGetLastError() << endl;
        return -1;
    }

    serverAddr.sin_addr.s_addr = INADDR_ANY;
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_port = htons(5555);

    if (::bind(server, (SOCKADDR*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR) {
        cout << api->GetDateTimeNow() << "Bind function failed with error: " << WSAGetLastError() << endl;
        return -1;
    }

    if (listen(server, 0) == SOCKET_ERROR) {
        cout << api->GetDateTimeNow() << "Listen function failed with error:" << WSAGetLastError() << endl;
        return -1;
    }

    cout << api->GetDateTimeNow() << "Listening for incoming connections...." << endl;

    int clientAddrSize = sizeof(clientAddr);

    if ((client = accept(server, (SOCKADDR*)&clientAddr, &clientAddrSize)) != INVALID_SOCKET) {
        cout << api->GetDateTimeNow() << "Client connected!" << endl;

        DWORD tid;
        HANDLE t1 = CreateThread(NULL, 0, serverReceive, &client, 0, &tid);
        if (t1 == NULL) {
            cout << api->GetDateTimeNow() << "Thread Creation Error: " << WSAGetLastError() << endl;
        }

        WaitForSingleObject(t1, INFINITE);

        closesocket(client);

        if (closesocket(server) == SOCKET_ERROR) {
            cout << api->GetDateTimeNow() << "Close socket failed with error: " << WSAGetLastError() << endl;
            return -1;
        }

        cout << api->GetDateTimeNow() << "Client disconnected!" << endl;

        WSACleanup();
    }

    return -1;
}

void ThreadWorkerPlace()
{
    while (true)
    {
        SocketWorkPlace();
    }
}

#pragma endregion

#pragma region Queries

struct ResponceMessageConnect
{
    string cmd;    
    string dataServerUrl;
    string dataServerPort;
};

void from_json(const nlohmann::json& json, ResponceMessageConnect& responce)
{
    json.at("cmd").get_to(responce.cmd);
    json.at("dataServerUrl").get_to(responce.dataServerUrl);
    json.at("dataServerPort").get_to(responce.dataServerPort);
}

bool Connection(string* str)
{
    try 
    {
        bool bret = false;
        int ret = 0;

        cout << api->GetDateTimeNow() << "Api Version: " << api->GetVerion() << endl;
        cout << "-----------------------------------------------------" << endl;

        const char* logPath = ".\\Esunny_Router\\MarketData\\MarketDataLogs\\";
        ret = _mkdir(logPath);
        if (ret != 0 && errno != EEXIST)
        {
            cout << api->GetDateTimeNow() << "Api: failed to create log directory " << logPath << ", errno=" << errno << endl;
        }

        if (!(api->Init()))
            cout << api->GetDateTimeNow() << "Api: init error" << endl;

        api->SetCpuId(2);
        api->SetAutoRelogin(true);

        nlohmann::json json = nlohmann::json::parse(*str);
        ResponceMessageConnect responce = json.get<ResponceMessageConnect>();

        std::string ip = responce.dataServerUrl;
        unsigned short port = stoi(responce.dataServerPort);

        bret = api->SetHostAddress(ip.c_str(), port);

        if (bret)
        {
            cout << api->GetDateTimeNow() << "Api: Host set correct " << ip << ":" << port << endl;
        }
        else
        {
            cout << api->GetDateTimeNow() << "Api: Host set wrong" << endl;
        }

        api->SetApiLogPath(logPath);

        api->Login();

        auto startTimer = std::chrono::steady_clock::now();

        while (true)
        {
            if (api->isReady)
            {
                cout << api->GetDateTimeNow() << "Api: API is Ready" << endl;
                cout << "-----------------------------------------------------" << endl;

                lock_guard<mutex> outLock(mutex_array_out);
                MessagesOut.push_back("{\"type\":\"connect\"}");
                break;
            }

            auto currentTimer = std::chrono::steady_clock::now();
            auto elapsed = std::chrono::duration_cast<std::chrono::seconds>(currentTimer - startTimer);

            if (elapsed.count() >= 30)
            {
                std::cout << api->GetDateTimeNow() << "No response from the server. Disconnected due to timeout: " << elapsed.count() << " с\n";

                lock_guard<mutex> outLock(mutex_array_out);
                MessagesOut.push_back("{\"type\":\"disconnect\"}");

                return false;
            }

            Sleep(1000);
        }

        return true;
    }
    catch (const std::exception& e)
    {
        cout << api->GetDateTimeNow() << "Connection error: " << e.what() << endl;
        return false;
	}
}

struct ResponceMessageSubscribeQuote
{
    string cmd;
    string symbol;
};

void from_json(const nlohmann::json& j, ResponceMessageSubscribeQuote& responce)
{
    j.at("cmd").get_to(responce.cmd);
    j.at("symbol").get_to(responce.symbol);
}

void SubscribeQuote(const string& str)
{
    try {
        if (str == "")// test subscription
        {
            string msg = R"({"cmd":"subscribeQuote","symbol":"PL607"})";

            nlohmann::json json = nlohmann::json::parse(msg);
            ResponceMessageSubscribeQuote responce = json.get<ResponceMessageSubscribeQuote>();

            api->SubscribeQuote(responce.symbol.c_str());
        }
        else
        {
            nlohmann::json json = nlohmann::json::parse(str);
            ResponceMessageSubscribeQuote responce = json.get<ResponceMessageSubscribeQuote>();

            api->SubscribeQuote(responce.symbol.c_str());
        }
    }
    catch (const std::exception& e)
    {
        cout << api->GetDateTimeNow() << "json/init error: " << e.what() << endl;
    }
}

void Disconnect()
{
    try {
        cout << api->GetDateTimeNow() << "Api: begin to free" << endl;
        Sleep(1000);
        api->Free();
        
        cout << "Api: free success" << endl;

        delete api;
    }
    catch (const std::exception& e)
    {
        cout << api->GetDateTimeNow() << "Disconnect error: " << e.what() << endl;
    }
}

#pragma endregion

int TestRequests()
{
    bool bret = false;
    apiClient* api = new apiClient();

    int ret = 0;

    cout << api->GetDateTimeNow() << "Api Version: " << api->GetVerion() << endl;
    cout << "-----------------------------------------------------" << endl;


    // 1： API work log directory (absolute path), note the \\ separator in Windows paths.
    const char* logPath = "\\md_logs\\";
    //_mkdir("C:\\OsE");
    //_mkdir("C:\\OsE\\esunny");
    //_mkdir("\\md_logs");
    ret = _mkdir(logPath);
    if (ret != 0 && errno != EEXIST)
    {
        cout << api->GetDateTimeNow() << "Api Demo: failed to create log directory " << logPath << ", errno=" << errno << endl;
    }

    if (!(api->Init()))
        cout << api->GetDateTimeNow() << "Api Demo: init error" << endl;

    // 2: To configure CPU core settings, you must call the `open` function before calling it; otherwise, the changes will not take effect.
    api->SetCpuId(2);

    // 3: Set whether to automatically restart
    api->SetAutoRelogin(true);

    // 4: Server directory settings to be written.
    std::string IP = "61.163.243.173";
    unsigned short port = 6161;

    bret = api->SetHostAddress(IP.c_str(), port);
    if (bret)
    {
        cout << api->GetDateTimeNow() << "Api Demo: Host set correct " << IP << ":" << port << endl;
    }
    else
    {
        cout << api->GetDateTimeNow() << "Api Demo: Host set wrong" << endl;
    }

    // 5: Set log directory
    api->SetApiLogPath(logPath);

    api->Login();

    while (1)
    {
        if (api->isReady)
        {
            cout << api->GetDateTimeNow() << "Api Demo: API is Ready" << endl;
            cout << "-----------------------------------------------------" << endl;
            break;
        }
        Sleep(1000);
    }

    while (1)
    {
        if (_kbhit())   // если нажата любая клавиша
        {
            break;
        }
        Sleep(100);
    }

    // Resource release
    cout << api->GetDateTimeNow() << "Api Demo: begin to free" << endl;
    Sleep(1000);
    api->Free();

    delete api;
    cout << api->GetDateTimeNow() << "Api Demo: free success" << endl;

    system("pause");
    return 0;
}

int main(int argc, char** argv)
{
    //if (TestRequests() == 0) return 0;

    std::thread thread(ThreadWorkerPlace);

    std::chrono::milliseconds timespan(10);
    bool isStarted = false;

    while (true)
    {
        try
        {
            std::this_thread::sleep_for(timespan);

            if (isDisconnected)
            {
                break;
            }

            list<string>::iterator it;

            mutex_array_in.lock();

            for (it = MessagesIn.begin(); it != MessagesIn.end(); it++)
            {
                string str = *it;

                if (str.find("{\"cmd\":\"connect\"") != string::npos)
                {
                    cout << api->GetDateTimeNow() << "Client -> " << str << endl;
                    isStarted = Connection(&str);
                }
                else if (str.find("{\"cmd\":\"disconnect\"") != string::npos)
                {
                    cout << api->GetDateTimeNow() << "Client -> " << str << endl;
                    Disconnect();
                }
                else if (str.find("{\"cmd\":\"subscribeQuote\"") != string::npos)               
                {
                    cout << api->GetDateTimeNow() << "Client -> " << str << endl;
                    SubscribeQuote(str);                    
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

	Disconnect();

    return 0;
}
