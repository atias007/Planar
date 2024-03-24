﻿using Microsoft.Extensions.Logging;
using Planar.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Planar.Job
{
    public static class PlanarJob
    {
        public static PlanarJobDebugger Debugger { get; } = new PlanarJobDebugger();
        internal static string Environment { get; private set; } = null!;
        internal static RunningMode Mode { get; set; } = RunningMode.Debug;
        internal static Stopwatch Stopwatch { get; private set; } = new Stopwatch();
        private static List<Argument> Arguments { get; set; } = new List<Argument>();
        private static string? ContextBase64 { get; set; }

        public static void Start<TJob>()
            where TJob : BaseJob, new()
        {
            Stopwatch.Start();
            FillProperties();
            try
            {
                Execute<TJob>();
            }
            catch (Exception ex)
            {
                var log = new LogEntity { Level = LogLevel.Critical, Message = $"Fail to execute {typeof(TJob).Name}" };
                Console.Error.WriteLine(log.ToString());
                log.Message = ex.ToString();
                Console.Error.WriteLine(log.ToString());
            }
        }

        private static string DecodeBase64ToString(string base64String)
        {
            try
            {
                // Convert the Base64 string to a byte array
                var bytes = Convert.FromBase64String(base64String);

                // Convert the byte array to the original string using UTF-8 encoding
                var decodedString = Encoding.UTF8.GetString(bytes);

                return decodedString;
            }
            catch (Exception ex)
            {
                throw new PlanarJobException("Fail to convert Base64 job arg to string", ex);
            }
        }

        private static void Execute<TJob>()
                     where TJob : BaseJob, new()
        {
            string json;
            if (Mode == RunningMode.Debug)
            {
                json = ShowDebugMenu<TJob>();
            }
            else
            {
                json = GetJsonFromArgs();
            }

            if (Mode == RunningMode.Debug)
            {
                Console.WriteLine("---------------------------------------");
                Console.Write("Environment: ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(Environment);
                Console.ResetColor();
                Console.WriteLine("---------------------------------------");
            }

            var instance = Activator.CreateInstance<TJob>();
            instance.Execute(json);

            if (Mode == RunningMode.Debug)
            {
                Console.WriteLine();
                Console.WriteLine("---------------------------------------");
                Console.WriteLine("[x] Press [Enter] to close window");
                Console.WriteLine("---------------------------------------");
                Console.ReadKey(true);
            }
        }

        private static void FillArguments()
        {
            var source = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < source.Length; i++)
            {
                Arguments.Add(new Argument { Key = source[i] });
            }

            for (int i = 1; i < Arguments.Count; i++)
            {
                var item1 = Arguments[i - 1];
                var item2 = Arguments[i];

                if (IsKeyArgument(item1) && !IsKeyArgument(item2))
                {
                    item1.Value = item2.Key;
                    item2.Key = null;
                    i++;
                }
            }
        }

        private static void FillProperties()
        {
            FillArguments();
            if (HasArgument("--planar-service-mode"))
            {
                Mode = RunningMode.Release;
            }
            else
            {
                Mode = RunningMode.Debug;
            }

            ContextBase64 = GetArgument("--context")?.Value;
            Environment = GetArgument("--environment")?.Value ?? "Development";
        }

        private static Argument? GetArgument(string key)
        {
            return Arguments.Find(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetJsonFromArgs()
        {
            if (string.IsNullOrWhiteSpace(ContextBase64)) { throw new PlanarJobException("Job was executed with empty context"); }
            var json = DecodeBase64ToString(ContextBase64);
            return json;
        }

        private static int? GetMenuItem(bool quiet)
        {
            int index = 0;
            var valid = false;
            while (!valid)
            {
                if (!quiet) { Console.Write("Code: "); }
                using var timer = new Timer(60_000);
                timer.Elapsed += TimerElapsed;
                timer.Start();
                var selected = Console.ReadLine();
                timer.Stop();
                if (string.IsNullOrEmpty(selected))
                {
                    if (!quiet) { Console.WriteLine("<Default>"); }
                    return null;
                }

                if (!int.TryParse(selected, out index))
                {
                    ShowErrorMenu($"Selected value '{selected}' is not valid numeric value");
                }
                else if (index > Debugger.Profiles.Count || index <= 0)
                {
                    ShowErrorMenu($"Selected value '{index}' is not exists");
                }
                else
                {
                    valid = true;
                }
            }

            return index;
        }

        private static void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("User input timeout. Close application");
        }

        private static bool HasArgument(string key)
        {
            return Arguments.Exists(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsKeyArgument(Argument arg)
        {
            if (string.IsNullOrEmpty(arg.Key)) { return false; }
            const string template = "^--[a-z,A-Z]";
            return Regex.IsMatch(arg.Key, template, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        }

        private static void PrintMenuItem(string text, string key)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"[{key}] ");
            Console.ResetColor();
            Console.WriteLine(text);
        }

        private static string ShowDebugMenu<TJob>()
            where TJob : BaseJob, new()
        {
            int? selectedIndex;

            if (!Debugger.Profiles.Any())
            {
                Debugger.AddDefaultProfile();
            }

            var typeName = typeof(TJob).Name;
            Console.Write("type the profile code ");
            Console.Write("to start executing the ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"{typeName} ");
            Console.ResetColor();
            Console.WriteLine("job");
            Console.WriteLine();

            var index = 1;
            foreach (var p in Debugger.Profiles)
            {
                PrintMenuItem(p.Key, index.ToString());
                index++;
            }
            Console.WriteLine("------------------");
            PrintMenuItem("<Default>", "Enter");
            Console.WriteLine();
            selectedIndex = GetMenuItem(quiet: false);

            MockJobExecutionContext context;
            IExecuteJobProperties properties;
            if (selectedIndex == null)
            {
                properties = new ExecuteJobPropertiesBuilder().SetDevelopmentEnvironment().Build();
                context = new MockJobExecutionContext(properties);
            }
            else
            {
                properties = Debugger.Profiles.Values.ToList()[selectedIndex.Value - 1];
                context = new MockJobExecutionContext(properties);
            }

            Environment = properties.Environment;
            var json = JsonSerializer.Serialize(context);
            return json;
        }

        private static void ShowErrorMenu(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}