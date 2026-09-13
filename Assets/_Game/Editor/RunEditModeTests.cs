using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace FriWorld.EditorTools
{
    /// <summary>
    /// Runs the EditMode tests and writes the outcome to a file.
    ///
    /// The Test Runner window is the normal way to do this, but its API is asynchronous: the
    /// call that starts a run returns long before the results exist. Writing them to a file
    /// means whatever started the run — the menu item, or a script — can read the outcome
    /// afterwards instead of watching the console.
    /// </summary>
    public static class RunEditModeTests
    {
        public const string ResultPath = "Temp/editmode-tests.txt";

        [MenuItem("Routine/Run EditMode Tests", priority = 900)]
        public static void Run()
        {
            if (File.Exists(ResultPath)) File.Delete(ResultPath);

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Writer());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));

            Debug.Log("EditMode tests started; the outcome lands in " + ResultPath);
        }

        sealed class Writer : ICallbacks
        {
            readonly StringBuilder failures = new StringBuilder();

            public void RunStarted(ITestAdaptor testsToRun) { }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.Test.IsSuite) return;
                if (result.TestStatus == TestStatus.Failed)
                    failures.Append(result.Test.FullName).Append("\n    ")
                            .Append((result.Message ?? string.Empty).Replace("\n", "\n    ")).Append('\n');
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var report = new StringBuilder();
                report.Append("passed ").Append(result.PassCount)
                      .Append(", failed ").Append(result.FailCount)
                      .Append(", skipped ").Append(result.SkipCount)
                      .Append(", inconclusive ").Append(result.InconclusiveCount)
                      .Append(", in ").Append(result.Duration.ToString("0.0")).Append(" s\n");

                if (failures.Length > 0) report.Append('\n').Append(failures);

                File.WriteAllText(ResultPath, report.ToString());
                Debug.Log("EditMode tests finished: " + report.ToString());
            }
        }
    }
}
