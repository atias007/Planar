namespace DatabaseMigrations
{
    public static class Current
    {
        internal static Status Status { get; set; } = Status.Success;

        internal const string ModuleName = "Planar";

        internal static string RunningPath => AppContext.BaseDirectory;

        internal static readonly string ProjectPath = new DirectoryInfo(Path.Combine(RunningPath, "..", "..", "..", "..", "DatabaseMigrations.Factory")).FullName;

        internal static string ScriptsPath => Path.Combine(ProjectPath, "Scripts");
    }
}