#!/bin/bash
set -e

if [[ -f Unity_v2019.x.ulf ]]; then
  # for local debugging
  cp Unity_v2019.x.ulf /tmp/Unity_v2019.x.ulf
elif [[ "$ULF" = "" ]]; then
  echo "The ULF environment variable is missing." > /dev/stderr
  exit 1
else
  echo -n "$ULF" | base64 -d > /tmp/Unity_v2019.x.ulf
fi

/opt/Unity/Editor/Unity \
  -quit \
  -batchmode \
  -nographics \
  -logFile \
  -manualLicenseFile /tmp/Unity_v2019.x.ulf || true

mkdir -p Dist
tar --help

for target in $TARGETS; do
  if [[ ! -d "Builds/$target" ]]; then
    /opt/Unity/Editor/Unity \
      -quit \
      -batchmode \
      -nographics \
      -logFile \
      -projectPath . \
      -executeMethod "Builder.Build$target"
  fi
  dist_path="$(pwd)/Dist/$target.tar.bz2"
  if [[ ! -f "$dist_path" ]]; then
    pushd "Builds/$target/"
    tar cvfj "$dist_path" ./*
    popd
  fi
done
