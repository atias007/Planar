using Planar.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Planar.Job
{
    public static partial class PlanarJob
    {
#if NETSTANDARD2_0
        internal static string Environment { get; private set; }
#else
        internal static string Environment { get; private set; } = null!;
#endif

        public static PlanarJobDebugger Debugger { get; } = new PlanarJobDebugger();
        private static List<Argument> Arguments { get; set; } = new List<Argument>();
        internal static RunningMode Mode { get; set; } = RunningMode.Release;

        private static Type ShowJobMenu(IHostetJobProperties properties)
        {
            var jobTypes = properties.JobTypes.Select(d => d).ToList();
            if (jobTypes.Count == 1) { return jobTypes[0]; }
            Console.WriteLine("Multiple job definitions found. Please select a job to execute:");
            Console.WriteLine();
            var index = 1;
            foreach (var jobType in jobTypes)
            {
                PrintMenuItem(jobType.Name, index.ToString());
                index++;
            }
            Console.WriteLine();

            while (true)
            {
                var selectedIndex = GetMenuItem(quiet: false, jobTypes.Count);
                if (selectedIndex == null || selectedIndex <= 0 || selectedIndex > jobTypes.Count)
                {
                    continue;
                }

                return jobTypes[selectedIndex.Value - 1];
            }
        }

        private static string ShowDebugMenu(Type JobType)
        {
            int? selectedIndex;

            var hasProfiles = Debugger.Profiles.Any();
            if (hasProfiles)
            {
                Console.Write("Type the Profile code ");
                Console.Write("to start executing ");
            }
            else
            {
                Console.Write("type [Enter] to start executing the ");
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"{JobType.Name} ");
            Console.ResetColor();
            Console.WriteLine();

            var index = 1;
            foreach (var p in Debugger.Profiles)
            {
                PrintMenuItem(p.Key, index.ToString());
                index++;
            }

            if (hasProfiles)
            {
                Console.WriteLine("------------------");
                PrintMenuItem("<Default>", "Enter");
                Console.WriteLine();
            }

            selectedIndex = GetMenuItem(quiet: !hasProfiles, Debugger.Profiles.Count);

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

        private static int? GetMenuItem(bool quiet, int maxIndex)
        {
            int index = 0;
            var valid = false;
            while (!valid)
            {
                if (!quiet) { Console.Write("Enter number: "); }
                using (var timer = new Timer(60_000))
                {
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
                    else if (index > maxIndex || index <= 0)
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
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine("User input timeout. Terminate application");
            Console.ResetColor();
            System.Environment.Exit(-1);
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

        private static void ShowErrorMenu(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static async Task<bool> Debug(IHostetJobProperties properties, CancellationToken cancellationToken)
        {
            if (Mode != RunningMode.Debug) { return false; }
            var type = ShowJobMenu(properties);
            return await Debug(type, cancellationToken);
        }

        private static async Task<bool> Debug(Type jobType, CancellationToken cancellationToken)
        {
            if (Mode != RunningMode.Debug) { return false; }

            var json = ShowDebugMenu(jobType);

            await Console.Out.WriteLineAsync("---------------------------------------");
            await Console.Out.WriteAsync(">> Environment: ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            await Console.Out.WriteLineAsync(Environment);
            Console.ResetColor();
            await Console.Out.WriteLineAsync("---------------------------------------");

            var (Success, Instance) = await Execute(jobType, planarHostName: null, json, cancellationToken);

            await Instance.PrintDebugSummary(Success);
            await Console.Out.WriteLineAsync("---------------------------------------");
            await Console.Out.WriteLineAsync(">> Press any key to exit");
            await Console.Out.WriteLineAsync("---------------------------------------");
            using (var timer = new Timer(60_000))
            {
                timer.Elapsed += TimerElapsed;
                timer.Start();
                Console.ReadKey(true);
                timer.Stop();
            }

            return true;
        }

#if NETSTANDARD2_0

        private static async Task<(bool Success, BaseJob Instance)> Execute(Type jobType, string planarHostName, string json, CancellationToken cancellationToken)
#else
        private static async Task<(bool Success, BaseJob Instance)> Execute(Type jobType, string? planarHostName, string json, CancellationToken cancellationToken)
#endif
        {
            var instance = GetInstance(jobType);
            return (await instance.Execute(json, planarHostName, cancellationToken), instance);
        }

        private static BaseJob GetInstance(Type jobType) => Activator.CreateInstance(jobType) as BaseJob ??
                throw new InvalidOperationException($"Failed to create an instance of {jobType?.Name}");
    }
}