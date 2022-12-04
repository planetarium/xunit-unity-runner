using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mono.Options;
using UnityEngine;
using Xunit;
using Xunit.Abstractions;
using Xunit.Runners;
using Random = System.Random;

public class EntryPoint : MonoBehaviour
{
    private static readonly OptionSet Options = new OptionSet
    {
        {
            "c|select-class=",
            "Select a test class by its fully qualified name, and exclude " +
            "other classes.  This option can be used multiple times, and " +
            "all specified classes (and others selected by -m/--select-method " +
            "and -t/--select-trait-condition options) are included together.",
            className => SelectedClasses.Add(className)
        },
        {
            "m|select-method=",
            "Select a test method by its fully qualified name, and exclude " +
            "other methods.  This option can be used multiple times, and " +
            "all specified methods (and others selected by -c/--select-class " +
            "and -t/--select-trait-condition options) are included together.",
            method => SelectedMethods.Add(method)
        },
        {
            "t|select-trait-condition=",
            "Select a trait condition (e.g., `-T TraitName=value') and exclude " +
            "others.  This option can be used multiple times, and " +
            "all specified methods (and others selected by -c/--select-class " +
            "and -m/--select-method options) are included together.",
            traitCondition =>
                SelectedTraitConditions.Add(ParseTraitCondition(traitCondition))
        },
        {
            "C|exclude-class=",
            "Exclude a test class by its fully qualified name.  " +
            "This option can be used multiple times.  " +
            "Prior to -c/--select-class, -m/--select-method, and " +
            "-t/--select-trait-condition options.",
            className => ExcludedClasses.Add(className)
        },
        {
            "M|exclude-method=",
            "Exclude a test method by its fully qualified name.  " +
            "This option can be used multiple times.  " +
            "Prior to -c/--select-class, -m/--select-method, and " +
            "-t/--select-trait-condition options.",
            method => ExcludedMethods.Add(method)
        },
        {
            "T|exclude-trait-condition=",
            "Exclude a trait condition (e.g., `-T TraitName=value').  " +
            "This option can be used multiple times.  " +
            "Prior to -c/--select-class, -m/--select-method, and " +
            "-t/--select-trait-condition options.",
            traitCondition =>
                ExcludedTraitConditions.Add(ParseTraitCondition(traitCondition))
        },
        {
            "p|parallel=",
            "The maximum number of parallel threads to run tests.  Zero " +
            "means not to limit the number of threads.  1 by default.",
            parallel => Parallel = int.TryParse(parallel, out int p) && p >= 0
                ? p
                : throw new OptionException(
                        "The maximum number of parallel threads must be " +
                        "an integer greater than or equal to zero.",
                        "-p/--parallel"
                    )
        },
        {
            "D|distributed=",
            "Run only randomly selected tests out of the all discovered " +
            "tests.  This is intended to be used as a single node out of " +
            "multiple distributed nodes in parallel.  The format is N/M, " +
            "where N is the zero-indexed node number, and M is the total " +
            "number of distributed nodes to run tests in parallel.  " +
            "For example, the option --distributed=2/5 implies there are " +
            "5 nodes to run the same set of tests, and this is third one " +
            "of these nodes.  You can optionally provide " +
            "-s/--distributed-seed option to manually configure the shared " +
            "seed among distributed nodes.",
            d =>
            {
                Match m = Regex.Match(d, @"^\s*(-?\d+)\s*/\s*(-?\d+)\s*$");
                if (!m.Success)
                {
                    throw new OptionException(
                        "Expected a fraction, i.e., N/M where N is the zero-" +
                        "indexed node number, and M is the total number of " +
                        "distributed nodes.  See -h/--help for details.",
                        "-D/--distributed");
                }

                var current = int.Parse(m.Groups[1].Value);
                int total = int.Parse(m.Groups[2].Value);
                if (current < 0)
                {
                    throw new OptionException(
                        "The current node number cannot be less than zero.",
                        "-D/--distributed"
                    );
                }
                else if (total < 1)
                {
                    throw new OptionException(
                        "The total number must be greater than zero.",
                        "-D/--distributed"
                    );
                }
                else if (current >= total)
                {
                    throw new OptionException(
                        "The current node number must less be than the total " +
                        "number." + (current == total
                            ? "\nHint: The current node number is zero-indexed."
                            : string.Empty),
                        "-D/--distributed"
                    );
                }

                CurrentNode = current;
                DistributedNodes = total;
            }
        },
        {
            "s|distributed-seed=",
            "(Only sensible together with -d/--distributed option.)  " +
            "The seed value in integer to be used to randomly and uniquely " +
            "distribute the equal set of tests to the nodes.  " +
            $"{DistributedSeed} by default.",
            seed => DistributedSeed = Int32.TryParse(seed, out int s)
                ? s
                : throw new OptionException(
                    "Expected an integer.",
                    "-s/--distributed-seed")
        },
        {
            "x|report-xml-path=",
            "The file path to write a result report in xUnit.net v2 XML format.",
            reportXmlPath => ReportXmlPath = reportXmlPath
        },
        {
            "H|hang-seconds=",
            "Detect hanging tests if a test runs for the specified seconds " +
            "or longer.  0 disables such detection.  0 by default.",
            hangSeconds => HangSeconds = int.TryParse(hangSeconds, out int h)
                ? h
                : throw new OptionException(
                    "Expected an integer.",
                    "-H/--hang-seconds")
        },
        {
            "f|stop-on-fail",
            "Stop the whole running tests when any test fails.",
            stopOnFail => StopOnFail = !(stopOnFail is null)
        },
        {
            "dry-run",
            "Do not run tests, but only discover tests.  More useful with" +
            "-d/--debug option.",
            dryRun => DryRun = !(dryRun is null)
        },
        {
            "d|debug",
            "Print debug logs.",
            debug => DebugLog = !(debug is null)
        },
        {
            "h|help",
            "Show this message and exit.",
            help => Help = !(help is null)
        }
    };

