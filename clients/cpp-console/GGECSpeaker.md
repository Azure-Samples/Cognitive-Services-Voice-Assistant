# Microsoft Cognitive Services - Voice Assistant Sample Code C++ console GGEC Speaker setup

## Overview

This readme should go over setting up a Windows dev box to build an arm32 binary using a docker container for the GGEC speaker. You could build it on a Linux machine but the scripts provided are for Windows.

## Setting up the device

GGEC instructions for setting up the device [GGEC instructions](link).

You will also need the Android Debug Bridge (adb) which can be found [here](https://developer.android.com/studio/releases/platform-tools).

## Setting up the code

The repo should be cloned onto your dev machine and we will operate out of the cpp-console folder

Since this device is unique we will also need the specific binaries NOT the general Microsoft.SpeechSDK binaries. This is call the Microsoft Speech DEVICES SDK. The device binaries can be found here: [binaries](https://aka.ms/sdsdk-download)

The native binaries should be copied to the lib folder in this repo so that you have a structure like this (for ARM32): ./lib/arm32. 

Headers and their folders should be copied into the include folder so that you have a structure like this: ./include/cxx_api and ./include/c_api

Replace the text in the configs/config.json file with your subscription key and key region. If you are using a custom speech commands application or custom speech font you can insert those GUID's there as well.

## Building for Linux Arm32 with Docker

For ARM32 devices you can compile binaries on a windows machine using docker.
Install docker for windows from the [docker website](https://docs.docker.com/docker-for-windows/).

Download the qemu-arm-static.tar.gz file from this [open source](https://github.com/multiarch/qemu-user-static/releases/) and place it in the docker folder. This is an arm emulator the container will use.

For linux machines you will also need to run 

    sudo apt-get install --yes binfmt-support qemu-user-static

Open a cmd prompt and cd into the docker folder and 
run 

    docker build -t dev_ubuntu_arm32 .

This will create an image and name it "dev_ubuntu_arm32" which is used inside the build scripts.

cd into the scripts/GGEC directory

run .\buildGGEC.bat

This should spin up the docker container and run the build command. The output executable will be placed in the out folder.

If you deploy those files from the out dir and copy an existing or create a config file you should now be able to run it. We have a script that will do it for you called "deployGGEC.bat". This will deploy all the configs, models, and binaries you will need along with the run.sh script into the /data/cppSample folder on your device.

## Running the sample

### usage: run.sh config-file
example running from the /data/cppSample folder:
    
    ./run.sh config.json
    
## Setting up the sample to run as a service

This can be useful if you want your speaker to start the sample automatically on boot and have it automatically restart if it fails.

cd into the scripts/GGEC directory

Change the startService.sh file to use the config.json file you would like. Then run

    .\deployService.bat

Then reboot your device.


#### [Main Devices Readme](README.md)
