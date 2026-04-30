using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Planar.Job
{
    public static partial class PlanarJob
    {
#if NETSTANDARD2_0
        private static string ContextBase64 { get; set; }
#else
        private static string? ContextBase64 { get; set; }
#endif
        internal static PlanarJobStartProperties Properties { get; private set; } = PlanarJobStartProperties.Default;

        public static Task StartAsync<TJob>()
                    where TJob : BaseJob, new()
        {
            return StartAsync<TJob>(PlanarJobStartProperties.Default);
        }

        public static async Task StartAsync<TJob>(PlanarJobStartProperties properties)
            where TJob : BaseJob, new()
        {
            Properties = properties;

            Stopwatch.Start();
            FillProperties();
            try
            {
                await Execute<TJob>();
            }
            catch (Exception ex)
            {
                var log = new LogEntity { Level = LogLevel.Critical, Message = $"Fail to execute {typeof(TJob).Name}" };
                await Console.Error.WriteLineAsync(log.ToString());
                log.Message = ex.ToString();
                await Console.Error.WriteLineAsync(log.ToString());
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
                throw new PlanarJobException("Fail to convert Base64 job context argument to string", ex);
            }
        }

        private static async Task Execute<TJob>()
                     where TJob : BaseJob, new()
        {
            string json;
            if (Mode == RunningMode.Debug)
            {
                json = ShowDebugMenu(typeof(TJob));
            }
            else
            {
                json = GetJsonFromArgs();
            }

            if (Mode == RunningMode.Debug)
            {
                await Console.Out.WriteLineAsync("---------------------------------------");
                await Console.Out.WriteAsync(">> Environment: ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                await Console.Out.WriteLineAsync(Environment);
                Console.ResetColor();
                await Console.Out.WriteLineAsync("---------------------------------------");
            }

            var instance = Activator.CreateInstance<TJob>();
            var success = await instance.Execute(json);

            if (Mode == RunningMode.Debug)
            {
                await instance.PrintDebugSummary(success);
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

        private static string GetJsonFromArgs()
        {
            if (string.IsNullOrWhiteSpace(ContextBase64)) { throw new PlanarJobException("Job was executed with empty context"); }
            var json = DecodeBase64ToString(ContextBase64);
#if NETSTANDARD2_0
            if (json.StartsWith("[") && json.EndsWith("]"))
#else
            if (json.StartsWith('[') && json.EndsWith(']'))
#endif
            {
                json = GetContextFromTemporaryFile(json);
            }

            return json;
        }

        private static string GetContextFromTemporaryFile(string value)
        {
            const string contextFolder = "context";
#if NETSTANDARD2_0
            var filename = value.Substring(1, value.Length - 2);
#else
            var filename = value[1..^1];
#endif
            filename = Path.Combine(contextFolder, $"{filename}.ctx");
            if (!File.Exists(filename)) { throw new PlanarJobException($"Temporary file '{filename}' not found"); }
            var content = File.ReadAllText(filename);
            SafeDeleteFile(filename);
            if (string.IsNullOrWhiteSpace(content)) { throw new PlanarJobException($"Job was executed with empty context file '{filename}'"); }
            var json = DecodeBase64ToString(content);
            return json;
        }

        private static void SafeDeleteFile(string filename)
        {
            try
            {
                File.Delete(filename);
            }
            catch
            {
                // *** Do nothing ***
            }
        }
    }
}