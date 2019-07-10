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

wget -O /tmp/submark \
  https://github.com/dahlia/submark/releases/download/0.2.0/submark-linux-x86_64
chmod +x /tmp/submark

github_user="${GITHUB_REPOSITORY%/*}"
github_repo="${GITHUB_REPOSITORY#*/}"

alias github-release=/tmp/bin/linux/amd64/github-release
alias submark=/tmp/submark

submark \
  -o /tmp/release-note.txt \
  -iO \
  --h2 "Version $tag" \
  CHANGES.md

cat /tmp/release-note.txt

github-release release \
  --user "$github_user" \
  --repo "$github_repo" \
  --tag "$tag" \
  --name "xunit-unity-runner $tag" \
  --description - < /tmp/release-note.txt || true

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
