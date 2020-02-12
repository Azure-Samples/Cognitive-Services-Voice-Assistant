@echo off
setlocal
cd ..
mkdir out

set imageId=dev_ubuntu_arm32
set dockerRunEnv=--rm --workdir /nf --volume %cd%:/nf %imageId%
set buildCmd=docker run %dockerRunEnv% g++
set incDir=-L lib/arm32

set inc=-I include %inc%
set inc=-I include/cxx_api %inc%
set inc=-I include/c_api %inc%

set lib=-lMicrosoft.CognitiveServices.Speech.core %lib%
set lib=-lpma %lib%
set lib=-lpthread %lib%
set lib=-lasound %lib%
REM set lib=-l:libcutils.so.0 %lib%

set commonTargets=-std=c++14 %inc% %incDir% %lib%

set src=src/AudioPlayerEntry.cpp %src%
set src=src/nightfury/LinuxAudioPlayer.cpp %src%
set src=src/nightfury/mainAudioFromFile.cpp %src%
set src=src/nightfury/DeviceStatusIndicators.cpp %src%
set src=src/AgentConfiguration.cpp %src%
set tgt=out/sample.exe


echo Building (this is slow): %tgt%
echo build command = %buildCmd% -o %tgt% %src% %commonTargets%
%buildCmd% -o %tgt% %src% %commonTargets%