using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Planar.Job
{
    public static class PlanarJob
    {
        internal static bool DebugMode { get; private set; }
        internal static string Environment { get; private set; } = "Development";
        internal static string[] Args { get; private set; } = System.Environment.GetCommandLineArgs();

        internal static Stopwatch Stopwatch { get; private set; } = new Stopwatch();

        public static void Start<TJob>()
            where TJob : BaseJob
        {
            Stopwatch.Start();
            ValidateArgs();
            FillProperties();
            var json = GetJsonFromArgs();
            Execute<TJob>(json);
        }

        private static void Execute<TJob>(string json)
             where TJob : BaseJob
        {
            if (DebugMode)
            {
                Console.WriteLine("[x] Debug mode: True");
                Console.WriteLine($"[x] Environment: {Environment}");
                Console.WriteLine("---------------------------------------");
                Console.WriteLine("[x] Press [Enter] to start execute job");
                Console.WriteLine("---------------------------------------");
                Console.ReadKey(true);
            }

            var instance = Activator.CreateInstance<TJob>();
            instance.Execute(json);

            if (DebugMode)
            {
                Console.WriteLine("---------------------------------------");
                Console.WriteLine("[x] Press [Enter] to close window");
                Console.WriteLine("---------------------------------------");
                Console.ReadKey(true);
            }
        }

        private static void ValidateArgs()
        {
            var args = Args ?? throw new PlanarJobException("Missing command line argument(s)");
            if (args.Count() == 1) { throw new PlanarJobException("Job was executed with no arguments"); }
        }

        private static string GetJsonFromArgs()
        {
            var base64 = Args[1];
            if (string.IsNullOrWhiteSpace(base64)) { throw new PlanarJobException("Job was executed empty argument"); }
            var json = DecodeBase64ToString(base64);
            return json;
        }

        private static void FillProperties()
        {
            DebugMode = Array.Exists(Args, a => a.ToLower() == "--debug" || a.ToLower() == "-d");
            for (int i = 0; i < Args.Length; i++)
            {
                if (Args[i].ToLower() == "--environment" || Args[i].ToLower() == "-e")
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