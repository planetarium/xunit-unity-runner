using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;
using Xunit.Runners;

public class MessageSink : IMessageSinkWithTypes
{
    public IList<ITestCase> TestCases { get; } = new List<ITestCase>();
    public EventWaitHandle DiscoveryCompletionWaitHandle { get; } =
        new EventWaitHandle(false, EventResetMode.ManualReset);
    public Action<TestInfo> OnTest { get; set; } = null;
    public Action<ExecutionCompleteInfo> OnExecutionComplete { get; set; } = null;
    public EventWaitHandle ExecutionCompletionWaitHandle { get; } =
        new EventWaitHandle(false, EventResetMode.ManualReset);
    public TextWriter LogWriter { get; set; }

    public bool OnMessageWithTypes(IMessageSinkMessage message, HashSet<string> messageTypes)
    {
        switch (message)
        {
            case ITestCaseDiscoveryMessage m:
                foreach (ITestCase testCase in m.TestCases)
                {
                    LogWriter?.WriteLine("Discovered: {0}", testCase.DisplayName);
                    TestCases.Add(testCase);
                }
                break;

            case IDiscoveryCompleteMessage _:
                DiscoveryCompletionWaitHandle.Set();
                break;

            case ITestPassed m:
                OnTest?.Invoke(
                    new TestPassedInfo(
                        m.TestClass.Class.Name,
                        m.TestMethod.Method.Name,
                        m.TestCase.Traits,
                        m.Test.DisplayName,
                        m.TestCollection.DisplayName,
                        m.ExecutionTime,
                        m.Output
                    )
                );
                break;

            case ITestFailed m:
                OnTest?.Invoke(
                    new TestFailedInfo(
                        m.TestClass.Class.Name,
                        m.TestMethod.Method.Name,
                        m.TestCase.Traits,
                        m.Test.DisplayName,
                        m.TestCollection.DisplayName,
                        m.ExecutionTime,
                        m.Output,
                        m.ExceptionTypes.FirstOrDefault(),
                        m.Messages.FirstOrDefault(),
                        m.StackTraces.FirstOrDefault()
                    )
                );
                break;

            case ITestSkipped m:
                OnTest?.Invoke(
                    new TestSkippedInfo(
                        m.TestClass.Class.Name,
                        m.TestMethod.Method.Name,
                        m.TestCase.Traits,
                        m.Test.DisplayName,
                        m.TestCollection.DisplayName,
                        m.Reason
                    )
                );
                break;

            case ITestAssemblyFinished m:
                OnExecutionComplete(
                    new ExecutionCompleteInfo(
                        m.TestsRun,
                        m.TestsFailed,
                        m.TestsSkipped,
                        m.ExecutionTime
                    )
                );
                ExecutionCompletionWaitHandle.Set();
                break;

            default:
                LogWriter?.WriteLine("Received message: {0}", message);
                break;
        }

        return true;
    }

    public void Dispose()
    {
    }
}
