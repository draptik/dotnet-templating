#!/bin/bash
#
# This script creates an executable file `APPLICATION_NAME` (see below) and a "resource" folder.
#
# The results are created in a timestamped folder: `OUTPUT_FOLDER` (see below). 

APPLICATION_NAME='TemplatingConsole'

RUNTIME=$1
FRAMEWORK=$2
OUTPUT_FOLDER=$3

dotnet publish \
        --configuration Release \
        --runtime "${RUNTIME}" \
        --framework "${FRAMEWORK}" \
        -p PublishSingleFile=true \
        -p DebugType=None \
        -p DebugSymbols=false \
        --self-contained \
        --output "${OUTPUT_FOLDER}" \
        src/${APPLICATION_NAME}

# cleanup:
# sometimes (?) there is a file `TemplatingLib.xml` in the output which is not needed
rm "${OUTPUT_FOLDER}/TemplatingLib.xml"