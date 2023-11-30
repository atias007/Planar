using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Planar.Monitor.Hook
{
    public static class PlanarHook
    {
        internal static string Environment { get; private set; } = null!;
        internal static RunningMode Mode { get; set; } = RunningMode.Debug;
        internal static Stopwatch Stopwatch { get; private set; } = new Stopwatch();
        private static List<Argument> Arguments { get; set; } = new List<Argument>();
        private static string? ContextBase64 { get; set; }

        public static void Start<THook>()
            where THook : BaseHook, new()
        {
            Stopwatch.Start();
            FillProperties();
        }

        private static void Execute<THook>()
                    where THook : BaseHook, new()
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
            if (string.IsNullOrWhiteSpace(ContextBase64)) { throw new PlanarHookException("Job was executed with empty context"); }
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
                throw new PlanarHookException("Fail to convert Base64 job arg to string", ex);
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
    }
}