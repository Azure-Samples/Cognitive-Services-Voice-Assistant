#!/bin/bash
clear
cd ..
mkdir out
mkdir SpeechSDK

echo "Downloading Speech SDK binaries"
wget -c https://aka.ms/csspeech/linuxbinary -O - | tar -xz -C ./SpeechSDK

echo "Copying SDK binaries to lib folder and headers to include"
cp -Rf ./SpeechSDK/SpeechSDK*/* .

echo "Building Raspberry Pi sample"
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
-L./lib/arm32 \
-I./include/cxx_api \
-I./include/c_api \
-I./include \
-pthread \
-lstdc++fs \
-lasound \
-lMicrosoft.CognitiveServices.Speech.core

cp ./scripts/run.sh ./out
chmod +x ./out/run.sh

echo Done. To start the demo execute:
echo cd ../out
echo export LD_LIBRARY_PATH="../lib/arm32"
echo ./sample.exe [path_to_configFile]
