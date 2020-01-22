namespace VoiceAssistantTest
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Main Service Test Class.
    /// </summary>
    [TestClass]
    public class MainServiceTest
    {
        /// <summary>
        /// StartUp Test Method obtains VoiceAssistantTestConfig.json File from Directory.
        /// Directory = bin/Debug when in debugger and external Directory when running with DLL.
        /// </summary>
        [TestMethod]
        public void StartUpTest()
        {
            string configFile;

            string getEnvironmentVariable = Environment.GetEnvironmentVariable(ProgramConstants.ConfigFileEnvVariable, EnvironmentVariableTarget.Process);

            string getDefaultVariable = Directory.GetCurrentDirectory() + "\\" + ProgramConstants.DefaultConfigFile;

            if (getEnvironmentVariable != null)
            {
                configFile = getEnvironmentVariable;
            }
            else
            {
                configFile = getDefaultVariable;
            }

            int result = MainService.StartUp(configFile).Result;

            if (result == 0)
            {
                Assert.IsTrue(true);
            }
            else
            {
                Assert.IsTrue(false);
            }
        }
    }
}
