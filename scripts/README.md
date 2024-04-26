# Scripting notes

## Requirements

- bash
- dotnet 8.0 or later
- [dasel](https://github.com/TomWright/dasel) (used for xml manipulation)

## Usage

- Copy this folder somewhere on your machine (e.g. `~/tmp/dotnet-template`)
- Switch to the folder where you want to create the new solution (e.g. `cd ~/tmp/here`)
  ```sh
  cd ~/tmp/here
  ~/tmp/dotnet-template/scripts/create.sh YourProjectName TargetFolderName
  ```