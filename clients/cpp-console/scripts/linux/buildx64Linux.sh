#!/bin/bash
clear
cd ../..
mkdir out
mkdir SDK

echo "Cleaning up libs and include directories that we will overwrite"
rm -R ./lib/*
rm -R ./include/c_api
rm -R ./include/cxx_api

echo "Downloading Speech SDK binaries"
wget -c https://aka.ms/csspeech/linuxbinary -O - | tar -xz -C ./SpeechSDK

echo "Copying SDK binaries to lib folder and headers to include"
cp -Rf ./SpeechSDK/SpeechSDK*/* .

echo "Building Raspberry Pi x64 sample"
g++ -Wno-psabi \
src/common/Main.cpp \
src/linux/LinuxAudioPlayer.cpp \
src/common/AudioPlayerEntry.cpp \
src/common/AgentConfiguration.cpp \
src/common/DeviceStatusIndicators.cpp \
src/common/DialogManager.cpp \
-o ./out/sample.exe \
-std=c++14 \
-D LINUX \
-L./lib/x64 \
-I./include/cxx_api \
-I./include/c_api \
-I./include \
-pthread \
-lstdc++fs \
-lasound \
-lMicrosoft.CognitiveServices.Speech.core

echo Cleaning up downloaded files
rm -R ./SDK

cp ./scripts/run.sh ./out
chmod +x ./out/run.sh