    static readonly ISet<string> ExcludedClasses = new HashSet<string>();
    static readonly ISet<string> ExcludedMethods = new HashSet<string>();
    static readonly ISet<(string, string)> ExcludedTraitConditions =
        new HashSet<(string, string)>();

    static readonly ISet<string> SelectedClasses = new HashSet<string>();
    static readonly ISet<string> SelectedMethods = new HashSet<string>();
    static readonly ISet<(string, string)> SelectedTraitConditions =
        new HashSet<(string, string)>();

    private static int Parallel { get; set; } = 1;
    private static int CurrentNode { get; set; } = 0;
    private static int DistributedNodes { get; set; } = 0;
    private static int DistributedSeed { get; set; } = 0;

    private static string ReportXmlPath { get; set; } = null;
    private static int HangSeconds { get; set; } = 0;
    private static bool StopOnFail { get; set; } = false;
    private static bool DryRun { get; set; } = false;
    private static bool DebugLog { get; set; } = false;
    private static bool Help { get; set; } = false;

    private static (string, string) ParseTraitCondition( string traitCondition)
    {
        int equal = traitCondition.IndexOf('=');
        if (equal < 0)
        {
            return (traitCondition, string.Empty);
        }

        return (
            traitCondition.Substring(0, equal),
            traitCondition.Substring(equal + 1)
        );
    }

    int ExitCode { get; set; } = 0;

    void Start()
    {
        if (Application.isEditor)
        {
            Debug.LogWarning("Xunit tests do not run on the Unity editor mode.");
            return;
        }

        int? exitCode = RunTests();
        if (exitCode is int code)
        {
            Application.Quit(code);
        }
    }

