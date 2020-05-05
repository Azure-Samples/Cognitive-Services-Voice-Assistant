// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <chrono>
#include <ctime>
#include <iostream>

using namespace std;

void log();

template<typename T, typename... Args>
void log(T v, Args... args)
{
    cout << v << flush;
    log(args...);
}

template<typename T, typename... Args>
void log_t(T v, Args... args)
{
    char buff[9];
    chrono::system_clock::time_point now = chrono::system_clock::now();
    time_t now_c = chrono::system_clock::to_time_t(now);
    tm now_tm;
#ifdef LINUX
    localtime_r(&now_c, &now_tm);
#endif
#ifdef WINDOWS
    localtime_s(&now_tm, &now_c);
#endif
    strftime(buff, sizeof buff, "%H:%M:%S", &now_tm);

    cout << buff << "." << chrono::duration_cast<chrono::milliseconds>(now.time_since_epoch()).count() % 1000 << "  ";
    log(v, args...);
}
