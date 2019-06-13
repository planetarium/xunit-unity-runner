using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Options;
using UnityEngine;
using Xunit;
using Xunit.Abstractions;
using Xunit.Runners;

public class EntryPoint : MonoBehaviour
{
    private static readonly OptionSet Options = new OptionSet
    {
        {
            "C|exclude-class=",
            "Exclude a test class by its fully qualified name.  " +
            "This option can be used multiple times.",
            className => ExcludedClasses.Add(className)
        },
        {
            "M|exclude-method=",
            "Exclude a test method by its fully qualified name.  " +
            "This option can be used multiple times.",
            method => ExcludedMethods.Add(method)
        },
        {
            "T|exclude-trait-condition=",
            "Exclude a trait condition (e.g., `-T TraitName=value').  " +
            "This option can be used multiple times.",
            traitCondition =>
            {
                int equal = traitCondition.IndexOf('=');
                string traitName, value;
                if (equal < 0)
                {
                    traitName = traitCondition;
                    value = string.Empty;
                }
                else
                {
                    traitName = traitCondition.Substring(0, equal);
                    value = traitCondition.Substring(equal + 1);
                }
                ExcludedTraitConditions.Add((traitName, value));
            }
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
    private static bool Help { get; set; }= false;

    int ExitCode { get; set; } = 0;

    void Start()
    {
        if (Application.isEditor)
        {
            Debug.LogWarning("Xunit tests do not run on the Unity editor mode.");
            return;
        }

        int? exitCode = Main();
        if (exitCode is int code)
        {
            Application.Quit(code);
        }
    }

    int? Main()
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
            Console.Error.WriteLine("Error: {0}", e.Message);
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

        void run(string path)
        {
            Console.Error.WriteLine("Discovering tests in {0}...", path);
            try
            {
                var messageSink = new MessageSink
                {
                    OnTest = OnTest,
                    OnExecutionComplete = OnExecutionComplete,
                };
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
                    ITestFrameworkDiscoveryOptions discoveryOptions =
                        TestFrameworkOptions.ForDiscovery(configuration);
                    discoveryOptions.SetSynchronousMessageReporting(true);
                    discoveryOptions.SetPreEnumerateTheories(false);
                    controller.Find(false, messageSink, discoveryOptions);
                    messageSink.DiscoveryCompletionWaitHandle.WaitOne();
                    ITestCase[] testCases =
                        FilterTestCases(messageSink.TestCases).ToArray();
                    Console.Error.WriteLine(
                        "{0} test cases were found in {1}.",
                        testCases.Length,
                        path
                    );
                    ITestFrameworkExecutionOptions executionOptions =
                        TestFrameworkOptions.ForExecution(configuration);
                    executionOptions.SetSynchronousMessageReporting(true);
                    controller.RunTests(
                        testCases,
                        messageSink,
                        executionOptions
                    );
                    messageSink.ExecutionCompletionWaitHandle.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("{0}: {1}", e.GetType().Name, e.Message);
                Console.Error.WriteLine(e.StackTrace);
                ExitCode = 1;
            }
            Console.Error.WriteLine("All tests in {0} ran.", path);
        }

        IEnumerator wait(string[] paths)
        {
            Console.WriteLine("Run {0} test assemblies.", paths.Length);
            foreach (string path in paths)
            {
                var task = Task.Run(() => run(path));
                while (!task.IsCompleted)
                {
                    yield return new WaitUntil(() => task.IsCompleted);
                }
            }

            Application.Quit(ExitCode);
        }

        StartCoroutine(wait(assemblyPaths.ToArray()));
        return null;
    }

    static IEnumerable<ITestCase> FilterTestCases(IList<ITestCase> testCases) =>
        testCases.Where(t =>
        {
            ITestMethod method = t.TestMethod;
            string className = method.TestClass.Class.Name;
            string methodName = $"{className}.{method.Method.Name}";
            return !ExcludedClasses.Contains(className) &&
                !ExcludedMethods.Contains(methodName) &&
                !ExcludedTraitConditions.Any(pair =>
                    t.Traits.ContainsKey(pair.Item1) &&
                    t.Traits[pair.Item1].Contains(pair.Item2)
                );
        });

    private void OnTest(TestInfo info)
    {
        switch (info)
        {
            case TestPassedInfo i:
                Console.WriteLine("PASS {0}: {1:}s", i.TestDisplayName, i.ExecutionTime);
                return;

            case TestFailedInfo i:
                Console.WriteLine(
                    "FAIL {0}: {1:}s\n  {2}: {3}\n  {4}",
                    i.TestDisplayName,
                    i.ExecutionTime,
                    i.ExceptionType,
                    string.Join("\n  ", i.ExceptionMessage.Split('\n')),
                    string.Join("\n  ", i.ExceptionStackTrace.Split('\n'))
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

    void Update()
    {
    }
}
