using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fclp;

namespace StoryTeller.TestRail.Sync
{
    public class TestRailSyncSettingsFromArgs : ITestRailSyncSettings
    {
        public int ProjectId { get; private set; }
        public string TestRailUrl { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string SpecsFolder { get; private set; }
        public string CredentialsFile { get; private set; }
        public bool PurgeTestRail { get; private set; }

        public TestRailSyncSettingsFromArgs(string[] args)
        {
            var parser = new FluentCommandLineParser();

            parser.Setup<int>("projectid")
                .Callback(value => ProjectId = value)
                .Required();

            parser.Setup<string>("password")
                .Callback(value => Password = value);

            parser.Setup<string>("username")
                .Callback(value => Username = value);

            parser.Setup<string>("credentialsfile")
                .Callback(value => CredentialsFile = value);

            parser.Setup<string>("testrailurl")
                .Callback(value => TestRailUrl = value)
                .Required();

            parser.Setup<string>("specsfolder")
                .Callback(value => SpecsFolder = value)
                .Required();

            parser.Setup<bool>("purge")
                .Callback(purge => PurgeTestRail = purge)
                .SetDefault(false);

            parser.Parse(args);

            if (!string.IsNullOrEmpty(CredentialsFile))
            {
                if (!File.Exists(CredentialsFile))
                    throw new Exception("Could not load credentials file");

                List<string> credentialsFile = File.ReadLines(CredentialsFile).ToList();

                if (credentialsFile.Count < 2)
                    throw new Exception(
                        "File is expected to have at least two lines. The first contianing the username and the second containing the password");

                Username = credentialsFile[0];
                Password = credentialsFile[1];
            }
        }
    }
}
