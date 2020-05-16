#!/bin/bash

cp ../service/VoiceAssistant.service /lib/systemd/system/VoiceAssistant.service
cp ../service/VoiceAssistant.timer /lib/systemd/system/VoiceAssistant.timer

if [ ! -d /data/cppSample ]; then
    mkdir -p /data/cppSample # only create directory if does not exist
fi

cp ../../lib/arm32/* /data/cppSample
cp ../../out/sample.exe /data/cppSample
cp ../../configs/* /data/cppSample
cp ../../../../keyword-models/* /data/cppSample
cp ../service/startService.sh /data/cppSample/startService.sh
chmod +x /data/cppSample/sample.exe
chmod +x /data/cppSample/startService.sh

systemctl enable VoiceAssistant.timer
systemctl daemon-reload