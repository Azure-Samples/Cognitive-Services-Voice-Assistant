#!/bin/bash
clear
cd ../..
if [ ! -d out ]; then
    mkdir out # only create directory if does not exist
fi
if [ ! -d SDK ]; then
    mkdir SDK # only create directory if does not exist
fi

if [ ! -f ./lib/arm32/libMicrosoft.CognitiveServices.Speech.core ]; then
echo "Cleaning up libs and include directories that we will overwrite"
rm -R ./lib/*
rm -R ./include/c_api
rm -R ./include/cxx_api

echo "Downloading Speech SDK binaries"
wget -c https://aka.ms/csspeech/linuxbinary -O - | tar -xz -C ./SDK

echo "Copying SDK binaries to lib folder and headers to include"
cp -Rf ./SDK/SpeechSDK*/* .
else
echo "Speech SDK lib found skipping download"
fi


echo "Building Linux Arm32 sample"
if g++ -Wno-psabi \
src/common/Main.cpp \
src/linux/LinuxAudioPlayer.cpp \
src/common/AudioPlayerEntry.cpp \
src/common/AgentConfiguration.cpp \
src/common/DeviceStatusIndicators.cpp \
src/common/DialogManager.cpp \
-o ./out/sample.exe \
-std=c++14 \
-D LINUX \
-D MAS \
-L./lib/arm32 \
-I./include/cxx_api \
-I./include/c_api \
-I./include \
-pthread \
-lstdc++fs \
-lasound \
-lMicrosoft.CognitiveServices.Speech.core; 
then
error=0;
else
error=1;
fi

cp ./scripts/run.sh ./out
chmod +x ./out/run.sh

echo Cleaning up downloaded files
rm -R ./SDK

echo Done. To start the demo execute:
echo cd ../../out
echo export LD_LIBRARY_PATH="../lib/arm32"
echo ./sample.exe [path_to_configFile]

exit $error