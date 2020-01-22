#!/bin/bash
clear
echo "Building Raspberry Pi x64 sample"
g++ main.cpp -o sample.exe \
-std=c++14 \
-I./include/cxx_api \
-I./include/c_api \
-I./include \
-L./lib/x64 \
-lMicrosoft.CognitiveServices.Speech.core \
-pthread \
