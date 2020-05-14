# Compile the C++ console Voice Assistant sample for Linux on a Windows machine

The C++ Console Voice Assistant sample needs to be compiled and run across Windows & Linux. Therefore, we must test any changes made to these clients and ensure they work cross-platform. Though the test can be done with a standalone Linux VM or a separate Linux machine, if your host operating system is Windows 10, WSL 2 (Windows Subsystem for Linux 2) is another handy and lightweight solution worth trying.

WSL 2 was released into the Insider Program last year. With the move to general availability, WSL 2 can now be automatically updated via standard Windows Updates. WSL 2 ships with a lightweight VM running a full Linux kernel. This VM runs directly on the Windows Hypervisor layer. This kernel includes full system call compatibility and allows for running apps like Docker and FUSE natively on Linux. With this new implementation, the Linux kernel has full access to the Windows file system and brings large improvements to performance especially for interactions that require accessing the file system.

For the official instructions for installing WSL 2, check [Installation Instructions for WSL 2](https://docs.microsoft.com/en-us/windows/wsl/wsl2-install). Following is the process I went through. **Warning! If you already had Ubuntu installed on WSL 1, you may have to completely uninstall and reinstall it.**
1. Join Windows Insider Program with Slow/Fast Ring and reboot as required. To get on the Slow/Fast Ring, go into **Settings > Update > Windows Insider Program**. You can also search for **Windows Insider Program** from the Start screen.
2. Run Windows Update to upgrade to Windows 10 version 2004 or higher and reboot as required.
3. In Windows features turn on Virtual Machine Platform and Windows Subsystem for Linux and reboot as required. Open Powershell with admin privileges and run 2 commands below to ensure the 2 turned on features are enabled.\
   Enable-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform\
   Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux
4. Go into the Microsoft Store and pick the Linux distribution you want. I choose Ubuntu 18.04 because Microsoft Speech SDK supports Ubuntu 16.04/18.04. Install, launch, and complete initialization of your distro with username and password as required.
5. To see which distros you have installed, you can run in CMD:\
   wsl -l\
   wsl -l -v\
   To set installed distro to be backed by WSL 2, you can run in CMD:\
   wsl --set-version <Distro> 2\
   Replace <Distro> with the actual installed version. You might be prompted to install an update to WSL 2 kernel component. For information please visit [Updating the WSL 2 Linux kernel](https://aka.ms/wsl2kernel).\
   Additionally, if you want to make WSL 2 your default architecture you can do so with below command. This will make any new distro that you install be initialized as a WSL 2 distro.\
   wsl --set-default-version 2
6. Open a Powershell or CMD, type wsl and return, your WSL 2 should be running.

For the detailed instructions for setting up C++ development for CPP Console client in WSL 2, check [Microsoft Cognitive Services - Voice Assistant C++ Console Sample](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/tree/master/clients/cpp-console). Following is what I have done:
1. Install g++, gdb, and required libraries.\
   sudo apt-get update\
   sudo apt-get install build-essential gdb libssl1.0.0 libasound2-dev\
   To check installed g++ and gdb version, you can run:\
   g++ --version\
   gdb --version
2. Download C++ binaries and header files [here](https://aka.ms/csspeech/linuxbinary), extract and put include folder and lib folder into cpp-console/.
3. Go to [alsa download page](https://www.alsa-project.org/wiki/Download), select "Library (alsa-lib)", and download alsa-lib-1.2.2.tar.bz2 or its newer version, extract head files in include folder and put them into cpp-console/include/alsa.
4. Execute build script, for me it's buildx64Linux.sh, to verify the environment is complete and working.

Other things worth mention are:
1. [User Experience Changes Between WSL 1 and WSL 2](https://docs.microsoft.com/en-us/windows/wsl/wsl2-ux-changes) will help if you have prior WSL 1 experience.
2. [WSL 2 with Visual Studio Code](https://code.visualstudio.com/blogs/2019/09/03/wsl2) shares how WSL 2 will help you be more productive. [Visual Studio Code](https://code.visualstudio.com) has an [extension](https://code.visualstudio.com/docs/remote/wsl) available to allow for developing within WSL from VS Code. The Visual Studio Code Remote-WSL extension allows for the VS Code UI to run on the Windows side with a VS Code Server running within the WSL VM. This allows for running commands directly within WSL and treating the mounted file system as a Linux file system reducing path issues or other cross-OS difficulties. Additionally, this extension allows for running and debugging applications directly within the Linux including the usage of breakpoints.
3. [Using Docker in WSL 2](https://code.visualstudio.com/blogs/2020/03/02/docker-in-wsl2) introduces how the [Technical Preview](https://docs.docker.com/docker-for-windows/wsl-tech-preview/) of Docker supports for running with WSL 2.
4. Last but not least, I am using the new Windows Terminal which brings a big improvement over the default cmd and Powershell experiences and is a great companion to use WSL 2. It has a look and many useful features that KDE terminal has. Windows Terminal (Preview) can be installed from the Windows Store.

WSL 2 is exciting and can be an indispensable tool if you want to be productive for cross-platform development.