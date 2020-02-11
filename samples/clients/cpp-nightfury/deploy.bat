@echo off
setlocal

set outDir=/data/cppSampleOld/
set progName=sample.exe

adb shell mkdir %outDir%
adb push out\%progName% %outDir%
adb push run.sh %outDir%

if "%1%" == "all" (
  for /f %%i in ('dir /b lib\arm32') do adb push lib\arm32\%%i %outDir%
  for /f %%i in ('dir /b configs') do adb push configs\%%i %outDir%
  for /f %%i in ('dir /b models') do adb push models\%%i %outDir%
)

adb shell chmod +x %outDir%%progName%
adb shell chmod +x %outDir%run.sh