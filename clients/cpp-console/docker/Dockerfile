FROM arm32v7/ubuntu:18.04
ADD qemu-arm-static.tar.gz /usr/bin

RUN export DEBIAN_FRONTEND=noninteractive && \
    apt-get update --quiet || true && \
    apt-get install --quiet --yes --no-install-recommends \
        # Speech SDK Core build dependencies (on top of cmake)
        build-essential \
        libssl-dev \
        uuid-dev \
        libasound2-dev \
        # Dependencies for gstreamer plugins
        libgstreamer1.0-dev \
        gstreamer1.0-plugins-base \
        gstreamer1.0-plugins-good \
        # opusparse is in bad plugin
        gstreamer1.0-plugins-bad \
        # mpg123audiodec is in ugly plugin
        gstreamer1.0-plugins-ugly \
        # Speech SDK Java binding build dependencies (on top of swig)
        # (AFAIR, default-jdk-headless could not be used in Ubuntu 16.04 since
        # it lacked JNI headers. Double-check on newer versions.)
        default-jdk \
        && \
    apt-get autoremove --quiet --yes && \
    rm -rf /var/lib/apt/lists/*

# Override at docker build time, as appropriate:
ARG BUILD_UID=0
ARG BUILD_USER=root

# N.B. Root already present, do not override.
RUN [ "$BUILD_UID" = 0 ] || adduser --gecos 'Speech SDK User' --disabled-password $BUILD_USER --uid $BUILD_UID
USER $BUILD_USER
