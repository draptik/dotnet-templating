#!/bin/sh

APPLICATION_NAME='TemplatingConsole'
DOTNET_BUILD_FOLDER="./out"
RUNTIME='linux-x64'
FRAMEWORK='net8.0'

dotnet publish \
        --configuration Release \
        --runtime ${RUNTIME} \
        --framework ${FRAMEWORK} \
        -p PublishSingleFile=true \
        -p DebugType=None \
        -p DebugSymbols=false \
        --self-contained \
        --output ${DOTNET_BUILD_FOLDER} \
        src/${APPLICATION_NAME}
