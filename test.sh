#!/bin/bash

set -e

MCS="mcs"
DEPENDENCIES="${DEPENDENCIES:-dependencies}"

mkdir -p dist

$MCS -t:exe -codepage:utf8 -sdk:2 -r:System \
    -out:dist/mttiInject.tests.exe \
    -r:"$DEPENDENCIES/nunit.framework.dll" \
    -r:"$DEPENDENCIES/nunitlite.dll" \
    -recurse:"src/common/*.cs" \
    -recurse:"src/tests/*.cs"

MONO_PATH=dependencies mono dist/mttiInject.tests.exe
