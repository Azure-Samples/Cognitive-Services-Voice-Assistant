adb push .\HospitalityDemo.service /lib/systemd/system/HospitalityDemo.service
adb push .\HospitalityDemo.timer /lib/systemd/system/HospitalityDemo.timer
adb push .\startService.sh /data/cppSample/startService.sh
adb shell mkdir /data/cppSample
adb shell chmod +x /data/cppSample/startService.sh

adb shell systemctl enable HospitalityDemo.timer
adb shell systemctl daemon-reload