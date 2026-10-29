using Microsoft.Extensions.Logging;
using Planar.Common;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Planar.Job
{
    public static partial class PlanarJob
    {
#if NETSTANDARD2_0
        private static string ContextBase64 { get; set; }
        private static string FileIdentifier { get; set; }
#else
        private static string? ContextBase64 { get; set; }
        private static string? FileIdentifier { get; set; }
#endif

        public static PlanarJobDebugger Debugger => PlanarJobCommon.Debugger;

        private static bool Encrypted { get; set; } = false;

        private static PlanarJobStartProperties _properties = PlanarJobStartProperties.Default;

        public static Task StartAsync<TJob>()
                    where TJob : BaseJob, new()
        {
            return StartAsync<TJob>(PlanarJobStartProperties.Default);
        }

        public static async Task StartAsync<TJob>(PlanarJobStartProperties properties)
            where TJob : BaseJob, new()
        {
            _properties = properties;
            PlanarJobCommon.Properties = properties;

            FillProperties();
            SetupGracefulShutdown();

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
            finally
            {
                PlanarJobCommon.SafeDispose();
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
            if (PlanarJobCommon.Mode == RunningMode.Debug)
            {
                json = PlanarJobCommon.ShowDebugMenu(typeof(TJob));
            }
            else
            {
                json = GetJsonFromArgs();
            }

            if (PlanarJobCommon.Mode == RunningMode.Debug)
            {
                await Console.Out.WriteLineAsync("---------------------------------------");
                await Console.Out.WriteAsync(">> Environment: ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                await Console.Out.WriteLineAsync(PlanarJobCommon.Environment);
                Console.ResetColor();
                await Console.Out.WriteLineAsync("---------------------------------------");
            }

            var instance = Activator.CreateInstance<TJob>();
            var success = await instance.Execute(json, hostedProperties: null, CancellationToken.None);

            if (PlanarJobCommon.Mode == RunningMode.Debug)
            {
                await instance.PrintDebugSummary(success);
                await Console.Out.WriteLineAsync("---------------------------------------");
                await Console.Out.WriteLineAsync(">> Press any key to exit");
                await Console.Out.WriteLineAsync("---------------------------------------");
                using (var timer = new Timer(60_000))
                {
                    timer.Elapsed += PlanarJobCommon.TimerElapsed;
                    timer.Start();
                    Console.ReadKey(true);
                    timer.Stop();
                }
            }
        }

        private static void FillProperties()
        {
            PlanarJobCommon.InitArguments();
            if (PlanarJobCommon.HasArgument("--planar-service-mode"))
            {
                PlanarJobCommon.Mode = RunningMode.Release;
            }
            else
            {
                PlanarJobCommon.Mode = RunningMode.Debug;
            }

            ContextBase64 = PlanarJobCommon.GetArgument("--context")?.Value;
            PlanarJobCommon.Environment = PlanarJobCommon.GetArgument("--environment")?.Value ?? "Development";
            Encrypted = PlanarJobCommon.HasArgument("--encrypted");
            FileIdentifier = PlanarJobCommon.GetArgument("--file-identifier")?.Value;
        }

        private static string GetJsonFromArgs()
        {
            if (string.IsNullOrWhiteSpace(ContextBase64)) { throw new PlanarJobException("Job was executed with empty context"); }
            if (!string.IsNullOrWhiteSpace(FileIdentifier))
            {
                ContextBase64 = GetContextFromTemporaryFile(FileIdentifier);
            }

            string json;
            if (Encrypted)
            {
                var encryptionKeyBytes = GetEncryptionKeyBytes();
                json = AesGcmStringCipher.Decrypt(ContextBase64, encryptionKeyBytes);
            }
            else
            {
                json = DecodeBase64ToString(ContextBase64);
            }

            return json;
        }

        private static byte[] GetEncryptionKeyBytes()
        {
            var key = _properties.EncryptionKey;

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new PlanarJobException("Encryption key is not provided. Please provide a valid 32-byte Base64 string as the encryption key.");
            }

            var encryptionKeyBytes = Convert.FromBase64String(key);
            if (encryptionKeyBytes.Length == 0)
            {
                throw new PlanarJobException("Encryption key is not provided. Please provide a valid 32-byte Base64 string as the encryption key.");
            }

            if (encryptionKeyBytes.Length != 32)
            {
                throw new PlanarJobException("Invalid encryption key. It must be a 32-byte Base64 string.");
            }

            return encryptionKeyBytes;
        }

        private static string GetContextFromTemporaryFile(string fileIdentifier)
        {
            const string contextFolder = "context";
            fileIdentifier = Path.Combine(contextFolder, $"{fileIdentifier}.ctx");
            if (!File.Exists(fileIdentifier)) { throw new PlanarJobException($"Temporary file '{fileIdentifier}' not found"); }
            var content = File.ReadAllText(fileIdentifier);
            SafeDeleteFile(fileIdentifier);
            if (string.IsNullOrWhiteSpace(content)) { throw new PlanarJobException($"Job was executed with empty context file '{fileIdentifier}'"); }
            return content;
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

        private static void SetupGracefulShutdown()
        {
            AppDomain.CurrentDomain.ProcessExit += (s, a) =>
            {
                try
                {
                    PlanarJobCommon.Cancel();
                }
                catch
                {
                    // *** DO NOTHING, we are shutting down anyway, just try best effort to cancel running jobs *** //
                }
            };

            Console.CancelKeyPress += (sender, args) =>
            {
                Console.WriteLine("\nCtrl+C detected! Performing cleanup...");

                // Prevent the application from terminating immediately
                args.Cancel = true;

                try
                {
                    PlanarJobCommon.Cancel();
                }
                catch
                {
                    // *** DO NOTHING, we are shutting down anyway, just try best effort to cancel running jobs *** //
                }
            };
        }
    }
}