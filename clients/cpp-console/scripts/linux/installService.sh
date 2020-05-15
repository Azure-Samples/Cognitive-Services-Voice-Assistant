#!/bin/bash

cp ../service/VoiceAssistant.service /lib/systemd/system/VoiceAssistant.service
cp ../service/VoiceAssistant.timer /lib/systemd/system/VoiceAssistant.timer
mkdir /data
mkdir /data/cppSample

cp ../../lib/arm32/* /data/cppSample
cp ../../out/sample.exe /data/cppSample
cp ../../configs/config.json /data/cppSample
cp ../../../../keyword-models/* /data/cppSample
cp ../service/startService.sh /data/cppSample/startService.sh
chmod +x /data/cppSample/sample.exe
chmod +x /data/cppSample/startService.sh

systemctl enable VoiceAssistant.timer
systemctl daemon-reload