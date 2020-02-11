#!/bin/bash
clear
cd ..
echo "Building Raspberry Pi sample"
g++ -Wno-psabi src/mainAudio.cpp src/LinuxAudioPlayer.cpp src/AudioPlayerEntry.cpp src/AgentConfiguration.cpp -o ./out/sample.exe \
-std=c++14 \
-L./lib/arm32 \
-I./include/cxx_api \
-I./include/c_api \
-I./include \
-pthread \
-lasound \
-lMicrosoft.CognitiveServices.Speech.core

