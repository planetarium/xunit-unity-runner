#!/bin/ash
set -e

if [ "${GITHUB_REF:0:10}" != "refs/tags/" ]; then
  echo "This script is run for only tag push; being skipped..." > /dev/stderr
  exit 1
fi

tag="${GITHUB_REF#refs/tags/}"

apk add --no-cache ca-certificates

wget -O /tmp/github-release.tar.bz2 \
  https://github.com/aktau/github-release/releases/download/v0.7.2/linux-amd64-github-release.tar.bz2
tar xvfj /tmp/github-release.tar.bz2 -C /tmp
rm /tmp/github-release.tar.bz2

github_user="${GITHUB_REPOSITORY%/*}"
github_repo="${GITHUB_REPOSITORY#*/}"

alias github-release=/tmp/bin/linux/amd64/github-release

github-release release \
  --user "$github_user" \
  --repo "$github_repo" \
  --tag "$tag" \
  --name "xunit-unity-runner $tag" || true

ls -alh Dist

for dist in Dist/*.tar.bz2; do
  filename="$(basename "$dist")"
  github-release upload \
    --user "$github_user" \
    --repo "$github_repo" \
    --tag "$tag" \
    --name "xunit-unity-runner-$tag-$filename" \
    --file "$dist"
done
