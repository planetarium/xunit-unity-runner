xUnit.net Unity Runner Changelog
================================

Version 0.5.0
-------------

To be released.

 -  Added `-f`/`--stop-on-fail` option.


Version 0.4.0
-------------

Released on November 1, 2021.

 -  Added `-D`/`--distributed` option for distributed exccution.
 -  Added `-s`/`--distributed-seed` option for distributed exccution.
 -  Added `--dry-run` option.


Version 0.3.1
-------------

Released on November 1, 2021.

 -  Fixed a build bug that the runner had hanged and started running tests.
 -  Applied filters became printed to the standard error for easier
    troubleshooting.


Version 0.3.0
-------------

Released on October 31, 2021.

 -  Upgraded Unity Editor from 2019.1.0f2 to 2020.3.4f1.
 -  Added `-x`/`--report-xml-path` option.
 -  Fixed a bug that executables in distributions for Windows had lacked
    the suffix *.exe*.


Version 0.2.5
-------------

Released on June 3, 2021.

 -  Added detailed log output for failed tests.  [[#6]]

[#6]: https://github.com/planetarium/xunit-unity-runner/pull/6


Version 0.2.4
-------------

Released on June 27, 2019.

 -  Added more filtering options:  [[#3]]
     -  `-c`/`--select-class`
     -  `-m`/`--select-method`
     -  `-t`/`--select-trait-condition`

[#3]: https://github.com/planetarium/xunit-unity-runner/pull/3


Version 0.2.3
-------------

Released on June 17, 2019.

 -  Fixed the bug that every test case had run twice.  [[#2] by Swen Mun]

[#2]: https://github.com/planetarium/xunit-unity-runner/pull/2


Version 0.2.2
-------------

Released on June 13, 2019.

 -  Fixed the bug that test runner had crashed if there are any test cases
    nnotated with `Xunit.TheoryAttribute`.  [[#1] by Lee Dogeon]

[#1]: https://github.com/planetarium/xunit-unity-runner/pull/1


Version 0.2.1
-------------

Released on June 5, 2019.

 - Improved the way to terminate the test runner process in order to finish
   tests gracefully.


Version 0.2.0
-------------

Released on May 25, 2019.

 -  A graphical window became no more shown.  It now has only a command-line
    interface.
 -  Added CLI options to filter tests:
     -  `-C`/`--exclude-class`
     -  `-M`/`--exclude-method`
     -  `-T`/`--exclude-trait-condition`
 -  Added a CLI option to print the usage manual: `-h`/`--help`.


Version 0.1.0
-------------

Initial release.  Released on May 24, 2019.
