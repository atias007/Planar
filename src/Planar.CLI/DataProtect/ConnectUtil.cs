using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.CLI.DataProtect;

public static class ConnectUtil
{
    public const string DefaultHost = "localhost";
    public const int DefaultPort = 2306;
    public const int DefaultSecurePort = 2610;

    static ConnectUtil()
    {
        InitializeMetadataFolder();
    }

    public static DataProtect.LoginData Current { get; set; } = null!;
    private static string MetadataPath { get; set; } = string.Empty;

    public static async Task DeleteAllLogins()
    {
        try
        {
            var files = Directory.GetFiles(MetadataPath, "*.hash");
            var protector = GetProtector();
            foreach (var file in files)
            {
                var text = await File.ReadAllTextAsync(file);
                text = protector.Unprotect(text);
                var data = JsonConvert.DeserializeObject<LoginData>(text);
                if (data == null) { continue; }
                File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
    }

    public static async Task DeleteLogin(LoginData loginData)
    {
        try
        {
            await DeleteLoginInner(loginData);
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
    }

    private static async Task SafeDeleteLogin(LoginData loginData)
    {
        try
        {
            await DeleteLoginInner(loginData);
        }
        catch
        {
            // Do nothing, just ignore the exception and continue to the next file
        }
    }

    private static async Task DeleteLoginInner(LoginData loginData)
    {
        var files = Directory.GetFiles(MetadataPath, "*.hash");
        var protector = GetProtector();
        foreach (var file in files)
        {
            var text = await File.ReadAllTextAsync(file);
            text = protector.Unprotect(text);
            var data = JsonConvert.DeserializeObject<LoginData>(text);
            if (data == null) { continue; }
            if (data.Key.Equals(loginData.Key))
            {
                File.Delete(file);
                break;
            }
        }
    }

    public static async Task<IReadOnlyList<LoginData>> GetLogins()
    {
        try
        {
            var files = Directory.GetFiles(MetadataPath, "*.hash");
            var loginData = new List<LoginData>();

            foreach (var file in files)
            {
                var data = await GetLoginData(file);
                if (data == null) { continue; }
                if (data.Deprecated)
                {
                    await SafeDeleteLogin(data);
                    continue;
                }
                loginData.Add(data);
            }

            return loginData;
        }
        catch (Exception ex)
        {
            if (CliTrace.Enable) { HandleException(ex); }
            return [];
        }
    }

    public static async Task<CliColors> GetLoginColor(string key)
    {
        var logins = await GetLogins();
        var login = logins.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        return login?.Color ?? CliColors.Default;
    }

    private static async Task<LoginData?> GetLogin(string key)
    {
        var files = Directory.GetFiles(MetadataPath, "*.hash");

        foreach (var file in files)
        {
            var data = await GetLoginData(file);
            if (data == null) { continue; }
            if (string.Equals(data.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return data;
            }
        }

        return null;
    }

    public static async Task Save(LoginData loginData)
    {
        try
        {
            var login = await GetLogin(loginData.Key);
            var name =
                login == null || string.IsNullOrWhiteSpace(login.Filename) ?
                $"{DateTime.UtcNow.Ticks}.hash" :
                login.Filename;

            var filename = Path.Combine(MetadataPath, name);
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

    private static async Task<LoginData?> GetLoginData(string filename)
    {
        try
        {
            var text = await File.ReadAllTextAsync(filename);
            var protector = GetProtector();
            text = protector.Unprotect(text);
            var data = JsonConvert.DeserializeObject<LoginData>(text);
            if (data == null) { return null; }
            data.Filename = filename;
            return data;
        }
        catch (Exception ex)
        {
            // Do nothing, just ignore the exception and continue to the next file
            if (CliTrace.Enable) { HandleException(ex); }
            return null;
        }
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
}