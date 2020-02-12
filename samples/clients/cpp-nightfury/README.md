

download the qemu-arm-static.tar.gz file from this link https://github.com/multiarch/qemu-user-static/releases/ and place it in the docker folder.

For linux machines you will also need to run "sudo apt-get install --yes binfmt-support qemu-user-static"

from the docker folder run "docker build -t dev_ubuntu_arm32 ."
This will create an image and name it "dev_ubuntu_arm32" which is used inside the build scripts.