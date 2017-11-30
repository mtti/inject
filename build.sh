#!/bin/bash

set -e

MCS="mcs"
RUNTIME_DLL_DIRECTORY="UnityDll"
EDITOR_DLL_DIRECTORY="UnityEditorDll"

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
DEPENDENCIES="${DEPENDENCIES:-$DIR/dependencies}"
UNITY_DLL="${UNITY_DLL:-$DEPENDENCIES}"

echo "UNITY_DLL: $UNITY_DLL"

mkdir -p $DIR/dist/mtti/Inject/$RUNTIME_DLL_DIRECTORY
$MCS -t:library -codepage:utf8 -sdk:2 -r:System -d:UNITY \
    -out:$DIR/dist/mtti/Inject/$RUNTIME_DLL_DIRECTORY/mttiInject.dll \
    -r:"$UNITY_DLL/UnityEngine.dll" \
    -recurse:"src/common/*.cs" \
    -recurse:"src/unity/*.cs"

mkdir -p $DIR/dist/mtti/Inject/$EDITOR_DLL_DIRECTORY
$MCS -t:library -codepage:utf8 -sdk:2 -r:System -d:"UNITY;UNITY_EDITOR" \
    -out:$DIR/dist/mtti/Inject/$EDITOR_DLL_DIRECTORY/mttiInject.dll \
    -r:"$UNITY_DLL/UnityEngine.dll" \
    -r:"$UNITY_DLL/UnityEditor.dll" \
    -recurse:"src/common/*.cs" \
    -recurse:"src/unity/*.cs" \
    -recurse:"src/unity-editor/*.cs"

cp $DIR/version.txt $DIR/dist/mtti/Inject/
