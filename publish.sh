#!/bin/bash

set -euo pipefail

if [ $# -ne 3 ]; 
  then echo "Provide 3 arguments: runtime, framework, and output folder"
  exit 1
fi

APPLICATION_NAME='TemplatingConsole'

RUNTIME=$1
FRAMEWORK=$2
OUTPUT_FOLDER=$3
CODE_FOLDER="code"

dotnet publish \
        --configuration Release \
        --runtime "${RUNTIME}" \
        --framework "${FRAMEWORK}" \
        -p PublishSingleFile=true \
        -p DebugType=None \
        -p DebugSymbols=false \
        --self-contained \
        --output "${OUTPUT_FOLDER}" \
        ${CODE_FOLDER}/src/${APPLICATION_NAME}

# cleanup:
# sometimes (?) there is a file `TemplatingLib.xml` in the output which is not needed
if test -f "${OUTPUT_FOLDER}/TemplatingLib.xml"; then
    rm "${OUTPUT_FOLDER}/TemplatingLib.xml"
fi