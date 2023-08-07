using Planar.Common;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Planar.Job
{
    public static class PlanarJob
    {
        internal static bool DebugMode { get; private set; } = true;
        internal static string Environment { get; private set; } = "Development";
        internal static string[] Args { get; private set; } = System.Environment.GetCommandLineArgs();
        internal static Stopwatch Stopwatch { get; private set; } = new Stopwatch();
        public static PlanarJobDebugger Debugger { get; } = new PlanarJobDebugger();

        public static void Start<TJob>()
            where TJob : BaseJob, new()
        {
            Stopwatch.Start();
            FillProperties();

            Execute<TJob>();
        }

        private static void Execute<TJob>()
             where TJob : BaseJob, new()
        {
            string json;
            if (DebugMode)
            {
                json = ShowDebugMenu<TJob>();
            }
            else
            {
                json = GetJsonFromArgs();
            }

            var instance = Activator.CreateInstance<TJob>();
            instance.Execute(json);

            if (DebugMode)
            {
                Console.WriteLine();
                Console.WriteLine("---------------------------------------");
                Console.WriteLine("[x] Press [Enter] to close window");
                Console.WriteLine("---------------------------------------");
                Console.ReadKey(true);
            }
        }

        private static string ShowDebugMenu<TJob>()
            where TJob : class, new()
        {
            PrintHeader();

            if (Debugger.Profiles.Any())
            {
                var typeName = typeof(TJob).Name;
                Console.Write("[x] type the profile code ");
                Console.Write("to start executing the ");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write($"{typeName} ");
                Console.ResetColor();
                Console.WriteLine("job");
                Console.WriteLine();
                PrintMenuItem("<Default>", "Enter");
                var index = 1;
                foreach (var p in Debugger.Profiles)
                {
                    PrintMenuItem(p.Key, index.ToString());
                    index++;
                }

                Console.WriteLine();
            }
            else
            {
                var typeName = typeof(TJob).Name;
                Console.Write("[x] Press ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[Enter] ");
                Console.ResetColor();
                Console.Write("to start executing the ");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write($"{typeName} ");
                Console.ResetColor();
                Console.WriteLine("job with default profile");
                Console.WriteLine();
            }

            var selectedIndex = GetMenuItem();
            if (selectedIndex == null)
            {
                var properties = ExecuteJobPropertiesBuilder.CreateBuilderForJob<TJob>().Build();
                var context = new MockJobExecutionContext(properties);
                var json = JsonSerializer.Serialize(context);
                return json;
            }
            else
            {
                var properties = Debugger.Profiles.Values.ToList()[selectedIndex.Value - 1];
                var context = new MockJobExecutionContext(properties);
                var json = JsonSerializer.Serialize(context);
                return json;
            }
        }

        private static int? GetMenuItem()
        {
            int index = 0;
            var valid = false;
            while (!valid)
            {
                Console.Write("Profile number: ");
                var selected = Console.ReadLine();
                if (string.IsNullOrEmpty(selected)) { return null; }

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

        private static void ShowErrorMenu(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void PrintMenuItem(string text, string key)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"[{key}] ");
            Console.ResetColor();
            Console.WriteLine(text);
        }

        private static void PrintHeader()
        {
            Console.Write("[x] ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Debug mode");
            Console.ResetColor();
            Console.Write("[x] Environment: ");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(Environment);
            Console.ResetColor();
        }

        private static string GetJsonFromArgs()
        {
            var base64 = Args[1];
            if (string.IsNullOrWhiteSpace(base64)) { throw new PlanarJobException("Job was executed empty argument"); }
            var json = DecodeBase64ToString(base64);
            return json;
        }

        // TODO: handle!
        private static void FillProperties()
        {
            DebugMode = !Array.Exists(Args, a => a.ToLower() == "--planar-service-mode");

            for (int i = 0; i < Args.Length; i++)
            {
                if (Args[i].ToLower() == "--context")
                {
                    var j = i + 1;
                    if (j < Args.Length || Args[j].StartsWith('-'))
                    {
                        Environment = Args[j];
                    }
                    else
                    {
                        throw new PlanarJobException($"Environment value for argument {Args[i]} was not supplied");
                    }
                }
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
    }
}