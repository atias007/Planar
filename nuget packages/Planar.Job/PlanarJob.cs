using System;
using System.Diagnostics;
using System.Text;

namespace Planar.Job
{
    public static class PlanarJob
    {
        internal static bool DebugMode { get; private set; }

        internal static Stopwatch Stopwatch { get; private set; } = new Stopwatch();

        public static void Start<TJob>()
            where TJob : BaseJob
        {
            Stopwatch.Start();
            ValidateArgs();
            CheckDebugMode();
            var json = GetJsonFromArgs();
            Execute<TJob>(json);
        }

        private static void Execute<TJob>(string json)
             where TJob : BaseJob
        {
            if (DebugMode)
            {
                Console.WriteLine("[x] Debug mode");
                Console.WriteLine("[x] Press [Enter] to start execute job");
                Console.WriteLine("---------------------------------------");
                Console.ReadKey(true);
            }

            var instance = Activator.CreateInstance<TJob>();
            instance.Execute(json).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static void ValidateArgs()
        {
            var args = Environment.GetCommandLineArgs() ?? throw new PlanarJobException("Missing command line argument(s)");
            if (args.Length == 1) { throw new PlanarJobException("Job was executed with no arguments"); }
        }

        private static string GetJsonFromArgs()
        {
            var args = Environment.GetCommandLineArgs();

            var base64 = args[1];
            if (string.IsNullOrWhiteSpace(base64)) { throw new PlanarJobException("Job was executed empty argument"); }
            var json = DecodeBase64ToString(base64);
            return json;
        }

        private static void CheckDebugMode()
        {
            var args = Environment.GetCommandLineArgs();
            DebugMode = Array.Exists(args, a => a.ToLower() == "debug");
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