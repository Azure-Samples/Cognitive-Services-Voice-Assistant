# Microsoft Cognitive Services - Voice Assistant C++ Console Sample - Linux Setup

## Overview

This readme describes how to run the C++ client on a ARM32 and ARM64 Linux OS, it uses Ubuntu 20.04 LTS on a [Raspberry Pi 3 or 4](https://www.raspberrypi.org/).

Note: [Raspberry Pi OS (32 bit)](https://www.raspberrypi.org/downloads/raspbian/) is not currently supported.
ARM 64 Linux support will be documented, Raspberry Pi OS (64 bit) and Ubuntu ARM 64 will work. 

## SSH Clients

There are many ways to do development on a Raspberry pi. It may be useful to use one of these SSH clients to connect to the device:

* [Visual Studio Code remote SSH plugin](https://code.visualstudio.com/docs/remote/ssh)
* [PuTTY SSH client](https://www.chiark.greenend.org.uk/~sgtatham/putty/latest.html)
* [Bitvise SSH client](https://www.bitvise.com/)

## Setting up the device

* Install Ubuntu on a Raspberry Pi, connecting it to the internet and using it remotely.
  * Follow the [install instructions](https://ubuntu.com/tutorials/how-to-install-ubuntu-on-your-raspberry-pi) and choose Ubuntu 20.04 LTS, both 32 bit and 64 bit server OS are supported. There is no need to install a desktop.

* Make sure you have speakers and a microphone attached

* Setup Alsa for Audio Playback and Recording (this part can be tricky)
  * [ALSA wiki](https://wiki.archlinux.org/index.php/Advanced_Linux_Sound_Architecture)
  * keep in mind you can test it with aplay, arecord, and speaker-test

## Setting up the code

* Clone the Voice Assistant git repo onto your device, it will download to /home/ubuntu/Cognitive-Services-Voice-Assistant

  ```sh
  git clone https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant.git
  ```

* The speech SDK will be downloaded as part of the build script if necessary. For reference it can be found here: [Linux Speech SDK](https://aka.ms/csspeech/linuxbinary). To force an update of the binaries delete the contents of the lib folder and the c_api and cxx_api folders in your include directory.

* Replace the text in the configs/config.json file with your subscription key and key region. If you are using a Custom Commands application or a Custom Voice insert those GUID's as well. The keyword_model should point to the Custom Keyword being used (.table file), these are in /home/ubuntu/Cognitive-Services-Voice-Assistant/keyword-models

## Build directly on Linux

* You will need to install some packages.

  ```sh
  sudo apt-get install g++ libasound2-dev alsa-utils
  ```

* Then run the build script.

  ```sh
  cd /home/ubuntu/Cognitive-Services-Voice-Assistant/clients/cpp-console/scripts/linux
  chmod a+x *.sh
  ./buildArmLinux.sh
  ```
  
## Build directly on Linux with Microsoft Audio Stack

Microsoft Audio Stack (MAS) can be used to enhance the experience.

Benefits include:
* Software Acoustic Echo Cancellation
* Beamforming
* Noise Suppression
* Automatic Gain Control
* Dereverberation

In order to use Microsoft Audio Stack you must specify additional configuration details in the config.json. Below are the ones that we used on our Raspberry Pi:</br>
"custom_mic_config_path" :"/home/ubuntu/Cognitive-Services-Voice-Assistant/clients/cpp-console",</br>
"linux_capture_device_name": "hw:1,0"

The custom_mic_config_path points to your microphone configuration file. The linux_capture_device_name is the one you intend to use, with the default of hardware 1 subdevice 0. You can use arecord -l to discover which device you have set up. [arecord](https://linux.die.net/man/1/arecord)

Examples of a single microphone and a 4 mic linear array are included in the configs folder of this repo. For more information on how to configure your device's mic array see [here](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/how-to-devices-microphone-array-configuration).

To build it simply run buildArmLinuxWithMAS.sh. This will download all necessary binaries and build the project with MAS.

```sh
cd /home/ubuntu/Cognitive-Services-Voice-Assistant/clients/cpp-console/scripts/linux
./buildArmLinuxWithMAS.sh
```


## Running the sample

### usage: sample.exe config-file

* Run the sample from the out folder

### Using Microphone Input
```sh
cd /home/ubuntu/Cognitive-Services-Voice-Assistant/clients/cpp-console/out
export LD_LIBRARY_PATH="../lib/lib"
./sample.exe ../configs/config.json
```

### Using Audio Files as Input
```sh
cd /home/ubuntu/Cognitive-Services-Voice-Assistant/clients/cpp-console/out
export LD_LIBRARY_PATH="../lib/lib"
./sample.exe ../configs/config.json audioFilePath
```

### Installing as a service

There is a script in the scripts/linux directory that can be run to install the Voice Assistant sample as a service that runs at boot and tries to recover if it exits for any reason. This script makes some assumptions that you can change if you wish. This section will go over the assumptions made and what it does.

The script assumes the following:
* The config.json file in the configs directory is the one you want to use.
* sample.exe is the name of the application you have built.
* The build for the app is already completed.
* /data/cppSample is the directory where the exe, binaries, configs, and keyword models will be copied. This directory will be created if it does not exist.
* You have correct configurations and confirmed the app will run without issues.

To run the script you will need root permissions because we are registering a service.

  ```sh
  cd /home/ubuntu/Cognitive-Services-Voice-Assistant/clients/cpp-console/scripts/linux
  sudo ./installService.sh
  ```

This will register a service called VoiceAssistant and a timer for that service to start 45 seconds after boot.

reboot your device to see the effect.

To stop the service run:

  ```sh
  systemctl disable VoiceAssistant.timer
  systemctl daemon-reload
  ```
  
  Then reboot your device.
  
#### [Main Devices Readme](README.md)

### Common Troubleshooting

## Alsa is installed but arecord and aplay do not work

Add the following line to /boot/firmware/config.txt

    dtparam=audio=on
  
Then reboot the device to ensure all settings are properly configured.

## More Alsa troubleshooting

To test if the audio output iw working run the speaker test

```sh
speaker-test
```

If you do not hear anything check the Alsa configuration

```sh
aplay -l
```

If a USB speaker is being used it typically shows as card 1, device 0

    card 1: USB [Jabra SPEAK 410 USB], device 0: USB Audio [USB Audio]
      Subdevices: 1/1
      Subdevice #0: subdevice #0

In that case update the Alsa configuration in /usr/share/alsa/alsa.conf to reference card 1 device 0 in the following lines

    defaults.ctl.card 1
    defaults.pcm.card 1
    defaults.pcm.device 0

Edit the alsa.conf file

```sh
sudo vi /usr/share/alsa/alsa.conf
```

## Service is not working

change the startService.sh file in the directory where you installed the service (default: /data/cppSample). Change the line that says "2>/dev/null" to point to a file to log the output. "2>/data/cppSample/log.txt" In this case we just used a log.txt file in our /data/cppSample folder.

Check this log for more data, but beware it will be overwritten every time the service starts. To stop the service from restarting, alter the VoiceAssistant.service file and change the Restart value to = "no". Reboot your device and the service should only attempt to run once. After this the log file should provide more information about what is wrong.
