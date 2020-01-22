# Use an official Ubuntu 16.04 ARM image as parent
FROM arm32v7/ubuntu:16.04

# Place QEMU ARM interpreter inside the container
ADD qemu-arm-static.tar.gz /usr/bin

# Install necessary packages and tools
RUN export DEBIAN_FRONTEND=noninteractive && \
    apt-get update --quiet && \
    apt-get install --quiet --yes --no-install-recommends wget ca-certificates build-essential libasound2-dev && \
    apt-get autoremove --quiet --yes && \
    rm -rf /var/lib/apt/lists/* && \
    wget --no-verbose --output-document=- https://cmake.org/files/v3.12/cmake-3.12.4.tar.gz | tar -xzf - && \
    cd cmake-3.12.4 && \
    ./bootstrap -- -DCMAKE_BUILD_TYPE:STRING=Release && \
    make -j && \
    make install && \
    cd .. && \
    rm -rf cmake-3.12.4
