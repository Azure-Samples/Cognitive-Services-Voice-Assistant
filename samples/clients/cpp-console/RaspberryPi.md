# Microsoft Cognitive Services - Voice Assistant Sample Code C++ console Linux setup

## Overview

This readme should go over setting up a Linux OS. In our example we are using Ubuntu 18.04 on a raspberry pi

## Useful tools

There are many ways to do development on a Raspberry pi. It may be useful to take advantage of these tools:
* [Visual Studio Code remote SSH plugin](https://code.visualstudio.com/docs/remote/ssh)
* [PuTTY SSH client](https://www.chiark.greenend.org.uk/~sgtatham/putty/latest.html)
* [Bitvise SSH client](https://www.bitvise.com/)

## Setting up the device

* Install Ubuntu Server on a Raspberry Pi, connecting it to the internet and using it remotely.
  * [install instructions](https://ubuntu.com/tutorials/how-to-install-ubuntu-on-your-raspberry-pi)

* Make sure you have speakers and a microphone attached

* Setup Alsa for Audio Playback and Recording (this part can be tricky)
  * [ALSA wiki](https://wiki.archlinux.org/index.php/Advanced_Linux_Sound_Architecture)
  * keep in mind you can test it with aplay, arecord, and speaker-test

## Setting up the code

The repo should be cloned onto your device and we will operate out of the cpp-console folder

Make sure you have the Microsoft speech SDK downloaded. Links are in the main readme

For Linux or ARM devices the target version and the native binaries should be copied to the lib folder in this repo so that you have a structure like this (for ARM32): ./lib/arm32. 

Headers and their folders should be copied into the include folder so that you have a structure like this: ./include/cxx_api and ./include/c_api

Replace the text in the configs/config.json file with your subscription key and key region. If you are using a custom speech commands application or custom speech font you can insert those GUID's there as well.

## Build directly on Linux arm32

* You will need to install some packages.

  ```sh
  sudo apt-get install g++ libasound2-dev
  ```

* Then run the build script.

  ```sh
  cd scripts
  sh ./buildArm32.sh
  ```

## Running the sample

### usage: sample.exe config-file

* Run the sample from the out folder

  ```sh
  cd ../out
  export LD_LIBRARY_PATH="../lib/arm32"
  ./sample.exe ../configs/config.json
  ```  

#### [Main Devices Readme](README.md)
