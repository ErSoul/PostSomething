#!/bin/sh

set -eu # Add -xv flags for debugging

## Uncomment if you want to check for root permissions
# [ $UID -eq 0 ] || (echo "ERROR: User must be root" 1>&2 && exit 1)

## TEMPLATE: check if program exists.
type dotnet-format > /dev/null 2>&1 || { echo "ERROR: dotnet-format must be installed"; exit 1; }
git config core.hookspath .githooks/
