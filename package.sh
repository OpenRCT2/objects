#!/bin/bash
# Create zip archive containing objects
mkdir artifacts > /dev/null
rm artifacts/objects.zip
pushd objects > /dev/null
  zip -r9 ../artifacts/objects.zip rct2 rct2ww rct2tt
popd > /dev/null
