using System;
using System.IO;
using System.Linq;

namespace Planar
{
    internal enum PlanarSpecialFolder
    {
        Settings,
        Calendars,
        Jobs,
        MonitorHooks,
        CentralConfig
    }

    internal static class FolderConsts
    {
        private const string Data = "Data";
        private const string Settings = "Settings";
        private const string Calendars = "Calendars";
        private const string Jobs = "Jobs";
        private const string MonitorHooks = "MonitorHooks";
        private const string CentralConfig = "CentralConfig";

        private static string BasePath => AppDomain.CurrentDomain.BaseDirectory;

        public const string JobFileName = "JobFile.yml";
        public const string JobFileExtPattern = "*.yml";

        public static string GetDataFolder(bool fullPath = false)
        {
            return fullPath ? Path.Combine(BasePath, Data) : Data;
        }

        public static string GetAbsoluteSpecialFilePath(PlanarSpecialFolder planarFolder, params string[] paths)
        {
#if NETSTANDARD2_0
            string specialPath;
            switch (planarFolder)
            {
                case PlanarSpecialFolder.Settings:
                    specialPath = Settings; // Assuming Settings is a string variable/property
                    break;

                case PlanarSpecialFolder.Calendars:
                    specialPath = Calendars; // Assuming Calendars is a string variable/property
                    break;

                case PlanarSpecialFolder.Jobs:
                    specialPath = Jobs; // Assuming Jobs is a string variable/property
                    break;

                case PlanarSpecialFolder.MonitorHooks:
                    specialPath = MonitorHooks; // Assuming MonitorHooks is a string variable/property
                    break;

                default:
                    throw new ArgumentNullException($"special folder {planarFolder} is not supported");
            }
#else
            var specialPath = planarFolder switch
            {
                PlanarSpecialFolder.Settings => Settings,
                PlanarSpecialFolder.Calendars => Calendars,
                PlanarSpecialFolder.Jobs => Jobs,
                PlanarSpecialFolder.MonitorHooks => MonitorHooks,
                _ => throw new ArgumentNullException($"special folder {planarFolder} is not supported"),
            };

#endif

            var folder = Path.Combine(Data, specialPath);
            if (paths == null || paths.Length == 0)
            {
                return folder;
            }

            var parts = paths.ToList();
            parts.Insert(0, folder);
#pragma warning disable IDE0305 // Simplify collection initialization
            var result = Path.Combine(parts.ToArray());
#pragma warning restore IDE0305 // Simplify collection initialization
            return result;
        }

        public static string GetSpecialFilePath(PlanarSpecialFolder planarFolder, params string[] paths)
        {
#if NETSTANDARD2_0

            string specialPath;
            switch (planarFolder)
            {
                case PlanarSpecialFolder.Settings:
                    specialPath = Settings; // Assuming Settings is a string variable/property
                    break;

                case PlanarSpecialFolder.Calendars:
                    specialPath = Calendars; // Assuming Calendars is a string variable/property
                    break;

                case PlanarSpecialFolder.Jobs:
                    specialPath = Jobs; // Assuming Jobs is a string variable/property
                    break;

                case PlanarSpecialFolder.MonitorHooks:
                    specialPath = MonitorHooks; // Assuming MonitorHooks is a string variable/property
                    break;

                default:
                    throw new ArgumentNullException($"special folder {planarFolder} is not supported");
            }
#else
            var specialPath = planarFolder switch
            {
                PlanarSpecialFolder.Settings => Settings,
                PlanarSpecialFolder.Calendars => Calendars,
                PlanarSpecialFolder.Jobs => Jobs,
                PlanarSpecialFolder.MonitorHooks => MonitorHooks,
                PlanarSpecialFolder.CentralConfig => CentralConfig,
                _ => throw new ArgumentNullException($"special folder {planarFolder} is not supported"),
            };
#endif

            var folder = Path.Combine(BasePath, Data, specialPath);
            if (paths == null || paths.Length == 0)
            {
                return folder;
            }

            var parts = paths.Where(p => !string.IsNullOrEmpty(p)).ToList();
            parts.Insert(0, folder);
            var notNullParts = parts.Where(p => p != null).Select(p => p ?? string.Empty).ToArray();
            var result = Path.Combine(notNullParts);
            return result;
        }

        public static string GetPath(params string[] paths)
        {
            var parts = paths.ToList();
            parts.Insert(0, BasePath);
#pragma warning disable IDE0305 // Simplify collection initialization
            var result = Path.Combine(parts.ToArray());
#pragma warning restore IDE0305 // Simplify collection initialization
            return result;
        }
    }
}