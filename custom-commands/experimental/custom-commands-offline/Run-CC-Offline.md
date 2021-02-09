# Running the Custom Command Embedded Service on Windows

## Prerequisites

### The host computer

The host is an x-64 or arm-64 based computer that runs the Docker container.

### Docker Engine

Install Docker on your device:

You need the Docker Engine installed on a host computer. Docker provides packages that configure the Docker environment on [macOS](https://docs.docker.com/docker-for-mac/), [Windows](https://docs.docker.com/docker-for-windows/), and [Linux](https://docs.docker.com/engine/install/). For a primer on Docker and container basics, see the [Docker overview](https://docs.docker.com/get-started/overview/).

On **Windows**, Docker must also be configured to support Linux containers.

### Familiarity with Docker

You should have a basic understanding of Docker concepts, like registries, repositories, containers, and container images, as well as knowledge of basic docker commands.

## Download Custom Commands container image with docker pull

Since, offline Custom Commands is an experimental feature, currently, you will only be able to consume it based on explicit approval. Please reach out to our team, and we can help you onboard.

Open a terminal (does not matter which folder) and type the following:

* Login to ACR: ```sudo docker login ACR_NAME -u ACR_USER_NAME -p ACR_USER_PASSWORD```
* Pull the image: ```docker pull **TBD**```
* Make sure the image is downloaded correctly by running: ```docker image ls``` . Verify **TBDDockerImageName** is present in the result.

## Container requirements and recommendations

The below table lists minimum and recommended values for the container host. Your requirements may change depending on traffic volume.

|Container| Minimum | Recommended |
|-----------|---------|-------------|--|
|Custom Commands|0.5 core, 256-MB memory|1 core, 512-MB memory

* Each core must be at least 1.5 gigahertz (GHz) or faster.

* Core and memory correspond to the `--cpus` and `--memory` settings, which are used as part of the [docker run](#run-container) command.

## Run Custom Commands container image

### Create mount folders

On the host computer, create a folder structure as follows:

```bash
    mkdir DockerMount
    cd DockerMount
    mkdir input
    mkdir output
```

These folders will be mounted to the docker container.

### Run container

Use the docker run command to run the container.
Example:

```bash
docker run -p 5000:5000
--cpus 1 --memory 512m
--mount type=bind,source=c:\Users\Public\DockerMount\input,destination=/app/input
--mount type=bind,source=c:\Users\Public\DockerMount\output,destination=/app/output
TBDFQDockerImageName
--CustomCommandsDataPath=/app/input
```

You should see this on the screen if everything went well:

```cmd
Hosting environment: Production
Content root path: /app/
Now listening on: http://[::]:5000
Application started. Press Ctrl+C to shut down.
```

## Download your Custom Commands application to be run offline from the Speech Portal

### Create an offline Custom Commands application

Since, offline Custom Commands is an experimental feature, currently, you will only be able to consume it based on explicit approval. Please reach out to our team, and we can help you onboard.

1. In the speech portal, create a new **offline** application.

    ![image](./media/customcommands-create-new-offline-application.png)

1. Build your application. Please refer to [How-To documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/custom-commands) if you are new to Custom Commands and need help in authoring an application.
1. TBD: Add an offline sample which can be imported in
1. Train and Test your application.

### Download offline application

#### Prerequisite

* A working offline CC application
* The application must be published

In the left panel, select **Settings** > **Offline Application**. Download files.
At this point, two files should be downloaded. Verify that the file names adhere to the following naming convention:

1. ```AppIdValue-StageValue-CultureValue_RuntimeDialogModel.Json```. Value for AppId, Stage and Culture of your offline application
2. ```ModelIdValue_SearchIndex.Json```. Value of the ModelId should be a GUID.
**Note**: In case you're using the default browser settings and download *offline application*, multiple times in the same location - the browser tend  to append file names with serial numbers. Please delete those before placing them in the [DockerMount input folder](#create-mount-folders).

## Configure Custom Commands container to use custom application

1. Stop the Docker container if running.
    * Get the container id: ```docker ps```
    * Stop the container: ```docker stop <container id>```
1. Move downloaded files to the [DockerMount **input** folder](#create-mount-folders) on the host computer.
1. [Run](#run-container) the container again.

## Configure the C++ client application

TODO: Add link to the Speech SDK documentation

```text
"CustomCommandsUrl": "ws://localhost:5000/apps/53b8478a-0ca9-4fcb-a11b-a2b55cb30df0/stages/offline/cultures/en-us"
```
