#!/bin/bash
#
# Requires: 
# - dotnet 8.0.0 or later
# - dasel for xml manipulation (https://github.com/TomWright/dasel)

DEFAULT_PROJECT_NAME="Demo"
DEFAULT_TARGET_DIR="../out"

# https://stackoverflow.com/a/246128
SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
RESOURCE_DIR="${SCRIPT_DIR}/resources"
echo "==> RESOURCE_DIR: ${RESOURCE_DIR}"

# if there are no command line argument, use default values
# otherwise use the command line arguments
if [ $# -eq 2 ]; then
	echo "==> Using command line arguments:"
	PROJECT_NAME=$1
	TARGET_DIR=$2
else
	echo "==> No arguments provided. You can provide 2 arguments (first: project-name, second: target-folder). Using default values:"
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

LIBRARY_NAME="${PROJECT_NAME}.MyLib"

# The first argument is the name of the project.
# The second argument is the name of the xml element to delete.
function delete_xml_element {
	INTERNAL_PROJECT_NAME=$1
	INTERNAL_XML_ELEMENT=$2
	echo "==> **** Deleting xml element: '${INTERNAL_XML_ELEMENT}' from project: '${INTERNAL_PROJECT_NAME}' ..."
	# NOTE:
	# 'dasel' is a command line tool for querying and updating XML documents.
	# It is simliar to 'jq' for JSON.
	# See: https://github.com/TomWright/dasel
	# Can be replaced by something else, this is just a quick and dirty solution.
	dasel \
		delete \
		--file "${INTERNAL_PROJECT_NAME}/${INTERNAL_PROJECT_NAME}.csproj" \
		--read xml \
		--write xml \
		"${INTERNAL_XML_ELEMENT}" # <- remove element
	echo "==> **** Deleted xml element: '${INTERNAL_XML_ELEMENT}' from project: '${INTERNAL_PROJECT_NAME}'"
}

# General files ---------------------------------------------------------------
echo "==> Creating general files in target folder: ${TARGET_DIR_ABSOLUTE_PATH} ..."

# if the target folder does not exist, create it
if [ ! -d "${TARGET_DIR_ABSOLUTE_PATH}" ]; then
	echo "==> Creating target folder: ${TARGET_DIR_ABSOLUTE_PATH}"
	mkdir "${TARGET_DIR_ABSOLUTE_PATH}"
fi

cp "${RESOURCE_DIR}/.gitattributes.template" "${TARGET_DIR_ABSOLUTE_PATH}/.gitattributes"
cp "${RESOURCE_DIR}/Directory.Build.props.template" "${TARGET_DIR_ABSOLUTE_PATH}/Directory.Build.props"
cp "${RESOURCE_DIR}/Directory.Packages.props.template" "${TARGET_DIR_ABSOLUTE_PATH}/Directory.Packages.props"
echo "==> Created general files in target folder: ${TARGET_DIR_ABSOLUTE_PATH}"

# Create global.json
echo "==> Creating global.json in target folder: ${TARGET_DIR_ABSOLUTE_PATH} ..."
dotnet new globaljson --sdk-version "8.0.0" --roll-forward "latestMajor" --output "${TARGET_DIR_ABSOLUTE_PATH}"
echo "==> Created global.json in target folder: ${TARGET_DIR_ABSOLUTE_PATH}"

# Create gitignore
echo "==> Creating gitignore in target folder: ${TARGET_DIR_ABSOLUTE_PATH} ..."
dotnet new gitignore --output "${TARGET_DIR_ABSOLUTE_PATH}"
echo "==> Created gitignore in target folder: ${TARGET_DIR_ABSOLUTE_PATH}"

# Create editorconfig
echo "==> Creating editorconfig in target folder: ${TARGET_DIR_ABSOLUTE_PATH} ..."
dotnet new editorconfig --output "${TARGET_DIR_ABSOLUTE_PATH}"
echo "==> Created editorconfig in target folder: ${TARGET_DIR_ABSOLUTE_PATH}"

# Create solution
echo "==> Creating solution: '${PROJECT_NAME}' in folder '${TARGET_DIR_ABSOLUTE_PATH}'..."
dotnet new sln -n "${PROJECT_NAME}" --output "${TARGET_DIR_ABSOLUTE_PATH}"
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
dotnet new classlib --no-restore --name "${LIBRARY_NAME}"
echo "==> Created a class library: ${LIBRARY_NAME}"

# Tests folder ----------------------------------------------------------------
mkdir "${TEST_DIR}"
cd "${TEST_DIR}" || exit

# Add Directory.Build.props to tests folder
echo "==> Adding Directory.Build.props to tests folder ..."
cp "${RESOURCE_DIR}/tests/Directory.Build.props.template" "${TARGET_DIR_ABSOLUTE_PATH}/tests/Directory.Build.props"
echo "==> Added Directory.Build.props to tests folder"

# Create Lib.Tests project
LIBRARY_TEST_NAME="${LIBRARY_NAME}.Tests"
dotnet new xunit --no-restore --name "${LIBRARY_TEST_NAME}"

# Lib.Tests depends on Lib
dotnet add "${LIBRARY_TEST_NAME}" reference "${SRC_DIR}/${LIBRARY_NAME}"

# Remove the PropertyGroup element from the test project file:
sed -i '/<PropertyGroup>/,/<\/PropertyGroup>/d' "${LIBRARY_TEST_NAME}/${LIBRARY_TEST_NAME}.csproj"

# Remove the PropertyGroup element from the test project file:
delete_xml_element "${LIBRARY_TEST_NAME}" "Project.ItemGroup.[0]"

# Back in main folder ---------------------------------------------------------
cd "${TARGET_DIR_ABSOLUTE_PATH}" || exit
echo "SLN - Current folder: $(pwd)"

# Add all projects to solution
dotnet sln add "${SRC_DIR}/${LIBRARY_NAME}"
dotnet sln add "${TEST_DIR}/${LIBRARY_TEST_NAME}"

# Restore nuget packages
echo "==> Restoring nuget packages ..."
dotnet restore
echo "==> Restored nuget packages"

# Run tests (this builds the projects as well)
echo "==> Running tests (builds the solution w/ tests) ..."
dotnet test
echo "==> Ran tests. Done."