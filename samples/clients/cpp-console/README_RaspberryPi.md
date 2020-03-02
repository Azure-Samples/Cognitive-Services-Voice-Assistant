# Microsoft Cognitive Services - Voice Assistant Sample Code C++ console Linux setup

## Overview

This readme should go over setting up a Linux OS. In our example we are using Ubuntu 18.04 on a raspberry pi

## Setting up the device

* Install the Ubuntu server OS onto your Raspberry pi. 
  * [ubuntu download](https://ubuntu.com/download/raspberry-pi)
  * [install instructions](https://www.raspberrypi.org/documentation/installation/installing-images/)

* Make sure you have speakers and a microphone attached

* Setup Alsa for Audio Playback and Recording (this part can be tricky)
  * [ALSA wiki](https://wiki.archlinux.org/index.php/Advanced_Linux_Sound_Architecture)
  * keep in mind you can test it with aplay and arecord

## Setting up the code

The repo should be cloned onto your device and we will operate out of the cpp-console folder

Make sure you have the Microsoft speech SDK downloaded. Links are in the main readme

For Linux or ARM devices the target version and the native binaries should be copied to the lib folder in this repo so that you have a structure like this (for ARM32): ./lib/arm32. 

Headers and their folders should be copied into the include folder so that you have a structure like this: ./include/cxx_api and ./include/c_api

Replace the text in the configs/config.json file with your subscription key and key region. If you are using a custom speech commands application or custom speech font you can insert those GUID's there as well.

## Build directly on Linux arm32

This assumes you have the repo's files on your device and you have the prerequisites in the proper folders. (see above)

You will need to install some packages.

    sudo apt-get install g++ libasound2-dev

cd into the scripts directory

run ./buildArm32.sh

## Running the sample

### usage: sample.exe config-file [volume on/off]
example running from the out folder:
    
    export LD_LIBRARY_PATH="../lib/arm32"
    sample.exe config.json on
