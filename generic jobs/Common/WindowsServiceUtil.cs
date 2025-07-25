using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.ComponentModel;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace Common;

#pragma warning disable CA1416 // Validate platform compatibility

public sealed class WindowsServiceUtil(ILogger logger, string serviceName, string host)
{
    private static readonly RetryPolicy _controllerRetry = Policy
        .Handle<Win32Exception>(ex => ex.Message.Contains("The service cannot accept control messages at this time"))
        .WaitAndRetry(3, count => TimeSpan.FromSeconds(count));

    public static void Continue(ServiceController controller)
    {
        ValidateWindowsServiceOs();

        PerformeControllerAction(controller, c => c.Continue());
    }

    public static void KillServiceProcess(string host, string serviceName)
    {
        ValidateWindowsServiceOs();

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

    public static void Refresh(ServiceController controller)
    {
        ValidateWindowsServiceOs();

        PerformeControllerAction(controller, c => c.Refresh());
    }

    public static void Start(ServiceController controller)
    {
        ValidateWindowsServiceOs();

        PerformeControllerAction(controller, c => c.Start());
    }

    public static void Stop(ServiceController controller)
    {
        ValidateWindowsServiceOs();

        PerformeControllerAction(controller, c => c.Stop());
    }

    public static void Stop(ServiceController controller, bool stopDependentServices)
    {
        ValidateWindowsServiceOs();

        PerformeControllerAction(controller, c => c.Stop(stopDependentServices));
    }

    public static void ValidateWindowsServiceOs()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("this application is only supported on Windows.");
        }
    }

    public ServiceControllerStatus WaitForStatus(ServiceController controller, ServiceControllerStatus status, TimeSpan timeout, bool restart)
    {
        ValidateWindowsServiceOs();

        try
        {
            PerformeControllerAction(controller, c => c.WaitForStatus(status, timeout));

            PerformeControllerAction(controller, c => c.Refresh());

            return controller.Status;
        }
        catch (System.ServiceProcess.TimeoutException)
        {
            if (!restart) { throw new CheckException($"timeout waiting for status {status} of service {controller.ServiceName}"); }

#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.

            logger.LogWarning("service '{Name}' on host '{Host}' is in {Status} status for long time. kill the process", serviceName, host, controller.Status);

#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.

            KillServiceProcess(host, serviceName);

            return RecoverServiceStatusAfterKill(controller).Result;
        }
    }

    private static void PerformeControllerAction(ServiceController controller, Action<ServiceController> action)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("This method is only supported on Windows.");
        }

        _controllerRetry.Execute(() => action.Invoke(controller));
    }

    private static async Task<ServiceControllerStatus> RecoverServiceStatusAfterKill(ServiceController controller)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("This method is only supported on Windows.");
        }

        await Task.Delay(10_000);

        if (controller.Status == ServiceControllerStatus.Running) { return ServiceControllerStatus.Running; }

        if (controller.Status == ServiceControllerStatus.Stopped)
        {
            Start(controller);

            PerformeControllerAction(controller, c => c.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMinutes(3)));

            PerformeControllerAction(controller, c => c.Refresh());
        }

        return controller.Status;
    }
}