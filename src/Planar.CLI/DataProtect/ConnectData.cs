using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Planar.CLI.CliGeneral;
using Planar.CLI.Entities;
using Spectre.Console;
using System;
using System.IO;

namespace Planar.CLI.DataProtect
{
    public static class ConnectData
    {
        static ConnectData()
        {
            InitializeMetadataFolder();
            Load();
        }

        private static UserMetadata Data { get; set; } = new();

        private static string MetadataFilename { get; set; } = string.Empty;

        public static CliLoginRequest? GetLoginRequest()
        {
            try
            {
                FilterOldItems();
                return Data?.LoginRequest;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return null;
            }
        }

        public static void SetLoginRequest(CliLoginRequest request)
        {
            try
            {
                if (!request.Remember) { return; }
                RemoveCurrentLogin();

                request.ConnectDate = DateTimeOffset.Now.Date;
                Data.LoginRequest = request;

                Save();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static void FilterOldItems()
        {
            var login = Data.LoginRequest;

            if (login == null) { return; }

            if (login.ConnectDate.AddDays(login.RememberDays) < DateTimeOffset.Now.Date)
            {
                Data.LoginRequest = null;
            }

            Save();
        }

        public static void Logout()
        {
            Data.LoginRequest = null;
            Save();
        }

        private static string GetLoginKey()
        {
            var loginName = $"{Environment.UserDomainName}|{Environment.UserName}";
            return loginName;
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

        private static void Load()
        {
            try
            {
                if (!File.Exists(MetadataFilename))
                {
                    return;
                }

                var text = File.ReadAllText(MetadataFilename);
                var protector = GetProtector();
                text = protector.Unprotect(text);
                Data = JsonConvert.DeserializeObject<UserMetadata>(text) ?? new UserMetadata();
                FilterOldItems();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static void InitializeMetadataFolder()
        {
            const string filename = "metadata.dat";
            var dataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
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

            MetadataFilename = Path.Combine(folder, filename);
        }

        private static void RemoveCurrentLogin()
        {
            Data.LoginRequest = null;
            Save();
        }

        private static void Save()
        {
            try
            {
                var text = JsonConvert.SerializeObject(Data);
                var protector = GetProtector();
                text = protector.Protect(text);
                File.WriteAllText(MetadataFilename, text);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }
    }
}