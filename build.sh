#!/bin/bash

set -e

MCS="mcs"
RUNTIME_DLL_DIRECTORY="UnityDll"
EDITOR_DLL_DIRECTORY="UnityEditorDll"

mkdir -p dist/mtti/Inject/$RUNTIME_DLL_DIRECTORY
$MCS -t:library -codepage:utf8 -sdk:2 -r:System -d:UNITY \
    -out:dist/mtti/Inject/$RUNTIME_DLL_DIRECTORY/mttiInject.dll \
    -r:"dependencies/UnityEngine.dll" \
    -recurse:"src/common/*.cs" \
    -recurse:"src/unity/*.cs"

mkdir -p dist/mtti/Inject/$EDITOR_DLL_DIRECTORY
$MCS -t:library -codepage:utf8 -sdk:2 -r:System -d:"UNITY;UNITY_EDITOR" \
    -out:dist/mtti/Inject/$EDITOR_DLL_DIRECTORY/mttiInject.dll \
    -r:"dependencies/UnityEngine.dll" \
    -r:"dependencies/UnityEditor.dll" \
    -recurse:"src/common/*.cs" \
    -recurse:"src/unity/*.cs" \
    -recurse:"src/unity-editor/*.cs"
