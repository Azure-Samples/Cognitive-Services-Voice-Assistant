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

* Clone the Voice Assistant git repo onto your device

  ```sh
  git clone https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant.git
  ```  

* Download the Speech SDK

  ```sh
  wget -c https://aka.ms/csspeech/linuxbinary -O - | tar -xz
  ```  

* Create a link to the cpp-console folder, move the Speech SDK libraries and headers to their destination, and change into the cpp-console folder. These commands are for the Speech SDK version 1.10, change it to match the version downloaded.

  ```sh
  ln -s Cognitive-Services-Voice-Assistant/clients/cpp-console
  ln -s Cognitive-Services-Voice-Assistant/keyword-models
  mv SpeechSDK-Linux-1.10.0/lib/arm32 cpp-console/lib/
  mv SpeechSDK-Linux-1.10.0/include/* cpp-console/include/
  cd cpp-console
  ```  

* Replace the text in the configs/config.json file with your subscription key and key region. If you are using a Custom Commands application or a Custom Voice insert those GUID's as well. The keyword_model should point to the Custom Keyword being used (.table file), these are in /home/ubuntu/keyword-models

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
