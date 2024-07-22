#!/bin/bash

APPLICATION_NAME='TemplatingConsole'

RUNTIME=$1
FRAMEWORK=$2
OUTPUT_FOLDER=$3
CODE_FOLDER="dotnet-fsharp"

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
