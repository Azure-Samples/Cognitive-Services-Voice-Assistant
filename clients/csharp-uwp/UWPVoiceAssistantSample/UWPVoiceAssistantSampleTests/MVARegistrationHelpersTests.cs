// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using UWPVoiceAssistantSample;
using System;

namespace MVARegistrationHelpersTests
{
    [TestClass]
    public class MVARegistrationHelpersTests
    {
        public bool TaskRegistered;

        [TestMethod]
        public void GetIsBackgroundTaskRegisteredTest()
        {
            this.TaskRegistered = MVARegistrationHelpers.IsBackgroundTaskRegistered;

            MVARegistrationHelpers.IsBackgroundTaskRegistered = false;
            Assert.IsFalse(MVARegistrationHelpers.IsBackgroundTaskRegistered);

            MVARegistrationHelpers.IsBackgroundTaskRegistered = true;
            Assert.IsTrue(MVARegistrationHelpers.IsBackgroundTaskRegistered);
        }

        [TestMethod]
        public void GetUnlockLimitedAccessFeatureTest()
        {
            try
            {
                MVARegistrationHelpers.UnlockLimitedAccessFeature();

                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(MethodAccessException));
                Assert.Fail($"UnlockLimitedAccessFeature failed with exception: {ex.Message}");
            }
        }
    }
}