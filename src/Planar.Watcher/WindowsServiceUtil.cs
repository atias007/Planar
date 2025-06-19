using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace Planar.Watcher;

public sealed class WindowsServiceUtil(ILogger logger, string serviceName, string host)
{
    public ServiceControllerStatus WaitForStatus(ServiceController controller, ServiceControllerStatus status, TimeSpan timeout, bool restart)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("This method is only supported on Windows.");
        }

        try
        {
            controller.WaitForStatus(status, timeout);
            controller.Refresh();
            return controller.Status;
        }
        catch (System.ServiceProcess.TimeoutException)
        {
            if (!restart) { throw new InvalidOperationException($"timeout waiting for status {status} of service {controller.ServiceName}"); }
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            logger.LogWarning("service '{Name}' on host '{Host}' is in {Status} status for long time. kill the process", serviceName, host, controller.Status);
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            KillServiceProcess();
            return WaitForStatus(controller, ServiceControllerStatus.Stopped, timeout, restart: false);
        }
    }

    public void KillServiceProcess()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("This method is only supported on Windows.");
        }

        var options = new ConnectionOptions { EnablePrivileges = true };
        var scope = new ManagementScope($"\\\\{host}\\root\\cimv2", options);
        scope.Connect();

        var query = new ObjectQuery($"SELECT * FROM Win32_Service WHERE Name='{serviceName}'");
        using var searcher = new ManagementObjectSearcher(scope, query);
        using var results = searcher.Get();

        foreach (var item in results)
        {
            if (item == null) { continue; }

            var processId = (uint)item["ProcessId"];

            // use processid to kill process with taskkill
            try
            {
                var processObjGetOpt = new ObjectGetOptions();
                var processPath = new ManagementPath("Win32_Process");
                using var processClass = new ManagementClass(scope, processPath, processObjGetOpt);
                using var processInParams = processClass.GetMethodParameters("Create");
                processInParams["CommandLine"] = $"cmd /c \"taskkill /f /pid {processId}\"";
                using var outParams = processClass.InvokeMethod("Create", processInParams, null);
                int returnCode = Convert.ToInt32(outParams["returnValue"], CultureInfo.CurrentCulture);
                if (returnCode != 0)
                {
                    Console.WriteLine("Error killing process: " + returnCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    public static void ValidateWindowsServiceOs()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("this application is only supported on Windows.");
        }
    }
}