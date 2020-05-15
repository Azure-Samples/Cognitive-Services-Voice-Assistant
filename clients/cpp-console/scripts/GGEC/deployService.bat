adb push ..\service\VoiceAssistant.service /lib/systemd/system/VoiceAssistant.service
adb push ..\service\VoiceAssistant.timer /lib/systemd/system/VoiceAssistant.timer

adb shell mkdir /data/cppSample
adb push ..\service\startService.sh /data/cppSample/startService.sh
adb shell chmod +x /data/cppSample/startService.sh

adb shell systemctl enable VoiceAssistant.timer
adb shell systemctl daemon-reload