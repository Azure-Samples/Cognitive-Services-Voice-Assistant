@echo off
setlocal enabledelayedexpansion

set progName=sample.exe
set outDir=/data/cppSample/

if "%1%" == "flags" (
  adb shell mkdir !outDir!
  for /f %%i in ('dir /b lib\arm32flags') do adb push lib\arm32flags\%%i !outDir!
  for /f %%i in ('dir /b configs') do adb push configs\%%i !outDir!
  for /f %%i in ('dir /b models') do adb push models\%%i !outDir!
) else (
  adb shell mkdir !outDir!
  for /f %%i in ('dir /b lib\arm32') do adb push lib\arm32\%%i !outDir!
  for /f %%i in ('dir /b configs') do adb push configs\%%i !outDir!
  for /f %%i in ('dir /b models') do adb push models\%%i !outDir!
)

adb push out\%progName% %outDir%
adb push run.sh %outDir%

adb shell chmod +x %outDir%%progName%
adb shell chmod +x %outDir%run.sh
endlocal