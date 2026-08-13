using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public static class ConversionEditModeTestRunner
    {
        [MenuItem("Booter & BigARM/Validation/Run Conversion EditMode Tests")]
        public static void Run()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ResultCallbacks());
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "BooterBigArm.Editor.Tests" }
            })
            {
                runSynchronously = true
            });
        }

        private sealed class ResultCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log("CONVERSION_EDITMODE_TESTS: started");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var summary = $"CONVERSION_EDITMODE_TESTS: Passed={result.PassCount} Failed={result.FailCount} "
                              + $"Skipped={result.SkipCount} Inconclusive={result.InconclusiveCount}";
                if (result.FailCount > 0)
                {
                    Debug.LogError(summary);
                }
                else
                {
                    Debug.Log(summary);
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.HasChildren
                    && result.TestStatus == UnityEditor.TestTools.TestRunner.Api.TestStatus.Failed)
                {
                    Debug.LogError($"CONVERSION_EDITMODE_TEST_FAILURE: {result.FullName}\n{result.Message}\n{result.StackTrace}");
                }
            }
        }
    }
}
