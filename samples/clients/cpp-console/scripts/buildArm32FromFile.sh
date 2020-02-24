#!/bin/bash
clear
cd ..
mkdir out
echo "Building Raspberry Pi arm32 from file sample"
g++ -Wno-psabi src/mainAudioFromFile.cpp src/LinuxAudioPlayer.cpp src/AudioPlayerEntry.cpp -o ./out/sample.exe \
-std=c++14 \
-D LINUX \
-I./include/cxx_api \
-I./include/c_api \
-I./include \
-L./lib/arm32 \
-lMicrosoft.CognitiveServices.Speech.core \
-lasound \
-pthread \
