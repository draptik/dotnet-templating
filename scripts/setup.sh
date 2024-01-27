#!/bin/bash

DEFAULT_PROJECT_NAME="Demo"
DEFAULT_TARGET_DIR="../out"

RESOURCE_DIR=$(readlink -f "./resources")
echo "==> RESOURCE_DIR: ${RESOURCE_DIR}"

# if there are no command line argument, use default values
# otherwise use the command line arguments
if [ $# -eq 2 ]; then
    echo "==> Using command line arguments:"
    PROJECT_NAME=$1
    TARGET_DIR=$2
else
    echo "==> No arguments provided. Provide 2 arguments (first: project-name, second: target-folder). Using default values:"
    PROJECT_NAME=$DEFAULT_PROJECT_NAME
    TARGET_DIR=$DEFAULT_TARGET_DIR
fi

echo "==>   PROJECT_NAME: ${PROJECT_NAME}"
echo "==>   TARGET_DIR:   ${TARGET_DIR}"

TARGET_DIR_ABSOLUTE_PATH=$(readlink -f "$TARGET_DIR")
echo "==> TARGET_DIR_ABSOLUTE_PATH: ${TARGET_DIR_ABSOLUTE_PATH}"

# Clean up target directory
rm -rf "${TARGET_DIR_ABSOLUTE_PATH:?}"/* "${TARGET_DIR_ABSOLUTE_PATH:?}"/.*

SRC_DIR="${TARGET_DIR_ABSOLUTE_PATH}/src"
TEST_DIR="${TARGET_DIR_ABSOLUTE_PATH}/tests"

WEBAPI_NAME="${PROJECT_NAME}.MyWebApi"
LIBRARY_NAME="${PROJECT_NAME}.MyLib"

# General files ---------------------------------------------------------------
echo "==> Creating general files in target folder: ${TARGET_DIR_ABSOLUTE_PATH} ..."
cp "${RESOURCE_DIR}/.gitattributes.template" "${TARGET_DIR_ABSOLUTE_PATH}/.gitattributes"
cp "${RESOURCE_DIR}/.global.json.template" "${TARGET_DIR_ABSOLUTE_PATH}/global.json"
mkdir "${TARGET_DIR_ABSOLUTE_PATH}/.config"
cp "${RESOURCE_DIR}/.config/dotnet-tools.json" "${TARGET_DIR_ABSOLUTE_PATH}/.config/dotnet-tools.json"

cp "${RESOURCE_DIR}/Directory.Build.props.template" "${TARGET_DIR_ABSOLUTE_PATH}/Directory.Build.props"
# cp "${RESOURCE_DIR}/Directory.Packages.props.template" "${TARGET_DIR_ABSOLUTE_PATH}/Directory.Packages.props"

# TODO add .editorconfig
echo "==> Created general files in target folder: ${TARGET_DIR_ABSOLUTE_PATH}"

echo "==> Creating solution: '${PROJECT_NAME}' in folder '${TARGET_DIR_ABSOLUTE_PATH}'..."
dotnet new sln -n "${PROJECT_NAME}" -o "${TARGET_DIR_ABSOLUTE_PATH}"
echo "==> Created solution: '${PROJECT_NAME}' in folder '${TARGET_DIR_ABSOLUTE_PATH}'"

# Src folder ------------------------------------------------------------------
echo "==> Creating src folder: ${SRC_DIR} ..."
mkdir "${SRC_DIR}"
cd "${SRC_DIR}" || exit
echo "==> Created src folder: $(pwd)"

# Add Directory.Build.props to src folder
echo "==> Adding Directory.Build.props to src folder ..."
cp "${RESOURCE_DIR}/src/Directory.Build.props.template" "${TARGET_DIR_ABSOLUTE_PATH}/src/Directory.Build.props"
echo "==> Added Directory.Build.props to src folder"

# Create Lib project
echo "==> Creating a class library: ${LIBRARY_NAME} ..."
dotnet new classlib --no-restore -n "${LIBRARY_NAME}"
echo "==> Created a class library: ${LIBRARY_NAME}"

# Create WebApi project
echo "==> Creating a webapi project: ${WEBAPI_NAME} ..."
dotnet new webapi --no-restore --use-program-main --use-controllers -n "${WEBAPI_NAME}"
echo "==> Created a webapi project: ${WEBAPI_NAME}"

# # The default webapi template doesn't know anything about 
# # the Central Package Management feature ('Directory.*.props').
# # We manually have to "cleanup".
# # We remove the version string from package references within the webapi project file:
# echo "==> Cleaning up webapi project file: ${WEBAPI_NAME}.csproj ..."
# sed -i \
#     's/\(<PackageReference Include="[^"]*" \)Version="[^"]*" /\1/' \
#     "${WEBAPI_NAME}/${WEBAPI_NAME}.csproj"
# echo "==> Cleaned up webapi project file: ${WEBAPI_NAME}.csproj"

# WebApi depends on Lib
dotnet add "${WEBAPI_NAME}" reference "${LIBRARY_NAME}"

# Tests folder ----------------------------------------------------------------
mkdir "${TEST_DIR}"
cd "${TEST_DIR}" || exit

# # Add Directory.Build.props to tests folder
# echo "==> Adding Directory.Build.props to tests folder ..."
# cp "${RESOURCE_DIR}/tests/Directory.Build.props.template" "${TARGET_DIR_ABSOLUTE_PATH}/tests/Directory.Build.props"
# echo "==> Added Directory.Build.props to tests folder"

# Create Lib.Tests project
LIBRARY_TEST_NAME="${LIBRARY_NAME}.Tests"
dotnet new xunit --no-restore -n "${LIBRARY_TEST_NAME}"
# # Remove the version string from package references within the test project file
# # Also remove the inner text of the <PackageReference> element
# sed -i \
#     '/<PackageReference Include="[^"]*" /,/<\/PackageReference>/ {/Version="[^"]*"/s///; /<PackageReference/s/>[^<]*</></;}' \
#     "${LIBRARY_TEST_NAME}/${LIBRARY_TEST_NAME}.csproj"

# Lib.Tests depends on Lib
dotnet add "${LIBRARY_TEST_NAME}" reference "${SRC_DIR}/${LIBRARY_NAME}"

# Back in main folder ---------------------------------------------------------
cd "${TARGET_DIR_ABSOLUTE_PATH}" || exit
echo "SLN - Current folder: $(pwd)"

# Add all projects to solution
dotnet sln add "${SRC_DIR}/${LIBRARY_NAME}"
dotnet sln add "${SRC_DIR}/${WEBAPI_NAME}"
dotnet sln add "${TEST_DIR}/${LIBRARY_TEST_NAME}"

# Restore dotnet tools
echo "==> Restoring dotnet tools ..."
dotnet tool restore
echo "==> Restored dotnet tools"

# Restore nuget packages
echo "==> Restoring nuget packages ..."
dotnet restore
echo "==> Restored nuget packages"

# Run test
# echo "==> Running tests ..."
# dotnet test
# echo "==> Ran tests"

echo "==> Running tocpm ... (when asked, select 'y' or 'enter', then press Ctrl+C to end tocpm)"
dotnet tool run tocpm execute .
echo "==> Ran tocpm"

# Upgrade nuget packages
echo "==> Upgrading nuget packages ..."
dotnet outdated --upgrade
echo "==> Upgraded nuget packages"
echo "==>"
echo "==> Done"