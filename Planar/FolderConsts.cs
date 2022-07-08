using System;

namespace Planar
{
    internal sealed class FolderConsts
    {
        public const string Data = "Data";
        public const string Settings = "Settings";
        public const string Calendars = "Calendars";
        public const string Jobs = "Jobs";
        public const string MonitorHooks = "MonitorHooks";

        public static string BasePath => AppDomain.CurrentDomain.BaseDirectory;
    }
}