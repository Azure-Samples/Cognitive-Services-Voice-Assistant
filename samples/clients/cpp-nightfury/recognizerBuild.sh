#!/bin/bash
clear
echo "Building Raspberry Pi recognizer sample"
g++ mainRecognizer.cpp -o recognizer \
-std=c++14 \
-L./lib/arm32 \
-I./include/cxx_api \
-I./include/c_api \
-I./include \
-pthread \
-lasound \
-lMicrosoft.CognitiveServices.Speech.core