namespace DatabaseMigrations
{
    public enum RunningMode
    {
        AddScript,
        Validate,
        ListScripts,
        DemoExecute,
        PullRequestReady,
        Execute,
        EnsureDatabase
    }
}