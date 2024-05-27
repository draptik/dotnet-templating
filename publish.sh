#!/bin/bash

APPLICATION_NAME="TemplatingConsole"
TARGET_FOLDER="$HOME/tmp/templating-out"
DOTNET_FOLDER="dotnet-fsharp"

dotnet publish \
	--configuration Release \
	--runtime linux-x64 \
	--self-contained \
	--framework net8.0 \
	-p PublishSingleFile=true \
	-p DebugType=None \
	-p DebugSymbols=false \
	--output "${TARGET_FOLDER}" \
	"${DOTNET_FOLDER}/src/${APPLICATION_NAME}"
