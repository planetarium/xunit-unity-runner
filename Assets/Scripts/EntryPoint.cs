using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Xunit;
using Xunit.Abstractions;
using Xunit.Runners;

public class EntryPoint : MonoBehaviour
{
    int ExitCode { get; set; } = 0;

    void Start()
    {
        if (Application.isEditor)
        {
            Debug.LogWarning("Xunit tests do not run on the Unity editor mode.");
            return;
        }

        IEnumerable<string> assemblyPaths = Environment.GetCommandLineArgs().Skip(1);

        void run(string path)
        {
            Console.WriteLine("Running tests in {0}...", path);
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
                    controller.Find(false, messageSink, discoveryOptions);
                    messageSink.DiscoveryCompletionWaitHandle.WaitOne();
                    Console.WriteLine(
                        "{0} test cases were found in {1}.",
                        messageSink.TestCases.Count,
                        path
                    );
                    ITestFrameworkExecutionOptions executionOptions =
                        TestFrameworkOptions.ForExecution(configuration);
                    executionOptions.SetSynchronousMessageReporting(true);
                    controller.RunTests(messageSink.TestCases, messageSink, executionOptions);
                    messageSink.ExecutionCompletionWaitHandle.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("{0}: {1}", e.GetType().Name, e.Message);
                Console.Error.WriteLine(e.StackTrace);
                ExitCode = 1;
            }
            Console.WriteLine("All tests in {0} ran.", path);
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