    int? RunTests()
    {
        string[] commandLineArgs = Environment.GetCommandLineArgs();
        string programName = Path.GetFileName(commandLineArgs[0]);
        IEnumerable<string> args = commandLineArgs.Skip(1);
        IList<string> assemblyPaths;
        try
        {
            assemblyPaths = Options.Parse(args);
        }
        catch (OptionException e)
        {
            Console.Error.WriteLine("Error: {0}: {1}", e.OptionName, e.Message);
            Console.Error.WriteLine(
                "Try `{0} --help' for more information",
                programName
            );
            return 1;
        }

        foreach (string path in assemblyPaths)
        {
            if (!Path.IsPathRooted(path))
            {
                Console.Error.WriteLine(
                    "Error: An assembly path should be absolute: `{0}'.",
                    path
                );
                return 1;
            }
        }

        if (Help)
        {
            Console.WriteLine(
                "Usage: {0} [options] ASSEMBLY [ASSEMBLY...]",
                programName
            );
            Console.WriteLine();
            Console.WriteLine("Options:");
            Options.WriteOptionDescriptions(Console.Out);
            return 0;
        }

        if (SelectedClasses.Any() || SelectedMethods.Any() ||
            SelectedTraitConditions.Any() || ExcludedClasses.Any() ||
            ExcludedMethods.Any() || ExcludedTraitConditions.Any() ||
            DistributedNodes > 0)
        {
            Console.Error.WriteLine("Applied filters:");
            PrintFilters("Selected classes", SelectedClasses);
            PrintFilters("Selected methods", SelectedMethods);
            PrintFilters("Selected traits", SelectedTraitConditions);
            PrintFilters("Excluded classes", ExcludedClasses);
            PrintFilters("Excluded methods", ExcludedMethods);
            PrintFilters("Excluded traits", ExcludedTraitConditions);
            Console.Error.WriteLine(
                "  Current node: {0}/{1}",
                CurrentNode,
                DistributedNodes
            );
        }
        else
        {
            Console.Error.WriteLine("Applied no filters.");
        }

        XElement run(string path)
        {
            Console.Error.WriteLine("Discovering tests in {0}...", path);
            XElement assemblyElement = new XElement("assembly");
            try
            {
                var messageSink = new MessageSink
                {
                    OnTest = OnTest,
                    OnExecutionComplete = OnExecutionComplete,
                    LogWriter = DebugLog ? Console.Error : null,
                };
                var summarySink = new DelegatingExecutionSummarySink(messageSink);
                IExecutionSink sink = new DelegatingXmlCreationSink(summarySink, assemblyElement);
                if (HangSeconds > 0)
                {
                    sink = new DelegatingLongRunningTestDetectionSink(
                        sink,
                        TimeSpan.FromSeconds(HangSeconds),
                        messageSink
                    );
                }
                using (sink)
                using (
                    var controller = new XunitFrontController(
                        AppDomainSupport.Denied,
                        path,
                        sourceInformationProvider: new NullSourceInformationProvider(),
                        diagnosticMessageSink: MessageSinkAdapter.Wrap(messageSink)
                    )
                )
                {
                    var configuration = ConfigReader.Load(path);
                    configuration.AppDomain = AppDomainSupport.Required;
                    configuration.DiagnosticMessages = true;
                    configuration.StopOnFail = StopOnFail;
                    configuration.MaxParallelThreads = Parallel;
                    configuration.LongRunningTestSeconds = HangSeconds;
                    ITestFrameworkDiscoveryOptions discoveryOptions =
                        TestFrameworkOptions.ForDiscovery(configuration);
                    discoveryOptions.SetSynchronousMessageReporting(true);
                    discoveryOptions.SetPreEnumerateTheories(false);
                    controller.Find(false, messageSink, discoveryOptions);
                    messageSink.DiscoveryCompletionWaitHandle.WaitOne();
                    ITestCase[] testCases =
                        FilterTestCases(messageSink.TestCases).ToArray();
                    if (DebugLog || DryRun)
                    {
                        lock (this)
                        {
                            Console.Error.WriteLine(
                                "{0} test cases were found in {1}:",
                                testCases.Length,
                                path
                            );
                            foreach (ITestCase testCase in testCases)
                            {
                                Console.Error.WriteLine(
                                    "- {0}",
                                    testCase.DisplayName
                                );
                            }

                            Console.Error.Flush();
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine(
                            "{0} test cases were found in {1}.",
                            testCases.Length,
                            path
                        );
                    }

                    if (!DryRun)
                    {
                        ITestFrameworkExecutionOptions executionOptions =
                            TestFrameworkOptions.ForExecution(configuration);
                        executionOptions.SetDiagnosticMessages(true);
                        executionOptions.SetSynchronousMessageReporting(true);
                        executionOptions.SetStopOnTestFail(StopOnFail);
                        if (Parallel != 1)
                        {
                            executionOptions.SetDisableParallelization(true);
                        }
                        else
                        {
                            executionOptions.SetDisableParallelization(false);
                            executionOptions.SetMaxParallelThreads(Parallel);
                        }

                        controller.RunTests(
                            testCases,
                            sink,
                            executionOptions
                        );
                        sink.Finished.WaitOne();
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("{0}: {1}", e.GetType().Name, e.Message);
                Console.Error.WriteLine(e.StackTrace);
                ExitCode = 1;
            }
            Console.Error.WriteLine("All tests in {0} ran.", path);
            return assemblyElement;
        }

        IEnumerator wait(string[] paths)
        {
            Console.Error.WriteLine("Run {0} test assemblies.", paths.Length);
            var assembliesElement = new XElement("assemblies");
            foreach (string path in paths)
            {
                var task = Task.Run(() => run(path));
                while (!task.IsCompleted)
                {
                    yield return new WaitUntil(() => task.IsCompleted);
                }
                assembliesElement.Add(task.Result);
            }

            if (!DryRun && ReportXmlPath is string reportXmlPath)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(reportXmlPath));
                assembliesElement.Save(reportXmlPath);
                Console.Error.Write("The result report is written: {0}", reportXmlPath);
            }

            Application.Quit(ExitCode);
        }

        StartCoroutine(wait(assemblyPaths.ToArray()));
        return null;
    }

    static IEnumerable<ITestCase> FilterTestCases(IList<ITestCase> testCases)
    {
        IEnumerable<ITestCase> filtered = testCases.Where(t =>
        {
            ITestMethod method = t.TestMethod;
            string className = method.TestClass.Class.Name;
            string methodName = $"{className}.{method.Method.Name}";
            bool selected = true;

            bool IsSatisfied((string, string) pair) =>
                t.Traits.ContainsKey(pair.Item1) &&
                t.Traits[pair.Item1].Contains(pair.Item2);

            if (SelectedClasses.Any() ||
                SelectedMethods.Any() ||
                SelectedTraitConditions.Any())
            {
                selected =
                    SelectedClasses.Contains(className) ||
                    SelectedMethods.Contains(methodName) ||
                    SelectedTraitConditions.Any(IsSatisfied);
            }

            return selected && !ExcludedClasses.Contains(className) &&
                !ExcludedMethods.Contains(methodName) &&
                !ExcludedTraitConditions.Any(IsSatisfied);
        });

        if (DistributedNodes > 0)
        {
            ITestCase[] tests = filtered.ToArray();
            var window = (int)Math.Ceiling(tests.Length / (double)DistributedNodes);
            var random = new Random(DistributedSeed);
            filtered = tests
                .OrderBy(t => t.UniqueID, StringComparer.InvariantCulture)
                .Select(t => (t, random.Next()))
                .OrderBy(pair => pair.Item2)
                .Select(pair => pair.Item1)
                .Skip(CurrentNode * window)
                .Take(window)
                .OrderBy(t =>
                    (t.TestMethod.TestClass.Class.Assembly.Name, t.DisplayName)
                );
        }

        return filtered;
    }

    private void OnTest(TestInfo info)
    {
        switch (info)
        {
            case TestPassedInfo i:
                Console.WriteLine("PASS {0}: {1:}s", i.TestDisplayName, i.ExecutionTime);
                return;

            case TestFailedInfo i:
                Console.WriteLine(
                    "FAIL {0}: {1:}s\n  {2}: {3}\n  {4}\n\nOutput:\n{5}",
                    i.TestDisplayName,
                    i.ExecutionTime,
                    i.ExceptionType,
                    string.Join("\n  ", i.ExceptionMessage.Split('\n')),
                    string.Join("\n  ", i.ExceptionStackTrace.Split('\n')),
                    i.Output.Replace("\n", "\n  ")
                );
                return;

            case TestSkippedInfo i:
                Console.WriteLine("SKIP {0}: {1}", i.TestDisplayName, i.SkipReason);
                return;
        }
    }

    private void OnExecutionComplete(ExecutionCompleteInfo info)
    {
        Console.WriteLine("Total:   {0}", info.TotalTests);
        Console.WriteLine("Passed:  {0}", info.TotalTests - info.TestsFailed - info.TestsSkipped);
        Console.WriteLine("Failed:  {0}", info.TestsFailed);
        Console.WriteLine("Skipped: {0}", info.TestsSkipped);
        if (info.TestsFailed > 0)
        {
            ExitCode = 1;
        }
    }

    private void PrintFilters(string label, ISet<string> filters)
    {
        if (!filters.Any())
        {
            return;
        }

        Console.Error.WriteLine("  {0}:", label);
        foreach (string f in filters.OrderBy(s => s))
        {
            Console.Error.WriteLine("  - {0}", f);
        }

        Console.Error.Flush();
    }

    private void PrintFilters(string label, ISet<(string, string)> filters)
    {
        if (!filters.Any())
        {
            return;
        }

        Console.Error.WriteLine("  {0}:", label);
        foreach ((string f, string v) in filters.OrderBy(pair => pair.Item1))
        {
            Console.Error.WriteLine("  - {0}: {1}", f, v);
        }

        Console.Error.Flush();
    }

    void Update()
    {
    }
}
