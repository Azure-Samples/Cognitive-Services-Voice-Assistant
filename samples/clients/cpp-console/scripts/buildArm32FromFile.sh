#!/bin/bash
clear
cd ..
mkdir out
echo "Building Raspberry Pi arm32 from file sample"
g++ -Wno-psabi \
src/mainAudioFromFile.cpp \
src/LinuxAudioPlayer.cpp \
src/AudioPlayerEntry.cpp \
src/AgentConfiguration.cpp \
src/DeviceStatusIndicators.cpp \
-o ./out/sample.exe \
-std=c++14 \
-D LINUX \
-L./lib/arm32110 \
-I./include/cxx_api \
-I./include/c_api \
-I./include \
-pthread \
-lasound \
-lMicrosoft.CognitiveServices.Speech.core