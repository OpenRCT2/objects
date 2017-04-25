#!/bin/bash
# Build objexport and export objects to json files

echo "Export objects"
if [ $# -ne 2 ]; then
    echo "Usage: export <objdata path> <output directory>"
    exit 1
fi
objdata=$1
outputdir=$2

function checkapp()
{
    which $1 &> /dev/null
    if [ $? -ne 0 ]; then
        echo -e "\033[0;31m$1 not found"
        exit
    fi
}

# Check for required apps
checkapp mono
checkapp nuget
checkapp xbuild
checkapp fsharpc

# Build objexport
pushd tools/objexport
    echo -e "\e[36mBuilding objexport"
    nuget restore > /dev/null
    if [ $? -ne 0 ]; then exit; fi
    xbuild /nologo /v:m /p:Configuration=Release "/p:Platform=Any CPU"
    if [ $? -ne 0 ]; then exit; fi
popd

objexport="tools/objexport/bin/Release/objexport.exe"
if [ ! -f $objexport ]; then
    echo -e "\033[0;31m$objexport not found"
    exit
fi

mono $objexport $objdata $outputdir
