using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Planar.Monitor.Hook
{
    public static class PlanarHook
    {
        public static PlanarHookDebugger Debugger { get; } = new PlanarHookDebugger();
        internal static string Environment { get; private set; } = null!;
        internal static RunningMode Mode { get; set; } = RunningMode.Debug;
        internal static Stopwatch Stopwatch { get; private set; } = new Stopwatch();
        private static List<Argument> Arguments { get; set; } = new List<Argument>();
        private static string? ContextBase64 { get; set; }
        private static Dictionary<int, string> _menuMapper = new Dictionary<int, string>();

        public static void Start<THook>()
            where THook : BaseHook, new()
        {
            Stopwatch.Start();
            FillProperties();

            try
            {
                Execute<THook>();
            }
            catch (Exception ex)
            {
                var log = new LogEntity { Level = LogLevel.Critical, Message = $"Fail to execute {typeof(THook).Name}" };
                Console.Error.WriteLine(log.ToString());
                log.Message = ex.ToString();
                Console.Error.WriteLine(log.ToString());
            }
        }

        private static void Execute<THook>()
            where THook : BaseHook, new()
        {
            string json;
            if (Mode == RunningMode.Debug)
            {
                json = ShowDebugMenu<THook>();
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

            var instance = Activator.CreateInstance<THook>();
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

        private static string ShowDebugMenu<THook>()
             where THook : BaseHook, new()
        {
            int? selectedIndex;

            if (Debugger.MonitorProfiles.Any() || Debugger.MonitorSystemProfiles.Any())
            {
                var typeName = typeof(THook).Name;
                Console.Write("type the profile code ");
                Console.Write("to start executing the ");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write($"{typeName} ");
                Console.ResetColor();
                Console.WriteLine("hook");
                Console.WriteLine();

                var index = 1;
                foreach (var p in Debugger.MonitorProfiles)
                {
                    PrintMenuItem(p.Key, index.ToString());
                    _menuMapper.Add(index, p.Key);
                    index++;
                }

                foreach (var p in Debugger.MonitorSystemProfiles)
                {
                    PrintMenuItem(p.Key, index.ToString());
                    _menuMapper.Add(index, p.Key);
                    index++;
                }

                Console.WriteLine("------------------");
                PrintMenuItem("<Default>", "Enter");
                Console.WriteLine();
                selectedIndex = GetMenuItem(quiet: false);
            }
            else
            {
                var typeName = typeof(THook).Name;
                Console.Write("[x] Press ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[Enter] ");
                Console.ResetColor();
                Console.Write("to start executing the ");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write($"{typeName} ");
                Console.ResetColor();
                Console.WriteLine("hook with default profile");
                Console.WriteLine();
                selectedIndex = GetMenuItem(quiet: true);
            }

            MonitorMessageWrapper wrapper;

            if (selectedIndex == null)
            {
                var details = new MonitorDetailsBuilder().SetDevelopmentEnvironment().Build();
                wrapper = new MonitorMessageWrapper((MonitorDetails)details);
            }
            else
            {
                var name = _menuMapper[selectedIndex.Value];
                if (Debugger.MonitorProfiles.TryGetValue(name, out var monitor))
                {
                    wrapper = new MonitorMessageWrapper((MonitorDetails)monitor);
                }
                else if (Debugger.MonitorSystemProfiles.TryGetValue(name, out var systemMonitor))
                {
                    wrapper = new MonitorMessageWrapper((MonitorSystemDetails)systemMonitor);
                }
                else
                {
                    throw new PlanarHookException("Hook was executed with empty context");
                }
            }

            return JsonSerializer.Serialize(wrapper);
        }

        private static int? GetMenuItem(bool quiet)
        {
            int index = 0;
            var valid = false;
            while (!valid)
            {
                if (!quiet) { Console.Write("Code: "); }
                var selected = Console.ReadLine();
                if (string.IsNullOrEmpty(selected))
                {
                    if (!quiet) { Console.WriteLine("<Default>"); }
                    return null;
                }

                if (!int.TryParse(selected, out index))
                {
                    ShowErrorMenu($"Selected value '{selected}' is not valid numeric value");
                }
                else if (index > Debugger.MonitorProfiles.Count + Debugger.MonitorSystemProfiles.Count || index <= 0)
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

        private static void PrintMenuItem(string text, string key)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"[{key}] ");
            Console.ResetColor();
            Console.WriteLine(text);
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

        private static Argument? GetArgument(string key)
        {
            return Arguments.Find(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetJsonFromArgs()
        {
            if (string.IsNullOrWhiteSpace(ContextBase64)) { throw new PlanarHookException("Hook was executed with empty context"); }
            var json = DecodeBase64ToString(ContextBase64);
            return json;
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
                throw new PlanarHookException("Fail to convert Base64 monitor arg to string", ex);
            }
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

        private static void ShowErrorMenu(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}