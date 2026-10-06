using System;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BooterBigArm.Tests
{
    [InitializeOnLoad]
    internal static class AuditTestCallbacks
    {
        private static readonly TestRunnerApi Api;
        static AuditTestCallbacks()
        {
            if (!Environment.GetCommandLineArgs().Contains("-repositoryAuditProgress")) return;
            Api = ScriptableObject.CreateInstance<TestRunnerApi>();
            Api.hideFlags = HideFlags.HideAndDontSave;
            Api.RegisterCallbacks(new Callbacks());
        }
        private sealed class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) => Debug.Log("AUDIT_TEST_RUN_STARTED");
            public void RunFinished(ITestResultAdaptor result) => Debug.Log("AUDIT_TEST_RUN_FINISHED " + result.ResultState);
            public void TestStarted(ITestAdaptor test) => Debug.Log("AUDIT_TEST_STARTED " + test.FullName);
            public void TestFinished(ITestResultAdaptor result) => Debug.Log("AUDIT_TEST_FINISHED " + result.FullName + " " + result.ResultState);
        }
    }
}
