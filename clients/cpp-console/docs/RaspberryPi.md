# Microsoft Cognitive Services - Voice Assistant C++ Console Sample - Linux Setup

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

* Download the Speech SDK: The speech SDK will be downloaded as part of the build script. Otherwise it can be found here: [Linux Speech SDK](https://aka.ms/csspeech/linuxbinary).

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

### Common Troubleshooting

## Alsa is installed but arecord and aplay do not work

Add the following line to /boot/firmware/config.txt

    dtparam=audio=on
  
Then reboot the device to ensure all settings are properly configured.
