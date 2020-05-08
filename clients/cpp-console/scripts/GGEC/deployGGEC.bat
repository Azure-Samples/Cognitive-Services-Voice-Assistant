@echo off
setlocal
cd ..\..

set progName=sample.exe
set outDir=/data/cppSample/

adb shell mkdir %outDir%
for /f %%i in ('dir /b lib\arm32') do adb push lib\arm32\%%i %outDir%
for /f %%i in ('dir /b configs') do adb push configs\%%i %outDir%
for /f %%i in ('dir /b ..\..\keyword-models') do adb push ..\..\keyword-models\%%i %outDir%

adb push out\%progName% %outDir%
adb push scripts\run.sh %outDir%

adb shell chmod +x %outDir%%progName%
adb shell chmod +x %outDir%run.sh
endlocal