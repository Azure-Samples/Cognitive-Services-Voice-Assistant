@echo off
setlocal
cd ..
set imageId=74a75a0a9a27
set dockerRunEnv=--rm --workdir /nf --volume %cd%:/nf %imageId%
set buildCmd=docker run %dockerRunEnv% g++
set incDir=-L lib/arm32
if "%1%" == "flags" (
    set buildCmd=%buildCmd% -march=armv6 -mfpu=vfp -marm -Wl,-latomic
    set incDir=-L lib/arm32flags
)


set inc=-I include %inc%
set inc=-I include/cxx_api %inc%
set inc=-I include/c_api %inc%

set lib=-lMicrosoft.CognitiveServices.Speech.core %lib%
set lib=-lpma %lib%
set lib=-lpthread %lib%
set lib=-lasound %lib%
REM set lib=-l:libcutils.so.0 %lib%

set defines=-D NIGHTFURY

set commonTargets=-std=c++14 %inc% %incDir% %lib%

set src=src/AudioPlayerEntry.cpp %src%
set src=src/LinuxAudioPlayer.cpp %src%
set src=src/mainAudioFromFile.cpp %src%
set src=src/AgentConfiguration.cpp %src%
set tgt=out/sample.exe


echo Building (this is slow): %tgt%
echo build command = %buildCmd% -o %tgt% %src% %commonTargets% %defines%
%buildCmd% -o %tgt% %src% %commonTargets% %defines%