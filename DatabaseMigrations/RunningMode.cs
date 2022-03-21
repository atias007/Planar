namespace DatabaseMigrations
{
    internal enum RunningMode
    {
        AddScript,
        Validate,
        ListScripts,
        DemoExecute,
        PullRequestReady,
        Execute
    }
}