#!/bin/bash
#
# This script creates an executable file `APPLICATION_NAME` (see below) and a "resource" folder.
#
# The results are created in a timestamped folder: `OUTPUT_FOLDER` (see below). 

set -euo pipefail

APPLICATION_NAME='TemplatingConsole'

RUNTIME='linux-x64' # any valid dotnet runtime
FRAMEWORK='net8.0'

CURRENT_DATE=$(date +%Y-%m-%d-%T)
OUTPUT_FOLDER="./out/${CURRENT_DATE}"

dotnet publish \
        --configuration Release \
        --runtime ${RUNTIME} \
        --framework ${FRAMEWORK} \
        -p PublishSingleFile=true \
        -p DebugType=None \
        -p DebugSymbols=false \
        --self-contained \
        --output "${OUTPUT_FOLDER}" \
        src/${APPLICATION_NAME}

# cleanup:
# sometimes (?) there is a file `TemplatingLib.xml` in the output which is not needed
rm "${OUTPUT_FOLDER}/TemplatingLib.xml"