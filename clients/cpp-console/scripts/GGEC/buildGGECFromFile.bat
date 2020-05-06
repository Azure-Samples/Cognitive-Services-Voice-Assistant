@echo off
setlocal
cd ..\..
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
set lib=-lstdc++fs %lib%
REM set lib=-l:libcutils.so.0 %lib%

set commonTargets=-std=c++14 %inc% %incDir% %lib%

set src=src/GGEC/GGECLinuxAudioPlayer.cpp %src%
set src=src/GGEC/GGECDeviceStatusIndicators.cpp %src%
set src=src/common/AudioPlayerEntry.cpp %src%
set src=src/common/mainAudioFromFile.cpp %src%
set src=src/common/AgentConfiguration.cpp %src%
set src=src/common/DialogManager.cpp %src%
set tgt=out/sample.exe

set defines=-D LINUX

echo Building (this is slow): %tgt%
set finalCommand=%buildCmd% -o %tgt% %src% %commonTargets% %defines%
echo build command = %finalCommand%
%finalCommand%