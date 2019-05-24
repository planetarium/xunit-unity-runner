xUnit.net Unity Runner
======================

This program runs [xUnit.net]-based unit tests on [Unity] player so that
a software work well on Unity's peculiar runtime environment, which differs
from stable [Mono].

You can download the executable binaries from the [releases] page.

This program takes one or more *.dll* files and run tests in them, e.g.:

~~~~ bash
./StandaloneLinux64 YourTests.dll
~~~~

~~~~ pwsh
StandaloneWindows64.exe YourTests.dll
~~~~

It also takes several options like `-C`/`--exclude-class` and
`-T`/`--exclude-trait-condition`.  See `--help` for details.

On macOS you need to invoke the actual executable binary in
*StandardOSX.app/Contents/MacOS/* directory, e.g.:

~~~~ bash
StandaloneOSX.app/Contents/MacOS/StandardOSX YourTests.dll
~~~~

Note that *.dll* files to test should target on .NET Framework (e.g., `net461`),
not .NET Core.

[xUnit.net]: https://xunit.net/
[Unity]: https://xunit.net/
[Mono]: https://www.mono-project.com/
[releases]: https://github.com/planetarium/xunit-unity-runner/releases
