@echo off
setlocal
cd ..

set outDir=/data/microsoftNightfury/
set progName=sample.exe

adb shell mkdir %outDir%
adb push %progName% %outDir%
adb push ./scripts/runMicrosoftNightfury.sh %outDir%

if "%1%" == "all" (
  for /f %%i in ('dir /b lib\arm32') do adb push lib\arm32\%%i %outDir%
  for /f %%i in ('dir /b configs') do adb push configs\%%i %outDir%
  for /f %%i in ('dir /b models') do adb push models\%%i %outDir%
)

adb shell chmod +x %outDir%%progName%
adb shell chmod +x %outDir%runMicrosoftNightfury.sh