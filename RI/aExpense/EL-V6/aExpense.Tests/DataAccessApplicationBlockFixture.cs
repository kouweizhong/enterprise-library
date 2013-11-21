﻿// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
using AExpense.DataAccessLayer;
using AExpense.FunctionalTests.Properties;
using AExpense.Tests.Util;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling.Configuration;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AExpense.Tests.Functional
{
    [TestClass]
    public class DataAccessApplicationBlockFixture
    {
        private SimulatedLdapProfileStore ldapStore;
        private IUnityContainer container;

        [TestInitialize]
        public void Init()
        {
            this.container = new UnityContainer();
            ContainerBootstrapper.Configure(container);
        
            this.ldapStore = ProfileStoreHelper.GetProfileStore("aExpense");
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.container.Dispose();
        }

        [TestMethod]
        public void CanGetAttributesForUser()
        {
            string username = "ADATUM\\johndoe";
            var attributes = this.ldapStore.GetAttributesFor(username, new[] { "costCenter", "manager", "displayName" });

            Assert.IsNotNull(attributes);
            Assert.AreEqual(3, attributes.Keys.Count);
            Assert.AreEqual("31023", attributes["costCenter"]);
            Assert.AreEqual("ADATUM\\mary", attributes["manager"]);
            Assert.AreEqual("John Doe", attributes["displayName"]);
        }

        [TestMethod]
        public void WrongUserThrows()
        {
            string username = "ADATUM\\wrongUser";

            var ex = ExceptionAssertHelper.Throws<ArgumentException>(
                () => this.ldapStore.GetAttributesFor(username, new[] { "costCenter", "manager", "displayName" }));

            Assert.AreEqual(Resources.UserDoesNotExistInLDAPMessage, ex.Message);
        }
    }
}
