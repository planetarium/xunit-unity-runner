workflow "push" {
  on = "push"
  resolves = ["release"]
}

action "build" {
  uses = "docker://gableroux/unity3d:2019.1.0f2"
  runs = ["./build.sh"]
  secrets = [
    # Base64-encoded Unity license file.
    #   base64 -w0 < Unity_v2019.x.ulf
    # See also: https://license.unity3d.com/manual
    "ULF"
  ]
  env = {
    TARGETS = "StandaloneLinux64 StandaloneOSX StandaloneWindows64"
  }
}

action "tag-filter" {
  uses = "actions/bin/filter@master"
  args = "tag"
}

action "release" {
  uses = "docker://alpine:3.9"
  needs = ["tag-filter", "build"]
  runs = ["./release.sh"]
  secrets = [
    "GITHUB_TOKEN"
  ]
}
