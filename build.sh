#!/usr/bin/env bash
export FrameworkPathOverride=$(dirname $(which mono))/../lib/mono/4.5/

set -eu
set -o pipefail

cd `dirname $0`

PAKET_EXE=.paket/paket.exe
FAKE_EXE=packages/build/FAKE/tools/FAKE.exe

FSIARGS=""
OS=${OS:-"unknown"}
if [[ "$OS" != "Windows_NT" ]]
then
  FSIARGS="--fsiargs -d:MONO"
fi

function run() {
  if [[ "$OS" != "Windows_NT" ]]
  then
    mono "$@"
  else
    "$@"
  fi
}

function yesno() {
  # NOTE: Defaults to NO
  read -p "$1 [y/N] " ynresult
  case "$ynresult" in
    [yY]*) true ;;
    *) false ;;
  esac
}

if [[ "$OS" != "Windows_NT" ]] &&
       [ $(certmgr -list -c Trust | grep X.509 | wc -l) -le 1 ] &&
       [ $(certmgr -list -c -m Trust | grep X.509 | wc -l) -le 1 ]
then
  echo "Your Mono installation has no trusted SSL root certificates set up."
  echo "This may result in the Paket bootstrapper failing to download Paket"
  echo "because Github's SSL certificate can't be verified. One way to fix"
  echo "this issue would be to download the list of SSL root certificates"
  echo "from the Mozilla project by running the following command:"
  echo ""
  echo "    mozroots --import --sync"
  echo ""
  echo "This will import over 100 SSL root certificates into your Mono"
  echo "certificate repository."
  echo ""
  if yesno "Run 'mozroots --import --sync' now?"
  then
    mozroots --import --sync
  else
    echo "Attempting to continue without running mozroots. This might fail."
  fi
fi

run $PAKET_EXE restore

run $FAKE_EXE "$@" $FSIARGS build.fsx