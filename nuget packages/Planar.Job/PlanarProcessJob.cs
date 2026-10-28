using System;
using System.Threading;

namespace Planar.Job
{
    public static partial class PlanarJob
    {
        public static void SetupGracefulShutdown()
        {
            AppDomain.CurrentDomain.ProcessExit += (s, a) =>
            {
                try
                {
                    PlanarJobCommon._mainCancellationTokenSource.Cancel();
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
                    PlanarJobCommon._mainCancellationTokenSource.Cancel();
                }
                catch
                {
                    // *** DO NOTHING, we are shutting down anyway, just try best effort to cancel running jobs *** //
                }
            };
        }

        private static void SafeDisposeCancellationTokenSource()
        {
            try
            {
                PlanarJobCommon._mainCancellationTokenSource.Dispose();
            }
            catch
            {
                // *** DO NOTHING, we are shutting down anyway, just try best effort to cancel running jobs *** //
            }
        }
    }
}