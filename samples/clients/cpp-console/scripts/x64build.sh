#!/bin/bash
clear
cd ..
mkdir out
echo "Building x64 sample"
g++ src/mainAudio.cpp src/LinuxAudioPlayer.cpp src/AudioPlayerEntry.cpp src/AgentConfiguration.cpp -o ./out/sample.exe \
-std=c++14 \
-L./lib/arm32 \
-I./include/cxx_api \
-I./include/c_api \
-I./include \
-pthread \
-lasound \
-lMicrosoft.CognitiveServices.Speech.core

