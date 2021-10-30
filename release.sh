#!/bin/bash
set -evx

if [ "${GITHUB_REF:0:10}" != "refs/tags/" ]; then
  echo "This script is run for only tag push; being skipped..." > /dev/stderr
  exit 1
fi

tag="${GITHUB_REF:10}"

wget -O /tmp/github-release.bz2 \
  https://github.com/github-release/github-release/releases/download/v0.10.0/linux-amd64-github-release.bz2
bzip2 -d /tmp/github-release.bz2
chmod +x /tmp/github-release

wget -O /tmp/submark \
  https://github.com/dahlia/submark/releases/download/0.2.0/submark-linux-x86_64
chmod +x /tmp/submark

github_user="${GITHUB_REPOSITORY%/*}"
github_repo="${GITHUB_REPOSITORY#*/}"

ls -al /tmp

/tmp/submark \
  -o /tmp/release-note.txt \
  -iO \
  --h2 "Version $tag" \
  CHANGES.md

cat /tmp/release-note.txt

/tmp/github-release release \
  --user "$github_user" \
  --repo "$github_repo" \
  --tag "$tag" \
  --name "xunit-unity-runner $tag" \
  --description - < /tmp/release-note.txt || true

ls -alh Dist

for dist in Dist/*.tar.bz2; do
  filename="$(basename "$dist")"
  /tmp/github-release upload \
    --user "$github_user" \
    --repo "$github_repo" \
    --tag "$tag" \
    --name "xunit-unity-runner-$tag-$filename" \
    --file "$dist"
done
