#!/bin/bash

# Exit on error.
set -e

# We will need some directions... Let's make sure we get them.
if [ $# -ne 1 ] || [ ! -d $1 ];
    then echo "Usage: $0 <directory>"
	exit
fi

# Traverse input directory, and verify syntax in all files is correct.
find $1 -type f -name '*.json' | {
    while read -r f;
    do jq empty "$f" || echo "in $f";
    done
}
