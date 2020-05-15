# Microsoft Cognitive Services - Voice Assistant C++ Console Sample - Linux Setup

## Overview

This readme should go over setting up a Linux OS. In our example we are using Ubuntu 18.04 on a [Raspberry Pi] (https://www.raspberrypi.org/).

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
  ./buildArm32Linux.sh
  ```
  
## Build directly on Linux arm32 with Microsoft Audio Stack

Microsoft Audio Stack (MAS) can be used to enhance the experience.

Benefits include:
* Software Acoustic Echo Cancellation
* Beamforming
* Noise Suppression
* Automatic Gain Control
* Dereverberation

In order to use Microsoft Audio Stack you must specify in the config file the appropriate values. Below are the ones that we used on our Raspberry Pi:</br>
"custom_mic_config_path" :"/home/ubuntu/cpp-console/configs/micConfig.json",
"linux_capture_device_name": "hw:1,0"

An example mic config.json is included in the configs folder of this repo. For more information on how to configure your device's mic array see [here](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/how-to-devices-microphone-array-configuration).

The config path can be anything that points to the micConfig.json file, but the capture device should be the one you intend to use. In this case it is the hardwar 1 subdevice 0. You can use arecord to discover which device you have set up. [arecord](https://linux.die.net/man/1/arecord)

To build it simply run the buildArm32LinusWithMAS.sh file. This will download all necessary binaries and build the project with the MAS macro defined.

```sh
cd scripts
./buildArm32LinuxWithMAS.sh
```


## Running the sample

### usage: sample.exe config-file

* Run the sample from the out folder

  ```sh
  cd ../out
  export LD_LIBRARY_PATH="../lib/arm32"
  ./sample.exe ../configs/config.json
  ```  

### Installing as a service

There is a script in the scripts/linux directory that can be run to install the Voice Assistant sample as a service that runs at boot and tries to recover if it exits for any reason. This script makes some assumptions that you can change if you wish. This section will go over the assumptions made and what it does.

The script assumes the following:
* The config.json file in the configs directory is the one you want to use.
* sample.exe is the name of the application you have built.
* The build for the app is already completed.
* /data/cppSample is the directory where the exe, binaries, configs, and keyword models will be copied.
* You have correct configurations and confirmed the app will run without issues.

To run the script you will need root permissions because we are registering a service.

  ```sh
  sudo ./installService.sh
  ```

This will register a service called VoiceAssistant and a timer for that service to start 45 seconds after boot.

reboot your device to see the effect.

#### [Main Devices Readme](README.md)

### Common Troubleshooting

## Alsa is installed but arecord and aplay do not work

Add the following line to /boot/firmware/config.txt

    dtparam=audio=on
  
Then reboot the device to ensure all settings are properly configured.

## Service is not working

change the startService.sh file in the directory where you installed the service (default: /data/cppSample). Change the line that says "2>/dev/null" to point to a file to log the output. "2>/data/cppSample/log.txt" In this case we just used a log.txt file in our /data/cppSample folder.

Check this log for more data, but beware it will be overwritten every time the service starts. To stop the service from restarting, alter the VoiceAssistant.service file and change the Restart value to = "no". Reboot your device and the service should only attempt to run once. After this the log file should provide more information about what is wrong.
