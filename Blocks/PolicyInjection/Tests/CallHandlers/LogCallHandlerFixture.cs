//===============================================================================
// Microsoft patterns & practices Enterprise Library
// Policy Injection Application Block
//===============================================================================
// Copyright � Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Filters;
using Microsoft.Practices.EnterpriseLibrary.Logging.Formatters;
using Microsoft.Practices.EnterpriseLibrary.Logging.TraceListeners;
using Microsoft.Practices.EnterpriseLibrary.PolicyInjection.CallHandlers.Configuration;
using Microsoft.Practices.EnterpriseLibrary.PolicyInjection.Configuration;
using Microsoft.Practices.EnterpriseLibrary.PolicyInjection.Tests.ObjectsUnderTest;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.InterceptionExtension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Practices.EnterpriseLibrary.PolicyInjection.CallHandlers.Tests
{
    [TestClass]
    public class LogCallHandlerFixture
    {
        StringWriter writer;
        TextWriterTraceListener listener;
        LogWriter log;
        LogCallHandler callHandler;
        const string beforeMessage = "@@@ Logging before calling target @@@";
        const string afterMessage = "@@@ Logging after calling target @@@";
        const string beginCallTimeMarker = "@@BEGIN CALL TIME@@";
        const string endCallTimeMarker = "@@END CALL TIME@@";

        string preCallTemplate =
            string.Format(
                @"
Call log of parameters for call to {{property(TypeName)}}.{{property(MethodName)}}{{newline}}
Log Category: {{category}}{{newline}}
Event ID: {{eventid}}{{newline}}
Priority: {{priority}}{{newline}}
Severity: {{severity}}{{newline}}
Log message: {{message}}{{newline}}
Call Time: {0}{{property(CallTime)}}{1}{{newline}}
Parameter values:{{newline}}
{{dictionary({{key}} = {{value}}{{newline}})}}
Return value: {{property(ReturnValue)}}{{newline}}
Exception: @@BEGIN EXCEPTION@@{{property(Exception)}}@@END EXCEPTION@@{{newline}}
Call Stack: @@BEGIN CALL STACK@@{{property(CallStack)}}@@END CALL STACK@@{{newline}}",
                beginCallTimeMarker, endCallTimeMarker);

        [TestMethod]
        public void CallGetsLogged()
        {
            LoggingTarget target = CreateTarget();

            Assert.IsTrue(string.IsNullOrEmpty(writer.ToString()));

            target.DoSomething(1, "two", 3.0);
            string logString = writer.ToString();

            Assert.IsFalse(string.IsNullOrEmpty(logString));
        }

        [TestMethod]
        public void ShouldLogBeforeAndAfter()
        {
            LoggingTarget target = CreateTarget();

            target.DoSomething(3, "Not two", 6.02e23);

            AssertContains(beforeMessage);
            AssertContains(afterMessage);
        }

        [TestMethod]
        public void ShouldLogBeforeOnly()
        {
            LoggingTarget target = CreateTarget();
            callHandler.LogAfterCall = false;

            target.DoSomething(5, "Why", 42.0);

            AssertContains(beforeMessage);
            AssertDoesNotContain(afterMessage);
        }

        [TestMethod]
        public void ShouldLogAfterOnly()
        {
            LoggingTarget target = CreateTarget();
            callHandler.LogBeforeCall = false;

            target.DoSomething(5, "Why", 42.0);

            AssertDoesNotContain(beforeMessage);
            AssertContains(afterMessage);
        }

        [TestMethod]
        public void ShouldLogReturnValue()
        {
            LoggingTarget target = CreateTarget();

            int one = 5;
            string two = "xy";
            double three = 8.0;

            target.DoSomething(one, two, three);

            AssertContains(string.Format("Return value: {1}: {0} {2}{3}", one, two, three, Environment.NewLine));
        }

        [TestMethod]
        public void ShouldIncludeParametersByDefault()
        {
            LoggingTarget target = CreateTarget();

            int one = 5;
            string two = "xy";
            double three = 8.0;

            target.DoSomething(one, two, three);

            AssertContains(string.Format("one = {0}{1}", one, Environment.NewLine));
            AssertContains(string.Format("two = {0}{1}", two, Environment.NewLine));
            AssertContains(string.Format("three = {0}{1}", three, Environment.NewLine));
        }

        [TestMethod]
        public void ShouldExcludeParameters()
        {
            LoggingTarget target = CreateTarget();
            callHandler.IncludeParameters = false;

            int one = 5;
            string two = "xy";
            double three = 8.0;

            target.DoSomething(one, two, three);

            AssertDoesNotContain(string.Format("one = {0}{1}", one, Environment.NewLine));
            AssertDoesNotContain(string.Format("two = {0}{1}", two, Environment.NewLine));
            AssertDoesNotContain(string.Format("three = {0}{1}", three, Environment.NewLine));
        }

        [TestMethod]
        public void ShouldNotIncludeCallStackByDefault()
        {
            LoggingTarget target = CreateTarget();

            target.DoSomething(5, "xy", 8.0);

            AssertContains(string.Format("@@BEGIN CALL STACK@@@@END CALL STACK@@{0}", Environment.NewLine));
        }

        [TestMethod]
        public void ShouldIncludeCallStackBefore()
        {
            LoggingTarget target = CreateTarget();
            callHandler.IncludeCallStack = true;
            callHandler.LogBeforeCall = true;
            callHandler.LogAfterCall = false;

            target.DoSomething(2, "foo", 42.0);

            AssertIsMatch(@"@@BEGIN CALL STACK@@.+?@@END CALL STACK@@");
        }

        [TestMethod]
        public void ShouldIncludeCallStackAfter()
        {
            LoggingTarget target = CreateTarget();
            callHandler.IncludeCallStack = true;
            callHandler.LogBeforeCall = false;
            callHandler.LogAfterCall = true;

            target.DoSomething(2, "foo", 42.0);

            AssertIsMatch(@"@@BEGIN CALL STACK@@.+?@@END CALL STACK@@");
        }

        [TestMethod]
        public void ShouldIncludeCallTimeByDefault()
        {
            LoggingTarget target = CreateTarget();
            target.DoSomething(3, "bar", 43.2);

            AssertIsNotMatch(string.Format(@"{0}.+{1}{2}",
                                           afterMessage, beginCallTimeMarker, endCallTimeMarker));
        }

        [TestMethod]
        public void ShouldBeAbleToTurnCallTimeOff()
        {
            LoggingTarget target = CreateTarget();
            callHandler.IncludeCallTime = false;

            target.DoSomething(5, "zzz", 6.02e23);
            AssertIsMatch(
                string.Format(@"{0}.+{1}{2}",
                              afterMessage,
                              beginCallTimeMarker,
                              endCallTimeMarker));
        }

        [TestMethod]
        public void ShouldBeAbleToSetPriority()
        {
            LoggingTarget target = CreateTarget();
            callHandler.Priority = 15;

            target.DoSomething(14, "yyy", 543.0);

            AssertContains(string.Format("Priority: 15{0}", Environment.NewLine));
        }

        [TestMethod]
        public void ShouldbeAbleToSetSeverity()
        {
            LoggingTarget target = CreateTarget();
            callHandler.Severity = TraceEventType.Critical;

            target.DoSomething(15, "xxx", 654.0);
            AssertContains("Severity: Critical");
        }

        [TestMethod]
        public void ShouldDefaultSeverityToInformation()
        {
            LoggingTarget target = CreateTarget();
            target.DoSomething(15, "xxx", 654.0);
            AssertContains("Severity: Information");
        }

        [TestMethod]
        public void ShouldFormatCategoryReplacements()
        {
            LoggingTarget target = CreateTarget();
            callHandler.Categories.Add("Type {type}");
            callHandler.Categories.Add("Namespace {namespace}");
            target.DoSomething(54, "werw", 5340.34);
            AssertContains("Log Category: General, PIAB, Type LoggingTarget, Namespace Microsoft.Practices.EnterpriseLibrary.PolicyInjection.CallHandlers.Tests");
        }

        [TestMethod]
        public void ShouldNotReportReturnValueWhenTargetMethodIsVoid()
        {
            LoggingTarget target = CreateTarget();
            target.DoSomethingElse("dummy");
            AssertContains(string.Format("Return value: {0}", Environment.NewLine));
        }

        [TestMethod]
        [DeploymentItem("LogCallHandler.config")]
        public void ShouldLogOnlyToCategoriesGivenInConfig()
        {
            FileConfigurationSource configSource =
                new FileConfigurationSource("LogCallHandler.config");

            EventLog eventLog = new EventLog("Application");

            int beforeEventLogEntries = eventLog.Entries.Count;

            LoggingTarget target = PolicyInjection.Create<LoggingTarget>(configSource);

            target.DoSomething(1, "two", 3.0);

            int afterEventLogEntries = eventLog.Entries.Count;

            Assert.AreEqual(beforeEventLogEntries + 1, afterEventLogEntries);
        }

        [TestMethod]
        public void ShouldRecordExceptionInLog()
        {
            LoggingTarget target = CreateTarget();
            try
            {
                target.DoSomethingBad();
                Assert.Fail("Should not get here, should have exception");
            }
            catch (ApplicationException) { }

            AssertIsMatch("Exception: @@BEGIN EXCEPTION@@[^@]+@@END EXCEPTION@@");
        }

        [TestMethod]
        public void ShouldNotHaveExceptionWhereThereIsntOne()
        {
            LoggingTarget target = CreateTarget();
            target.DoSomethingElse("hey there");
            AssertContains("Exception: @@BEGIN EXCEPTION@@@@END EXCEPTION@@");
        }

        [TestMethod]
        public void ShouldNotLogReturnValueIfIncludeParametersIsFalse()
        {
            LoggingTarget target = CreateTarget();
            callHandler.LogBeforeCall = false;
            callHandler.IncludeParameters = false;
            target.DoSomething(1, "two", 3.0);
            AssertContains("Return value: \r\n");
        }

        [TestMethod]
        public void ShouldDefaultTo0ForEventId()
        {
            LoggingTarget target = CreateTarget();
            target.DoSomethingElse("boo!");
            AssertContains("Event ID: 0\r\n");
        }

        [TestMethod]
        public void ShouldBeAbleToChangeEventId()
        {
            LoggingTarget target = CreateTarget();
            callHandler.EventId = 42;
            target.DoSomething(2, "three", 4.0);
            AssertContains("Event ID: 42\r\n");
        }

        void AssertContains(string contained)
        {
            string logString = writer.ToString();
            Assert.IsTrue(logString.Contains(contained));
        }

        void AssertDoesNotContain(string contained)
        {
            string logString = writer.ToString();
            Assert.IsFalse(logString.Contains(contained));
        }

        void AssertIsMatch(string pattern)
        {
            string logString = writer.ToString();
            Assert.IsTrue(Regex.IsMatch(logString, pattern, RegexOptions.Singleline));
        }

        void AssertIsNotMatch(string pattern)
        {
            string logString = writer.ToString();
            Assert.IsFalse(Regex.IsMatch(logString, pattern, RegexOptions.Singleline));
        }

        [TestInitialize]
        public void SetupLogWriter()
        {
            writer = new StringWriter();
            TextFormatter formatter = new TextFormatter(preCallTemplate);
            listener = new FormattedTextWriterTraceListener(writer, formatter);

            LogSource logSource = new LogSource("Logging");
            logSource.Listeners.Add(listener);

            log = new LogWriter(new ILogFilter[0], new LogSource[] { logSource },
                                logSource, logSource, logSource, "General", true, true);
        }

        [TestMethod]
        [DeploymentItem("LogCallHandler.config")]
        public void AssembledCorrectlyLogCallHandler()
        {
            FileConfigurationSource configSource =
                new FileConfigurationSource("LogCallHandler.config");

            PolicyInjectionSettings settings = new PolicyInjectionSettings();

            PolicyData policyData = new PolicyData("policy");
            LogCallHandlerData data = new LogCallHandlerData("fooHandler", 66);
            data.BeforeMessage = "before";
            data.AfterMessage = "after";
            data.IncludeCallTime = true;
            data.EventId = 100;
            data.Categories.Add(new LogCallHandlerCategoryEntry("category1"));
            data.Categories.Add(new LogCallHandlerCategoryEntry("category2"));
            policyData.MatchingRules.Add(new CustomMatchingRuleData("matchesEverything", typeof(AlwaysMatchingRule)));
            policyData.Handlers.Add(data);
            settings.Policies.Add(policyData);

            IUnityContainer container = new UnityContainer().AddNewExtension<Interception>();
            settings.ConfigureContainer(container, configSource);

            RuleDrivenPolicy policy = container.Resolve<RuleDrivenPolicy>("policy");

            LogCallHandler handler
                = (LogCallHandler)(policy.GetHandlersFor(MethodInfo.GetCurrentMethod(), container)).ElementAt(0);
            Assert.IsNotNull(handler);
            Assert.AreEqual(66, handler.Order);
            Assert.AreEqual("before", handler.BeforeMessage);
            Assert.AreEqual("after", handler.AfterMessage);
            Assert.AreEqual(true, handler.IncludeCallTime);
            Assert.AreEqual(100, handler.EventId);
            Assert.AreEqual(2, handler.Categories.Count);
            CollectionAssert.Contains(handler.Categories, "category1");
            CollectionAssert.Contains(handler.Categories, "category2");
        }

        [TestMethod]
        [DeploymentItem("LogCallHandler.config")]
        public void ConfiguresCallHandlerAsSingleton()
        {
            FileConfigurationSource configSource =
                new FileConfigurationSource("LogCallHandler.config");

            PolicyInjectionSettings settings = new PolicyInjectionSettings();

            PolicyData policyData = new PolicyData("policy");
            LogCallHandlerData data = new LogCallHandlerData("fooHandler", 66);
            data.BeforeMessage = "before";
            data.AfterMessage = "after";
            data.IncludeCallTime = true;
            data.EventId = 100;
            data.Categories.Add(new LogCallHandlerCategoryEntry("category1"));
            data.Categories.Add(new LogCallHandlerCategoryEntry("category2"));
            policyData.MatchingRules.Add(new CustomMatchingRuleData("matchesEverything", typeof(AlwaysMatchingRule)));
            policyData.Handlers.Add(data);
            settings.Policies.Add(policyData);

            IUnityContainer container = new UnityContainer().AddNewExtension<Interception>();
            settings.ConfigureContainer(container, configSource);

            RuleDrivenPolicy policy = container.Resolve<RuleDrivenPolicy>("policy");

            LogCallHandler handler1
                = (LogCallHandler)(policy.GetHandlersFor(MethodInfo.GetCurrentMethod(), container)).ElementAt(0);
            LogCallHandler handler2
                = (LogCallHandler)(policy.GetHandlersFor(MethodInfo.GetCurrentMethod(), container)).ElementAt(0);

            Assert.AreSame(handler1, handler2);
        }

        [TestMethod]
        public void CreatesHandlerProperlyFromAttributes()
        {
            MethodInfo method = typeof(LoggingTarget).GetMethod("DoSomethingElse");

            Assert.IsNotNull(method);

            object[] attributes = method.GetCustomAttributes(typeof(LogCallHandlerAttribute), false);

            Assert.AreEqual(1, attributes.Length);

            LogCallHandlerAttribute att = attributes[0] as LogCallHandlerAttribute;
            ICallHandler callHandler = att.CreateHandler(null);

            Assert.IsNotNull(callHandler);
            Assert.AreEqual(9, callHandler.Order);
        }

        [TestCleanup]
        public void TeardownLogWriter()
        {
            listener.Flush();
            log.Dispose();
            log = null;
            listener = null; // Disposing the log also disposes the trace listener
            writer.Flush();
            writer.Dispose();
            writer = null;
        }

        LoggingTarget CreateTarget()
        {
            callHandler = new LogCallHandler(log);
            callHandler.LogBeforeCall = callHandler.LogAfterCall = true;
            callHandler.BeforeMessage = beforeMessage;
            callHandler.AfterMessage = afterMessage;
            callHandler.Categories.Add("General");
            callHandler.Categories.Add("PIAB");

            IUnityContainer container = new UnityContainer().AddNewExtension<Interception>();
            container.Configure<Interception>()
                .SetDefaultInjectorFor<LoggingTarget>(new TransparentProxyPolicyInjector())
                .AddPolicy("Logging")
                    .AddMatchingRule(new TypeMatchingRule("LoggingTarget"))
                    .AddCallHandler(callHandler);

            return container.Resolve<LoggingTarget>();
        }
    }

    public class LoggingTarget : MarshalByRefObject
    {
        public string DoSomething(int one,
                                  string two,
                                  double three)
        {
            return string.Format("{1}: {0} {2}", one, two, three);
        }

        public void DoSomethingBad()
        {
            throw new ApplicationException("Exception thrown here");
        }

        [LogCallHandler(Order = 9)]
        public void DoSomethingElse(string message) { }
    }
}