using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Planar.API.Common.Entities;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Spectre.Console;
using System;
using System.IO;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Planar.CLI.DataProtect;

public static class ConnectUtil
{
    // if (!File.Exists(MetadataFilename)) { return; }

    public const string DefaultHost = "localhost";
    private const int DefaultPort = 2306;
    private const int DefaultSecurePort = 2610;

    static ConnectUtil()
    {
        InitializeMetadataFolder();
    }

    public static int GetDefaultPort()
    {
        if (Current.SecureProtocol)
        {
            return DefaultSecurePort;
        }
        else
        {
            return DefaultPort;
        }
    }

    public static CliLoginRequest Current { get; private set; } = new CliLoginRequest();

    private static string MetadataPath { get; set; } = string.Empty;

    public static LoginData? GetSavedLogin(string key)
    {
        try
        {
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return null;
        }
    }

    public static void Logout()
    {
        Current.Username = null;
        Current.Password = null;
    }

    public static void Logout(LoginData login)
    {
        login.Username = null;
        login.Password = null;
        login.Token = null;
    }

    public static void SaveLoginRequest(CliLoginRequest request)
    {
        try
        {
            Current = request;

            var login = ReverseMap(request);
            ////    login.Token = token;
            ////    Data.Logins.RemoveAll(l => l.Key == request.Key);

            ////    if (request.Remember)
            ////    {
            ////        var clear = Data.Logins.Where(l => l.Remember).ToList();
            ////        clear.ForEach(l =>
            ////        {
            ////            l.Remember = false;
            ////            l.RememberDays = null;
            ////        });
            ////    }

            ////    Data.Logins.Add(login);

            ////    Save();
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
    }

    private static LoginData ReverseMap(CliLoginRequest data)
    {
        var result = new LoginData
        {
            Color = data.Color,
            Host = data.Host,
            Password = data.Password,
            Port = data.Port,
            Username = data.Username,
            SecureProtocol = data.SecureProtocol,
            ConnectDate = DateTimeOffset.Now.DateTime
        };

        return result;
    }

    private static IDataProtector GetProtector()
    {
        const string purpose = "RememberConnect";
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDataProtection();
        var provider = serviceCollection.BuildServiceProvider();
        var protector = provider.GetRequiredService<IDataProtectionProvider>();
        return protector.CreateProtector(purpose);
    }

    private static void HandleException(Exception ex)
    {
        AnsiConsole.MarkupLine($"{CliFormat.GetWarningMarkup("fail to read/write saved logins info")}");
        AnsiConsole.MarkupLine($"[{CliFormat.WarningColor}]exception message: {ex.Message.EscapeMarkup()}[/]");
        AnsiConsole.WriteLine(string.Empty.PadLeft(80, '-'));
        AnsiConsole.WriteException(ex);
    }

    private static void InitializeMetadataFolder()
    {
        var dataFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var folder = Path.Combine(dataFolder, nameof(Planar));

        try
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }

        MetadataPath = folder;
    }

    public static async Task Save(LoginData loginData)
    {
        try
        {
            var name = DateTime.UtcNow.Ticks.ToString();
            var filename = Path.Combine(MetadataPath, $"{name}.hash");
            var text = JsonConvert.SerializeObject(loginData);
            var protector = GetProtector();
            text = protector.Protect(text);
            await File.WriteAllTextAsync(filename, text);
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
    }
}