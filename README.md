xUnit.net Unity Runner
======================

This program runs [xUnit.net]-based unit tests on [Unity] player so that
a software work well on Unity's peculiar runtime environment, which differs
from stable [Mono].

You can download the executable binaries from the [releases] page.

This program takes one or more *absolute* paths to .NET assembly files (*.dll*)
and run tests in them, e.g.:

~~~~ bash
./StandaloneLinux64 "$(pwd)"/YourTests.dll
~~~~

~~~~ pwsh
StandaloneWindows64.exe C:\path\to\YourTests.dll
~~~~

It also takes several options like `-c`/`--select-class` and
`-T`/`--exclude-trait-condition`.  See `--help` for details.

On macOS you need to invoke the actual executable binary in
*StandardOSX.app/Contents/MacOS/* directory, e.g.:

~~~~ bash
StandaloneOSX.app/Contents/MacOS/StandardOSX "$(pwd)"/YourTests.dll
~~~~

Note that *.dll* files to test should target on .NET Framework (e.g., `net461`),
not .NET Core.

[xUnit.net]: https://xunit.net/
[Unity]: https://xunit.net/
[Mono]: https://www.mono-project.com/
[releases]: https://github.com/planetarium/xunit-unity-runner/releases


CircleCI-style parallelism[^1]
------------------------------

There are two options for running tests in distributed nodes:
`-D`/`--distributed` and `-s`/`--distributed-seed`.  The former option takes
an argument in the `N/M` format, where `N` is the current node's zero-indexed
number and `M` is the total number of distributed nodes.  This option selects
the subset of the test cases[^2] for the current node, and this guarantees
two different nodes never run the same test case, which is redundant.

For example, the following options make the test execution to take advantage
of CircleCI's parallelism:

~~~~ bash
--distributed=$CIRCLE_NODE_INDEX/$CIRCLE_NODE_TOTAL \
--distributed-seed=$CIRCLE_BUILD_NUM
~~~~


[^1]: See also CircleCI's related docs: [*Running Tests in Parallel*][1].
[^2]: Before a subset is selected, all test filters are applied first.
      Therefore, all distributed nodes have to apply the same set of filters.

[1]: https://circleci.com/docs/2.0/parallelism-faster-jobs/


FAQ
---

### I got `Magic number is wrong: 542` error.  I'm on Linux.

If the `TERM` environment variable is not set or it's a value unsupported by
Mono (e.g., `xterm-256color`) yet Unity player's built-in Mono runtime could
throw such an exception.  You could work around this by setting it `xterm`:

~~~~ bash
TERM=xterm ./StandaloneLinux64 "$(pwd)"/YourTests.dll
~~~~

See also the related issue on the Mono project:

<https://github.com/mono/mono/issues/6752>
