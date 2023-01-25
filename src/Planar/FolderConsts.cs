﻿using System;
using System.IO;
using System.Linq;

namespace Planar
{
    internal enum PlanarSpecialFolder
    {
        Settings,
        Calendars,
        Jobs,
        MonitorHooks
    }

    internal static class FolderConsts
    {
        private const string Data = "Data";
        private const string Settings = "Settings";
        private const string Calendars = "Calendars";
        private const string Jobs = "Jobs";
        private const string MonitorHooks = "MonitorHooks";

        private static string BasePath => AppDomain.CurrentDomain.BaseDirectory;

        public const string JobFileName = "JobFile.yml";

        public static string GetAbsoluteSpecialFilePath(PlanarSpecialFolder planarFolder, params string[] paths)
        {
            var specialPath = planarFolder switch
            {
                PlanarSpecialFolder.Settings => Settings,
                PlanarSpecialFolder.Calendars => Calendars,
                PlanarSpecialFolder.Jobs => Jobs,
                PlanarSpecialFolder.MonitorHooks => MonitorHooks,
                _ => throw new ArgumentNullException($"Special folder {planarFolder} is not supported"),
            };

            var folder = Path.Combine(Data, specialPath);
            if (paths == null || !paths.Any())
            {
                return folder;
            }

            var parts = paths.ToList();
            parts.Insert(0, folder);
            var result = Path.Combine(parts.ToArray());
            return result;
        }

        public static string GetSpecialFilePath(PlanarSpecialFolder planarFolder, params string[] paths)
        {
            var specialPath = planarFolder switch
            {
                PlanarSpecialFolder.Settings => Settings,
                PlanarSpecialFolder.Calendars => Calendars,
                PlanarSpecialFolder.Jobs => Jobs,
                PlanarSpecialFolder.MonitorHooks => MonitorHooks,
                _ => throw new ArgumentNullException($"Special folder {planarFolder} is not supported"),
            };

            var folder = Path.Combine(BasePath, Data, specialPath);
            if (paths == null || !paths.Any())
            {
                return folder;
            }

            var parts = paths.ToList();
            parts.Insert(0, folder);
            var result = Path.Combine(parts.ToArray());
            return result;
        }

        public static string GetPath(params string[] paths)
        {
            var parts = paths.ToList();
            parts.Insert(0, BasePath);
            var result = Path.Combine(parts.ToArray());
            return result;
        }
    }
}