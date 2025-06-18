using Planar.Hook.Internals;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Planar.Hook
{
    public static class PlanarHook
    {
        public static PlanarHookDebugger Debugger { get; } = new PlanarHookDebugger();
#if NETSTANDARD2_0
        internal static string Environment { get; private set; }
        private static string ContextBase64 { get; set; }

#else
        internal static string Environment { get; private set; } = null!;
        private static string? ContextBase64 { get; set; }

#endif
        internal static RunningMode Mode { get; set; } = RunningMode.Debug;
        internal static Stopwatch Stopwatch { get; private set; } = new Stopwatch();
        private static List<Argument> Arguments { get; set; } = new List<Argument>();
        private static readonly Dictionary<int, string> _menuMapper = new Dictionary<int, string>();

        public static async Task StartAsync<THook>()
            where THook : BaseHook, new()
        {
            FillArguments();
            CheckHealthCheckMode<THook>();

            Stopwatch.Start();
            FillProperties();

            try
            {
                await ExecuteAsync<THook>();
            }
            catch (Exception ex)
            {
                var log = new LogEntity { Level = LogLevel.Critical, Message = $"Fail to execute {typeof(THook).Name}" };
                await Console.Error.WriteLineAsync(log.ToString());
                log.Message = ex.ToString();
                await Console.Error.WriteLineAsync(log.ToString());
            }
        }

        private static void CheckHealthCheckMode<THook>()
            where THook : BaseHook, new()
        {
            if (HasArgument("--planar-healthcheck-mode"))
            {
                var hook = new THook();
                var name = Utils.CleanText(hook.Name);
                var description = Utils.CleanText(hook.Description);
                Console.WriteLine($"<hook.healthcheck.name>{name}</hook.healthcheck.name>");
                Console.WriteLine($"<hook.healthcheck.description>{description}</hook.healthcheck.description>");
                System.Environment.Exit(0);
            }
        }

        private static async Task ExecuteAsync<THook>()
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
            await instance.ExecuteAsync(json);

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
            int selectedIndex;

            Debugger.AddMonitorProfile("Default monitor profile", builder => builder.AddTestUser());
            Debugger.AddMonitorSystemProfile("Default system monitor profile", builder => builder.AddTestUser());

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
            Console.WriteLine();
            selectedIndex = GetMenuItem(quiet: false);

            MonitorMessageWrapper wrapper;

            var name = _menuMapper[selectedIndex];
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

            return JsonSerializer.Serialize(wrapper);
        }

        private static int GetMenuItem(bool quiet)
        {
            int index = 0;
            var valid = false;
            while (!valid)
            {
                if (!quiet) { Console.Write("Code: "); }
                using (var timer = new Timer(60_000))
                {
                    timer.Elapsed += TimerElapsed;
                    timer.Start();
                    var selected = Console.ReadLine();
                    timer.Stop();
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
            }

            return index;
        }

        private static void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("User input timeout. Close application");
            System.Environment.Exit(-1);
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

#if NETSTANDARD2_0

        private static Argument GetArgument(string key)
#else
        private static Argument? GetArgument(string key)
#endif
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