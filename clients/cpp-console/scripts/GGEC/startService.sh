#!/bin/sh

export LD_LIBRARY_PATH="/data/cppSample"
/data/cppSample/sample.exe /data/cppSample/config.json 2>/dev/null &

exit 0