@echo off
setlocal

set imageId=74a75a0a9a27
set dockerRunEnv=--rm --workdir /nf --volume %cd%:/nf %imageId%
set buildCmd=docker run %dockerRunEnv% g++ 
REM -march=armv6 -mfpu=vfp -marm -Wl,-latomic

set inc=-I include %inc%
set inc=-I include/cxx_api %inc%
set inc=-I include/c_api %inc%

set lib=-lMicrosoft.CognitiveServices.Speech.core %lib%
REM set lib=-lpma %lib%
set lib=-lpthread %lib%
set lib=-lasound %lib%
REM set lib=-l:libcutils.so.0 %lib%

set commonTargets=-std=c++14 %inc% -L lib/arm32 %lib%

set src=src/AudioPlayerEntry.cpp %src%
set src=src/LinuxAudioPlayer.cpp %src%
set src=src/mainAudio.cpp %src%
set src=src/AgentConfiguration.cpp %src%
set tgt=out/sample.exe


echo Building (this is slow): %tgt%
echo build command = %buildCmd% -o %tgt% %src% %commonTargets%
%buildCmd% -o %tgt% %src% %commonTargets%