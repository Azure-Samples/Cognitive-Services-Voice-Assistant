#!/bin/bash
clear
cd ..
mkdir out
echo "Building Raspberry Pi arm32 from file sample"
g++ -Wno-psabi \
src/common/mainAudioFromFile.cpp \
src/linux/LinuxAudioPlayer.cpp \
src/common/AudioPlayerEntry.cpp \
src/common/AgentConfiguration.cpp \
src/common/DeviceStatusIndicators.cpp \
src/common/DialogConnector.cpp \
src/common/helper.cpp \
-o ./out/sample.exe \
-std=c++14 \
-D LINUX \
-L./lib/arm32 \
-I./include/cxx_api \
-I./include/c_api \
-I./include \
-pthread \
-lstdc++fs \
-lasound \
-lMicrosoft.CognitiveServices.Speech.core