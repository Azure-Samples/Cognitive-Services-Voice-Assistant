#!/bin/bash
clear
echo "Building cpp x64 sample"
g++ ../src/mainAudio.cpp \
../src/LinuxAudioPlayer.cpp \
../src/AudioPlayerEntry.cpp \
../src/AgentConfiguration.cpp \
-o ../out/sample.exe \
-std=c++14 \
-I../include/cxx_api \
-I../include/c_api \
-I../include \
-L../lib/x64 \
-lMicrosoft.CognitiveServices.Speech.core \
-pthread \
