using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Job
{
    public static partial class PlanarJob
    {
        private static readonly CancellationTokenSource _mainCancellationTokenSource = new CancellationTokenSource();

        static partial void GracefullShutdownSetup()
        {
            AppDomain.CurrentDomain.ProcessExit += (s, a) =>
            {
                try
                {
                    _mainCancellationTokenSource.Cancel();
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
                    _mainCancellationTokenSource.Cancel();
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
                _mainCancellationTokenSource.Dispose();
            }
            catch
            {
                // *** DO NOTHING, we are shutting down anyway, just try best effort to cancel running jobs *** //
            }
        }
    }
}