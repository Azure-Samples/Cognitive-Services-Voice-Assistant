# Microsoft Cognitive Services - Voice Assistant Sample Code C++ console

## Overview

This sample is intended to be a starting point for any application coded in c++. It has some generic files that implement an IAudioPlayer interface for audio playback. This interface will be specific to the target OS and/or device. A sample player for linux devices is included.

## Prerequisites
* You will need a speech service subscription key and region. Instructions for creating one can be found on this [page](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/tutorial-voice-enable-your-bot-speech-sdk) under "Create a resource group" and "Create resources"

* The [Microsoft Speech SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-sdk) will need to be downloaded.

For Windows you should use the nuget package.

For Linux or ARM devices the target version and the native binaries should be copied to the lib folder in this repo so that you have a structure like this (for ARM32): ./lib/arm32. 

Headers and their folders should be copied into the include folder so that you have a structure like this: ./include/cxx_api and ./include/c_api

## Setup

Replace the text in the configs/config.json file with your subscription key and key region. If you are using a custom speech commands application or custom speech font you can insert those GUID's there as well.

### For the Nightfury device you will need the speech devices SDK
TODO:
This should be updated once the nightfury build is public

## Build directly on Linux arm32

TODO: Convert to link once files are in master
Check out the README_RaspberryPi.md

## Building for Linux Arm32 with Docker

For ARM32 devices you can compile binaries on a windows machine using docker.
Install docker for windows from the [docker website](https://docs.docker.com/docker-for-windows/).

Download the qemu-arm-static.tar.gz file from this [open source](https://github.com/multiarch/qemu-user-static/releases/) and place it in the docker folder.

For linux machines you will also need to run 

    sudo apt-get install --yes binfmt-support qemu-user-static

Open a cmd prompt and cd into the docker folder and 
run 

    docker build -t dev_ubuntu_arm32 .

This will create an image and name it "dev_ubuntu_arm32" which is used inside the build scripts.

cd into the scripts directory.

Then run the buildArm32Linux.bat

This should spin up the docker container and run the build command. The output executable will be placed in the out folder along with the binaries you included.

If you deploy those files from the out dir and copy an existing or create a config file you should now be able to run it.

## Running the sample

### usage: sample.exe config-file [volume on/off]
example running from the out folder:
    
    export LD_LIBRARY_PATH="../lib/arm32"
    sample.exe config.json on
