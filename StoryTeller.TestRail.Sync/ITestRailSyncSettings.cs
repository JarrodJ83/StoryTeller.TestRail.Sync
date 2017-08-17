namespace StoryTeller.TestRail.Sync
{
    public interface ITestRailSyncSettings
    {
        int ProjectId { get; }
        string TestRailUrl { get; }
        string Username { get; }
        string Password { get; }
        string SpecsFolder { get; }
        string CredentialsFile { get; }
        bool PurgeTestRail { get; }
    }
}
