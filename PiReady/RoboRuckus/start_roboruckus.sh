#!/usr/bin/env bash

# This file is used to run the RoboRuckus application as a service 
# on a linux system, via systemd. it needs to be placed in the same
# directory as the application binary and support files.  This file
# also needs to be made executable.

# Set the rundir to the source of this script
# (which sould be in the same directory as the RoboRuckus binary)
SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" >/dev/null 2>&1 && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ $SOURCE != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
RUNDIR="$( cd -P "$( dirname "$SOURCE" )" >/dev/null 2>&1 && pwd )"

# Change working directory to the installation folder
cd ${RUNDIR}

# Run the service
./RoboRuckus